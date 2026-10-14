// iOS export post-process for the Xcode Cloud export (DECISIONS #59, docs/XCODE_CLOUD_PLAN.md).
// Adds the Game Center capability (the only capability) and pins automatic signing for team 9TR6R5LV8M.
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
            // The game uses only exempt OS HTTPS (Game Center). Without this declaration,
            // processed Xcode Cloud builds stay unavailable as MISSING_EXPORT_COMPLIANCE.
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
            proj.WriteToFile(projPath);

            var caps = new ProjectCapabilityManager(projPath, "Unity-iPhone/KestrelHeavy.entitlements", null, app);
            caps.AddGameCenter();
            caps.WriteToFile();
        }
    }
}
#endif
