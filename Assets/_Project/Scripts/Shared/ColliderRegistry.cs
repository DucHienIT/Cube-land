using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    public sealed class ColliderRegistry<T> where T : class
    {
        readonly Dictionary<Collider, T> _entries = new Dictionary<Collider, T>();

        public void Register(Collider collider, T owner)
        {
            if (collider == null || owner == null) return;
            _entries[collider] = owner;
        }

        public void Unregister(Collider collider)
        {
            if (collider == null) return;
            _entries.Remove(collider);
        }

        public T Find(Collider collider) =>
            collider != null && _entries.TryGetValue(collider, out T owner) ? owner : null;
    }
}
