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

        [Header("Scene-authored refs (two AudioSources on this object)")]
        [FormerlySerializedAs("_sfx")]
        [SerializeField] AudioSource sfxSource;
        [FormerlySerializedAs("_music")]
        [SerializeField] AudioSource musicSource;

        GameClips _clips;

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

        public void PlayShoot(float progress01) =>
            PlayOneShot(_clips.Shoot, ShootVolume, Mathf.Lerp(0.9f, 1.7f, Mathf.Clamp01(progress01)));

        public void PlayVoxelBreak() =>
            PlayOneShot(_clips.Break, BreakVolume, Random.Range(0.9f, 1.15f));

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
