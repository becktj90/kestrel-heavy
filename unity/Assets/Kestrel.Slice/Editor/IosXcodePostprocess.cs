// iOS export post-process for the Xcode Cloud export (DECISIONS #59, docs/XCODE_CLOUD_PLAN.md).
// Pins automatic signing for team 9TR6R5LV8M. Game Center out of 1.0 (DECISIONS #98, Trevor 2026-11-09):
// strip GameKit.framework and com.apple.developer.game-center if Unity re-adds them; write a clean entitlements file.
// Compiles only while the active build target is iOS (UNITY_IOS), so non-iOS editors are unaffected.
#if UNITY_IOS
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

namespace Kestrel.Slice.Editor
{
    public sealed class IosXcodePostprocess : IPostprocessBuildWithReport
    {
        public const string EntitlementsRelPath = "Unity-iPhone/KestrelHeavy.entitlements";

        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            string root = report.summary.outputPath;
            // Exempt encryption declaration so processed Xcode Cloud builds are not held as MISSING_EXPORT_COMPLIANCE.
            string plistPath = Path.Combine(root, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);

            string projPath = PBXProject.GetPBXProjectPath(root);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            string app = proj.GetUnityMainTargetGuid();
            foreach (var t in new[] { app, proj.GetUnityFrameworkTargetGuid(), proj.ProjectGuid() })
            {
                proj.SetBuildProperty(t, "CODE_SIGN_STYLE", "Automatic");
                proj.SetBuildProperty(t, "DEVELOPMENT_TEAM", PlayerSettings.iOS.appleDeveloperTeamID);
                // DECISIONS #98: drop GameKit even if Unity's Social/GameCenter module weakly re-adds it.
                if (proj.ContainsFramework(t, "GameKit.framework"))
                    proj.RemoveFrameworkFromProject(t, "GameKit.framework");
            }
            proj.WriteToFile(projPath);

            StripGameKitFromPbxprojText(projPath);
            WriteCleanEntitlements(root);
            StripGameCenterEntitlementKeys(root);
        }

        /// <summary>
        /// Belt-and-suspenders text pass: PBXProject APIs differ across Unity minors; also drop any
        /// GameKit.framework file refs a naive grep would flag. Does NOT touch UnityEngine.GameCenterModule*
        /// il2cpp sources (engine Social stubs; not a GameKit link).
        /// </summary>
        internal static void StripGameKitFromPbxprojText(string projPath)
        {
            if (!File.Exists(projPath)) return;
            var text = File.ReadAllText(projPath);
            var cleaned = Regex.Replace(
                text,
                @"^[^\n]*GameKit\.framework[^\n]*\n",
                "",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (cleaned != text) File.WriteAllText(projPath, cleaned);
        }

        /// <summary>Empty entitlements file with no Game Center capability (DECISIONS #98).</summary>
        internal static void WriteCleanEntitlements(string root)
        {
            var path = Path.Combine(root, EntitlementsRelPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                "<plist version=\"1.0\">\n<dict>\n</dict>\n</plist>\n");
        }

        internal static void StripGameCenterEntitlementKeys(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
            foreach (var path in Directory.GetFiles(root, "*.entitlements", SearchOption.AllDirectories))
                StripGameCenterKey(path);
        }

        internal static void StripGameCenterKey(string path)
        {
            if (!File.Exists(path)) return;
            var doc = XDocument.Load(path);
            var dict = doc.Root?.Element("dict");
            if (dict == null) return;
            bool changed = false;
            foreach (var key in dict.Elements("key").ToList())
            {
                if (key.Value != "com.apple.developer.game-center") continue;
                var next = key.NextNode as XElement;
                key.Remove();
                next?.Remove();
                changed = true;
            }
            if (changed) doc.Save(path);
        }
    }
}
#endif
