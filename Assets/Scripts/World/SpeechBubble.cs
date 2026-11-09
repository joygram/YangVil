using TMPro;
using UnityEngine;

namespace YangVil.World
{
    [RequireComponent(typeof(TextMeshPro))]
    public class SpeechBubble : MonoBehaviour
    {
        const int BodyPx = 32;
        const int Radius = 10;
        const int Line = 2;
        const int TailPx = 16;
        const float PixelsPerUnit = 100f;
        static readonly Color Ink = new Color(0.17f, 0.12f, 0.09f);
        static readonly Color Paper = Color.white;
        static readonly Vector2 Padding = new Vector2(0.32f, 0.22f);

        static Sprite _body;
        static Sprite _tail;

        TextMeshPro _text;
        SpriteRenderer _bodyRenderer;
        SpriteRenderer _tailRenderer;
        bool _on;
        string _shown;

        void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _bodyRenderer = Part("BubbleBody", BodySprite, _text.sortingOrder - 2);
            _bodyRenderer.drawMode = SpriteDrawMode.Sliced;
            _tailRenderer = Part("BubbleTail", TailSprite, _text.sortingOrder - 1);
            Show(false);
        }

        SpriteRenderer Part(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            return r;
        }

        public void Show(bool on)
        {
            _on = on;
            _shown = null;
            _text.outlineWidth = on ? 0f : 0.2f;
            if (on) _text.color = Ink;
            _bodyRenderer.enabled = _tailRenderer.enabled = false;
        }

        void LateUpdate()
        {
            if (!_on || _shown == _text.text) return;
            _shown = _text.text;
            var visible = !string.IsNullOrEmpty(_shown);
            _bodyRenderer.enabled = _tailRenderer.enabled = visible;
            if (!visible) return;
            var size = (Vector2)_text.GetPreferredValues(_shown) + Padding;
            _bodyRenderer.size = size;
            _bodyRenderer.transform.localPosition = new Vector3(0, 0, 0.01f);
            var tail = TailPx / PixelsPerUnit;
            _tailRenderer.transform.localPosition = new Vector3(-size.x * 0.18f, -size.y / 2f - tail / 2f + Line / PixelsPerUnit, 0.005f);
        }

        static Sprite BodySprite => _body != null ? _body : _body = MakeBody();

        static Sprite TailSprite => _tail != null ? _tail : _tail = MakeTail();

        static Sprite MakeBody()
        {
            var t = NewTexture(BodyPx, BodyPx);
            for (var y = 0; y < BodyPx; y++)
            for (var x = 0; x < BodyPx; x++)
            {
                var d = EdgeDistance(x, y);
                t.SetPixel(x, y, d < 0 ? Color.clear : d < Line ? Ink : Paper);
            }
            t.Apply();
            var border = Radius + Line;
            return Sprite.Create(t, new Rect(0, 0, BodyPx, BodyPx), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static float EdgeDistance(int x, int y)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            var core = new Vector2(Mathf.Clamp(p.x, Radius, BodyPx - Radius), Mathf.Clamp(p.y, Radius, BodyPx - Radius));
            return Radius - Vector2.Distance(p, core);
        }

        static Sprite MakeTail()
        {
            var t = NewTexture(TailPx, TailPx);
            for (var y = 0; y < TailPx; y++)
            {
                var half = (y + 1) * 0.5f;
                for (var x = 0; x < TailPx; x++)
                {
                    var left = x + 0.5f;
                    var inside = left <= half * 1.1f;
                    var edge = left > half * 1.1f - Line || x < Line;
                    var top = y >= TailPx - Line;
                    t.SetPixel(x, y, !inside ? Color.clear : edge && !top ? Ink : Paper);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, TailPx, TailPx), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        static Texture2D NewTexture(int w, int h) => new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }
}
