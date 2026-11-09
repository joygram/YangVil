using YangVil.Data;
using UnityEngine;

namespace YangVil.Core
{
    public static class CoatPainter
    {
        public static Texture2D Paint(Sprite source, CatGenes g)
        {
            var r = source.rect;
            var w = (int)r.width;
            var h = (int)r.height;
            var src = source.texture.GetPixels((int)r.x, (int)r.y, w, h);
            var dst = new Color[src.Length];
            var minY = h;
            var maxY = 0;
            for (var i = 0; i < src.Length; i++)
            {
                if (src[i].a < 0.05f) continue;
                var y = i / w;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            var bodyH = Mathf.Max(1, maxY - minY + 1);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                var s = src[i];
                if (s.a < 0.05f)
                {
                    dst[i] = Color.clear;
                    continue;
                }
                var l = s.r * 0.3f + s.g * 0.59f + s.b * 0.11f;
                var shade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(l * 1.6f));
                var rel = (float)(y - minY) / bodyH;
                Color c;
                switch (g.pattern)
                {
                    case CoatPattern.Tabby:
                        c = Color.Lerp(g.dark, g.light, shade);
                        if (((x + y / 2 + g.seed) % 4) == 0) c = Color.Lerp(c, g.dark, 0.55f);
                        break;
                    case CoatPattern.Tuxedo:
                        var belly = rel < 0.38f || (rel < 0.6f && Mathf.Abs(x - w / 2f) < w * 0.14f);
                        c = belly ? Color.Lerp(g.accent * 0.8f, g.accent, shade) : Color.Lerp(g.accent2, g.dark, shade);
                        break;
                    case CoatPattern.Calico:
                        var n = Hash(x / 8, y / 6, g.seed) % 5;
                        var baseColor = n == 0 ? g.accent : n == 1 ? g.accent2 : g.light;
                        c = Color.Lerp(baseColor * 0.75f, baseColor, shade);
                        break;
                    default:
                        c = Color.Lerp(g.dark, g.light, shade);
                        break;
                }
                if (l < 0.12f) c = Color.Lerp(c, Color.black, 0.65f);
                if (g.outlier == Outlier.Rainbow)
                {
                    Color.RGBToHSV(c, out _, out var sat, out var val);
                    c = Color.HSVToRGB(((x + y) / (float)(w + h) + g.seed % 100 / 100f) % 1f, Mathf.Max(0.55f, sat), Mathf.Max(0.6f, val));
                }
                c.a = g.outlier == Outlier.Ghost ? s.a * 0.55f : s.a;
                dst[i] = c;
            }

            if (g.outlier == Outlier.Grandpa) Mustache(dst, w, minY, bodyH);

            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = source.texture.filterMode, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels(dst);
            t.Apply();
            return t;
        }

        static void Mustache(Color[] px, int w, int minY, int bodyH)
        {
            var y = minY + (int)(bodyH * 0.62f);
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (i < 0 || i >= px.Length || px[i].a < 0.05f) continue;
                if (x % 3 != 1) px[i] = new Color(0.97f, 0.97f, 0.95f, px[i].a);
            }
        }

        static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = x * 374761393 + y * 668265263 + seed * 2147483647;
                h = (h ^ (h >> 13)) * 1274126177;
                return (h ^ (h >> 16)) & 0x7fffffff;
            }
        }
    }
}
