using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YangVil.EditorTools
{
    public static class BuildTool
    {
        const string BuildRoot = "Builds";
        const int WebGLInitialMemoryMB = 64;
        const int WebGLMaximumMemoryMB = 2048;

        public static void BuildWindows()
        {
            Build(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, Path.Combine(BuildRoot, "Windows", "YangVil.exe"));
        }

        public static void BuildWebGL() => TryBuildWebGL();

        public static bool TryBuildWebGL()
        {
            PlayerSettings.WebGL.template = "PROJECT:YangVil";
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
            var (version, date) = StampBuildInfo();
            if (!Build(BuildTargetGroup.WebGL, BuildTarget.WebGL, output)) return false;
            var headers = Path.Combine(output, "_headers");
            if (File.Exists(headers)) File.Delete(headers);
            var sha = System.Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "";
            File.WriteAllText(Path.Combine(output, "version.json"),
                $"{{\"version\":\"{version}\",\"builtAt\":\"{date}\",\"commit\":\"{(sha.Length >= 7 ? sha.Substring(0, 7) : sha)}\"}}");
            Debug.Log($"BuildTool: 버전 {version} ({date})");
            return CheckAssetLimit(output);
        }

        const string BuildInfoPath = "Assets/Resources/BuildInfo.txt";
        const string LocalBuildCounter = "Library/BuildNumber.txt";

        static (string version, string date) StampBuildInfo()
        {
            var majorMinor = File.Exists("VERSION") ? File.ReadAllText("VERSION").Trim() : "0.1";
            var run = System.Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
            int patch;
            if (!int.TryParse(run, out patch))
            {
                int.TryParse(File.Exists(LocalBuildCounter) ? File.ReadAllText(LocalBuildCounter).Trim() : "0", out patch);
                patch++;
                File.WriteAllText(LocalBuildCounter, patch.ToString());
            }
            var version = $"{majorMinor}.{patch}";
            var date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            PlayerSettings.bundleVersion = version;
            Directory.CreateDirectory(Path.GetDirectoryName(BuildInfoPath));
            File.WriteAllText(BuildInfoPath, $"{version}\n{date}\n");
            AssetDatabase.ImportAsset(BuildInfoPath);
            return (version, date);
        }

        const long WorkerAssetLimitBytes = 25L * 1024 * 1024;

        static bool CheckAssetLimit(string output)
        {
            foreach (var file in Directory.GetFiles(output, "*", SearchOption.AllDirectories))
            {
                var size = new FileInfo(file).Length;
                if (size <= WorkerAssetLimitBytes) continue;
                Fail($"{file} {size / 1048576}MB → Cloudflare 정적 에셋 25MiB 제한 초과");
                return false;
            }
            return true;
        }

        public static void BuildAll()
        {
            BuildWindows();
            BuildWebGL();
        }

        static bool Build(BuildTargetGroup group, BuildTarget target, string outputPath)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                Fail("컴파일 오류가 있어 빌드를 중단합니다. error CS 를 먼저 고치세요.");
                return false;
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Fail("Build Settings에 활성 씬이 없습니다.");
                return false;
            }

            if (EditorUserBuildSettings.activeBuildTarget != target)
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

            if (!AddressablesSetup.BuildContent(System.Environment.GetCommandLineArgs().Contains("-cleanBuild")))
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
