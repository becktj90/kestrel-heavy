// WebGL entry point (partial of BuildScript so the iOS-only boundary-fake dotnet tests never see WebGL APIs).
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Kestrel.Slice.Editor
{
    public static partial class BuildScript
    {
        // WebGL player for beckify.com (GameCI "WebGL build" workflow: buildMethod Kestrel.Slice.Editor.BuildScript.BuildWebGL).
        // Same shipping shell scenes as iOS. Brotli files + decompressionFallback so static hosts without
        // Content-Encoding headers (GitHub Pages) work. -khWebCompression Brotli|Gzip|Disabled overrides.
        public static void BuildWebGL()
        {
            string path = Arg("-customBuildPath") ?? Environment.GetEnvironmentVariable("KH_BUILD_PATH") ?? "build/WebGL";
            SliceBootstrap.EnsureAll();
            global::Kestrel.Game.Editor.ShellSetup.EnsureAll();
            PackGuideClips();

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
            // Fallback canvas before the template's CSS runs. The page fills the viewport (100dvh);
            // it is not a 540×1170 phone column.
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
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
    }
}
