using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Keeps a label parallel to the screen plane (matches the camera's rotation exactly, so text
    /// is never skewed under the steep board pitch). Optionally pulls it toward the camera from
    /// its home point so it renders centered over its parent block instead of on one face.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        /// <summary>World units to pull toward the camera from the home position (0 = stay put).</summary>
        public float towardCamera = 0f;

        Transform _cam;
        Vector3 _home;
        bool _homeSet;

        void LateUpdate()
        {
            if (_cam == null)
            {
                var c = CameraRig.Main; // scene-wired camera, no Camera.main lookup
                if (c != null) _cam = c.transform;
                else return;
            }
            if (!_homeSet) { _home = transform.localPosition; _homeSet = true; }

            transform.rotation = _cam.rotation;
            if (towardCamera > 0f && transform.parent != null)
                transform.position = transform.parent.TransformPoint(_home) - _cam.forward * towardCamera;
        }
    }
}
