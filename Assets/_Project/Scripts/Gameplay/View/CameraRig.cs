using UnityEngine;
using UnityEngine.Serialization;

namespace CubeBlaster
{
    public class CameraRig : MonoBehaviour
    {
        const float ShakeDecaySpeed = 12f;
        const float ShakeEpsilon = 0.0001f;

        [Header("Scene-authored refs")]
        [FormerlySerializedAs("cam")]
        [SerializeField] Camera sceneCamera;

        public static Camera Main { get; private set; }
        public static CameraRig Rig { get; private set; }

        Vector3 _basePosition;
        float _shake;

        void Awake()
        {
            Main = sceneCamera;
            Rig = this;
        }

        public void FitTo(Bounds sculpture)
        {
            if (sceneCamera == null) return;

            var framing = CameraFramingSolver.Solve(sculpture, sceneCamera.aspect, GameConfig.Active);
            sceneCamera.orthographic = framing.Orthographic;
            if (framing.Orthographic) sceneCamera.orthographicSize = framing.OrthographicSize;
            else sceneCamera.fieldOfView = framing.FieldOfView;

            _basePosition = framing.Position;
            transform.SetPositionAndRotation(framing.Position, framing.Rotation);
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        void LateUpdate()
        {
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
