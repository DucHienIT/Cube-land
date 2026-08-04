using UnityEngine;

namespace CubeBlaster
{
    public readonly struct CameraFraming
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly bool Orthographic;
        public readonly float OrthographicSize;
        public readonly float FieldOfView;

        public CameraFraming(Vector3 position, Quaternion rotation, bool orthographic,
            float orthographicSize, float fieldOfView)
        {
            Position = position;
            Rotation = rotation;
            Orthographic = orthographic;
            OrthographicSize = orthographicSize;
            FieldOfView = fieldOfView;
        }
    }
}
