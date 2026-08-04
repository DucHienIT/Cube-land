using UnityEngine;

namespace CubeBlaster
{
    /// Owns the one thing the instanced draw path needs and nothing else does: the mapping from
    /// (colour slot, jitter variant) and (colour slot, flash level) to a flat group index, plus
    /// the material each group draws with.
    ///
    /// Grouping IS the batching. Two cubes land in one draw call exactly when they land in one
    /// group, which is why per-cube variation has to be a small fixed set of baked materials
    /// rather than a continuous tint.
    public sealed class VoxelGroupTable
    {
        public const int GroupsPerSlot = ColorTools.JitterVariants + ColorTools.FlashLevels;

        readonly Material[] _materials;
        readonly Color[] _slotColors;

        public VoxelGroupTable(VisualLibrary library, int paletteSet)
        {
            int slots = library != null ? library.GetVoxelSlotCount(paletteSet) : 0;
            _materials = new Material[slots * GroupsPerSlot];
            _slotColors = new Color[slots];

            for (int slot = 0; slot < slots; slot++)
            {
                _slotColors[slot] = library.GetVoxelColor(paletteSet, slot);

                for (int variant = 0; variant < ColorTools.JitterVariants; variant++)
                    _materials[slot * GroupsPerSlot + variant] =
                        library.GetVoxelMaterial(paletteSet, slot, variant);

                for (int level = 1; level <= ColorTools.FlashLevels; level++)
                    _materials[slot * GroupsPerSlot + ColorTools.JitterVariants + level - 1] =
                        library.GetVoxelFlashMaterial(paletteSet, slot, level);
            }
        }

        public Material[] Materials => _materials;

        public Color GetColor(int slot) => _slotColors.Length == 0
            ? Color.white
            : _slotColors[Mathf.Clamp(slot, 0, _slotColors.Length - 1)];

        public int GetBaseGroup(int slot, int variant) =>
            ClampSlot(slot) * GroupsPerSlot + Mathf.Clamp(variant, 0, ColorTools.JitterVariants - 1);

        public int GetFlashGroup(int slot, int level) =>
            ClampSlot(slot) * GroupsPerSlot + ColorTools.JitterVariants
            + Mathf.Clamp(level - 1, 0, ColorTools.FlashLevels - 1);

        int ClampSlot(int slot) => _slotColors.Length == 0
            ? 0
            : Mathf.Clamp(slot, 0, _slotColors.Length - 1);
    }
}
