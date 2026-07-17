using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    /// <summary>
    /// Code-first uGUI helpers. Legacy <see cref="Text"/> + built-in LegacyRuntime.ttf only
    /// (no TMP dependency). All chrome is procedural sprites from <see cref="SpriteFactory"/>.
    /// </summary>
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
            img.sprite = SpriteFactory.RoundedRect(96, 96, Palette.Active.uiCornerRadius, color, false);
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
            img.sprite = SpriteFactory.RoundedRect(160, 96, Palette.Active.uiCornerRadius, color, true);
            img.type = Image.Type.Sliced;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors; colors.pressedColor = new Color(0.88f, 0.88f, 0.88f); colors.fadeDuration = 0.06f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var t = Label("Label", go.transform, label, fontSize, Palette.Active.uiText);
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

        // ---------------- candy-style helpers ----------------

        /// <summary>Double Outline + drop Shadow so labels read puzzle-style (white text, dark rim).</summary>
        public static void AddOutline(Text t, Color color, float thickness = 3f)
        {
            // One Outline only covers the 4 diagonal offsets; two crossed ones close the rim.
            var o1 = t.gameObject.AddComponent<Outline>();
            o1.effectColor = color; o1.effectDistance = new Vector2(thickness, -thickness);
            var o2 = t.gameObject.AddComponent<Outline>();
            o2.effectColor = color; o2.effectDistance = new Vector2(-thickness, thickness);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = color; sh.effectDistance = new Vector2(0, -thickness * 1.2f);
        }

        /// <summary>Full-screen vertical gradient backdrop; tint decides the hue.</summary>
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

        /// <summary>Soft drop shadow stretched behind its parent (slightly larger, pushed down).</summary>
        public static Image SoftShadow(Transform parent)
        {
            var go = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Rect(go, Vector2.zero, Vector2.one, new Vector2(-16, -26), new Vector2(16, 2));
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.UISoftShadow();
            img.type = Image.Type.Sliced;
            img.color = Palette.Active.shadowInk;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Candy button: root (Button + UIPressEffect) → Shadow → Body (gloss, tinted) → outlined Label.
        /// Empty label = icon button (no Text child created). Position the returned button's rect yourself.
        /// </summary>
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
            btn.transition = Selectable.Transition.None; // UIPressEffect handles feedback
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            if (!string.IsNullOrEmpty(label))
            {
                var t = Label("Label", root.transform, label, fontSize, Color.white);
                var rt = FullRect(t.gameObject);
                rt.offsetMin = new Vector2(0, 4); // sprite's bottom lip is dark — optically center
                AddOutline(t, Color.Lerp(color, Color.black, 0.5f), fontSize >= 60 ? 3f : 2.5f);
            }
            return btn;
        }

        /// <summary>White rounded card with soft shadow. Children added to the returned root render on top.</summary>
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

        /// <summary>Ribbon title poking above a popup card's top edge.</summary>
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
            // Neutral ink outline — ribbons can be re-tinted per state, never bake a state color here.
            AddOutline(t, Palette.Active.outlineInk);
            return t;
        }
    }
}
