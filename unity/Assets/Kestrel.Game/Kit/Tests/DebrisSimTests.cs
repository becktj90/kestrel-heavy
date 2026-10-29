using System;
using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class DebrisSimTests
    {
        static DebrisWorld World(int q = 2) => new DebrisWorld(q) { Surfaces = new DebrisSurfaces { GroundY = 0f } };

        static void Run(DebrisWorld w, float seconds, float dt = 1f / 120f) { for (float t = 0; t < seconds; t += dt) w.Step(dt); }

        static int Ball(DebrisWorld w, V3 p, V3 v, float e = 0.5f, float mu = 0.5f, bool collides = true)
        {
            int h = w.SpawnSmall(DebrisKind.Fragment, p, v, new V3(), 0.1f, 1f, 0f, 1f, 0f, e, mu, collides);
            return h;
        }

        [Test]
        public void BudgetsAreTheDocumentedNumbers()
        {
            Assert.That(new[] { DebrisBudgets.Fragments(0), DebrisBudgets.Fragments(1), DebrisBudgets.Fragments(2) }, Is.EqualTo(new[] { 12, 32, 64 }));
            Assert.That(new[] { DebrisBudgets.Sparks(0), DebrisBudgets.Sparks(1), DebrisBudgets.Sparks(2) }, Is.EqualTo(new[] { 24, 80, 200 }));
            Assert.That(DebrisBudgets.RigidSlots, Is.EqualTo(3));
            Assert.That(DebrisBudgets.Capacity(2), Is.EqualTo(3 + 64 + 200));
            Assert.That(new DebrisWorld(1).Bodies.Length, Is.EqualTo(3 + 32 + 80));
        }

        [Test]
        public void FreeFallMatchesGravity()
        {
            var w = World(); var h = Ball(w, new V3(0, 100, 0), new V3(), collides: false);
            Run(w, 2f);
            var b = w.Bodies[w.SlotOf(h)];
            Assert.That(b.P.Y, Is.EqualTo(100f - 0.5f * 9.81f * 4f).Within(0.5f));   // 2 s: 19.6 m, drag is tiny
            Assert.That(b.V.Y, Is.EqualTo(-9.81f * 2f).Within(1.2f));
        }

        [Test]
        public void BounceHeightFollowsRestitutionSquared()
        {
            var w = World(); var h = Ball(w, new V3(0, 10.1f, 0), new V3(), e: 0.5f);
            float peak = 0f; bool bounced = false;
            for (int i = 0; i < 600; i++)
            {
                w.Step(1f / 120f);
                var b = w.Bodies[w.SlotOf(h)];
                if (b.V.Y > 0.5f) bounced = true;
                if (bounced) peak = Math.Max(peak, b.P.Y - 0.1f);
            }
            Assert.That(bounced, Is.True);
            Assert.That(peak, Is.EqualTo(10f * 0.25f).Within(0.6f));
        }

        [Test]
        public void BallComesToRestOnTheGroundAndSleeps()
        {
            var w = World(); var h = Ball(w, new V3(0, 3, 0), new V3(2, 0, 0));
            Run(w, 12f);
            var b = w.Bodies[w.SlotOf(h)];
            Assert.That(b.P.Y, Is.EqualTo(0.1f).Within(0.02f));
            Assert.That(b.Asleep, Is.True);
            Assert.That(b.P.X, Is.GreaterThan(0f).And.LessThan(6f), "friction stops the slide");
        }

        [Test]
        public void FrictionSlowsASlidingBody()
        {
            var low = World(); var high = World();
            int a = Ball(low, new V3(0, 0.1f, 0), new V3(10, 0, 0), e: 0f, mu: 0.1f);
            int b = Ball(high, new V3(0, 0.1f, 0), new V3(10, 0, 0), e: 0f, mu: 0.9f);
            Run(low, 1f); Run(high, 1f);
            Assert.That(high.Bodies[high.SlotOf(b)].P.X, Is.LessThan(low.Bodies[low.SlotOf(a)].P.X));
        }

        [Test]
        public void DeckHoldsBodiesAboveGroundAndEdgeDropsThem()
        {
            var w = new DebrisWorld(2) { Surfaces = new DebrisSurfaces { GroundY = 0f, DeckY = 5f, DeckRadius = 10f } };
            int on = Ball(w, new V3(0, 20, 0), new V3());
            int off = Ball(w, new V3(30, 20, 0), new V3());
            Run(w, 8f);
            Assert.That(w.Bodies[w.SlotOf(on)].P.Y, Is.EqualTo(5.1f).Within(0.05f));
            Assert.That(w.Bodies[w.SlotOf(off)].P.Y, Is.EqualTo(0.1f).Within(0.05f));
        }

        [Test]
        public void SlidingOffTheDeckEdgeFallsToTheGround()
        {
            var w = new DebrisWorld(2) { Surfaces = new DebrisSurfaces { GroundY = 0f, DeckY = 5f, DeckRadius = 10f } };
            int h = Ball(w, new V3(8, 5.1f, 0), new V3(6, 0, 0), e: 0f, mu: 0.05f);
            Run(w, 10f);
            Assert.That(w.Bodies[w.SlotOf(h)].P.Y, Is.EqualTo(0.1f).Within(0.05f));
            Assert.That(w.Bodies[w.SlotOf(h)].P.X, Is.GreaterThan(10f));
        }

        [Test]
        public void EnergyNeverGrowsInAFallAndBounce()
        {
            var w = World(); var h = Ball(w, new V3(0, 8, 0), new V3(3, 0, 0));
            float e0 = 0.5f * 1f * 9f + 1f * 9.81f * 8f;
            for (int i = 0; i < 720; i++)
            {
                w.Step(1f / 120f);
                var b = w.Bodies[w.SlotOf(h)];
                float e = 0.5f * b.Mass * V3.Dot(b.V, b.V) + 0.5f * b.Inertia * V3.Dot(b.W, b.W) + b.Mass * 9.81f * Math.Max(0f, b.P.Y);
                Assert.That(e, Is.LessThanOrEqualTo(e0 * 1.03f), $"step {i}");
            }
        }

        [Test]
        public void SeparationSpinIsLeverTimesImpulseOverInertia()
        {
            float m = 12000f, r = 1.85f, len = 40f, j = m * 2.5f, lever = 4.8f;
            float inertia = DebrisMath.CylinderInertia(m, r, len);
            Assert.That(inertia, Is.EqualTo(m * (3f * r * r + len * len) / 12f).Within(1f));
            float w = DebrisMath.SpinFromImpulse(j, lever, inertia);
            Assert.That(w, Is.EqualTo(lever * j / inertia).Within(1e-6f));
            Assert.That(DebrisMath.SpinFromImpulse(j, 0f, inertia), Is.EqualTo(0f), "no lever, no tumble");
            Assert.That(DebrisMath.SpinFromImpulse(j, lever * 2f, inertia), Is.EqualTo(2f * w).Within(1e-6f));
        }

        [Test]
        public void FreeTumbleKeepsItsSpinAndTurnsByOmegaTimesT()
        {
            var w = new DebrisWorld(2);
            var h = w.SpawnRigid(DebrisKind.Stage, new V3(), new V3(0, -2.5f, 0), Q4.Identity, new V3(0, 0, 0.3f), 12000f, 1.85f, 40f, 0f, 0f, 30f);
            Run(w, 6f);
            var b = w.Bodies[w.SlotOf(h)];
            Assert.That(b.W.Z, Is.EqualTo(0.3f).Within(1e-5f));
            var axis = b.R.Rotate(new V3(0, 1, 0));
            float angle = (float)Math.Atan2(axis.X, axis.Y);   // rotation about Z by +theta takes +Y toward -X
            Assert.That(Math.Abs(angle), Is.EqualTo(0.3f * 6f).Within(0.01f));
            Assert.That(b.P.Y, Is.EqualTo(-2.5f * 6f).Within(0.05f), "relative-frame body keeps its separation velocity");
        }

        [Test]
        public void FrameAccelerationDropsTheStageBehindTheStack()
        {
            var w = new DebrisWorld(2);
            var h = w.SpawnRigid(DebrisKind.Stage, new V3(), new V3(), Q4.Identity, new V3(), 1000f, 1f, 10f, -4f, 0f, 30f);
            Run(w, 3f);
            Assert.That(w.Bodies[w.SlotOf(h)].P.Y, Is.EqualTo(-0.5f * 4f * 9f).Within(0.1f));
        }

        [Test]
        public void StageLyingOnTheGroundRestsAtItsRadiusAndAStandingOneToppleOntoItsSide()
        {
            var w = World();
            var side = w.SpawnRigid(DebrisKind.Stage, new V3(0, 6, 0), new V3(), new Q4 { W = (float)Math.Cos(Math.PI / 4), Z = (float)Math.Sin(Math.PI / 4) }, new V3(), 5000f, 1.5f, 20f, 0f, 1f, 30f);
            Run(w, 8f);
            Assert.That(w.Bodies[w.SlotOf(side)].P.Y, Is.EqualTo(1.5f).Within(0.05f));
            var w2 = World();
            var up = w2.SpawnRigid(DebrisKind.Stage, new V3(0, 40, 0), new V3(), Q4.Identity, new V3(), 5000f, 1.5f, 20f, 0f, 1f, 30f);
            Run(w2, 8f);
            Assert.That(w2.Bodies[w2.SlotOf(up)].P.Y, Is.EqualTo(1.5f).Within(0.1f), "a single rim contact cannot hold a standing cylinder");
        }

        [Test]
        public void TumblingStageHittingTheDeckLosesEnergyAndSettlesWithoutPenetration()
        {
            var w = new DebrisWorld(2) { Surfaces = new DebrisSurfaces { GroundY = 0f, DeckY = 2f, DeckRadius = 100f } };
            var h = w.SpawnRigid(DebrisKind.Stage, new V3(0, 30, 0), new V3(3, 0, 0), Q4.Identity, new V3(0, 0, 0.8f), 8000f, 1.5f, 18f, 0f, 1f, 60f);
            float k0 = w.KineticEnergy();
            float minLow = 1e9f;
            for (int i = 0; i < 120 * 40; i++)
            {
                w.Step(1f / 120f);
                var b = w.Bodies[w.SlotOf(h)];
                var a = b.R.Rotate(new V3(0, 1, 0));
                float lowest = b.P.Y - Math.Abs(a.Y) * b.Length * 0.5f - (float)Math.Sqrt(Math.Max(0f, 1f - a.Y * a.Y)) * b.Radius;
                minLow = Math.Min(minLow, lowest);
            }
            Assert.That(minLow, Is.GreaterThan(1.9f), "never sinks through the deck");
            Assert.That(w.KineticEnergy(), Is.LessThan(k0));
            Assert.That(w.Bodies[w.SlotOf(h)].Asleep, Is.True);
        }

        [Test]
        public void PoolNeverGrowsAndRecyclesTheOldestOfItsKind()
        {
            var w = new DebrisWorld(0);
            int len = w.Bodies.Length;
            for (int i = 0; i < 500; i++) Ball(w, new V3(i, 5, 0), new V3());
            for (int i = 0; i < 500; i++) w.SpawnSmall(DebrisKind.Spark, new V3(i, 5, 0), new V3(), new V3(), 0.04f, 0.02f, 1f, 0.4f, 2f, 0.4f, 0.3f, true);
            Assert.That(w.Bodies.Length, Is.EqualTo(len));
            Assert.That(w.LiveOf(DebrisKind.Fragment), Is.EqualTo(DebrisBudgets.Fragments(0)));
            Assert.That(w.LiveOf(DebrisKind.Spark), Is.EqualTo(DebrisBudgets.Sparks(0)));
            for (int i = 0; i < 20; i++) w.SpawnRigid(DebrisKind.Fairing, new V3(), new V3(), Q4.Identity, new V3(), 100f, 1f, 5f, 0f, 0f, 10f);
            Assert.That(w.LiveOf(DebrisKind.Fairing), Is.EqualTo(DebrisBudgets.RigidSlots));
        }

        [Test]
        public void HotChunkEmissionNeverClipsToWhite()
        {
            for (int i = 0; i <= 100; i++)
                foreach (float expo in new[] { 1f, 0.7f })
                {
                    var e = DebrisMath.FragmentEmission(i / 100f, expo);
                    Assert.That(e.R, Is.InRange(0f, DebrisMath.MaxFragmentGlow)); Assert.That(e.G, Is.InRange(0f, DebrisMath.MaxFragmentGlow)); Assert.That(e.B, Is.InRange(0f, DebrisMath.MaxFragmentGlow));
                    Assert.That(e.B, Is.LessThan(0.8f * DebrisMath.MaxFragmentGlow + 0.001f), "blue stays under the hot colour so a chunk reads orange or cream, never flat white");
                }
            var hot = DebrisMath.FragmentEmission(1f, 1f); var cold = DebrisMath.FragmentEmission(0.05f, 1f);
            Assert.That(hot.R + hot.G + hot.B, Is.GreaterThan(cold.R + cold.G + cold.B), "hotter is still brighter");
            Assert.That(DebrisMath.FragmentEmission(float.NaN, 1f).R, Is.EqualTo(0f));
        }

        [Test]
        public void BurstRespectsTierAndReduceMotion()
        {
            for (int q = 0; q <= 2; q++)
            {
                var w = World(q); w.Burst(new V3(0, 5, 0), new V3(), 1f, 3f, true, 1, false);
                Assert.That(w.LiveOf(DebrisKind.Fragment), Is.InRange(1, DebrisBudgets.Fragments(q)));
                Assert.That(w.LiveOf(DebrisKind.Spark), Is.InRange(1, DebrisBudgets.Sparks(q)));
                var r = World(q); r.Burst(new V3(0, 5, 0), new V3(), 1f, 3f, true, 1, true);
                Assert.That(r.LiveOf(DebrisKind.Spark), Is.EqualTo(0));
                Assert.That(r.LiveOf(DebrisKind.Fragment), Is.LessThan(w.LiveOf(DebrisKind.Fragment)));
            }
        }

        [Test]
        public void BurstIsDeterministicPerSeed()
        {
            var a = World(); var b = World(); var c = World();
            a.Burst(new V3(0, 5, 0), new V3(), 1f, 3f, true, 7, false); b.Burst(new V3(0, 5, 0), new V3(), 1f, 3f, true, 7, false); c.Burst(new V3(0, 5, 0), new V3(), 1f, 3f, true, 8, false);
            Run(a, 1f); Run(b, 1f); Run(c, 1f);
            Assert.That(a.KineticEnergy(), Is.EqualTo(b.KineticEnergy())); Assert.That(a.KineticEnergy(), Is.Not.EqualTo(c.KineticEnergy()));
        }

        [Test]
        public void GroundedBurstNeverEndsUpBelowTheGround()
        {
            var w = World(); w.Burst(new V3(0, 0.5f, 0), new V3(), 1f, 3f, true, 3, false);
            for (int i = 0; i < 120 * 6; i++)
            {
                w.Step(1f / 120f);
                for (int k = 0; k < w.Bodies.Length; k++) if (w.Bodies[k].Alive) Assert.That(w.Bodies[k].P.Y - w.Bodies[k].Radius, Is.GreaterThan(-0.05f));
            }
        }

        [Test]
        public void CoolingRampGoesWhiteOrangeRedDarkAndDimsMonotonically()
        {
            float prev = float.MaxValue;
            for (float t = 1f; t >= 0f; t -= 0.01f)
            {
                var g = DebrisMath.Glow(t);
                float lum = ExplosionPlan.Luminance(g);
                Assert.That(lum, Is.LessThanOrEqualTo(prev + 1e-4f), $"t={t}");
                prev = lum;
            }
            var hot = DebrisMath.Glow(1f); var mid = DebrisMath.Glow(0.5f); var cold = DebrisMath.Glow(0f);
            Assert.That(hot.B, Is.GreaterThan(mid.B)); Assert.That(mid.R, Is.GreaterThan(mid.B * 3f)); Assert.That(ExplosionPlan.Luminance(cold), Is.LessThan(0.03f));
        }

        [Test]
        public void TemperatureDecaysExponentially()
        {
            var w = World();
            int h = w.SpawnSmall(DebrisKind.Spark, new V3(0, 50, 0), new V3(), new V3(), 0.04f, 0.02f, 1f, 0.5f, 10f, 0.4f, 0.3f, false);
            Run(w, 0.5f);
            Assert.That(w.Bodies[w.SlotOf(h)].Temp, Is.EqualTo((float)Math.Exp(-1)).Within(0.02f));
        }

        [Test]
        public void StepWithBadDtIsIgnoredAndLargeDtIsSubstepped()
        {
            var w = World(); var h = Ball(w, new V3(0, 5, 0), new V3(), collides: false);
            w.Step(float.NaN); w.Step(-1f); w.Step(0f);
            Assert.That(w.Bodies[w.SlotOf(h)].P.Y, Is.EqualTo(5f));
            w.Step(10f);   // clamped to 0.05 s, 6 substeps
            Assert.That(w.Bodies[w.SlotOf(h)].P.Y, Is.EqualTo(5f - 0.5f * 9.81f * 0.0025f).Within(0.01f));
        }

        [Test]
        public void HandlesGoStaleWhenASlotIsRecycled()
        {
            var w = World();
            var h1 = w.SpawnRigid(DebrisKind.Stage, new V3(), new V3(), Q4.Identity, new V3(), 100f, 1f, 5f, 0f, 0f, 10f);
            Assert.That(w.IsAlive(h1), Is.True);
            for (int i = 0; i < DebrisBudgets.RigidSlots; i++) w.SpawnRigid(DebrisKind.Fairing, new V3(), new V3(), Q4.Identity, new V3(), 100f, 1f, 5f, 0f, 0f, 10f);
            Assert.That(w.IsAlive(h1), Is.False);
            Assert.That(w.IsAlive(0), Is.False);
            w.Clear(); Assert.That(w.Live, Is.EqualTo(0));
        }

        [Test]
        public void SeparationEventsCarryTheirPayload()
        {
            CinematicEvents.ClearSubscribers();
            CinematicEvent got = default;
            System.Action<CinematicEvent> hnd = e => got = e;
            CinematicEvents.Raised += hnd;
            CinematicEvents.StageSeparation(1, 2, 3, 4, 5, 6, 2, 9000f, 30f, 1.8f, 22500f, 3.6f, 4f, true, 11);
            Assert.That(got.Kind, Is.EqualTo(CinematicKind.StageSeparation)); Assert.That(got.Stage, Is.EqualTo(2)); Assert.That(got.MassKg, Is.EqualTo(9000f));
            Assert.That(got.ImpulseNs, Is.EqualTo(22500f)); Assert.That(got.LeverM, Is.EqualTo(3.6f)); Assert.That(got.Reduce, Is.True); Assert.That(got.VelZ, Is.EqualTo(6f));
            CinematicEvents.FairingSeparation(0, 50, 0, 0, 0, 0, 900f, 12f, 2.2f, 1800f, 0.5f, 4f, false);
            Assert.That(got.Kind, Is.EqualTo(CinematicKind.FairingSeparation)); Assert.That(got.MassKg, Is.EqualTo(900f));
            CinematicEvents.DebrisBurst(0, 0, 0, 0, 0, 0, 0.7f, 2f, true, false, 5);
            Assert.That(got.Kind, Is.EqualTo(CinematicKind.DebrisBurst)); Assert.That(got.Grounded, Is.True); Assert.That(got.Seed, Is.EqualTo(5));
            CinematicEvents.DebrisClear(); Assert.That(got.Kind, Is.EqualTo(CinematicKind.DebrisClear));
            CinematicEvents.Raised -= hnd; CinematicEvents.ClearSubscribers();
        }
    }
}
