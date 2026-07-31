namespace CubeBlaster
{
    public static class FxService
    {
        static IFxService _current;

        public static IFxService Current
        {
            get => _current ?? (_current = new ShockwaveFxService(() => VisualLibrary.Active));
            set => _current = value;
        }
    }
}
