// Minimal editor boundary for exercising the real build entry point without Unity.
namespace UnityEngine {
    public static class Application { public static bool isBatchMode; }
    public static class Debug { public static void Log(object value) {} }
}
namespace UnityEditor.Build.Reporting {
    public enum BuildResult { Succeeded, Failed }
    public sealed class BuildSummary { public BuildResult result; public System.TimeSpan totalTime; }
    public sealed class BuildReport { public BuildSummary summary = new BuildSummary(); }
}
namespace UnityEditor {
    public enum BuildTarget { iOS }
    public enum BuildOptions { None }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildOptions options; }
    public sealed class EditorBuildSettingsScene { public string path; public bool enabled; public EditorBuildSettingsScene(string p, bool e) { path=p; enabled=e; } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
    public static class PlayerSettings { public static class iOS { public static string buildNumber; } }
    public static class EditorApplication { public static int ExitCode; public static void Exit(int value) { ExitCode=value; } }
    public static class BuildPipeline {
        public static int Calls; public static BuildPlayerOptions LastOptions;
        public static Build.Reporting.BuildResult Result;
        public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions options) {
            Calls++; LastOptions=options;
            return new Build.Reporting.BuildReport { summary = new Build.Reporting.BuildSummary { result=Result } };
        }
    }
}
namespace Kestrel.Slice.Editor { public static class SliceBootstrap { public static void EnsureAll() {} } }
namespace Kestrel.Game.Editor { public static class ShellSetup { public static void EnsureAll() {} } }
