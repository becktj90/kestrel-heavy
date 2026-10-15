// Code-first U2 shell setup. Generates, idempotently:
//  - Assets/Scenes/Shell/{Boot,Home,Design,Flight}.unity (Boot holds only AppShell; screens hold a ScreenStub)
//  - Assets/Resources/UI/KH_ShellPanelSettings.asset (UI Toolkit, scale with screen, portrait reference)
//  - portrait-only orientation PlayerSettings
//  - Build Settings: Boot first, then the screens; any other scenes already listed (e.g. the PR #13
//    Slice) are kept after them.
// Deliberately NOT touched (approval gates in docs/UNITY_SHIP_PLAN.md §6): bundle id, team, signing,
// UIRequiresFullScreen (G5), Xcode project, CI.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Game.Shell;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Editor
{
    [InitializeOnLoad]
    public static class ShellSetup
    {
        public const string SceneDir = "Assets/Scenes/Shell";
        public const string PanelPath = "Assets/Resources/UI/KH_ShellPanelSettings.asset";
        public const string TokensPath = "Assets/Kestrel.Game/Shell/UI/KH_Tokens.uss";
        public const string ThemePath = "Assets/Kestrel.Game/Shell/UI/KH_Theme.tss";

        /// <summary>Optional content provider for a screen scene (called with the new scene active). Return true
        /// when it populated the screen, so no ScreenStub is added. Kestrel.Slice.Editor registers the real Design
        /// screen (SliceApp) here; this assembly cannot reference Kestrel.Slice directly.</summary>
        public static System.Func<AppScreen, bool> PopulateScreen;

        static ShellSetup() { EditorApplication.delayCall += () => { if (!Application.isBatchMode) EnsureAll(); }; }

        [MenuItem("Kestrel/Setup Shell (U2)")]
        public static void EnsureAllMenu() => EnsureAll(regenerateScenes: true);

        /// <summary>Scenes are only (re)written when missing or when regenerateScenes is set, so a domain
        /// reload never closes the scene you have open.</summary>
        public static void EnsureAll() => EnsureAll(regenerateScenes: false);

        public static void EnsureAll(bool regenerateScenes)
        {
            GameDataSync.Sync();
            QualitySetup.Ensure();
            EnsurePortrait();
            var panel = EnsurePanelSettings();
            EnsureScenes(panel, regenerateScenes);
            EnsureMenuTmpShader();
            AssetDatabase.SaveAssets();
        }

        /// <summary>A Resources material holding the TextMeshPro SDF shader, so the player build does not
        /// strip it. Shader.Find alone does not keep a shader.</summary>
        public static void EnsureMenuTmpShader()
        {
            Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (shader == null) shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader == null) return;
            const string path = "Assets/Resources/UI/KH_MenuTmp.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = "KH_MenuTmp" };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
                EditorUtility.SetDirty(mat);
            }
        }

        public static void EnsurePortrait()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;
            // iOS render-extra-frame-on-pause etc. left default. UIRequiresFullScreen: gate G5 (Trevor).
        }

        static PanelSettings EnsurePanelSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PanelPath));
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, PanelPath); }
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            // Scale With Screen Size, portrait reference = iPhone SE in points (375x667), match height.
            // 1 USS px = 1 pt on SE; Pro Max (956 pt tall) scales 1.43x and iPad 13 (1376 pt) ~2.06x, so the
            // Home and shell UI keep proportion instead of shrinking to a phone-sized island. Matching height
            // (not width) guarantees the vertical layout fits on iPad, where width-matching would overflow,
            // and keeps the 44 pt touch minimum at or above 44 pt on every device. Width in panel units:
            // SE 375, Pro Max ~308, iPad ~500, so screens must lay out in ≥ 300 units of width.
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(375, 667);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 1f;
            ps.clearColor = false;     // camera clear = themed fill
            ps.sortingOrder = 0;
            EditorUtility.SetDirty(ps);
            return ps;
        }

        static void EnsureScenes(PanelSettings panel, bool regenerate)
        {
            Directory.CreateDirectory(SceneDir);
            string boot = $"{SceneDir}/{SceneFlow.BootScene}.unity";
            var ours = new List<string> { boot };
            foreach (var n in SceneFlow.SceneNames) ours.Add($"{SceneDir}/{n}.unity");
            bool missing = ours.Any(p => !File.Exists(p));
            if (regenerate || missing)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                WriteScenes(panel, boot);
            }
            var others = EditorBuildSettings.scenes.Where(s => !ours.Contains(s.path)).ToList();
            EditorBuildSettings.scenes = ours.Select(p => new EditorBuildSettingsScene(p, true)).Concat(others).ToArray();
        }

        static void WriteScenes(PanelSettings panel, string boot)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("AppShell");
            var shell = go.AddComponent<AppShell>();
            shell.PanelSettings = panel;
            shell.Tokens = AssetDatabase.LoadAssetAtPath<StyleSheet>(TokensPath);
            EditorSceneManager.SaveScene(scene, boot);

            for (int i = 0; i < SceneFlow.SceneNames.Length; i++)
            {
                string path = $"{SceneDir}/{SceneFlow.SceneNames[i]}.unity";
                var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (PopulateScreen == null || !PopulateScreen((AppScreen)i))
                {
                    var stub = new GameObject(SceneFlow.SceneNames[i]).AddComponent<ScreenStub>();
                    stub.Kind = (AppScreen)i;
                }
                EditorSceneManager.SaveScene(s, path);
            }
        }
    }

    /// <summary>Re-run the shell setup before every player build (local or via the Unity CLI).</summary>
    public sealed class ShellPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 10;
        public void OnPreprocessBuild(BuildReport report) => ShellSetup.EnsureAll();
    }
}
