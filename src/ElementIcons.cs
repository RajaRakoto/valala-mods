using System.Collections.Generic;
using UnityEngine;

namespace ValalaMods
{
    /// <summary>
    /// Generates crisp element icons at runtime (no external asset): a shaded disc tinted with the
    /// element colour, plus a minimalist glyph (bolt / rock / flame / poison / ice) rasterised per pixel.
    /// </summary>
    internal static class ElementIcons
    {
        private const int Size = 128;

        private static readonly Dictionary<Element, Sprite> Cache = new Dictionary<Element, Sprite>();
        private static Sprite _hub;

        internal static Sprite Get(Element element)
        {
            if (Cache.TryGetValue(element, out Sprite sprite) && sprite != null)
                return sprite;

            sprite = Build(element, ElementRegistry.Get(element).Tint);
            Cache[element] = sprite;
            return sprite;
        }

        /// <summary>Plain dark disc used for the wheel hub.</summary>
        internal static Sprite Hub()
        {
            if (_hub != null)
                return _hub;

            _hub = Build(Element.Lightning, new Color(0.10f, 0.11f, 0.14f), drawGlyph: false);
            _hub.name = "Valala_ElementHub";
            return _hub;
        }

        private static Sprite Build(Element element, Color tint, bool drawGlyph = true)
        {
            Color32[] pixels = new Color32[Size * Size];
            float cx = (Size - 1) / 2f;
            float cy = (Size - 1) / 2f;
            float radius = Size * 0.46f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float coverage = Mathf.Clamp01((radius - dist) / 2f); // anti-aliased edge
                    if (coverage <= 0f)
                        continue;

                    float shade = Mathf.Lerp(0.55f, 1.05f, dist / radius);
                    Color col = tint * shade;
                    float alpha = coverage * 0.92f;

                    if (drawGlyph)
                    {
                        float nx = x / (float)(Size - 1);
                        float ny = y / (float)(Size - 1);
                        if (InGlyph(element, nx, ny))
                        {
                            col = Color.white;
                            alpha = coverage;
                        }
                    }

                    pixels[y * Size + x] = new Color(col.r, col.g, col.b, alpha);
                }
            }

            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
            sprite.name = "Valala_ElementIcon_" + element;
            return sprite;
        }

        private static bool InGlyph(Element element, float x, float y)
        {
            switch (element)
            {
                case Element.Lightning:
                    return DistanceToSegment(x, y, 0.58f, 0.84f, 0.40f, 0.54f) < 0.045f
                        || DistanceToSegment(x, y, 0.40f, 0.54f, 0.60f, 0.50f) < 0.045f
                        || DistanceToSegment(x, y, 0.60f, 0.50f, 0.42f, 0.16f) < 0.045f;

                case Element.Rock:
                    return DistanceToSegment(x, y, 0.50f, 0.82f, 0.24f, 0.62f) < 0.035f
                        || DistanceToSegment(x, y, 0.24f, 0.62f, 0.32f, 0.30f) < 0.035f
                        || DistanceToSegment(x, y, 0.32f, 0.30f, 0.68f, 0.30f) < 0.035f
                        || DistanceToSegment(x, y, 0.68f, 0.30f, 0.76f, 0.62f) < 0.035f
                        || DistanceToSegment(x, y, 0.76f, 0.62f, 0.50f, 0.82f) < 0.035f
                        || DistanceToSegment(x, y, 0.38f, 0.58f, 0.62f, 0.46f) < 0.025f;

                case Element.Fire:
                    return InTriangle(x, y, 0.50f, 0.86f, 0.28f, 0.34f, 0.72f, 0.34f)
                        && !InTriangle(x, y, 0.50f, 0.62f, 0.42f, 0.28f, 0.58f, 0.28f);

                case Element.Poison:
                    return InCircle(x, y, 0.50f, 0.38f, 0.17f)
                        || InCircle(x, y, 0.50f, 0.64f, 0.09f)
                        || InCircle(x, y, 0.33f, 0.72f, 0.055f)
                        || InCircle(x, y, 0.67f, 0.72f, 0.055f);

                case Element.Ice:
                    return DistanceToSegment(x, y, 0.50f, 0.16f, 0.50f, 0.84f) < 0.035f
                        || DistanceToSegment(x, y, 0.30f, 0.28f, 0.70f, 0.72f) < 0.035f
                        || DistanceToSegment(x, y, 0.70f, 0.28f, 0.30f, 0.72f) < 0.035f;

                default:
                    return false;
            }
        }

        private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax;
            float vy = by - ay;
            float wx = px - ax;
            float wy = py - ay;
            float len2 = vx * vx + vy * vy;
            float t = len2 <= 0f ? 0f : Mathf.Clamp01((wx * vx + wy * vy) / len2);
            float dx = wx - vx * t;
            float dy = wy - vy * t;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static bool InCircle(float px, float py, float cx, float cy, float radius)
        {
            float dx = px - cx;
            float dy = py - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static bool InTriangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = Sign(px, py, ax, ay, bx, by);
            float d2 = Sign(px, py, bx, by, cx, cy);
            float d3 = Sign(px, py, cx, cy, ax, ay);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Sign(float px, float py, float ax, float ay, float bx, float by)
        {
            return (px - bx) * (ay - by) - (ax - bx) * (py - by);
        }
    }
}
