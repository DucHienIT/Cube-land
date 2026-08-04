using UnityEngine;

namespace CubeBlaster
{
    public class Billboard : MonoBehaviour
    {
        public float towardCamera = 0f;

        Transform _camera;
        Vector3 _home;
        bool _homeCaptured;

        void LateUpdate()
        {
            if (!ResolveCamera()) return;

            if (!_homeCaptured)
            {
                _home = transform.localPosition;
                _homeCaptured = true;
            }

            transform.rotation = _camera.rotation;
            if (towardCamera > 0f && transform.parent != null)
                transform.position = transform.parent.TransformPoint(_home) - _camera.forward * towardCamera;
        }

        bool ResolveCamera()
        {
            if (_camera != null) return true;
            var camera = CameraRig.Main;
            if (camera == null) return false;
            _camera = camera.transform;
            return true;
        }
    }
}
