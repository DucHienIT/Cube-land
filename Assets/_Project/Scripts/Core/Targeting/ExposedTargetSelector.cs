using System;
using System.Collections.Generic;

namespace CubeBlaster
{
    public sealed class ExposedTargetSelector : ITargetSelector
    {
        readonly IVoxelGrid _grid;
        readonly IVoxelVisibility _visibility;
        readonly Func<float> _random;
        readonly HashSet<int> _reserved = new HashSet<int>();

        public ExposedTargetSelector(IVoxelGrid grid, IVoxelVisibility visibility, Func<float> random = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _visibility = visibility ?? AlwaysVisible.Instance;
            _random = random ?? (() => UnityEngine.Random.value);
        }

        public int Reserve(int colorIndex)
        {
            int best = -1;
            int bestBand = int.MinValue;
            int candidatesInBand = 0;

            for (int i = 0; i < _grid.Count; i++)
            {
                if (!IsEligible(i, colorIndex)) continue;

                var cell = _grid.GetCell(i);
                if (cell.Y > bestBand)
                {
                    bestBand = cell.Y;
                    best = i;
                    candidatesInBand = 1;
                }
                else if (cell.Y == bestBand)
                {
                    candidatesInBand++;
                    if (_random() < 1f / candidatesInBand) best = i;
                }
            }

            if (best >= 0) _reserved.Add(best);
            return best;
        }

        public void Release(int index) => _reserved.Remove(index);

        public void Clear() => _reserved.Clear();

        bool IsEligible(int index, int colorIndex)
        {
            if (!_grid.IsAlive(index) || _reserved.Contains(index)) return false;
            if (_grid.GetCell(index).ColorIndex != colorIndex) return false;
            return _visibility.IsVisible(index);
        }
    }
}
