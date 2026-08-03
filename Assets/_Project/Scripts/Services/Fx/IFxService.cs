using UnityEngine;

namespace CubeBlaster
{
    public interface IFxService
    {
        void PlayImpact(Vector3 position, Color color);

        /// Builds whatever the service would otherwise build on its first impact. Called at
        /// level load: the first impact happens on the frame all four guns land their opening
        /// darts, which is the worst possible moment to instantiate a pool.
        void Prewarm();
    }
}
