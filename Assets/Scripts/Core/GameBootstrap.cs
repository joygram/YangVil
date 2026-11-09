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
        public SceneFlow sceneFlow;

        public static Game Game { get; private set; }

        public static bool Ready => Game != null && SceneFlow.Instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartFromBoot()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        void Start()
        {
            if (config == null) config = GameConfig.Load();
            Ui.Config = config;
            Game = new Game(config);
            screens.Init(Game, screenLayer);
            sceneFlow.Init(config, screens);
            screens.Show<IntroScreen>();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Game?.Commit();
        }

        void OnApplicationQuit() => Game?.Commit();
    }
}
