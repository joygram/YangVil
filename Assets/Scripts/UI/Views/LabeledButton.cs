using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YangVil.UI
{
    public class LabeledButton : MonoBehaviour
    {
        public Button button;
        public TMP_Text label;
        public Image background;
        public Sprite normalSprite;
        public Sprite highlightSprite;

        public string Text
        {
            get => label.text;
            set => label.text = value;
        }

        public bool Interactable
        {
            get => button.interactable;
            set => button.interactable = value;
        }

        public void OnClick(Action action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }

        public void Highlight(bool on)
        {
            if (background != null && highlightSprite != null)
                background.sprite = on ? highlightSprite : normalSprite;
        }
    }
}
