using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Lightweight one-shot particle effects. The four shared world-space ParticleSystems
    /// (burst, shards, flash, ring) are prefabs in the VisualLibrary — all curves, gradients,
    /// materials and sprites are hand-editable there. One instance of each is spawned on first
    /// use and reused for every hit, so per-hit cost is just Emit calls.
    /// </summary>
    public static class Fx
    {
        static ParticleSystem _burst, _shards, _flash, _ring;

        static ParticleSystem Resolve(ref ParticleSystem cached, ParticleSystem prefab)
        {
            if (cached != null) return cached;
            if (prefab == null) return null;
            cached = Object.Instantiate(prefab);
            cached.name = prefab.name;
            Object.DontDestroyOnLoad(cached.gameObject);
            return cached;
        }

        static ParticleSystem BurstPS => Resolve(ref _burst, Visuals.Active.fxBurst);
        static ParticleSystem ShardPS => Resolve(ref _shards, Visuals.Active.fxShards);
        static ParticleSystem FlashPS => Resolve(ref _flash, Visuals.Active.fxFlash);
        static ParticleSystem RingPS => Resolve(ref _ring, Visuals.Active.fxRing);

        /// <summary>Colored confetti-pop at a destroyed voxel (reference: sparks fly off hits).</summary>
        public static void Burst(Vector3 pos, Color color, int count = 7)
        {
            var ps = BurstPS;
            if (ps == null) return;
            for (int i = 0; i < count; i++)
            {
                var ep = new ParticleSystem.EmitParams();
                ep.position = pos + Random.insideUnitSphere * 0.08f;
                Vector3 v = Random.onUnitSphere;
                v.y = Mathf.Abs(v.y) * 0.9f + 0.4f;
                ep.velocity = v.normalized * Random.Range(1.6f, 3.6f);
                ep.startColor = Color.Lerp(color, Color.white, Random.Range(0.15f, 0.55f));
                ep.startSize = Random.Range(0.05f, 0.13f);
                ep.rotation = Random.Range(0f, 360f); // tumbled squares, not aligned tiles
                ps.Emit(ep, 1);
            }
        }

        /// <summary>Short white impact flash + tiny expanding shockwave ring at the hit point.</summary>
        public static void Flash(Vector3 pos, Color color)
        {
            var ps = FlashPS;
            if (ps != null)
            {
                var ep = new ParticleSystem.EmitParams();
                ep.position = pos;
                ep.startColor = Color.white;
                ep.startSize = 0.55f;
                ps.Emit(ep, 1);
                // faint colored halo just behind the white core
                ep.startColor = Color.Lerp(color, Color.white, 0.35f);
                ep.startSize = 0.8f;
                ep.startLifetime = 0.14f;
                ps.Emit(ep, 1);
            }

            var ringPs = RingPS;
            if (ringPs != null)
            {
                var ring = new ParticleSystem.EmitParams();
                ring.position = pos;
                ring.startColor = Color.Lerp(color, Color.white, 0.6f);
                ring.startSize = 1.1f;
                ringPs.Emit(ring, 1);
            }
        }

        /// <summary>Soft slow dust puffs at a destroy point — reuses the burst system with big,
        /// faded, desaturated particles so no extra ParticleSystem/prefab is needed.</summary>
        public static void Puff(Vector3 pos, Color color, int count = 3)
        {
            var ps = BurstPS;
            if (ps == null) return;
            for (int i = 0; i < count; i++)
            {
                var ep = new ParticleSystem.EmitParams();
                ep.position = pos + Random.insideUnitSphere * 0.12f;
                Vector3 v = Random.onUnitSphere;
                v.y = Mathf.Abs(v.y) * 0.5f + 0.2f;
                ep.velocity = v.normalized * Random.Range(0.4f, 0.9f); // drifts, doesn't fly
                Color c = Color.Lerp(color, new Color(0.92f, 0.93f, 0.98f), 0.75f); // chalky, near-white
                c.a = Random.Range(0.18f, 0.30f);
                ep.startColor = c;
                ep.startSize = Random.Range(0.28f, 0.5f);
                ep.startLifetime = Random.Range(0.35f, 0.55f);
                ep.rotation = Random.Range(0f, 360f);
                ps.Emit(ep, 1);
            }
        }

        /// <summary>Fast transparent shard streaks at a destruction point (sparkle + speed read).</summary>
        public static void Shards(Vector3 pos, Color color, int count = 4)
        {
            var ps = ShardPS;
            if (ps == null) return;
            for (int i = 0; i < count; i++)
            {
                var ep = new ParticleSystem.EmitParams();
                ep.position = pos + Random.insideUnitSphere * 0.06f;
                Vector3 v = Random.onUnitSphere;
                v.y = Mathf.Abs(v.y) * 0.6f + 0.3f;
                ep.velocity = v.normalized * Random.Range(4.5f, 7.5f);
                Color c = Color.Lerp(color, Color.white, 0.65f);
                c.a = Random.Range(0.4f, 0.7f);
                ep.startColor = c;
                ep.startSize = Random.Range(0.03f, 0.06f);
                ep.startLifetime = Random.Range(0.15f, 0.32f);
                ps.Emit(ep, 1);
            }
        }
    }
}
