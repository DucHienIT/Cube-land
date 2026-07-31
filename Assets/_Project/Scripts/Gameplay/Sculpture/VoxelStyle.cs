namespace CubeBlaster
{
    public readonly struct VoxelStyle
    {
        public readonly float Size;
        public readonly float HueJitter;
        public readonly float ValueJitter;
        public readonly float ScaleJitter;

        public VoxelStyle(float size, float hueJitter, float valueJitter, float scaleJitter)
        {
            Size = size;
            HueJitter = hueJitter;
            ValueJitter = valueJitter;
            ScaleJitter = scaleJitter;
        }

        public static VoxelStyle From(GameConfig config) => new VoxelStyle(
            config.voxelSize,
            config.voxelHueJitter,
            config.voxelValueJitter,
            config.voxelScaleJitter);
    }
}
