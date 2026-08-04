using UnityEngine;

namespace CubeBlaster
{
    /// What the framing solver needs to know about the sculpture. The world box alone is not
    /// enough: under a steep camera the screen's vertical axis is a MIX of world Y and Z, and the
    /// box's top-back corner is nowhere near the object — on a thin shape it overstates the
    /// on-screen top by half the frame. `TopEdge` is the real extreme along that axis, collapsed
    /// onto the world-Y scale the solver works in.
    public readonly struct SculptureFrame
    {
        public readonly Bounds Bounds;
        public readonly float TopEdge;

        public SculptureFrame(Bounds bounds, float topEdge)
        {
            Bounds = bounds;
            TopEdge = topEdge;
        }
    }
}
