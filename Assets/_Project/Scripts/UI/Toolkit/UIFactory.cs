using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public static class UIFactory
    {
        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static RectTransform FullRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static GameObject Panel(string name, Transform parent, Color color, bool fill = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.RoundedRect(96, 96, PaletteConfig.Active.uiCornerRadius, color, false);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            if (fill) FullRect(go);
            return go;
        }

        public static RectTransform Rect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = align; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Color color, System.Action onClick, int fontSize = 46)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.RoundedRect(160, 96, PaletteConfig.Active.uiCornerRadius, color, true);
            img.type = Image.Type.Sliced;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var t = Label("Label", go.transform, label, fontSize, PaletteConfig.Active.uiText);
            FullRect(t.gameObject);
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.25f);
            shadow.effectDistance = new Vector2(0, -4);
            return btn;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false;
            return img;
        }

        public static void AddOutline(Text t, Color color, float thickness = 3f)
        {
            var first = t.gameObject.AddComponent<Outline>();
            first.effectColor = color;
            first.effectDistance = new Vector2(thickness, -thickness);
            var second = t.gameObject.AddComponent<Outline>();
            second.effectColor = color;
            second.effectDistance = new Vector2(-thickness, thickness);
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = new Vector2(0, -thickness * 1.2f);
        }

        public static Image GradientBG(Transform parent, Color tint)
        {
            var go = new GameObject("GradientBG", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            FullRect(go);
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.UIGradient();
            img.color = tint;
            img.raycastTarget = false;
            return img;
        }

        public static Image SoftShadow(Transform parent)
        {
            var go = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Rect(go, Vector2.zero, Vector2.one, new Vector2(-16, -26), new Vector2(16, 2));
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.UISoftShadow();
            img.type = Image.Type.Sliced;
            img.color = PaletteConfig.Active.shadowInk;
            img.raycastTarget = false;
            return img;
        }

        public static Button CandyButton(string name, Transform parent, string label, Color color, System.Action onClick, int fontSize = 46)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(UIPressEffect));
            root.transform.SetParent(parent, false);

            SoftShadow(root.transform);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(root.transform, false);
            FullRect(bodyGo);
            var body = bodyGo.GetComponent<Image>();
            body.sprite = SpriteFactory.UIGloss();
            body.type = Image.Type.Sliced;
            body.color = color;
            body.raycastTarget = true;

            var btn = root.GetComponent<Button>();
            btn.targetGraphic = body;
            btn.transition = Selectable.Transition.None;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            if (!string.IsNullOrEmpty(label))
            {
                var t = Label("Label", root.transform, label, fontSize, Color.white);
                var rt = FullRect(t.gameObject);
                rt.offsetMin = new Vector2(0, 4);
                AddOutline(t, Color.Lerp(color, Color.black, 0.5f), fontSize >= 60 ? 3f : 2.5f);
            }
            return btn;
        }

        public static GameObject Card(string name, Transform parent, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            SoftShadow(root.transform);
            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(root.transform, false);
            FullRect(bodyGo);
            var body = bodyGo.GetComponent<Image>();
            body.sprite = SpriteFactory.UIGloss();
            body.type = Image.Type.Sliced;
            body.color = color;
            body.raycastTarget = true;
            return root;
        }

        public static Text Ribbon(Transform card, string title, Color color, float width = 560f, int fontSize = 56)
        {
            var go = new GameObject("Ribbon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(card, false);
            Rect(go, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-width * 0.5f, 0), new Vector2(width * 0.5f, 120));
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.UIGloss();
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;

            var t = Label("Title", go.transform, title, fontSize, Color.white);
            var rt = FullRect(t.gameObject);
            rt.offsetMin = new Vector2(0, 4);
            AddOutline(t, PaletteConfig.Active.outlineInk);
            return t;
        }
    }
}
