using UnityEngine;

namespace CubeBlaster
{
    /// The one place that decides what shape the game is being played in. Everything that lays
    /// itself out — the UI screens, the bank, the camera framing — reads the orientation from
    /// here rather than from `Screen` directly, so a portrait lock is a single decision instead
    /// of a flag threaded through every layout.
    ///
    /// The lock does NOT change the window: it pillarboxes inside it. A host we do not control
    /// (a desktop browser, an editor Game view, an embedded iframe) hands us whatever rectangle
    /// it likes, and re-laying the whole game out for a wide one is a second design to keep
    /// correct. `Viewport` is the portrait slice we actually render into; the bars beside it are
    /// never drawn to.
    public static class ScreenLayout
    {
        public const float LandscapeAspect = 1.2f;

        static readonly Rect FullScreen = new Rect(0f, 0f, 1f, 1f);

        /// Aspect the game is pinned to (width / height), or 0 when it follows the window.
        public static float LockedAspect { get; private set; }

        public static bool IsLocked => LockedAspect > 0f;

        public static float ScreenAspect => Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

        /// Aspect of the rectangle the game is actually drawn into — the locked one whenever the
        /// window is wider than it, the window's own otherwise.
        public static float Aspect
        {
            get
            {
                float aspect = ScreenAspect;
                return IsLocked && aspect > LockedAspect ? LockedAspect : aspect;
            }
        }

        public static bool IsLandscape => IsLandscapeAspect(Aspect);

        public static bool IsLandscapeAspect(float aspect) => aspect >= LandscapeAspect;

        /// Pins the game to `aspect` (pass 0 or less to follow the window again).
        public static void Lock(float aspect)
        {
            LockedAspect = aspect > 0f ? aspect : 0f;
        }

        /// The slice of the screen the game renders into, in normalized viewport coordinates —
        /// the full screen unless the window is WIDER than the lock, in which case it is a
        /// centred column of the locked aspect. A window that is TALLER than the lock is left
        /// alone on purpose: extra height is the one thing a portrait layout can absorb, and
        /// letterboxing it would just throw away screen on a modern phone.
        public static Rect Viewport
        {
            get
            {
                if (!IsLocked) return FullScreen;
                float aspect = ScreenAspect;
                if (aspect <= LockedAspect) return FullScreen;
                float width = LockedAspect / aspect;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
        }

        /// `Viewport` in pixels — what the UI has to size itself against once the camera is
        /// pillarboxed, since a Canvas still spans the whole window.
        public static Vector2 ViewportPixels
        {
            get
            {
                var viewport = Viewport;
                return new Vector2(Screen.width * viewport.width, Screen.height * viewport.height);
            }
        }
    }
}
