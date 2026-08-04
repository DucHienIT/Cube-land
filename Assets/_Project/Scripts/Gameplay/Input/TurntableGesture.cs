using System;
using UnityEngine;

namespace CubeBlaster
{
    public sealed class TurntableGesture : IPointerGesture
    {
        readonly Func<ISpinnable> _sculpture;

        float _lastX;
        bool _active;

        public TurntableGesture(Func<ISpinnable> sculpture)
        {
            _sculpture = sculpture ?? throw new ArgumentNullException(nameof(sculpture));
        }

        public bool TryBegin(Vector2 screenPosition)
        {
            if (_sculpture() == null) return false;
            _lastX = screenPosition.x;
            _active = true;
            return true;
        }

        public void Drag(Vector2 screenPosition)
        {
            var sculpture = _sculpture();
            if (!_active || sculpture == null) return;
            sculpture.ApplyYaw(screenPosition.x - _lastX);
            _lastX = screenPosition.x;
        }

        public void End(Vector2 screenPosition) => Cancel();

        public void Cancel() => _active = false;
    }
}
