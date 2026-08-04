using UnityEngine;

namespace CubeBlaster
{
    /// The arc a tapped bank block flies along on its way into a shooter slot.
    ///
    /// Pure math, like DartArc: the block's MonoBehaviour only advances a clock and asks for a
    /// position, so the shape of the hop can be retuned without touching anything that owns a
    /// Transform.
    public readonly struct BlockHop
    {
        /// How far the arc leans OUT of the board plane, as a fraction of its height. The board is
        /// seen at a 75deg pitch, so an arc that rises only along the camera's up axis stays in
        /// the sculpture's plane and the block can pass behind it on the way. Leaning it toward
        /// the camera keeps the block in front of everything for the whole flight — the same
        /// trick the shockwave ring and the ammo labels use.
        const float TowardCameraShare = 0.55f;

        /// The block starts shrinking into the slot here, and hits zero exactly as the gun is
        /// spawned, so the two read as one object arriving rather than a swap.
        const float ShrinkFrom = 0.74f;
        const float TakeoffStretch = 0.20f;
        const float TakeoffSpan = 0.35f;

        readonly Vector3 _from;
        readonly Vector3 _to;
        readonly Vector3 _lift;
        readonly float _height;

        public BlockHop(Vector3 from, Vector3 to, Camera camera, float height)
        {
            _from = from;
            _to = to;
            _height = height;

            Vector3 up = camera != null ? camera.transform.up : Vector3.up;
            Vector3 towardCamera = camera != null ? -camera.transform.forward : Vector3.back;
            _lift = (up + towardCamera * TowardCameraShare).normalized;
        }

        /// Eased along the ground, a clean parabola in the air. Easing BOTH makes the block hang
        /// at the apex and read as floating up rather than being thrown.
        public Vector3 Sample(float t01)
        {
            float t = Mathf.Clamp01(t01);
            return Vector3.Lerp(_from, _to, Ease.SmoothStep01(t))
                   + _lift * (_height * 4f * t * (1f - t));
        }

        public static float SampleScale(float t01)
        {
            float t = Mathf.Clamp01(t01);
            float stretch = t < TakeoffSpan ? 1f + TakeoffStretch * Ease.Pulse(t / TakeoffSpan) : 1f;
            float shrink = t < ShrinkFrom
                ? 1f
                : 1f - Ease.SmoothStep01((t - ShrinkFrom) / (1f - ShrinkFrom));
            return stretch * shrink;
        }
    }
}
