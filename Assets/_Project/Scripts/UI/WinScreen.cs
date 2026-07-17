using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class WinScreen : UIScreen
    {
        Image[] _stars = new Image[3];
        UIPopIn[] _starPops = new UIPopIn[3];
        Text _coins;
        Button _next;

        public WinScreen(UIController ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("Win", parent);
            var t = Root.transform;
            var pal = Palette.Active;

            // dim backdrop
            var dim = UIFactory.Panel("Dim", t, new Color(0, 0, 0, 0.5f));
            var dimImg = dim.GetComponent<Image>();
            // Panel bakes color into the sprite and leaves img.color white — recolor after nulling.
            dimImg.sprite = null; dimImg.color = new Color(0, 0, 0, 0.5f); dimImg.raycastTarget = true;

            var card = UIFactory.Card("Card", t, pal.cardWhite);
            UIFactory.Rect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -480), new Vector2(420, 480));
            var pop = card.AddComponent<UIPopIn>();
            pop.from = 0.75f;

            // Ribbon title poking above the card. This screen only ever shows wins, but the
            // label outline stays neutral ink so a re-tint never bakes a stale color.
            UIFactory.Ribbon(card.transform, "LEVEL CLEAR!", pal.btnGreen, 620f, 60);

            var starRow = new GameObject("Stars", typeof(RectTransform));
            starRow.transform.SetParent(card.transform, false);
            UIFactory.Rect(starRow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-270, -400), new Vector2(270, -120));
            for (int i = 0; i < 3; i++)
            {
                var star = UIFactory.Icon("S" + i, starRow.transform, SpriteFactory.Star(160, Color.white), Palette.StarEmpty);
                float x = -180 + i * 180;
                float y = i == 1 ? 20 : 0;
                UIFactory.Rect(star.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x - 80, y - 80), new Vector2(x + 80, y + 80));
                _stars[i] = star;
                _starPops[i] = star.gameObject.AddComponent<UIPopIn>();
                _starPops[i].delay = 0.25f + i * 0.15f;
                _starPops[i].duration = 0.3f;
                _starPops[i].from = 0f;
            }

            var coinChip = UIFactory.Card("CoinChip", card.transform, new Color(1f, 0.93f, 0.76f));
            UIFactory.Rect(coinChip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190, -60), new Vector2(190, 40));
            var ci = UIFactory.Icon("Coin", coinChip.transform, SpriteFactory.Circle(64, Palette.Coin), Palette.Coin);
            UIFactory.Rect(ci.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, -34), new Vector2(88, 34));
            _coins = UIFactory.Label("Coins", coinChip.transform, "0", 48, pal.cardInk, TextAnchor.MiddleCenter);
            UIFactory.Rect(_coins.gameObject, new Vector2(0, 0), new Vector2(1, 1), new Vector2(90, 0), new Vector2(-24, 0));

            _next = UIFactory.CandyButton("Next", card.transform, "NEXT", pal.btnGreen, () => UI.Next(), 60);
            UIFactory.Rect(_next.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-300, 200), new Vector2(300, 340));

            var replay = UIFactory.CandyButton("Replay", card.transform, "REPLAY", pal.btnOrange, () => UI.Restart(), 44);
            UIFactory.Rect(replay.gameObject, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(60, 70), new Vector2(-15, 180));

            var menu = UIFactory.CandyButton("Menu", card.transform, "MENU", pal.btnSlate, () => UI.BackToMenu(), 44);
            UIFactory.Rect(menu.gameObject, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(15, 70), new Vector2(-60, 180));
        }

        public void Set(int level, int stars, int coins, bool hasNext)
        {
            for (int i = 0; i < 3; i++)
            {
                bool on = i < stars;
                _stars[i].color = on ? Palette.Star : Palette.StarEmpty;
                Vector3 target = Vector3.one * (on ? 1f : 0.8f);
                _stars[i].transform.localScale = target;
                _starPops[i].SetTarget(target);
            }
            if (_coins != null) _coins.text = coins.ToString();
            if (_next != null) _next.gameObject.SetActive(hasNext);
        }
    }
}
