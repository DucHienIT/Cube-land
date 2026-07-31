namespace CubeBlaster
{
    public sealed class TimeStarRule : IStarRule
    {
        readonly float _baseSeconds;
        readonly float _secondsPerVoxel;
        readonly float _perfectFraction;

        public TimeStarRule(float baseSeconds = 3f, float secondsPerVoxel = 0.045f, float perfectFraction = 0.6f)
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
