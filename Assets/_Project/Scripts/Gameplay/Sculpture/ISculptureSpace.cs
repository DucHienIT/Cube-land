using UnityEngine;

namespace CubeBlaster
{
    public interface ISculptureSpace
    {
        Vector3 WorldToGrid(Vector3 world);
        Vector3 GetWorldPosition(int voxelIndex);
    }
}
