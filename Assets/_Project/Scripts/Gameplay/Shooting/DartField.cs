using UnityEngine;
using UnityEngine.Rendering;

namespace CubeBlaster
{
    /// Every dart in the air, as an array of states drawn through one instanced call per colour.
    ///
    /// A dart used to be a GameObject: a root Transform, a TrailRenderer, a billboarded bullet
    /// quad and a MaterialPropertyBlock tint. Four guns at the current fire interval keep ~90 of
    /// them alive at once, so that was ~200 extra draw calls and ~130 Instantiate/Destroy pairs a
    /// second — on WebGL, where each draw crosses the JS/WASM boundary, easily the second biggest
    /// cost in the frame after the sculpture.
    ///
    /// It still LOOKS the same: a round bullet with a tapering tail behind it, drawn as two quads
    /// per dart. They are two because the tail quad is stretched ~19:1 and anything sharing it is
    /// stretched too — a bullet painted into the tail's texture comes out as a ray, not a dot.
    /// Both are tinted by BAKED per-colour materials, because grouping IS the batching and a
    /// per-dart tint would put every dart back in a draw call of its own; a barrage is therefore
    /// two draws per colour on screen, whatever the dart count.
    public sealed class DartField
    {
        /// Floor under GameConfig.dartPoolCapacity, for the case where the field is ticked before
        /// a config is resolved. A dart may never be recycled while it is still flying — ammo is
        /// exact, so a dart that disappears without resolving its hit leaves a voxel nobody can
        /// ever shoot — so the array GROWS rather than overwriting the oldest, and a grow is a
        /// full array copy on the busiest frame of the level. Sizing it once at Configure is
        /// what keeps that off the frame where all four guns open up.
        const int MinimumCapacity = 32;
        const float MinArcLength = 0.1f;
        const float MinSquaredMagnitude = 1e-8f;

        /// Tail first, bullet second, so the bullet draws over its own tail.
        const int GroupsPerSlot = 2;
        const int StreakGroup = 0;
        const int BulletGroup = 1;

        struct DartInstance
        {
            public bool Active;
            public int VoxelIndex;
            public int Group;
            public Vector3 Position;
            public Vector3 Start;
            public Vector3 MuzzleDirection;
            public Vector3 Heading;
            public Vector3 Target;
            public float Progress;
            public float Life;
            public float Age;
            public bool Diving;
        }

        readonly Transform _host;
        readonly MeshInstanceBatcher _batcher = new MeshInstanceBatcher();

        IDartContext _context;
        Mesh _mesh;
        DartInstance[] _darts = new DartInstance[MinimumCapacity];
        int _next;

        public DartField(Transform host)
        {
            _host = host;
        }

        public void Configure(IDartContext context, VisualLibrary library, int paletteSet)
        {
            Reserve(GameConfig.Active.dartPoolCapacity);
            Clear();
            _context = context;
            _mesh = library != null ? library.GetDartMesh() : null;
            _batcher.Configure(BuildMaterials(library, paletteSet));
        }

        /// Grows the array to the configured capacity at LEVEL LOAD, where a copy costs nothing,
        /// so the peak of a four-gun barrage never has to. Never shrinks: a smaller array would
        /// have to drop darts that are still in the air.
        void Reserve(int capacity)
        {
            capacity = Mathf.Max(MinimumCapacity, capacity);
            if (_darts.Length < capacity) _darts = new DartInstance[capacity];
        }

        public void Clear()
        {
            for (int i = 0; i < _darts.Length; i++) _darts[i].Active = false;
            _next = 0;
        }

        public void Spawn(int voxelIndex, int colorSlot, Vector3 origin, Vector3 muzzleDirection,
            Vector3 target, GameConfig config)
        {
            int index = TakeFreeIndex();
            _darts[index] = new DartInstance
            {
                Active = true,
                VoxelIndex = voxelIndex,
                Group = Mathf.Max(0, colorSlot),
                Position = origin,
                Start = origin,
                MuzzleDirection = muzzleDirection.sqrMagnitude > 1e-6f
                    ? muzzleDirection.normalized
                    : Vector3.forward,
                Heading = muzzleDirection.sqrMagnitude > 1e-6f
                    ? muzzleDirection.normalized
                    : Vector3.forward,
                Target = target,
                Progress = 0f,
                Life = config.dartLife,
                Age = 0f,
                Diving = false
            };
        }

