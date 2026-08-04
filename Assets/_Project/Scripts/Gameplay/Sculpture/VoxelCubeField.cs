using UnityEngine;
using UnityEngine.Rendering;

namespace CubeBlaster
{
    public sealed class VoxelCubeField
    {
        const float MinPunchFalloff = 0.35f;
        const int HashX = 73856093;
        const int HashY = 19349663;
        const int HashZ = 83492791;
        const float HashQuantization = 16f;

        static readonly Vector3Int[] Neighbours =
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        struct VoxelInstance
        {
            public bool Spawned;
            public float BaseScale;
            public int BaseGroup;
            public int FlashGroup;
            public int Slot;
            public bool Punching;
            public float PunchElapsed;
            public float PunchAmount;
            public float PunchFlash;
        }

        readonly Transform _parent;
        readonly MeshInstanceBatcher _batcher = new MeshInstanceBatcher();
        readonly VoxelPopPool _pops = new VoxelPopPool();

        IVoxelGrid _grid;
        VoxelGroupTable _groups;
        VoxelStyle _style;
        Mesh _mesh;

        VoxelInstance[] _instances = new VoxelInstance[0];
        Vector3[] _localPositions = new Vector3[0];

        public VoxelCubeField(Transform parent)
        {
            _parent = parent;
        }

        public int Count => _instances.Length;

        /// Sculptures are SOLID — a level is several thousand cubes, most of them buried where
        /// the player can neither see nor shoot them. Only the EXPOSED ones become instances;
        /// `Reveal` adds a buried cube the moment one of its neighbours dies. That keeps the
        /// drawn count at the surface area of the sculpture no matter how solid it is, which is
        /// what lets the generator drop its cube ceiling entirely.
        ///
        /// Cubes are NOT GameObjects. Each one used to be a Transform + MeshRenderer, i.e. a
        /// draw call, a culling entry and (while punched) a coroutine; the whole sculpture is now
        /// a matrix array submitted through Graphics.DrawMeshInstanced, grouped by material.
        public void Build(IVoxelGrid grid, SculptureLayout layout, VisualLibrary library,
            int paletteIndex, GameConfig config)
        {
            Clear();

            _grid = grid;
            _style = VoxelStyle.From(config);
            _groups = new VoxelGroupTable(library, paletteIndex);
            _mesh = library != null ? library.GetVoxelMesh() : null;
            _batcher.Configure(_groups.Materials);
            _pops.Configure(config.popMaxActive);

            int count = grid.Count;
            _instances = new VoxelInstance[count];
            _localPositions = layout.LocalPositions;

            for (int i = 0; i < count; i++)
                if (IsExposed(i)) Spawn(i);
        }

        public void Clear()
        {
            _grid = null;
            _groups = null;
            _mesh = null;
            _instances = new VoxelInstance[0];
            _localPositions = new Vector3[0];
            _pops.Clear();
        }

        /// Advances every live animation and submits the whole sculpture. Must run every frame —
        /// Graphics.DrawMeshInstanced draws for one frame only.
        public void Tick(float deltaTime, GameConfig config)
        {
            if (_mesh == null || _instances.Length == 0) return;

            _batcher.Begin();

            Matrix4x4 root = _parent.localToWorldMatrix;
            float punchDuration = Mathf.Max(0.0001f, config.hitPunchTime);
            float flashTime = config.hitFlashTime;
            float punchSpan = Mathf.Max(punchDuration, flashTime);

            for (int i = 0; i < _instances.Length; i++)
            {
                ref var instance = ref _instances[i];
                if (!instance.Spawned) continue;

                float scale = instance.BaseScale;
                int group = instance.BaseGroup;

                if (instance.Punching)
                {
                    instance.PunchElapsed += deltaTime;
                    float elapsed = instance.PunchElapsed;

                    if (elapsed < punchDuration)
                        scale *= 1f + instance.PunchAmount * Ease.Pulse(elapsed / punchDuration);

                    if (flashTime > 0f && instance.PunchFlash > 0f)
                    {
                        int level = ColorTools.PickFlashLevel(
                            Mathf.Max(0f, 1f - elapsed / flashTime) * instance.PunchFlash);
                        if (level > 0) group = instance.FlashGroup + level - 1;
                    }

                    if (elapsed >= punchSpan) instance.Punching = false;
                }

                _batcher.Add(group, Compose(ref root, _localPositions[i], scale));
            }

            _pops.Tick(deltaTime, PopSettings.From(config), _batcher);

            // receiveShadows is false to match: with no caster in the scene the only thing a
            // shadow-receiving draw buys is the extra shader variant and the per-draw shadow
            // sampling state. Turn both back on together for a desktop-only build.
            _batcher.Draw(_mesh, _parent.gameObject.layer,
                config.voxelCastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows: config.voxelCastShadows);
        }

        public bool IsSpawned(int index) =>
            index >= 0 && index < _instances.Length && _instances[index].Spawned;

        public Color GetColor(int index) => _groups != null && IsSpawned(index)
            ? _groups.GetColor(_instances[index].Slot)
            : Color.white;

        /// Hands the cube over to the pop pool and takes it out of the sculpture. The pop plays
        /// in world space so it stops following the turntable, exactly as the old detached
        /// GameObject did.
        public void PopCube(int index, Vector3 outward, PopSettings settings)
        {
            if (!IsSpawned(index)) return;

            ref var instance = ref _instances[index];
            _pops.Play(
                _parent.TransformPoint(_localPositions[index]),
                _parent.rotation,
                instance.BaseScale * _parent.lossyScale.x,
                outward,
                instance.BaseGroup,
                instance.FlashGroup,
                settings);

            instance.Spawned = false;
            instance.Punching = false;
        }

