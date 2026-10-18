using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class CameraFxTests
    {
        GameObject go; CameraFx fx; GameObject camGo; Camera cam; System.Func<bool> savedReduce;

        [SetUp]
        public void SetUp()
        {
            savedReduce = CameraFx.ReduceMotion; CameraFx.ReduceMotion = () => false;
            go = new GameObject("camfx-test"); fx = go.AddComponent<CameraFx>(); fx.Subscribe();
            camGo = new GameObject("cam"); cam = camGo.AddComponent<Camera>(); fx.Target = cam;
        }

        [TearDown]
        public void TearDown()
        {
            fx.Unsubscribe(); Object.DestroyImmediate(go); Object.DestroyImmediate(camGo);
            CameraFx.ReduceMotion = savedReduce; CinematicEvents.ClearSubscribers(); CinematicEvents.ClearCameraSubscribers();
        }

        [Test]
        public void BeatsDriveStateAndPublishAPresentationScaleOnly()
        {
            CinematicEvents.CameraBeat(CameraBeat.Crash, 1f, false);
            for (int i = 0; i < 12; i++) fx.Tick(1f / 120f);
            Assert.That(CinematicEvents.PresentationScale, Is.LessThan(1.0));
            Assert.That(CinematicEvents.PresentationScale, Is.GreaterThanOrEqualTo(0.4));
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Time.timeScale is never touched");
            for (int i = 0; i < 120; i++) fx.Tick(1f / 120f);
            Assert.That(CinematicEvents.PresentationScale, Is.EqualTo(1.0));
        }

        [Test]
        public void ReduceMotionPlaysNothingEvenIfTheEventForgot()
        {
            CameraFx.ReduceMotion = () => true;
            CinematicEvents.CameraBeat(CameraBeat.Crash, 1f, false);
            fx.Tick(1f / 60f);
            Assert.That(fx.State.Trauma, Is.EqualTo(0f)); Assert.That(CinematicEvents.PresentationScale, Is.EqualTo(1.0));
        }

        [Test]
        public void CrashAndFailureCuesBecomeACrashBeat_FailureLeavesMostShakeToThePresenter()
        {
            CinematicEvents.Crash(0f, 1f, 0f, 1f, 2f, true, false);
            float crash = fx.State.Trauma; fx.State.Reset();
            CinematicEvents.Failure(0f, 1f, 0f, 1f, 2f, false);
            Assert.That(crash, Is.GreaterThan(0f)); Assert.That(fx.State.Trauma, Is.LessThan(crash));
        }

        [Test]
        public void CaptureOverrideSuppressesTheEffectAndTargetIsRestored()
        {
            CinematicEvents.CameraBeat(CameraBeat.Crash, 1f, false); fx.Tick(1f / 120f);
            var pos = camGo.transform.position; var fov = cam.fieldOfView;
            CaptureCamera.Override(new Vector3(0, 5, -10), Vector3.zero);
            try { fx.Apply(cam); } finally { CaptureCamera.Clear(); }
            Assert.That(camGo.transform.position, Is.EqualTo(pos)); Assert.That(cam.fieldOfView, Is.EqualTo(fov));
            Assert.That(fx.AppliedThisFrame, Is.False);
        }
    
        [Test]
        public void ApplyOffsetsOnlyDuringTheRenderAndRestoreReturnsTheExactPose()
        {
            camGo.transform.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(10, 20, 0)); cam.fieldOfView = 50f;
            CinematicEvents.CameraBeat(CameraBeat.Crash, 1f, false); fx.Tick(0.05f);
            var pos = camGo.transform.position; var rot = camGo.transform.rotation;
            fx.Apply(cam);
            Assert.That(fx.AppliedThisFrame, Is.True); Assert.That(cam.fieldOfView, Is.GreaterThan(50f));
            fx.Restore(cam);
            Assert.That(camGo.transform.position, Is.EqualTo(pos)); Assert.That(camGo.transform.rotation, Is.EqualTo(rot)); Assert.That(cam.fieldOfView, Is.EqualTo(50f));
            Assert.That(fx.AppliedThisFrame, Is.False);
        }

        [Test]
        public void OtherCamerasAreLeftAlone()
        {
            var other = new GameObject("other").AddComponent<Camera>();
            try { CinematicEvents.CameraBeat(CameraBeat.Crash, 1f, false); fx.Tick(0.05f); var p = other.transform.position; fx.Apply(other); Assert.That(fx.AppliedThisFrame, Is.False); Assert.That(other.transform.position, Is.EqualTo(p)); }
            finally { Object.DestroyImmediate(other.gameObject); }
        }
    }
}