        /// Advances every dart and submits the whole swarm. Must run every frame —
        /// Graphics.DrawMeshInstanced draws for one frame only.
        public void Tick(float deltaTime, GameConfig config)
        {
            if (_mesh == null) return;

            _batcher.Begin();

            var camera = CameraRig.Main;
            Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
            Vector3 cameraUp = camera != null ? camera.transform.up : Vector3.up;
            Quaternion cameraRotation = camera != null ? camera.transform.rotation : Quaternion.identity;
            float trailTime = Mathf.Max(0.0001f, config.dartTrailTime);
            float length = trailTime * config.dartSpeed;
            float width = config.dartTrailWidth;
            Vector3 bulletScale = Vector3.one * config.dartBulletSize;

            for (int i = 0; i < _darts.Length; i++)
            {
                ref var dart = ref _darts[i];
                if (!dart.Active) continue;

                Advance(ref dart, deltaTime, config);
                if (!dart.Active) continue;

                int slot = dart.Group * GroupsPerSlot;

                // The old TrailRenderer grew its streak over dartTrailTime, so a dart just out of
                // the muzzle wore a short one. Without this the full-length tail pops into
                // existence at the barrel, which reads as a flash rather than a shot.
                float grown = length * Mathf.Clamp01(dart.Age / trailTime);
                if (grown > 0f)
                    _batcher.Add(slot + StreakGroup, ComposeStreak(dart.Position, dart.Heading,
                        grown, width, cameraPosition, cameraUp));

                _batcher.Add(slot + BulletGroup,
                    Matrix4x4.TRS(dart.Position, cameraRotation, bulletScale));
            }

            _batcher.Draw(_mesh, _host != null ? _host.gameObject.layer : 0,
                ShadowCastingMode.Off, receiveShadows: false);
        }

        void Advance(ref DartInstance dart, float deltaTime, GameConfig config)
        {
            dart.Age += deltaTime;
            dart.Life -= deltaTime;
            if (dart.Life <= 0f)
            {
                Arrive(ref dart);
                return;
            }

            if (_context != null) dart.Target = _context.GetVoxelWorldPosition(dart.VoxelIndex);

            float step = config.dartSpeed * deltaTime;
            if (dart.Diving) Dive(ref dart, step);
            else FollowArc(ref dart, step, config);
        }

        void Dive(ref DartInstance dart, float step)
        {
            Vector3 toTarget = dart.Target - dart.Position;
            if (toTarget.magnitude <= step)
            {
                Arrive(ref dart);
                return;
            }
            toTarget.Normalize();
            dart.Position += toTarget * step;
            dart.Heading = toTarget;
        }

        void FollowArc(ref DartInstance dart, float step, GameConfig config)
        {
            var arc = DartArc.Create(dart.Start, dart.MuzzleDirection, dart.Target,
                ApproachDirection(dart.Start, dart.Target), config.dartApproachOffset);

            dart.Progress += step / Mathf.Max(MinArcLength, arc.Length);
            if (dart.Progress >= 1f)
            {
                dart.Diving = true;
                dart.Position = arc.Approach;
                return;
            }

            Vector3 next = arc.Sample(dart.Progress);
            Vector3 velocity = next - dart.Position;
            dart.Position = next;
            if (velocity.sqrMagnitude > MinSquaredMagnitude) dart.Heading = velocity.normalized;
        }

        void Arrive(ref DartInstance dart)
        {
            dart.Active = false;
            if (_context != null) _context.ResolveDartHit(dart.VoxelIndex, dart.Position);
        }

        static Vector3 ApproachDirection(Vector3 start, Vector3 target)
        {
            var camera = CameraRig.Main;
            return camera != null
                ? (camera.transform.position - target).normalized
                : (start - target).normalized;
        }

        int TakeFreeIndex()
        {
            for (int scanned = 0; scanned < _darts.Length; scanned++)
            {
                int index = _next;
                _next = (_next + 1) % _darts.Length;
                if (!_darts[index].Active) return index;
            }

            int previousLength = _darts.Length;
            System.Array.Resize(ref _darts, previousLength * 2);
            _next = previousLength + 1;
            return previousLength;
        }

        /// The tail's +Y runs along the direction of travel with its widest end at the bullet, so
        /// the quad is placed half a length behind the dart. Its normal is the component of the
        /// view ray perpendicular to that heading — an ordinary billboard (which is right for the
        /// bullet) would twist the tail off its own flight path.
        static Matrix4x4 ComposeStreak(Vector3 position, Vector3 heading, float length, float width,
            Vector3 cameraPosition, Vector3 cameraUp)
        {
            Vector3 toCamera = cameraPosition - position;
            Vector3 right = Vector3.Cross(heading, toCamera);
            if (right.sqrMagnitude < MinSquaredMagnitude) right = Vector3.Cross(heading, cameraUp);
            if (right.sqrMagnitude < MinSquaredMagnitude) right = Vector3.Cross(heading, Vector3.right);

            // Away from the camera: the built-in quad's visible face is its -Z, the same
            // convention Billboard uses when it copies the camera's rotation outright.
            Vector3 normal = Vector3.Cross(right.normalized, heading);

            return Matrix4x4.TRS(
                position - heading * (length * 0.5f),
                Quaternion.LookRotation(normal, heading),
                new Vector3(width, length, 1f));
        }

        static Material[] BuildMaterials(VisualLibrary library, int paletteSet)
        {
            int slots = library != null ? library.GetVoxelSlotCount(paletteSet) : 0;
            var materials = new Material[slots * GroupsPerSlot];
            for (int slot = 0; slot < slots; slot++)
            {
                materials[slot * GroupsPerSlot + StreakGroup] =
                    library.GetDartStreakMaterial(paletteSet, slot);
                materials[slot * GroupsPerSlot + BulletGroup] =
                    library.GetDartMaterial(paletteSet, slot);
            }
            return materials;
        }
    }
}
