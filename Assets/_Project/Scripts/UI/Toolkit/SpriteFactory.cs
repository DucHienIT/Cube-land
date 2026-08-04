using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    public static class SpriteFactory
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite RoundedRect(int w, int h, float radius, Color color, bool gradient = true)
        {
            string key = $"rr_{w}x{h}_{radius}_{ColorKey(color)}_{gradient}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = NewTexture(w, h);
            var px = new Color[w * h];
            Color top = LightenColor(color, 0.16f);
            Color bottom = DarkenColor(color, 0.14f);

            for (int y = 0; y < h; y++)
            {
                float ty = h <= 1 ? 0f : (float)y / (h - 1);
                Color baseColor = gradient ? Color.Lerp(bottom, top, ty) : color;
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedBoxSDF(x + 0.5f, y + 0.5f, w, h, radius);
                    float a = Mathf.Clamp01(0.5f - d);
                    Color c = baseColor;
                    if (ty > 0.62f) c = Color.Lerp(c, LightenColor(color, 0.28f), (ty - 0.62f) * 1.6f);
                    px[y * w + x] = new Color(c.r, c.g, c.b, a * color.a);
                }
            }
            Commit(tex, px);

            float border = Mathf.Min(radius, Mathf.Min(w, h) * 0.45f);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite Circle(int d, Color color, bool gradient = true)
        {
            string key = $"ci_{d}_{ColorKey(color)}_{gradient}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = NewTexture(d, d);
            var px = new Color[d * d];
            float r = d * 0.5f;
            Color top = LightenColor(color, 0.2f);
            Color bottom = DarkenColor(color, 0.16f);

            for (int y = 0; y < d; y++)
            {
                float ty = (float)y / (d - 1);
                Color baseColor = gradient ? Color.Lerp(bottom, top, ty) : color;
                for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                    float a = Mathf.Clamp01(r - dist);
                    px[y * d + x] = new Color(baseColor.r, baseColor.g, baseColor.b, a * color.a);
                }
            }
            Commit(tex, px);

            var sprite = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite Star(int size, Color color)
        {
            string key = $"st_{size}_{ColorKey(color)}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = NewTexture(size, size);
            var px = new Color[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float outer = size * 0.48f, inner = outer * 0.44f;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float segment = Mathf.PI * 2f / 5f;
                    float a = Mathf.Repeat(angle + Mathf.PI / 2f, segment) / segment;
                    float t = Mathf.Abs(a - 0.5f) * 2f;
                    float edge = Mathf.Lerp(inner, outer, t);
                    float alpha = Mathf.Clamp01(edge - p.magnitude);
                    px[y * size + x] = new Color(color.r, color.g, color.b, alpha * color.a);
                }
            Commit(tex, px);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite UIGloss()
        {
            const string key = "uigloss";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            const int S = 96;
            const float R = 30f;
            var tex = NewTexture(S, S);
            var px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = RoundedBoxSDF(x + 0.5f, y + 0.5f, S, S, R);
                    float a = Mathf.Clamp01(0.5f - d);
                    float g = 0.86f + 0.14f * (y / (float)(S - 1));
                    if (y < 14) g *= 0.76f + 0.24f * (y / 14f);
                    if (y > S - 20 && d < -7f) g += 0.10f;
                    if (d > -3f) g *= 0.58f;
                    g = Mathf.Clamp01(g);
                    px[y * S + x] = new Color(g, g, g, a);
                }
            Commit(tex, px);

            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(34, 34, 34, 34));
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite UISoftShadow()
        {
            const string key = "uishadow";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            const int S = 96;
            var tex = NewTexture(S, S);
            var px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = RoundedBoxSDF(x + 0.5f, y + 0.5f, S, S, 30f);
                    float a = Mathf.Clamp01(1f - (d + 12f) / 16f);
                    a *= a;
                    px[y * S + x] = new Color(1f, 1f, 1f, a);
                }
            Commit(tex, px);

            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(44, 44, 44, 44));
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite UIGradient()
        {
            const string key = "uigradient";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            const int W = 8, H = 256;
            var tex = NewTexture(W, H);
            var px = new Color[W * H];

            for (int y = 0; y < H; y++)
            {
                float g = Mathf.Lerp(0.72f, 1f, y / (float)(H - 1));
                for (int x = 0; x < W; x++) px[y * W + x] = new Color(g, g, g, 1f);
            }
            Commit(tex, px);

            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite Padlock()
        {
            const string key = "padlock";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            const int S = 96;
            var tex = NewTexture(S, S);
            var px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float body = BoxSDF(p, 48f, 32f, 30f, 24f, 10f);
                    float a = Mathf.Clamp01(0.5f - body);
                    if (p.y > 52f)
                    {
                        float ring = Mathf.Abs(Vector2.Distance(p, new Vector2(48f, 52f)) - 17f) - 6f;
                        a = Mathf.Max(a, Mathf.Clamp01(0.5f - ring));
                    }
                    float hole = Mathf.Min(Vector2.Distance(p, new Vector2(48f, 40f)) - 8f,
                        BoxSDF(p, 48f, 28f, 4f, 12f, 3f));
                    a = Mathf.Min(a, Mathf.Clamp01(hole + 0.25f));
                    px[y * S + x] = new Color(1f, 1f, 1f, a);
                }
            Commit(tex, px);

            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = sprite;
            return sprite;
        }

        static Texture2D NewTexture(int w, int h) => new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        static void Commit(Texture2D tex, Color[] pixels)
        {
            tex.SetPixels(pixels);
            tex.Apply();
        }

        static float BoxSDF(Vector2 p, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(p.x - cx) - (hx - r);
            float qy = Mathf.Abs(p.y - cy) - (hy - r);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static float RoundedBoxSDF(float px, float py, int w, int h, float r)
        {
            r = Mathf.Min(r, Mathf.Min(w, h) * 0.5f);
            float cx = w * 0.5f, cy = h * 0.5f;
            float qx = Mathf.Abs(px - cx) - (cx - r);
            float qy = Mathf.Abs(py - cy) - (cy - r);
            float ax = Mathf.Max(qx, 0), ay = Mathf.Max(qy, 0);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static Color LightenColor(Color c, float t) =>
            new Color(Mathf.Lerp(c.r, 1f, t), Mathf.Lerp(c.g, 1f, t), Mathf.Lerp(c.b, 1f, t), c.a);

        static Color DarkenColor(Color c, float t) =>
            new Color(Mathf.Lerp(c.r, 0f, t), Mathf.Lerp(c.g, 0f, t), Mathf.Lerp(c.b, 0f, t), c.a);

        static string ColorKey(Color c) =>
            $"{(int)(c.r * 255)}_{(int)(c.g * 255)}_{(int)(c.b * 255)}_{(int)(c.a * 255)}";
    }
}
