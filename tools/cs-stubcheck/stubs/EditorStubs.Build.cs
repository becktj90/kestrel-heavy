// Stub UnityEditor build, scene, asset and PlayerSettings APIs used by the repo's Editor assemblies (compile-only).
using System;
using UnityEngine;
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)] public sealed class MenuItemAttribute : Attribute { public MenuItemAttribute(string p) { } public MenuItemAttribute(string p, bool v) { } public MenuItemAttribute(string p, bool v, int prio) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class InitializeOnLoadAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class InitializeOnLoadMethodAttribute : Attribute { }
    public enum BuildTarget { iOS, StandaloneOSX, Android, WebGL }
    public enum BuildTargetGroup { iOS, Standalone, Android, WebGL }
    [Flags] public enum BuildOptions { None = 0, Development = 1 }
    public enum UIOrientation { Portrait, PortraitUpsideDown, LandscapeRight, LandscapeLeft, AutoRotation }
    public enum ScriptingImplementation { Mono2x, IL2CPP }
    public enum iOSTargetDevice { iPhoneOnly, iPadOnly, iPhoneAndiPad }
    public enum IconKind { Any, Application, Settings, Notification, Spotlight, Store }
    public enum WebGLCompressionFormat { Brotli, Gzip, Disabled }
    public enum WebGLExceptionSupport { None, ExplicitlyThrownExceptionsOnly, FullWithStacktrace, FullWithoutStacktrace }
    public enum ManagedStrippingLevel { Disabled, Minimal, Low, Medium, High }
    public enum Il2CppCodeGeneration { OptimizeSpeed, OptimizeSize }
    public enum ImportAssetOptions { Default, ForceUpdate, ForceSynchronousImport }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildTargetGroup targetGroup; public BuildOptions options; }
    public static class BuildPipeline { public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o) => null; }
    public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; } public string path; public bool enabled; }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } }
    public static class EditorApplication { public delegate void CallbackFunction(); public static CallbackFunction delayCall; public static CallbackFunction update; public static void Exit(int code) { } public static bool isPlaying => false; public static bool isCompiling => false; }
    public static class EditorUtility { public static void SetDirty(UnityEngine.Object o) { } public static bool DisplayDialog(string t, string m, string ok, string cancel = "") => true; }
    public static class EditorPrefs { public static int GetInt(string k, int d = 0) => d; public static void SetInt(string k, int v) { } public static bool GetBool(string k, bool d = false) => d; public static void SetBool(string k, bool v) { } public static string GetString(string k, string d = "") => d; public static void SetString(string k, string v) { } }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string p) where T : UnityEngine.Object => null;
        public static UnityEngine.Object LoadAssetAtPath(string p, Type t) => null;
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string p) => null;
        public static void CreateAsset(UnityEngine.Object o, string p) { }
        public static void SaveAssets() { } public static void Refresh() { }
        public static void ImportAsset(string p, ImportAssetOptions o = ImportAssetOptions.Default) { }
        public static bool DeleteAsset(string p) => true; public static string CreateFolder(string parent, string name) => ""; public static bool IsValidFolder(string p) => true;
        public static string GetAssetPath(UnityEngine.Object o) => ""; public static string[] FindAssets(string filter, string[] folders = null) => new string[0]; public static string GUIDToAssetPath(string g) => "";
    }
    public class AssetImporter : UnityEngine.Object { public static AssetImporter GetAtPath(string p) => null; public void SaveAndReimport() { } }
    public enum TextureImporterType { Default, NormalMap, Sprite }
    public enum TextureImporterNPOTScale { None, ToNearest, ToLarger, ToSmaller }
    public enum TextureImporterCompression { Uncompressed, Compressed, CompressedHQ, CompressedLQ }
    public enum TextureImporterFormat { Automatic, RGBA32, ASTC_6x6 }
    public class TextureImporterPlatformSettings { public string name; public bool overridden; public TextureImporterFormat format; public int maxTextureSize; public TextureImporterCompression textureCompression; }
    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; } public TextureImporterNPOTScale npotScale { get; set; } public TextureImporterCompression textureCompression { get; set; }
        public bool mipmapEnabled { get; set; } public bool alphaIsTransparency { get; set; } public bool isReadable { get; set; } public bool sRGBTexture { get; set; } public int maxTextureSize { get; set; }
        public TextureImporterPlatformSettings GetPlatformTextureSettings(string platform) => new TextureImporterPlatformSettings(); public void SetPlatformTextureSettings(TextureImporterPlatformSettings s) { }
    }
    public class PlatformIconKind { }
    public class PlatformIcon { public int width => 0; public int height => 0; public void SetTexture(Texture2D t, int layer = 0) { } public Texture2D GetTexture(int layer = 0) => null; }
    public static class PlayerSettings
    {
        public static string companyName { get; set; } public static string productName { get; set; } public static string bundleVersion { get; set; }
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; } public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; } public static bool allowedAutorotateToLandscapeRight { get; set; }
        public static bool useAnimatedAutorotation { get; set; } public static bool statusBarHidden { get; set; }
        public static void SetApplicationIdentifier(Build.NamedBuildTarget t, string id) { } public static string GetApplicationIdentifier(Build.NamedBuildTarget t) => "";
        public static void SetScriptingBackend(Build.NamedBuildTarget t, ScriptingImplementation s) { }
        public static void SetIcons(Build.NamedBuildTarget t, Texture2D[] icons, IconKind k) { }
        public static PlatformIcon[] GetPlatformIcons(Build.NamedBuildTarget t, PlatformIconKind k) => new PlatformIcon[0];
        public static void SetPlatformIcons(Build.NamedBuildTarget t, PlatformIconKind k, PlatformIcon[] icons) { }
        public static bool stripEngineCode { get; set; }
        public static int defaultWebScreenWidth { get; set; }
        public static int defaultWebScreenHeight { get; set; }
        public static void SetManagedStrippingLevel(Build.NamedBuildTarget t, ManagedStrippingLevel level) { }
        public static void SetIl2CppCodeGeneration(Build.NamedBuildTarget t, Il2CppCodeGeneration generation) { }
        public static void SetGraphicsAPIs(BuildTarget target, UnityEngine.Rendering.GraphicsDeviceType[] apis) { }
        public static class WebGL
        {
            public static string template { get; set; }
            public static WebGLCompressionFormat compressionFormat { get; set; }
            public static bool decompressionFallback { get; set; }
            public static WebGLExceptionSupport exceptionSupport { get; set; }
            public static bool nameFilesAsHashes { get; set; }
            public static bool dataCaching { get; set; }
            public static bool threadsSupport { get; set; }
            public static int initialMemorySize { get; set; }
            public static int maximumMemorySize { get; set; }
        }
        public static class iOS
        {
            public static string buildNumber { get; set; } public static string appleDeveloperTeamID { get; set; } public static bool appleEnableAutomaticSigning { get; set; }
            public static string targetOSVersionString { get; set; } public static iOSTargetDevice targetDevice { get; set; } public static bool requiresFullScreen { get; set; }
        }
    }
}
namespace UnityEditor.Build
{
    public struct NamedBuildTarget { public static readonly NamedBuildTarget iOS = default; public static readonly NamedBuildTarget Standalone = default; public static readonly NamedBuildTarget WebGL = default; public static NamedBuildTarget FromBuildTargetGroup(BuildTargetGroup g) => default; }
    public interface IOrderedCallback { int callbackOrder { get; } }
    public interface IPreprocessBuildWithReport : IOrderedCallback { void OnPreprocessBuild(Reporting.BuildReport report); }
    public interface IPostprocessBuildWithReport : IOrderedCallback { void OnPostprocessBuild(Reporting.BuildReport report); }
}
namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
    public struct BuildSummary { public TimeSpan totalTime; public BuildResult result; public string outputPath; public BuildTarget platform; public ulong totalSize; public int totalErrors; public int totalWarnings; }
    public class BuildReport : UnityEngine.Object { public BuildSummary summary => default; }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading }
    public static class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m = NewSceneMode.Single) => default;
        public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string path = "", bool copy = false) => true;
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
        public static UnityEngine.SceneManagement.Scene OpenScene(string p, OpenSceneMode m = OpenSceneMode.Single) => default;
        public static bool MarkSceneDirty(UnityEngine.SceneManagement.Scene s) => true;
    }
}
namespace UnityEditor.iOS { public class iOSPlatformIconKind : PlatformIconKind { public static readonly iOSPlatformIconKind Application = new iOSPlatformIconKind(), Spotlight = new iOSPlatformIconKind(), Settings = new iOSPlatformIconKind(), Notification = new iOSPlatformIconKind(), Marketing = new iOSPlatformIconKind(); } }
namespace UnityEditor.iOS.Xcode
{
    public class PBXProject
    {
        public static string GetPBXProjectPath(string root) => ""; public void ReadFromFile(string p) { } public void WriteToFile(string p) { } public void ReadFromString(string s) { } public string WriteToString() => "";
        public string GetUnityMainTargetGuid() => ""; public string GetUnityFrameworkTargetGuid() => ""; public string ProjectGuid() => "";
        public void SetBuildProperty(string target, string name, string value) { } public void AddBuildProperty(string target, string name, string value) { } public string GetBuildPropertyForAnyConfig(string target, string name) => "";
    }
}
namespace UnityEditor
{
    public class SerializedProperty
    {
        public int arraySize { get; set; } public int intValue { get; set; } public bool boolValue { get; set; } public float floatValue { get; set; } public string stringValue { get; set; }
        public UnityEngine.Object objectReferenceValue { get; set; } public string name => "";
        public SerializedProperty GetArrayElementAtIndex(int i) => this; public SerializedProperty FindPropertyRelative(string n) => this;
        public void InsertArrayElementAtIndex(int i) { } public void DeleteArrayElementAtIndex(int i) { } public bool Next(bool enter) => false; public SerializedProperty Copy() => this;
    }
    public class SerializedObject { public SerializedObject(UnityEngine.Object o) { } public SerializedProperty FindProperty(string n) => new SerializedProperty(); public bool ApplyModifiedPropertiesWithoutUndo() => true; public bool ApplyModifiedProperties() => true; public void Update() { } }
    public class EditorWindow : ScriptableObject { public static EditorWindow GetWindow(Type t, bool utility = false, string title = null, bool focus = true) => null; public static T GetWindow<T>() where T : EditorWindow => null; public void Repaint() { } public void Show() { } public void Close() { } public void Focus() { } public Rect position { get; set; } public Vector2 minSize { get; set; } public Vector2 maxSize { get; set; } }
    public class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject { public static T instance => null; }
}
