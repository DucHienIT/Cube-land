using UnityEngine;

namespace CubeBlaster
{
    public sealed class Turntable : ISpinnable
    {
        readonly Transform _target;

        TurntableSettings _settings;
        float _yaw;

        public Turntable(Transform target)
        {
            _target = target;
        }

        public void Configure(TurntableSettings settings, float restYaw)
        {
            _settings = settings;
            _yaw = restYaw;
            ApplyRotation();
        }

        public void ApplyYaw(float pixelsDeltaX)
        {
            _yaw += pixelsDeltaX * _settings.DragSensitivity;
            ApplyRotation();
        }

        void ApplyRotation()
        {
            if (_target == null) return;
            _target.rotation = Quaternion.Euler(_settings.Tilt, 0f, 0f) * Quaternion.Euler(0f, _yaw, 0f);
        }
    }
}
