using CatVillage.Data;
using CatVillage.UI;
using UnityEngine;

namespace CatVillage.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        public GameConfig config;
        public ScreenManager screens;
        public RectTransform screenLayer;

        public static Game Game { get; private set; }

        void Start()
        {
            if (config == null) config = GameConfig.Load();
            Ui.Config = config;
            Game = new Game(config);
            screens.Init(Game, screenLayer);
            screens.Show<IntroScreen>();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Game?.Commit();
        }

        void OnApplicationQuit() => Game?.Commit();
    }
}
