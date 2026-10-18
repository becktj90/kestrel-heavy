using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class ShockwavePlanTests
    {
        static ShockwaveEvent Ev(float energy, bool grounded = true, bool reduce = false) => new ShockwaveEvent { Energy01 = energy, Radius = 2f, Grounded = grounded, Reduce = reduce };
        // Elevation only: this file must not name the LaunchTimeOfDay enum, which moves from Kit to Core in #275.
        static readonly TimeOfDayPreset DayP = new TimeOfDayPreset { SunElevationDeg = 40f }, NightP = new TimeOfDayPreset { SunElevationDeg = -20f };
        static TimeOfDayPreset Tod(int night) => night == 0 ? DayP : NightP;

        [Test]
        public void BudgetsAreGatedByTier()
        {
            Assert.That(new[] { ShockwaveBudgets.Rings(0), ShockwaveBudgets.Rings(1), ShockwaveBudgets.Rings(2) }, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(new[] { ShockwaveBudgets.HazeCards(0), ShockwaveBudgets.HazeCards(1), ShockwaveBudgets.HazeCards(2) }, Is.EqualTo(new[] { 0, 2, 4 }));
            Assert.That(ShockwavePlan.Build(Ev(1f), 0, Tod(0)).Draw, Is.False, "off on Low");
            Assert.That(ShockwavePlan.Build(Ev(1f), 1, Tod(0)).Draw, Is.True);
        }

        [Test]
        public void ReduceMotionGivesNoRingAndNoHaze()
        {
            foreach (var t in new[] { 0, 1 })
                for (int q = 0; q <= 2; q++)
                {
                    Assert.That(ShockwavePlan.Build(Ev(1f, true, true), q, Tod(t)).Draw, Is.False);
                    Assert.That(ShockwavePlan.HazeAlpha(new HazeEvent { Intensity01 = 1f, Reduce = true }, q, Tod(t)), Is.EqualTo(0f));
                }
            Assert.That(ShockwavePlan.HazeAlpha(new HazeEvent { Intensity01 = 1f }, 0, null), Is.EqualTo(0f), "off on Low");
        }

        [Test]
        public void RadiusScalesWithTheCubeRootOfEnergy()
        {
            var a = ShockwavePlan.Build(Ev(0.125f), 2, null); var b = ShockwavePlan.Build(Ev(1f), 2, null);
            Assert.That(b.MaxRadius / a.MaxRadius, Is.EqualTo(2f).Within(0.01f));
            Assert.That(ShockwavePlan.Build(Ev(0.01f), 2, null).Draw, Is.False, "a spark is not a blast wave");
            Assert.That(b.PeakAlpha, Is.GreaterThan(a.PeakAlpha));
        }

        [Test]
        public void AlphaIsCappedRampsOverSixtyMsAndOnlyFalls()
        {
            foreach (var t in new[] { 0, 1 })
            {
                var p = ShockwavePlan.Build(Ev(5f), 2, Tod(t));
                Assert.That(p.PeakAlpha, Is.LessThanOrEqualTo(ShockwaveBudgets.MaxRingAlpha + 1e-5f));
                float prev = 0f; bool falling = false; int rise = 0;
                for (float s = 0f; s <= p.Duration + 0.1f; s += 1f / 120f)
                {
                    float a = p.AlphaAt(s);
                    Assert.That(a, Is.InRange(0f, ShockwaveBudgets.MaxRingAlpha));
                    if (a > prev + 1e-6f) { Assert.That(falling, Is.False, "single peak"); rise++; }
                    if (a < prev - 1e-6f) falling = true;
                    Assert.That(a - prev, Is.LessThan(ShockwaveBudgets.MaxRingAlpha / 5f), "no one-frame jump");
                    prev = a;
                }
                Assert.That(rise, Is.GreaterThanOrEqualTo(5)); Assert.That(prev, Is.EqualTo(0f));
            }
        }

        [Test]
        public void RingExpandsMonotonicallyToItsMaxRadius()
        {
            var p = ShockwavePlan.Build(Ev(1f), 2, null);
            float prev = 0f;
            for (float s = 0f; s <= p.Duration; s += 0.01f) { float r = p.RadiusAt(s); Assert.That(r, Is.GreaterThanOrEqualTo(prev)); prev = r; }
            Assert.That(p.RadiusAt(p.Duration), Is.EqualTo(p.MaxRadius).Within(1e-3f));
            Assert.That(p.RadiusAt(0f), Is.EqualTo(0f));
        }

        [Test]
        public void NightRingIsWarmerAndDimmerAndHazeFadesInTheDark()
        {
            var d = ShockwavePlan.Build(Ev(1f), 2, Tod(0)); var n = ShockwavePlan.Build(Ev(1f), 2, Tod(1));
            Assert.That(n.Color.B, Is.LessThan(d.Color.B)); Assert.That(n.PeakAlpha, Is.LessThan(d.PeakAlpha));
            var h = new HazeEvent { Intensity01 = 1f };
            float hd = ShockwavePlan.HazeAlpha(h, 2, Tod(0)), hn = ShockwavePlan.HazeAlpha(h, 2, Tod(1));
            Assert.That(hn, Is.LessThan(hd)); Assert.That(hd, Is.LessThanOrEqualTo(ShockwaveBudgets.MaxHazeAlpha));
        }

        [Test]
        public void EventsReachSubscribersWithTheirPayload()
        {
            CinematicEvents.ClearShockwaveSubscribers();
            ShockwaveEvent s = default; HazeEvent h = default;
            System.Action<ShockwaveEvent> a = e => s = e; System.Action<HazeEvent> b = e => h = e;
            CinematicEvents.ShockwaveRaised += a; CinematicEvents.HazeRaised += b;
            CinematicEvents.Shockwave(1, 2, 3, 0.5f, true, false, 3f); CinematicEvents.HeatHaze(4, 5, 6, 0.7f, 2f, true);
            Assert.That(s.X, Is.EqualTo(1f)); Assert.That(s.Grounded, Is.True); Assert.That(s.Radius, Is.EqualTo(3f));
            Assert.That(h.Z, Is.EqualTo(6f)); Assert.That(h.Reduce, Is.True); Assert.That(h.Seconds, Is.EqualTo(2f));
            CinematicEvents.ClearShockwaveSubscribers();
        }
    }
}
