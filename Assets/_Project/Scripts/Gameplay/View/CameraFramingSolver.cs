using UnityEngine;

namespace CubeBlaster
{
    public static class CameraFramingSolver
    {
        const float MinFramedHalfWidth = 4.3f;
        const float BankSideMargin = 0.6f;
        const float SculptureHalfWidthFactor = 0.62f;
        const float OrthographicDistance = 30f;
        const float MinAspect = 0.1f;
        const float MinObjectRadius = 0.25f;
        const float MinFill = 0.25f;
        const float MaxFill = 0.95f;
        const float MinCosPitch = 0.01f;

        public static CameraFraming Solve(SculptureFrame frame, float aspect, GameConfig config)
        {
            bool landscape = ScreenLayout.IsLandscapeAspect(aspect);
            var sculpture = frame.Bounds;

            float bottomY = config.GetCameraFitBottomY(landscape);
            float topY = SolveTopY(frame, bottomY, config, landscape);
            float halfHeight = (topY - bottomY) * 0.5f * config.cameraFitPadding;
            float halfWidth = Mathf.Max(BoardHalfWidth(config, landscape),
                sculpture.extents.x * SculptureHalfWidthFactor);

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

        /// The HUD is a screen-space band, so the headroom it needs is a FRACTION of the frame,
        /// not a world distance: the same world margin is a much smaller share of a wide frame
        /// than of a tall one, which is what put the level title and the block counter on top of
        /// the sculpture in landscape.
        ///
        /// The fraction has to be measured along the camera's own up axis, NOT along world Y.
        /// The rig looks down at ~75deg, so up is (0, cos p, sin p) — almost entirely world +Z —
        /// and `max.y` alone says nothing about where a tilted sculpture's top lands on screen:
        /// across the 60 levels it varied from 0.03 to 0.19 of the screen height, so the rocket
        /// (level 16) grazed the frame's top edge while the compact fruits wasted a fifth of it.
        /// `SculptureFrame.TopEdge` is the sculpture's real extreme along that axis, and the span
        /// is then solved in closed form from `up(top) = halfHeight * (1 - 2 * reserve)`.
        static float SolveTopY(SculptureFrame frame, float bottomY, GameConfig config, bool landscape)
        {
            float floor = frame.Bounds.max.y + config.cameraTopMargin;
            float cosPitch = Mathf.Cos(config.cameraPitch * Mathf.Deg2Rad);
            if (cosPitch < MinCosPitch) return floor;

            float reserve = config.cameraFitPadding * (1f - 2f * config.GetCameraTopReserve(landscape));
            float topY = (2f * cosPitch * frame.TopEdge + bottomY * (reserve - cosPitch)) / (reserve + cosPitch);
            return Mathf.Max(floor, topY);
        }

        /// Half-width the BOARD needs, not the sculpture: the bank row is the widest thing on
        /// screen. It is the same width in both orientations now that the bank has one lane per
        /// shooter slot (`GetBankColumns`), so `landscape` no longer enters into it — at four
        /// lanes the floor wins anyway, and the term is kept because bankSlotSpacing and
        /// gunSlotCount are both live knobs.
        static float BoardHalfWidth(GameConfig config, bool landscape)
        {
            float bankHalfWidth =
                (config.GetBankColumns() - 1) * 0.5f * config.bankSlotSpacing + BankSideMargin;
            return Mathf.Max(MinFramedHalfWidth, bankHalfWidth);
        }
    }
}
