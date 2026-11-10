using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Kestrel.Slice.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class IosPrivacyManifestTests
{
    [Test]
    public void EnsurePrivacyManifest_PinsTrackingOffAndEmptyCollectedTypes()
    {
        var root = Path.Combine(Path.GetTempPath(), "kh-privacy-" + Guid.NewGuid());
        var framework = Path.Combine(root, "UnityFramework");
        Directory.CreateDirectory(framework);
        Directory.CreateDirectory(Path.Combine(root, "Unity-iPhone.xcodeproj"));
        try
        {
            var path = Path.Combine(framework, "PrivacyInfo.xcprivacy");
            File.WriteAllText(path,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<plist version=\"1.0\"><dict>" +
                "<key>NSPrivacyAccessedAPITypes</key><array>" +
                "<dict><key>NSPrivacyAccessedAPIType</key>" +
                "<string>NSPrivacyAccessedAPICategoryUserDefaults</string>" +
                "<key>NSPrivacyAccessedAPITypeReasons</key><array><string>CA92.1</string></array>" +
                "</dict></array></dict></plist>");
            File.WriteAllText(Path.Combine(root, "Info.plist"),
                "<plist version=\"1.0\"><dict><key>CFBundleVersion</key><string>218</string></dict></plist>");
            File.WriteAllText(Path.Combine(root, "Unity-iPhone.xcodeproj/project.pbxproj"), "// empty\n");
            new IosXcodePostprocess().OnPostprocessBuild(new BuildReport
            {
                summary = new BuildSummary { platform = BuildTarget.iOS, outputPath = root }
            });
            var dict = XDocument.Load(path).Root.Element("dict");
            Assert.That(BoolAfter(dict, "NSPrivacyTracking"), Is.False);
            Assert.That(ArrayAfter(dict, "NSPrivacyTrackingDomains"), Is.Empty);
            Assert.That(ArrayAfter(dict, "NSPrivacyCollectedDataTypes"), Is.Empty);
            Assert.That(ArrayAfter(dict, "NSPrivacyAccessedAPITypes"), Is.Not.Empty);
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public void EnsurePrivacyManifest_ClearsNonEmptyCollectedTypes()
    {
        var path = Path.Combine(Path.GetTempPath(), "kh-privacy-one-" + Guid.NewGuid() + ".xcprivacy");
        try
        {
            File.WriteAllText(path,
                "<plist version=\"1.0\"><dict>" +
                "<key>NSPrivacyTracking</key><true/>" +
                "<key>NSPrivacyCollectedDataTypes</key><array><dict/></array>" +
                "</dict></plist>");
            IosXcodePostprocess.EnsurePrivacyManifest(path);
            var dict = XDocument.Load(path).Root.Element("dict");
            Assert.That(BoolAfter(dict, "NSPrivacyTracking"), Is.False);
            Assert.That(ArrayAfter(dict, "NSPrivacyCollectedDataTypes"), Is.Empty);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    static bool BoolAfter(XElement dict, string key)
    {
        var el = dict.Elements("key").Single(k => k.Value == key);
        return ((XElement)el.NextNode).Name.LocalName == "true";
    }

    static System.Collections.Generic.List<XElement> ArrayAfter(XElement dict, string key)
    {
        var el = dict.Elements("key").Single(k => k.Value == key);
        return ((XElement)el.NextNode).Elements().ToList();
    }
}
