using UnityEngine;

namespace CubeBlaster
{
    public interface IDartContext
    {
        Vector3 GetVoxelWorldPosition(int voxelIndex);
        void ResolveDartHit(int voxelIndex, Vector3 position);
    }
}
