namespace CubeBlaster
{
    public sealed class NullAudioService : IAudioService
    {
        public static readonly NullAudioService Instance = new NullAudioService();

        public void PlayShoot(float progress01) { }
        public void PlayVoxelBreak() { }
        public void PlayThunk() { }
        public void PlayClick() { }
        public void PlayWin() { }
        public void StartMusic() { }
        public void SetMuted(bool muted) => SaveSystem.Muted = muted;
    }
}
