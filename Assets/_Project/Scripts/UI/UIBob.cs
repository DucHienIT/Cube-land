using UnityEngine;

namespace CubeBlaster
{
    /// <summary>Gentle idle scale pulse for call-to-action buttons (no DOTween dependency).</summary>
    public class UIBob : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.05f;
        [SerializeField] float speed = 3f;
        Vector3 _base;
        float _phase;

        void OnEnable() { _base = transform.localScale; _phase = 0f; }

        void Update()
        {
            _phase += Time.unscaledDeltaTime * speed;
            float s = 1f + Mathf.Sin(_phase) * amplitude;
            transform.localScale = _base * s;
        }
    }
}
