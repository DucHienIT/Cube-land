using UnityEngine;
using UnityEngine.Serialization;

namespace CubeBlaster
{
    [DefaultExecutionOrder(-50)]
    public class AudioManager : MonoBehaviour, IAudioService
    {
        const float MusicVolume = 0.28f;
        const float ShootVolume = 0.35f;
        const float BreakVolume = 0.5f;
        const float ThunkVolume = 0.5f;
        const float ClickVolume = 0.6f;
        const float WinVolume = 0.7f;

        // A 1000-2000 cube level fires and breaks ~60 cubes a second. One PlayOneShot per
        // event past roughly 20/s stops reading as individual hits and turns into a flat
        // buzz, on top of churning voices; dropping the overflow keeps the same rhythm.
        const float MinShootInterval = 0.05f;
        const float MinBreakInterval = 0.045f;

        [Header("Scene-authored refs (two AudioSources on this object)")]
        [FormerlySerializedAs("_sfx")]
        [SerializeField] AudioSource sfxSource;
        [FormerlySerializedAs("_music")]
        [SerializeField] AudioSource musicSource;

        GameClips _clips;
        float _nextShoot;
        float _nextBreak;

        void Awake()
        {
            _clips = ProceduralClipFactory.CreateDefaultSet();

            sfxSource.playOnAwake = false;
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = MusicVolume;

            AudioService.Register(this);
            SetMuted(SaveSystem.Muted);
        }

        void OnDestroy() => AudioService.Unregister(this);

        public void PlayShoot(float progress01)
        {
            if (!ReadyAt(ref _nextShoot, MinShootInterval)) return;
            PlayOneShot(_clips.Shoot, ShootVolume, Mathf.Lerp(0.9f, 1.7f, Mathf.Clamp01(progress01)));
        }

        public void PlayVoxelBreak()
        {
            if (!ReadyAt(ref _nextBreak, MinBreakInterval)) return;
            PlayOneShot(_clips.Break, BreakVolume, Random.Range(0.9f, 1.15f));
        }

        static bool ReadyAt(ref float nextTime, float interval)
        {
            if (Time.unscaledTime < nextTime) return false;
            nextTime = Time.unscaledTime + interval;
            return true;
        }

        public void PlayThunk() => PlayOneShot(_clips.Thunk, ThunkVolume);

        public void PlayClick() => PlayOneShot(_clips.Click, ClickVolume);

        public void PlayWin() => PlayOneShot(_clips.Win, WinVolume);

        public void StartMusic()
        {
            if (musicSource.isPlaying) return;
            musicSource.clip = _clips.Music;
            musicSource.Play();
        }

        public void SetMuted(bool muted)
        {
            SaveSystem.Muted = muted;
            AudioListener.volume = muted ? 0f : 1f;
        }

        void PlayOneShot(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
