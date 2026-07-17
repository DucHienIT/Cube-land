using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// All colors + visual-mood knobs in one place. Mood is inspired by the reference
    /// (deep navy backdrop, playful candy voxels) but every value is our own, not sampled.
    /// Access via the <see cref="Palette"/> facade.
    /// </summary>
    [CreateAssetMenu(fileName = "PaletteConfig", menuName = "CubeBlaster/PaletteConfig")]
    public class PaletteConfig : ScriptableObject
    {
        [Header("Scene / mood")]
        public Color background = new Color(0.149f, 0.231f, 0.396f, 1f); // navy #263B65 (art doc)
        public Color fogTint = new Color(0.122f, 0.18f, 0.294f, 1f);     // dark navy #1F2E4B
        public bool useGradientSprites = true;
        public float uiCornerRadius = 26f;

        [Header("Voxel palettes â€” rows are per-level variants, columns are color slots.")]
        // Art-doc palette: hot reds/oranges vs greens, purple + yellow accents, warm white,
        // dark outline ink. Slot 4 is always the warm white; slot 5 is always the dark accent.
        // Base swatches (art review r3 â€” secondaries pushed sweeter, whites cleaner):
        // red #C92D20, yellow #F5C518, purple #6B2BE8, green #32D272, blue-tinted white #E4EBF8 (lighting review r8: pure-bright white burned out).
        public Color[] voxelSetA = {
            new Color(0.788f, 0.176f, 0.125f), // main red #C92D20
            new Color(1.00f, 0.459f, 0.263f),  // orange highlight #FF7543
            new Color(0.196f, 0.824f, 0.447f), // main green #32D272
            new Color(0.420f, 0.169f, 0.910f), // purple #6B2BE8
            new Color(0.894f, 0.922f, 0.973f), // blue-tinted white #E4EBF8
            new Color(0.188f, 0.129f, 0.157f), // dark outline #302128
        };
        public Color[] voxelSetB = {
            new Color(0.882f, 0.388f, 0.200f), // red-orange #E16333
            new Color(0.961f, 0.773f, 0.094f), // yellow #F5C518
            new Color(0.196f, 0.824f, 0.447f), // main green #32D272
            new Color(0.420f, 0.169f, 0.910f), // purple #6B2BE8
            new Color(0.894f, 0.922f, 0.973f), // blue-tinted white #E4EBF8
            new Color(0.122f, 0.180f, 0.294f), // dark navy #1F2E4B
        };
        public Color[] voxelSetC = {
            new Color(0.663f, 0.137f, 0.094f), // deep red #A92318
            new Color(1.00f, 0.459f, 0.263f),  // orange highlight #FF7543
            new Color(0.196f, 0.824f, 0.447f), // main green #32D272
            new Color(0.961f, 0.773f, 0.094f), // yellow #F5C518
            new Color(0.894f, 0.922f, 0.973f), // blue-tinted white #E4EBF8
            new Color(0.188f, 0.129f, 0.157f), // dark outline #302128
        };
        public Color[] voxelSetD = {
            new Color(0.788f, 0.176f, 0.125f), // main red #C92D20
            new Color(0.961f, 0.773f, 0.094f), // yellow #F5C518
            new Color(0.408f, 0.859f, 0.537f), // light green #68DB89
            new Color(0.420f, 0.169f, 0.910f), // purple #6B2BE8
            new Color(0.894f, 0.922f, 0.973f), // blue-tinted white #E4EBF8
            new Color(0.188f, 0.129f, 0.157f), // dark outline #302128
        };

        [Header("Guns & darts")]
        public Color gunBody = new Color(0.92f, 0.28f, 0.58f);
        public Color gunBodyAlt = new Color(0.56f, 0.36f, 0.87f);
        public Color dartColor = new Color(1f, 1f, 1f);
        public Color dartTrail = new Color(1f, 1f, 1f, 0.85f);

        [Header("Bank blocks (by value band, light â†’ dark)")]
        public Color[] bankBands = {
            new Color(0.961f, 0.773f, 0.094f), // low â€” yellow #F5C518
            new Color(1.00f, 0.459f, 0.263f),  // orange #FF7543
            new Color(0.788f, 0.176f, 0.125f), // red #C92D20
            new Color(0.188f, 0.129f, 0.157f), // high â€” dark #302128
        };

        [Header("UI")]
        public Color uiPanel = new Color(0.125f, 0.18f, 0.31f, 0.97f);
        public Color uiButton = new Color(0.99f, 0.66f, 0.15f);
        public Color uiButtonAlt = new Color(0.93f, 0.29f, 0.60f);
        public Color uiText = new Color(0.97f, 0.97f, 1f);
        public Color uiTextDim = new Color(0.64f, 0.70f, 0.82f);
        public Color coin = new Color(0.99f, 0.80f, 0.32f);
        public Color star = new Color(0.99f, 0.83f, 0.34f);
        public Color starEmpty = new Color(0.28f, 0.33f, 0.45f);

        [Header("Candy UI (menus/popups)")]
        // Button color convention: green = primary/forward, blue = navigation,
        // orange = retry, red = quit/danger, slate = secondary (sound, back, menu).
        public Color menuBg = new Color(0.663f, 0.812f, 1f);       // sky #A9CFFF
        public Color btnGreen = new Color(0.341f, 0.769f, 0.239f); // #57C43D
        public Color btnBlue = new Color(0.239f, 0.545f, 1f);      // #3D8BFF
        public Color btnOrange = new Color(1f, 0.631f, 0.196f);    // #FFA132
        public Color btnRed = new Color(0.957f, 0.341f, 0.29f);    // #F4574A
        public Color btnSlate = new Color(0.561f, 0.635f, 0.78f);  // #8FA2C7
        public Color cardWhite = new Color(0.992f, 0.996f, 1f);    // #FDFEFF
        public Color cardLocked = new Color(0.78f, 0.82f, 0.90f);
        public Color cardInk = new Color(0.23f, 0.29f, 0.42f);     // dark text on white cards
        public Color outlineInk = new Color(0.10f, 0.14f, 0.22f);  // neutral label outline (never state-colored)
        public Color shadowInk = new Color(0.07f, 0.11f, 0.28f, 0.35f);

        public Color[] VoxelSet(int index)
        {
            switch (((index % 4) + 4) % 4)
            {
                case 0: return voxelSetA;
                case 1: return voxelSetB;
                case 2: return voxelSetC;
                default: return voxelSetD;
            }
        }

        public Color BankColor(int value)
        {
            // Map ammo value to a band for readable difficulty cueing.
            int band = value <= 10 ? 0 : value <= 30 ? 1 : value <= 60 ? 2 : 3;
            return bankBands[Mathf.Clamp(band, 0, bankBands.Length - 1)];
        }
    }
}
