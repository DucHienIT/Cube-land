using UnityEngine;

namespace CubeBlaster
{
    public sealed class LevelAssetSource : ILevelSource
    {
        readonly string _folder;
        int _count = -1;

        public LevelAssetSource(string folder = "Levels")
        {
            _folder = folder;
        }

        public int Count
        {
            get
            {
                if (_count < 0) _count = CountContiguousLevels();
                return _count;
            }
        }

        public LevelData Load(int level)
        {
            var asset = Resources.Load<LevelAsset>(PathOf(level));
            if (asset == null) return null;

            var data = asset.ToLevelData();
            return data.VoxelCount > 0 ? data : null;
        }

        int CountContiguousLevels()
        {
            int found = 0;
            while (Resources.Load<LevelAsset>(PathOf(found + 1)) != null) found++;
            return found;
        }

        string PathOf(int level) => $"{_folder}/level_{level:000}";
    }
}
