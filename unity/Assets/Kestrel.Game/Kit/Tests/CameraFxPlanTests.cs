using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class CameraFxPlanTests
    {
        static CameraBeatEvent Beat(CameraBeat b, float i = 1f, bool reduce = false, float mul = 1f) => new CameraBeatEvent { Beat = b, Intensity01 = i, ShakeMul = mul, Reduce = reduce };

        static float MinScale(CameraFxState s, float seconds, float dt = 1f / 120f) { float m = 1f; for (float t = 0; t < seconds; t += dt) { s.Step(dt); if (s.TimeScale < m) m = s.TimeScale; } return m; }

        [Test]
        public void ReduceMotionPlaysNothing()
        {
            var s = new CameraFxState();
            foreach (CameraBeat b in System.Enum.GetValues(typeof(CameraBeat))) s.Trigger(Beat(b, 1f, true));
            for (int i = 0; i < 120; i++) { s.Step(1f / 60f); Assert.That(s.Trauma, Is.EqualTo(0f)); Assert.That(s.FovDeg, Is.EqualTo(0f)); Assert.That(s.TimeScale, Is.EqualTo(1f)); Assert.That(s.ShakeX, Is.EqualTo(0f)); }
        }

        [Test]
        public void SlowMoNeverGoesBelowTheFloorOrLastsPastTheCap()
        {
            foreach (CameraBeat b in System.Enum.GetValues(typeof(CameraBeat)))
            {
                var s = new CameraFxState(); s.Trigger(Beat(b, 5f));
                float min = 1f, slowSeconds = 0f;
                for (float t = 0; t < 1.5f; t += 1f / 240f) { s.Step(1f / 240f); min = System.Math.Min(min, s.TimeScale); if (s.TimeScale < 0.999f) slowSeconds += 1f / 240f; }
                Assert.That(min, Is.GreaterThanOrEqualTo(CameraFxLimits.MinTimeScale - 1e-4f), b.ToString());
                Assert.That(slowSeconds, Is.LessThanOrEqualTo(CameraFxLimits.MaxSlowSeconds + CameraFxLimits.EaseOut + 0.02f), b.ToString());
                Assert.That(s.TimeScale, Is.EqualTo(1f));
            }
            var c = new CameraFxState(); c.Trigger(Beat(CameraBeat.Crash)); Assert.That(MinScale(c, 1f), Is.EqualTo(0.4f).Within(0.01f));
        }

        [Test]
        public void OnlyOneSlowMoPerThreeSeconds()
        {
            var s = new CameraFxState();
            s.Trigger(Beat(CameraBeat.Touchdown)); MinScale(s, 0.8f);
            s.Trigger(Beat(CameraBeat.Crash));
            Assert.That(MinScale(s, 0.8f), Is.EqualTo(1f), "second slow-mo at ~0.8 s is refused");
            MinScale(s, 2.6f);   // now ~4.2 s after the first
            s.Trigger(Beat(CameraBeat.Crash));
            Assert.That(MinScale(s, 0.8f), Is.LessThan(0.5f), "allowed again after the gap");
        }

        [Test]
        public void SlowMoIsEasedNotAHardStep()
        {
            var s = new CameraFxState(); s.Trigger(Beat(CameraBeat.Crash));
            float prev = 1f; float maxStep = 0f;
            for (float t = 0; t < 0.7f; t += 1f / 240f) { s.Step(1f / 240f); maxStep = System.Math.Max(maxStep, System.Math.Abs(s.TimeScale - prev)); prev = s.TimeScale; }
            Assert.That(maxStep, Is.LessThan(0.6f * (1f / 240f) / CameraFxLimits.EaseIn + 0.01f));
        }

        [Test]
        public void TraumaAddsClampsAndDecaysToZero()
        {
            var s = new CameraFxState();
            for (int i = 0; i < 10; i++) s.Trigger(Beat(CameraBeat.Crash));
            Assert.That(s.Trauma, Is.EqualTo(CameraFxLimits.MaxTrauma));
            Assert.That(s.FovDeg, Is.LessThanOrEqualTo(CameraFxLimits.MaxFovPunchDeg));
            float prev = s.Trauma; for (int i = 0; i < 240; i++) { s.Step(1f / 120f); Assert.That(s.Trauma, Is.LessThanOrEqualTo(prev)); prev = s.Trauma; }
            Assert.That(s.Trauma, Is.EqualTo(0f)); Assert.That(s.ShakeX, Is.EqualTo(0f));
        }

        [Test]
        public void ShakeIsBoundedAndSquaredInTrauma()
        {
            var s = new CameraFxState(); s.Trigger(Beat(CameraBeat.Crash)); s.Trigger(Beat(CameraBeat.Crash));
            float maxX = 0f, maxRoll = 0f;
            for (int i = 0; i < 240; i++) { s.Step(1f / 120f); maxX = System.Math.Max(maxX, System.Math.Abs(s.ShakeX)); maxRoll = System.Math.Max(maxRoll, System.Math.Abs(s.RollDeg)); Assert.That(System.Math.Abs(s.ShakeY), Is.LessThanOrEqualTo(1f)); }
            Assert.That(maxX, Is.GreaterThan(0.05f)); Assert.That(maxX, Is.LessThanOrEqualTo(1f)); Assert.That(maxRoll, Is.LessThanOrEqualTo(CameraFxLimits.MaxRollDeg));
            Assert.That(CameraFxState.OffsetMetres(1f, 60f), Is.EqualTo(0.72f).Within(1e-4f));
        }

        [Test]
        public void ShakeMulLetsAnotherShakerOwnTheBeat()
        {
            var a = new CameraFxState(); var b = new CameraFxState();
            a.Trigger(Beat(CameraBeat.Liftoff)); b.Trigger(Beat(CameraBeat.Liftoff, 1f, false, 0f));
            Assert.That(b.Trauma, Is.EqualTo(0f)); Assert.That(a.Trauma, Is.GreaterThan(0f));
            Assert.That(b.FovDeg, Is.GreaterThan(0f), "the FOV punch and slow-mo still play");
        }

        [Test]
        public void OnlyTheBeatsInTheSpecHaveSlowMo()
        {
            Assert.That(CameraFxLimits.Spec(CameraBeat.Ignition).SlowMo, Is.False); Assert.That(CameraFxLimits.Spec(CameraBeat.MaxQ).SlowMo, Is.False);
            foreach (var b in new[] { CameraBeat.Liftoff, CameraBeat.Staging, CameraBeat.Touchdown, CameraBeat.Crash })
            { var sp = CameraFxLimits.Spec(b); Assert.That(sp.SlowMo, Is.True); Assert.That(sp.SlowScale, Is.GreaterThanOrEqualTo(0.4f)); Assert.That(sp.SlowSeconds, Is.LessThanOrEqualTo(0.35f)); }
        }

        [Test]
        public void BadInputsAreIgnored()
        {
            var s = new CameraFxState();
            s.Trigger(Beat(CameraBeat.Crash, float.NaN)); s.Trigger(Beat(CameraBeat.Crash, 0f)); s.Step(float.NaN); s.Step(-1f);
            Assert.That(s.Trauma, Is.EqualTo(0f)); Assert.That(s.TimeScale, Is.EqualTo(1f));
        }

        [Test]
        public void EventReachesSubscribers()
        {
            CinematicEvents.ClearCameraSubscribers(); CameraBeatEvent got = default;
            System.Action<CameraBeatEvent> h = e => got = e; CinematicEvents.CameraBeatRaised += h;
            CinematicEvents.CameraBeat(CameraBeat.Staging, 0.5f, false, 0.3f);
            Assert.That(got.Beat, Is.EqualTo(CameraBeat.Staging)); Assert.That(got.ShakeMul, Is.EqualTo(0.3f));
            CinematicEvents.ClearCameraSubscribers();
        }
    }
}
