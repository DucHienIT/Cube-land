using System;
using UnityEngine;

namespace CubeBlaster
{
    public sealed class BlockDragGesture : IPointerGesture
    {
        const float TapMovePixels = 18f;
        const float SlotSnapPixels = 130f;
        const float PickRayLength = 100f;
        const float DragLift = 0.6f;

        readonly IBoardContext _board;
        readonly Func<Camera> _camera;

        BankBlock _block;
        Plane _dragPlane;
        Vector2 _pressPosition;
        bool _moved;

        public BlockDragGesture(IBoardContext board, Func<Camera> camera)
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
            _dragPlane = new Plane(-camera.transform.forward, block.transform.position);
            return true;
        }

        public void Drag(Vector2 screenPosition)
        {
            var camera = _camera();
            if (_block == null || camera == null) return;

            if ((screenPosition - _pressPosition).magnitude > TapMovePixels) _moved = true;

            var ray = camera.ScreenPointToRay(screenPosition);
            if (!_dragPlane.Raycast(ray, out float enter)) return;
            _block.FollowTo(ray.GetPoint(enter) - camera.transform.forward * DragLift);
        }

        public void End(Vector2 screenPosition)
        {
            var block = _block;
            _block = null;
            if (block == null) return;

            var slot = _moved
                ? _board.FindNearestEmptySlot(screenPosition, SlotSnapPixels, _camera())
                : _board.FindFirstEmptySlot();

            if (slot != null && _board.DeployBlock(block, slot)) return;
            block.ReturnHome();
        }

        public void Cancel()
        {
            if (_block == null) return;
            _block.ReturnHome();
            _block = null;
        }
    }
}
