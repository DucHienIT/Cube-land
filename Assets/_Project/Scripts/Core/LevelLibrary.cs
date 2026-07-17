using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Loads level JSON from Resources/Levels/level_NNN.json. Falls back to a tiny built-in
    /// level if none are found, so the game always runs even before content is generated.
    /// </summary>
    public static class LevelLibrary
    {
        static int _count = -1;

        /// <summary>Total number of level_NNN.json files present (contiguous from 1).</summary>
        public static int Count
        {
            get
            {
                if (_count < 0) _count = CountLevels();
                return _count;
            }
        }

        static int CountLevels()
        {
            int n = 0;
            while (Resources.Load<TextAsset>($"Levels/level_{(n + 1):000}") != null) n++;
            return n;
        }

        public static LevelData Load(int level)
        {
            var ta = Resources.Load<TextAsset>($"Levels/level_{level:000}");
            if (ta != null)
            {
                var data = JsonUtility.FromJson<LevelData>(ta.text);
                if (data != null && data.VoxelCount > 0)
                {
                    EnsureBankColors(data);
                    return data;
                }
            }
            return Fallback(level);
        }

        /// <summary>
        /// Guns are color-locked, so every bank block needs a color. Legacy JSON has no bankColors;
        /// derive them greedily: each block takes the color with the largest remaining voxel need,
        /// so per-color ammo covers per-color voxel count whenever total ammo has enough surplus.
        /// </summary>
        static void EnsureBankColors(LevelData data)
        {
            if (data.bank == null) return;
            if (data.bankColors != null && data.bankColors.Length == data.bank.Length) return;

            var need = new System.Collections.Generic.Dictionary<int, int>();
            for (int i = 0; i < data.voxels.Length; i++)
            {
                int c = data.voxels[i].c;
                need.TryGetValue(c, out int n);
                need[c] = n + 1;
            }

            data.bankColors = new int[data.bank.Length];
            for (int i = 0; i < data.bank.Length; i++)
            {
                int bestColor = 0, bestNeed = int.MinValue;
                foreach (var kv in need)
                    if (kv.Value > bestNeed) { bestNeed = kv.Value; bestColor = kv.Key; }
                data.bankColors[i] = bestColor;
                need[bestColor] = bestNeed - data.bank[i];
            }
        }

        /// <summary>A minimal procedurally-defined level used when no JSON is available.</summary>
        static LevelData Fallback(int level)
        {
            int size = 3 + Mathf.Min(level, 4);
            var voxels = new System.Collections.Generic.List<VoxelDef>();
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    for (int z = 0; z < 2; z++)
                        voxels.Add(new VoxelDef { x = x, y = y, z = z, c = (x + y) % 4 });

            // Per-color bank: guns only shoot their own color, so each color's ammo must cover
            // that color's voxel count on its own (~15% surplus, split into 3 blocks per color).
            var need = new System.Collections.Generic.Dictionary<int, int>();
            foreach (var v in voxels)
            {
                need.TryGetValue(v.c, out int n);
                need[v.c] = n + 1;
            }
            var bank = new System.Collections.Generic.List<int>();
            var bankColors = new System.Collections.Generic.List<int>();
            foreach (var kv in need)
            {
                int per = Mathf.CeilToInt(kv.Value * 1.15f / 3f);
                for (int i = 0; i < 3; i++) { bank.Add(per); bankColors.Add(kv.Key); }
            }

            return new LevelData
            {
                level = level,
                paletteIndex = level % 4,
                gunSlots = 4,
                bankColumns = 5,
                voxels = voxels.ToArray(),
                bank = bank.ToArray(),
                bankColors = bankColors.ToArray()
            };
        }
    }
}
