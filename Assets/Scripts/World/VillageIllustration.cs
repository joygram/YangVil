using System.Collections.Generic;
using UnityEngine;

namespace YangVil.World
{
    public enum VillageMotif { Panel, Disc, Tree, Bush, Flowers, Pond, Basket, Shop, Tower, Gate, Book, House, Bowl, Yarn, Paw, Lock, Flag }

    // Native shape art shared by world scenery and the UI asset baker.
    public static class VillageIllustration
    {
        public static readonly Color Ink = Hex(0x664f43);
        public static readonly Color Cream = Hex(0xfff3d8);
        public static readonly Color Grass = Hex(0xb8ce91);
        public static readonly Color Sage = Hex(0x668a68);
        public static readonly Color Peach = Hex(0xde927b);
        static readonly Dictionary<VillageMotif, Sprite> Cache = new Dictionary<VillageMotif, Sprite>();
        public static Color Hex(uint rgb) => new Color((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f);

        public static Sprite Get(VillageMotif motif)
        {
            if (Cache.TryGetValue(motif, out var sprite) && sprite != null) return sprite;
            var texture = Draw(motif);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, 100,
                0, SpriteMeshType.FullRect, motif == VillageMotif.Panel ? Vector4.one * 24 : Vector4.zero);
            sprite.name = "village_" + motif.ToString().ToLowerInvariant();
            Cache[motif] = sprite;
            return sprite;
        }

