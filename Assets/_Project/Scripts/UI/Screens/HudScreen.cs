using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class HudScreen : UIScreen
    {
        const float MinVisibleBarFill = 0.03f;

        Text _level;
        Text _progress;
        Image _bar;

        public HudScreen(IUIHost ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("Hud", parent);
            var t = Root.transform;
            var pal = PaletteConfig.Active;

            var menu = UIFactory.CandyButton("Menu", t, "≡", pal.btnSlate, () => UI.GoToMainMenu(), 56);
            UIFactory.Rect(menu.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -170), new Vector2(160, -60));

            var restart = UIFactory.CandyButton("Restart", t, "↻", pal.btnOrange, () => UI.RestartLevel(), 56);
            UIFactory.Rect(restart.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -170), new Vector2(-40, -60));

            _level = UIFactory.Label("Level", t, "LEVEL 1", 60, Color.white);
            UIFactory.Rect(_level.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -170), new Vector2(0, -70));
            UIFactory.AddOutline(_level, pal.outlineInk);

            BuildProgressBar(t, pal);

            var hint = UIFactory.Label("Hint", t, "drag a number block onto a shooter", 30, new Color(1, 1, 1, 0.7f));
            UIFactory.Rect(hint.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 24), new Vector2(0, 70));
            var hintShadow = hint.gameObject.AddComponent<Shadow>();
            hintShadow.effectColor = new Color(0, 0, 0, 0.4f);
            hintShadow.effectDistance = new Vector2(0, -2);
        }

        void BuildProgressBar(Transform parent, PaletteConfig pal)
        {
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            UIFactory.Rect(track, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-300, -250), new Vector2(300, -210));
            var trackImage = track.GetComponent<Image>();
            trackImage.sprite = SpriteFactory.UIGloss();
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color(0.10f, 0.13f, 0.22f, 0.85f);
            trackImage.raycastTarget = false;

            var barGo = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(track.transform, false);
            _bar = barGo.GetComponent<Image>();
            _bar.sprite = SpriteFactory.UIGloss();
            _bar.type = Image.Type.Sliced;
            _bar.color = pal.btnGreen;
            _bar.raycastTarget = false;
            _bar.rectTransform.anchorMin = new Vector2(0, 0);
            _bar.rectTransform.anchorMax = new Vector2(1, 1);
            _bar.rectTransform.offsetMin = new Vector2(4, 4);
            _bar.rectTransform.offsetMax = new Vector2(-4, -4);

            _progress = UIFactory.Label("Prog", parent, "", 34, Color.white);
            UIFactory.Rect(_progress.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-300, -300), new Vector2(300, -256));
            UIFactory.AddOutline(_progress, pal.outlineInk, 2f);
        }

        public void Set(int level)
        {
            if (_level != null) _level.text = "LEVEL " + level;
            SetProgress(1, 1);
        }

        public void SetProgress(int alive, int total)
        {
            if (total <= 0) total = 1;
            float destroyed = 1f - (float)alive / total;

            if (_bar != null)
            {
                _bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(destroyed), 1f);
                _bar.enabled = destroyed > MinVisibleBarFill;
            }
            if (_progress != null) _progress.text = alive + " blocks left";
        }
    }
}
