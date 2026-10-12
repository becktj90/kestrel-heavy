// Batch-mode entry points (used by GameCI unity-builder via buildMethod, or locally:
//   Unity -batchmode -quit -projectPath unity -executeMethod Kestrel.Slice.Editor.BuildScript.BuildIOS
// Exports an Xcode project to build/iOS (or $KH_BUILD_PATH / GameCI's -customBuildPath).
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Kestrel.Slice.Editor
{
    public static class BuildScript
    {
        public static void BuildIOS()
        {
            SliceBootstrap.EnsureAll();
            string path = Arg("-customBuildPath") ?? Environment.GetEnvironmentVariable("KH_BUILD_PATH") ?? "build/iOS";
            var buildNumber = Arg("-khBuildNumber") ?? Environment.GetEnvironmentVariable("KH_BUILD_NUMBER");
            if (!string.IsNullOrEmpty(buildNumber)) PlayerSettings.iOS.buildNumber = buildNumber;
            Directory.CreateDirectory(path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { SliceBootstrap.ScenePath },
                locationPathName = path,
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            });
            Debug.Log($"[Kestrel] iOS export {report.summary.result} in {report.summary.totalTime} -> {path}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
