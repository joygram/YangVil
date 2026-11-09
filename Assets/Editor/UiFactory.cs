using YangVil.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace YangVil.EditorTools
{
    public class UiFactory
    {
        const string UiRoot = "Assets/Art/ThirdParty/KenneyPixelUI/9-Slice";
        public const float PixelScale = 3f;

        public readonly TMP_FontAsset Font;
        public readonly Sprite Panel = Load("Ancient/tan.png");
        public readonly Sprite Inlay = Load("Ancient/tan_inlay.png");
        public readonly Sprite Blue = Load("Colored/blue.png");
        public readonly Sprite BluePressed = Load("Colored/blue_pressed.png");
        public readonly Sprite Green = Load("Colored/green.png");
        public readonly Sprite GreenPressed = Load("Colored/green_pressed.png");
        public readonly Sprite Red = Load("Colored/red.png");
        public readonly Sprite Yellow = Load("Colored/yellow.png");
        public readonly Sprite Grey = Load("Colored/grey.png");

        public UiFactory(TMP_FontAsset font) => Font = font;

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiRoot}/{path}");

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public Image Sliced(Transform parent, string name, Sprite sprite, Color? color = null)
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / PixelScale;
            img.color = color ?? Color.white;
            return img;
        }

        public static Image Solid(Transform parent, string name, Color color)
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public TMP_Text Text(Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = Node("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public LabeledButton Button(Transform parent, string name, string label, Sprite normal, Sprite pressed, Sprite highlight = null, float fontSize = 34)
        {
            var img = Sliced(parent, name, normal);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = pressed, disabledSprite = Grey };
            var text = Text(img.transform, label, fontSize, Color.white);
            Stretch(text.rectTransform, 10, 10, 10, 10);
            var lb = img.gameObject.AddComponent<LabeledButton>();
            lb.button = button;
            lb.label = text;
            lb.background = img;
            lb.normalSprite = normal;
            lb.highlightSprite = highlight;
            return lb;
        }

        public LabeledButton BlueButton(Transform parent, string name, string label, float fontSize = 34) =>
            Button(parent, name, label, Blue, BluePressed, Green, fontSize);

        public LabeledButton GreenButton(Transform parent, string name, string label, float fontSize = 34) =>
            Button(parent, name, label, Green, GreenPressed, Yellow, fontSize);

        public static HorizontalLayoutGroup Row(RectTransform rt, float spacing)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            return h;
        }

        public static void Height(Component c, float h)
        {
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = h;
        }

        public RectTransform ScrollList(Transform parent, string name)
        {
            var frame = Sliced(parent, name, Inlay);
            frame.gameObject.AddComponent<RectMask2D>();
            var scroll = frame.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var content = Node("Content", frame.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.padding = new RectOffset(16, 16, 16, 16);
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return content;
        }

        public static UnitSprite Unit(Transform parent, float size)
        {
            var rt = Node("Unit", parent);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return rt.gameObject.AddComponent<UnitSprite>();
        }

        public static Image Background(Transform parent, Color color) => Stretch(Solid(parent, "Background", color).rectTransform).GetComponent<Image>();

        public Bar Bar(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var bg = Solid(parent, name, new Color(0, 0, 0, 0.45f));
            Place(bg.rectTransform, new Vector2(0.5f, 0f), pos, size);
            var fill = Solid(bg.transform, "Fill", color);
            Stretch(fill.rectTransform);
            var bar = bg.gameObject.AddComponent<Bar>();
            bar.fill = fill.rectTransform;
            return bar;
        }
    }
}
