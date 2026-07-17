using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Builds the voxel sculpture from a <see cref="VoxelModel"/> and animates destruction.
    /// The sculpture is a turntable: it auto-rotates while idle and can be spun by dragging on it
    /// (see BoardInput). Cubes are children at LOCAL offsets so rotation pivots on the base center;
    /// world positions are computed live so darts still hit voxels that are turning.
    /// Cube materials come from the baked VisualLibrary assets; extra destruction fragments are
    /// spawned from the Debris prefab (both hand-editable in the editor).
    /// </summary>
    public class SculptureView : MonoBehaviour
    {
        [SerializeField] VoxelCube voxelPrefab;

        VoxelModel _model;
        VoxelCube[] _cubes;
        Vector3[] _localPos;
        float _cell;
        float _yaw;
        float _idle;
        bool _spinning;
        public Bounds Bounds { get; private set; }

        public void Init(VoxelModel model, int paletteIndex)
        {
            _model = model;
            var cfg = Cfg.Active;
            _cell = cfg.voxelSize + cfg.voxelGap;

            // The sculpture object sits at the configured center and rotates about it.
            transform.position = cfg.sculptureCenter;
            _yaw = 0f; _idle = 0f;
            transform.rotation = Quaternion.Euler(cfg.sculptureTilt, 0f, 0f);

            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            int n = model.Count;
            _cubes = new VoxelCube[n];
            _localPos = new Vector3[n];

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < n; i++)
            {
                var c = model.GetCell(i);
                Vector3 g = new Vector3(c.x, c.y, c.z);
                min = Vector3.Min(min, g); max = Vector3.Max(max, g);
            }
            Vector3 gridCenter = (min + max) * 0.5f;

            var lib = Visuals.Active;
            for (int i = 0; i < n; i++)
            {
                var c = model.GetCell(i);
                // centered on X/Z, base sits at the object origin (y grows upward)
                Vector3 local = new Vector3(c.x - gridCenter.x, c.y - min.y, c.z - gridCenter.z) * _cell;
                _localPos[i] = local;

                var cube = Instantiate(voxelPrefab, transform);
                cube.transform.localPosition = local;
                cube.transform.localRotation = Quaternion.identity;
                cube.Init(lib.VoxelMaterial(paletteIndex, c.c), lib.VoxelColor(paletteIndex, c.c), cfg.voxelSize);
                _cubes[i] = cube;
            }

            // World bounds (rotation-invariant estimate) for camera fit.
            float height = (max.y - min.y) * _cell + cfg.voxelSize;
            float radius = Mathf.Max(max.x - min.x, max.z - min.z) * 0.5f * _cell + cfg.voxelSize;
            Vector3 center = cfg.sculptureCenter + new Vector3(0, height * 0.5f - cfg.voxelSize * 0.5f, 0);
            Bounds = new Bounds(center, new Vector3(radius * 2f, height, radius * 2f));

            model.VoxelDestroyed += OnVoxelDestroyed;
        }

        /// <summary>Current world position of a voxel (accounts for live rotation).</summary>
        public Vector3 GetWorldPos(int index) =>
            (_localPos != null && index >= 0 && index < _localPos.Length)
                ? transform.TransformPoint(_localPos[index])
                : Cfg.Active.sculptureCenter;

        /// <summary>Spin the turntable by a horizontal drag delta (pixels).</summary>
        public void ApplyYaw(float pixelsDeltaX)
        {
            _yaw += pixelsDeltaX * Cfg.Active.rotateSensitivity;
            _idle = 0f;
            _spinning = true;
        }

        public void EndSpin() { _spinning = false; _idle = 0f; }

        void Update()
        {
            if (!_spinning)
            {
                _idle += Time.deltaTime;
                if (_idle >= Cfg.Active.autoRotateDelay)
                    _yaw += Cfg.Active.autoRotateSpeed * Time.deltaTime;
            }
            // Lean back toward the camera first, then spin about the sculpture's own tilted axis.
            transform.rotation = Quaternion.Euler(Cfg.Active.sculptureTilt, 0f, 0f) * Quaternion.Euler(0f, _yaw, 0f);
        }

        void OnVoxelDestroyed(int index)
        {
            if (_cubes == null || index < 0 || index >= _cubes.Length) return;
            var cube = _cubes[index];
            if (cube == null) return;
            var cfg = Cfg.Active;
            Vector3 pos = cube.transform.position;
            Vector3 dir = pos - transform.position;
            dir.y = Mathf.Abs(dir.y) + 0.5f;
            // Layered destruction (art doc): contact flash, big chunk (the cube itself), mid
            // fragments for density, fast small slivers, particle burst + stretched shard streaks.
            Fx.Flash(pos, cube.Color);
            Fx.Burst(pos, cube.Color);
            Fx.Shards(pos, cube.Color);
            SpawnFragments(pos, cube.SharedMaterial, dir.normalized, cfg);
            cube.Explode(dir, cfg.debrisForce, cfg.debrisTorque, cfg.debrisLife);
            _cubes[index] = null;
        }

        void SpawnFragments(Vector3 pos, Material mat, Vector3 outDir, GameConfig cfg)
        {
            // Medium fragments (50-70% of a block): weighty, tumble, can bounce.
            for (int i = 0; i < cfg.debrisMediumCount; i++)
                SpawnFragment(pos, mat, outDir, cfg,
                    Random.Range(0.5f, 0.7f), Random.Range(3.0f, 4.8f), cfg.debrisLife, withCollider: true);
            // Small fragments (20-40%): fastest, short-lived, no collision cost.
            for (int i = 0; i < cfg.debrisSmallCount; i++)
                SpawnFragment(pos, mat, outDir, cfg,
                    Random.Range(0.2f, 0.4f), Random.Range(4.5f, 7.0f), cfg.debrisLife * 0.6f, withCollider: false);
        }

        static void SpawnFragment(Vector3 pos, Material mat, Vector3 outDir, GameConfig cfg,
            float scale, float speed, float life, bool withCollider)
        {
            var prefab = Visuals.Active.debrisPrefab;
            if (prefab == null) return;
            var d = Object.Instantiate(prefab);
            d.transform.position = pos + Random.insideUnitSphere * cfg.voxelSize * 0.25f;
            d.transform.rotation = Random.rotation;
            d.transform.localScale = Vector3.one * cfg.voxelSize * scale;
            d.Launch(mat,
                (outDir + Random.insideUnitSphere * 0.6f).normalized * speed,
                Random.insideUnitSphere * 10f,
                withCollider, life);
        }

        void OnDestroy()
        {
            if (_model != null) _model.VoxelDestroyed -= OnVoxelDestroyed;
        }
    }
}
