using CatVillage.Core;
using UnityEngine;

namespace CatVillage.UI
{
    public abstract class GameScreen : MonoBehaviour
    {
        protected ScreenManager Screens;
        protected Game Game => Screens.Game;
        protected RectTransform Root => (RectTransform)transform;

        public void Init(ScreenManager screens)
        {
            Screens = screens;
            Build();
        }

        protected abstract void Build();
    }
}
