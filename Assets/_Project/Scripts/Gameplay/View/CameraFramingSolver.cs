using UnityEngine;

namespace CubeBlaster
{
    public static class CameraFramingSolver
    {
        const float MinFramedHalfWidth = 4.3f;
        const float SculptureHalfWidthFactor = 0.62f;
        const float OrthographicDistance = 30f;
        const float MinAspect = 0.1f;
        const float MinObjectRadius = 0.25f;
        const float MinFill = 0.25f;
        const float MaxFill = 0.95f;

        public static CameraFraming Solve(Bounds sculpture, float aspect, GameConfig config)
        {
            float topY = sculpture.max.y + config.cameraTopMargin;
            float bottomY = config.cameraFitBottomY;
            float halfHeight = (topY - bottomY) * 0.5f * config.cameraFitPadding;
            float halfWidth = Mathf.Max(MinFramedHalfWidth, sculpture.extents.x * SculptureHalfWidthFactor);

            var rotation = Quaternion.Euler(config.cameraPitch, 0f, 0f);
            var lookAt = new Vector3(0f, (topY + bottomY) * 0.5f, 0f);
            float safeAspect = Mathf.Max(MinAspect, aspect);

            if (config.cameraOrthographic)
            {
                float size = Mathf.Max(halfHeight, halfWidth / safeAspect);
                return new CameraFraming(
                    lookAt - rotation * Vector3.forward * OrthographicDistance,
                    rotation, true, size, config.cameraFov);
            }

            float tanV = Mathf.Tan(config.cameraFov * Mathf.Deg2Rad * 0.5f);
            float tanH = tanV * safeAspect;

            float fill = Mathf.Clamp(config.sculptureFillWidth, MinFill, MaxFill);
            float objectRadius = Mathf.Max(MinObjectRadius, sculpture.extents.x);
            float distanceForFill = objectRadius / (fill * tanH);
            float distanceForFit = Mathf.Max(halfHeight / tanV, halfWidth / tanH);
            float distance = Mathf.Max(distanceForFill, distanceForFit);

            return new CameraFraming(
                lookAt - rotation * Vector3.forward * distance,
                rotation, false, 0f, config.cameraFov);
        }
    }
}
