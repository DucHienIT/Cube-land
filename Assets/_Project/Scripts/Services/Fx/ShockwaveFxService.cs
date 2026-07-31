using System;
using UnityEngine;

namespace CubeBlaster
{
    public sealed class ShockwaveFxService : IFxService
    {
        readonly Func<VisualLibrary> _library;

        Transform _host;
        Shockwave[] _rings;
        int _next;

        public ShockwaveFxService(Func<VisualLibrary> library)
        {
            _library = library ?? throw new ArgumentNullException(nameof(library));
        }

        public void PlayImpact(Vector3 position, Color color)
        {
            var ring = FindNextRing();
            if (ring == null) return;
            ring.Play(position, color, GameConfig.Active);
        }

        Shockwave FindNextRing()
        {
            if (_host == null && !Build()) return null;

            var ring = _rings[_next];
            _next = (_next + 1) % _rings.Length;
            return ring;
        }

        bool Build()
        {
            var prefab = _library().shockwavePrefab;
            if (prefab == null) return false;

            _host = new GameObject("Shockwaves").transform;
            UnityEngine.Object.DontDestroyOnLoad(_host.gameObject);

            int capacity = Mathf.Max(1, GameConfig.Active.shockwaveMaxActive);
            _rings = new Shockwave[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var ring = UnityEngine.Object.Instantiate(prefab, _host);
                ring.gameObject.SetActive(false);
                _rings[i] = ring;
            }
            _next = 0;
            return true;
        }
    }
}
