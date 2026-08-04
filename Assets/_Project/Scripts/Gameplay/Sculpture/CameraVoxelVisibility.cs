using System;
using UnityEngine;

namespace CubeBlaster
{
    public sealed class CameraVoxelVisibility : IVoxelVisibility
    {
        const float MarchStep = 0.25f;
        const float MarchStart = 0.55f;
        const float TrivialDistance = 1f;

        readonly IVoxelGrid _grid;
        readonly ISculptureSpace _space;
        readonly Func<Camera> _camera;

        public CameraVoxelVisibility(IVoxelGrid grid, ISculptureSpace space, Func<Camera> camera)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _space = space ?? throw new ArgumentNullException(nameof(space));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public bool IsVisible(int index)
        {
            var camera = _camera();
            if (camera == null) return true;

            var cell = _grid.GetCell(index);
            Vector3 origin = cell.GridPosition;
            Vector3 toCamera = _space.WorldToGrid(camera.transform.position) - origin;
            float distance = toCamera.magnitude;
            if (distance <= TrivialDistance) return true;
            toCamera /= distance;

            for (float travelled = MarchStart; travelled < distance; travelled += MarchStep)
            {
                Vector3 sample = origin + toCamera * travelled;
                int x = Mathf.RoundToInt(sample.x);
                int y = Mathf.RoundToInt(sample.y);
                int z = Mathf.RoundToInt(sample.z);
                if (cell.SameCoordinate(x, y, z)) continue;
                if (!_grid.InBounds(x, y, z)) return true;

                int blocker = _grid.GetIndexAt(x, y, z);
                if (blocker >= 0 && blocker != index && _grid.IsAlive(blocker)) return false;
            }
            return true;
        }
    }
}
