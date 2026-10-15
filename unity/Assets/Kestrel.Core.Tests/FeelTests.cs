using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kestrel.Core.Feel;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FeelTests
    {
        [Test]
        public void Stick_FullThrowIsRateLimited_AndReleaseBeatsAttackOnASmallError()
        {
            double dt = 1.0 / 60.0;
            double full = StickResponse.Step(0, 1, dt);
            Assert.AreEqual(StickResponse.MaxRate * dt, full, 1e-12);
            double attack = StickResponse.Step(0, 0.1, dt);
            double release = Math.Abs(StickResponse.Step(0.1, 0, dt));
            Assert.That(release, Is.GreaterThan(attack));
            Assert.That(attack, Is.LessThan(0.1));
        }

        [Test]
        public void Stick_RejectsNaN_AndStaysInsideTheAxis()
        {
            Assert.AreEqual(StickResponse.MaxRate * 0.016, StickResponse.Step(double.NaN, 1, 0.016), 1e-12);
            double toward = StickResponse.Step(0.4, double.NaN, 0.016);
            Assert.That(toward, Is.LessThan(0.4));
            Assert.That(toward, Is.GreaterThan(0));
            Assert.AreEqual(0.4, StickResponse.Step(0.4, 1, 0), 0);
            double axis = 0;
            for (int i = 0; i < 180; i++) axis = StickResponse.Step(axis, 2, 1.0 / 60.0);
            Assert.AreEqual(1, axis, 1e-6);
            axis = StickResponse.Step(0, -4, 1);
            Assert.That(axis, Is.InRange(-1, 0));
        }

        [Test]
        public void Lead_IsAheadOfVelocity_AndQuietVertically()
        {
            CameraRig.Lead(0, 0, 70, out double x, out double y);
            Assert.AreEqual(0, x, 0);
            Assert.AreEqual(0, y, 0);
            CameraRig.Lead(180, 0, 70, out x, out y);
            double reach = 70 * CameraRig.LeadFraction;
            double scale = reach * (1 - Math.Exp(-1)) / 180.0;
            Assert.AreEqual(180 * scale, x, 1e-9);
            Assert.AreEqual(0, y, 0);
            CameraRig.Lead(0, 180, 70, out x, out y);
            Assert.AreEqual(0, x, 1e-9);
            Assert.AreEqual(180 * scale * CameraRig.LeadVertical, y, 1e-9);
        }

        [Test]
        public void Fov_WidensWithMach_AndPunchPullsBackIn()
        {
            Assert.AreEqual(1, CameraRig.FovScale(0, 0, 0), 1e-12);
            double wide = CameraRig.FovScale(3, 1, 0);
            Assert.That(wide, Is.GreaterThan(1));
            Assert.That(wide, Is.LessThan(1.12));
            Assert.That(CameraRig.FovScale(3, 1, 1), Is.LessThan(wide));
            Assert.That(CameraRig.DecayPunch(1, 0.5), Is.LessThan(0.3));
        }

        [Test]
        public void Shake_KicksThenDies()
        {
            var s = new ShakeState();
            CameraRig.Kick(ref s, 16, 0.4);
            double peak = 0;
            for (int i = 0; i < 12; i++)
            {
                CameraRig.StepShake(ref s, 1.0 / 60.0);
                peak = Math.Max(peak, Math.Abs(s.X) + Math.Abs(s.Y));
            }
            Assert.That(peak, Is.GreaterThan(0.05));
            for (int i = 0; i < 180; i++) CameraRig.StepShake(ref s, 1.0 / 60.0);
            Assert.That(Math.Abs(s.X) + Math.Abs(s.Y), Is.LessThan(1e-3));
        }

        [Test]
        public void Pitch_LagsAStep_ThenCatchesIt()
        {
            double pitch = CameraRig.StepPitch(90, 60, FlightPresenter.PitchRate, 1.0 / 60.0);
            Assert.That(pitch, Is.GreaterThan(60));
            Assert.That(pitch, Is.LessThan(90));
            for (int i = 0; i < 240; i++) pitch = CameraRig.StepPitch(pitch, 60, 4.2, 1.0 / 60.0);
            Assert.AreEqual(60, pitch, 1e-3);
        }

        [Test]
        public void Cues_ReducedMotionDropsShakeAndHaptic()
        {
            var hit = FlightCues.For("breakup", false);
            Assert.AreEqual(1, hit.Shake, 0);
            Assert.That(hit.Hard, Is.True);
            var calm = FlightCues.For("breakup", true);
            Assert.AreEqual(0, calm.Shake, 0);
            Assert.AreEqual(0, calm.HapticMs, 0);
            Assert.That(calm.Hard, Is.False);
            Assert.AreEqual("nominal", AirframeBand.Of(1));
            Assert.AreEqual("critical", AirframeBand.Of(0.2));
            Assert.AreEqual("lost", AirframeBand.Of(0));
        }

        [Test]
        public void Risk_NamesMaxQ_BeforeItNamesAShortDesign()
        {
            var hot = HudRisk.Evaluate(1, 0.8, 0.7, 90000, 100000, -0.2);
            Assert.AreEqual("MAX-Q", hot.Call);
            Assert.That(hot.QRisk, Is.EqualTo(0.9).Within(1e-12));
            var hurt = HudRisk.Evaluate(0.2, 0.8, 0.7, 1000, 100000, 0.1);
            Assert.AreEqual("AIRFRAME", hurt.Call);
            Assert.AreEqual("critical", hurt.Band);
            var prop = HudRisk.Evaluate(1, 0.4, 0.1, 0, 100000, 0.2);
            Assert.AreEqual("PROP", prop.Call);
            var ok = HudRisk.Evaluate(1, 1, 1, 1000, 100000, 0.1);
            Assert.AreEqual("NOMINAL", ok.Call);
            Assert.AreEqual("", ok.Line);
        }

        [Test]
        public void Grid_FindsNeighbours_AndSkipsAFarPoint()
        {
            var grid = new SpatialGrid(10);
            grid.Insert(1, 0, 0);
            grid.Insert(2, 8, -6);
            grid.Insert(3, -3, 4);
            grid.Insert(9, 500, 500);
            var hits = new List<int>();
            grid.Query(0, 0, 12, hits);
            Assert.That(hits, Does.Contain(1));
            Assert.That(hits, Does.Contain(2));
            Assert.That(hits, Does.Contain(3));
            Assert.That(hits, Does.Not.Contain(9));
            grid.Query(-0.2, -0.2, 1, hits);
            Assert.That(hits, Does.Contain(1));
            var sw = Stopwatch.StartNew();
            for (int n = 0; n < 2000; n++) grid.Query((n % 50) - 25, (n % 17) - 8, 12, hits);
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(250));
        }

        [Test]
        public void Debris_SameSeed_Separates_AndOverlapsPushApart()
        {
            Assert.AreEqual(DebrisField.Hash01(1), DebrisField.Hash01(1), 0);
            Assert.That(DebrisField.Hash01(1), Is.InRange(0, 1));
            Assert.That(DebrisField.Hash01(2), Is.Not.EqualTo(DebrisField.Hash01(1)));

            var a = new DebrisField(40, 2.5);
            var b = new DebrisField(40, 2.5);
            Assert.AreEqual(28, a.Burst(0, 100, 200, 40, 28, 180, 0.04, 0x5EED));
            b.Burst(0, 100, 200, 40, 28, 180, 0.04, 0x5EED);
            Assert.AreEqual(a.Shards[0].Vx, b.Shards[0].Vx, 0);
            Assert.AreEqual(a.Shards[3].Y, b.Shards[3].Y, 0);

            a.Shards[0].X = 0; a.Shards[0].Y = 0; a.Shards[0].Vx = 0; a.Shards[0].Vy = 0;
            a.Shards[0].Radius = 1; a.Shards[0].Mass = 1; a.Shards[0].Alive = true; a.Shards[0].Life = 10;
            a.Shards[1].X = 0.4; a.Shards[1].Y = 0; a.Shards[1].Vx = 0; a.Shards[1].Vy = 0;
            a.Shards[1].Radius = 1; a.Shards[1].Mass = 1; a.Shards[1].Alive = true; a.Shards[1].Life = 10;
            a.Step(1.0 / 60.0, 0, 0);
            double dx = a.Shards[1].X - a.Shards[0].X;
            double dy = a.Shards[1].Y - a.Shards[0].Y;
            Assert.That(dx * dx + dy * dy, Is.GreaterThan(0.4 * 0.4));

            var cloud = new DebrisField(40, 2.5);
            cloud.Burst(0, 0, 0, 0, 20, 180, 0, 7);
            for (int i = 0; i < 90; i++) cloud.Step(1.0 / 60.0, 9.81, 0.35);
            double span = 0;
            int overlaps = 0;
            for (int i = 0; i < cloud.Shards.Length; i++)
            {
                if (!cloud.Shards[i].Alive) continue;
                span = Math.Max(span, Math.Abs(cloud.Shards[i].X) + Math.Abs(cloud.Shards[i].Y));
                for (int j = i + 1; j < cloud.Shards.Length; j++)
                {
                    if (!cloud.Shards[j].Alive) continue;
                    double ex = cloud.Shards[j].X - cloud.Shards[i].X;
                    double ey = cloud.Shards[j].Y - cloud.Shards[i].Y;
                    double min = cloud.Shards[i].Radius + cloud.Shards[j].Radius;
                    if (ex * ex + ey * ey < min * min * 0.98) overlaps++;
                }
            }
            Assert.That(span, Is.GreaterThan(4));
            Assert.AreEqual(0, overlaps);
        }

        [Test]
        public void Steering_Arrives_AndWallsPushInward()
        {
            Steering.Seek(0, 0, 0, 0, 10, 0, 5, out double ax, out double ay);
            Assert.AreEqual(5, ax, 1e-9);
            Assert.AreEqual(0, ay, 1e-9);
            Steering.Arrive(0, 0, 0, 0, 2, 0, 8, 4, out ax, out ay);
            Assert.AreEqual(4, ax, 1e-9);
            Assert.That(Steering.AvoidWalls(-9, 10, 2), Is.GreaterThan(0));
            Assert.That(Steering.AvoidWalls(9, 10, 2), Is.LessThan(0));
            Assert.AreEqual(0, Steering.AvoidWalls(0, 10, 2), 0);
            Steering.Blend(4, 0, 0, 8, 1, 1, out ax, out ay);
            Assert.AreEqual(2, ax, 1e-12);
            Assert.AreEqual(4, ay, 1e-12);
        }

        [Test]
        public void Presenter_LiftoffShakes_MaxQOnce_BreakupSpawns_PitchLags()
        {
            var p = new FlightPresenter(40);
            var s = new FlightSample
            {
                Running = true, HasAttitude = true, AttitudeDeg = 90, VehicleHeight = 70,
                Stage = 0, Health01 = 1, Fuel01 = 1, ThrustFraction = 1, QLimitPa = 35000, DvMargin = 0.1,
            };
            p.Step(s, 1.0 / 60.0, false);
            Assert.That(p.Began, Is.True);
            Assert.AreEqual(28, p.HapticMs);
            Assert.That(Math.Abs(p.Shake.Vx) + Math.Abs(p.Shake.Vy), Is.GreaterThan(1));
            s.Stage = 0;
            p.Step(s, 1.0 / 60.0, false);
            Assert.AreEqual(0, p.HapticMs);
            s.Stage = 1;
            p.Step(s, 1.0 / 60.0, false);
            Assert.AreEqual(14, p.HapticMs);

            s.DynPressure = 40000;
            s.Mach = 1.1;
            s.Speed = 320;
            s.AttitudeDeg = 70;
            for (int i = 0; i < 30; i++) p.Step(s, 1.0 / 60.0, false);
            Assert.That(p.QCue, Is.False);
            s.DynPressure = 40000 * FlightPresenter.MaxQDrop * 0.99;
            p.Step(s, 1.0 / 60.0, false);
            Assert.That(p.QCue, Is.True);
            Assert.AreEqual(18, p.HapticMs);
            p.Step(s, 1.0 / 60.0, false);
            Assert.AreEqual(0, p.HapticMs);
            Assert.That(p.Pitch, Is.GreaterThan(70));
            Assert.That(p.Pitch, Is.LessThan(90));
            Assert.That(p.LeadX, Is.GreaterThan(1));
            Assert.That(p.FovScale, Is.GreaterThan(1));

            s.Lost = true;
            s.Running = false;
            s.Downrange = 1000;
            s.Altitude = 12000;
            p.Step(s, 1.0 / 60.0, false);
            Assert.That(p.Debris.Live, Is.EqualTo(28));
            Assert.AreEqual("lost", p.Band);
            Assert.AreEqual("AIRFRAME", p.Call);
            double before = p.Debris.Live;
            for (int i = 0; i < 30; i++) p.Step(s, 1.0 / 60.0, false);
            Assert.That(p.Debris.Live, Is.EqualTo(before));

            var calm = new FlightPresenter(40);
            calm.Step(s, 1.0 / 60.0, true);
            // Lost sample still breaks up, but the reduced cloud is the small one and shake stays down.
            s.Running = true;
            s.Lost = false;
            calm.Reset();
            calm.Step(s, 1.0 / 60.0, true);
            Assert.AreEqual(0, calm.Shake.Vx, 0);
            Assert.AreEqual(0, calm.HapticMs);
            Assert.AreEqual(0, calm.LeadX, 0);
        }
    }
}
