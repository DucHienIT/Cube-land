using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Always-non-null access to the active configs. Resolution order:
    /// serialized override (set by GameBootstrap) → Resources/Config/... → CreateInstance defaults.
    /// This keeps the scene zero-setup and runnable even when the .asset files are missing.
    /// </summary>
    public static class Cfg
    {
        static GameConfig _active;

        public static void SetActive(GameConfig c) { if (c != null) _active = c; }

        public static GameConfig Active
        {
            get
            {
                if (_active == null) _active = Resources.Load<GameConfig>("Config/GameConfig");
                if (_active == null) _active = ScriptableObject.CreateInstance<GameConfig>();
                return _active;
            }
        }
    }

    public static class Palette
    {
        static PaletteConfig _active;

        public static void SetActive(PaletteConfig c) { if (c != null) _active = c; }

        public static PaletteConfig Active
        {
            get
            {
                if (_active == null) _active = Resources.Load<PaletteConfig>("Config/PaletteConfig");
                if (_active == null) _active = ScriptableObject.CreateInstance<PaletteConfig>();
                return _active;
            }
        }

        // Convenience shortcuts so call sites stay terse.
        public static Color Background => Active.background;
        public static Color DartColor => Active.dartColor;
        public static Color UIText => Active.uiText;
        public static Color UIPanel => Active.uiPanel;
        public static Color Coin => Active.coin;
        public static Color Star => Active.star;
        public static Color StarEmpty => Active.starEmpty;
    }
}
