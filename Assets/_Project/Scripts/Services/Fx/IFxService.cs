using UnityEngine;

namespace CubeBlaster
{
    public interface IFxService
    {
        void PlayImpact(Vector3 position, Color color);
    }
}
