using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Godot YangVillage Kit.cs 이식. Godot 컨테이너 → UGUI 레이아웃 그룹, StyleBoxFlat → 절차 생성 9-slice.
namespace YangVillage.Ui
{
    public static class Kit
    {
        public static readonly Color Ink = Hex("4a3728");
        public static readonly Color Paper = Hex("fff6e4");
        public static readonly Color Cream = Hex("ffe9c4");
        public static readonly Color Accent = Hex("ff9f6b");
        public static readonly Color AccentDark = Hex("d9713f");
        public static readonly Color Sky = Hex("bfe6f5");
        public static readonly Color Grass = Hex("a9d98a");
        public static readonly Color Night = Hex("2c2a4a");
        public static readonly Color Good = Hex("6fbf5f");
        public static readonly Color Bad = Hex("e2574c");
        public static readonly Color Muted = Hex("9a8b7c");
        public static readonly Color Gold = Hex("ffd935");

        const int Border = 3;
        public static TMP_FontAsset Font;

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;

        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static Color Lightened(Color c, float amount) => Color.Lerp(c, Color.white, amount);

        // 둥근 테두리 상자 (채움·테두리·반지름). 같은 조합은 재사용.
        static readonly Dictionary<(Color, Color, int), Sprite> Boxes = new Dictionary<(Color, Color, int), Sprite>();

        public static Sprite BoxSprite(Color fill, Color border, int radius)
        {
            var key = (fill, border, radius);
            if (Boxes.TryGetValue(key, out var s)) return s;
            var size = radius * 2 + 2;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                var cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                var d = radius - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                var edge = Mathf.Min(x + 0.5f, y + 0.5f, size - x - 0.5f, size - y - 0.5f);
                var inside = radius > 0 ? Mathf.Clamp01(d + 0.5f) : 1f;
                var depth = radius > 0 && (x < radius || x >= size - radius) && (y < radius || y >= size - radius) ? d : edge;
                var c = depth < Border ? border : fill;
                c.a *= inside;
                px[y * size + x] = c;
            }
            t.SetPixels(px);
            t.Apply();
            var b = radius + 1;
            return Boxes[key] = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        public static Image Rect(Transform parent, Color color, bool block = false)
        {
            var r = Node("rect", parent);
            Fill(r);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = block;
            return img;
        }

        // 테두리 상자 패널 + 안쪽 여백.
        public static Image Panel(Transform parent, Color fill, Color border, int radius = 16, int pad = 14)
        {
            var r = Node("panel", parent);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = BoxSprite(fill, border, radius);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;
            return img;
        }

        static T Group<T>(RectTransform r, int gap, int pad) where T : HorizontalOrVerticalLayoutGroup
        {
            var g = r.gameObject.AddComponent<T>();
            g.spacing = gap;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = g.childForceExpandHeight = false;
            return g;
        }

        public static VerticalLayoutGroup VBox(RectTransform r, int gap = 10, int pad = 0)
        {
            var g = Group<VerticalLayoutGroup>(r, gap, pad);
            g.childForceExpandWidth = true;
            g.childAlignment = TextAnchor.UpperCenter;
            return g;
        }

        // 화면 세로 줄: 바깥 여백 margin, 칸 사이 gap.
        public static RectTransform Column(Transform parent, int margin = 16, int gap = 10)
        {
            var r = Node("column", parent);
            Fill(r);
            VBox(r, gap, margin);
            return r;
        }

        public static RectTransform Row(Transform parent, int gap = 8)
        {
            var r = Node("row", parent);
            var g = Group<HorizontalLayoutGroup>(r, gap, 0);
            g.childAlignment = TextAnchor.MiddleCenter;
            return r;
        }

        public static LayoutElement Layout(Component c)
        {
            return c.TryGetComponent<LayoutElement>(out var le) ? le : c.gameObject.AddComponent<LayoutElement>();
        }

        public static void Expand(Component c, float ratio = 1) => Layout(c).flexibleWidth = ratio;

        public static void Spacer(Transform parent) => Layout(Node("spacer", parent)).flexibleHeight = 1;

