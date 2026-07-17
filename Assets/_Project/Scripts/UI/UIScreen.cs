using UnityEngine;

namespace CubeBlaster
{
    /// <summary>Base for plain-class screens: owns a root RectTransform that is toggled active.</summary>
    public abstract class UIScreen
    {
        protected readonly UIController UI;
        protected GameObject Root;

        protected UIScreen(UIController ui) { UI = ui; }

        public bool Visible => Root != null && Root.activeSelf;
        public void Show() { if (Root != null) Root.SetActive(true); }
        public void Hide() { if (Root != null) Root.SetActive(false); }

        public abstract void Build(RectTransform parent);
        public virtual void Refresh() { }

        protected GameObject MakeRoot(string name, RectTransform parent)
        {
            var go = UIFactory.Panel(name, parent, Palette.Background, true);
            // Root panels are transparent full-screen containers (background handled by camera).
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;
            return go;
        }
    }
}
