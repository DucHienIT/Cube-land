using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class WinScreen : UIScreen
    {
        const int StarCount = 3;

        readonly Image[] _stars = new Image[StarCount];
        readonly UIPopIn[] _starPops = new UIPopIn[StarCount];

        Text _coins;
        Button _next;

        public WinScreen(IUIHost ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("Win", parent);
            var t = Root.transform;
            var pal = PaletteConfig.Active;

            var dim = UIFactory.Panel("Dim", t, new Color(0, 0, 0, 0.5f));
            var dimImage = dim.GetComponent<Image>();
            dimImage.sprite = null;
            dimImage.color = new Color(0, 0, 0, 0.5f);
            dimImage.raycastTarget = true;

            var card = UIFactory.Card("Card", t, pal.cardWhite);
            UIFactory.Rect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -480), new Vector2(420, 480));
            card.AddComponent<UIPopIn>().from = 0.75f;

            UIFactory.Ribbon(card.transform, "LEVEL CLEAR!", pal.btnGreen, 620f, 60);

            BuildStarRow(card.transform);
            BuildCoinChip(card.transform, pal);

            _next = UIFactory.CandyButton("Next", card.transform, "NEXT", pal.btnGreen, () => UI.GoToNextLevel(), 60);
            UIFactory.Rect(_next.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-300, 200), new Vector2(300, 340));

            var replay = UIFactory.CandyButton("Replay", card.transform, "REPLAY", pal.btnOrange, () => UI.RestartLevel(), 44);
            UIFactory.Rect(replay.gameObject, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(60, 70), new Vector2(-15, 180));

            var menu = UIFactory.CandyButton("Menu", card.transform, "MENU", pal.btnSlate, () => UI.GoToMainMenu(), 44);
            UIFactory.Rect(menu.gameObject, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(15, 70), new Vector2(-60, 180));
        }

        void BuildStarRow(Transform parent)
        {
            var starRow = new GameObject("Stars", typeof(RectTransform));
            starRow.transform.SetParent(parent, false);
            UIFactory.Rect(starRow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-270, -400), new Vector2(270, -120));

            for (int i = 0; i < StarCount; i++)
            {
                var star = UIFactory.Icon("S" + i, starRow.transform, SpriteFactory.Star(160, Color.white), PaletteConfig.Active.starEmpty);
                float x = -180 + i * 180;
                float y = i == 1 ? 20 : 0;
                UIFactory.Rect(star.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x - 80, y - 80), new Vector2(x + 80, y + 80));

                _stars[i] = star;
                _starPops[i] = star.gameObject.AddComponent<UIPopIn>();
                _starPops[i].delay = 0.25f + i * 0.15f;
                _starPops[i].duration = 0.3f;
                _starPops[i].from = 0f;
            }
        }

        void BuildCoinChip(Transform parent, PaletteConfig pal)
        {
            var chip = UIFactory.Card("CoinChip", parent, new Color(1f, 0.93f, 0.76f));
            UIFactory.Rect(chip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190, -60), new Vector2(190, 40));

            var icon = UIFactory.Icon("Coin", chip.transform, SpriteFactory.Circle(64, PaletteConfig.Active.coin), PaletteConfig.Active.coin);
            UIFactory.Rect(icon.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, -34), new Vector2(88, 34));

            _coins = UIFactory.Label("Coins", chip.transform, "0", 48, pal.cardInk, TextAnchor.MiddleCenter);
            UIFactory.Rect(_coins.gameObject, new Vector2(0, 0), new Vector2(1, 1), new Vector2(90, 0), new Vector2(-24, 0));
        }

        public void Set(int level, int stars, int coins, bool hasNext)
        {
            for (int i = 0; i < StarCount; i++)
            {
                bool earned = i < stars;
                _stars[i].color = earned ? PaletteConfig.Active.star : PaletteConfig.Active.starEmpty;
                Vector3 target = Vector3.one * (earned ? 1f : 0.8f);
                _stars[i].transform.localScale = target;
                _starPops[i].SetTarget(target);
            }
            if (_coins != null) _coins.text = coins.ToString();
            if (_next != null) _next.gameObject.SetActive(hasNext);
        }
    }
}
