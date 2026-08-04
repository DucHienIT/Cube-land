using System;
using System.Collections.Generic;

namespace CubeBlaster
{
    public class VoxelModel : IVoxelGrid
    {
        readonly List<VoxelCell> _cells = new List<VoxelCell>();
        readonly Dictionary<int, int> _alivePerColor = new Dictionary<int, int>();
        readonly Dictionary<(int x, int y, int z), int> _byPosition =
            new Dictionary<(int x, int y, int z), int>();

        int _aliveCount;
        int _minX, _maxX, _minY, _maxY, _minZ, _maxZ;

        public event Action<int> VoxelDestroyed;
        public event Action AllCleared;

        public int Count => _cells.Count;
        public int AliveCount => _aliveCount;
        public bool Cleared => _aliveCount == 0;

        public VoxelModel(LevelData data)
        {
            _minX = _minY = _minZ = int.MaxValue;
            _maxX = _maxY = _maxZ = int.MinValue;
            if (data?.voxels == null) return;

            for (int i = 0; i < data.voxels.Length; i++)
            {
                var voxel = data.voxels[i];
                _cells.Add(new VoxelCell(voxel.x, voxel.y, voxel.z, voxel.c, true));
                _alivePerColor.TryGetValue(voxel.c, out int aliveOfColor);
                _alivePerColor[voxel.c] = aliveOfColor + 1;
                _byPosition[(voxel.x, voxel.y, voxel.z)] = i;
                Encompass(voxel.x, voxel.y, voxel.z);
            }
            _aliveCount = _cells.Count;
        }

        void Encompass(int x, int y, int z)
        {
            if (x < _minX) _minX = x;
            if (x > _maxX) _maxX = x;
            if (y < _minY) _minY = y;
            if (y > _maxY) _maxY = y;
            if (z < _minZ) _minZ = z;
            if (z > _maxZ) _maxZ = z;
        }

        public int CountAliveOfColor(int colorIndex) =>
            _alivePerColor.TryGetValue(colorIndex, out int alive) ? alive : 0;

        public int GetIndexAt(int x, int y, int z) =>
            _byPosition.TryGetValue((x, y, z), out int index) ? index : -1;

        public bool InBounds(int x, int y, int z) =>
            x >= _minX && x <= _maxX && y >= _minY && y <= _maxY && z >= _minZ && z <= _maxZ;

        public VoxelCell GetCell(int index) => _cells[index];

        public bool IsAlive(int index) => index >= 0 && index < _cells.Count && _cells[index].Alive;

        public bool DestroyVoxel(int index)
        {
            if (!IsAlive(index)) return false;

            var cell = _cells[index];
            _cells[index] = cell.AsDead();
            _aliveCount--;
            if (_alivePerColor.TryGetValue(cell.ColorIndex, out int alive))
                _alivePerColor[cell.ColorIndex] = alive - 1;

            VoxelDestroyed?.Invoke(index);
            if (_aliveCount == 0) AllCleared?.Invoke();
            return true;
        }
    }
}
