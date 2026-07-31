using UnityEngine;

namespace CubeBlaster
{
    public readonly struct DartArc
    {
        const float MuzzleControlFactor = 0.5f;
        const float TargetControlFactor = 2f;

        readonly Vector3 _start;
        readonly Vector3 _control1;
        readonly Vector3 _control2;
        readonly Vector3 _approach;

        DartArc(Vector3 start, Vector3 control1, Vector3 control2, Vector3 approach)
        {
            _start = start;
            _control1 = control1;
            _control2 = control2;
            _approach = approach;
        }

        public Vector3 Approach => _approach;

        public float Length =>
            (_control1 - _start).magnitude +
            (_control2 - _control1).magnitude +
            (_approach - _control2).magnitude;

        public static DartArc Create(Vector3 start, Vector3 muzzleDirection, Vector3 target,
            Vector3 approachDirection, float offset) => new DartArc(
                start,
                start + muzzleDirection * (offset * MuzzleControlFactor),
                target + approachDirection * (offset * TargetControlFactor) + Vector3.back * offset,
                target + approachDirection * offset);

        public Vector3 Sample(float t)
        {
            float u = 1f - t;
            return u * u * u * _start
                 + 3f * u * u * t * _control1
                 + 3f * u * t * t * _control2
                 + t * t * t * _approach;
        }
    }
}
