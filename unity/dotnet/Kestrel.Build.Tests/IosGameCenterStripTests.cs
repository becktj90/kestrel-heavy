using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Kestrel.Slice.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

public class IosGameCenterStripTests
{
    [Test]
    public void ExportStripsGameKitFrameworkAndGameCenterEntitlement()
    {
        var root = Path.Combine(Path.GetTempPath(), "kh-gc-strip-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "Unity-iPhone.xcodeproj"));
        try
        {
            File.WriteAllText(Path.Combine(root, "Info.plist"),
                "<plist version=\"1.0\"><dict><key>CFBundleVersion</key><string>217</string></dict></plist>");
            var pbx = Path.Combine(root, "Unity-iPhone.xcodeproj/project.pbxproj");
            File.WriteAllText(pbx,
                "/* Begin PBXBuildFile */\n" +
                "\t\tAA /* GameKit.framework in Frameworks */ = {isa = PBXBuildFile; fileRef = BB /* GameKit.framework */; };\n" +
                "\t\tCC /* UnityEngine.GameCenterModule.cpp */ = {isa = PBXBuildFile; fileRef = DD /* UnityEngine.GameCenterModule.cpp */; };\n" +
                "/* End PBXBuildFile */\n");
            var dirtyEnt = Path.Combine(root, "Unity-iPhone/KestrelHeavy.entitlements");
            Directory.CreateDirectory(Path.GetDirectoryName(dirtyEnt));
            File.WriteAllText(dirtyEnt,
                "<plist version=\"1.0\"><dict>" +
                "<key>com.apple.developer.game-center</key><true/>" +
                "</dict></plist>");

            PBXProject.RemoveFrameworkCalls = 0;
            new IosXcodePostprocess().OnPostprocessBuild(new BuildReport
            {
                summary = new BuildSummary { platform = BuildTarget.iOS, outputPath = root }
            });

            Assert.That(PBXProject.RemoveFrameworkCalls, Is.GreaterThan(0));
            var pbxText = File.ReadAllText(pbx);
            Assert.That(pbxText, Does.Not.Contain("GameKit.framework"));
            // Engine Social stub may remain; it is not a GameKit link.
            Assert.That(pbxText, Does.Contain("UnityEngine.GameCenterModule.cpp"));

            var ent = File.ReadAllText(Path.Combine(root, IosXcodePostprocess.EntitlementsRelPath.Replace('/', Path.DirectorySeparatorChar)));
            Assert.That(ent, Does.Not.Contain("com.apple.developer.game-center"));
            Assert.That(XDocument.Parse(ent).Root.Element("dict").Elements().Any(), Is.False);
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public void PreciseVerifyIgnoresUnityEngineGameCenterModule()
    {
        const string pbx =
            "AA /* UnityEngine.GameCenterModule.cpp */ = {isa = PBXFileReference; path = Il2CppOutputProject/Source/il2cppOutput/UnityEngine.GameCenterModule.cpp; };\n";
        Assert.That(HasRealGameKitHit(pbx), Is.False);
        Assert.That(HasRealGameKitHit("BB /* GameKit.framework in Frameworks */ = {isa = PBXBuildFile; };\n"), Is.True);
        Assert.That(HasRealGameCenterEntitlement("<key>com.apple.developer.game-center</key><true/>"), Is.True);
        Assert.That(HasRealGameCenterEntitlement("<dict></dict>"), Is.False);
    }

    public static bool HasRealGameKitHit(string pbxOrEntText)
    {
        foreach (var line in pbxOrEntText.Split('\n'))
        {
            if (line.Contains("UnityEngine.GameCenterModule")) continue;
            if (line.Contains("GameCenterModule")) continue;
            if (Regex.IsMatch(line, @"GameKit\.framework") || Regex.IsMatch(line, @"\bGameKit\b"))
                return true;
        }
        return false;
    }

    public static bool HasRealGameCenterEntitlement(string text) =>
        text.Contains("com.apple.developer.game-center");
}
