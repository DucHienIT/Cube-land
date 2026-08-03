using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CubeBlaster.EditorTools
{

    public static class WebGLBuilder
    {
        const string OutputRoot = "Builds/WebGL";
        const string ReleaseDirectory = OutputRoot + "/Release";
        const string DevelopmentDirectory = OutputRoot + "/Development";
        const string FallbackScenePath = "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/Cube Blaster/Build WebGL (Release)")]
        public static void BuildRelease()
        {
            Build(ReleaseDirectory, BuildOptions.None, WebGLCompressionFormat.Brotli, decompressionFallback: true);
        }

        [MenuItem("Tools/Cube Blaster/Build WebGL (Development)")]
        public static void BuildDevelopment()
        {
            Build(DevelopmentDirectory, BuildOptions.Development, WebGLCompressionFormat.Disabled, decompressionFallback: false);
        }

        [MenuItem("Tools/Cube Blaster/Build And Run WebGL (Development)")]
        public static void BuildAndRunDevelopment()
        {
            Build(DevelopmentDirectory, BuildOptions.Development | BuildOptions.AutoRunPlayer, WebGLCompressionFormat.Disabled, decompressionFallback: false);
        }

        [MenuItem("Tools/Cube Blaster/Open WebGL Build Folder")]
        public static void OpenBuildFolder()
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputRoot));
        }

        [MenuItem("Tools/Cube Blaster/Open WebGL Build Folder", true)]
        public static bool CanOpenBuildFolder()
        {
            return Directory.Exists(OutputRoot);
        }

        static void Build(string outputDirectory, BuildOptions options, WebGLCompressionFormat compression, bool decompressionFallback)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("WebGL Build", "Exit Play mode before building.", "OK");
                return;
            }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog(
                    "WebGL Build",
                    "WebGL Build Support is not installed for editor 2022.3.62f3. Add the module via Unity Hub, then retry.",
                    "OK");
                return;
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                scenes = new[] { FallbackScenePath };

            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = decompressionFallback;
            PlayerSettings.WebGL.dataCaching = true;

            Directory.CreateDirectory(outputDirectory);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputDirectory,
                target = BuildTarget.WebGL,
                options = options,
            });

            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"WebGL build succeeded in {summary.totalTime.TotalSeconds:F0}s ({summary.totalSize / (1024f * 1024f):F1} MB) -> {Path.GetFullPath(outputDirectory)}\n{GetPayloadSummary(outputDirectory)}");
                EditorUtility.RevealInFinder(Path.Combine(Path.GetFullPath(outputDirectory), "index.html"));
            }
            else
            {
                Debug.LogError($"WebGL build {summary.result}: {summary.totalErrors} errors, {summary.totalWarnings} warnings.");
            }
        }

        static string GetPayloadSummary(string outputDirectory)
        {
            string payloadDirectory = Path.Combine(outputDirectory, "Build");
            if (!Directory.Exists(payloadDirectory))
                return "No Build payload folder found.";
            var lines = Directory.GetFiles(payloadDirectory)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.Length)
                .Select(file => $"  {file.Name}  {file.Length / (1024f * 1024f):F2} MB");
            return "Payload:\n" + string.Join("\n", lines);
        }
    }
}
