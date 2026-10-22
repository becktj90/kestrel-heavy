// The rail shows the stops this flight really has (#332 follow-up for Kestrel Gameplay Dev's #331):
// strap-on burnouts read BECO, the core MECO, and SECO only with a real upper stage; recovery stops follow the planned
// mode and a skipped stop never holds the marker; a flat ascent still lights Fairing and reaches the end.
using System;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class TrajectoryRailStopsTests
    {
        static TrajectoryWatch Watch(int stages, bool straps, string mode)
        {
            var w = new TrajectoryWatch();
            w.Reset();
            w.HasFairing = true;
            w.Stages = stages;
            w.StrapOns = straps;
            w.RecoveryMode = mode;
            w.FairingAltM = 110000;
            return w;
        }

        static TrajectoryEvent Ev(double t, string type, int stage, double alt, double v) =>
            new TrajectoryEvent { T = t, Type = type, Stage = stage, AltM = alt, SpeedMps = v };

        static string[] Labels(TrajectoryPlan p) => Array.ConvertAll(p.Marks, m => m.Label);

        static TrajectoryMark Mark(TrajectoryPlan p, TrajectoryStop s) => Array.Find(p.Marks, m => m.Stop == s);

        [Test]
        public void StrapOnBurnoutsReadBecoThenMecoAndSecoOnlyForAnUpperStage()
        {
            // Strap-ons on a core with an upper stage: sim stages 1 boosters, 2 core, 3 upper (AscentVehicle.Stages = 3).
            var w = Watch(3, true, null);
            Assert.That(Labels(w.At(0, 0, 0)), Is.EqualTo(new[] { "Liftoff", "Max-Q", "BECO", "MECO", "Fairing", "SECO" }));
            w.Seen(Ev(1, "liftoff", 0, 2, 5));
            w.Seen(Ev(45, "maxq", 1, 12000, 420));
            w.Seen(Ev(110, "burnout", 1, 42000, 1300));
            w.Seen(Ev(111, "separation", 1, 42000, 1300));
            w.Seen(Ev(170, "burnout", 2, 80000, 2600));
            w.Seen(Ev(171, "separation", 2, 80000, 2600));
            w.Seen(Ev(200, "fairing", 0, 112000, 2900));
            var mid = w.At(205, 115000, 3000);
            Assert.That(Mark(mid, TrajectoryStop.Beco).Passed && Mark(mid, TrajectoryStop.Beco).TimeS == 110, Is.True, "booster burnout is BECO");
            Assert.That(Mark(mid, TrajectoryStop.Meco).Passed && Mark(mid, TrajectoryStop.Meco).TimeS == 170, Is.True, "core burnout is MECO");
            Assert.That(Mark(mid, TrajectoryStop.Seco).Passed, Is.False, "the core's burnout is not SECO");
            w.Seen(Ev(480, "burnout", 3, 200000, 7600));
            var end = w.At(480, 200000, 7600);
            Assert.That(Mark(end, TrajectoryStop.Seco).Passed && Mark(end, TrajectoryStop.Seco).TimeS == 480, Is.True, "upper-stage burnout is SECO");
            Assert.That(end.Progress, Is.EqualTo(1f));

            // Strap-ons on a single core (no upper stage): BECO, MECO, no SECO tick; the core reaching orbit reads Orbit.
            var one = Watch(2, true, null);
            Assert.That(Labels(one.At(0, 0, 0)), Is.EqualTo(new[] { "Liftoff", "Max-Q", "BECO", "MECO", "Fairing" }));
            one.Seen(Ev(100, "burnout", 1, 40000, 1200));
            one.Seen(Ev(400, "burnout", 2, 180000, 7500));
            var core = one.At(400, 180000, 7500);
            Assert.That(Mark(core, TrajectoryStop.Meco).TimeS, Is.EqualTo(400), "the only core stage cuts off as MECO");
            Assert.That(Array.Exists(core.Marks, m => m.Stop == TrajectoryStop.Seco), Is.False, "no SECO without an upper stage");
            one.Seen(Ev(401, "orbit", 2, 180000, 7500));
            Assert.That(Labels(one.At(401, 180000, 7500))[5], Is.EqualTo("Orbit"));

            // No strap-ons: unchanged, stage 1 MECO, stage 2 SECO.
            var plain = Watch(2, false, null);
            plain.Seen(Ev(150, "burnout", 1, 64000, 1600));
            plain.Seen(Ev(480, "burnout", 2, 200000, 7600));
            var p = plain.At(480, 200000, 7600);
            Assert.That(Labels(p), Is.EqualTo(new[] { "Liftoff", "Max-Q", "MECO", "Fairing", "SECO" }));
            Assert.That(Mark(p, TrajectoryStop.Meco).TimeS, Is.EqualTo(150));
        }

        [Test]
        public void RecoveryStopsFollowThePlannedModeAndASkippedStopNeverHoldsTheMarker()
        {
            // Barge: no boostback planned; entry and Haven are.
            var barge = Watch(2, false, "barge");
            barge.Recovery = true;
            Assert.That(Labels(barge.At(0, 0, 0)), Is.EqualTo(new[] { "Liftoff", "Max-Q", "MECO", "Fairing", "SECO", "Entry", "Haven" }));
            var rtls = Watch(2, false, "rtls");
            rtls.Recovery = true;
            Assert.That(Labels(rtls.At(0, 0, 0)), Is.EqualTo(new[] { "Liftoff", "Max-Q", "MECO", "Fairing", "SECO", "Boostback", "Entry", "Haven" }));

            // A barge run that never needed its entry burn: Haven lights, Entry goes, the marker reaches the end.
            Ascend(barge);
            barge.RecoveryPhase("landing", 560, 0, 2);
            var down = barge.At(560, 0, 2);
            Assert.That(Labels(down), Is.EqualTo(new[] { "Liftoff", "Max-Q", "MECO", "Fairing", "SECO", "Haven" }));
            Assert.That(down.Upcoming, Is.EqualTo(-1));
            Assert.That(down.Progress, Is.EqualTo(1f), "was 0.88: an unflown Entry held the marker");

            // Unknown mode (legacy, what #331's play-through rail uses): boostback skipped, entry flown.
            var legacy = Watch(2, false, null);
            legacy.Recovery = true;
            Ascend(legacy);
            legacy.RecoveryPhase("entry", 600, 40000, 1200);
            var entering = legacy.At(610, 35000, 1100);
            Assert.That(Array.Exists(entering.Marks, m => m.Stop == TrajectoryStop.Boostback), Is.False, "a skipped boostback goes once entry lights");
            Assert.That(entering.Marks[entering.Upcoming].Label, Is.EqualTo("Haven"));
            legacy.RecoveryPhase("landing", 700, 0, 2);
            var landed = legacy.At(700, 0, 2);
            Assert.That(landed.Progress, Is.EqualTo(1f));
            Assert.That(landed.Marks[landed.Marks.Length - 1].Label, Is.EqualTo("Haven"));
            Assert.That(landed.Marks[landed.Marks.Length - 1].Passed, Is.True);

            // A barge booster that did fly a boostback trim shows it.
            var trim = Watch(2, false, "barge");
            trim.Recovery = true;
            trim.RecoveryPhase("boostback", 170, 70000, 1500);
            Assert.That(Array.Exists(trim.At(171, 70000, 1500).Marks, m => m.Stop == TrajectoryStop.Boostback), Is.True);
        }

        [Test]
        public void AFlatAscentStillLightsFairingAndTheMarkerNeverFreezes()
        {
            // Cutoff at 92 km, never 110 km: no altitude jettison, no director call (it arms at JettisonAltM - 12 km).
            var w = Watch(2, false, null);
            w.Seen(Ev(1, "liftoff", 0, 2, 5));
            w.Seen(Ev(50, "maxq", 1, 11000, 450));
            w.Seen(Ev(150, "burnout", 1, 55000, 2100));
            float last = w.At(150, 55000, 2100).Progress;
            for (double t = 160; t < 420; t += 10)
            {
                double alt = Math.Min(92000, 55000 + (t - 150) * 300);
                w.Sample(t, alt, 2100 + (t - 150) * 20, 50);
                w.Burn(2, 1 - (t - 155) / 270.0);   // the upper stage burns from T+155 to cutoff at T+425
                var p = w.At(t, alt, 2100);
                // Visibly: at least a thousandth of the rail every 10 s (a float-epsilon creep is a freeze on screen).
                Assert.That(p.Progress - last, Is.GreaterThan(0.001f), "T+" + t + " the marker keeps moving");
                Assert.That(Mark(p, TrajectoryStop.Fairing).Passed, Is.False, "nothing lights Fairing before cutoff on a flat ascent");
                last = p.Progress;
            }
            w.Seen(Ev(420, "burnout", 2, 92000, 7500));
            var cut = w.At(420, 92000, 7500);
            var fairing = Mark(cut, TrajectoryStop.Fairing);
            Assert.That(fairing.Passed, Is.True, "the payload is out at cutoff");
            Assert.That(fairing.TimeS, Is.EqualTo(420));
            Assert.That(Mark(cut, TrajectoryStop.Seco).Passed, Is.True);
            Assert.That(cut.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void EveryStopFitsTheRailAtEveryPhoneInBudgetWidth()
        {
            // The longest rail: strap-ons, an upper stage and an RTLS return, every stop lit with numbers.
            var w = Watch(3, true, "rtls");
            w.Recovery = true;
            w.Seen(Ev(1, "liftoff", 0, 2, 5));
            w.Seen(Ev(45, "maxq", 1, 12000, 420));
            w.Seen(Ev(110, "burnout", 1, 42000, 1300));
            w.Seen(Ev(170, "burnout", 2, 80000, 2600));
            w.Seen(Ev(200, "fairing", 0, 112000, 2900));
            w.Seen(Ev(480, "burnout", 3, 200000, 7600));
            w.RecoveryPhase("boostback", 500, 120000, 1500);
            w.RecoveryPhase("entry", 650, 50000, 1200);
            w.RecoveryPhase("landing", 760, 0, 2);
            var plan = w.At(760, 0, 2);
            Assert.That(plan.Marks.Length, Is.EqualTo(9));
            foreach (var d in FlightHudLayout.PortraitDevices())
            foreach (var mode in new[] { FlightHudMode.Cruise, FlightHudMode.Notes, FlightHudMode.Prompt, FlightHudMode.PromptNotes })
            {
                var frame = FlightHudLayout.Place(d, mode);
                var local = new HudBox { X = 0, Y = 0, W = frame.Plot.W, H = frame.Plot.H };
                var tags = TrajectoryWatch.Tags(local, plan.Marks);
                string where = d.Name + " " + mode;
                Assert.That(tags.Length, Is.EqualTo(9), where);
                for (int i = 0; i < tags.Length; i++)
                {
                    Assert.That(tags[i].Box.Right, Is.LessThanOrEqualTo(local.W + 0.6f), where);
                    Assert.That(tags[i].Box.Y, Is.GreaterThanOrEqualTo(-0.6f), where);
                    Assert.That(tags[i].Box.Bottom, Is.LessThanOrEqualTo(local.H + 0.6f), where);
                    foreach (var line in tags[i].Lines)
                        Assert.That(FlightHudLayout.BudgetWidth(line, tags[i].FontPt), Is.LessThanOrEqualTo(tags[i].Box.W + 0.6f), where + " clips " + line);
                    if (i > 0) Assert.That(tags[i].Box.Overlaps(tags[i - 1].Box), Is.False, where + " " + tags[i].Lines[0]);
                }
            }
        }

        static void Ascend(TrajectoryWatch w)
        {
            w.Seen(Ev(1, "liftoff", 0, 2, 5));
            w.Seen(Ev(45, "maxq", 1, 12000, 420));
            w.Seen(Ev(150, "burnout", 1, 64000, 1600));
            w.Seen(Ev(200, "fairing", 0, 112000, 2900));
            w.Seen(Ev(480, "burnout", 2, 200000, 7600));
        }
    }
}
