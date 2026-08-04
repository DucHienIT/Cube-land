using UnityEngine;

namespace CubeBlaster
{
    public class UIPopIn : MonoBehaviour
    {
        public float delay = 0f;
        public float duration = 0.35f;
        public float from = 0.72f;

        Vector3 _target = Vector3.one;
        bool _captured;
        float _elapsed;

        public void SetTarget(Vector3 target)
        {
            _target = target;
            _captured = true;
        }

        void OnEnable()
        {
            if (!_captured)
            {
                _target = transform.localScale;
                _captured = true;
            }
            _elapsed = 0f;
            transform.localScale = _target * from;
        }

        void OnDisable()
        {
            if (_captured) transform.localScale = _target;
        }

        void Update()
        {
            if (_elapsed > delay + duration) return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < delay)
            {
                transform.localScale = _target * from;
                return;
            }

            float t = Mathf.Clamp01((_elapsed - delay) / duration);
            transform.localScale = _target * Mathf.LerpUnclamped(from, 1f, Ease.OutBack(t));
        }
    }
}
