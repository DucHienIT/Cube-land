using UnityEngine;

namespace CubeBlaster
{
    public static class ProceduralClipFactory
    {
        const int SampleRate = 44100;
        const int NoiseSeed = 12345;

        public static GameClips CreateDefaultSet() => new GameClips
        {
            Shoot = Tone("shoot", 880f, 0.07f, 0.35f),
            Break = Tone("break", 300f, 0.12f, 0.4f, saw: true, drop: 120f),
            Thunk = Noise("thunk", 0.09f, 0.35f),
            Click = Tone("click", 560f, 0.05f, 0.3f),
            Win = Arpeggio("win", new[] { 523f, 659f, 784f, 1046f }, 0.10f, 0.4f),
            Music = Pad("pad", 2.6f)
        };

        public static AudioClip Tone(string name, float frequency, float duration, float volume,
            bool saw = false, float drop = 0f)
        {
            int samples = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float f = frequency - drop * (t / duration);
                float wave = saw ? Mathf.Repeat(f * t, 1f) * 2f - 1f : Mathf.Sin(2f * Mathf.PI * f * t);
                data[i] = wave * Mathf.Exp(-t * 14f) * volume;
            }
            return Build(name, data);
        }

        public static AudioClip Noise(string name, float duration, float volume)
        {
            int samples = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[samples];
            var random = new System.Random(NoiseSeed);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                data[i] = (float)(random.NextDouble() * 2 - 1) * Mathf.Exp(-t * 26f) * volume;
            }
            return Build(name, data);
        }

        public static AudioClip Arpeggio(string name, float[] frequencies, float step, float volume)
        {
            int perNote = Mathf.CeilToInt(SampleRate * step);
            var data = new float[perNote * frequencies.Length];
            for (int note = 0; note < frequencies.Length; note++)
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / SampleRate;
                    data[note * perNote + i] =
                        Mathf.Sin(2f * Mathf.PI * frequencies[note] * t) * Mathf.Exp(-t * 8f) * volume;
                }
            return Build(name, data);
        }

        public static AudioClip Pad(string name, float duration)
        {
            int samples = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[samples];
            float[] frequencies = { 130.8f, 164.8f, 196f };
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float sum = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                    sum += Mathf.Sin(2f * Mathf.PI * frequencies[f] * t + Mathf.Sin(t * 0.7f)) / frequencies.Length;
                float window = Mathf.Sin(Mathf.PI * i / samples);
                data[i] = sum * window * 0.5f;
            }
            return Build(name, data);
        }

        static AudioClip Build(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
