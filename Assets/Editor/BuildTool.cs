using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CatVillage.EditorTools
{
    public static class BuildTool
    {
        const string BuildRoot = "Builds";

        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, Path.Combine(BuildRoot, "Windows", "CatVillage.exe"));
        }

        public static void BuildWebGL()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            Build(BuildTarget.WebGL, Path.Combine(BuildRoot, "WebGL"));
        }

        public static void BuildAll()
        {
            BuildWindows();
            BuildWebGL();
        }

        static void Build(BuildTarget target, string outputPath)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("BuildTool: Build Settings에 활성 씬이 없습니다.");
                EditorApplication.Exit(1);
                return;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            });

            Debug.Log($"BuildTool: {target} → {report.summary.result}, {report.summary.totalSize} bytes, {outputPath}");
            if (report.summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
