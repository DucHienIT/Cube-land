using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class LevelSelectScreen : UIScreen
    {
        const int StarsPerLevel = 3;
        const int GridColumns = 4;

        RectTransform _content;

        public LevelSelectScreen(IUIHost ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("LevelSelect", parent);
            var t = Root.transform;
            var pal = PaletteConfig.Active;

            UIFactory.GradientBG(t, pal.menuBg);

            var header = UIFactory.Label("Header", t, "SELECT LEVEL", 66, Color.white);
            UIFactory.Rect(header.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -240), new Vector2(0, -120));
            UIFactory.AddOutline(header, pal.outlineInk);

            var back = UIFactory.CandyButton("Back", t, "MENU", pal.btnSlate, () => UI.GoToMainMenu(), 40);
            UIFactory.Rect(back.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -240), new Vector2(260, -130));

            BuildScrollGrid(t);
        }

        void BuildScrollGrid(Transform parent)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(parent, false);
            UIFactory.Rect(scrollGo, new Vector2(0, 0), new Vector2(1, 1), new Vector2(60, 160), new Vector2(-60, -300));

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.anchoredPosition = Vector2.zero;

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(210, 210);
            grid.spacing = new Vector2(28, 28);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = _content;
            scroll.viewport = scrollGo.GetComponent<RectTransform>();
        }

        public override void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Object.Destroy(_content.GetChild(i).gameObject);

            var pal = PaletteConfig.Active;
            Color[] accents = { pal.btnGreen, pal.btnBlue, pal.btnOrange, pal.btnRed };
            int count = Mathf.Max(1, LevelLibrary.Count);
            int unlocked = SaveSystem.HighestUnlocked;

            for (int level = 1; level <= count; level++)
            {
                bool unlockedLevel = level <= unlocked;
                var card = UIFactory.Card("Lv" + level, _content, unlockedLevel ? pal.cardWhite : pal.cardLocked);

                if (unlockedLevel) BuildUnlockedCard(card, level, pal, accents);
                else BuildLockedCard(card, level);
            }
        }

        void BuildUnlockedCard(GameObject card, int level, PaletteConfig pal, Color[] accents)
        {
            int captured = level;

            var button = card.AddComponent<Button>();
            button.targetGraphic = card.transform.Find("Body").GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => UI.StartLevel(captured));
            card.AddComponent<UIPressEffect>();

            var strip = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(card.transform, false);
            UIFactory.Rect(strip, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -30), new Vector2(-14, -10));
            var stripImage = strip.GetComponent<Image>();
            stripImage.sprite = SpriteFactory.UIGloss();
            stripImage.type = Image.Type.Sliced;
            stripImage.color = accents[((level - 1) / GridColumns) % accents.Length];
            stripImage.raycastTarget = false;

            var number = UIFactory.Label("Num", card.transform, level.ToString(), 74, pal.cardInk);
            UIFactory.Rect(number.gameObject, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 30), new Vector2(0, -20));

            BuildStarRow(card.transform, SaveSystem.GetStars(level));
        }

        void BuildStarRow(Transform parent, int stars)
        {
            var starRow = new GameObject("Stars", typeof(RectTransform));
            starRow.transform.SetParent(parent, false);
            UIFactory.Rect(starRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-90, 16), new Vector2(90, 60));

            for (int i = 0; i < StarsPerLevel; i++)
            {
                Color color = i < stars ? PaletteConfig.Active.star : new Color(0, 0, 0, 0.10f);
                var star = UIFactory.Icon("S" + i, starRow.transform, SpriteFactory.Star(48, Color.white), color);
                UIFactory.Rect(star.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-78 + i * 54, -24), new Vector2(-30 + i * 54, 24));
            }
        }

        void BuildLockedCard(GameObject card, int level)
        {
            var lockIcon = UIFactory.Icon("Lock", card.transform, SpriteFactory.Padlock(), Color.white);
            UIFactory.Rect(lockIcon.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-44, -24), new Vector2(44, 64));

            var number = UIFactory.Label("Num", card.transform, level.ToString(), 40, new Color(1, 1, 1, 0.85f));
            UIFactory.Rect(number.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-60, 14), new Vector2(60, 62));
        }
    }
}
