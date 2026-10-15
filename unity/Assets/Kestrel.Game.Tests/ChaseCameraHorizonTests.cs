using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // P0-1 regression (#117 Unity capture): by T+2.2 s (iPhone) / T+3.7 s (iPad) the portrait frame was all
    // sky because the chase camera pitched ~32 deg nose-up. Projects the horizon and the vehicle through a
    // real perspective matrix at the capture times and across the early ascent.
    public class ChaseCameraHorizonTests
    {
        const float H = 70f, VFov = 50f, Accel = 14f;  // slice camera FOV; ~1.4 g net early-ascent accel

        static Vector3 Project(Vector3 world, Vector3 camPos, Quaternion rot, float aspect)
        {
            var view = Matrix4x4.TRS(camPos, rot, Vector3.one).inverse;
            var proj = Matrix4x4.Perspective(VFov, aspect, 0.5f, 200000f);
            var c = view.MultiplyPoint(world); c.z = -c.z;                 // Unity view space looks down -z
            var clip = proj * new Vector4(c.x, c.y, c.z, 1f);
            return new Vector3(clip.x / clip.w * 0.5f + 0.5f, clip.y / clip.w * 0.5f + 0.5f, clip.w);
        }

        static void Check(float t, float aspect, string device)
        {
            float alt = 0.5f * Accel * t * t;
            var v = new Vector3(0, alt, 0); var up = Vector3.up;
            var cam = PadGround.ClampAboveGround(v + ChaseCamera.DesiredOffset(up, H, alt), PadGround.GroundY);
            var rot = ChaseCamera.Aim(cam, v + up * H * 0.5f, VFov, alt, cam.y - PadGround.GroundY);
            // Horizon point straight ahead (same yaw), at the geometric dip below the camera.
            var fwd = rot * Vector3.forward; var flat = new Vector3(fwd.x, 0, fwd.z).normalized;
            float dip = ChaseCamera.HorizonDip(cam.y - PadGround.GroundY);
            var hz = Project(cam + (flat * Mathf.Cos(dip) - Vector3.up * Mathf.Sin(dip)) * 50000f, cam, rot, aspect);
            Assert.That(hz.y, Is.InRange(ChaseCamera.HorizonMin - 0.01f, ChaseCamera.HorizonMax + 0.01f), $"{device} T+{t}: horizon y");
            Assert.That(hz.y, Is.EqualTo(ChaseCamera.HorizonViewportY(Mathf.Asin(fwd.y), VFov, cam.y - PadGround.GroundY)).Within(0.01f));
            var b = Project(v, cam, rot, aspect); var top = Project(v + up * H, cam, rot, aspect);
            Assert.That(b.y, Is.InRange(0f, 1f), $"{device} T+{t}: vehicle base in frame");
            Assert.That(top.y, Is.InRange(0f, 1f), $"{device} T+{t}: vehicle top in frame");
            Assert.That(b.x, Is.InRange(0f, 1f)); Assert.That(top.x, Is.InRange(0f, 1f));
        }

        [TestCase(2.2f, 1290f / 2796f, "iPhone")]
        [TestCase(3.7f, 2064f / 2752f, "iPad13")]
        [TestCase(3.7f, 1290f / 2796f, "iPhone")]
        [TestCase(2.2f, 2064f / 2752f, "iPad13")]
        public void HorizonStaysInLowerBandAtCaptureTimes(float t, float aspect, string device) => Check(t, aspect, device);

        [Test]
        public void HorizonHeldThroughEarlyAscent()
        {
            for (float t = 1.5f; ; t += 0.5f)
            {
                if (0.5f * Accel * t * t > ChaseCamera.HoldFullAlt) break;
                Check(t, 1290f / 2796f, "iPhone"); Check(t, 2064f / 2752f, "iPad13");
            }
        }

        [Test]
        public void DefaultChaseFramesTheWholeStackWithSky()
        {
            const float alt = 6600f;
            var offset = ChaseCamera.DesiredOffset(Vector3.up, H, alt);
            float radial = new Vector2(offset.x, offset.z).magnitude / H;
            Assert.That(radial, Is.GreaterThan(4f), "default follow stands far enough back to show the stack and the sky");
            Assert.That(ChaseCamera.DesiredOffset(Vector3.up, H).y, Is.LessThan(PadGround.GroundY));
            var rot = ChaseCamera.Aim(offset, Vector3.up * H * 0.5f, VFov, alt, alt + offset.y);
            foreach (var aspect in new[] { 375f / 667f, 440f / 956f, 1032f / 1376f })
            {
                var b = Project(Vector3.zero, offset, rot, aspect);
                var top = Project(Vector3.up * H, offset, rot, aspect);
                float lo = Mathf.Min(b.y, top.y), hi = Mathf.Max(b.y, top.y);
                Assert.That(hi - lo, Is.InRange(0.12f, 0.34f), "stack span " + aspect);
                Assert.That(lo, Is.GreaterThan(0.08f), "sky above the stack");
                Assert.That(hi, Is.LessThan(0.92f), "sky below the stack");
                Assert.That(b.x, Is.InRange(0.15f, 0.85f));
                Assert.That(top.x, Is.InRange(0.15f, 0.85f));
            }
        }

        [Test]
        public void HoldBlendsOut()
        {
            Assert.That(ChaseCamera.HoldWeight(0f), Is.EqualTo(1f));
            Assert.That(ChaseCamera.HoldWeight(ChaseCamera.HoldEndAlt + 1f), Is.EqualTo(0f));
            Assert.That(ChaseCamera.DesiredOffset(Vector3.up, H, 1e6f), Is.EqualTo(ChaseCamera.DesiredOffset(Vector3.up, H)));
        }
    }
}
