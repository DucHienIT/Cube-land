using UnityEngine;

namespace CubeBlaster
{
    public class UIBob : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.05f;
        [SerializeField] float speed = 3f;

        Vector3 _restScale;
        float _phase;

        void OnEnable()
        {
            _restScale = transform.localScale;
            _phase = 0f;
        }

        void Update()
        {
            _phase += Time.unscaledDeltaTime * speed;
            transform.localScale = _restScale * (1f + Mathf.Sin(_phase) * amplitude);
        }
    }
}
