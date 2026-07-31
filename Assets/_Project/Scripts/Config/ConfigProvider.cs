using UnityEngine;

namespace CubeBlaster
{
    public sealed class ConfigProvider<T> where T : ScriptableObject
    {
        readonly string _resourcePath;
        T _active;

        public ConfigProvider(string resourcePath)
        {
            _resourcePath = resourcePath;
        }

        public void Use(T asset)
        {
            if (asset != null) _active = asset;
        }

        public T Active
        {
            get
            {
                if (_active == null) _active = Resources.Load<T>(_resourcePath);
                if (_active == null) _active = ScriptableObject.CreateInstance<T>();
                return _active;
            }
        }
    }
}
