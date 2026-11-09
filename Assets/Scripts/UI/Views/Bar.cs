using UnityEngine;

namespace YangVil.UI
{
    public class Bar : MonoBehaviour
    {
        public RectTransform fill;

        public void Set(float ratio)
        {
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }
    }
}
