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

            [Tooltip("Hit-flash shades, baked for the same reason as the jitter: voxels are drawn " +
                     "with Graphics.DrawMeshInstanced and grouped by material, so a continuous " +
                     "per-cube flash colour would cost one draw call per flashing cube. Indexed " +
                     "slot * ColorTools.FlashLevels + (level - 1).")]
            public Material[] flash;

            [Tooltip("Dart BULLET per colour slot — the round camera-facing dot. Darts have no " +
                     "GameObject either: DartField draws the whole swarm instanced and grouped by " +
                     "material, so a dart's tint is a baked material, not a MaterialPropertyBlock.")]
            public Material[] dart;

            [Tooltip("Dart TAIL per colour slot. Separate from the bullet because the two quads " +
                     "need different textures (and the tail is the only one that carries " +
                     "PaletteConfig.dartTrail's alpha) — one stretched quad cannot hold both, see " +
                     "the Instanced-dart pass.")]
            public Material[] dartStreak;
        }

        [Header("Voxel materials — [palette set][color slot]")]
        [Tooltip("Edit these .mat assets to change how each voxel color renders. Gameplay matches by slot index, so recoloring is safe.")]
        public MaterialSet[] voxelSets;

        [Header("Meshes")]
        [Tooltip("Unity's built-in 12-triangle cube. Voxels have no GameObject and no prefab — " +
                 "VoxelCubeField draws them all with Graphics.DrawMeshInstanced — so this asset " +
                 "is where the sculpture's mesh comes from.")]
        public Mesh voxelMesh;

        [Tooltip("Unity's built-in quad. One stretched quad IS the dart: the comet head and its " +
                 "tapering tail are painted into DartStreak.png, which is what replaced a " +
                 "TrailRenderer plus a billboarded bullet on every dart.")]
        public Mesh dartMesh;

        [Header("Fixed materials")]
        public Material backdrop;
        public Material slotPad;

        [Tooltip("Untinted white bullet — the fallback for a palette slot that has no baked one.")]
        public Material dartBullet;

        [Tooltip("Untinted white tail — the fallback for a palette slot that has no baked one.")]
        public Material dartStreak;

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

        /// Levels run 1..ColorTools.FlashLevels; level 0 means "not flashing" and never reaches
        /// here. Falls back to the unflashed material so an un-baked project still renders.
        public Material GetVoxelFlashMaterial(int set, int slot, int level)
        {
            var materials = GetSet(set);
            int index = Mathf.Max(0, slot) * ColorTools.FlashLevels
                        + Mathf.Clamp(level - 1, 0, ColorTools.FlashLevels - 1);
            if (materials == null || materials.flash == null || index >= materials.flash.Length)
                return GetVoxelMaterial(set, slot);
            return materials.flash[index] != null ? materials.flash[index] : GetVoxelMaterial(set, slot);
        }

        public int GetVoxelSlotCount(int set)
        {
            var materials = GetSet(set);
            return materials != null && materials.colors != null && materials.colors.Length > 0
                ? materials.colors.Length
                : PaletteConfig.Active.GetVoxelSet(set).Length;
        }

        public Mesh GetVoxelMesh() =>
            voxelMesh != null ? voxelMesh : Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        public Mesh GetDartMesh() =>
            dartMesh != null ? dartMesh : Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        /// Falls back to the untinted bullet/tail so an un-baked project still shows its darts —
        /// white ones, which is the symptom to look for if the dart materials are missing.
        public Material GetDartMaterial(int set, int slot) =>
            PickDartMaterial(GetSet(set) != null ? GetSet(set).dart : null, slot, dartBullet);

        public Material GetDartStreakMaterial(int set, int slot) =>
            PickDartMaterial(GetSet(set) != null ? GetSet(set).dartStreak : null, slot, dartStreak);

        static Material PickDartMaterial(Material[] materials, int slot, Material fallback)
        {
            int index = Mathf.Max(0, slot);
            if (materials == null || index >= materials.Length) return fallback;
            return materials[index] != null ? materials[index] : fallback;
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
