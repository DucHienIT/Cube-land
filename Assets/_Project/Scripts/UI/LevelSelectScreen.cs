using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class LevelSelectScreen : UIScreen
    {
        RectTransform _content;

        public LevelSelectScreen(UIController ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("LevelSelect", parent);
            var t = Root.transform;
            var pal = Palette.Active;

            UIFactory.GradientBG(t, pal.menuBg);

            var header = UIFactory.Label("Header", t, "SELECT LEVEL", 66, Color.white);
            UIFactory.Rect(header.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -240), new Vector2(0, -120));
            UIFactory.AddOutline(header, pal.outlineInk);

            var back = UIFactory.CandyButton("Back", t, "MENU", pal.btnSlate, () => UI.BackToMenu(), 40);
            UIFactory.Rect(back.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -240), new Vector2(260, -130));

            // Scroll view (RectMask2D clips without a stencil graphic — robust for scroll grids)
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(t, false);
            UIFactory.Rect(scrollGo, new Vector2(0, 0), new Vector2(1, 1), new Vector2(60, 160), new Vector2(-60, -300));
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Elastic;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1); _content.pivot = new Vector2(0.5f, 1);
            _content.anchoredPosition = Vector2.zero;
            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(210, 210); grid.spacing = new Vector2(28, 28);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content; scroll.viewport = scrollGo.GetComponent<RectTransform>();
        }

        public override void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--) Object.Destroy(_content.GetChild(i).gameObject);

            var pal = Palette.Active;
            Color[] accents = { pal.btnGreen, pal.btnBlue, pal.btnOrange, pal.btnRed };
            int count = Mathf.Max(1, LevelLibrary.Count);
            int unlocked = SaveSystem.HighestUnlocked;

            for (int lv = 1; lv <= count; lv++)
            {
                bool open = lv <= unlocked;
                int level = lv;

                var card = UIFactory.Card("Lv" + lv, _content, open ? pal.cardWhite : pal.cardLocked);

                if (open)
                {
                    var btn = card.AddComponent<Button>();
                    btn.targetGraphic = card.transform.Find("Body").GetComponent<Image>();
                    btn.transition = Selectable.Transition.None;
                    btn.onClick.AddListener(() => UI.StartLevel(level));
                    card.AddComponent<UIPressEffect>();

                    // Accent strip along the top edge, colored per grid row.
                    var strip = new GameObject("Accent", typeof(RectTransform), typeof(Image));
                    strip.transform.SetParent(card.transform, false);
                    UIFactory.Rect(strip, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -30), new Vector2(-14, -10));
                    var stripImg = strip.GetComponent<Image>();
                    stripImg.sprite = SpriteFactory.UIGloss();
                    stripImg.type = Image.Type.Sliced;
                    stripImg.color = accents[((lv - 1) / 4) % accents.Length];
                    stripImg.raycastTarget = false;

                    var num = UIFactory.Label("Num", card.transform, lv.ToString(), 74, pal.cardInk);
                    UIFactory.Rect(num.gameObject, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 30), new Vector2(0, -20));

                    int stars = SaveSystem.Stars(lv);
                    var starRow = new GameObject("Stars", typeof(RectTransform));
                    starRow.transform.SetParent(card.transform, false);
                    UIFactory.Rect(starRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-90, 16), new Vector2(90, 60));
                    for (int s = 0; s < 3; s++)
                    {
                        Color starCol = s < stars ? Palette.Star : new Color(0, 0, 0, 0.10f);
                        var star = UIFactory.Icon("S" + s, starRow.transform, SpriteFactory.Star(48, Color.white), starCol);
                        UIFactory.Rect(star.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                            new Vector2(-78 + s * 54, -24), new Vector2(-30 + s * 54, 24));
                    }
                }
                else
                {
                    var lockIcon = UIFactory.Icon("Lock", card.transform, SpriteFactory.Padlock(), Color.white);
                    UIFactory.Rect(lockIcon.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-44, -24), new Vector2(44, 64));
                    var num = UIFactory.Label("Num", card.transform, lv.ToString(), 40, new Color(1, 1, 1, 0.85f));
                    UIFactory.Rect(num.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-60, 14), new Vector2(60, 62));
                }
            }
        }
    }
}
