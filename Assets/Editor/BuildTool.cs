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
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.initialMemorySize = WebGLInitialMemoryMB;
            PlayerSettings.WebGL.maximumMemorySize = WebGLMaximumMemoryMB;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            EditorUserBuildSettings.SetPlatformSettings("WebGL", "CodeOptimization", "diskSizeLTO");
            var output = Path.Combine(BuildRoot, "WebGL");
            if (!Build(BuildTargetGroup.WebGL, BuildTarget.WebGL, output)) return;
            var headers = Path.Combine(output, "_headers");
            if (File.Exists(headers)) File.Delete(headers);
            CheckAssetLimit(output);
        }

        const long WorkerAssetLimitBytes = 25L * 1024 * 1024;

        static void CheckAssetLimit(string output)
        {
            foreach (var file in Directory.GetFiles(output, "*", SearchOption.AllDirectories))
            {
                var size = new FileInfo(file).Length;
                if (size > WorkerAssetLimitBytes) Fail($"{file} {size / 1048576}MB → Cloudflare 정적 에셋 25MiB 제한 초과");
            }
        }

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
