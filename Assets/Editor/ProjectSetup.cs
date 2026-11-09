using System.IO;
using CatVillage.Core;
using CatVillage.Data;
using CatVillage.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CatVillage.EditorTools
{
    public static class ProjectSetup
    {
        const string IntroScenePath = "Assets/Scenes/Intro.unity";
        const string LegacyBootScenePath = "Assets/Scenes/Boot.unity";
        const string RendererPath = "Assets/Settings/Renderer2D.asset";
        const string PipelinePath = "Assets/Settings/URP2D.asset";

        static readonly string[] Folders =
        {
            "Assets/Scenes", "Assets/Settings", "Assets/Scripts/Core", "Assets/Scripts/Data",
            "Assets/Scripts/Battle", "Assets/Scripts/UI", "Assets/Art/Cats", "Assets/Art/Enemies",
            "Assets/Art/UI", "Assets/Art/FX", "Assets/Data", "Assets/Prefabs"
        };

        public static void Run()
        {
            CreateFolders();
            SetupUrp2D();
            SetupPlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("ProjectSetup: 완료");
        }

        [MenuItem("CatVillage/Build All Content")]
        public static void BuildContent()
        {
            GameDataBuilder.Build();
            UiPrefabBuilder.Build();
            WorldSceneBuilder.Build();
            BuildIntroScene();
            AssetDatabase.SaveAssets();
            Debug.Log("ProjectSetup: 콘텐츠 빌드 완료");
        }

        static void CreateFolders()
        {
            foreach (var folder in Folders) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        static void SetupUrp2D()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        static void SetupPlayer()
        {
            PlayerSettings.companyName = "CatVillage";
            PlayerSettings.productName = "CatVillage";
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.runInBackground = true;
        }

        [MenuItem("CatVillage/Build Intro Scene")]
        public static void BuildIntroScene()
        {
            if (File.Exists(LegacyBootScenePath)) AssetDatabase.DeleteAsset(LegacyBootScenePath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var camera = Camera.main;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.17f, 0.13f, 0.1f);
            camera.depth = -10;
            camera.cullingMask = 0;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            var layer = UiFactory.Stretch(UiFactory.Node("Screens", canvasGo.transform));
            var screens = canvasGo.AddComponent<ScreenManager>();

            var loading = UiFactory.Solid(canvasGo.transform, "Loading", new Color(0.17f, 0.13f, 0.1f, 0.85f));
            UiFactory.Stretch(loading.rectTransform);
            var spinner = UiFactory.Solid(loading.transform, "Spinner", new Color(1f, 0.85f, 0.4f));
            UiFactory.Place(spinner.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
            spinner.gameObject.AddComponent<Spinner>();
            loading.gameObject.SetActive(false);
            screens.loadingIndicator = loading.gameObject;

            var bootGo = new GameObject("GameBootstrap");
            var boot = bootGo.AddComponent<GameBootstrap>();
            boot.config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
            boot.screens = screens;
            boot.screenLayer = layer;
            boot.sceneFlow = bootGo.AddComponent<SceneFlow>();
            bootGo.AddComponent<AudioHub>();

            EditorSceneManager.SaveScene(scene, IntroScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(IntroScenePath, true) };
        }
    }
}