        public static TextMeshProUGUI Label(Transform parent, string text, int size = 20, Color? color = null)
        {
            var r = Node("label", parent);
            var label = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = color ?? Ink;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        // 버튼: 강조색 상자 + 가운데 글. 아이콘이 있으면 그림 위·글 아래.
        public static Button Button(Transform parent, string text, Action onPress, int size = 20, Color? fill = null, Color? hover = null)
        {
            var bg = Panel(parent, fill ?? Accent, Ink, 12);
            var go = bg.gameObject;
            go.name = "button";
            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.targetGraphic = bg;
            var f = fill ?? Accent;
            button.spriteState = new SpriteState
            {
                highlightedSprite = BoxSprite(hover ?? Lightened(f, 0.15f), Ink, 12),
                selectedSprite = BoxSprite(f, Ink, 12),
                pressedSprite = BoxSprite(AccentDark, Ink, 12),
                disabledSprite = BoxSprite(Cream, Muted, 12)
            };
            button.onClick.AddListener(new UnityAction(onPress));
            var g = VBox((RectTransform)go.transform, 4, 10);
            g.childAlignment = TextAnchor.MiddleCenter;
            Label(go.transform, text, size);
            Layout(bg).minHeight = 44;
            Expand(bg);
            return button;
        }

        public static TextMeshProUGUI ButtonText(Button button) => button.GetComponentInChildren<TextMeshProUGUI>();

        public static readonly Color Down = new Color(0.45f, 0.45f, 0.5f, 0.5f);

        // 화면 루트: 바탕색 채움 (Godot Kit.Root).
        public static void Root(RectTransform root, Color background) => Rect(root, background);

        // Godot PanelContainer + VBoxContainer: 종이 상자 + 안쪽 세로 줄.
        public static RectTransform Card(Transform parent, int gap = 4)
        {
            var panel = Panel(parent, Paper, Ink, 16);
            var r = (RectTransform)panel.transform;
            VBox(r, gap, 14);
            return r;
        }

        // 세로 줄 가운데 정렬 (Godot BoxContainer.AlignmentMode.Center).
        public static void CenterColumn(RectTransform column) => column.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

        // Godot Control 자리(왼쪽 위·크기) → 가운데 축으로 놓기.
        public static void PlaceCentered(RectTransform r, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            r.anchoredPosition = new Vector2(position.x + size.x / 2, -(position.y + size.y / 2));
        }

        public static void Outline(TextMeshProUGUI label, Color color, int size)
        {
            label.outlineColor = color;
            label.outlineWidth = Mathf.Clamp01(size / 24f);
        }

        public static void NoWrap(TextMeshProUGUI label) => label.textWrappingMode = TextWrappingModes.NoWrap;

        public static void SetDisabled(Button button, bool disabled)
        {
            button.interactable = !disabled;
            ButtonText(button).color = disabled ? Muted : Ink;
        }

        // 버튼 상자 색 바꾸기 (Godot AddThemeStyleboxOverride).
        public static void Restyle(Button button, Color fill, Color border, int radius = 12)
        {
            var img = (Image)button.targetGraphic;
            img.sprite = BoxSprite(fill, border, radius);
            button.transition = Selectable.Transition.None;
        }

        // Godot Modulate: 하위 그림·글 색에 곱함. 처음 색을 기억해 둔다.
        public static void Modulate(Component root, Color tint)
        {
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (!g.TryGetComponent<BaseColor>(out var b)) { b = g.gameObject.AddComponent<BaseColor>(); b.Value = g.color; }
                g.color = b.Value * tint;
            }
        }

        public class BaseColor : MonoBehaviour { public Color Value; }

        public static string Stars(int stars)
        {
            var text = "";
            for (var i = 0; i < YangVillage.Battle.BattleState.MaxStars; i++) text += App.T(i < stars ? "ui_star_on" : "ui_star_off");
            return text;
        }

        // 버튼 글 위에 그림 (Godot ExpandIcon + 위쪽 정렬).
        public static Image ButtonIcon(Button button, Sprite sprite, float size, bool above = true)
        {
            var r = Node("icon", button.transform);
            if (above) r.SetAsFirstSibling();
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var le = Layout(img);
            le.preferredWidth = le.preferredHeight = le.minHeight = size;
            return img;
        }
    }
}
