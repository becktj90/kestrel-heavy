// Minimal editor boundary for exercising the real build entry point without Unity.
namespace UnityEngine {
    public static class Application { public static bool isBatchMode; }
    public static class Debug { public static void Log(object value) {} }
}
namespace UnityEditor.Build.Reporting {
    public enum BuildResult { Succeeded, Failed }
    public sealed class BuildSummary { public BuildResult result; public System.TimeSpan totalTime; public UnityEditor.BuildTarget platform; public string outputPath; }
    public sealed class BuildReport { public BuildSummary summary = new BuildSummary(); }
}
namespace UnityEditor {
    public enum BuildTarget { iOS, Standalone }
    public enum BuildOptions { None }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildOptions options; }
    public sealed class EditorBuildSettingsScene { public string path; public bool enabled; public EditorBuildSettingsScene(string p, bool e) { path=p; enabled=e; } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
    public static class PlayerSettings { public static class iOS { public static string buildNumber; public static string appleDeveloperTeamID; } }
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

namespace UnityEditor.Build {
    public interface IPostprocessBuildWithReport { int callbackOrder { get; } void OnPostprocessBuild(Reporting.BuildReport report); }
}
namespace UnityEditor.iOS.Xcode {
    public sealed class PBXProject {
        public static string GetPBXProjectPath(string root) => System.IO.Path.Combine(root, "Unity-iPhone.xcodeproj/project.pbxproj");
        public void ReadFromFile(string path) {} public void WriteToFile(string path) {}
        public string GetUnityMainTargetGuid() => "app"; public string GetUnityFrameworkTargetGuid() => "framework";
        public string ProjectGuid() => "project"; public void SetBuildProperty(string target, string key, string value) {}
    }
    public sealed class ProjectCapabilityManager {
        public ProjectCapabilityManager(string path, string entitlements, string name, string target) {}
        public void AddGameCenter() {} public void WriteToFile() {}
    }
    public sealed class PlistDocument {
        System.Xml.Linq.XDocument document;
        public PlistElementDict root;
        public void ReadFromFile(string path) {
            document=System.Xml.Linq.XDocument.Load(path);
            root=new PlistElementDict(document.Root.Element("dict"));
        }
        public void WriteToFile(string path) => document.Save(path);
    }
    public sealed class PlistElementDict {
        readonly System.Xml.Linq.XElement element;
        public PlistElementDict(System.Xml.Linq.XElement value) { element=value; }
        public void SetBoolean(string name, bool value) {
            foreach (var key in element.Elements("key")) {
                if (key.Value!=name) continue;
                ((System.Xml.Linq.XElement)key.NextNode).ReplaceWith(new System.Xml.Linq.XElement(value ? "true" : "false"));
                return;
            }
            element.Add(new System.Xml.Linq.XElement("key",name), new System.Xml.Linq.XElement(value ? "true" : "false"));
        }
    }
}
