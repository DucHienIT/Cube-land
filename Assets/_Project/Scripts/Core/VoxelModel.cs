using System;
using System.Collections.Generic;

namespace CubeBlaster
{
    /// <summary>
    /// Pure-logic runtime state of a sculpture. Views subscribe to events; no UnityEngine types
    /// beyond the integer grid so this can be mirrored/tested outside the editor.
    /// </summary>
    public class VoxelModel
    {
        public struct Cell
        {
            public int x, y, z, c;
            public bool alive;
        }

        readonly List<Cell> _cells = new List<Cell>();
        readonly Dictionary<int, int> _alivePerColor = new Dictionary<int, int>();
        int _aliveCount;

        public int Count => _cells.Count;
        public int AliveCount => _aliveCount;
        public bool Cleared => _aliveCount == 0;

        /// <summary>How many voxels of the given color slot are still alive.</summary>
        public int AliveCountOfColor(int color) =>
            _alivePerColor.TryGetValue(color, out int n) ? n : 0;

        /// <summary>Fires with the index of a voxel that was just destroyed.</summary>
        public event Action<int> VoxelDestroyed;
        /// <summary>Fires once when the last voxel is destroyed.</summary>
        public event Action AllCleared;

        public VoxelModel(LevelData data)
        {
            if (data != null && data.voxels != null)
            {
                for (int i = 0; i < data.voxels.Length; i++)
                {
                    var v = data.voxels[i];
                    _cells.Add(new Cell { x = v.x, y = v.y, z = v.z, c = v.c, alive = true });
                    _alivePerColor.TryGetValue(v.c, out int n);
                    _alivePerColor[v.c] = n + 1;
                }
            }
            _aliveCount = _cells.Count;
        }

        public Cell GetCell(int index) => _cells[index];

        /// <summary>Destroy the voxel at index (no-op if already gone). Returns true if it died now.</summary>
        public bool Destroy(int index)
        {
            if (index < 0 || index >= _cells.Count) return false;
            var c = _cells[index];
            if (!c.alive) return false;
            c.alive = false;
            _cells[index] = c;
            _aliveCount--;
            if (_alivePerColor.TryGetValue(c.c, out int n)) _alivePerColor[c.c] = n - 1;
            VoxelDestroyed?.Invoke(index);
            if (_aliveCount == 0) AllCleared?.Invoke();
            return true;
        }

        public bool IsAlive(int index) => index >= 0 && index < _cells.Count && _cells[index].alive;

        /// <summary>Pick a live voxel to target. Prefers higher-up voxels so towers topple visibly.</summary>
        public int PickTarget(Func<double> rng)
        {
            if (_aliveCount == 0) return -1;
            // Bias toward the highest live band for a satisfying top-down collapse.
            int maxY = int.MinValue;
            for (int i = 0; i < _cells.Count; i++)
                if (_cells[i].alive && _cells[i].y > maxY) maxY = _cells[i].y;

            // 70% of the time hit near the top band, else any live voxel.
            bool topBias = rng() < 0.7;
            int lo = topBias ? Math.Max(int.MinValue, maxY - 2) : int.MinValue;

            // Reservoir pick among eligible.
            int chosen = -1; int seen = 0;
            for (int i = 0; i < _cells.Count; i++)
            {
                if (!_cells[i].alive) continue;
                if (_cells[i].y < lo) continue;
                seen++;
                if (rng() < 1.0 / seen) chosen = i;
            }
            if (chosen < 0)
            {
                // fallback: any live
                for (int i = 0; i < _cells.Count; i++)
                    if (_cells[i].alive) { chosen = i; break; }
            }
            return chosen;
        }
    }
}
