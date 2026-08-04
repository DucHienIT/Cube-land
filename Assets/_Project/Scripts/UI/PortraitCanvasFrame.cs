using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    /// Pillarboxes the UI to match the camera's portrait viewport.
    ///
    /// A Screen Space - Overlay Canvas always spans the whole window, so `CameraRig`'s camera rect
    /// does nothing for it: the HUD would still stretch across the black bars. Two things have to
    /// move together for the UI to land inside the same rectangle the game renders into.
    ///
    /// 1. A `Frame` child that every screen is parented to, sized to the viewport. Screens fill
    ///    their parent, so nothing below this file knows the frame exists.
    /// 2. The CanvasScaler's scale factor, which UNITY derives from `Screen.width/height` — it has
    ///    no idea part of the window is a bar. Left alone, a 1920x1080 window scales the UI as if
    ///    it had 1920 units of width to spend and then draws it into a 607-pixel column. The scale
    ///    factor is therefore computed here from the VIEWPORT with the scaler's own authored
    ///    formula, and pushed as ConstantPixelSize. On a window that is already portrait the
    ///    viewport IS the screen, so the number is identical to what the scaler would have
    ///    produced on its own and nothing changes.
    public class PortraitCanvasFrame
    {
        const float MinScaleFactor = 0.0001f;

        readonly RectTransform _canvas;
        readonly CanvasScaler _scaler;
        readonly RectTransform _frame;

        readonly CanvasScaler.ScaleMode _authoredMode;
        readonly CanvasScaler.ScreenMatchMode _authoredMatchMode;
        readonly Vector2 _authoredReference;
        readonly float _authoredMatch;
        readonly float _authoredScaleFactor;

        Vector2 _appliedViewport;
        bool _appliedLock;
        bool _applied;

        public PortraitCanvasFrame(RectTransform canvas, CanvasScaler scaler)
        {
            _canvas = canvas;
            _scaler = scaler;

            if (scaler != null)
            {
                _authoredMode = scaler.uiScaleMode;
                _authoredMatchMode = scaler.screenMatchMode;
                _authoredReference = scaler.referenceResolution;
                _authoredMatch = scaler.matchWidthOrHeight;
                _authoredScaleFactor = scaler.scaleFactor;
            }
            else
            {
                _authoredReference = new Vector2(1080f, 1920f);
                _authoredScaleFactor = 1f;
            }

            var go = new GameObject("Frame", typeof(RectTransform));
            _frame = (RectTransform)go.transform;
            _frame.SetParent(canvas, false);
            _frame.anchorMin = _frame.anchorMax = _frame.pivot = new Vector2(0.5f, 0.5f);
            _frame.anchoredPosition = Vector2.zero;
            _frame.localScale = Vector3.one;
        }

        /// Parent for every screen — the portrait rectangle, not the raw canvas.
        public RectTransform Rect => _frame;

        public void Sync()
        {
            bool locked = ScreenLayout.IsLocked;
            var viewport = ScreenLayout.ViewportPixels;
            if (_applied && locked == _appliedLock && viewport == _appliedViewport) return;
            _applied = true;
            _appliedLock = locked;
            _appliedViewport = viewport;

            if (!locked)
            {
                Restore();
                return;
            }

            float scale = Mathf.Max(MinScaleFactor, SolveScaleFactor(viewport));
            if (_scaler != null)
            {
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                _scaler.scaleFactor = scale;
            }
            _frame.anchoredPosition = Vector2.zero;
            _frame.sizeDelta = viewport / scale;
        }

        /// Hands the canvas back to Unity when the lock is off, so turning `portraitLock` off is a
        /// true revert rather than a third layout mode.
        void Restore()
        {
            if (_scaler != null)
            {
                _scaler.uiScaleMode = _authoredMode;
                _scaler.screenMatchMode = _authoredMatchMode;
                _scaler.referenceResolution = _authoredReference;
                _scaler.matchWidthOrHeight = _authoredMatch;
                _scaler.scaleFactor = _authoredScaleFactor;
            }
            _frame.anchorMin = Vector2.zero;
            _frame.anchorMax = Vector2.one;
            _frame.pivot = new Vector2(0.5f, 0.5f);
            _frame.offsetMin = Vector2.zero;
            _frame.offsetMax = Vector2.zero;
        }

        /// The scaler's own three formulas, fed the viewport instead of the screen. Reproducing
        /// them rather than picking a simpler rule is the point: on a portrait window this has to
        /// return exactly what the authored scaler returns, or the lock would silently re-size
        /// every screen on the platform it was tuned for.
        float SolveScaleFactor(Vector2 viewport)
        {
            if (_scaler == null || _authoredMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                return _authoredScaleFactor;
            if (_authoredMode == CanvasScaler.ScaleMode.ConstantPhysicalSize)
                return _canvas != null && _canvas.lossyScale.x > 0f ? _canvas.lossyScale.x : 1f;

            float referenceWidth = Mathf.Max(1f, _authoredReference.x);
            float referenceHeight = Mathf.Max(1f, _authoredReference.y);
            float widthRatio = Mathf.Max(MinScaleFactor, viewport.x / referenceWidth);
            float heightRatio = Mathf.Max(MinScaleFactor, viewport.y / referenceHeight);

            switch (_authoredMatchMode)
            {
                case CanvasScaler.ScreenMatchMode.Expand:
                    return Mathf.Min(widthRatio, heightRatio);
                case CanvasScaler.ScreenMatchMode.Shrink:
                    return Mathf.Max(widthRatio, heightRatio);
                default:
                    float logWidth = Mathf.Log(widthRatio, 2f);
                    float logHeight = Mathf.Log(heightRatio, 2f);
                    return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, Mathf.Clamp01(_authoredMatch)));
            }
        }
    }
}
