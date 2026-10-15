// Batch-mode entry points (used by GameCI unity-builder via buildMethod, or locally:
//   Unity -batchmode -quit -projectPath unity -executeMethod Kestrel.Slice.Editor.BuildScript.BuildIOS
// Exports an Xcode project to build/iOS (or $KH_BUILD_PATH / GameCI's -customBuildPath).
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Kestrel.Slice.Editor
{
    public static class BuildScript
    {
        public static void BuildIOS()
        {
            string path = Arg("-customBuildPath") ?? Environment.GetEnvironmentVariable("KH_BUILD_PATH") ?? "build/iOS";
            var buildNumber = Arg("-khBuildNumber") ?? Environment.GetEnvironmentVariable("KH_BUILD_NUMBER");
            if (!string.IsNullOrEmpty(buildNumber))
            {
                if (!int.TryParse(buildNumber, out var number) || number < 100)
                    throw new ArgumentException("Shipping iOS build number must be an integer >= 100.");
                PlayerSettings.iOS.buildNumber = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            SliceBootstrap.EnsureAll();
            global::Kestrel.Game.Editor.ShellSetup.EnsureAll();
            Directory.CreateDirectory(path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = BuildScenes(),
                locationPathName = path,
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            });
            Debug.Log($"[Kestrel] iOS export {report.summary.result} in {report.summary.totalTime} -> {path}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        // WebGL player for beckify.com (GameCI "WebGL build" workflow: buildMethod Kestrel.Slice.Editor.BuildScript.BuildWebGL).
        // Same shipping shell scenes as iOS. Brotli files + decompressionFallback so static hosts without
        // Content-Encoding headers (GitHub Pages) work. -khWebCompression Brotli|Gzip|Disabled overrides.
        public static void BuildWebGL()
        {
            string path = Arg("-customBuildPath") ?? Environment.GetEnvironmentVariable("KH_BUILD_PATH") ?? "build/WebGL";
            SliceBootstrap.EnsureAll();
            global::Kestrel.Game.Editor.ShellSetup.EnsureAll();

            PlayerSettings.WebGL.template = "PROJECT:Kestrel";
            switch ((Arg("-khWebCompression") ?? "Brotli").ToLowerInvariant())
            {
                case "gzip": PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip; break;
                case "disabled": PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; break;
                default: PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli; break;
            }
            PlayerSettings.WebGL.decompressionFallback = PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false; // no SharedArrayBuffer headers on static hosts
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 1024; // iOS Safari tab limit
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 1170;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

            Directory.CreateDirectory(path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = BuildScenes(),
                locationPathName = path,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            Debug.Log($"[Kestrel] WebGL build {report.summary.result} in {report.summary.totalTime}, {report.summary.totalSize / (1024 * 1024)} MB -> {path}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        // Export the shipping shell; never silently substitute the local Slice sandbox.
        static string[] BuildScenes()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) list.Add(s.path);
            var required = new[] { "Boot", "Home", "Design", "Flight" };
            foreach (var name in required)
            {
                var path = $"Assets/Scenes/Shell/{name}.unity";
                if (!list.Contains(path) || !File.Exists(path))
                    throw new InvalidOperationException($"Shipping iOS build requires enabled scene {path}.");
            }
            if (list[0] != "Assets/Scenes/Shell/Boot.unity")
                throw new InvalidOperationException("Shipping iOS build must start with the Boot scene.");
            return list.ToArray();
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
