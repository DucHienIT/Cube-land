using UnityEngine;

namespace CubeBlaster
{
    public interface IBoardContext
    {
        bool AcceptingInput { get; }
        ISpinnable Sculpture { get; }
        GunSlot FindFirstEmptySlot();

        /// Launches the block at the slot and returns whether the play was legal. The gun is NOT
        /// on the board when this returns — it appears when the block lands.
        bool DeployBlock(BankBlock block, GunSlot slot);
    }
}
