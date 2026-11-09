// iOS export post-process for the Xcode Cloud export (DECISIONS #59, docs/XCODE_CLOUD_PLAN.md).
// Pins automatic signing for team 9TR6R5LV8M. Game Center capability removed for 1.0 (DECISIONS #98, Trevor 2026-11-09).
// Ensures Unity PrivacyInfo.xcprivacy declares NSPrivacyTracking false and no collected types (RELEASE_READINESS G-6).
// Compiles only while the active build target is iOS (UNITY_IOS), so non-iOS editors are unaffected.
#if UNITY_IOS
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

namespace Kestrel.Slice.Editor
{
    public sealed class IosXcodePostprocess : IPostprocessBuildWithReport
    {
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
            }
            // Do not link GameKit / com.apple.developer.game-center for 1.0. NullGameCenter stays the runtime stub.
            proj.WriteToFile(projPath);

            EnsurePrivacyManifests(root);
        }

        /// <summary>
        /// Unity emits PrivacyInfo.xcprivacy under UnityFramework with required-reason APIs only.
        /// Pin tracking off and empty collected/tracking-domain lists so the "Data Not Collected"
        /// store label is honest (RELEASE_READINESS G-6).
        /// </summary>
        internal static void EnsurePrivacyManifests(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
            foreach (var path in Directory.GetFiles(root, "PrivacyInfo.xcprivacy", SearchOption.AllDirectories))
                EnsurePrivacyManifest(path);
        }

        internal static void EnsurePrivacyManifest(string path)
        {
            var doc = File.Exists(path)
                ? XDocument.Load(path)
                : new XDocument(new XElement("plist", new XAttribute("version", "1.0"), new XElement("dict")));
            var dict = doc.Root?.Element("dict");
            if (dict == null)
            {
                doc = new XDocument(new XElement("plist", new XAttribute("version", "1.0"), new XElement("dict")));
                dict = doc.Root.Element("dict");
            }
            SetBoolean(dict, "NSPrivacyTracking", false);
            EnsureEmptyArray(dict, "NSPrivacyTrackingDomains");
            EnsureEmptyArray(dict, "NSPrivacyCollectedDataTypes");
            // Keep any NSPrivacyAccessedAPITypes Unity already wrote.
            doc.Save(path);
        }

        static void SetBoolean(XElement dict, string key, bool value)
        {
            foreach (var el in dict.Elements("key"))
            {
                if (el.Value != key) continue;
                var next = el.NextNode as XElement;
                if (next != null) next.ReplaceWith(new XElement(value ? "true" : "false"));
                else el.AddAfterSelf(new XElement(value ? "true" : "false"));
                return;
            }
            dict.Add(new XElement("key", key), new XElement(value ? "true" : "false"));
        }

        static void EnsureEmptyArray(XElement dict, string key)
        {
            foreach (var el in dict.Elements("key").ToList())
            {
                if (el.Value != key) continue;
                var next = el.NextNode as XElement;
                if (next != null && next.Name.LocalName == "array")
                {
                    next.RemoveNodes();
                    return;
                }
                if (next != null) next.ReplaceWith(new XElement("array"));
                else el.AddAfterSelf(new XElement("array"));
                return;
            }
            dict.Add(new XElement("key", key), new XElement("array"));
        }
    }
}
#endif
