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
            AssetDatabase.SaveAssets();
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
            // Constant physical size in points: 1 panel px = 1 iOS pt on every device, so 44 pt touch
            // targets and 8 pt margins from VISUAL_SPEC map 1:1 to USS values. Big screens get more room
            // (themed fill + wider layouts), never a blown-up phone UI.
            ps.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            ps.referenceDpi = 160;     // Unity iOS reports dpi ~ 163 x scale; 160 dpi ≈ 1 pt
            ps.fallbackDpi = 160;
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
                var stub = new GameObject(SceneFlow.SceneNames[i]).AddComponent<ScreenStub>();
                stub.Kind = (AppScreen)i;
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
