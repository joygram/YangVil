using TMPro;
using UnityEngine;

namespace CatVillage.UI
{
    public class Toast : MonoBehaviour
    {
        public TMP_Text label;
        public float duration = 1.6f;

        float _hideAt;

        public void Show(string message)
        {
            label.text = message;
            gameObject.SetActive(true);
            _hideAt = Time.unscaledTime + duration;
        }

        void Update()
        {
            if (Time.unscaledTime > _hideAt) gameObject.SetActive(false);
        }
    }
}
