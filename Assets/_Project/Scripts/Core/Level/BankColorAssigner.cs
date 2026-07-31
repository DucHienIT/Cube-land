using System.Collections.Generic;

namespace CubeBlaster
{
    public static class BankColorAssigner
    {
        public static void EnsureColors(LevelData data)
        {
            if (data?.bank == null) return;
            if (data.bankColors != null && data.bankColors.Length == data.bank.Length) return;

            var need = CountVoxelsPerColor(data);
            data.bankColors = new int[data.bank.Length];

            for (int i = 0; i < data.bank.Length; i++)
            {
                int color = MostNeededColor(need, out int remaining);
                data.bankColors[i] = color;
                need[color] = remaining - data.bank[i];
            }
        }

        public static Dictionary<int, int> CountVoxelsPerColor(LevelData data)
        {
            var need = new Dictionary<int, int>();
            if (data?.voxels == null) return need;
            for (int i = 0; i < data.voxels.Length; i++)
            {
                int color = data.voxels[i].c;
                need.TryGetValue(color, out int count);
                need[color] = count + 1;
            }
            return need;
        }

        static int MostNeededColor(Dictionary<int, int> need, out int remaining)
        {
            int bestColor = 0;
            int bestNeed = int.MinValue;
            foreach (var entry in need)
            {
                if (entry.Value <= bestNeed) continue;
                bestNeed = entry.Value;
                bestColor = entry.Key;
            }
            remaining = bestNeed;
            return bestColor;
        }
    }
}
