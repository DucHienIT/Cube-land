namespace CubeBlaster
{
    public sealed class TimeStarRule : IStarRule
    {
        readonly float _baseSeconds;
        readonly float _secondsPerVoxel;
        readonly float _perfectFraction;

        // Par is derived from the fastest the guns can physically empty a sculpture:
        // gunFireInterval / gunSlotCount = 0.03 / 4 = 0.0075s per cube. Two stars sits at
        // twice that, three at 0.6x par, so the ramp still rewards keeping every slot fed.
        // Anything near the old 0.045 hands out three stars unconditionally now that a level
        // is several thousand cubes cleared at ~130 a second.
        public TimeStarRule(float baseSeconds = 3f, float secondsPerVoxel = 0.015f, float perfectFraction = 0.6f)
        {
            _baseSeconds = baseSeconds;
            _secondsPerVoxel = secondsPerVoxel;
            _perfectFraction = perfectFraction;
        }

        public int Evaluate(int voxelCount, float elapsedSeconds)
        {
            float par = _baseSeconds + voxelCount * _secondsPerVoxel;
            if (elapsedSeconds <= par * _perfectFraction) return 3;
            return elapsedSeconds <= par ? 2 : 1;
        }
    }
}
