using UnityEngine;

namespace CubeBlaster
{
    public interface IShooterContext
    {
        int RequestTarget(int colorIndex);
        int CountAliveOfColor(int colorIndex);
        Color GetColor(int colorIndex);
        void SpawnDart(int voxelIndex, Vector3 origin, Vector3 barrelDirection);
    }
}
