using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeBlaster
{
    public class UIPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float PressedScale = 0.92f;
        const float ResponseSpeed = 22f;

        Vector3 _restScale;
        bool _captured;
        bool _pressed;

        void OnEnable()
        {
            if (_captured) return;
            _restScale = transform.localScale;
            _captured = true;
        }

        void OnDisable()
        {
            if (_captured) transform.localScale = _restScale;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => _pressed = true;

        public void OnPointerUp(PointerEventData eventData) => _pressed = false;

        public void OnPointerExit(PointerEventData eventData) => _pressed = false;

        void Update()
        {
            Vector3 target = _pressed ? _restScale * PressedScale : _restScale;
            transform.localScale = Vector3.Lerp(transform.localScale, target,
                Ease.Damp(ResponseSpeed, Time.unscaledDeltaTime));
        }
    }
}
