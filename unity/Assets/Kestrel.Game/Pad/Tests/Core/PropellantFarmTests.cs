using System.Linq;
using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class PropellantFarmTests
    {
        static readonly PadFit Heavy = new PadFit(70f, 2.5f);
        static readonly PadFit Small = new PadFit(15f, 0.6f);

        static PadLayout Build(params string[] ids) =>
            PadGenerator.Generate(4, PadArchetype.Tower, Heavy, null, FarmPlan.FromIds(ids));

        [Test]
        public void KeroloxIsAnRp1FarmAndLoxTanks()
        {
            var L = Build("kerolox");
            Assert.That(L.Graph.Get("pad.farm.rp1").DisplayName, Is.EqualTo("RP-1 farm"));
            Assert.That(L.Graph.Get("pad.farm.lox").DisplayName, Is.EqualTo("LOX tanks"));
            Assert.That(L.Graph.Contains("pad.farm.lh2"), Is.False);
            Assert.That(L.Graph.Contains("pad.farm.ch4"), Is.False);
            Assert.That(L.Members.Any(m => m.Id.Contains("pad.farm.rp1")), Is.True);
        }

        [Test]
        public void MethaloxIsMethaneAndLoxSpheres()
        {
            var L = Build("methalox");
            Assert.That(L.Graph.Get("pad.farm.ch4").DisplayName, Is.EqualTo("Methane sphere"));
            Assert.That(L.Graph.Get("pad.farm.lox").DisplayName, Is.EqualTo("LOX sphere"));
            Assert.That(L.Graph.Contains("pad.farm.rp1"), Is.False);
            Assert.That(L.Graph.Contains("pad.farm.lh2"), Is.False);
        }

        [Test]
        public void HydroloxIsAHydrogenSphereWithAVentStack()
        {
            var L = Build("hydrolox");
            Assert.That(L.Graph.Get("pad.farm.lh2").DisplayName, Is.EqualTo("Hydrogen sphere"));
            Assert.That(L.Members.Any(m => m.Id.Contains("lh2vent")), Is.True);
            Assert.That(L.Graph.Get("pad.farm.lox").DisplayName, Is.EqualTo("LOX sphere"));
            Assert.That(L.Graph.Contains("pad.farm.rp1"), Is.False);
        }

        [Test]
        public void ASolidHasNoCryoPlumbing()
        {
            var rail = PadGenerator.Generate(2, PadArchetype.Rail, Small, null, FarmPlan.FromIds(new[] { "solid-apcp" }));
            Assert.That(rail.Graph.Contains("pad.farm"), Is.False);
            Assert.That(rail.Deluge.Active, Is.False, "a solid on a rail has no deluge");
            Assert.That(rail.Members.Any(m => m.Id.StartsWith("pad.farm")), Is.False);
            Assert.That(rail.Members.Any(m => m.Material == MaterialRole.Insulation && m.ParentId != null && m.ParentId.StartsWith("pad.farm")), Is.False);

            var tower = PadGenerator.Generate(2, PadArchetype.Tower, Heavy, null, FarmPlan.FromIds(new[] { "solid-apcp" }));
            Assert.That(tower.Graph.Contains("pad.farm"), Is.False, "solids do not grow cryo tanks");
            Assert.That(tower.Deluge.Active, Is.True, "a tower still has sound-suppression water");
        }

        [Test]
        public void MixedFamiliesDoNotDoubleTheLoxTank()
        {
            var L = Build("gg-kerolox", "exp-hydrolox");
            Assert.That(L.Graph.Contains("pad.farm.rp1"), Is.True);
            Assert.That(L.Graph.Contains("pad.farm.lh2"), Is.True);
            Assert.That(L.Graph.Query(ElementCategory.Tank, "lox").Count, Is.EqualTo(1));
        }

        [Test]
        public void TanksStayOutOfThePlumeAndTheVehicle()
        {
            foreach (var ids in new[] { new[] { "kerolox" }, new[] { "methalox" }, new[] { "hydrolox" } })
            {
                var L = Build(ids);
                float keep = Heavy.VehicleRadius + 0.5f;
                foreach (var m in L.Members)
                {
                    if (m.ParentId == null || (!m.ParentId.StartsWith("pad.farm") && m.ParentId != "pad.deluge" && !m.Id.StartsWith("pad.deluge") && !m.Id.StartsWith("pad.farm"))) continue;
                    foreach (var p in new[] { m.A, m.B })
                    {
                        Assert.That(p.X * p.X + p.Z * p.Z, Is.GreaterThan(keep * keep), m.Id);
                        Assert.That(TrenchGrammar.InPlumeSector(L.Trench, p.X, p.Z, Heavy.VehicleHeight), Is.False, m.Id);
                    }
                }
            }
        }

        [Test]
        public void SamePropellantSameFarm()
        {
            var a = Build("methalox");
            var b = PadGenerator.Generate(4, PadArchetype.Tower, Heavy, null, FarmPlan.FromIds(new[] { "methalox" }));
            Assert.That(a.Hash(), Is.EqualTo(b.Hash()));
        }

        [Test]
        public void FullFarmStaysInsideThePadBudget()
        {
            var plan = FarmPlan.FromIds(new[] { "kerolox", "methalox", "hydrolox" });
            var L = PadGenerator.Generate(9, PadArchetype.TowerStrongback, new PadFit(120f, 5f), null, plan);
            var led = new BudgetLedger();
            PadPerf.Estimate(L, led);
            Assert.That(led.DrawCalls, Is.LessThanOrEqualTo(14), led.Report(KitBudgets.High));
            Assert.That(led.Triangles, Is.LessThan(40_000), led.Report(KitBudgets.High));
            Assert.That(led.Within(KitBudgets.Low), Is.True, led.Report(KitBudgets.Low));
        }

        [Test]
        public void FarmLabelsFitANarrowPhone()
        {
            var L = Build("kerolox", "methalox", "hydrolox");
            foreach (var e in L.Graph.All)
            {
                if (e.Id != "pad.farm" && !e.Id.StartsWith("pad.farm.") && e.Id != "pad.deluge" && !e.Id.StartsWith("pad.deluge.")) continue;
                if (e.Id.Contains(".s") || e.Id.Contains("leg") || e.Id.Contains("noz")) continue;
                float px = e.DisplayName.Length * 14f * 0.62f;
                Assert.That(px, Is.LessThan(335f), e.DisplayName);
            }
        }
    }

    public class DelugeRigTests
    {
        [Test]
        public void TowerDelugeHasATowerPipesAndNozzles()
        {
            var L = PadGenerator.Generate(6, PadArchetype.Tower, new PadFit(70f, 2.5f));
            Assert.That(L.Deluge.Active, Is.True);
            Assert.That(L.Graph.Get("pad.deluge").DisplayName, Is.EqualTo("Water tower"));
            Assert.That(L.Deluge.Nozzles, Is.InRange(6, 16));
            Assert.That(L.Members.Any(m => m.Id.Contains("feed")), Is.True);
            Assert.That(L.Members.Any(m => m.Id.Contains("noz")), Is.True);
            for (int i = 0; i < L.Deluge.Nozzles; i++)
            {
                float x = L.Deluge.X[i], z = L.Deluge.Z[i];
                Assert.That(x * x + z * z, Is.GreaterThan(3f * 3f));
                Assert.That(TrenchGrammar.InPlumeSector(L.Trench, x, z, 70f), Is.False);
            }
        }

        [Test]
        public void SprayAndSteamFollowIgnitionAndDoNotFlash()
        {
            Assert.That(PadVfxPlan.Spray(0f, true, false), Is.EqualTo(0f));
            Assert.That(PadVfxPlan.Spray(1f, false, false), Is.EqualTo(0f));
            Assert.That(PadVfxPlan.Steam(1f, 0f, true, false), Is.EqualTo(0f), "steam has not built at the ignition frame");
            Assert.That(PadVfxPlan.Steam(1f, 0.2f, true, false), Is.LessThan(0.2f));
            Assert.That(PadVfxPlan.Steam(1f, 2f, true, false), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(PadVfxPlan.SteamRiseSeconds, Is.GreaterThan(1f));
            Assert.That(PadVfxPlan.SteamAlpha, Is.LessThanOrEqualTo(0.5f));
            Assert.That(PadVfxPlan.BlastAlpha, Is.LessThanOrEqualTo(0.5f));
            Assert.That(PadVfxPlan.Blast(1f, 0f, 80f, false), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(PadVfxPlan.Blast(1f, 200f, 80f, false), Is.EqualTo(0f));
            Assert.That(PadVfxPlan.Steam(1f, 2f, true, true), Is.LessThan(PadVfxPlan.Steam(1f, 2f, true, false)));
        }

        [Test]
        public void DiamondsSpaceOutAtSeaLevelAndVanishWhenUnderexpanded()
        {
            float sea = PadVfxPlan.PressureRatio(0f);
            float high = PadVfxPlan.PressureRatio(20000f);
            Assert.That(sea, Is.GreaterThan(1.15f));
            Assert.That(high, Is.LessThanOrEqualTo(1.15f));
            Assert.That(PadVfxPlan.DiamondCount(0, sea), Is.EqualTo(0), "low tier draws no trench diamonds");
            Assert.That(PadVfxPlan.DiamondCount(2, sea), Is.EqualTo(PadVfxBudgets.Diamonds(2)));
            Assert.That(PadVfxPlan.DiamondCount(2, 1f), Is.EqualTo(0));
            float a = PadVfxPlan.DiamondSpacing(2f, 1f);
            float b = PadVfxPlan.DiamondSpacing(2f, 4f);
            Assert.That(b, Is.GreaterThan(a));
            Assert.That(b / a, Is.EqualTo(2f).Within(0.05f), "spacing follows sqrt of the pressure ratio");
        }
    }

    public class SkyBirdPlanTests
    {
        [Test]
        public void BirdsMatchTheWebWingAndStayGullSized()
        {
            Assert.That(SkyBirds.WingSpan, Is.InRange(0.85f, 1.2f));
            Assert.That(SurfaceGrain.WingY(0f), Is.LessThan(SurfaceGrain.WingY(0.4f)), "body sits below the wrist");
            Assert.That(SurfaceGrain.WingY(0.82f), Is.LessThan(SurfaceGrain.WingY(0.4f)), "tip sits below the wrist");
            var px = new byte[SurfaceGrain.Bytes];
            SurfaceGrain.Fill(SurfaceKind.Bird, px);
            int amber = 0, dark = 0, clear = 0;
            for (int i = 0; i < px.Length; i += 4)
            {
                if (px[i + 3] < 8) { clear++; continue; }
                if (px[i] > 200 && px[i + 1] > 150) amber++;
                else dark++;
            }
            Assert.That(clear, Is.GreaterThan(amber), "the mark is not a filled box");
            Assert.That(amber, Is.GreaterThan(20));
            Assert.That(dark, Is.GreaterThan(10));
        }

        [Test]
        public void PadBirdsFlushAndSkyBirdsRemain()
        {
            var poses = new SkyBirdPose[SkyBirds.Count];
            SkyBirds.Evaluate(3f, 0f, 1f, 2, poses);
            int pad = 0, sky = 0;
            for (int i = 0; i < poses.Length; i++)
            {
                if (poses[i].Presence <= 0f) continue;
                if (SkyBirds.IsSky(i)) sky++; else pad++;
                Assert.That(poses[i].Span, Is.InRange(0.7f, 1.4f));
            }
            Assert.That(pad, Is.EqualTo(PadVfxBudgets.PadBirds(2)));
            Assert.That(sky, Is.EqualTo(PadVfxBudgets.SkyBirds(2)));

            SkyBirds.Evaluate(3f, 0f, 0f, 2, poses);
            SkyBirds.Evaluate(4.2f, 0f, 1f, 2, poses);
            var flapped = poses[0].Span;
            SkyBirds.Evaluate(4.2f, 0f, 0f, 2, poses);
            Assert.That(poses[0].Span, Is.EqualTo(SkyBirds.WingSpan * (0.9f)).Within(0.05f));
            Assert.That(flapped, Is.Not.EqualTo(poses[0].Span).Within(0.001f));

            SkyBirds.Evaluate(3f, 500f, 1f, 2, poses);
            for (int i = 0; i < SkyBirds.PadCount; i++) Assert.That(poses[i].Presence, Is.EqualTo(0f));
            Assert.That(poses[SkyBirds.PadCount].Presence, Is.GreaterThan(0.5f));

            SkyBirds.Evaluate(3f, 8000f, 1f, 2, poses);
            foreach (var p in poses) Assert.That(p.Presence, Is.EqualTo(0f));

            SkyBirds.Evaluate(3f, 0f, 1f, 0, poses);
            int shown = poses.Count(p => p.Presence > 0f);
            Assert.That(shown, Is.EqualTo(PadVfxBudgets.Birds(0)));
        }

        [Test]
        public void SurfaceGrainStaysNearWhiteAndProcedural()
        {
            var px = new byte[SurfaceGrain.Bytes];
            foreach (var kind in new[] { SurfaceKind.Concrete, SurfaceKind.Steel, SurfaceKind.Grass, SurfaceKind.Sand, SurfaceKind.Water })
            {
                SurfaceGrain.Fill(kind, px);
                Assert.That(SurfaceGrain.Mean(px), Is.InRange(0.75f, 0.98f), kind.ToString());
            }
            Assert.That(SurfaceGrain.Bytes, Is.LessThan(32 * 1024));
        }
    }
}
