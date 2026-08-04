using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class HudScreen : UIScreen
    {
        const float MinVisibleBarFill = 0.03f;

        static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        static readonly Vector2 TopRight = new Vector2(1f, 1f);
        static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        Button _menu;
        Button _restart;
        Text _level;
        Text _progress;
        Text _hint;
        Image _bar;
        RectTransform _track;

        public HudScreen(IUIHost ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("Hud", parent);
            var t = Root.transform;
            var pal = PaletteConfig.Active;

            _menu = UIFactory.CandyButton("Menu", t, "≡", pal.btnSlate, () => UI.GoToMainMenu(), 56);
            _restart = UIFactory.CandyButton("Restart", t, "↻", pal.btnOrange, () => UI.RestartLevel(), 56);

            _level = UIFactory.Label("Level", t, "LEVEL 1", 60, Color.white);
            UIFactory.AddOutline(_level, pal.outlineInk);

            BuildProgressBar(t, pal);

            _hint = UIFactory.Label("Hint", t, "drag a number block onto a shooter", 30, new Color(1, 1, 1, 0.7f));
            var hintShadow = _hint.gameObject.AddComponent<Shadow>();
            hintShadow.effectColor = new Color(0, 0, 0, 0.4f);
            hintShadow.effectDistance = new Vector2(0, -2);

            ApplyLayout(ScreenLayout.IsLandscape);
        }

        void BuildProgressBar(Transform parent, PaletteConfig pal)
        {
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            _track = track.GetComponent<RectTransform>();
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
            UIFactory.AddOutline(_progress, pal.outlineInk, 2f);
        }

        /// Portrait stacks title / bar / counter down the top of the screen, which is free space
        /// there. In landscape that same stack is ~28% of the frame and lands straight on the
        /// sculpture, so everything collapses onto ONE bar across the top and the counter moves
        /// inside the progress track instead of below it. CameraFramingSolver reserves the band
        /// this leaves — cameraTopReserve / cameraTopReserveLandscape have to follow it.
        public override void ApplyLayout(bool landscape)
        {
            if (landscape) LayoutLandscape();
            else LayoutPortrait();
        }

        void LayoutPortrait()
        {
            UIFactory.Rect(_menu.gameObject, TopLeft, TopLeft, new Vector2(40, -170), new Vector2(160, -60));
            UIFactory.Rect(_restart.gameObject, TopRight, TopRight, new Vector2(-160, -170), new Vector2(-40, -60));

            _level.fontSize = 60;
            _level.alignment = TextAnchor.MiddleCenter;
            UIFactory.Rect(_level.gameObject, TopLeft, TopRight, new Vector2(0, -170), new Vector2(0, -70));

            UIFactory.Rect(_track.gameObject, TopCenter, TopCenter, new Vector2(-300, -250), new Vector2(300, -210));

            _progress.fontSize = 34;
            UIFactory.Rect(_progress.gameObject, TopCenter, TopCenter, new Vector2(-300, -300), new Vector2(300, -256));

            _hint.fontSize = 30;
            _hint.alignment = TextAnchor.MiddleCenter;
            UIFactory.Rect(_hint.gameObject, BottomLeft, BottomRight, new Vector2(0, 24), new Vector2(0, 70));
        }

        /// The band is deliberately shallower than it looks like it needs to be. The framing
        /// solver reserves the top from the sculpture's world AABB, and that AABB is a poor
        /// predictor of where the object actually lands on screen — a 55deg tilt under a 75deg
        /// camera leans a tall shape's top toward the frame's top edge, so the measured top of
        /// the content runs from 0.18 of the screen (compact shapes) to 0.088 (level 10/20).
        /// Only the corners are safe at that height, which is why the buttons keep their size and
        /// the centred bar is the thing that had to get thinner: the sculpture is ~0.3 of the
        /// screen wide, so it passes under the bar and never under the corners.
        void LayoutLandscape()
        {
            UIFactory.Rect(_menu.gameObject, TopLeft, TopLeft, new Vector2(32, -102), new Vector2(128, -22));
            UIFactory.Rect(_restart.gameObject, TopRight, TopRight, new Vector2(-128, -102), new Vector2(-32, -22));

            _level.fontSize = 44;
            _level.alignment = TextAnchor.MiddleLeft;
            UIFactory.Rect(_level.gameObject, TopLeft, TopLeft, new Vector2(150, -96), new Vector2(560, -26));

            UIFactory.Rect(_track.gameObject, TopCenter, TopCenter, new Vector2(-300, -74), new Vector2(300, -26));

            _progress.fontSize = 28;
            UIFactory.Rect(_progress.gameObject, TopCenter, TopCenter, new Vector2(-300, -74), new Vector2(300, -26));

            // Bottom-LEFT, not centred: the bank is centred and ~0.36 of the width, and a block
            // sliding forward into the window is drawn a full row below it for the length of the
            // animation — over a centred hint every time the player deploys.
            _hint.fontSize = 26;
            _hint.alignment = TextAnchor.MiddleLeft;
            UIFactory.Rect(_hint.gameObject, BottomLeft, BottomLeft, new Vector2(34, 14), new Vector2(600, 56));
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
