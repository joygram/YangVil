using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Godot Node2D·Sprite2D·_Draw 대응 (UGUI 위).
// 좌표 = Godot 그대로: 오른쪽 +x, 아래 +y, 회전 라디안 시계 방향 +. 부모 원점 기준.
namespace YangVillage.Ui
{
    public class Node2 : MonoBehaviour
    {
        Vector2 _position, _scale = Vector2.one;
        float _rotation;
        public int ZIndex;

        public RectTransform Rect => (RectTransform)transform;

        public Vector2 Position { get => _position; set { _position = value; Rect.anchoredPosition = new Vector2(value.x, -value.y); } }
        public float Rotation { get => _rotation; set { _rotation = value; transform.localRotation = Quaternion.Euler(0, 0, -value * Mathf.Rad2Deg); } }
        public Vector2 Scale { get => _scale; set { _scale = value; transform.localScale = new Vector3(value.x, value.y, 1); } }
        public bool Visible { get => gameObject.activeSelf; set => gameObject.SetActive(value); }

        public static T Make<T>(Transform parent, string name = null) where T : Node2
        {
            var go = new GameObject(name ?? typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = Vector2.zero;
            return go.AddComponent<T>();
        }

        // Godot 원점(왼쪽 위) 컨테이너 안에 놓을 때: 부모 rect 의 왼쪽 위에 붙인다.
        public void AnchorTopLeft()
        {
            Rect.anchorMin = Rect.anchorMax = new Vector2(0, 1);
            Position = _position;
        }

        public void Free() { if (this != null) Destroy(gameObject); }

        // Godot z_index: 형제 사이에서 z 순, 같으면 만든 순.
        protected virtual void LateUpdate()
        {
            var n = transform.childCount;
            if (n < 2) return;
            var kids = new List<(Transform t, int z, int i)>(n);
            for (var i = 0; i < n; i++)
            {
                var t = transform.GetChild(i);
                kids.Add((t, t.TryGetComponent<Node2>(out var node) ? node.ZIndex : 0, i));
            }
            var sorted = kids.OrderBy(k => k.z).ThenBy(k => k.i).ToList();
            for (var i = 0; i < sorted.Count; i++)
                if (sorted[i].t.GetSiblingIndex() != i) sorted[i].t.SetSiblingIndex(i);
        }

        // 자식 Label (Godot Control 위치 = 왼쪽 위, 크기). 회전·확대 축은 가운데.
        public TextMeshProUGUI AddLabel(string text, int size, Color color, Vector2 position, Vector2 box)
        {
            var label = Kit.Label(transform, text, size, color);
            Kit.PlaceCentered(label.rectTransform, position, box);
            return label;
        }
    }

    // 그림 한 장. Offset = 그림 가운데의 위치(노드 기준).
    public class Sprite2 : Node2
    {
        static Shader _boost;
        Image _image;
        Vector2 _offset;
        bool _flipH, _flipV;
        Color _modulate = Color.white;
        Material _material;

        public Image Image => _image;

        public static Sprite2 Of(Transform parent, Sprite sprite)
        {
            var s = Make<Sprite2>(parent, "sprite");
            var img = Kit.Node("img", s.transform);
            img.anchorMin = img.anchorMax = img.pivot = new Vector2(0.5f, 0.5f);
            s._image = img.gameObject.AddComponent<Image>();
            s._image.raycastTarget = false;
            s.Texture = sprite;
            return s;
        }

        public Sprite Texture
        {
            get => _image.sprite;
            set { _image.sprite = value; _image.rectTransform.sizeDelta = value.rect.size; }
        }

        public float Width => _image.sprite.rect.width;
        public float Height => _image.sprite.rect.height;

        public Vector2 Offset { get => _offset; set { _offset = value; _image.rectTransform.anchoredPosition = new Vector2(value.x, -value.y); } }
        public bool FlipH { get => _flipH; set { _flipH = value; Flip(); } }
        public bool FlipV { get => _flipV; set { _flipV = value; Flip(); } }

        void Flip() => _image.rectTransform.localScale = new Vector3(_flipH ? -1 : 1, _flipV ? -1 : 1, 1);

