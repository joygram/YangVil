using YangVil.Data;
using TMPro;
using UnityEngine;

namespace YangVil.World
{
    [RequireComponent(typeof(TextMeshPro))]
    public class SpeechBubble : MonoBehaviour
    {
        static SpeechArt _art;
        static Sprite _body;
        static Sprite _tail;

        public static void UseArt(SpeechArt art, Sprite body, Sprite tail)
        {
            _art = art;
            if (body != null) _body = body;
            if (tail != null) _tail = tail;
        }

        public static void ClearArt()
        {
            _body = null;
            _tail = null;
        }

        TextMeshPro _text;
        SpriteRenderer _bodyRenderer;
        SpriteRenderer _tailRenderer;
        bool _on;
        string _shown;

        void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _bodyRenderer = Part("BubbleBody", _text.sortingOrder - 2);
            _bodyRenderer.drawMode = SpriteDrawMode.Sliced;
            _tailRenderer = Part("BubbleTail", _text.sortingOrder - 1);
            Show(false);
        }

        SpriteRenderer Part(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sortingOrder = order;
            return r;
        }

        public void Show(bool on)
        {
            _on = on;
            _shown = null;
            _text.outlineWidth = on && _art != null ? 0f : 0.2f;
            if (on && _art != null) _text.color = _art.ink;
            _bodyRenderer.enabled = _tailRenderer.enabled = false;
        }

        void LateUpdate()
        {
            if (!_on || _body == null || _tail == null) return;
            if (_shown == _text.text && _bodyRenderer.sprite == _body) return;
            _shown = _text.text;
            _bodyRenderer.sprite = _body;
            _tailRenderer.sprite = _tail;
            _text.outlineWidth = 0f;
            _text.color = _art.ink;
            var visible = !string.IsNullOrEmpty(_shown);
            _bodyRenderer.enabled = _tailRenderer.enabled = visible;
            if (!visible) return;
            var size = (Vector2)_text.GetPreferredValues(_shown) + _art.padding;
            _bodyRenderer.size = size;
            _bodyRenderer.transform.localPosition = new Vector3(0, 0, 0.01f);
            _tailRenderer.transform.localPosition = new Vector3(-size.x * 0.18f, -size.y / 2f + _art.tailOverlap, 0.005f);
        }
    }
}
