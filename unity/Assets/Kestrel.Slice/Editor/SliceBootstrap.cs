// Code-first project setup so the prototype needs zero hand edits in the editor.
// Runs on every domain reload (idempotent) and before builds; also under menu Kestrel/*.
//  - copies ../data/*.json + economy constants into Assets/Resources/KestrelData (TextAssets)
//  - creates a URP pipeline asset, the body/plume materials and UI Toolkit PanelSettings
//  - generates Assets/Scenes/Slice.unity with a SliceApp object and registers it in Build Settings
//  - enforces portrait-only iOS PlayerSettings with the DEV bundle id (never the shipping one)
using System.IO;
using Kestrel.Core.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Material = UnityEngine.Material;

namespace Kestrel.Slice.Editor
{
    [InitializeOnLoad]
    public static class SliceBootstrap
    {
        public const string DevBundleId = "com.beckify.kestrelheavy.unityproto"; // legacy proto id, no longer stamped
        public const string ShipBundleId = "com.beckify.kestrelheavy"; // Trevor 2026-10-12 12:27 PM (DECISIONS #59)
        public const string ShipVersion = "2.0.0";
        public const string TeamId = "9TR6R5LV8M";
        public const string ScenePath = "Assets/Scenes/Slice.unity";
        static readonly string[] DataFiles = { "propellants", "engines", "materials", "payloads", "presets" };

        static SliceBootstrap()
        {
            // Shell Design screen = the real designer (design panel, pad, vehicle, framing). ShellSetup builds the scene.
            global::Kestrel.Game.Editor.ShellSetup.PopulateScreen = screen =>
            {
                if (screen != global::Kestrel.Game.Shell.AppScreen.Design) return false;
                var go = new GameObject("Design");
                go.AddComponent<ShellDesignHost>();
                var app = go.AddComponent<SliceApp>();
                app.PanelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/UI/KH_PanelSettings.asset");
                return true;
            };
EditorApplication.delayCall += () => { if (!Application.isBatchMode) EnsureAll(); };
        }

        [MenuItem("Kestrel/Setup Project (idempotent)")]
        public static void EnsureAll()
        {
            SyncData();
            EnsurePlayerSettings();
            EnsurePipelineAndMaterials();
            EnsurePanelSettings();
            EnsureScene();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Kestrel/Sync Data")]
        public static void SyncData()
        {
            string repo = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            string dst = "Assets/Resources/KestrelData";
            Directory.CreateDirectory(dst);
            foreach (var f in DataFiles) CopyIfChanged(Path.Combine(repo, "data", f + ".json"), Path.Combine(dst, f + ".json"));
            CopyIfChanged(Path.Combine(repo, "web", "js", "economy-data", "constants.json"), Path.Combine(dst, "constants.json"));
            AssetDatabase.Refresh();
        }

        static void CopyIfChanged(string src, string dst)
        {
            var text = File.ReadAllText(src);
            if (!File.Exists(dst) || File.ReadAllText(dst) != text) File.WriteAllText(dst, text);
        }

        public static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = "Beckify";
            PlayerSettings.productName = "Kestrel Heavy";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ShipBundleId);
            PlayerSettings.bundleVersion = ShipVersion;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.statusBarHidden = true;
        }

        static void EnsurePipelineAndMaterials()
        {
            Directory.CreateDirectory("Assets/Settings");
            Directory.CreateDirectory("Assets/Resources/Materials");
            const string rpPath = "Assets/Settings/KH_URP.asset";
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
            if (rp == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, "Assets/Settings/KH_URP_Renderer.asset");
                rp = UniversalRenderPipelineAsset.Create(rendererData);
                rp.msaaSampleCount = 4; rp.supportsHDR = true;
                AssetDatabase.CreateAsset(rp, rpPath);
            }
            if (GraphicsSettings.defaultRenderPipeline != rp) GraphicsSettings.defaultRenderPipeline = rp;
            // Per-quality-level pipeline assets are owned by U2's QualitySetup (data/quality.json);
            // don't overwrite them here. The default pipeline above is the fallback.
            EnsureMaterial("Assets/Resources/Materials/KH_Body.mat", "Universal Render Pipeline/Lit", m => { m.SetFloat("_Smoothness", 0.55f); m.SetFloat("_Metallic", 0.1f); });
            EnsureMaterial("Assets/Resources/Materials/KH_Plume.mat", "Universal Render Pipeline/Particles/Unlit", m =>
            {
                // Additive transparent particles.
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 2);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.One); m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
            });
        }

        static void EnsureMaterial(string path, string shader, System.Action<Material> setup)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError($"[Kestrel] shader not found: {shader} (is URP installed?)"); return; }
            var m = new Material(sh); setup(m);
            AssetDatabase.CreateAsset(m, path);
        }

        static void EnsurePanelSettings()
        {
            const string path = "Assets/Resources/UI/KH_PanelSettings.asset";
            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(path) != null) return;
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1179, 2556); // iPhone 15/16 Pro portrait
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0f; // match width (portrait)
            AssetDatabase.CreateAsset(ps, path);
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("SliceApp");
                var app = go.AddComponent<SliceApp>();
                app.PanelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/UI/KH_PanelSettings.asset");
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            // Slice.unity is a local, editor-generated sandbox (gitignored). Nothing loads it by name: the shell
            // Design scene hosts SliceApp via ShellDesignHost. Keep it OUT of Build Settings so a clean checkout
            // (cloud build) never references an untracked scene; drop any stale entry left by older bootstraps.
            var scenes = EditorBuildSettings.scenes;
            if (System.Array.Exists(scenes, s => s.path == ScenePath))
                EditorBuildSettings.scenes = System.Array.FindAll(scenes, s => s.path != ScenePath);
        }
    }

    /// <summary>Re-sync data and settings before every player build (local or CI).</summary>
    public sealed class SlicePreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => SliceBootstrap.EnsureAll();
    }
}
