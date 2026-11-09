using CatVillage.Core;
using UnityEngine;

namespace CatVillage.UI
{
    public abstract class GameScreen : MonoBehaviour
    {
        protected ScreenManager Screens;
        protected Game Game => Screens.Game;
        protected AssetScope Assets { get; private set; }
        protected RectTransform Root => (RectTransform)transform;

        public void Init(ScreenManager screens, AssetScope scope)
        {
            Screens = screens;
            Assets = scope;
            Build();
        }

        protected abstract void Build();

        protected void BindUnit(UnitSprite unit, Data.SpriteSet set, Color tint)
        {
            unit.Prepare(tint);
            StartCoroutine(Assets.LoadFrames(set, unit.SetFrames));
        }
    }
}
