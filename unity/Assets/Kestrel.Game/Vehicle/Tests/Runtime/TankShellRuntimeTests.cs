// Unity EditMode tests (need UnityEngine). Not run locally: no Unity on the Art box; the lead runs them in the CLI pass.
using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Vehicle.Tests
{
    public class TankShellRuntimeTests
    {
        static TankShellSpec Spec() => TankShellSpec.Default(1.85f, 9.6f, 42);

        [TearDown] public void TearDown() { SharedMeshes.Clear(); VehicleMaterials.ClearCache(); MaterialLibrary.ClearCache(); }

        [Test]
        public void MeshPerLodMatchesBudgetEstimate()
        {
            // #106 got 1 submesh: Mesh.CombineMeshes drops empty inputs. The builder now always emits 3 submeshes.
            foreach (var s in new[] { Spec(), TankShellSpec.Default(1.2f, 0f, 3), TankShellSpec.Default(2.6f, 42f, 9) })
            {
                var L = TankLayout.Build(s);
                for (int lod = 0; lod < 3; lod++)
                {
                    var m = TankShellBuilder.Build(s, lod);
                    try
                    {
                        Assert.That(m.subMeshCount, Is.EqualTo(TankShellBuilder.SubmeshCount), $"lod {lod}");
                        Assert.That(m.normals.Length, Is.EqualTo(m.vertexCount));
                        Assert.That(m.tangents.Length, Is.EqualTo(m.vertexCount));
                        int est = TankBudget.Triangles(L, TankLod.For(lod), out int shell, out int hw);
                        Assert.That(KitMesh.TriangleCount(m), Is.EqualTo(est), $"lod {lod}: estimate must be exact");
                        Assert.That((int)m.GetIndexCount(TankShellBuilder.SubmeshShell) / 3, Is.EqualTo(shell));
                        if (lod == 0) Assert.That(est, Is.LessThanOrEqualTo(TankLayout.Lod0TriangleCap));
                        foreach (var v in m.vertices) Assert.That(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z), Is.False);
                        Assert.That(m.bounds.min.y, Is.GreaterThanOrEqualTo(s.ShellBottom - TankShellBuilder.StiffenerRadius - 1e-3f), "flush aft end");
                        Assert.That(m.bounds.max.y, Is.LessThanOrEqualTo(s.ShellTop + 1e-3f));
                    }
                    finally { Object.DestroyImmediate(m); }
                }
            }
        }

        [Test]
        public void MaterialsComeFromKit()
        {
            var mats = TankShellBuilder.Materials();
            Assert.That(mats.Length, Is.EqualTo(TankShellBuilder.SubmeshCount));
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
                Assert.That(Mathf.Atan2(p.x, p.z), Is.InRange(-1e-3f, Mathf.PI / 2 + 1e-3f));
                Assert.That(p.y, Is.InRange(s.BarrelBottom - 1e-3f, s.BarrelTop + 1e-3f));
            }
            Assert.That(IsogridCutaway.RibMatrices(TankShellSpec.Default(1.2f, 0, 1), 0, 1), Is.Empty);
        }

        [Test]
        public void SharedMeshesAreReusedAndReleased()
        {
            int builds = 0;
            Mesh Make() { builds++; return new Mesh(); }
            var a = SharedMeshes.Acquire("k", Make); var b = SharedMeshes.Acquire("k", Make);
            Assert.That(ReferenceEquals(a, b)); Assert.That(builds, Is.EqualTo(1)); Assert.That(SharedMeshes.RefCount("k"), Is.EqualTo(2));
            SharedMeshes.Release("k"); Assert.That(a != null, "still referenced");
            SharedMeshes.Release("k"); Assert.That(a == null, "destroyed with the last release");
            Assert.That(SharedMeshes.Count, Is.EqualTo(0));
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
                v.Init(TankShellSpec.Default(1.85f, 4f, (ulong)i), KitBudgets.Low);   // rebuild releases the old set
                v.SetCutaway(true); Assert.That(v.RibCount, Is.GreaterThan(0));
                v.SetCutaway(false);                                                 // toggle releases cutaway + rib mesh
                v.SetCutaway(true);
                Object.DestroyImmediate(go);                                         // OnDestroy releases the rest
                Assert.That(SharedMeshes.Count, Is.EqualTo(0), $"iteration {i}");
            }
            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Length, Is.LessThanOrEqualTo(before));
        }

        [Test]
        public void IdenticalTanksShareMeshes()
        {
            var a = new GameObject("a", typeof(MeshFilter), typeof(MeshRenderer)).AddComponent<TankShellView>();
            var b = new GameObject("b", typeof(MeshFilter), typeof(MeshRenderer)).AddComponent<TankShellView>();
            a.Init(Spec(), KitBudgets.Low); b.Init(Spec(), KitBudgets.Low);
            Assert.That(SharedMeshes.Count, Is.EqualTo(3), "3 LODs, built once for both views");
            Assert.That(ReferenceEquals(a.GetComponent<MeshFilter>().sharedMesh, b.GetComponent<MeshFilter>().sharedMesh));
            Object.DestroyImmediate(a.gameObject); Assert.That(SharedMeshes.Count, Is.EqualTo(3));
            Object.DestroyImmediate(b.gameObject); Assert.That(SharedMeshes.Count, Is.EqualTo(0));
        }
    }
}
