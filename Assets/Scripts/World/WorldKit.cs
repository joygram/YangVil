using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatVillage.World
{
    public static class WorldKit
    {
        static Sprite _square;

        public static Sprite Square
        {
            get
            {
                if (_square != null) return _square;
                var t = new Texture2D(4, 4) { filterMode = FilterMode.Point };
                var px = new Color[16];
                for (var i = 0; i < px.Length; i++) px[i] = Color.white;
                t.SetPixels(px);
                t.Apply();
                _square = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                return _square;
            }
        }

        public static SpriteRenderer Box(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Square;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        public static TextMeshPro Label(Transform parent, TMP_FontAsset font, string text, Vector2 position, float size, Color color, int order)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var t = go.AddComponent<TextMeshPro>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(6, 1);
            t.sortingOrder = order;
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(40, 28, 20, 255);
            return t;
        }

        public static Clickable Clickable(GameObject go, Vector2 size, Action onClick)
        {
            var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
            col.size = size;
            var c = go.GetComponent<Clickable>() ?? go.AddComponent<Clickable>();
            c.OnClick = onClick;
            return c;
        }
    }

    public class Clickable : MonoBehaviour, IPointerClickHandler
    {
        public Action OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            OnClick?.Invoke();
        }
    }
}
