using UnityEngine;

namespace CubeBlaster
{
    public sealed class VoxelCubeField
    {
        const float MinPunchFalloff = 0.35f;

        readonly VoxelCube _prefab;
        readonly Transform _parent;

        VoxelCube[] _cubes = new VoxelCube[0];
        Vector3[] _localPositions = new Vector3[0];

        public VoxelCubeField(VoxelCube prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public int Count => _cubes.Length;

        public void Build(IVoxelGrid grid, SculptureLayout layout, VisualLibrary library,
            int paletteIndex, VoxelStyle style)
        {
            Clear();

            int count = grid.Count;
            _cubes = new VoxelCube[count];
            _localPositions = layout.LocalPositions;

            for (int i = 0; i < count; i++)
            {
                var cell = grid.GetCell(i);
                var cube = Object.Instantiate(_prefab, _parent);
                cube.transform.localPosition = _localPositions[i];
                cube.transform.localRotation = Quaternion.identity;
                cube.Initialize(
                    library.GetVoxelMaterial(paletteIndex, cell.ColorIndex),
                    library.GetVoxelColor(paletteIndex, cell.ColorIndex),
                    style);
                _cubes[i] = cube;
            }
        }

        public void Clear()
        {
            if (_parent == null) return;
            for (int i = _parent.childCount - 1; i >= 0; i--)
                Object.Destroy(_parent.GetChild(i).gameObject);
            _cubes = new VoxelCube[0];
            _localPositions = new Vector3[0];
        }

        public VoxelCube DetachCube(int index)
        {
            if (index < 0 || index >= _cubes.Length) return null;
            var cube = _cubes[index];
            _cubes[index] = null;
            return cube;
        }

        public Vector3 LocalPosition(int index) =>
            index >= 0 && index < _localPositions.Length ? _localPositions[index] : Vector3.zero;

        public bool HasLocalPosition(int index) => index >= 0 && index < _localPositions.Length;

        public void PunchAround(int index, float cellSize, GameConfig config)
        {
            if (config.hitPunchScale <= 0f || !HasLocalPosition(index)) return;

            Vector3 epicenter = _localPositions[index];
            float maxDistance = cellSize * config.hitPunchRadius;
            float maxDistanceSqr = maxDistance * maxDistance;

            for (int i = 0; i < _cubes.Length; i++)
            {
                var cube = _cubes[i];
                if (cube == null) continue;

                float distanceSqr = (_localPositions[i] - epicenter).sqrMagnitude;
                if (distanceSqr > maxDistanceSqr) continue;

                float falloff = Mathf.Max(MinPunchFalloff, 1f - Mathf.Sqrt(distanceSqr) / maxDistance);
                float amount = config.hitPunchScale * falloff;
                float flash = Mathf.Clamp01(config.hitFlashIntensity * falloff);
                cube.Punch(new PunchSettings(amount, config.hitPunchTime, config.hitFlashTime, flash));
            }
        }
    }
}
