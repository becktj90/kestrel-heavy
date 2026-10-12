// Unity EditMode tests (need UnityEngine). Not run locally: no Unity on the Art box; the lead runs them in the CLI pass.
using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Vehicle.Tests
{
    public class TankShellRuntimeTests
    {
        static TankShellSpec Spec() => TankShellSpec.Default(1.85f, 9.6f, 42);

        [TearDown] public void TearDown() { VehicleMaterials.ClearCache(); MaterialLibrary.ClearCache(); }

        [Test]
        public void MeshPerLodMatchesBudgetEstimate()
        {
            var s = Spec(); var L = TankLayout.Build(s);
            for (int lod = 0; lod < 3; lod++)
            {
                var m = TankShellBuilder.Build(s, lod);
                try
                {
                    Assert.That(m.subMeshCount, Is.EqualTo(3));
                    Assert.That(m.vertexCount, Is.GreaterThan(0));
                    Assert.That(m.normals.Length, Is.EqualTo(m.vertexCount));
                    int est = TankBudget.Triangles(s, L, TankLod.For(lod), LodPolicy.RadialSegments(lod), out _, out _);
                    Assert.That(KitMesh.TriangleCount(m), Is.EqualTo(est).Within(est * 0.05 + 1), $"lod {lod}");
                    foreach (var v in m.vertices) Assert.That(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z), Is.False);
                    Assert.That(m.bounds.size.y, Is.EqualTo(s.TotalHeight).Within(0.05f));
                }
                finally { Object.DestroyImmediate(m); }
            }
        }

        [Test]
        public void MaterialsComeFromKit()
        {
            var mats = TankShellBuilder.Materials();
            Assert.That(mats.Length, Is.EqualTo(3));
            Assert.That(ReferenceEquals(mats[TankShellBuilder.SubmeshWelds], MaterialLibrary.Get(MaterialRole.Galvanized)));
            Assert.That(ReferenceEquals(mats[TankShellBuilder.SubmeshStiffeners], MaterialLibrary.Get(MaterialRole.PaintedSteel)));
            Assert.That(ReferenceEquals(mats[0], VehicleMaterials.ShellIsogrid()), "shell variant is cached, never per object");
            Assert.That(mats[0].shader, Is.SameAs(MaterialLibrary.Get(MaterialRole.PaintedSteel).shader));
            Assert.That(mats[0].GetTexture("_BumpMap"), Is.SameAs(VehicleMaterials.IsogridNormal()));
        }

        [Test]
        public void IsogridNormalIsSmallLinearAndMipped()
        {
            var t = VehicleMaterials.IsogridNormal();
            Assert.That(t.width, Is.EqualTo(VehicleMaterials.IsogridTileWidth));
            Assert.That(t.mipmapCount, Is.GreaterThan(1));
            Assert.That(VehicleMaterials.GeneratedTextureBytes, Is.LessThan(200 * 1024));
        }

        [Test]
        public void CutawayRibsOnlyInSector()
        {
            var s = Spec();
            var ribs = IsogridCutaway.RibMatrices(s, 0, Mathf.PI / 2);
            Assert.That(ribs.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(IsogridCutaway.MaxRibs));
            foreach (var m in ribs)
            {
                Vector3 p = m.GetColumn(3);
                float az = Mathf.Atan2(p.x, p.z);
                Assert.That(az, Is.InRange(-1e-3f, Mathf.PI / 2 + 1e-3f));
                Assert.That(p.y, Is.InRange(s.BarrelBottom - 1e-3f, s.BarrelTop + 1e-3f));
            }
            Assert.That(IsogridCutaway.RibMatrices(TankShellSpec.Default(1.2f, 0, 1), 0, 1), Is.Empty);
        }

        [Test]
        public void ViewInitAndCutawayDoNotLeakMeshes()
        {
            int before = Resources.FindObjectsOfTypeAll<Mesh>().Length;
            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject("tank", typeof(MeshFilter), typeof(MeshRenderer));
                var v = go.AddComponent<TankShellView>();
                v.Init(Spec(), KitBudgets.Low);
                v.SetCutaway(true);
                Assert.That(v.RibCount, Is.GreaterThan(0));
                Object.DestroyImmediate(go);
            }
            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Length, Is.LessThanOrEqualTo(before));
        }
    }
}
