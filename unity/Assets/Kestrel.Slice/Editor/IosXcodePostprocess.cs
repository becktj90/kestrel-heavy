// iOS export post-process for the Xcode Cloud export (DECISIONS #59, docs/XCODE_CLOUD_PLAN.md).
// Pins automatic signing for team 9TR6R5LV8M. Game Center capability removed for 1.0 (DECISIONS #98, Trevor 2026-11-09).
// Compiles only while the active build target is iOS (UNITY_IOS), so non-iOS editors are unaffected.
#if UNITY_IOS
using System.IO;
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
        }
    }
}
#endif
