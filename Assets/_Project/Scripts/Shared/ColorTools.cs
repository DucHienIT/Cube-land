using UnityEngine;

namespace CubeBlaster
{
    public static class ColorTools
    {
        public static readonly Color InkDark = new Color(0.106f, 0.153f, 0.278f, 1f);
        public static readonly Color InkLight = new Color(1f, 0.99f, 0.96f, 1f);

        const float InkSwitchLuminance = 0.62f;
        const int JitterSteps = 5;

        public static float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        public static Color LabelInk(Color background) =>
            Luminance(background) > InkSwitchLuminance ? InkDark : InkLight;

        public static Color ClampBrightness(Color c, float maxChannel)
        {
            float peak = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (peak <= maxChannel || peak <= 0.0001f) return c;
            float k = maxChannel / peak;
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        public static Color Jitter(Color color, int seed, float hueAmount, float valueAmount)
        {
            if (hueAmount <= 0f && valueAmount <= 0f) return color;
            float t = GetQuantizedOffset(seed);
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
