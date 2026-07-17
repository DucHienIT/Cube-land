using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// All baked visual assets in one place — meshes, materials, FX prefabs. Nothing is generated
    /// at runtime anymore: every entry is a real asset on disk that can be hand-tuned in the
    /// editor. Rebake defaults via Tools ▸ Cube Blaster ▸ Bake Visual Assets (only overwrites what
    /// the baker owns; hand edits to .mat/.prefab values survive a rebake unless "force" is used).
    /// Access via the <see cref="Visuals"/> facade.
    /// </summary>
    [CreateAssetMenu(fileName = "VisualLibrary", menuName = "CubeBlaster/VisualLibrary")]
    public class VisualLibrary : ScriptableObject
    {
        [System.Serializable]
        public class MaterialSet
        {
            public Material[] colors;
        }

        [Header("Voxel materials — [palette set][color slot]. THE source of truth for voxel look:")]
        [Tooltip("Edit these .mat assets to change how each voxel color renders (base color, toon ramp, spec...). Gameplay matches by slot index, so recoloring is safe.")]
        public MaterialSet[] voxelSets;

        [Header("Fixed materials")]
        public Material slotPad;      // gun-slot tray socket
        public Material dartBullet;   // near-white unlit bullet (tinted per gun color at runtime)
        public Material dartTrail;    // white vertex-color trail (per-dart color via start/endColor)

        [Header("FX prefabs (shared world-space particle systems, one instance each)")]
        public ParticleSystem fxBurst;
        public ParticleSystem fxShards;
        public ParticleSystem fxFlash;
        public ParticleSystem fxRing;

        [Header("Debris")]
        public Debris debrisPrefab; // rounded-cube fragment prefab (all components authored + referenced)

        /// <summary>Baked material for a voxel color slot of a palette set (never null-crashes).</summary>
        public Material VoxelMaterial(int set, int slot)
        {
            if (voxelSets == null || voxelSets.Length == 0) return null;
            var s = voxelSets[((set % voxelSets.Length) + voxelSets.Length) % voxelSets.Length];
            if (s == null || s.colors == null || s.colors.Length == 0) return null;
            return s.colors[Mathf.Clamp(slot, 0, s.colors.Length - 1)];
        }

        /// <summary>
        /// Display color of a voxel slot, read from the baked material so hand-edits to the .mat
        /// propagate everywhere (fx tints, dart/gun tints). Falls back to PaletteConfig.
        /// </summary>
        public Color VoxelColor(int set, int slot)
        {
            var m = VoxelMaterial(set, slot);
            if (m != null)
            {
                if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
                if (m.HasProperty("_Color")) return m.color;
            }
            var pal = Palette.Active.VoxelSet(set);
            return pal[Mathf.Clamp(slot, 0, pal.Length - 1)];
        }

        /// <summary>
        /// Quantized per-block color variation (±hue/±value from GameConfig) applied on top of a
        /// material's base color via MaterialPropertyBlock. Only 5 variants per color.
        /// </summary>
        public static Color Vary(Color color, int seed)
        {
            float hueJ = Cfg.Active.voxelHueJitter;
            float valJ = Cfg.Active.voxelValueJitter;
            if (hueJ <= 0f && valJ <= 0f) return color;
            int v = ((seed % 5) + 5) % 5;
            float t = (v - 2) * 0.5f; // -1, -0.5, 0, 0.5, 1
            float h, s, val;
            Color.RGBToHSV(color, out h, out s, out val);
            h = Mathf.Repeat(h + hueJ * t, 1f);
            val = Mathf.Clamp01(val * (1f + valJ * t));
            return Color.HSVToRGB(h, s, val);
        }

        /// <summary>Sets both URP (_BaseColor) and legacy (_Color) tint slots on a property block.</summary>
        public static void Tint(MaterialPropertyBlock mpb, Color c)
        {
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
        }
    }

    /// <summary>Always-non-null access to the active VisualLibrary (same pattern as Cfg/Palette).</summary>
    public static class Visuals
    {
        static VisualLibrary _active;

        public static void SetActive(VisualLibrary v) { if (v != null) _active = v; }

        public static VisualLibrary Active
        {
            get
            {
                if (_active == null) _active = Resources.Load<VisualLibrary>("Config/VisualLibrary");
                if (_active == null) _active = ScriptableObject.CreateInstance<VisualLibrary>();
                return _active;
            }
        }
    }
}
