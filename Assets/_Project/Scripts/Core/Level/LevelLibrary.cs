namespace CubeBlaster
{
    public static class LevelLibrary
    {
        static ILevelSource _primary = new LevelAssetSource();
        static ILevelSource _fallback = new ProceduralLevelSource();

        public static void Use(ILevelSource primary, ILevelSource fallback = null)
        {
            if (primary != null) _primary = primary;
            if (fallback != null) _fallback = fallback;
        }

        public static int Count => _primary.Count;

        public static LevelData Load(int level) => _primary.Load(level) ?? _fallback.Load(level);
    }
}
