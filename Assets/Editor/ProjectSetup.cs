using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatVillage.EditorTools
{
    public static class ProjectSetup
    {
        const string BootScenePath = "Assets/Scenes/Boot.unity";
        const string RendererPath = "Assets/Settings/Renderer2D.asset";
        const string PipelinePath = "Assets/Settings/URP2D.asset";

        static readonly string[] Folders =
        {
            "Assets/Scenes", "Assets/Settings", "Assets/Scripts/Core", "Assets/Scripts/Data",
            "Assets/Scripts/Cat", "Assets/Scripts/House", "Assets/Scripts/Battle", "Assets/Scripts/UI",
            "Assets/Art/Cats", "Assets/Art/Enemies", "Assets/Art/UI", "Assets/Art/FX", "Assets/Data", "Assets/Prefabs"
        };

        public static void Run()
        {
            CreateFolders();
            SetupUrp2D();
            SetupPlayer();
            CreateBootScene();
            AssetDatabase.SaveAssets();
            Debug.Log("ProjectSetup: 완료");
        }

        static void CreateFolders()
        {
            foreach (var folder in Folders)
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, ".keep"), string.Empty);
            }
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

        static void CreateBootScene()
        {
            if (!File.Exists(BootScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var camera = Camera.main;
                camera.orthographic = true;
                camera.orthographicSize = 9.6f;
                camera.backgroundColor = new Color(0.98f, 0.94f, 0.88f);
                EditorSceneManager.SaveScene(scene, BootScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScenePath, true) };
        }
    }
}
