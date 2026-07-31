using UnityEngine;

namespace CubeBlaster
{
    public sealed class VoxelCubeField
    {
        const float MinPunchFalloff = 0.35f;

        static readonly Vector3Int[] Neighbours =
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        readonly VoxelCube _prefab;
        readonly Transform _parent;

        IVoxelGrid _grid;
        VisualLibrary _library;
        int _paletteIndex;
        VoxelStyle _style;

        VoxelCube[] _cubes = new VoxelCube[0];
        Vector3[] _localPositions = new Vector3[0];

        public VoxelCubeField(VoxelCube prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public int Count => _cubes.Length;

        /// Sculptures are SOLID — a level is several thousand cubes, most of them buried where
        /// the player can neither see nor shoot them. Only the EXPOSED ones get a GameObject;
        /// `Reveal` spawns a buried cube the moment one of its neighbours dies. That keeps the
        /// renderer count at the surface area of the sculpture no matter how solid it is, which
        /// is what lets the generator drop its cube ceiling entirely. Do not go back to
        /// instantiating every voxel: the buried ones are pure cost with nothing on screen.
        public void Build(IVoxelGrid grid, SculptureLayout layout, VisualLibrary library,
            int paletteIndex, VoxelStyle style)
        {
            Clear();

            _grid = grid;
            _library = library;
            _paletteIndex = paletteIndex;
            _style = style;

            int count = grid.Count;
            _cubes = new VoxelCube[count];
            _localPositions = layout.LocalPositions;

            for (int i = 0; i < count; i++)
                if (IsExposed(i)) Spawn(i);
        }

        public void Clear()
        {
            if (_parent != null)
                for (int i = _parent.childCount - 1; i >= 0; i--)
                    Object.Destroy(_parent.GetChild(i).gameObject);

            _grid = null;
            _library = null;
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

        /// Spawns the still-living neighbours of a just-destroyed voxel. They were buried a
        /// moment ago, so losing that neighbour is exactly what exposes them.
        public void Reveal(int index)
        {
            if (_grid == null || index < 0 || index >= _cubes.Length) return;

            var cell = _grid.GetCell(index);
            for (int n = 0; n < Neighbours.Length; n++)
            {
                int neighbour = _grid.GetIndexAt(
                    cell.X + Neighbours[n].x, cell.Y + Neighbours[n].y, cell.Z + Neighbours[n].z);
                if (neighbour < 0 || !_grid.IsAlive(neighbour)) continue;
                if (_cubes[neighbour] == null) Spawn(neighbour);
            }
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
            int variant = ColorTools.PickJitterVariant(VoxelCube.PositionSeed(_localPositions[index]));

            var cube = Object.Instantiate(_prefab, _parent);
            cube.transform.localPosition = _localPositions[index];
            cube.transform.localRotation = Quaternion.identity;
            cube.Initialize(
                _library.GetVoxelMaterial(_paletteIndex, cell.ColorIndex, variant),
                _library.GetVoxelColor(_paletteIndex, cell.ColorIndex),
                _library.GetVoxelColor(_paletteIndex, cell.ColorIndex, variant),
                _style);
            _cubes[index] = cube;
        }
    }
}
