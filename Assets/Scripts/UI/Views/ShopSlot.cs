using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YangVil.UI
{
    public class ShopSlot : MonoBehaviour
    {
        public Image icon;
        public TMP_Text iconLetter;
        public TMP_Text title;
        public TMP_Text detail;
        public TMP_Text owned;
        public LabeledButton buy;

        public void SetPlaceholder(string name, Color color)
        {
            icon.sprite = null;
            icon.color = color;
            iconLetter.text = string.IsNullOrEmpty(name) ? "" : name.Substring(0, 1);
            iconLetter.gameObject.SetActive(true);
        }

        public void SetIcon(Sprite sprite)
        {
            if (this == null || sprite == null) return;
            icon.sprite = sprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            iconLetter.gameObject.SetActive(false);
        }
    }
}
