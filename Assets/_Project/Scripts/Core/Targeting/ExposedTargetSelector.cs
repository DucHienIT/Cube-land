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

        /// Highest live, unreserved, camera-exposed voxel of this colour, picked uniformly
        /// among ties.
        ///
        /// Resolved ONE HEIGHT BAND AT A TIME rather than by testing every voxel, because
        /// visibility is a ray march through the grid and everything else here is a field
        /// compare. Scanning the whole sculpture would march once per voxel — at 1000-2000
        /// cubes and four guns firing that is the single most expensive thing in the frame.
        /// A band almost always contains something visible, so in practice only a few
        /// marches run per shot; the walk down to the next band is the rare occluded case.
        public int Reserve(int colorIndex)
        {
            int ceiling = int.MaxValue;
            while (true)
            {
                int band = FindHighestBandBelow(colorIndex, ceiling);
                if (band == int.MinValue) return -1;

                int best = PickVisibleInBand(colorIndex, band);
                if (best >= 0)
                {
                    _reserved.Add(best);
                    return best;
                }
                ceiling = band;
            }
        }

        public void Release(int index) => _reserved.Remove(index);

        public void Clear() => _reserved.Clear();

        int FindHighestBandBelow(int colorIndex, int ceiling)
        {
            int band = int.MinValue;
            for (int i = 0; i < _grid.Count; i++)
            {
                if (!IsCandidate(i, colorIndex)) continue;
                int y = _grid.GetCell(i).Y;
                if (y < ceiling && y > band) band = y;
            }
            return band;
        }

        int PickVisibleInBand(int colorIndex, int band)
        {
            int best = -1;
            int seen = 0;
            for (int i = 0; i < _grid.Count; i++)
            {
                if (!IsCandidate(i, colorIndex) || _grid.GetCell(i).Y != band) continue;
                if (!_visibility.IsVisible(i)) continue;

                seen++;
                if (seen == 1 || _random() < 1f / seen) best = i;
            }
            return best;
        }

        bool IsCandidate(int index, int colorIndex)
        {
            if (!_grid.IsAlive(index) || _reserved.Contains(index)) return false;
            return _grid.GetCell(index).ColorIndex == colorIndex;
        }
    }
}
