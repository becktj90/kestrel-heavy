using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class HangarCameraTests
    {
        [Test]
        public void PhonesHaveNoSideShift()
        {
            Assert.That(DesignFraming.HasSidePanel(1290f, 2796f), Is.False);
            Assert.That(DesignFraming.HasSidePanel(1320f, 2868f), Is.False);
            Assert.That(DesignFraming.SideVisibleFraction(1290f, 2796f, 460f), Is.EqualTo(1f));
            Assert.That(DesignFraming.NdcShiftX(1f), Is.EqualTo(0f));
            var basis = Matrix4x4.Perspective(50f, 1290f / 2796f, 0.5f, 200000f);
            var phone = DesignFraming.ShiftedProjection(basis, 0.73f, 1f);
            var legacy = DesignFraming.ShiftedProjection(basis, 0.73f);
            Assert.That(phone.m02, Is.EqualTo(legacy.m02).Within(1e-5f));
            Assert.That(phone.m12, Is.EqualTo(legacy.m12).Within(1e-5f));
        }

        [Test]
        public void IpadShiftsLeftForThe400PointPanel()
        {
            // iPad 13: 2064 x 2752 px at 2x → 1032 x 1376 pt. Panel is 400 pt on the right.
            Assert.That(DesignFraming.HasSidePanel(2064f, 2752f), Is.True);
            Assert.That(DesignFraming.PixelsPerPoint(2064f, 2752f, 264f), Is.EqualTo(2f));
            float fx = (1032f - DesignFraming.SidePanelPoints) / 1032f;
            Assert.That(DesignFraming.SideVisibleFraction(2064f, 2752f, 264f), Is.EqualTo(fx).Within(1e-4f));
            Assert.That(DesignFraming.SideVisibleFraction(2064f, 2752f, 0f), Is.EqualTo(fx).Within(1e-4f));
            Assert.That(DesignFraming.NdcShiftX(fx), Is.EqualTo(fx - 1f).Within(1e-5f));
            Assert.That(DesignFraming.NdcShiftX(fx), Is.LessThan(0f));
            var basis = Matrix4x4.Perspective(50f, 2064f / 2752f, 0.5f, 200000f);
            var shifted = DesignFraming.ShiftedProjection(basis, 0.73f, fx);
            var upright = DesignFraming.ShiftedProjection(basis, 0.73f);
            Assert.That(shifted.m02, Is.EqualTo(upright.m02 - DesignFraming.NdcShiftX(fx)).Within(1e-4f));
            Assert.That(shifted.m12, Is.EqualTo(upright.m12).Within(1e-4f));
        }

        [Test]
        public void StackAndDeckStayLeftOfTheIpadPanel()
        {
            const float fov = 50f;
            float w = 2064f, hpx = 2752f, aspect = w / hpx;
            float side = DesignFraming.SideVisibleFraction(w, hpx, 264f);
            foreach (var covered in new[] { 0.27f, 0.40f, 0.08f })
            foreach (var pair in new[] { (70f, 6f), (40f, 1.5f), (110f, 9f) })
            {
                float f = 1f - covered;
                var pts = DesignFraming.SubjectPoints(pair.Item1, pair.Item2, 18f);
                float k = DesignFraming.FitDistanceScale(pts, pair.Item1, aspect, fov, f, side);
                Assert.That(k, Is.GreaterThanOrEqualTo(DesignFraming.DistanceScale(f) - 1e-4f));
                DesignFraming.VisibleBand(f, side, out float xLo, out float xHi, out float yLo, out float yHi);
                Assert.That(xHi, Is.LessThan(1f));
                foreach (var p in pts)
                {
                    var n = DesignFraming.Project(p, pair.Item1, k, aspect, fov, f, side);
                    Assert.That(n.x, Is.GreaterThan(xLo).And.LessThan(xHi), $"x {p} covered {covered}");
                    Assert.That(n.y, Is.GreaterThan(yLo).And.LessThan(yHi), $"y {p} covered {covered}");
                }
            }
        }

        [Test]
        public void RestPoseMatchesDesignFramingAndTracksHeight()
        {
            HangarOrbit.Pose(70f, 1.2f, 0f, 0f, 1f, out var pos, out var aim);
            var expected = DesignFraming.CameraPosition(70f, 1.2f, out var aim2);
            Assert.That(pos.x, Is.EqualTo(expected.x).Within(1e-3f));
            Assert.That(pos.y, Is.EqualTo(expected.y).Within(1e-3f));
            Assert.That(pos.z, Is.EqualTo(expected.z).Within(1e-3f));
            Assert.That(aim.y, Is.EqualTo(aim2.y).Within(1e-3f));

            HangarOrbit.Pose(40f, 1f, 0f, 0f, 1f, out var a, out var aa);
            HangarOrbit.Pose(80f, 1f, 0f, 0f, 1f, out var b, out var bb);
            Assert.That(bb.y, Is.EqualTo(aa.y * 2f).Within(1e-3f));
            Assert.That((b - bb).magnitude, Is.EqualTo((a - aa).magnitude * 2f).Within(1e-2f));
        }

        [Test]
        public void YawTurnsAroundTheVehicleAndPitchRaisesTheCamera()
        {
            HangarOrbit.Pose(70f, 1f, 0f, 0f, 1f, out var rest, out var aim);
            HangarOrbit.Pose(70f, 1f, 180f, 0f, 1f, out var back, out _);
            Assert.That(back.x, Is.EqualTo(-rest.x).Within(1e-2f));
            Assert.That(back.z, Is.EqualTo(-rest.z).Within(1e-2f));
            Assert.That(back.y, Is.EqualTo(rest.y).Within(1e-2f));
            Assert.That(aim.x, Is.EqualTo(0f));

            HangarOrbit.Pose(70f, 1f, 0f, 20f, 1f, out var raised, out _);
            Assert.That(raised.y, Is.GreaterThan(rest.y + 10f));
            HangarOrbit.Pose(70f, 1f, 0f, HangarMotion.PitchMinDeg, 1f, out var low, out _);
            Assert.That(low.y, Is.LessThan(rest.y));
        }

        [Test]
        public void ZoomIsMagnificationClampedToTheSpecRange()
        {
            HangarOrbit.Pose(70f, 1f, 0f, 0f, 1f, out var framed, out var aim);
            HangarOrbit.Pose(70f, 1f, 0f, 0f, 3f, out var close, out var aimC);
            HangarOrbit.Pose(70f, 1f, 0f, 0f, 0.6f, out var wide, out _);
            float d0 = (framed - aim).magnitude;
            Assert.That((close - aimC).magnitude, Is.EqualTo(d0 / 3f).Within(0.05f));
            Assert.That((wide - aim).magnitude, Is.EqualTo(d0 / 0.6f).Within(0.05f));

            var o = HangarOrbit.Rest();
            o.Pinch(10f);
            Assert.That(o.Zoom, Is.EqualTo(HangarMotion.ZoomMax));
            o.Pinch(0.01f);
            Assert.That(o.Zoom, Is.EqualTo(HangarMotion.ZoomMin));
            o.Pinch(float.NaN);
            Assert.That(o.Zoom, Is.EqualTo(HangarMotion.ZoomMin));
        }

        [Test]
        public void PitchAndYawClampWithInertiaThatStopsAtTheLimit()
        {
            var o = HangarOrbit.Rest();
            o.Drag(40f, 0f, 0.05f, false);
            Assert.That(o.YawDeg, Is.EqualTo(40f * HangarMotion.DegreesPerPixel).Within(1e-3f));
            float vel = o.YawVel;
            Assert.That(vel, Is.GreaterThan(0f));
            float yaw = o.YawDeg;
            o.Step(0.1f, false);
            Assert.That(o.YawDeg, Is.GreaterThan(yaw));
            Assert.That(o.YawVel, Is.LessThan(vel));
            Assert.That(o.YawVel, Is.GreaterThan(0f));

            o.Drag(100000f, 0f, 0.016f, false);
            Assert.That(o.YawDeg, Is.EqualTo(HangarMotion.YawLimitDeg));
            Assert.That(o.YawVel, Is.EqualTo(0f));
            o.YawVel = 400f;
            o.Step(0.2f, false);
            Assert.That(o.YawDeg, Is.EqualTo(HangarMotion.YawLimitDeg));
            Assert.That(o.YawVel, Is.EqualTo(0f));

            o = HangarOrbit.Rest();
            o.Drag(0f, 100000f, 0.016f, false);
            Assert.That(o.PitchDeg, Is.EqualTo(HangarMotion.PitchMaxDeg));
            o = HangarOrbit.Rest();
            o.Drag(0f, -100000f, 0.016f, false);
            Assert.That(o.PitchDeg, Is.EqualTo(HangarMotion.PitchMinDeg));
        }

        [Test]
        public void ReduceMotionKillsInertiaAndSnapsReframe()
        {
            var o = HangarOrbit.Rest();
            o.Drag(30f, 12f, 0.02f, true);
            Assert.That(o.YawDeg, Is.GreaterThan(0f));
            Assert.That(o.YawVel, Is.EqualTo(0f));
            Assert.That(o.PitchVel, Is.EqualTo(0f));
            float yaw = o.YawDeg;
            o.Step(0.5f, true);
            Assert.That(o.YawDeg, Is.EqualTo(yaw));

            o.BeginReframe(false);
            Assert.That(o.Reframing, Is.True);
            o.Step(HangarMotion.ReframeSeconds * 0.5f, false);
            Assert.That(o.Reframing, Is.True);
            Assert.That(o.YawDeg, Is.EqualTo(yaw * 0.5f).Within(1e-3f));
            o.Step(0.0001f, true);
            Assert.That(o.Reframing, Is.False);
            Assert.That(o.YawDeg, Is.EqualTo(0f));
            Assert.That(o.Zoom, Is.EqualTo(1f));

            o.Drag(20f, 0f, 0.02f, false);
            o.Pinch(2f);
            o.BeginReframe(true);
            Assert.That(o.YawDeg, Is.EqualTo(0f));
            Assert.That(o.Zoom, Is.EqualTo(1f));
            Assert.That(o.Reframing, Is.False);
        }

        [Test]
        public void ReframeEasesBackOver400Milliseconds()
        {
            var o = HangarOrbit.Rest();
            o.Drag(40f, 10f, 0.05f, false);
            o.Pinch(2f);
            float yaw = o.YawDeg, pitch = o.PitchDeg, zoom = o.Zoom;
            o.BeginReframe(false);
            o.Step(HangarMotion.ReframeSeconds, false);
            Assert.That(o.Reframing, Is.False);
            Assert.That(o.YawDeg, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(o.PitchDeg, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(o.Zoom, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(yaw, Is.Not.EqualTo(0f));
            Assert.That(pitch, Is.Not.EqualTo(0f));
            Assert.That(zoom, Is.EqualTo(2f));
        }

        [Test]
        public void ExplodedViewMovesCloserAndRestoresThePose()
        {
            Assert.That(HangarMotion.ExplodedSeconds, Is.EqualTo(HangarExplode.Seconds));
            Assert.That(HangarMotion.ExplodedZoom, Is.GreaterThan(1f).And.LessThanOrEqualTo(HangarMotion.ZoomMax));
            Assert.That(HangarMotion.ExplodedPitchDeg, Is.GreaterThan(HangarMotion.PitchMinDeg).And.LessThan(HangarMotion.PitchMaxDeg));

            HangarOrbit.Pose(70f, 1f, 0f, 0f, 1f, out var framed, out var aim);
            HangarOrbit.Pose(70f, 1f, 20f, HangarMotion.ExplodedPitchDeg, HangarMotion.ExplodedZoom, out var close, out var aimC);
            Assert.That((close - aimC).magnitude, Is.LessThan((framed - aim).magnitude / HangarMotion.ExplodedZoom + 1f));

            var o = HangarOrbit.Rest();
            o.Drag(40f, 8f, 0.05f, false);
            float yaw = o.YawDeg, pitch = o.PitchDeg, zoom = o.Zoom;
            o.BeginExploded(false, false);
            Assert.That(o.ExplodeFraming, Is.False, "closing a view that was never open does not move the camera");
            Assert.That(o.Zoom, Is.EqualTo(zoom).Within(1e-4f));

            o.BeginExploded(true, false);
            Assert.That(o.ExplodeFraming, Is.True);
            o.Step(HangarMotion.ExplodedSeconds * 0.5f, false);
            float midZoom = Mathf.Lerp(zoom, HangarMotion.ExplodedZoom, 0.5f);
            float midPitch = Mathf.Lerp(pitch, HangarMotion.ExplodedPitchDeg, 0.5f);
            Assert.That(o.Zoom, Is.EqualTo(midZoom).Within(1e-3f));
            Assert.That(o.PitchDeg, Is.EqualTo(midPitch).Within(1e-3f));
            Assert.That(o.YawDeg, Is.EqualTo(yaw).Within(1e-3f));
            o.Step(HangarMotion.ExplodedSeconds, false);
            Assert.That(o.ExplodeFraming, Is.False);
            Assert.That(o.Zoom, Is.EqualTo(HangarMotion.ExplodedZoom).Within(1e-3f));
            Assert.That(o.PitchDeg, Is.EqualTo(HangarMotion.ExplodedPitchDeg).Within(1e-3f));
            Assert.That(o.YawDeg, Is.EqualTo(yaw).Within(1e-3f));

            // A second open must not recapture the closer pose as the one to restore.
            o.BeginExploded(true, true);
            Assert.That(o.Zoom, Is.EqualTo(HangarMotion.ExplodedZoom).Within(1e-3f));
            o.BeginExploded(false, true);
            Assert.That(o.YawDeg, Is.EqualTo(yaw).Within(1e-3f));
            Assert.That(o.PitchDeg, Is.EqualTo(pitch).Within(1e-3f));
            Assert.That(o.Zoom, Is.EqualTo(zoom).Within(1e-3f));
            Assert.That(o.ExplodeFraming, Is.False);
        }

        [Test]
        public void ExplodedToggleFollowsCodeAndTheCameraFollowsTheToggle()
        {
            var camGo = new GameObject("Camera");
            var uiGo = new GameObject("UI");
            try
            {
                camGo.AddComponent<Camera>();
                var hangar = camGo.AddComponent<HangarCamera>();
                hangar.ReduceMotion = true;
                var doc = uiGo.AddComponent<UIDocument>();
                doc.panelSettings = Resources.Load<PanelSettings>("UI/KH_PanelSettings");
                Assert.That(doc.panelSettings, Is.Not.Null);
                Assert.That(doc.rootVisualElement, Is.Not.Null);
                var toggle = new Toggle { name = "explode" };
                doc.rootVisualElement.Add(toggle);

                var link = new ExplodedToggleLink();
                int calls = 0;
                link.Bind(toggle, false, on =>
                {
                    calls++;
                    Assert.That(calls, Is.LessThan(3), "exploded toggle fed back into itself");
                    hangar.SetExploded(on);
                    link.Push(hangar.Exploded);
                });

                Assert.That(toggle.value, Is.False);
                Assert.That(hangar.Exploded, Is.False);
                Assert.That(hangar.Zoom, Is.EqualTo(1f).Within(1e-3f));

                hangar.SetExploded(true);
                link.Push(hangar.Exploded);
                Assert.That(calls, Is.EqualTo(0), "Push must not notify the checkbox");
                Assert.That(hangar.Exploded, Is.True);
                Assert.That(toggle.value, Is.True);
                Assert.That(link.Value, Is.True);
                Assert.That(hangar.Zoom, Is.EqualTo(HangarMotion.ExplodedZoom).Within(1e-3f));

                toggle.value = false;
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(hangar.Exploded, Is.False);
                Assert.That(toggle.value, Is.False);
                Assert.That(link.Value, Is.False);
                Assert.That(hangar.Zoom, Is.EqualTo(1f).Within(1e-3f));

                toggle.value = true;
                Assert.That(calls, Is.EqualTo(2));
                Assert.That(hangar.Exploded, Is.True);
                Assert.That(hangar.Zoom, Is.EqualTo(HangarMotion.ExplodedZoom).Within(1e-3f));
                link.Unbind();
            }
            finally
            {
                Object.DestroyImmediate(uiGo);
                Object.DestroyImmediate(camGo);
            }
        }

        [Test]
        public void IpadPanelBlocksTheRightEdgeAndTheBottomSheetBlocksLowTouches()
        {
            float w = 2064f, h = 2752f;
            Assert.That(HangarCamera.BlocksGesture(new Vector2(w - 4f, h * 0.8f), w, h, 264f, 0.73f), Is.True);
            Assert.That(HangarCamera.BlocksGesture(new Vector2(w * 0.2f, h * 0.8f), w, h, 264f, 0.73f), Is.False);
            Assert.That(HangarCamera.BlocksGesture(new Vector2(w * 0.2f, 10f), w, h, 264f, 0.73f), Is.True);
            // Phone: no right-edge block. A touch near the right still orbits.
            Assert.That(HangarCamera.BlocksGesture(new Vector2(1280f, 2000f), 1290f, 2796f, 460f, 0.73f), Is.False);
        }
    }
}
