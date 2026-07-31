namespace CubeBlaster
{
    public sealed class AlwaysVisible : IVoxelVisibility
    {
        public static readonly AlwaysVisible Instance = new AlwaysVisible();

        public bool IsVisible(int index) => true;
    }
}
