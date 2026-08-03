using System;
using System.Collections.Generic;

namespace CubeBlaster
{
    /// Hands a gun the highest live, unreserved, camera-exposed voxel of its colour.
    ///
    /// The candidates are held in a BUCKET PER (colour, height band) rather than being searched
    /// for in the voxel array. The array version scanned all `grid.Count` entries twice per shot
    /// — once to find the highest band with a candidate and once to pick inside it — which at
    /// four guns, ~133 shots a second and 3000-8000 cubes was measured at 0.35ms per shot, i.e.
    /// ~47ms of CPU per second of barrage. That is the "lag when all four guns open up": it is
    /// not the darts and not the draw calls, it is this search, and it gets worse the bigger the
    /// level is. Bucketed, a shot touches one band's list and nothing else.
    ///
    /// Reservation still guarantees no two darts claim the same voxel, which the exact-ammo
    /// levels depend on — a reserved voxel simply leaves its bucket until it is released.
    public sealed class ExposedTargetSelector : ITargetSelector
    {
        /// Random probes before falling back to scanning the whole band. Visibility is a ray
        /// march, so each probe is the expensive part of a shot; the top band is normally mostly
        /// exposed, so the first probe almost always lands. Rejection sampling like this picks
        /// uniformly among the visible candidates, exactly as the old full reservoir pass did.
        const int VisibilityProbes = 6;
        const int NotQueued = -1;

        readonly IVoxelGrid _grid;
        readonly IVoxelVisibility _visibility;
        readonly Func<float> _random;
        readonly HashSet<int> _reserved = new HashSet<int>();

        readonly int _minY;
        readonly int _bands;
        readonly int _colors;
        readonly List<int>[] _buckets;
        readonly int[] _bucketOf;
        readonly int[] _slotOf;
        readonly int[] _topBand;

        public ExposedTargetSelector(IVoxelGrid grid, IVoxelVisibility visibility, Func<float> random = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _visibility = visibility ?? AlwaysVisible.Instance;
            _random = random ?? (() => UnityEngine.Random.value);

            int minY = int.MaxValue, maxY = int.MinValue, maxColor = 0;
            for (int i = 0; i < _grid.Count; i++)
            {
                var cell = _grid.GetCell(i);
                if (cell.Y < minY) minY = cell.Y;
                if (cell.Y > maxY) maxY = cell.Y;
                if (cell.ColorIndex > maxColor) maxColor = cell.ColorIndex;
            }

            _minY = _grid.Count > 0 ? minY : 0;
            _bands = _grid.Count > 0 ? maxY - minY + 1 : 1;
            _colors = maxColor + 1;
            _buckets = new List<int>[_colors * _bands];
            _bucketOf = new int[_grid.Count];
            _slotOf = new int[_grid.Count];
            _topBand = new int[_colors];

            Clear();
        }

        public int Reserve(int colorIndex)
        {
            if (colorIndex < 0 || colorIndex >= _colors) return -1;

            for (int band = _topBand[colorIndex]; band >= 0; band--)
            {
                var bucket = _buckets[colorIndex * _bands + band];
                int found = bucket != null ? PickVisible(bucket) : -1;

                if (found >= 0)
                {
                    Dequeue(found);
                    _reserved.Add(found);
                    return found;
                }

                // Only an EMPTY band lowers the cached top. A band that is merely occluded right
                // now becomes shootable again the moment the player rotates the turntable or
                // breaks whatever is in front of it, so it has to stay in range.
                if ((bucket == null || bucket.Count == 0) && band == _topBand[colorIndex])
                    _topBand[colorIndex] = band - 1;
            }
            return -1;
        }

        public void Release(int index)
        {
            if (index < 0 || index >= _grid.Count) return;
            _reserved.Remove(index);
            Enqueue(index);
        }

        public void Clear()
        {
            _reserved.Clear();
            for (int i = 0; i < _buckets.Length; i++) _buckets[i]?.Clear();
            for (int i = 0; i < _bucketOf.Length; i++)
            {
                _bucketOf[i] = NotQueued;
                _slotOf[i] = NotQueued;
            }
            for (int c = 0; c < _colors; c++) _topBand[c] = _bands - 1;
            for (int i = 0; i < _grid.Count; i++) Enqueue(i);
        }

        /// Rejection sampling, pruning as it goes. Dead entries exist because a voxel is released
        /// on impact and destroyed a moment later; rather than subscribing to the grid, they are
        /// dropped the first time they are looked at.
        int PickVisible(List<int> bucket)
        {
            for (int probe = 0; probe < VisibilityProbes && bucket.Count > 0; )
            {
                int at = (int)(_random() * bucket.Count);
                if (at >= bucket.Count) at = bucket.Count - 1;

                int candidate = bucket[at];
                if (!_grid.IsAlive(candidate)) { Dequeue(candidate); continue; }

                probe++;
                if (_visibility.IsVisible(candidate)) return candidate;
            }

            // The band is occluded (or nearly so). Walking it backwards makes the swap-remove
            // safe: whatever is swapped into the hole has already been examined.
            for (int i = bucket.Count - 1; i >= 0; i--)
            {
                int candidate = bucket[i];
                if (!_grid.IsAlive(candidate)) { Dequeue(candidate); continue; }
                if (_visibility.IsVisible(candidate)) return candidate;
            }
            return -1;
        }

        void Enqueue(int index)
        {
            if (_bucketOf[index] != NotQueued || !_grid.IsAlive(index)) return;

            var cell = _grid.GetCell(index);
            int band = cell.Y - _minY;
            if (band < 0 || band >= _bands || cell.ColorIndex < 0 || cell.ColorIndex >= _colors) return;

            int id = cell.ColorIndex * _bands + band;
            var bucket = _buckets[id] ?? (_buckets[id] = new List<int>());

            _bucketOf[index] = id;
            _slotOf[index] = bucket.Count;
            bucket.Add(index);
            if (band > _topBand[cell.ColorIndex]) _topBand[cell.ColorIndex] = band;
        }

        /// Swap-with-last removal, so taking a voxel out of the middle of a band costs the same
        /// as taking it off the end. `_slotOf` is what makes that O(1) — without it every
        /// release would be a linear search of the band.
        void Dequeue(int index)
        {
            int id = _bucketOf[index];
            if (id == NotQueued) return;

            var bucket = _buckets[id];
            int slot = _slotOf[index];
            int last = bucket.Count - 1;

            if (slot != last)
            {
                int moved = bucket[last];
                bucket[slot] = moved;
                _slotOf[moved] = slot;
            }
            bucket.RemoveAt(last);

            _bucketOf[index] = NotQueued;
            _slotOf[index] = NotQueued;
        }
    }
}
