using UnityEngine;

namespace CubeBlaster
{
    public interface IUIScreen
    {
        bool Visible { get; }
        void Build(RectTransform parent);
        void Show();
        void Hide();
        void Refresh();
    }
}
