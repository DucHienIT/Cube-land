using System;
using UnityEngine;

namespace CubeBlaster
{
    /// Tap a front-row bank block and it hops into the first free shooter slot.
    ///
    /// This replaced a drag gesture (2026-08-03). Dragging let the player choose WHICH slot, which
    /// the tap path never did, and the choice was worth nothing: the slots are interchangeable —
    /// they hold the same gun, fire at the same rate, and the block carries its own colour — so
    /// the drag was a longer way to say what a tap already says.
    ///
    /// The press is claimed here rather than deployed on the spot so a press that turns into a
    /// drag is discarded instead of firing a deploy the player did not mean. It is NOT forwarded
    /// to the turntable: the bank sits well below the sculpture, so a spin that starts on a block
    /// is a misgrab, and swallowing it is better than spinning the board by accident.
    public sealed class BlockTapGesture : IPointerGesture
    {
        const float TapMovePixels = 18f;
        const float PickRayLength = 100f;

        readonly IBoardContext _board;
        readonly Func<Camera> _camera;

        BankBlock _block;
        Vector2 _pressPosition;
        bool _moved;

        public BlockTapGesture(IBoardContext board, Func<Camera> camera)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public bool TryBegin(Vector2 screenPosition)
        {
            var camera = _camera();
            if (camera == null) return false;
            if (!Physics.Raycast(camera.ScreenPointToRay(screenPosition), out var hit, PickRayLength)) return false;

            var block = BankBlock.FindByCollider(hit.collider);
            if (block == null || block.Consumed || block.Row != 0) return false;

            _block = block;
            _pressPosition = screenPosition;
            _moved = false;
            return true;
        }

        public void Drag(Vector2 screenPosition)
        {
            if ((screenPosition - _pressPosition).magnitude > TapMovePixels) _moved = true;
        }

        public void End(Vector2 screenPosition)
        {
            var block = _block;
            _block = null;
            if (block == null || _moved || block.Consumed) return;

            var slot = _board.FindFirstEmptySlot();
            if (slot != null && _board.DeployBlock(block, slot)) return;

            // Every slot busy, or the colour is already cleared. Saying nothing reads as a dropped
            // input, so the block answers the tap even when it cannot be played.
            block.RejectTap();
        }

        public void Cancel() => _block = null;
    }
}
