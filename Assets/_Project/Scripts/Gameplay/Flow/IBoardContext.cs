using UnityEngine;

namespace CubeBlaster
{
    public interface IBoardContext
    {
        bool AcceptingInput { get; }
        ISpinnable Sculpture { get; }
        GunSlot FindFirstEmptySlot();
        GunSlot FindNearestEmptySlot(Vector2 screenPosition, float maxPixels, Camera camera);
        bool DeployBlock(BankBlock block, GunSlot slot);
    }
}
