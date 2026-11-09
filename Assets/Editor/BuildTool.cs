using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CatVillage.EditorTools
{
    public static class BuildTool
    {
        const string BuildRoot = "Builds";
        const int WebGLInitialMemoryMB = 64;
        const int WebGLMaximumMemoryMB = 2048;

        public static void BuildWindows()
        {
            Build(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, Path.Combine(BuildRoot, "Windows", "CatVillage.exe"));
        }

        public static void BuildWebGL()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.initialMemorySize = WebGLInitialMemoryMB;
            PlayerSettings.WebGL.maximumMemorySize = WebGLMaximumMemoryMB;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            var output = Path.Combine(BuildRoot, "WebGL");
            if (Build(BuildTargetGroup.WebGL, BuildTarget.WebGL, output))
                File.WriteAllText(Path.Combine(output, "_headers"), WebGLHeaders);
        }

        const string WebGLHeaders =
            "/Build/*.js.br\n  Content-Type: application/javascript\n  Content-Encoding: br\n" +
            "/Build/*.wasm.br\n  Content-Type: application/wasm\n  Content-Encoding: br\n" +
            "/Build/*.data.br\n  Content-Type: application/octet-stream\n  Content-Encoding: br\n";

        public static void BuildAll()
        {
            BuildWindows();
            BuildWebGL();
        }

        static bool Build(BuildTargetGroup group, BuildTarget target, string outputPath)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Fail("Build Settings에 활성 씬이 없습니다.");
                return false;
            }

            if (EditorUserBuildSettings.activeBuildTarget != target)
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

            if (!AddressablesSetup.BuildContent())
            {
                Fail("Addressables 빌드 실패");
                return false;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            });

            Debug.Log($"BuildTool: {target} → {report.summary.result}, {report.summary.totalSize} bytes, {outputPath}");
            if (report.summary.result == BuildResult.Succeeded) return true;
            Fail("플레이어 빌드 실패");
            return false;
        }

        static void Fail(string message)
        {
            Debug.LogError($"BuildTool: {message}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
