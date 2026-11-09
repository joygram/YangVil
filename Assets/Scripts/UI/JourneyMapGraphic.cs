using UnityEngine;
using UnityEngine.UI;
using YangVil.World;

namespace YangVil.UI
{
    // Map geometry uses the same normalized route as the interactive stage markers.
    [RequireComponent(typeof(CanvasRenderer))]
    public class JourneyMapGraphic : MaskableGraphic
    {
        public int stageCount = 6;
        public int unlockedCount = 1;
        public static float MapHeight(int count) => Mathf.Max(1900, 560 + Mathf.Max(0, count - 1) * 270);
        public static Vector2 RoutePoint(int index, int count)
        {
            var x = Mathf.Sin(index * 1.8f - 0.7f) * 0.26f;
            return new Vector2(x, (280 + index * 270) / MapHeight(count));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Vector2 At(float x, float y) => new Vector2(r.center.x + x * r.width, r.yMin + y * r.height);
            var grass = VillageIllustration.Grass;
            Quad(vh, r, grass);
            Oval(vh, At(-0.42f, 0.17f), new Vector2(r.width * 0.42f, 360), VillageIllustration.Hex(0xcbd9a3));
            Oval(vh, At(0.43f, 0.39f), new Vector2(r.width * 0.42f, 440), VillageIllustration.Hex(0xc3d29a));
            Oval(vh, At(-0.3f, 0.77f), new Vector2(r.width * 0.59f, 420), VillageIllustration.Hex(0xa3bf8d));
            // Creek across the landscape, behind the footbridge and route.
            for (var layer = 0; layer < 2; layer++)
            for (var i = 0; i < 36; i++)
            {
                var x = -0.56f + i * 0.033f;
                var y = 0.48f + Mathf.Sin(x * 7) * 0.045f;
                if (layer == 0) Oval(vh, At(x, y), new Vector2(44, 57), VillageIllustration.Hex(0xe1d8b0));
                else Oval(vh, At(x, y + 0.004f), new Vector2(34, 43), VillageIllustration.Hex(0x91bdb7));
            }
            for (var i = 0; i < 85; i++)
            {
                var x = Mathf.Sin(i * 23.41f) * 0.47f;
                var y = Mathf.Repeat(i * 0.137f, 0.91f) + 0.04f;
                var c = i % 3 == 0 ? VillageIllustration.Cream : VillageIllustration.Hex(0xd5dfa9);
                Oval(vh, At(x, y), new Vector2(4, 3), c);
                if (i % 4 == 0) Oval(vh, At(x + 0.012f, y + 0.006f), new Vector2(3, 3), c);
            }
            var previous = At(0, 0.06f);
            for (var i = 0; i < stageCount; i++)
            {
                var point = RoutePoint(i, stageCount);
                var next = At(point.x, point.y);
                var open = i < unlockedCount;
                for (var layer = 0; layer < 2; layer++)
                for (var j = 0; j < 28; j++)
                {
                    var t = j / 27f;
                    var smooth = t * t * (3 - 2 * t);
                    var center = new Vector2(Mathf.Lerp(previous.x, next.x, smooth), Mathf.Lerp(previous.y, next.y, t));
                    if (layer == 0) Oval(vh, center + Vector2.down * 5, new Vector2(31, 27), VillageIllustration.Hex(0xa7b381));
                    else
                    {
                        Oval(vh, center, new Vector2(27, 24), open ? VillageIllustration.Hex(0xf3e2bb) : VillageIllustration.Hex(0xb9bf9a));
                        if (!open && j % 5 == 0) Oval(vh, center, new Vector2(5, 5), VillageIllustration.Hex(0x8e9b7c));
                    }
                }
                previous = next;
            }
            var crossingY = 0.48f;
            var crossingX = 0f;
            for (var pass = 0; pass < 6; pass++)
            {
                for (var i = 1; i < stageCount; i++)
                {
                    var a = RoutePoint(i - 1, stageCount);
                    var b = RoutePoint(i, stageCount);
                    if (crossingY < a.y || crossingY > b.y) continue;
                    var t = Mathf.InverseLerp(a.y, b.y, crossingY);
                    crossingX = Mathf.Lerp(a.x, b.x, t * t * (3 - 2 * t));
                }
                crossingY = 0.48f + Mathf.Sin(crossingX * 7) * 0.045f;
            }
            var bridge = At(crossingX, crossingY);
            for (var i = 0; i < 7; i++)
                Quad(vh, new Rect(bridge.x - 43, bridge.y - 45 + i * 14, 86, 10), VillageIllustration.Hex(0xbf986d));
            Quad(vh, new Rect(bridge.x - 50, bridge.y - 52, 7, 105), VillageIllustration.Hex(0x967957));
            Quad(vh, new Rect(bridge.x + 43, bridge.y - 52, 7, 105), VillageIllustration.Hex(0x967957));
        }

        static void Quad(VertexHelper vh, Rect rect, Color color)
        {
            var n = vh.currentVertCount;
            vh.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        static void Oval(VertexHelper vh, Vector2 center, Vector2 radius, Color color)
        {
            const int sides = 20;
            var n = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (var i = 0; i <= sides; i++)
            {
                var a = i * Mathf.PI * 2 / sides;
                vh.AddVert(center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y), color, Vector2.zero);
                if (i > 0) vh.AddTriangle(n, n + i, n + i + 1);
            }
        }
    }
}
