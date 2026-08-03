using UnityEngine;

namespace CubeBlaster
{
    public static class ScreenLayout
    {
        public const float LandscapeAspect = 1.2f;

        public static float Aspect => Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

        public static bool IsLandscape => IsLandscapeAspect(Aspect);

        public static bool IsLandscapeAspect(float aspect) => aspect >= LandscapeAspect;
    }
}
