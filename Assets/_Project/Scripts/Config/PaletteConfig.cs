using UnityEngine;

namespace CubeBlaster
{
    [CreateAssetMenu(fileName = "PaletteConfig", menuName = "CubeBlaster/PaletteConfig")]
    public class PaletteConfig : ScriptableObject
    {
        const string ResourcePath = "Config/PaletteConfig";
        const int VoxelSetCount = 4;

        static readonly ConfigProvider<PaletteConfig> Provider = new ConfigProvider<PaletteConfig>(ResourcePath);

        public static PaletteConfig Active => Provider.Active;

        public static void Use(PaletteConfig asset) => Provider.Use(asset);

        [Header("Scene / mood")]
        public Color background = new Color(0.149f, 0.231f, 0.396f, 1f);
        public Color fogTint = new Color(0.10f, 0.16f, 0.28f, 1f);
        public bool useGradientSprites = true;
        public float uiCornerRadius = 26f;

        [Header("Voxel palettes — rows are per-level variants, columns are color slots")]
        public Color[] voxelSetA = {
            new Color(0.78f, 0.13f, 0.08f),
            new Color(1.00f, 0.41f, 0.11f),
            new Color(0.07f, 0.78f, 0.35f),
            new Color(0.47f, 0.17f, 0.90f),
            new Color(0.90f, 0.93f, 0.97f),
            new Color(0.20f, 0.10f, 0.13f),
        };
        public Color[] voxelSetB = {
            new Color(0.88f, 0.25f, 0.10f),
            new Color(0.96f, 0.73f, 0.07f),
            new Color(0.07f, 0.78f, 0.35f),
            new Color(0.47f, 0.17f, 0.90f),
            new Color(0.90f, 0.93f, 0.97f),
            new Color(0.12f, 0.18f, 0.29f),
        };
        public Color[] voxelSetC = {
            new Color(0.66f, 0.10f, 0.06f),
            new Color(1.00f, 0.41f, 0.11f),
            new Color(0.07f, 0.78f, 0.35f),
            new Color(0.96f, 0.73f, 0.07f),
            new Color(0.90f, 0.93f, 0.97f),
            new Color(0.20f, 0.10f, 0.13f),
        };
        public Color[] voxelSetD = {
            new Color(0.78f, 0.13f, 0.08f),
            new Color(0.96f, 0.73f, 0.07f),
            new Color(0.12f, 0.82f, 0.38f),
            new Color(0.47f, 0.17f, 0.90f),
            new Color(0.90f, 0.93f, 0.97f),
            new Color(0.20f, 0.10f, 0.13f),
        };

        [Header("Guns & darts")]
        public Color gunBody = new Color(0.92f, 0.28f, 0.58f);
        public Color gunBodyAlt = new Color(0.56f, 0.36f, 0.87f);
        public Color dartColor = new Color(1f, 1f, 1f);
        // Alpha only — the rgb comes from the voxel colour the dart is going to break, and only
        // the TAIL uses it (the bullet is opaque). ~90 darts are in the air at once, so a tail
        // much over 0.55 turns the stream into solid ribbons over the sculpture. Re-bake after
        // changing it: it is baked into the dart materials, not read at runtime.
        public Color dartTrail = new Color(1f, 1f, 1f, 0.72f);

        [Header("Bank blocks (by value band, light to dark)")]
        public Color[] bankBands = {
            new Color(0.961f, 0.773f, 0.094f),
            new Color(1.00f, 0.459f, 0.263f),
            new Color(0.788f, 0.176f, 0.125f),
            new Color(0.188f, 0.129f, 0.157f),
        };

        [Header("UI")]
        public Color uiPanel = new Color(0.10f, 0.16f, 0.29f, 0.97f);
        public Color uiButton = new Color(1.00f, 0.57f, 0.09f);
        public Color uiButtonAlt = new Color(0.87f, 0.20f, 0.52f);
        public Color uiText = new Color(0.97f, 0.97f, 1f);
        public Color uiTextDim = new Color(0.64f, 0.70f, 0.82f);
        public Color coin = new Color(0.99f, 0.80f, 0.32f);
        public Color star = new Color(0.99f, 0.83f, 0.34f);
        public Color starEmpty = new Color(0.28f, 0.33f, 0.45f);

        [Header("Candy UI (menus/popups)")]
        public Color menuBg = new Color(0.184f, 0.322f, 0.49f);
        public Color btnGreen = new Color(0.14f, 0.79f, 0.35f);
        public Color btnBlue = new Color(0.16f, 0.45f, 0.90f);
        public Color btnOrange = new Color(1f, 0.49f, 0.15f);
        public Color btnRed = new Color(0.85f, 0.22f, 0.15f);
        public Color btnSlate = new Color(0.20f, 0.49f, 0.85f);
        public Color cardWhite = new Color(0.992f, 0.996f, 1f);
        public Color cardLocked = new Color(0.58f, 0.65f, 0.78f);
        public Color cardInk = new Color(0.12f, 0.18f, 0.31f);
        public Color outlineInk = new Color(0.055f, 0.09f, 0.16f);
        public Color shadowInk = new Color(0.035f, 0.055f, 0.14f, 0.52f);

        public Color[] GetVoxelSet(int index)
        {
            switch (((index % VoxelSetCount) + VoxelSetCount) % VoxelSetCount)
            {
                case 0: return voxelSetA;
                case 1: return voxelSetB;
                case 2: return voxelSetC;
                default: return voxelSetD;
            }
        }

        public Color GetBankColor(int value)
        {
            int band = value <= 10 ? 0 : value <= 30 ? 1 : value <= 60 ? 2 : 3;
            return bankBands[Mathf.Clamp(band, 0, bankBands.Length - 1)];
        }
    }
}
