using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public abstract class UIScreen : IUIScreen
    {
        protected readonly IUIHost UI;
        protected GameObject Root;

        protected UIScreen(IUIHost ui)
        {
            UI = ui;
        }

        public bool Visible => Root != null && Root.activeSelf;

        public void Show()
        {
            if (Root != null) Root.SetActive(true);
        }

        public void Hide()
        {
            if (Root != null) Root.SetActive(false);
        }

        public abstract void Build(RectTransform parent);

        public virtual void Refresh() { }

        protected GameObject MakeRoot(string name, RectTransform parent)
        {
            var go = UIFactory.Panel(name, parent, PaletteConfig.Active.background, true);
            var image = go.GetComponent<Image>();
            image.color = new Color(0, 0, 0, 0);
            image.raycastTarget = false;
            return go;
        }
    }
}
