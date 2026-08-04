using UnityEngine;

namespace CubeBlaster
{
    public sealed class RecoilSpring
    {
        const float ReturnSpeed = 12f;

        readonly Transform _target;
        readonly Vector3 _restScale;
        readonly Vector3 _squash;

        public RecoilSpring(Transform target, Vector3 squash)
        {
            _target = target;
            _restScale = target != null ? target.localScale : Vector3.one;
            _squash = squash;
        }

        public void Kick()
        {
            if (_target == null) return;
            _target.localScale = Vector3.Scale(_restScale, _squash);
        }

        public void Tick(float deltaTime)
        {
            if (_target == null) return;
            _target.localScale = Vector3.Lerp(_target.localScale, _restScale, deltaTime * ReturnSpeed);
        }
    }
}
