using System;

namespace CubeBlaster
{
    [Serializable]
    public class LevelData
    {
        public int level;
        public int paletteIndex;
        public int gunSlots = 4;
        public VoxelDef[] voxels;
        public int[] bank;
        public int[] bankColors;
        public int bankColumns = 5;

        public int VoxelCount => voxels != null ? voxels.Length : 0;

        public int TotalAmmo
        {
            get
            {
                int total = 0;
                if (bank != null)
                    for (int i = 0; i < bank.Length; i++) total += bank[i];
                return total;
            }
        }
    }
}
