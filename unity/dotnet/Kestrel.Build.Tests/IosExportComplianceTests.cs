using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Kestrel.Slice.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class IosExportComplianceTests
{
    [TestCase(false), TestCase(true)]
    public void ExportDeclaresExemptEncryptionAndPreservesVersion(bool existingDeclaration)
    {
        var root=Path.Combine(Path.GetTempPath(), "kh-compliance-"+Guid.NewGuid());
        Directory.CreateDirectory(root);
        try {
            var path=Path.Combine(root,"Info.plist");
            File.WriteAllText(path,"<plist version=\"1.0\"><dict><key>CFBundleVersion</key><string>105</string>"+
                (existingDeclaration ? "<key>ITSAppUsesNonExemptEncryption</key><true/>" : "")+"</dict></plist>");
            var report=new BuildReport { summary=new BuildSummary { platform=BuildTarget.iOS, outputPath=root } };
            new IosXcodePostprocess().OnPostprocessBuild(report);
            new IosXcodePostprocess().OnPostprocessBuild(report);
            var dict=XDocument.Load(path).Root.Element("dict");
            var declaration=dict.Elements("key").Single(key=>key.Value=="ITSAppUsesNonExemptEncryption");
            Assert.That(((XElement)declaration.NextNode).Name.LocalName,Is.EqualTo("false"));
            var version=dict.Elements("key").Single(key=>key.Value=="CFBundleVersion");
            Assert.That(((XElement)version.NextNode).Value,Is.EqualTo("105"));
        } finally { Directory.Delete(root,true); }
    }
    [Test] public void NonIosBuildDoesNotTouchOutput()
    {
        var report=new BuildReport { summary=new BuildSummary { platform=BuildTarget.Standalone, outputPath="nonexistent" } };
        Assert.DoesNotThrow(()=>new IosXcodePostprocess().OnPostprocessBuild(report));
    }
}
