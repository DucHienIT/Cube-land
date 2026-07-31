using UnityEngine;

namespace CubeBlaster
{
    public interface IPointerGesture
    {
        bool TryBegin(Vector2 screenPosition);
        void Drag(Vector2 screenPosition);
        void End(Vector2 screenPosition);
        void Cancel();
    }
}
