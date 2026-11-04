// PR5 (UX #416 item 6): coast time-warp. The policy, and the proof that warp changes only how many fixed steps run per
// frame: a warped flight ends bit-identical to the unwarped pad loop.
using System;
using System.Collections.Generic;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CoastWarpTests
    {
        static CoastWarpState Clear(double next) => new CoastWarpState { SecondsToNext = next };

        [Test]
        public void FactorFollowsTheClearTimeAhead()
        {
            Assert.That(CoastWarp.Factor(Clear(double.PositiveInfinity)), Is.EqualTo(10));
            Assert.That(CoastWarp.Factor(Clear(CoastWarp.FarS)), Is.EqualTo(10));
            Assert.That(CoastWarp.Factor(Clear(CoastWarp.FarS - 0.01)), Is.EqualTo(4));
            Assert.That(CoastWarp.Factor(Clear(CoastWarp.LeadS)), Is.EqualTo(4));
            Assert.That(CoastWarp.Factor(Clear(CoastWarp.LeadS - 0.01)), Is.EqualTo(1));
            Assert.That(CoastWarp.Factor(Clear(0)), Is.EqualTo(1));
            Assert.That(CoastWarp.Factor(Clear(double.NaN)), Is.EqualTo(1));
        }

        [Test]
        public void AnyBlockerHoldsRealTime()
        {
            var s = Clear(1000); s.OpenCalls = 1; Assert.That(CoastWarp.Factor(s), Is.EqualTo(1), "open call");
            s = Clear(1000); s.CameraOnBooster = true; Assert.That(CoastWarp.Factor(s), Is.EqualTo(1), "booster camera");
            s = Clear(1000); s.SlowMo = true; Assert.That(CoastWarp.Factor(s), Is.EqualTo(1), "slow-mo");
            s = Clear(1000); s.Disabled = true; Assert.That(CoastWarp.Factor(s), Is.EqualTo(1), "player off");
        }

        [Test]
        public void StepCapCoversTenTimesAHitchFrame()
        {
            Assert.That(CoastWarp.StepCap(1), Is.EqualTo(64), "unwarped cap unchanged");
            int need = (int)Math.Ceiling(0.1 * CoastWarp.Fast / MissionFlightSetup.LiveDt);
            Assert.That(CoastWarp.StepCap(CoastWarp.Fast), Is.GreaterThanOrEqualTo(need));
            Assert.That(CoastWarp.StepCap(CoastWarp.Fast), Is.LessThanOrEqualTo(200), "sane per-frame budget");
        }

        [Test]
        public void UntilIsTimeToCloseTheGap()
        {
            Assert.That(CoastWarp.Until(10, 2), Is.EqualTo(5));
            Assert.That(CoastWarp.Until(-1, 2), Is.EqualTo(0));
            Assert.That(CoastWarp.Until(10, -2), Is.EqualTo(double.PositiveInfinity));
            Assert.That(CoastWarp.Until(10, double.NaN), Is.EqualTo(double.PositiveInfinity));
        }

        sealed class Flown
        {
            public SimResult Result;
            public double[] Y;
            public AscentFlightScore Score;
            public int Frames;
            public double MaxFactor;
            public List<string> WarpedEvents = new List<string>();
            public double WallS;
            public List<string> Log = new List<string>();
            public int Forced;
        }

        static (DirectedFlight flight, PlayedChecks checks, double fairingAlt) Build(string missionId)
        {
            var c = StarterFixture.Catalog;
            var m = StarterFixture.Campaign.Missions.Find(x => x.Id == missionId);
            var d = Design.FromJson(StarterFixture.Rocket(m.Id).BuildDesign());
            ulong hash = FlightBatch.DesignHash(d);
            double target = Stack.RequiredDeltaV(c, d.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(m)) target = m.GuidanceAltM;
            var monitor = FailureMonitor.Engages(d) ? FailureMonitor.Create(c, d, target, hash, 1) : null;
            var director = MissionFlightSetup.Director(c, d, monitor?.Events, null, FailureMonitor.StructuralQLimPa(c, d),
                FailureMonitor.GentleTier(d), target, hash, 1, true, false, false, null);
            var setup = MissionFlightSetup.Build(c, m, d, MissionFlightSetup.LiveDt, null);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            return (new DirectedFlight(sim, director, opts), new PlayedChecks(c, d, setup.Stages, opts, sim), opts.fairingJettisonAltM);
        }

        /// <summary>The pre-PR5 FlightDriver loop, verbatim: 1/60 s frames, ×1, 64-step guard.</summary>
        static Flown FlyOld(string id)
        {
            var (flight, checks, _) = Build(id);
            var sim = flight.Sim;
            double clock = 0; int frames = 0;
            while (!sim.Done && frames < 2000000)
            {
                frames++;
                clock += Math.Min(1.0 / 60.0, 0.1);
                int guard = 0;
                while (sim.T < clock && !sim.Done && guard++ < 64)
                {
                    int before = sim.Events.Count;
                    flight.Step();
                    checks.NoteEvents(sim, before);
                    if (checks.StopForBreakup(sim)) break;
                }
            }
            return new Flown { Result = flight.Finish(), Y = (double[])sim.Y.Clone(), Score = flight.Director.Score(), Frames = frames, WallS = frames / 60.0 };
        }

        static Flown FlyWarped(string id, Func<int, double> frame, Func<int, bool> narrationBusy = null)
        {
            var (flight, checks, fairingAlt) = Build(id);
            var sim = flight.Sim;
            var clock = new WarpedFlightClock(flight, checks, fairingAlt);
            var f = new Flown();
            clock.WarpChanged += (a, b, t) => f.Log.Add("C " + a + " " + b);
            clock.WarpWillDrop += (a, r) => f.Log.Add("W " + a + " " + r.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            while (!sim.Done && f.Frames < 2000000)
            {
                double s = frame(f.Frames++);
                f.WallS += s;
                int before = sim.Events.Count, open = flight.Director.OpenCount;
                clock.Advance(s, 1, false, narrationBusy != null && narrationBusy(f.Frames));
                if (clock.Factor > f.MaxFactor) f.MaxFactor = clock.Factor;
                if (clock.Factor > 1)
                {
                    for (int i = before; i < sim.Events.Count; i++) f.WarpedEvents.Add(sim.Events[i].type + "@" + sim.Events[i].t.ToString("0.00"));
                    if (flight.Director.OpenCount > open) f.WarpedEvents.Add("call@" + sim.T.ToString("0.00"));
                }
            }
            f.Forced = clock.ForcedDrops;
            f.Result = flight.Finish(); f.Y = (double[])sim.Y.Clone(); f.Score = flight.Director.Score();
            return f;
        }

        static double Jitter(int i)
        {
            // A deterministic mix of 144 Hz, 60 Hz, 30 Hz frames and 0.25 s hitches (clamped to 0.1 s).
            switch (i % 7) { case 0: return 1.0 / 144; case 1: return 1.0 / 60; case 2: return 1.0 / 30; case 3: return 0.25; case 4: return 1.0 / 144; case 5: return 1.0 / 90; default: return 1.0 / 60; }
        }

        static void AssertBitIdentical(Flown a, Flown b, string what)
        {
            Assert.That(b.Result.outcome, Is.EqualTo(a.Result.outcome), what + " outcome");
            Assert.That(b.Y.Length, Is.EqualTo(a.Y.Length));
            for (int i = 0; i < a.Y.Length; i++)
                Assert.That(BitConverter.DoubleToInt64Bits(b.Y[i]), Is.EqualTo(BitConverter.DoubleToInt64Bits(a.Y[i])), what + " Y[" + i + "]");
            Assert.That(b.Result.events.Count, Is.EqualTo(a.Result.events.Count), what + " events");
            for (int i = 0; i < a.Result.events.Count; i++)
            {
                var ea = a.Result.events[i]; var eb = b.Result.events[i];
                Assert.That(eb.type, Is.EqualTo(ea.type), what + " event " + i);
                Assert.That(BitConverter.DoubleToInt64Bits(eb.t), Is.EqualTo(BitConverter.DoubleToInt64Bits(ea.t)), what + " event t " + i);
            }
            CompareFields(a.Result, b.Result, what + " result");
            CompareFields(a.Result.orbit, b.Result.orbit, what + " orbit");
            CompareFields(a.Score, b.Score, what + " score");
        }

        static void CompareFields(object a, object b, string what)
        {
            if (a == null || b == null) { Assert.That(b == null, Is.EqualTo(a == null), what); return; }
            foreach (var f in a.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var va = f.GetValue(a); var vb = f.GetValue(b);
                if (va is double da) Assert.That(BitConverter.DoubleToInt64Bits((double)vb), Is.EqualTo(BitConverter.DoubleToInt64Bits(da)), what + "." + f.Name);
                else if (va is int || va is bool || va is string || va is Enum) Assert.That(vb, Is.EqualTo(va), what + "." + f.Name);
            }
        }

        // Two, three and four stage Starter defaults, with a fairing and the full call set.
        [TestCase("m3")]
        [TestCase("m6")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void WarpedFlightIsBitIdentical(string id)
        {
            var old = FlyOld(id);
            var warped = FlyWarped(id, _ => 1.0 / 60);
            var jitter = FlyWarped(id, Jitter);
            Assert.That(warped.MaxFactor, Is.EqualTo(CoastWarp.Fast), "the flight actually warped");
            Assert.That(warped.Frames, Is.LessThan(old.Frames / 2), "warp shortens the flight");
            AssertBitIdentical(old, warped, id + " 60 Hz warped");
            AssertBitIdentical(old, jitter, id + " jittered warped");
        }

        [TestCase("m3")]
        [TestCase("m6")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void EveryEventAndCallLandsAtRealTime(string id)
        {
            var f = FlyWarped(id, _ => 1.0 / 60);
            Assert.That(f.WarpedEvents, Is.Empty, id + ": events or calls inside a warped frame");
        }
    
        [TestCase("m3")]
        [TestCase("m6")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void NarrationHoldStaysBitIdentical(string id)
        {
            var old = FlyOld(id);
            // A non-interruptible line every ~8 s of real time, 2 s long.
            var held = FlyWarped(id, _ => 1.0 / 60, i => i % 480 < 120);
            AssertBitIdentical(old, held, id + " narration hold");
            Assert.That(held.Frames, Is.GreaterThan(FlyWarped(id, _ => 1.0 / 60).Frames), "the hold slows the flight");
        }

        /// <summary>UX: ×10 steps down through ×4 before ×1 wherever the clock sees what is ahead, ×1 at least
        /// <see cref="CoastWarp.LeadS"/> sim seconds before every call and event. Sound: WarpWillDrop comes before every
        /// WarpChanged(x→1), about 0.5 s of real time ahead; WarpChanged only on a real change.</summary>
        [TestCase("m3")]
        [TestCase("m6")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void StepsDownThroughFourAndWarnsBeforeEveryDrop(string id)
        {
            var f = FlyWarped(id, _ => 1.0 / 60);
            Assert.That(f.Log, Does.Contain("C 10 4"), id + " steps down through ×4");
            Assert.That(f.Log, Has.None.EqualTo("C 10 1"), id + " never drops ×10 straight to ×1");
            Assert.That(f.Forced, Is.EqualTo(0), id + " every drop was foreseen");
            bool warned = false; int drops = 0, shortWarn = 0;
            foreach (var e in f.Log)
            {
                var p = e.Split(' ');
                if (p[0] == "W")
                {
                    float r = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
                    Assert.That(r, Is.InRange(0f, (float)CoastWarp.WillDropLeadRealS + 1e-4f), e);
                    Assert.That(r, Is.GreaterThan(0f), id + " a foreseen drop warns ahead: " + e);
                    if (r < 0.4f) shortWarn++; // the estimate stepped late (m19 has one at ~0.14 s; documented in DECISIONS #79)
                    warned = true;
                    continue;
                }
                Assert.That(p[1], Is.Not.EqualTo(p[2]), "WarpChanged only on a change: " + e);
                if (p[2] == "1") { Assert.That(warned, Is.True, id + " WarpWillDrop before " + e); drops++; }
                warned = false;
            }
            Assert.That(drops, Is.GreaterThan(0));
            Assert.That(shortWarn, Is.LessThanOrEqualTo(1), id + " drops warned well under 0.5 s");
        }

        [Test]
        public void UnforeseenDropWarnsWithZeroFirst()
        {
            var (flight, checks, fairingAlt) = Build("m6");
            var clock = new WarpedFlightClock(flight, checks, fairingAlt);
            var log = new List<string>();
            clock.WarpChanged += (a, b, t) => log.Add("C " + a + " " + b);
            clock.WarpWillDrop += (a, r) => log.Add("W " + a + " " + r);
            while (clock.Factor < CoastWarp.Fast && !flight.Sim.Done) clock.Advance(1.0 / 60, 1, false);
            log.Clear();
            clock.Advance(1.0 / 60, 1, true); // the camera swings to the booster: a hard blocker
            Assert.That(log, Is.EqualTo(new[] { "W 10 0", "C 10 1" }));
            Assert.That(clock.ForcedDrops, Is.EqualTo(1));
        }

        [Test]
        public void NarrationBusyHoldsRealTime()
        {
            var s = Clear(1000); s.NarrationBusy = true;
            Assert.That(CoastWarp.Factor(s), Is.EqualTo(1));
        }

        [Test]
        public void NarrationHoldKeepsHalfASecondTail()
        {
            var h = new NarrationWarpHold();
            Assert.That(h.Holds(0), Is.False);
            h.Note(true, 10); Assert.That(h.Holds(10), Is.True);
            h.Note(true, 12); h.Note(false, 12.1);
            Assert.That(h.Holds(12.4), Is.True, "inside the 0.5 s tail");
            Assert.That(h.Holds(12.51), Is.False, "tail over");
            h.Note(false, 20); Assert.That(h.Holds(20), Is.False, "interruptible lines never hold");
        }

        [Test]
        public void LineStartsOnlyWhenItFitsBeforeTheNextCallAtTheCurrentWarpWithWarpOff()
        {
            // 6 s line + 1.5 s tail = 7.5 wall s. Warp off: the factor given is the one the clock keeps.
            Assert.That(CoastWarp.LineFits(6, 75, 10, false), Is.True, "75 sim s at ×10 = 7.5 wall s");
            Assert.That(CoastWarp.LineFits(6, 74, 10, false), Is.False);
            Assert.That(CoastWarp.LineFits(6, 30, 4, false), Is.True);
            Assert.That(CoastWarp.LineFits(6, 29, 4, false), Is.False);
            Assert.That(CoastWarp.LineFits(6, 7.5, 1, false), Is.True);
            Assert.That(CoastWarp.LineFits(6, 0, 1, false), Is.False, "nothing fits a call due now: silence");
            Assert.That(CoastWarp.LineFits(6, double.PositiveInfinity, 10, false), Is.True, "nothing ahead");
            Assert.That(CoastWarp.LineFits(6, double.NaN, 1, false), Is.False);
        }

        /// <summary>
        /// Narrator Voice on #436: during a brief ×1 (the 0.5 s tail, the booster camera, an open call) the fit is measured
        /// at the factor the clock returns to, so a 10 s or longer color line is not picked into a gap warp then cuts.
        /// </summary>
        [Test]
        public void LongColorLineIsNotPickedIntoAGapWarpWillShrink()
        {
            Assert.That(CoastWarp.ResumeFactor(4.9), Is.EqualTo(CoastWarp.Off));
            Assert.That(CoastWarp.ResumeFactor(15), Is.EqualTo(CoastWarp.Slow));
            Assert.That(CoastWarp.ResumeFactor(30), Is.EqualTo(CoastWarp.Fast));
            // 15 sim s at a brief ×1 looked like 15 wall s; warp resumes ×4, so it is 3.75 wall s.
            Assert.That(CoastWarp.LineFits(10, 15, CoastWarp.Off), Is.False, "10 s color into 15 sim s at a brief ×1");
            Assert.That(CoastWarp.LineFits(10, 15, CoastWarp.Off, false), Is.True, "warp off: ×1 is where the clock stays");
            Assert.That(CoastWarp.LineFits(10, 100, CoastWarp.Off), Is.False, "100 sim s resumes ×10: 10 wall s");
            Assert.That(CoastWarp.LineFits(10, 115, CoastWarp.Off), Is.True, "115 sim s at ×10 = 11.5 wall s");
            Assert.That(CoastWarp.LineFits(10, 46, CoastWarp.Slow), Is.False, "×4 now, ×10 next: the larger wins");
            Assert.That(CoastWarp.LineFits(2, 4, CoastWarp.Off), Is.True, "under the lead the clock stays ×1");
        }

        [Test]
        public void TimeWarpSettingIsAutoUnlessSavedOff()
        {
            Assert.That(TimeWarpSetting.Pref, Is.EqualTo("kh.flight.timewarp"));
            Assert.That(TimeWarpSetting.Resolve(false, 0), Is.EqualTo(TimeWarpMode.Auto));
            Assert.That(TimeWarpSetting.Resolve(true, 0), Is.EqualTo(TimeWarpMode.Auto));
            Assert.That(TimeWarpSetting.Resolve(true, 1), Is.EqualTo(TimeWarpMode.Off));
            CollectionAssert.Contains(Kestrel.Core.Save.SettingsMirror.Keys, TimeWarpSetting.Pref);
        }

        [Test]
        public void ChipAndAccessibleText()
        {
            Assert.That(CoastWarp.ChipText(10), Is.EqualTo("⏩ ×10"));
            Assert.That(CoastWarp.ChipText(4), Is.EqualTo("⏩ ×4"));
            Assert.That(CoastWarp.ChipText(1), Is.EqualTo(""));
            Assert.That(CoastWarp.AccessibleText(10), Is.EqualTo("Time warp, 10 times"));
            Assert.That(CoastWarp.AccessibleText(4), Is.EqualTo("Time warp, 4 times"));
        }
}
}
