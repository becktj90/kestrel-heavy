using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class KitCoreTests
    {
        [Test]
        public void LodDistancesAreMonotonic()
        {
            for (int q = 0; q <= 2; q++)
                foreach (DetailSize s in System.Enum.GetValues(typeof(DetailSize)))
                {
                    Assert.That(LodPolicy.Lod0Max(q, s), Is.LessThan(LodPolicy.Lod1Max(q, s)));
                    Assert.That(LodPolicy.Lod1Max(q, s), Is.LessThan(LodPolicy.CullDistance(q, s)));
                    if (q > 0) Assert.That(LodPolicy.CullDistance(q, s), Is.GreaterThanOrEqualTo(LodPolicy.CullDistance(q - 1, s)));
                    if (s > 0) Assert.That(LodPolicy.CullDistance(q, s), Is.GreaterThan(LodPolicy.CullDistance(q, s - 1)));
                }
        }

        [Test]
        public void LodSelectBandsAndCull()
        {
            Assert.That(LodPolicy.Select(2, DetailSize.Prop, 10), Is.EqualTo(0));
            Assert.That(LodPolicy.Select(2, DetailSize.Prop, 150), Is.EqualTo(2));
            Assert.That(LodPolicy.Select(0, DetailSize.Prop, 150), Is.EqualTo(-1));
            Assert.That(LodPolicy.Select(0, DetailSize.Prop, 150, lodBias: 2f), Is.EqualTo(2));
        }

        [Test]
        public void BudgetsScaleWithQualityAndArtCapIs8MB()
        {
            Assert.That(KitBudgets.EnvironmentArtBytes, Is.EqualTo(8 * 1024 * 1024));
            Assert.That(KitBudgets.TargetHz(2), Is.EqualTo(120)); Assert.That(KitBudgets.TargetHz(0), Is.EqualTo(60));
            Assert.That(KitBudgets.PadDrawCalls(0), Is.LessThan(KitBudgets.PadDrawCalls(2)));
            var l = new BudgetLedger(); l.Add(50, 100_000, 1 << 20);
            Assert.That(l.Within(0), Is.True);
            l.Add(0, 0, 8 << 20); Assert.That(l.Within(2), Is.False);
        }

        [Test]
        public void CatenaryHitsEndpointsHasLengthAndSags()
        {
            const int n = 200; var h = new double[n + 1]; var v = new double[n + 1];
            Catenary.Sample2D(0, 10, 8, 6, 12, n, h, v);
            Assert.That(h[0], Is.EqualTo(0).Within(1e-9)); Assert.That(h[n], Is.EqualTo(8).Within(1e-6));
            Assert.That(v[0], Is.EqualTo(10).Within(1e-9)); Assert.That(v[n], Is.EqualTo(6).Within(1e-6));
            double len = 0; for (int i = 1; i <= n; i++) len += System.Math.Sqrt((h[i] - h[i - 1]) * (h[i] - h[i - 1]) + (v[i] - v[i - 1]) * (v[i] - v[i - 1]));
            Assert.That(len, Is.EqualTo(12).Within(0.01));
            Assert.That(Catenary.MinV(v, n + 1), Is.LessThan(6), "slack hose sags below the lower end");
        }

        [Test]
        public void CatenaryTautIsNearlyStraight()
        {
            const int n = 50; var h = new double[n + 1]; var v = new double[n + 1];
            Catenary.Sample2D(0, 0, 10, 0, 10, n, h, v);
            Assert.That(Catenary.MinV(v, n + 1), Is.GreaterThan(-0.2));
        }
    }
}
