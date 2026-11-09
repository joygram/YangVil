using UnityEngine;

namespace CatVillage.World
{
    public enum CursorKind { Default, Hand, Grab }

    public static class GameCursor
    {
        const int Size = 32;
        static Texture2D _hand;
        static Texture2D _grab;
        static CursorKind _current;

        public static void Set(CursorKind kind)
        {
            if (kind == _current) return;
            _current = kind;
            switch (kind)
            {
                case CursorKind.Hand: Cursor.SetCursor(_hand ??= Paw(false), new Vector2(Size / 2f, 6), CursorMode.Auto); break;
                case CursorKind.Grab: Cursor.SetCursor(_grab ??= Paw(true), new Vector2(Size / 2f, Size / 2f), CursorMode.Auto); break;
                default: Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); break;
            }
        }

        static Texture2D Paw(bool closed)
        {
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var clear = new Color(0, 0, 0, 0);
            var fill = new Color(1f, 0.92f, 0.8f);
            var line = new Color(0.25f, 0.17f, 0.12f);
            var px = new Color[Size * Size];
            for (var i = 0; i < px.Length; i++) px[i] = clear;

            var pads = closed
                ? new[] { (16f, 14f, 8f), (10f, 22f, 3.5f), (16f, 24f, 3.5f), (22f, 22f, 3.5f) }
                : new[] { (16f, 12f, 7f), (8f, 21f, 3.5f), (13f, 26f, 3.5f), (19f, 26f, 3.5f), (24f, 21f, 3.5f) };
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                foreach (var (cx, cy, r) in pads)
                {
                    var d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= r) px[y * Size + x] = fill;
                    else if (d <= r + 1.2f && px[y * Size + x] == clear) px[y * Size + x] = line;
                }
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
