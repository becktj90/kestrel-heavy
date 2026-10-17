// Milestone order, live progress, and labels that stay plain and inside the rail.
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class TrajectoryLineTests
    {
        static readonly TrajectoryStop[] Ascent =
        {
            TrajectoryStop.Liftoff, TrajectoryStop.MaxQ, TrajectoryStop.Meco, TrajectoryStop.Fairing, TrajectoryStop.Seco,
        };

        static readonly TrajectoryStop[] Recovery =
        {
            TrajectoryStop.Boostback, TrajectoryStop.Entry, TrajectoryStop.Landing,
        };

        [Test]
        public void AscentOrderIsBottomToTopAndRecoveryComesAfter()
        {
            var watch = Flight();
            var bare = watch.At(0, 0, 0);
            Assert.That(Stops(bare), Is.EqualTo(Ascent));
            Assert.That(bare.Marks[0].Along, Is.EqualTo(0f));
            Assert.That(bare.Marks[bare.Marks.Length - 1].Along, Is.EqualTo(1f));
            Assert.That(bare.Marks[0].Label, Is.EqualTo("Liftoff"));
            Assert.That(bare.Upcoming, Is.EqualTo(0));
            Assert.That(bare.Progress, Is.EqualTo(0f));

            watch.Recovery = true;
            var back = watch.At(0, 0, 0);
            var both = new TrajectoryStop[Ascent.Length + Recovery.Length];
            Ascent.CopyTo(both, 0);
            Recovery.CopyTo(both, Ascent.Length);
            Assert.That(Stops(back), Is.EqualTo(both));
            Assert.That(back.Marks[back.Marks.Length - 1].Label, Is.EqualTo("Haven"));
        }

        [Test]
        public void FairingAndRecoveryStayOffWhenTheyDoNotApply()
        {
            var watch = Flight();
            watch.HasFairing = false;
            watch.Stages = 1;
            watch.Recovery = false;
            var plan = watch.At(0, 0, 0);
            Assert.That(Stops(plan), Is.EqualTo(new[] { TrajectoryStop.Liftoff, TrajectoryStop.MaxQ, TrajectoryStop.Meco }));
        }

        [Test]
        public void ProgressWalksTheLineAndPassedTicksCarryTheRealNumbers()
        {
            var watch = Flight();
            watch.Seen(Ev(0.4, "liftoff", 0, 2, 8));
            var lifted = watch.At(10, 400, 80);
            Assert.That(lifted.Marks[0].Passed, Is.True);
            Assert.That(lifted.Marks[0].HasNumbers, Is.True);
            Assert.That(lifted.Marks[0].Clock, Is.EqualTo("T+0:00"));
            Assert.That(lifted.Marks[0].Altitude, Is.EqualTo("2 m"));
            Assert.That(lifted.Marks[0].Speed, Is.EqualTo("8 m/s"));
            Assert.That(lifted.Marks[1].Upcoming, Is.True);
            Assert.That(lifted.Marks[1].HasNumbers, Is.False);
            Assert.That(lifted.Progress, Is.GreaterThan(0f));
            Assert.That(lifted.Progress, Is.LessThan(lifted.Marks[1].Along));

            float early = lifted.Progress;
            var later = watch.At(30, 4000, 300);
            Assert.That(later.Progress, Is.GreaterThan(early));
            Assert.That(later.Progress, Is.LessThan(later.Marks[1].Along));

            watch.Sample(40, 11000, 420, 28000);
            watch.Sample(55, 14000, 500, 18000);
            var hot = watch.At(55, 14000, 500);
            Assert.That(hot.Marks[1].Passed, Is.True, "max-Q");
            Assert.That(hot.Marks[1].Label, Is.EqualTo("Max-Q"));
            Assert.That(hot.Marks[1].TimeS, Is.EqualTo(40).Within(0.01));
            Assert.That(hot.Marks[1].AltM, Is.EqualTo(11000).Within(1));
            Assert.That(hot.Marks[1].SpeedMps, Is.EqualTo(420).Within(1));
            Assert.That(hot.Marks[1].Altitude, Is.EqualTo("11 km"));
            Assert.That(hot.Marks[1].Speed, Is.EqualTo("420 m/s"));
            Assert.That(hot.Marks[2].Upcoming, Is.True);
            Assert.That(hot.Progress, Is.GreaterThan(hot.Marks[1].Along));
            Assert.That(hot.Progress, Is.LessThan(hot.Marks[2].Along));

            watch.Seen(Ev(150, "burnout", 1, 64000, 1600));
            watch.Seen(Ev(151, "separation", 1, 64000, 1600));
            watch.Seen(Ev(180, "fairing", 2, 90000, 2200));
            watch.Seen(Ev(480, "orbit", 2, 200000, 7600));
            var orbited = watch.At(480, 200000, 7600);
            Assert.That(orbited.Marks[2].Label, Is.EqualTo("MECO"));
            Assert.That(orbited.Marks[2].Clock, Is.EqualTo("T+2:30"));
            Assert.That(orbited.Marks[2].Speed, Is.EqualTo("1.6 km/s"));
            Assert.That(orbited.Marks[3].Label, Is.EqualTo("Fairing"));
            Assert.That(orbited.Marks[4].Label, Is.EqualTo("Orbit"));
            Assert.That(orbited.Marks[4].Altitude, Is.EqualTo("200 km"));
            Assert.That(orbited.Marks[4].Speed, Is.EqualTo("7.6 km/s"));
            Assert.That(orbited.Progress, Is.EqualTo(1f));
            Assert.That(orbited.Upcoming, Is.EqualTo(-1));
        }

        [Test]
        public void RecoveryPhasesLightInOrderAfterOrbit()
        {
            var watch = Flight();
            watch.Recovery = true;
            watch.Seen(Ev(1, "liftoff", 0, 3, 10));
            watch.Seen(Ev(40, "maxq", 1, 12000, 400));
            watch.Seen(Ev(150, "meco", 1, 66000, 1800));
            watch.Seen(Ev(190, "fairing", 2, 100000, 2500));
            watch.Seen(Ev(500, "seco", 2, 200000, 7500));
            var coast = watch.At(520, 190000, 1700);
            Assert.That(coast.Marks[4].Label, Is.EqualTo("SECO"));
            Assert.That(coast.Marks[5].Upcoming, Is.True);
            Assert.That(coast.Marks[5].Label, Is.EqualTo("Boostback"));
            Assert.That(coast.Progress, Is.GreaterThan(coast.Marks[4].Along));
            Assert.That(coast.Progress, Is.LessThan(coast.Marks[5].Along));

            watch.RecoveryPhase("boostback", 520, 180000, 1600);
            watch.RecoveryPhase("entry", 700, 70000, 1400);
            watch.RecoveryPhase("landing", 780, 0, 2);
            var down = watch.At(780, 0, 2);
            Assert.That(down.Marks[5].Passed && down.Marks[6].Passed && down.Marks[7].Passed, Is.True);
            Assert.That(down.Marks[7].Label, Is.EqualTo("Haven"));
            Assert.That(down.Marks[7].Clock, Is.EqualTo("T+13:00"));
            Assert.That(down.Progress, Is.EqualTo(1f));
            Assert.That(Stops(down)[5], Is.EqualTo(TrajectoryStop.Boostback));
            Assert.That(Stops(down)[6], Is.EqualTo(TrajectoryStop.Entry));
            Assert.That(Stops(down)[7], Is.EqualTo(TrajectoryStop.Landing));
        }

        [Test]
        public void LabelsHaveNoRawIdsAndFitSeProMaxAndIpad()
        {
            var watch = Flight();
            watch.Recovery = true;
            watch.Seen(Ev(0.2, "tl.asc.liftoff.short", 0, 2, 8));
            var ignored = watch.At(0.2, 2, 8);
            Assert.That(ignored.Marks[0].Passed, Is.False, "an unknown id is not liftoff");
            Assert.That(ignored.Marks[0].Label, Is.EqualTo("Liftoff"));
            watch.Seen(Ev(1, "liftoff", 0, 2, 8));
            watch.Seen(Ev(46, "max-q", 1, 12000, 800));
            watch.Seen(Ev(160, "meco", 1, 70000, 2300));
            watch.Seen(Ev(200, "fairing", 2, 110000, 2800));
            watch.Seen(Ev(510, "seco", 2, 200000, 7600));
            watch.RecoveryPhase("boostback", 530, 160000, 1500);
            watch.RecoveryPhase("entry", 710, 55000, 1200);
            watch.RecoveryPhase("landing", 790, 20, 4);
            var plan = watch.At(790, 20, 4);
            foreach (var mark in plan.Marks)
            {
                Assert.That(TrajectoryWatch.Plain(mark.Label), Is.True, mark.Label);
                Assert.That(TrajectoryWatch.Plain(mark.Clock), Is.True, mark.Clock);
                Assert.That(TrajectoryWatch.Plain(mark.Altitude), Is.True, mark.Altitude);
                Assert.That(TrajectoryWatch.Plain(mark.Speed), Is.True, mark.Speed);
                Assert.That(mark.Label, Does.Not.Contain("tl."));
                Assert.That(mark.Label, Does.Not.Contain("stg."));
                Assert.That(mark.Label, Does.Not.Contain("_"));
            }
            foreach (var d in FlightHudLayout.PortraitDevices())
            foreach (var mode in new[] { FlightHudMode.Cruise, FlightHudMode.Notes, FlightHudMode.Prompt, FlightHudMode.PromptNotes })
            {
                var frame = FlightHudLayout.Place(d, mode);
                var local = new HudBox { X = 0, Y = 0, W = frame.Plot.W, H = frame.Plot.H };
                var tags = TrajectoryWatch.Tags(local, plan.Marks);
                Assert.That(tags.Length, Is.EqualTo(plan.Marks.Length), d.Name + " " + mode);
                string where = d.Name + " " + mode;
                for (int i = 0; i < tags.Length; i++)
                {
                    Assert.That(tags[i].Box.X, Is.GreaterThanOrEqualTo(-0.6f), where);
                    Assert.That(tags[i].Box.Right, Is.LessThanOrEqualTo(local.W + 0.6f), where + " " + tags[i].Lines[0]);
                    Assert.That(tags[i].Box.Y, Is.GreaterThanOrEqualTo(-0.6f), where);
                    Assert.That(tags[i].Box.Bottom, Is.LessThanOrEqualTo(local.H + 0.6f), where);
                    foreach (var line in tags[i].Lines)
                    {
                        Assert.That(TrajectoryWatch.Plain(line), Is.True, where + " " + line);
                        Assert.That(FlightHudLayout.TextWidth(line, tags[i].FontPt), Is.LessThanOrEqualTo(tags[i].Box.W + 0.6f), where + " clips " + line);
                    }
                    if (i > 0)
                        Assert.That(tags[i].Box.Overlaps(tags[i - 1].Box), Is.False, where + " " + tags[i].Lines[0]);
                }
            }
        }

        static TrajectoryWatch Flight()
        {
            var watch = new TrajectoryWatch();
            watch.Reset();
            watch.HasFairing = true;
            watch.Stages = 2;
            watch.FairingAltM = 110000;
            return watch;
        }

        static TrajectoryEvent Ev(double t, string type, int stage, double alt, double speed) =>
            new TrajectoryEvent { T = t, Type = type, Stage = stage, AltM = alt, SpeedMps = speed };

        static TrajectoryStop[] Stops(TrajectoryPlan plan)
        {
            var s = new TrajectoryStop[plan.Marks.Length];
            for (int i = 0; i < s.Length; i++) s[i] = plan.Marks[i].Stop;
            return s;
        }
    }
}
