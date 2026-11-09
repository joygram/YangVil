using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CatVillage.UI
{
    public class UnitSlot : MonoBehaviour
    {
        public UnitSprite sprite;
        public TMP_Text nameLabel;
        public Bar hp;
        public Bar gauge;
        public GameObject focusMarker;
        public Button button;
        public RectTransform popupAnchor;
    }
}
