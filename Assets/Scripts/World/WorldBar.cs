using UnityEngine;

namespace YangVil.World
{
    public class WorldBar : MonoBehaviour
    {
        SpriteRenderer _fill;
        float _width;

        public static WorldBar Create(Transform parent, Vector2 position, Vector2 size, Color color, int order)
        {
            var root = new GameObject("Bar");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            WorldKit.Box(root.transform, "Back", Vector2.zero, size + new Vector2(0.06f, 0.06f), new Color(0.1f, 0.08f, 0.06f, 0.85f), order);
            var bar = root.AddComponent<WorldBar>();
            bar._width = size.x;
            bar._fill = WorldKit.Box(root.transform, "Fill", Vector2.zero, size, color, order + 1);
            return bar;
        }

        public void Set(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            var t = _fill.transform;
            t.localScale = new Vector3(_width * ratio, t.localScale.y, 1);
            t.localPosition = new Vector3(-_width * (1 - ratio) / 2f, 0, 0);
        }
    }
}