        // Godot modulate: 1 넘는 밝기(번쩍)는 셰이더 배율로.
        public Color Modulate
        {
            get => _modulate;
            set
            {
                _modulate = value;
                var over = value.r > 1 || value.g > 1 || value.b > 1;
                if (over)
                {
                    if (_material == null)
                    {
                        _boost ??= Resources.Load<Shader>("YangVillage/UiBoost");
                        _material = new Material(_boost);
                    }
                    _material.SetColor("_Boost", new Color(value.r, value.g, value.b, 1));
                    _image.material = _material;
                    _image.color = new Color(1, 1, 1, Mathf.Clamp01(value.a));
                }
                else
                {
                    _image.material = null;
                    _image.color = value;
                }
            }
        }

        public float Alpha { get => _modulate.a; set { var c = _modulate; c.a = value; Modulate = c; } }

        void OnDestroy() { if (_material != null) Destroy(_material); }
    }

    // Godot _Draw: 자식이 Draw 를 채우고 Redraw() 로 다시 그림.
    public abstract class Paint : MaskableGraphic
    {
        VertexHelper _vh;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;
        }

        public void Redraw() => SetVerticesDirty();

        protected abstract void Draw();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            _vh = vh;
            Draw();
            _vh = null;
        }

        static Vector3 P(Vector2 p) => new Vector3(p.x, -p.y);

        void Tri(Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
        {
            var i = _vh.currentVertCount;
            _vh.AddVert(P(a), ca, Vector2.zero);
            _vh.AddVert(P(b), cb, Vector2.zero);
            _vh.AddVert(P(c), cc, Vector2.zero);
            _vh.AddTriangle(i, i + 1, i + 2);
        }

        protected void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            Tri(a, b, c, color, color, color);
            Tri(a, c, d, color, color, color);
        }

        protected void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            var dir = b - a;
            if (dir.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-dir.y, dir.x).normalized * width / 2;
            Quad(a + n, b + n, b - n, a - n, color);
        }

        protected void DrawRect(Rect r, Color color) =>
            Quad(r.position, new Vector2(r.xMax, r.y), r.max, new Vector2(r.x, r.yMax), color);

        // 볼록·별 모양 다각형 (가운데에서 부채꼴).
        protected void DrawPolygon(IReadOnlyList<Vector2> points, IReadOnlyList<Color> colors)
        {
            var center = Vector2.zero;
            var mid = Color.clear;
            for (var i = 0; i < points.Count; i++) { center += points[i]; mid += colors[i]; }
            center /= points.Count;
            mid /= points.Count;
            for (var i = 0; i < points.Count; i++)
            {
                var j = (i + 1) % points.Count;
                Tri(center, points[i], points[j], mid, colors[i], colors[j]);
            }
        }

        protected void DrawPolygon(IReadOnlyList<Vector2> points, Color color) =>
            DrawPolygon(points, Enumerable.Repeat(color, points.Count).ToList());

        protected void DrawEllipse(Vector2 c, float rx, float ry, Color color, int segments = 32)
        {
            for (var i = 0; i < segments; i++)
            {
                var a0 = i * Mathf.PI * 2 / segments;
                var a1 = (i + 1) * Mathf.PI * 2 / segments;
                Tri(c, c + new Vector2(Mathf.Cos(a0) * rx, Mathf.Sin(a0) * ry), c + new Vector2(Mathf.Cos(a1) * rx, Mathf.Sin(a1) * ry), color, color, color);
            }
        }

        protected void DrawCircle(Vector2 c, float r, Color color) => DrawEllipse(c, r, r, color);

        protected void DrawArc(Vector2 c, float r, float from, float to, int segments, Color color, float width)
        {
            for (var i = 0; i < segments; i++)
            {
                var a0 = Mathf.Lerp(from, to, i / (float)segments);
                var a1 = Mathf.Lerp(from, to, (i + 1) / (float)segments);
                var d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                var d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                Quad(c + d0 * (r + width / 2), c + d1 * (r + width / 2), c + d1 * (r - width / 2), c + d0 * (r - width / 2), color);
            }
        }

        // 두 줄(바깥·안쪽)을 잇는 띠 — 오목한 모양(초승달) 용.
        protected void DrawStrip(IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> inner, Color color)
        {
            for (var i = 0; i + 1 < outer.Count; i++) Quad(outer[i], outer[i + 1], inner[i + 1], inner[i], color);
        }
    }

    // Paint 를 Node2 처럼 놓기.
    public class PaintNode : Node2
    {
        public static T Of<T>(Transform parent) where T : Paint
        {
            var node = Make<PaintNode>(parent, typeof(T).Name);
            return node.gameObject.AddComponent<T>();
        }
    }
}
