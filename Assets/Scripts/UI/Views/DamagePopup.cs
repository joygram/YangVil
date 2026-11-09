using System.Collections;
using TMPro;
using UnityEngine;

namespace CatVillage.UI
{
    public class DamagePopup : MonoBehaviour
    {
        public TMP_Text label;
        public float rise = 120f;
        public float duration = 0.8f;

        public void Play(string text, Color color, float scale, float speed)
        {
            label.text = text;
            label.color = color;
            transform.localScale = Vector3.one * scale;
            StartCoroutine(Run(duration / speed));
        }

        IEnumerator Run(float time)
        {
            var rt = (RectTransform)transform;
            var start = rt.anchoredPosition;
            for (var t = 0f; t < 1f; t += Time.deltaTime / time)
            {
                rt.anchoredPosition = start + new Vector2(0, rise * t);
                label.alpha = 1 - t;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
