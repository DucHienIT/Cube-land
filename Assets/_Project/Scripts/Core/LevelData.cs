using System;
using System.Collections.Generic;

namespace CubeBlaster
{
    /// <summary>
    /// Serializable level schema. Loaded from Resources/Levels/level_NNN.json via JsonUtility.
    /// A level is a set of voxels (the sculpture) plus a bank of ammo blocks the player deploys
    /// into gun slots. Guns are COLOR-LOCKED: a deployed gun only shoots voxels of its own color,
    /// so solvability is per color: for every color c, sum(bank values of color c) >= voxel count
    /// of color c (generator guarantees this; LevelLibrary derives colors for legacy JSON).
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int level;            // 1-based level number
        public int paletteIndex;     // which palette variant to tint the sculpture with
        public int gunSlots = 4;     // number of shooter slots
        public VoxelDef[] voxels;    // the sculpture, one entry per cube
        public int[] bank;           // ammo values available in the bottom bank (top-of-column first)
        public int[] bankColors;     // color slot per bank entry (parallel to bank); guns shoot only this color
        public int bankColumns = 5;  // how many columns to lay the bank out in

        public int VoxelCount => voxels != null ? voxels.Length : 0;

        public int TotalAmmo
        {
            get
            {
                int t = 0;
                if (bank != null) for (int i = 0; i < bank.Length; i++) t += bank[i];
                return t;
            }
        }
    }

    /// <summary>A single voxel in the sculpture: integer grid coordinate + color slot.</summary>
    [Serializable]
    public struct VoxelDef
    {
        public int x;
        public int y;
        public int z;
        public int c; // color index into the level palette
    }
}
