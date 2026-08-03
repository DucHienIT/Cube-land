using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class MainMenuScreen : UIScreen
    {
        /// The title block hangs off ONE fractional anchor and spaces its rows with absolute
        /// offsets. Both parts matter: a fraction keeps the block in the same place on a canvas
        /// that is 1920 tall in portrait and 1080 in landscape, while the gap between two rows
        /// has to stay in the tiles' own units — a fractional gap that clears 150px tiles in
        /// portrait is only ~100px in landscape and the words overlap.
        const float TitleAnchorY = 0.72f;

        Text _coins;

        public MainMenuScreen(IUIHost ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("MainMenu", parent);
            var t = Root.transform;
            var pal = PaletteConfig.Active;

            UIFactory.GradientBG(t, pal.menuBg);

            Color[] deco = { pal.btnBlue, pal.btnOrange, pal.btnGreen, pal.btnRed, pal.uiButtonAlt };
            BuildDecoration(t, deco);
            BuildCoinChip(t, pal);

            var settings = UIFactory.CandyButton("Settings", t, "OPTIONS", pal.btnSlate, () => UI.ToggleSettings(), 36);
            UIFactory.Rect(settings.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -170), new Vector2(300, -70));

            BuildTitleRow(t, "CUBE", 95f, 150f, 164f, deco);
            BuildTitleRow(t, "BLASTER", -85f, 116f, 128f, deco);

            var sub = UIFactory.Label("Sub", t, "tap PLAY to demolish", 40, Color.white);
            UIFactory.Rect(sub.gameObject, new Vector2(0, TitleAnchorY), new Vector2(1, TitleAnchorY),
                new Vector2(0, -245), new Vector2(0, -165));
            UIFactory.AddOutline(sub, pal.outlineInk, 2f);

            var play = UIFactory.CandyButton("Play", t, "PLAY", pal.btnGreen, () => UI.GoToLevelSelect(), 72);
            UIFactory.Rect(play.gameObject, new Vector2(0.5f, 0.339f), new Vector2(0.5f, 0.339f), new Vector2(-330, -90), new Vector2(330, 90));
            var bob = play.gameObject.AddComponent<UIMotion>();
            bob.amp = 10f;
            bob.speed = 2.2f;
        }

        /// Anchors are FRACTIONS of the screen, not pixel offsets from an edge: the canvas is
        /// 1080x1920 logical in portrait but 1920x1080 in landscape, so anything placed by a
        /// pixel offset from the opposite edge collides in one of the two — which is what put
        /// the PLAY button through the middle of the title on a wide screen.
        void BuildDecoration(Transform parent, Color[] deco)
        {
            MakeDecoCube(parent, deco[0], new Vector2(0.06f, 0.16f), 120, -12f, 0.0f);
            MakeDecoCube(parent, deco[1], new Vector2(0.93f, 0.26f), 96, 9f, 1.3f);
            MakeDecoCube(parent, deco[2], new Vector2(0.12f, 0.74f), 84, 14f, 2.1f);
            MakeDecoCube(parent, deco[3], new Vector2(0.91f, 0.54f), 108, -8f, 3.0f);
            MakeDecoCube(parent, deco[4], new Vector2(0.05f, 0.46f), 70, 20f, 4.2f);
            MakeDecoCube(parent, deco[0], new Vector2(0.95f, 0.68f), 76, -18f, 5.1f);
        }

        void BuildCoinChip(Transform parent, PaletteConfig pal)
        {
            var chip = UIFactory.Card("CoinChip", parent, pal.cardWhite);
            UIFactory.Rect(chip, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-320, -170), new Vector2(-40, -70));

            var coinIcon = UIFactory.Icon("Coin", chip.transform, SpriteFactory.Circle(64, PaletteConfig.Active.coin), PaletteConfig.Active.coin);
            UIFactory.Rect(coinIcon.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, -34), new Vector2(86, 34));

            _coins = UIFactory.Label("Coins", chip.transform, "0", 44, pal.cardInk, TextAnchor.MiddleRight);
            UIFactory.Rect(_coins.gameObject, new Vector2(0, 0), new Vector2(1, 1), new Vector2(90, 0), new Vector2(-24, 0));
        }

        void BuildTitleRow(Transform parent, string word, float offsetY, float tile, float spacing, Color[] colors)
        {
            float x0 = -(word.Length - 1) * spacing * 0.5f;
            var anchor = new Vector2(0.5f, TitleAnchorY);
            for (int i = 0; i < word.Length; i++)
            {
                Color color = colors[i % colors.Length];
                var go = new GameObject("T" + word[i], typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);

                float cx = x0 + i * spacing;
                UIFactory.Rect(go, anchor, anchor,
                    new Vector2(cx - tile * 0.5f, offsetY - tile * 0.5f),
                    new Vector2(cx + tile * 0.5f, offsetY + tile * 0.5f));

                var image = go.GetComponent<Image>();
                image.sprite = SpriteFactory.UIGloss();
                image.type = Image.Type.Sliced;
                image.color = color;
                image.raycastTarget = false;
                go.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0) ? 5f : -5f);

                var letter = UIFactory.Label("L", go.transform, word[i].ToString(), Mathf.RoundToInt(tile * 0.62f), Color.white);
                var rect = UIFactory.FullRect(letter.gameObject);
                rect.offsetMin = new Vector2(0, 4);
                UIFactory.AddOutline(letter, Color.Lerp(color, Color.black, 0.55f));

                var motion = go.AddComponent<UIMotion>();
                motion.amp = 6f;
                motion.speed = 2f;
                motion.phase = i * 0.65f;
                motion.rotAmp = 2f;
            }
        }

        void MakeDecoCube(Transform parent, Color color, Vector2 anchor, float size, float tilt, float phase)
        {
            var go = new GameObject("Deco", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            UIFactory.Rect(go, anchor, anchor,
                new Vector2(-size * 0.5f, -size * 0.5f),
                new Vector2(size * 0.5f, size * 0.5f));

            var image = go.GetComponent<Image>();
            image.sprite = SpriteFactory.UIGloss();
            image.type = Image.Type.Sliced;
            image.color = new Color(color.r, color.g, color.b, 0.75f);
            image.raycastTarget = false;
            go.transform.localRotation = Quaternion.Euler(0, 0, tilt);

            var motion = go.AddComponent<UIMotion>();
            motion.amp = 9f;
            motion.speed = 1.6f;
            motion.phase = phase;
            motion.rotAmp = 3f;
        }

        public override void Refresh()
        {
            if (_coins != null) _coins.text = SaveSystem.Coins.ToString();
        }
    }
}
