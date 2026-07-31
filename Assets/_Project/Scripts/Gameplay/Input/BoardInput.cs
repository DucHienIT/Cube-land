using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CubeBlaster
{
    public class BoardInput : MonoBehaviour
    {
        [Header("Scene-authored refs")]
        [FormerlySerializedAs("cam")]
        [SerializeField] Camera sceneCamera;

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

        IBoardContext _board;
        IPointerGesture[] _gestures;
        IPointerGesture _active;

        public void Initialize(IBoardContext board)
        {
            _board = board;
            _gestures = new IPointerGesture[]
            {
                new BlockDragGesture(board, () => sceneCamera),
                new TurntableGesture(() => board.Sculpture)
            };
        }

        void Update()
        {
            if (_board == null || !_board.AcceptingInput)
            {
                CancelActive();
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null || sceneCamera == null) return;

            Vector2 position = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame) BeginGesture(position);
            else if (pointer.press.isPressed) _active?.Drag(position);
            else if (pointer.press.wasReleasedThisFrame) EndGesture(position);
        }

        void BeginGesture(Vector2 position)
        {
            if (IsOverUI(position)) return;
            for (int i = 0; i < _gestures.Length; i++)
            {
                if (!_gestures[i].TryBegin(position)) continue;
                _active = _gestures[i];
                return;
            }
        }

        void EndGesture(Vector2 position)
        {
            if (_active == null) return;
            _active.End(position);
            _active = null;
        }

        void CancelActive()
        {
            if (_active == null) return;
            _active.Cancel();
            _active = null;
        }

        bool IsOverUI(Vector2 position)
        {
            var events = EventSystem.current;
            if (events == null) return false;

            _uiHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = position }, _uiHits);
            return _uiHits.Count > 0;
        }
    }
}
