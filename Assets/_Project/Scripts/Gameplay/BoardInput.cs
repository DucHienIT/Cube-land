using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CubeBlaster
{
    /// <summary>
    /// Pointer drag/drop for bank blocks → gun slots. Uses the new Input System (Pointer.current),
    /// covers both mouse and touch. Blocks gameplay input while the pointer is over UI.
    /// A quick tap (no drag) auto-assigns the tapped block to the first empty slot.
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        [Header("Scene-authored refs")]
        [SerializeField] Camera cam;

        GameManager _gm;
        Camera _cam;

        BankBlock _dragging;
        Plane _dragPlane;
        Vector2 _pressPos;
        bool _moved;
        bool _rotating;
        float _lastRotX;

        const float TapMovePx = 18f;
        const float SlotSnapPx = 130f;

        public void Init(GameManager gm)
        {
            _gm = gm;
            _cam = cam;
        }

        void Update()
        {
            if (_gm == null || !_gm.AcceptingInput) { CancelDrag(); return; }
            if (_cam == null) { _cam = cam; if (_cam == null) return; }

            var p = Pointer.current;
            if (p == null) return;
            Vector2 pos = p.position.ReadValue();

            if (p.press.wasPressedThisFrame) TryBeginDrag(pos);
            else if (p.press.isPressed)
            {
                if (_dragging != null) UpdateDrag(pos);
                else if (_rotating) UpdateRotate(pos);
            }
            else if (p.press.wasReleasedThisFrame)
            {
                if (_dragging != null) EndDrag(pos);
                if (_rotating) { if (_gm.Sculpture != null) _gm.Sculpture.EndSpin(); _rotating = false; }
            }
        }

        void TryBeginDrag(Vector2 pos)
        {
            if (IsOverUI(pos)) return;
            var ray = _cam.ScreenPointToRay(pos);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var block = BankBlock.FromCollider(hit.collider);
                // Only the top block of each stack (row 0) is playable — queued rows below are
                // fully visible in the head-on view, so they must not be draggable.
                if (block != null && !block.Consumed && block.Row == 0)
                {
                    _dragging = block;
                    _pressPos = pos;
                    _moved = false;
                    _dragPlane = new Plane(-_cam.transform.forward, block.transform.position);
                    return;
                }
            }
            // Not on a block → spin the sculpture.
            _rotating = true;
            _lastRotX = pos.x;
        }

        void UpdateRotate(Vector2 pos)
        {
            if (_gm.Sculpture == null) return;
            float dx = pos.x - _lastRotX;
            _lastRotX = pos.x;
            _gm.Sculpture.ApplyYaw(dx);
        }

        void UpdateDrag(Vector2 pos)
        {
            if ((pos - _pressPos).magnitude > TapMovePx) _moved = true;
            var ray = _cam.ScreenPointToRay(pos);
            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 world = ray.GetPoint(enter) - _cam.transform.forward * 0.6f; // lift toward camera
                _dragging.FollowTo(world);
            }
        }

        void EndDrag(Vector2 pos)
        {
            var block = _dragging;
            _dragging = null;

            GunSlot target = null;
            if (!_moved)
            {
                // Tap: auto-assign to first empty slot.
                target = _gm.FirstEmptySlot();
            }
            else
            {
                target = _gm.NearestEmptySlotOnScreen(pos, SlotSnapPx, _cam);
            }

            if (target != null && _gm.DeployBlock(block, target)) return;
            block.ReturnHome();
        }

        void CancelDrag()
        {
            if (_dragging != null) { _dragging.ReturnHome(); _dragging = null; }
            if (_rotating) { if (_gm != null && _gm.Sculpture != null) _gm.Sculpture.EndSpin(); _rotating = false; }
        }

        static bool IsOverUI(Vector2 pos)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = pos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            return results.Count > 0;
        }
    }
}