        public static Texture2D Draw(VillageMotif motif)
        {
            var p = new Painter(192);
            var dark = Ink;
            var cream = Cream;
            var leaf = Hex(0x82a878);
            var lightLeaf = Hex(0xa5bf83);
            var wood = Hex(0xb88763);
            var shadow = new Color(0.27f, 0.36f, 0.24f, 0.19f);
            if (motif == VillageMotif.Panel)
            {
                p.Round(3, 2, 94, 94, 13, Hex(0xc6af87));
                p.Round(3, 5, 94, 92, 13, cream);
                p.Round(6, 9, 88, 85, 10, Hex(0xfff9e9));
            }
            else if (motif == VillageMotif.Disc) p.Oval(50, 50, 49, 49, Color.white);
            else if (motif == VillageMotif.Tree)
            {
                p.Oval(50, 12, 32, 9, shadow);
                p.Round(44, 12, 13, 48, 4, wood);
                p.Poly(wood, 46, 35, 30, 50, 35, 54, 49, 45);
                p.Oval(50, 57, 34, 33, Sage);
                p.Oval(32, 62, 23, 23, leaf);
                p.Oval(63, 65, 27, 25, leaf);
                p.Oval(48, 78, 26, 20, lightLeaf);
                p.Oval(33, 73, 12, 8, Hex(0xb9ce92));
                foreach (var v in new[] { new Vector2(32, 58), new Vector2(65, 70), new Vector2(52, 84) })
                    p.Oval(v.x, v.y, 3, 3, Hex(0xe5ac7e));
            }
            else if (motif == VillageMotif.Bush || motif == VillageMotif.Flowers)
            {
                p.Oval(50, 24, 43, 10, shadow);
                p.Oval(29, 42, 24, 24, leaf); p.Oval(65, 43, 28, 27, Sage); p.Oval(47, 49, 26, 28, lightLeaf);
                if (motif == VillageMotif.Flowers)
                    foreach (var v in new[] { new Vector2(22, 49), new Vector2(46, 65), new Vector2(70, 44), new Vector2(55, 37) })
                    {
                        p.Oval(v.x, v.y, 6, 6, cream); p.Oval(v.x, v.y, 2, 2, Hex(0xe8b967));
                    }
            }
            else if (motif == VillageMotif.Pond)
            {
                p.Oval(50, 45, 47, 35, Hex(0xe4d9ad)); p.Oval(50, 47, 42, 29, Hex(0x7facac));
                p.Oval(49, 51, 38, 24, Hex(0x9dc7c4));
                p.Round(23, 51, 27, 2, 1, Hex(0xc7e3d8)); p.Round(51, 38, 25, 2, 1, Hex(0xc7e3d8));
                p.Oval(68, 61, 9, 5, Sage); p.Oval(72, 64, 4, 4, Hex(0xf2c4b1));
                p.Oval(19, 30, 9, 5, Hex(0xb5b69a)); p.Oval(80, 30, 7, 5, Hex(0xc6c3a2));
            }
            else if (motif == VillageMotif.Bowl)
            {
                p.Oval(50, 30, 38, 12, shadow); p.Poly(Peach, 12, 48, 20, 22, 80, 22, 88, 48);
                p.Oval(50, 48, 38, 15, cream); p.Oval(50, 48, 28, 9, wood);
                p.Oval(42, 49, 7, 4, Hex(0xd8b57d)); p.Oval(58, 48, 7, 4, Hex(0xd8b57d));
            }
            else if (motif == VillageMotif.Yarn || motif == VillageMotif.Paw)
            {
                if (motif == VillageMotif.Yarn)
                {
                    p.Oval(50, 22, 35, 8, shadow); p.Oval(48, 51, 33, 33, Peach);
                    p.Oval(42, 59, 24, 23, Hex(0xf0b6a0));
                    p.Line(new Vector2(25, 65), new Vector2(62, 30), 2, cream);
                    p.Line(new Vector2(23, 54), new Vector2(55, 26), 2, cream);
                    p.Line(new Vector2(42, 80), new Vector2(76, 46), 2, Hex(0xce856d));
                    p.Line(new Vector2(71, 33), new Vector2(89, 22), 2, Peach);
                }
                else
                {
                    p.Oval(50, 37, 23, 19, Peach); p.Oval(22, 58, 10, 13, Peach);
                    p.Oval(42, 76, 10, 13, Peach); p.Oval(64, 76, 10, 13, Peach); p.Oval(82, 56, 10, 13, Peach);
                }
            }
            else if (motif == VillageMotif.Lock)
            {
                p.Round(27, 44, 46, 42, 20, dark); p.Round(35, 44, 30, 33, 13, cream);
                p.Round(20, 16, 60, 47, 8, wood); p.Oval(50, 42, 6, 6, cream); p.Box(47, 28, 6, 14, cream);
            }
            else if (motif == VillageMotif.Flag)
            {
                p.Box(29, 12, 5, 77, wood); p.Poly(Peach, 34, 88, 87, 82, 73, 65, 84, 50, 34, 56);
                p.Oval(31, 89, 5, 5, Hex(0xe8bd74));
            }
            else
            {
                p.Oval(50, 12, 43, 10, shadow);
                if (motif == VillageMotif.Gate)
                {
                    p.Round(14, 12, 19, 66, 7, wood); p.Round(67, 12, 19, 66, 7, wood);
                    p.Round(7, 64, 86, 15, 5, dark); p.Round(10, 68, 80, 13, 4, wood);
                    p.Poly(Sage, 6, 81, 50, 98, 95, 81); p.Oval(50, 75, 12, 10, cream);
                    p.Poly(Peach, 44, 74, 56, 74, 50, 64); p.Round(29, 43, 42, 11, 4, cream);
                    p.Box(22, 14, 4, 44, Hex(0xd6ac7c)); p.Box(74, 14, 4, 44, Hex(0xd6ac7c));
                }
                else if (motif == VillageMotif.Basket)
                {
                    p.Oval(50, 46, 39, 39, wood); p.Oval(50, 49, 31, 29, cream);
                    p.Oval(50, 46, 24, 20, Hex(0xf2d5bd));
                    p.Poly(wood, 9, 49, 18, 13, 82, 13, 91, 49);
                    p.Round(8, 43, 84, 12, 5, Hex(0xd3a377));
                    for (var x = 23; x < 85; x += 15) p.Box(x, 17, 2, 24, Hex(0xe3bc8c));
                    p.Box(17, 29, 66, 2, Hex(0xe3bc8c));
                    p.Oval(50, 44, 17, 10, cream);
                    p.Poly(Peach, 38, 52, 33, 65, 45, 57); p.Poly(Peach, 55, 57, 68, 65, 63, 51);
                    p.Oval(44, 46, 2, 2, dark); p.Oval(56, 46, 2, 2, dark);
                }
                else
                {
                    var roof = motif == VillageMotif.Shop ? Sage : motif == VillageMotif.Book ? Hex(0x9690b3) : Peach;
                    var tall = motif == VillageMotif.Tower;
                    p.Round(tall ? 30 : 17, 15, tall ? 40 : 66, tall ? 62 : 46, 5, Hex(0xd9bf96));
                    p.Round(tall ? 33 : 20, 19, tall ? 34 : 60, tall ? 59 : 42, 4, cream);
                    p.Poly(dark, tall ? 23 : 9, 58, 50, tall ? 99 : 88, tall ? 77 : 92, 58);
                    p.Poly(roof, tall ? 25 : 10, 61, 50, tall ? 98 : 88, tall ? 75 : 91, 61);
                    p.Poly(Color.Lerp(roof, cream, 0.25f), 13, 63, 50, 87, 52, 74, 30, 62);
                    p.Round(41, 17, 19, 27, 9, wood); p.Oval(55, 28, 1.5f, 1.5f, cream);
                    p.Round(25, 37, 12, 14, 3, Hex(0x9ebcb4)); p.Box(30, 37, 2, 14, cream); p.Box(25, 43, 12, 2, cream);
                    p.Round(64, 37, 12, 14, 3, Hex(0x9ebcb4)); p.Box(69, 37, 2, 14, cream); p.Box(64, 43, 12, 2, cream);
                    if (motif == VillageMotif.Shop)
                    {
                        p.Round(13, 45, 75, 13, 3, roof);
                        for (var x = 16; x < 87; x += 20) p.Box(x, 45, 9, 13, cream);
                    }
                    if (motif == VillageMotif.Book)
                    {
                        p.Poly(cream, 34, 69, 48, 65, 64, 69, 64, 54, 49, 51, 34, 55);
                        p.Line(new Vector2(49, 52), new Vector2(49, 64), 1, wood);
                    }
                    if (motif == VillageMotif.Tower)
                    {
                        p.Oval(47, 77, 4, 4, cream); p.Oval(53, 77, 4, 4, cream);
                        p.Poly(cream, 43, 76, 57, 76, 50, 68);
                    }
                    p.Oval(19, 18, 14, 9, lightLeaf); p.Oval(80, 20, 12, 8, leaf);
                    p.Oval(18, 22, 3, 3, Peach); p.Oval(81, 23, 3, 3, cream);
                }
            }
            return p.Finish();
        }

