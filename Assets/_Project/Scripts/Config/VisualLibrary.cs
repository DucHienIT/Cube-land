using UnityEngine;

namespace CubeBlaster
{
    [CreateAssetMenu(fileName = "VisualLibrary", menuName = "CubeBlaster/VisualLibrary")]
    public class VisualLibrary : ScriptableObject
    {
        const string ResourcePath = "Config/VisualLibrary";

        static readonly ConfigProvider<VisualLibrary> Provider = new ConfigProvider<VisualLibrary>(ResourcePath);

        public static VisualLibrary Active => Provider.Active;

        public static void Use(VisualLibrary asset) => Provider.Use(asset);

        [System.Serializable]
        public class MaterialSet
        {
            public Material[] colors;

            [Tooltip("Per-block colour variation, baked as real materials instead of applied per " +
                     "renderer. A MaterialPropertyBlock evicts its renderer from the SRP Batcher, " +
                     "which at 1000-2000 voxels costs a full material bind per cube; distinct " +
                     "materials of the same shader stay in one batch. Indexed " +
                     "slot * ColorTools.JitterVariants + variant.")]
            public Material[] jitter;
        }

        [Header("Voxel materials — [palette set][color slot]")]
        [Tooltip("Edit these .mat assets to change how each voxel color renders. Gameplay matches by slot index, so recoloring is safe.")]
        public MaterialSet[] voxelSets;

        [Header("Fixed materials")]
        public Material backdrop;
        public Material slotPad;
        public Material dartBullet;
        public Material dartTrail;

        [Header("FX")]
        [Tooltip("Pooled shockwave ring quad — the only object a cube destroy spawns.")]
        public Shockwave shockwavePrefab;

        public Material GetVoxelMaterial(int set, int slot)
        {
            var materials = GetSet(set);
            if (materials == null || materials.colors == null || materials.colors.Length == 0) return null;
            return materials.colors[Mathf.Clamp(slot, 0, materials.colors.Length - 1)];
        }

        public Material GetVoxelMaterial(int set, int slot, int variant)
        {
            var materials = GetSet(set);
            int index = Mathf.Max(0, slot) * ColorTools.JitterVariants
                        + Mathf.Clamp(variant, 0, ColorTools.JitterVariants - 1);
            if (materials == null || materials.jitter == null || index >= materials.jitter.Length)
                return GetVoxelMaterial(set, slot);
            return materials.jitter[index] != null ? materials.jitter[index] : GetVoxelMaterial(set, slot);
        }

        public Color GetVoxelColor(int set, int slot) => ReadColor(GetVoxelMaterial(set, slot), set, slot);

        public Color GetVoxelColor(int set, int slot, int variant) =>
            ReadColor(GetVoxelMaterial(set, slot, variant), set, slot);

        MaterialSet GetSet(int set)
        {
            if (voxelSets == null || voxelSets.Length == 0) return null;
            return voxelSets[((set % voxelSets.Length) + voxelSets.Length) % voxelSets.Length];
        }

        static Color ReadColor(Material material, int set, int slot)
        {
            if (material != null)
            {
                if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
                if (material.HasProperty("_Color")) return material.color;
            }
            var fallback = PaletteConfig.Active.GetVoxelSet(set);
            return fallback[Mathf.Clamp(slot, 0, fallback.Length - 1)];
        }
    }
}
