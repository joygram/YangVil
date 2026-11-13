using YangVil.Data;
using YangVil.UI;
using UnityEngine;

namespace YangVil.Core
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
            // 시작 씬 = YangVillage(Godot 이식, 빌드 0). 기존 흐름의 Village·Battle 씬(빌드 목록 밖)을 직접 열면 기존 Intro 로.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex < 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene("Intro");
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
