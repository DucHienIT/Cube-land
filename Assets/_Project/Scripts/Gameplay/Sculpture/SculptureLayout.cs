using UnityEngine;

namespace CubeBlaster
{
    public sealed class SculptureLayout
    {
        const float MaxFramingPitch = 89f;

        public Vector3[] LocalPositions { get; }
        public Vector3 GridOrigin { get; }
        public Bounds WorldBounds { get; }
        public float TopEdge { get; }

        public SculptureLayout(IVoxelGrid grid, GameConfig config, float cellSize, float scale)
        {
            int count = grid.Count;
            LocalPositions = new Vector3[count];

            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;
            for (int i = 0; i < count; i++)
            {
                Vector3 position = grid.GetCell(i).GridPosition;
                min = Vector3.Min(min, position);
                max = Vector3.Max(max, position);
            }

            Vector3 gridCenter = (min + max) * 0.5f;
            GridOrigin = new Vector3(gridCenter.x, min.y, gridCenter.z);

            for (int i = 0; i < count; i++)
            {
                var cell = grid.GetCell(i);
                LocalPositions[i] = new Vector3(
                    cell.X - gridCenter.x,
                    cell.Y - min.y,
                    cell.Z - gridCenter.z) * cellSize;
            }

            WorldBounds = MeasureBounds(min, max, config, cellSize, scale);
            TopEdge = MeasureTopEdge(config, scale);
        }

        /// The box is measured AFTER the tilt, on both axes the tilt touches. Y was already
        /// tilt-aware; Z was not, and it is the one the camera cares about most — the rig looks
        /// down at ~75deg, so its screen-up axis is almost entirely world +Z and a 55deg tilt
        /// leans a tall sculpture's top most of a body-length up-screen.
        /// Rotation is Euler(tilt, 0, 0) * Euler(0, yaw, 0), so yaw only spins x/z — using one
        /// radius for both keeps the box valid at any turntable angle.
        static Bounds MeasureBounds(Vector3 min, Vector3 max, GameConfig config, float cellSize, float scale)
        {
            float height = ((max.y - min.y) * cellSize + config.voxelSize) * scale;
            float radius = (Mathf.Max(max.x - min.x, max.z - min.z) * 0.5f * cellSize + config.voxelSize) * scale;

            float tilt = config.sculptureTilt * Mathf.Deg2Rad;
            float cosTilt = Mathf.Abs(Mathf.Cos(tilt));
            float sinTilt = Mathf.Abs(Mathf.Sin(tilt));

            float localCenterY = height * 0.5f - config.voxelSize * scale * 0.5f;
            float halfHeight = height * 0.5f * cosTilt + radius * sinTilt;
            float halfDepth = height * 0.5f * sinTilt + radius * cosTilt;

            return new Bounds(
                config.sculptureCenter + new Vector3(0f, localCenterY * cosTilt, localCenterY * sinTilt),
                new Vector3(radius * 2f, halfHeight * 2f, halfDepth * 2f));
        }

        /// How high the sculpture reaches on SCREEN, expressed as `worldY + tan(pitch) * worldZ`
        /// so the solver can keep working in world-Y units. Measured over the actual voxels
        /// rather than the bounding box: the box's top-back corner is empty on anything that
        /// tapers (the rocket, the ice cream), and framing against it wasted a fifth of the
        /// screen. A voxel's x/z only enter as their RADIUS, because the turntable can spin any
        /// of them to the back — that keeps the value valid at every drag angle, exactly like
        /// the box's single radius does.
        float MeasureTopEdge(GameConfig config, float scale)
        {
            float tilt = config.sculptureTilt * Mathf.Deg2Rad;
            float cosTilt = Mathf.Abs(Mathf.Cos(tilt));
            float sinTilt = Mathf.Abs(Mathf.Sin(tilt));
            float slope = Mathf.Tan(Mathf.Min(config.cameraPitch, MaxFramingPitch) * Mathf.Deg2Rad);

            float heightWeight = cosTilt + slope * sinTilt;
            float radiusWeight = Mathf.Abs(slope * cosTilt - sinTilt);

            float top = 0f;
            for (int i = 0; i < LocalPositions.Length; i++)
            {
                var local = LocalPositions[i];
                float radius = Mathf.Sqrt(local.x * local.x + local.z * local.z);
                float reach = (local.y * heightWeight + radius * radiusWeight) * scale;
                if (reach > top) top = reach;
            }

            float halfVoxel = config.voxelSize * scale * 0.5f;
            return config.sculptureCenter.y + slope * config.sculptureCenter.z
                + top + halfVoxel * (heightWeight + radiusWeight);
        }
    }
}
