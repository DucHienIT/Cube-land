using UnityEngine;
using UnityEngine.Serialization;

namespace CubeBlaster
{
    public class CameraRig : MonoBehaviour
    {
        const float ShakeDecaySpeed = 12f;
        const float ShakeEpsilon = 0.0001f;
        const float AspectEpsilon = 0.001f;

        [Header("Scene-authored refs")]
        [FormerlySerializedAs("cam")]
        [SerializeField] Camera sceneCamera;

        public static Camera Main { get; private set; }
        public static CameraRig Rig { get; private set; }

        Vector3 _basePosition;
        float _shake;
        SculptureFrame _framed;
        float _framedAspect;
        bool _hasFraming;

        void Awake()
        {
            Main = sceneCamera;
            Rig = this;
        }

        public void FitTo(SculptureFrame sculpture)
        {
            if (sceneCamera == null) return;

            _framed = sculpture;
            _framedAspect = sceneCamera.aspect;
            _hasFraming = true;

            var framing = CameraFramingSolver.Solve(sculpture, sceneCamera.aspect, GameConfig.Active);
            sceneCamera.orthographic = framing.Orthographic;
            if (framing.Orthographic) sceneCamera.orthographicSize = framing.OrthographicSize;
            else sceneCamera.fieldOfView = framing.FieldOfView;

            _basePosition = framing.Position;
            transform.SetPositionAndRotation(framing.Position, framing.Rotation);
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        /// A WebGL canvas is resized by the player, not by the build, and the framing is solved
        /// from the aspect — so a browser resize has to re-solve it or the board ends up framed
        /// for a window that no longer exists.
        void RefitOnAspectChange()
        {
            if (!_hasFraming || sceneCamera == null) return;
            if (Mathf.Abs(sceneCamera.aspect - _framedAspect) < AspectEpsilon) return;
            FitTo(_framed);
        }

        void LateUpdate()
        {
            RefitOnAspectChange();

            if (_shake > ShakeEpsilon)
            {
                transform.position = _basePosition + (Vector3)(Random.insideUnitCircle * _shake);
                _shake = Mathf.Lerp(_shake, 0f, Time.deltaTime * ShakeDecaySpeed);
            }
            else if (_basePosition != Vector3.zero)
            {
                transform.position = _basePosition;
            }
        }
    }
}
