using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;

/// <summary>
/// WebGL strips engine code (stripEngineCode + ManagedStrippingLevel.High). GameObject.CreatePrimitive adds its collider
/// by name, so a collider no script references is stripped and every primitive logs "Can't add component ... doesn't
/// exist" in the web player (QA #417: 8 CapsuleCollider errors per break-up). Every primitive type the runtime creates
/// must keep its collider in unity/Assets/link.xml.
/// </summary>
public class LinkXmlTests
{
    static readonly Dictionary<string, string> ColliderOf = new Dictionary<string, string>
    {
        ["Sphere"] = "UnityEngine.SphereCollider",
        ["Capsule"] = "UnityEngine.CapsuleCollider",
        ["Cylinder"] = "UnityEngine.CapsuleCollider",
        ["Cube"] = "UnityEngine.BoxCollider",
        ["Plane"] = "UnityEngine.MeshCollider",
        ["Quad"] = "UnityEngine.MeshCollider",
    };

    static string Assets()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
        {
            var assets = Path.Combine(dir.FullName, "unity", "Assets");
            if (Directory.Exists(assets)) return assets;
            if (Path.GetFileName(dir.FullName) == "unity" && Directory.Exists(Path.Combine(dir.FullName, "Assets")))
                return Path.Combine(dir.FullName, "Assets");
        }
        Assert.Fail("unity/Assets not found above " + TestContext.CurrentContext.TestDirectory);
        return null;
    }

    static bool IsRuntime(string path)
    {
        var parts = path.Replace('\\', '/').Split('/');
        return !parts.Any(p => p == "Editor" || p == "Tests" || p == "CaptureTests" || p.EndsWith(".Tests", StringComparison.Ordinal));
    }

    static HashSet<string> Preserved(string assets)
    {
        var doc = XDocument.Load(Path.Combine(assets, "link.xml"));
        return new HashSet<string>(doc.Descendants("assembly")
            .Where(a => (string)a.Attribute("fullname") == "UnityEngine.PhysicsModule")
            .Descendants("type")
            .Where(t => (string)t.Attribute("preserve") == "all")
            .Select(t => (string)t.Attribute("fullname")));
    }

    [Test]
    public void EveryRuntimePrimitiveKeepsItsCollider()
    {
        var assets = Assets();
        var used = new SortedSet<string>();
        var rx = new Regex(@"PrimitiveType\.(\w+)");
        foreach (var file in Directory.EnumerateFiles(assets, "*.cs", SearchOption.AllDirectories).Where(IsRuntime))
            foreach (Match m in rx.Matches(File.ReadAllText(file)))
                used.Add(m.Groups[1].Value);
        Assert.That(used, Does.Contain("Sphere").And.Contain("Cylinder"), "scan found the failure marker and shards");
        var kept = Preserved(assets);
        foreach (var type in used)
        {
            Assert.That(ColliderOf.ContainsKey(type), "unknown PrimitiveType." + type);
            Assert.That(kept, Does.Contain(ColliderOf[type]),
                "PrimitiveType." + type + " is created at runtime; link.xml must preserve " + ColliderOf[type] + " for the stripped WebGL player");
        }
    }

    [Test]
    public void WebGlWorkflowBuildsOnLinkXmlChanges()
    {
        var repo = Path.GetDirectoryName(Path.GetDirectoryName(Assets()));
        var yml = File.ReadAllText(Path.Combine(repo, ".github", "workflows", "webgl-build.yml"));
        Assert.That(yml, Does.Contain("unity/Assets/link.xml"), "a keep-list change only shows in a stripped player");
    }
}
