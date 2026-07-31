using UnityEngine;

namespace CubeBlaster
{
    public sealed class SculptureLayout
    {
        public Vector3[] LocalPositions { get; }
        public Vector3 GridOrigin { get; }
        public Bounds WorldBounds { get; }

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
        }

        static Bounds MeasureBounds(Vector3 min, Vector3 max, GameConfig config, float cellSize, float scale)
        {
            float height = ((max.y - min.y) * cellSize + config.voxelSize) * scale;
            float radius = (Mathf.Max(max.x - min.x, max.z - min.z) * 0.5f * cellSize + config.voxelSize) * scale;

            float tilt = config.sculptureTilt * Mathf.Deg2Rad;
            float cosTilt = Mathf.Abs(Mathf.Cos(tilt));
            float sinTilt = Mathf.Abs(Mathf.Sin(tilt));

            float halfHeight = height * 0.5f * cosTilt + radius * sinTilt;
            float centerY = (height * 0.5f - config.voxelSize * scale * 0.5f) * cosTilt;

            return new Bounds(
                config.sculptureCenter + new Vector3(0f, centerY, 0f),
                new Vector3(radius * 2f, halfHeight * 2f, radius * 2f));
        }
    }
}
