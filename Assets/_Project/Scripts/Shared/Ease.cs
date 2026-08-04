using UnityEngine;

namespace CubeBlaster
{
    public static class Ease
    {
        const float DefaultOvershoot = 1.70158f;

        public static float Pulse(float t01) => Mathf.Sin(Mathf.Clamp01(t01) * Mathf.PI);

        public static float OutBack(float t01, float overshoot = DefaultOvershoot)
        {
            float f = Mathf.Clamp01(t01) - 1f;
            return f * f * ((overshoot + 1f) * f + overshoot) + 1f;
        }

        public static float SmoothStep01(float t01) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t01));

        public static float Damp(float speed, float deltaTime) => 1f - Mathf.Exp(-speed * deltaTime);
    }
}
