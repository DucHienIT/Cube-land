using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    public sealed class ProceduralLevelSource : ILevelSource
    {
        // Sized to the same several-thousand-cube contract the authored levels hold to, so
        // running with no content in Resources/Levels still exercises the real load.
        const int MinSide = 22;
        const int MaxExtraSide = 8;
        const int Depth = 6;
        const int ColorCount = 4;
        const int BlocksPerColor = 3;

        public int Count => int.MaxValue;

        public LevelData Load(int level)
        {
            var voxels = BuildVoxels(level);
            var data = new LevelData
            {
                level = level,
                paletteIndex = level % ColorCount,
                gunSlots = 4,
                bankColumns = 5,
                voxels = voxels.ToArray()
            };
            BuildBank(data, voxels);
            return data;
        }

        static List<VoxelDef> BuildVoxels(int level)
        {
            int side = MinSide + Mathf.Min(level, MaxExtraSide);
            var voxels = new List<VoxelDef>(side * side * Depth);
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                    for (int z = 0; z < Depth; z++)
                        voxels.Add(new VoxelDef { x = x, y = y, z = z, c = (x + y) % ColorCount });
            return voxels;
        }

        static void BuildBank(LevelData data, List<VoxelDef> voxels)
        {
            var need = new Dictionary<int, int>();
            foreach (var voxel in voxels)
            {
                need.TryGetValue(voxel.c, out int count);
                need[voxel.c] = count + 1;
            }

            var bank = new List<int>();
            var colors = new List<int>();
            foreach (var entry in need)
            {
                int blocks = Mathf.Clamp(BlocksPerColor, 1, entry.Value);
                int perBlock = entry.Value / blocks;
                int remainder = entry.Value % blocks;
                for (int i = 0; i < blocks; i++)
                {
                    bank.Add(perBlock + (i < remainder ? 1 : 0));
                    colors.Add(entry.Key);
                }
            }

            data.bank = bank.ToArray();
            data.bankColors = colors.ToArray();
        }
    }
}
