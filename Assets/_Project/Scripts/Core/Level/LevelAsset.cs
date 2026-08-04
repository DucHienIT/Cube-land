using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    /// One level, as a real project asset instead of a JSON blob.
    ///
    /// Everything a human ever edits — palette, slot count, the bank — is a plain serialized
    /// field, so it is inspectable and tweakable. The sculpture itself is 3000-8000 machine-
    /// generated cubes that nobody hand-places, so it is stored PACKED: one int per cube
    /// (x, y, z, colour as four bytes). Serialising it as a VoxelDef[] would put five YAML
    /// lines per cube in the file — roughly a megabyte an asset — and hand the Inspector an
    /// 8000-element array to draw. `packedVoxels` is private so no default inspector ever
    /// tries; LevelAssetEditor shows a summary and a solvability check instead.
    [CreateAssetMenu(fileName = "level_000", menuName = "CubeBlaster/Level")]
    public class LevelAsset : ScriptableObject
    {
        /// Coordinates are centred on the sculpture, so z is negative on one side. Shifting by
        /// half a byte lets all three axes share the same unsigned encoding.
        public const int CoordinateOffset = 128;

        [Header("Level")]
        public int level = 1;
        [Tooltip("Which PaletteConfig voxel set the sculpture is painted with (0-3). The sets " +
                 "differ — set A has no yellow, set C no purple — so this is not cosmetic: a " +
                 "voxel's colour slot means a different colour in each set.")]
        public int paletteIndex;
        public int gunSlots = 4;
        public int bankColumns = 5;

        [Header("Bank")]
        [Tooltip("Ammo per block. Guns are colour-locked and there is no surplus, so the ammo " +
                 "must total the cube count PER COLOUR or the level cannot be won — the " +
                 "inspector below checks exactly that. Parallel to bankColors.")]
        public int[] bank;
        [Tooltip("Colour slot each bank block carries, parallel to bank.")]
        public int[] bankColors;

        [SerializeField] int[] packedVoxels;

        public int VoxelCount => packedVoxels != null ? packedVoxels.Length : 0;

        public static int Pack(int x, int y, int z, int colorIndex) =>
            (x + CoordinateOffset)
            | ((y + CoordinateOffset) << 8)
            | ((z + CoordinateOffset) << 16)
            | (colorIndex << 24);

        public static VoxelDef Unpack(int packed) => new VoxelDef
        {
            x = (packed & 0xFF) - CoordinateOffset,
            y = ((packed >> 8) & 0xFF) - CoordinateOffset,
            z = ((packed >> 16) & 0xFF) - CoordinateOffset,
            c = (packed >> 24) & 0xFF
        };

        public LevelData ToLevelData()
        {
            int count = VoxelCount;
            var voxels = new VoxelDef[count];
            for (int i = 0; i < count; i++) voxels[i] = Unpack(packedVoxels[i]);

            // The arrays are cloned: LevelData is handed to gameplay, and gameplay writing
            // through to a ScriptableObject would edit the asset on disk in the editor.
            return new LevelData
            {
                level = level,
                paletteIndex = paletteIndex,
                gunSlots = gunSlots,
                bankColumns = bankColumns,
                bank = bank != null ? (int[])bank.Clone() : null,
                bankColors = bankColors != null ? (int[])bankColors.Clone() : null,
                voxels = voxels
            };
        }

        public void SetVoxels(IReadOnlyList<VoxelDef> voxels)
        {
            packedVoxels = new int[voxels != null ? voxels.Count : 0];
            for (int i = 0; i < packedVoxels.Length; i++)
                packedVoxels[i] = Pack(voxels[i].x, voxels[i].y, voxels[i].z, voxels[i].c);
        }

        public Dictionary<int, int> CountVoxelsPerColor()
        {
            var counts = new Dictionary<int, int>();
            if (packedVoxels == null) return counts;
            for (int i = 0; i < packedVoxels.Length; i++)
            {
                int color = (packedVoxels[i] >> 24) & 0xFF;
                counts.TryGetValue(color, out int count);
                counts[color] = count + 1;
            }
            return counts;
        }

        public Dictionary<int, int> CountAmmoPerColor()
        {
            var counts = new Dictionary<int, int>();
            if (bank == null || bankColors == null) return counts;
            for (int i = 0; i < bank.Length && i < bankColors.Length; i++)
            {
                counts.TryGetValue(bankColors[i], out int total);
                counts[bankColors[i]] = total + bank[i];
            }
            return counts;
        }

        /// Everything that would make this level unwinnable. Empty means solvable.
        public List<string> FindBankIssues()
        {
            var issues = new List<string>();
            if (bank == null || bank.Length == 0)
            {
                issues.Add("bank is empty");
                return issues;
            }
            if (bankColors == null || bankColors.Length != bank.Length)
            {
                issues.Add($"bankColors has {(bankColors != null ? bankColors.Length : 0)} entries, bank has {bank.Length}");
                return issues;
            }

            for (int i = 0; i < bank.Length; i++)
                if (bank[i] <= 0)
                    issues.Add($"block {i} holds {bank[i]} ammo — a gun with no darts never retires");

            var cubes = CountVoxelsPerColor();
            var ammo = CountAmmoPerColor();
            foreach (var entry in cubes)
            {
                ammo.TryGetValue(entry.Key, out int have);
                if (have != entry.Value)
                    issues.Add($"colour {entry.Key}: {entry.Value} cubes but {have} ammo");
            }
            foreach (var entry in ammo)
                if (!cubes.ContainsKey(entry.Key))
                    issues.Add($"colour {entry.Key}: {entry.Value} ammo but no cubes of that colour");

            return issues;
        }
    }
}
