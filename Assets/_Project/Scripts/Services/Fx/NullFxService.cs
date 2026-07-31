using UnityEngine;

namespace CubeBlaster
{
    public sealed class NullFxService : IFxService
    {
        public static readonly NullFxService Instance = new NullFxService();

        public void PlayImpact(Vector3 position, Color color) { }
    }
}
