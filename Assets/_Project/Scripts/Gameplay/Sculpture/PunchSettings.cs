namespace CubeBlaster
{
    public readonly struct PunchSettings
    {
        public readonly float Amount;
        public readonly float Duration;
        public readonly float FlashTime;
        public readonly float FlashIntensity;

        public PunchSettings(float amount, float duration, float flashTime, float flashIntensity)
        {
            Amount = amount;
            Duration = duration;
            FlashTime = flashTime;
            FlashIntensity = flashIntensity;
        }

        public bool IsActive => Amount > 0f && Duration > 0f;

        public bool Flashes => FlashTime > 0f && FlashIntensity > 0.001f;
    }
}
