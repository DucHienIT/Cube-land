using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Positions the main camera to frame the whole play area (sculpture top → bank bottom) and
    /// provides a decaying shake used on impacts. Attached to the Main Camera in the scene.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [Header("Scene-authored refs")]
        [SerializeField] Camera cam;

        /// <summary>The gameplay camera (scene-wired) — used instead of Camera.main lookups.</summary>
        public static Camera Main { get; private set; }

        /// <summary>The rig itself (scene-wired) — lets views trigger Shake without a serialized ref.</summary>
        public static CameraRig Rig { get; private set; }

        Camera _cam;
        Vector3 _basePos;
        float _shake;

        void Awake() { _cam = cam; Main = cam; Rig = this; }

        public void Fit(Bounds sculpture)
        {
            if (_cam == null) _cam = cam;
            float pitch = Cfg.Active.cameraPitch;
            float topY = sculpture.max.y + Cfg.Active.cameraFitPadding;
            float bottomY = Cfg.Active.cameraFitBottomY;
            float midY = (topY + bottomY) * 0.5f;
            float halfV = (topY - bottomY) * 0.5f * Cfg.Active.cameraFitPadding;
            float halfH = Mathf.Max(3.2f, sculpture.extents.x + 3.0f);

            Quaternion rot = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 look = new Vector3(0f, midY, 0f);
            float dist;

            if (Cfg.Active.cameraOrthographic)
            {
                // Flat puzzle view: size covers the vertical span (and width on narrow aspects);
                // distance only needs to keep everything in front of the near plane.
                _cam.orthographic = true;
                _cam.orthographicSize = Mathf.Max(halfV, halfH / Mathf.Max(0.1f, _cam.aspect));
                dist = 30f;
            }
            else
            {
                // 3/4 view with slight perspective; narrow FOV keeps distortion near-isometric.
                _cam.orthographic = false;
                _cam.fieldOfView = Cfg.Active.cameraFov;
                float fovRad = _cam.fieldOfView * Mathf.Deg2Rad;
                float distV = halfV / Mathf.Tan(fovRad * 0.5f);
                // account for portrait aspect so width also fits
                float distH = halfH / Mathf.Tan(Mathf.Atan(Mathf.Tan(fovRad * 0.5f) * _cam.aspect));
                dist = Mathf.Max(distV, distH);
            }

            _basePos = look - rot * Vector3.forward * dist;
            transform.position = _basePos;
            transform.rotation = rot;
        }

        public void Shake(float amount)
        {
            _shake = Mathf.Max(_shake, amount);
        }

        void LateUpdate()
        {
            if (_shake > 0.0001f)
            {
                transform.position = _basePos + (Vector3)(Random.insideUnitCircle * _shake);
                _shake = Mathf.Lerp(_shake, 0f, Time.deltaTime * 12f);
            }
            else if (_basePos != Vector3.zero)
            {
                transform.position = _basePos;
            }
        }
    }
}
