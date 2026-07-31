using UnityEngine;

namespace CubeBlaster
{
    public sealed class ResourceLevelSource : ILevelSource
    {
        readonly string _folder;
        int _count = -1;

        public ResourceLevelSource(string folder = "Levels")
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
            var asset = Resources.Load<TextAsset>(PathOf(level));
            if (asset == null) return null;

            var data = JsonUtility.FromJson<LevelData>(asset.text);
            if (data == null || data.VoxelCount <= 0) return null;

            BankColorAssigner.EnsureColors(data);
            return data;
        }

        int CountContiguousLevels()
        {
            int found = 0;
            while (Resources.Load<TextAsset>(PathOf(found + 1)) != null) found++;
            return found;
        }

        string PathOf(int level) => $"{_folder}/level_{level:000}";
    }
}
