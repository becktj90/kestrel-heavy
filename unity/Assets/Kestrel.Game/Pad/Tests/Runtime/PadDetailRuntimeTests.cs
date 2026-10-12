using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Pad.Tests
{
    public class PadDetailRuntimeTests
    {
        GameObject _root;
        [TearDown] public void TearDown() { if (_root != null) Object.DestroyImmediate(_root); MaterialLibrary.ClearCache(); }

        [Test]
        public void DetailMeshesShareTheKitMaterials()
        {
            _root = new GameObject("pad");
            PadBuilder.Build(_root.transform, 4, PadArchetype.Tower, new PadFit(70f, 2.5f, 6f));
            var d = _root.GetComponent<PadDetailRenderer>();
            Assert.That(d, Is.Not.Null);
            Assert.That(d.MeshFor(MaterialRole.Concrete).vertexCount, Is.GreaterThan(8));
            Assert.That(d.MeshFor(MaterialRole.Refractory).vertexCount, Is.GreaterThan(3));
            Assert.That(d.MeshFor(MaterialRole.Galvanized).vertexCount, Is.GreaterThan(3));
            Assert.That(d.MeshFor(MaterialRole.WarningStripes).vertexCount, Is.GreaterThan(3));
            Assert.That(_root.GetComponentsInChildren<MeshRenderer>(), Is.Empty, "detail is drawn, not a child renderer");
            var shader = Shader.Find(MaterialLibrary.LitShader);
            if (shader == null) shader = Shader.Find(MaterialLibrary.FallbackShader);
            if (shader == null) Assert.Ignore("URP Lit shader not available");
            Assert.That(d.MaterialFor(MaterialRole.Concrete), Is.SameAs(MaterialLibrary.Get(MaterialRole.Concrete)));
            Assert.That(d.MaterialFor(MaterialRole.Refractory), Is.SameAs(MaterialLibrary.Get(MaterialRole.Refractory)));
            Assert.That(d.MaterialFor(MaterialRole.WarningStripes), Is.SameAs(MaterialLibrary.Get(MaterialRole.WarningStripes)));
        }
    }
}
