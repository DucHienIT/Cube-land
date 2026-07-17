using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeBlaster
{
    /// <summary>
    /// Pure-Update UI juice (deliberately no DOTween). All timing uses unscaled time so the
    /// effects keep running while the game is paused (timeScale = 0).
    /// </summary>
    public class UIPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 _base;
        bool _captured;
        bool _down;

        void OnEnable()
        {
            if (!_captured) { _base = transform.localScale; _captured = true; }
        }

        void OnDisable()
        {
            if (_captured) transform.localScale = _base;
            _down = false;
        }

        public void OnPointerDown(PointerEventData e) { _down = true; }
        public void OnPointerUp(PointerEventData e) { _down = false; }
        public void OnPointerExit(PointerEventData e) { _down = false; }

        void Update()
        {
            Vector3 target = _down ? _base * 0.92f : _base;
            float k = 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, target, k);
        }
    }

    /// <summary>Gentle idle bob (position) + optional sway (rotation) for decorative elements.</summary>
    public class UIMotion : MonoBehaviour
    {
        public float amp = 8f;
        public float speed = 2f;
        public float phase = 0f;
        public float rotAmp = 0f;

        RectTransform _rt;
        Vector2 _basePos;
        Vector3 _baseRot;
        bool _ready;

        void Start()
        {
            // Capture in Start so the caller has already positioned/rotated the element.
            _rt = (RectTransform)transform;
            _basePos = _rt.anchoredPosition;
            _baseRot = _rt.localEulerAngles;
            _ready = true;
        }

        void Update()
        {
            if (!_ready) return;
            float t = Time.unscaledTime;
            _rt.anchoredPosition = _basePos + new Vector2(0f, Mathf.Sin(t * speed + phase) * amp);
            if (rotAmp > 0f)
                _rt.localEulerAngles = new Vector3(_baseRot.x, _baseRot.y,
                    _baseRot.z + Mathf.Sin(t * 0.7f + phase) * rotAmp);
        }
    }

    /// <summary>Scale pop-in (out-back ease) replayed every time the object is enabled.</summary>
    public class UIPopIn : MonoBehaviour
    {
        public float delay = 0f;
        public float duration = 0.35f;
        public float from = 0.72f;

        Vector3 _target = Vector3.one;
        bool _captured;
        float _t;

        /// <summary>Set the final scale explicitly (use when the caller changes localScale between shows).</summary>
        public void SetTarget(Vector3 target) { _target = target; _captured = true; }

        void OnEnable()
        {
            if (!_captured) { _target = transform.localScale; _captured = true; }
            _t = 0f;
            transform.localScale = _target * from;
        }

        void OnDisable()
        {
            if (_captured) transform.localScale = _target;
        }

        void Update()
        {
            if (_t > delay + duration) return;
            _t += Time.unscaledDeltaTime;
            if (_t < delay) { transform.localScale = _target * from; return; }
            float u = Mathf.Clamp01((_t - delay) / duration);
            const float s = 1.70158f;
            float f = u - 1f;
            float e = f * f * ((s + 1f) * f + s) + 1f; // out-back
            transform.localScale = _target * Mathf.LerpUnclamped(from, 1f, e);
        }
    }
}
