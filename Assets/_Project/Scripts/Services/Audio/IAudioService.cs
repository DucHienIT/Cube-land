namespace CubeBlaster
{
    public interface IAudioService
    {
        void PlayShoot(float progress01);
        void PlayVoxelBreak();
        void PlayThunk();
        void PlayClick();
        void PlayWin();
        void StartMusic();
        void SetMuted(bool muted);
    }
}
