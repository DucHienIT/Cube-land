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
            if (voxelSets == null || voxelSets.Length == 0) return null;
            var materials = voxelSets[((set % voxelSets.Length) + voxelSets.Length) % voxelSets.Length];
            if (materials == null || materials.colors == null || materials.colors.Length == 0) return null;
            return materials.colors[Mathf.Clamp(slot, 0, materials.colors.Length - 1)];
        }

        public Color GetVoxelColor(int set, int slot)
        {
            var material = GetVoxelMaterial(set, slot);
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
