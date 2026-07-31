namespace CubeBlaster
{
    public readonly struct TurntableSettings
    {
        public readonly float Tilt;
        public readonly float DragSensitivity;

        public TurntableSettings(float tilt, float dragSensitivity)
        {
            Tilt = tilt;
            DragSensitivity = dragSensitivity;
        }

        public static TurntableSettings From(GameConfig config) => new TurntableSettings(
            config.sculptureTilt,
            config.rotateSensitivity);
    }
}
