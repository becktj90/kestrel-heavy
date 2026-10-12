using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Kit.Tests
{
    public class KitMeshTests
    {
        [Test]
        public void BeamAndIBeamTriangleCounts()
        {
            var b = KitMesh.Beam(0.3f, 0.3f); Assert.That(KitMesh.TriangleCount(b), Is.EqualTo(12));
            var i = KitMesh.IBeam(0.3f, 0.4f, 0.03f); Assert.That(KitMesh.TriangleCount(i), Is.EqualTo(36));
            Object.DestroyImmediate(b); Object.DestroyImmediate(i);
        }

        [Test]
        public void TubeRingsHaveConstantRadiusThroughBends()
        {
            var path = new[] { Vector3.zero, new Vector3(0, 2, 0), new Vector3(2, 2, 0), new Vector3(2, 2, 3) };
            var m = KitMesh.Tube(path, 0.1f, 8);
            Assert.That(m.vertexCount, Is.EqualTo(4 * 9));
            Assert.That(KitMesh.TriangleCount(m), Is.EqualTo(3 * 8 * 2));
            var vs = m.vertices; for (int r = 0; r < 9; r++) Assert.That(Vector3.Distance(vs[r], path[0]), Is.EqualTo(0.1f).Within(1e-4f));
            Object.DestroyImmediate(m);
        }

        [Test]
        public void HoseEndsOnAttachPointsAndLodReducesTris()
        {
            var a = new Vector3(0, 30, 0); var b = new Vector3(6, 22, 3);
            var p = KitMesh.HosePath(a, b, 1.15f, 24);
            Assert.That(Vector3.Distance(p[0], a), Is.LessThan(1e-3f)); Assert.That(Vector3.Distance(p[24], b), Is.LessThan(1e-3f));
            var h0 = KitMesh.Hose(a, b, 0.08f, 1.15f, 0); var h2 = KitMesh.Hose(a, b, 0.08f, 1.15f, 2);
            Assert.That(KitMesh.TriangleCount(h2), Is.LessThan(KitMesh.TriangleCount(h0) / 4));
            Object.DestroyImmediate(h0); Object.DestroyImmediate(h2);
        }

        [Test]
        public void InstanceDrawCallsChunkAt1023()
        {
            Assert.That(InstanceBatch.DrawCallsFor(0), Is.EqualTo(0));
            Assert.That(InstanceBatch.DrawCallsFor(1023), Is.EqualTo(1));
            Assert.That(InstanceBatch.DrawCallsFor(1024), Is.EqualTo(2));
        }

        [Test]
        public void MaterialSpecsCoverEveryRoleAndStripeTextureIsTiny()
        {
            foreach (MaterialRole r in System.Enum.GetValues(typeof(MaterialRole)))
                Assert.That(MaterialLibrary.Spec(r).BaseColor, Is.Not.EqualTo(Color.magenta), r.ToString());
            var t = MaterialLibrary.StripeTexture();
            Assert.That(t.width * t.height * 4, Is.LessThan(4096));
        }
    }
}
