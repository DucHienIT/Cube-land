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
        const string PortraitTemplate = "PROJECT:CubeBlasterPortrait";
        const string PortraitTemplatePath = "Assets/WebGLTemplates/CubeBlasterPortrait/index.html";
        const int PortraitWidth = 1080;
        const int PortraitHeight = 1920;

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

        [MenuItem("Tools/Cube Blaster/Apply Portrait Presentation")]
        public static void ApplyPortraitPresentationMenu()
        {
            ApplyPortraitPresentation();
            AssetDatabase.SaveAssets();
            Debug.Log($"[WebGLBuilder] Portrait presentation applied: WebGL template {PortraitTemplate}, " +
                      $"canvas {PortraitWidth}x{PortraitHeight}, mobile orientation Portrait.");
        }

        /// The game is portrait-only, and that takes agreement from three settings that live in
        /// three different places — which is why it is one action rather than three checkboxes,
        /// and why every build re-applies it:
        ///   - the WEB CANVAS aspect, which the template reads from defaultWebScreenWidth/Height
        ///     to size the element Unity renders into (the browser window is not portrait, and
        ///     Unity takes its render target from the canvas element, not from the window);
        ///   - the TEMPLATE itself, since the stock one stretches the canvas to the window;
        ///   - the MOBILE orientation, for a native build of the same scene.
        /// The in-game pillarbox (GameConfig.portraitLock) covers hosts that ignore all three.
        static void ApplyPortraitPresentation()
        {
            if (File.Exists(PortraitTemplatePath))
                PlayerSettings.WebGL.template = PortraitTemplate;
            else
                Debug.LogWarning($"[WebGLBuilder] {PortraitTemplatePath} is missing — the build will " +
                                 "use the currently selected template and will NOT be locked to portrait.");

            PlayerSettings.defaultWebScreenWidth = PortraitWidth;
            PlayerSettings.defaultWebScreenHeight = PortraitHeight;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
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
            ApplyPortraitPresentation();

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
