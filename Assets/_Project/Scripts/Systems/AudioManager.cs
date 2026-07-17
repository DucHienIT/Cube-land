using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Synthesizes every sound at runtime (no audio assets). Lives on a scene object.
    /// Blip pitch rises with a progress value for "juice"; a soft looped pad plays under menus.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Scene-authored refs (two AudioSources on this object)")]
        [SerializeField] AudioSource _sfx;
        [SerializeField] AudioSource _music;

        AudioClip _blip, _pop, _thunk, _win, _click, _pad;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _sfx.playOnAwake = false;
            _music.playOnAwake = false; _music.loop = true; _music.volume = 0.28f;

            _blip = Tone(880f, 0.07f, 0.35f, waveSaw: false);
            _pop = Tone(300f, 0.12f, 0.4f, waveSaw: true, drop: 120f);
            _thunk = Noise(0.09f, 0.35f);
            _click = Tone(560f, 0.05f, 0.3f);
            _win = Arp(new float[] { 523f, 659f, 784f, 1046f }, 0.10f, 0.4f);
            _pad = Pad(2.6f);

            SetMuted(SaveSystem.Muted);
        }

        public void PlayShoot(float progress01)
        {
            float pitch = Mathf.Lerp(0.9f, 1.7f, Mathf.Clamp01(progress01));
            _sfx.pitch = pitch;
            _sfx.PlayOneShot(_blip, 0.35f);
        }

        public void PlayVoxelBreak() { _sfx.pitch = Random.Range(0.9f, 1.15f); _sfx.PlayOneShot(_pop, 0.5f); }
        public void PlayThunk() { _sfx.pitch = 1f; _sfx.PlayOneShot(_thunk, 0.5f); }
        public void PlayClick() { _sfx.pitch = 1f; _sfx.PlayOneShot(_click, 0.6f); }
        public void PlayWin() { _sfx.pitch = 1f; _sfx.PlayOneShot(_win, 0.7f); }

        public void StartMusic() { if (!_music.isPlaying) { _music.clip = _pad; _music.Play(); } }

        public void SetMuted(bool muted)
        {
            SaveSystem.Muted = muted;
            AudioListener.volume = muted ? 0f : 1f;
        }

        // ---- synths ----

        static AudioClip Tone(float freq, float dur, float vol, bool waveSaw = false, float drop = 0f)
        {
            int rate = 44100; int n = Mathf.CeilToInt(rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float f = freq - drop * (t / dur);
                float phase = 2f * Mathf.PI * f * t;
                float s = waveSaw ? Mathf.Repeat(f * t, 1f) * 2f - 1f : Mathf.Sin(phase);
                float env = Mathf.Exp(-t * 14f);
                data[i] = s * env * vol;
            }
            var clip = AudioClip.Create("t", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(float dur, float vol)
        {
            int rate = 44100; int n = Mathf.CeilToInt(rate * dur);
            var data = new float[n];
            var rnd = new System.Random(12345);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 26f);
                data[i] = (float)(rnd.NextDouble() * 2 - 1) * env * vol;
            }
            var clip = AudioClip.Create("n", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Arp(float[] freqs, float step, float vol)
        {
            int rate = 44100; int perN = Mathf.CeilToInt(rate * step); int n = perN * freqs.Length;
            var data = new float[n];
            for (int k = 0; k < freqs.Length; k++)
                for (int i = 0; i < perN; i++)
                {
                    float t = (float)i / rate;
                    float env = Mathf.Exp(-t * 8f);
                    data[k * perN + i] = Mathf.Sin(2f * Mathf.PI * freqs[k] * t) * env * vol;
                }
            var clip = AudioClip.Create("arp", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Pad(float dur)
        {
            int rate = 44100; int n = Mathf.CeilToInt(rate * dur);
            var data = new float[n];
            float[] freqs = { 130.8f, 164.8f, 196f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float s = 0f;
                for (int f = 0; f < freqs.Length; f++)
                    s += Mathf.Sin(2f * Mathf.PI * freqs[f] * t + Mathf.Sin(t * 0.7f)) / freqs.Length;
                // sin window so the loop point does not click
                float w = Mathf.Sin(Mathf.PI * (float)i / n);
                data[i] = s * w * 0.5f;
            }
            var clip = AudioClip.Create("pad", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
