using UnityEngine;
using UnityEngine.Serialization;

namespace CubeBlaster
{
    [ExecuteAlways]
    public class Backdrop : MonoBehaviour
    {
        const float MaxFarClipFraction = 0.9f;

        [Header("Scene-authored refs")]
        [FormerlySerializedAs("cam")]
        [SerializeField] Camera sceneCamera;

        [Tooltip("Distance in front of the camera. Must sit beyond the play area but inside the far clip plane.")]
        [SerializeField] float distance = 60f;
        [Tooltip("Extra margin beyond the exact frustum fit, so no seam shows during camera shake.")]
        [SerializeField] float margin = 1.08f;

        float _lastAspect = -1f;
        float _lastSize = -1f;

        void OnEnable() => FitToFrustum();

        void LateUpdate()
        {
            if (sceneCamera == null) return;
            if (Mathf.Approximately(sceneCamera.aspect, _lastAspect) && Mathf.Approximately(ViewSize, _lastSize)) return;
            FitToFrustum();
        }

        float ViewSize => sceneCamera.orthographic ? sceneCamera.orthographicSize : sceneCamera.fieldOfView;

        void FitToFrustum()
        {
            if (sceneCamera == null) return;
            _lastAspect = sceneCamera.aspect;
            _lastSize = ViewSize;

            float depth = Mathf.Min(distance, sceneCamera.farClipPlane * MaxFarClipFraction);
            float height = sceneCamera.orthographic
                ? sceneCamera.orthographicSize * 2f
                : 2f * depth * Mathf.Tan(sceneCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * sceneCamera.aspect;

            transform.localPosition = new Vector3(0f, 0f, depth);
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(width * margin, height * margin, 1f);
        }
    }
}
