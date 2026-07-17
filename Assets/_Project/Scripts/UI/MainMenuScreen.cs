using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class MainMenuScreen : UIScreen
    {
        Text _coins;

        public MainMenuScreen(UIController ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("MainMenu", parent);
            var t = Root.transform;
            var pal = Palette.Active;

            UIFactory.GradientBG(t, pal.menuBg);

            // Decorative candy cubes drifting near the screen edges.
            Color[] deco = { pal.btnBlue, pal.btnOrange, pal.btnGreen, pal.btnRed, pal.uiButtonAlt };
            MakeDecoCube(t, deco[0], new Vector2(0f, 0f), new Vector2(100, 300), 120, -12f, 0.0f);
            MakeDecoCube(t, deco[1], new Vector2(1f, 0f), new Vector2(-100, 480), 96, 9f, 1.3f);
            MakeDecoCube(t, deco[2], new Vector2(0f, 1f), new Vector2(110, -340), 84, 14f, 2.1f);
            MakeDecoCube(t, deco[3], new Vector2(1f, 1f), new Vector2(-90, -900), 108, -8f, 3.0f);
            MakeDecoCube(t, deco[4], new Vector2(0f, 0.5f), new Vector2(70, -260), 70, 20f, 4.2f);
            MakeDecoCube(t, deco[0], new Vector2(1f, 0.5f), new Vector2(-80, 120), 76, -18f, 5.1f);

            // Coin chip top-right: white card + coin + dark count.
            var chip = UIFactory.Card("CoinChip", t, pal.cardWhite);
            UIFactory.Rect(chip, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-320, -170), new Vector2(-40, -70));
            var coinIcon = UIFactory.Icon("Coin", chip.transform, SpriteFactory.Circle(64, Palette.Coin), Palette.Coin);
            UIFactory.Rect(coinIcon.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, -34), new Vector2(86, 34));
            _coins = UIFactory.Label("Coins", chip.transform, "0", 44, pal.cardInk, TextAnchor.MiddleRight);
            UIFactory.Rect(_coins.gameObject, new Vector2(0, 0), new Vector2(1, 1), new Vector2(90, 0), new Vector2(-24, 0));

            // Settings top-left.
            var settings = UIFactory.CandyButton("Settings", t, "OPTIONS", pal.btnSlate, () => UI.ToggleSettings(), 36);
            UIFactory.Rect(settings.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -170), new Vector2(300, -70));

            // Tile-letter title: each letter its own candy tile, alternating tilt + phased bob.
            BuildTitleRow(t, "CUBE", -430f, 150f, 164f, deco);
            BuildTitleRow(t, "BLASTER", -610f, 116f, 128f, deco);

            var sub = UIFactory.Label("Sub", t, "tap PLAY to demolish", 40, Color.white);
            UIFactory.Rect(sub.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -820), new Vector2(0, -740));
            UIFactory.AddOutline(sub, pal.outlineInk, 2f);

            // Play button — green primary, position bob (UIMotion moves position, so it
            // doesn't fight UIPressEffect's scale squash).
            var play = UIFactory.CandyButton("Play", t, "PLAY", pal.btnGreen, () => UI.PlayPressed(), 72);
            UIFactory.Rect(play.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-330, 560), new Vector2(330, 740));
            var bob = play.gameObject.AddComponent<UIMotion>();
            bob.amp = 10f; bob.speed = 2.2f;
        }

        void BuildTitleRow(Transform parent, string word, float centerY, float tile, float spacing, Color[] colors)
        {
            float x0 = -(word.Length - 1) * spacing * 0.5f;
            for (int i = 0; i < word.Length; i++)
            {
                Color col = colors[i % colors.Length];
                var go = new GameObject("T" + word[i], typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                float cx = x0 + i * spacing;
                UIFactory.Rect(go, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(cx - tile * 0.5f, centerY - tile * 0.5f),
                    new Vector2(cx + tile * 0.5f, centerY + tile * 0.5f));
                var img = go.GetComponent<Image>();
                img.sprite = SpriteFactory.UIGloss();
                img.type = Image.Type.Sliced;
                img.color = col;
                img.raycastTarget = false;
                go.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0) ? 5f : -5f);

                var letter = UIFactory.Label("L", go.transform, word[i].ToString(), Mathf.RoundToInt(tile * 0.62f), Color.white);
                var rt = UIFactory.FullRect(letter.gameObject);
                rt.offsetMin = new Vector2(0, 4);
                UIFactory.AddOutline(letter, Color.Lerp(col, Color.black, 0.55f));

                var motion = go.AddComponent<UIMotion>();
                motion.amp = 6f; motion.speed = 2f; motion.phase = i * 0.65f; motion.rotAmp = 2f;
            }
        }

        void MakeDecoCube(Transform parent, Color col, Vector2 anchor, Vector2 pos, float size, float tilt, float phase)
        {
            var go = new GameObject("Deco", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            UIFactory.Rect(go, anchor, anchor,
                new Vector2(pos.x - size * 0.5f, pos.y - size * 0.5f),
                new Vector2(pos.x + size * 0.5f, pos.y + size * 0.5f));
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.UIGloss();
            img.type = Image.Type.Sliced;
            img.color = new Color(col.r, col.g, col.b, 0.75f);
            img.raycastTarget = false;
            go.transform.localRotation = Quaternion.Euler(0, 0, tilt);
            var motion = go.AddComponent<UIMotion>();
            motion.amp = 9f; motion.speed = 1.6f; motion.phase = phase; motion.rotAmp = 3f;
        }

        public override void Refresh()
        {
            if (_coins != null) _coins.text = SaveSystem.Coins.ToString();
        }
    }
}
