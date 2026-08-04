namespace CubeBlaster
{
    public readonly struct PopSettings
    {
        public readonly float Duration;
        public readonly float Swell;
        public readonly float SwellPhase;
        public readonly float Rise;
        public readonly float Flash;
        public readonly float Spin;

        public PopSettings(float duration, float swell, float swellPhase, float rise, float flash, float spin)
        {
            Duration = duration;
            Swell = swell;
            SwellPhase = swellPhase;
            Rise = rise;
            Flash = flash;
            Spin = spin;
        }

        public static PopSettings From(GameConfig config) => new PopSettings(
            config.popTime, config.popSwell, config.popSwellPhase,
            config.popRise, config.popFlash, config.popSpin);
    }
}