        sealed class Painter
        {
            readonly int _size;
            readonly Color[] _pixels;
            public Painter(int size) { _size = size; _pixels = new Color[size * size]; }
            void Fill(float x0, float y0, float x1, float y1, System.Func<float, float, bool> inside, Color c)
            {
                var k = _size / 100f;
                for (var y = Mathf.Max(0, Mathf.FloorToInt(y0 * k)); y < Mathf.Min(_size, Mathf.CeilToInt(y1 * k)); y++)
                for (var x = Mathf.Max(0, Mathf.FloorToInt(x0 * k)); x < Mathf.Min(_size, Mathf.CeilToInt(x1 * k)); x++)
                    if (inside((x + 0.5f) / k, (y + 0.5f) / k)) _pixels[y * _size + x] = c;
            }
            public void Box(float x, float y, float w, float h, Color c) => Fill(x, y, x + w, y + h, (a, b) => true, c);
            public void Oval(float x, float y, float rx, float ry, Color c) => Fill(x - rx, y - ry, x + rx, y + ry,
                (a, b) => (a - x) * (a - x) / (rx * rx) + (b - y) * (b - y) / (ry * ry) <= 1, c);
            public void Round(float x, float y, float w, float h, float r, Color c) => Fill(x, y, x + w, y + h,
                (a, b) => Vector2.SqrMagnitude(new Vector2(a - Mathf.Clamp(a, x + r, x + w - r), b - Mathf.Clamp(b, y + r, y + h - r))) <= r * r, c);
            public void Line(Vector2 a, Vector2 b, float width, Color c)
            {
                var d = b - a;
                Fill(Mathf.Min(a.x, b.x) - width, Mathf.Min(a.y, b.y) - width, Mathf.Max(a.x, b.x) + width, Mathf.Max(a.y, b.y) + width,
                    (x, y) => Vector2.Distance(new Vector2(x, y), a + d * Mathf.Clamp01(Vector2.Dot(new Vector2(x, y) - a, d) / d.sqrMagnitude)) <= width, c);
            }
            public void Poly(Color c, params float[] points)
            {
                Fill(0, 0, 100, 100, (x, y) =>
                {
                    var hit = false;
                    for (int i = 0, j = points.Length - 2; i < points.Length; j = i, i += 2)
                        if ((points[i + 1] > y) != (points[j + 1] > y) && x < (points[j] - points[i]) * (y - points[i + 1]) / (points[j + 1] - points[i + 1]) + points[i]) hit = !hit;
                    return hit;
                }, c);
            }
            public Texture2D Finish()
            {
                var texture = new Texture2D(_size, _size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                texture.SetPixels(_pixels); texture.Apply(); return texture;
            }
        }
    }
}
