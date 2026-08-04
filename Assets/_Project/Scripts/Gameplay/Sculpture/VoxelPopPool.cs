using UnityEngine;

namespace CubeBlaster
{
    /// The struck cube's death animation, for cubes that no longer have a GameObject to animate.
    ///
    /// A pop used to be the cube's own Transform + coroutine, reparented out of the sculpture and
    /// then destroyed. Four guns at the current fire interval kill well over a hundred cubes a
    /// second, so that was a hundred Destroy calls a second feeding the GC. This is a fixed ring
    /// of animation states instead: nothing is created, nothing is destroyed, and the pops join
    /// the same instanced draw as the sculpture because they use the same materials.
    public sealed class VoxelPopPool
    {
        struct Pop
        {
            public bool Active;
            public Vector3 Start;
            public Vector3 Drift;
            public Quaternion Rotation;
            public Vector3 SpinAxis;
            public float Elapsed;
            public float Scale;
            public int BaseGroup;
            public int FlashGroup;
        }

        Pop[] _pops = new Pop[0];
        int _next;

        public void Configure(int capacity)
        {
            capacity = Mathf.Max(1, capacity);
            if (_pops.Length != capacity) _pops = new Pop[capacity];
            Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < _pops.Length; i++) _pops[i].Active = false;
            _next = 0;
        }

        /// Oldest-first recycling, same as the shockwave ring pool: overflowing cuts a pop short
        /// rather than allocating mid-barrage.
        public void Play(Vector3 position, Quaternion rotation, float scale, Vector3 outward,
            int baseGroup, int flashGroup, PopSettings settings)
        {
            if (_pops.Length == 0) return;

            _pops[_next] = new Pop
            {
                Active = true,
                Start = position,
                Drift = outward.normalized * settings.Rise,
                Rotation = rotation,
                SpinAxis = Random.onUnitSphere,
                Elapsed = 0f,
                Scale = scale,
                BaseGroup = baseGroup,
                FlashGroup = flashGroup
            };
            _next = (_next + 1) % _pops.Length;
        }

        public void Tick(float deltaTime, PopSettings settings, MeshInstanceBatcher batcher)
        {
            float duration = Mathf.Max(0.01f, settings.Duration);
            float swellPhase = Mathf.Clamp(settings.SwellPhase, 0.05f, 0.95f);

            for (int i = 0; i < _pops.Length; i++)
            {
                ref var pop = ref _pops[i];
                if (!pop.Active) continue;

                pop.Elapsed += deltaTime;
                float t = pop.Elapsed / duration;
                if (t >= 1f)
                {
                    pop.Active = false;
                    continue;
                }

                float swell = t < swellPhase
                    ? Mathf.Lerp(1f, 1f + settings.Swell, Ease.SmoothStep01(t / swellPhase))
                    : Mathf.Lerp(1f + settings.Swell, 0f, Ease.SmoothStep01((t - swellPhase) / (1f - swellPhase)));

                int level = ColorTools.PickFlashLevel(Mathf.Max(0f, 1f - t / swellPhase) * settings.Flash);
                int group = level > 0 ? pop.FlashGroup + level - 1 : pop.BaseGroup;

                batcher.Add(group, Matrix4x4.TRS(
                    pop.Start + pop.Drift * Ease.SmoothStep01(t),
                    pop.Rotation * Quaternion.AngleAxis(settings.Spin * t, pop.SpinAxis),
                    Vector3.one * (pop.Scale * swell)));
            }
        }
    }
}
