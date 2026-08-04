using UnityEngine;

namespace CubeBlaster
{
    public class UIMotion : MonoBehaviour
    {
        const float SwaySpeed = 0.7f;

        public float amp = 8f;
        public float speed = 2f;
        public float phase = 0f;
        public float rotAmp = 0f;

        RectTransform _rect;
        Vector2 _basePosition;
        Vector3 _baseRotation;
        bool _ready;

        void Start()
        {
            _rect = (RectTransform)transform;
            _basePosition = _rect.anchoredPosition;
            _baseRotation = _rect.localEulerAngles;
            _ready = true;
        }

        void Update()
        {
            if (!_ready) return;

            float time = Time.unscaledTime;
            _rect.anchoredPosition = _basePosition + new Vector2(0f, Mathf.Sin(time * speed + phase) * amp);

            if (rotAmp <= 0f) return;
            _rect.localEulerAngles = new Vector3(_baseRotation.x, _baseRotation.y,
                _baseRotation.z + Mathf.Sin(time * SwaySpeed + phase) * rotAmp);
        }
    }
}
