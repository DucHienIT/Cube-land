using UnityEngine;

namespace CubeBlaster
{
    public static class ColorTools
    {
        /// Ammo numbers are ALWAYS this warm white, on every block colour. Contrast comes from
        /// the heavy dark outline baked into NumberLabel.mat, not from re-colouring the glyph.
        /// This replaced a luminance switch that flipped to navy ink above 0.62 — it kept the
        /// number readable but made the bank read as two different label styles depending on
        /// which colours a level happened to use.
        public static readonly Color LabelInk = new Color(1f, 0.99f, 0.96f, 1f);

        const int JitterSteps = 5;

        public const int JitterVariants = 3;

        /// Hit flash is quantised into this many baked shades instead of being a continuous
        /// per-cube tint. Voxels have no renderer of their own — they are drawn with
        /// Graphics.DrawMeshInstanced, grouped by material — so a continuous colour would mean
        /// one draw call per flashing cube. A handful of shades keeps the whole barrage inside
        /// the same handful of instanced draws.
        public const int FlashLevels = 3;

        /// Whitening added per level. Deliberately stops at 0.9 rather than reaching pure white:
        /// the two callers land at 0.35 (hitFlashIntensity) and 0.85 (popFlash), and a fully
        /// white cube reads as a rendering glitch — see the blowout passes in CLAUDE.md.
        const float FlashStep = 0.3f;

        const float FlashEpsilon = 0.001f;

        /// 0 means "no flash, use the cube's own jitter shade".
        public static int PickFlashLevel(float amount) => amount <= FlashEpsilon
            ? 0
            : Mathf.Clamp(Mathf.RoundToInt(amount / FlashStep), 1, FlashLevels);

        public static float GetFlashShade(int level) => Mathf.Clamp01(level * FlashStep);

        public static Color ClampBrightness(Color c, float maxChannel)
        {
            float peak = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (peak <= maxChannel || peak <= 0.0001f) return c;
            float k = maxChannel / peak;
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        public static int PickJitterVariant(int seed) =>
            ((seed % JitterVariants) + JitterVariants) % JitterVariants;

        public static float GetJitterOffset(int variant) =>
            JitterVariants <= 1 ? 0f : variant / ((JitterVariants - 1) * 0.5f) - 1f;

        public static Color Jitter(Color color, int variant, float hueAmount, float valueAmount)
        {
            if (hueAmount <= 0f && valueAmount <= 0f) return color;
            float t = GetJitterOffset(variant);
            Color.RGBToHSV(color, out float h, out float s, out float v);
            h = Mathf.Repeat(h + hueAmount * t, 1f);
            v = Mathf.Clamp01(v * (1f + valueAmount * t));
            return Color.HSVToRGB(h, s, v);
        }

        public static float GetQuantizedOffset(int seed)
        {
            int step = ((seed % JitterSteps) + JitterSteps) % JitterSteps;
            return (step - JitterSteps / 2) * 0.5f;
        }
    }
}