        /// Adds the still-living neighbours of a just-destroyed voxel. They were buried a moment
        /// ago, so losing that neighbour is exactly what exposes them.
        public void Reveal(int index)
        {
            if (_grid == null || index < 0 || index >= _instances.Length) return;

            var cell = _grid.GetCell(index);
            for (int n = 0; n < Neighbours.Length; n++)
            {
                int neighbour = _grid.GetIndexAt(
                    cell.X + Neighbours[n].x, cell.Y + Neighbours[n].y, cell.Z + Neighbours[n].z);
                if (neighbour < 0 || !_grid.IsAlive(neighbour)) continue;
                if (!_instances[neighbour].Spawned) Spawn(neighbour);
            }
        }

        public Vector3 LocalPosition(int index) =>
            index >= 0 && index < _localPositions.Length ? _localPositions[index] : Vector3.zero;

        public bool HasLocalPosition(int index) => index >= 0 && index < _localPositions.Length;

        /// Walks the grid neighbourhood rather than the whole voxel array. The radius is stated
        /// in CELLS, so the integer box around the epicentre already contains every candidate —
        /// and at a hundred-plus destroys a second, scanning several thousand entries per hit was
        /// a bigger main-thread cost than the draw calls this pass set out to remove.
        public void PunchAround(int index, float cellSize, GameConfig config)
        {
            if (config.hitPunchScale <= 0f || _grid == null || !HasLocalPosition(index)) return;

            Vector3 epicenter = _localPositions[index];
            float maxDistance = cellSize * config.hitPunchRadius;
            if (maxDistance <= 0f) return;
            float maxDistanceSqr = maxDistance * maxDistance;

            var cell = _grid.GetCell(index);
            int reach = Mathf.CeilToInt(config.hitPunchRadius);

            for (int dx = -reach; dx <= reach; dx++)
            for (int dy = -reach; dy <= reach; dy++)
            for (int dz = -reach; dz <= reach; dz++)
            {
                int neighbour = _grid.GetIndexAt(cell.X + dx, cell.Y + dy, cell.Z + dz);
                if (neighbour < 0 || !_instances[neighbour].Spawned) continue;

                float distanceSqr = (_localPositions[neighbour] - epicenter).sqrMagnitude;
                if (distanceSqr > maxDistanceSqr) continue;

                float falloff = Mathf.Max(MinPunchFalloff, 1f - Mathf.Sqrt(distanceSqr) / maxDistance);
                ref var instance = ref _instances[neighbour];
                instance.Punching = true;
                instance.PunchElapsed = 0f;
                instance.PunchAmount = config.hitPunchScale * falloff;
                instance.PunchFlash = Mathf.Clamp01(config.hitFlashIntensity * falloff);
            }
        }

        bool IsExposed(int index)
        {
            var cell = _grid.GetCell(index);
            for (int n = 0; n < Neighbours.Length; n++)
            {
                int neighbour = _grid.GetIndexAt(
                    cell.X + Neighbours[n].x, cell.Y + Neighbours[n].y, cell.Z + Neighbours[n].z);
                if (neighbour < 0 || !_grid.IsAlive(neighbour)) return true;
            }
            return false;
        }

        void Spawn(int index)
        {
            var cell = _grid.GetCell(index);
            int seed = PositionSeed(_localPositions[index]);

            ref var instance = ref _instances[index];
            instance.Spawned = true;
            instance.Slot = cell.ColorIndex;
            instance.BaseScale = _style.Size *
                                 (1f + ColorTools.GetQuantizedOffset(seed >> 3) * _style.ScaleJitter);
            instance.BaseGroup = _groups.GetBaseGroup(cell.ColorIndex, ColorTools.PickJitterVariant(seed));
            instance.FlashGroup = _groups.GetFlashGroup(cell.ColorIndex, 1);
            instance.Punching = false;
        }

        /// root * translate(local) * uniformScale, written out because the cubes carry no rotation
        /// of their own — a full Matrix4x4 multiply is four times the arithmetic for the same
        /// result, and this runs once per drawn cube per frame.
        static Matrix4x4 Compose(ref Matrix4x4 root, Vector3 local, float scale)
        {
            Matrix4x4 m;
            m.m00 = root.m00 * scale; m.m01 = root.m01 * scale; m.m02 = root.m02 * scale;
            m.m10 = root.m10 * scale; m.m11 = root.m11 * scale; m.m12 = root.m12 * scale;
            m.m20 = root.m20 * scale; m.m21 = root.m21 * scale; m.m22 = root.m22 * scale;
            m.m03 = root.m00 * local.x + root.m01 * local.y + root.m02 * local.z + root.m03;
            m.m13 = root.m10 * local.x + root.m11 * local.y + root.m12 * local.z + root.m13;
            m.m23 = root.m20 * local.x + root.m21 * local.y + root.m22 * local.z + root.m23;
            m.m30 = 0f; m.m31 = 0f; m.m32 = 0f; m.m33 = 1f;
            return m;
        }

        static int PositionSeed(Vector3 localPosition) =>
            Mathf.RoundToInt(localPosition.x * HashQuantization) * HashX
            ^ Mathf.RoundToInt(localPosition.y * HashQuantization) * HashY
            ^ Mathf.RoundToInt(localPosition.z * HashQuantization) * HashZ;
    }
}
