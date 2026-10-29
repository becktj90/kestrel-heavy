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

        // #381 iPad flight frame: the telemetry strip reaches ~18% down a 4:3 screen and the nose sat under it.
        [TestCase(2064f / 2752f, 0.184f)]
        [TestCase(1320f / 2868f, 0.117f)]
        public void KeepBelowPutsTheNoseUnderTheHudStrip(float aspect, float cover)
        {
            var cam = new Vector3(0f, 0f, -200f);
            var b = new Vector3(0f, 0f, 0f); var top = b + Vector3.up * H;
            var level = Quaternion.LookRotation(Vector3.forward, Vector3.up);   // nose at ~0.88: under a 0.82 strip edge
            float maxY = 1f - cover - ChaseCamera.HudMargin;
            var rot = ChaseCamera.KeepBelow(level, cam, top, b, VFov, maxY);
            var t = Project(top, cam, rot, aspect); var f = Project(b, cam, rot, aspect);
            Assert.That(t.y, Is.LessThanOrEqualTo(maxY + 0.005f), "nose under the strip");
            Assert.That(f.y, Is.GreaterThan(0.05f), "base still in frame");
            Assert.That(t.y, Is.GreaterThan(maxY - 0.02f), "raised only as far as needed");
            // Already clear: unchanged.
            var low = ChaseCamera.Aim(cam, b + Vector3.up * H * 0.5f, VFov, 0f, 0f);
            Assert.That(Quaternion.Angle(low, ChaseCamera.KeepBelow(low, cam, top, b, VFov, 0.95f)), Is.LessThan(0.01f));
            // No cover measured: unchanged.
            Assert.That(Quaternion.Angle(level, ChaseCamera.KeepBelow(level, cam, top, b, VFov, 1f)), Is.LessThan(0.01f));
        }

        // #381 landing frames: the followed booster's engines, flame and the deck sat behind the bottom control row.
        // Covers are the measured top strip / bottom tools-row shares at each App Store size and the 390x844 phone.
        [TestCase(1320f / 2868f, 0.117f, 0.27f, "6.9in")]
        [TestCase(1206f / 2622f, 0.120f, 0.28f, "6.3in")]
        [TestCase(2064f / 2752f, 0.184f, 0.40f, "iPad 13in")]
        [TestCase(390f / 844f, 0.130f, 0.30f, "390x844")]
        public void FitBandKeepsTheLandingBoosterBetweenTheHudBars(float aspect, float topCover, float bottomCover, string device)
        {
            const float Booster = 26f, R = 1.85f, Flame = 12.2f;
            float yMax = 1f - topCover - 0.03f, yMin = bottomCover + 0.03f;
            foreach (float alt in new[] { 40f, 15f, 3f, 120f })
            {
                var b = new Vector3(0f, alt, 0f);
                var cam = b + ChaseCamera.DesiredOffset(Vector3.up, Booster, alt);
                var rot = ChaseCamera.Aim(cam, b + Vector3.up * Booster * 0.25f, VFov, alt, cam.y - PadGround.GroundY);
                var pts = new Vector3[9]; int n = 0;
                float lo = alt - Flame, hiY = alt + Booster;
                for (int i = 0; i < 8; i++)
                    pts[n++] = new Vector3((i & 1) == 0 ? -R : R, (i & 2) == 0 ? lo : hiY, (i & 4) == 0 ? -R : R);
                pts[n++] = new Vector3(0f, Mathf.Max(0f, lo - 1.6f * Booster), 0f);   // the deck once it is near
                var fit = ChaseCamera.FitBand(rot, cam, pts, n, VFov, aspect, yMin, yMax);
                foreach (var p in pts)
                {
                    var v = Project(p, fit, rot, aspect);
                    Assert.That(v.z, Is.GreaterThan(0f), $"{device} alt {alt}: in front");
                    Assert.That(v.y, Is.InRange(yMin - 0.005f, yMax + 0.005f), $"{device} alt {alt}: {p} between the HUD bars");
                    Assert.That(v.x, Is.InRange(0.045f, 0.955f), $"{device} alt {alt}: {p} across the frame");
                }
                // Widens only: never closer than the follow offset.
                Assert.That(Vector3.Dot(fit - cam, rot * Vector3.forward), Is.LessThanOrEqualTo(1e-3f), $"{device}: backs off only");
                // Tight: the band is filled at one end or the other (not zoomed out for nothing).
                float top = float.NegativeInfinity, bot = float.PositiveInfinity;
                foreach (var p in pts) { var v = Project(p, fit, rot, aspect); top = Mathf.Max(top, v.y); bot = Mathf.Min(bot, v.y); }
                if (Vector3.Dot(cam - fit, rot * Vector3.forward) > 0.01f)
                    Assert.That(Mathf.Min(yMax - top, bot - yMin), Is.LessThan(0.01f), $"{device} alt {alt}: no needless zoom-out");
            }
            // Already inside a generous band: unchanged. Degenerate band: unchanged.
            var c0 = new Vector3(0f, 10f, -400f); var r0 = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var one = new[] { new Vector3(0f, 10f, 0f) };
            Assert.That(ChaseCamera.FitBand(r0, c0, one, 1, VFov, aspect, 0.1f, 0.9f), Is.EqualTo(c0));
            Assert.That(ChaseCamera.FitBand(r0, c0, one, 1, VFov, aspect, 0.6f, 0.55f), Is.EqualTo(c0));
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
