using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // Regression (v4 captures): Design panel hid the bottom of the stack and the pad deck.
    public class DesignFramingTests
    {
        [Test]
        public void ProjectedStackAndDeckLandAboveThePanel()
        {
            // Rocket 70 m, camera placed like SliceApp.FrameCamera, panel covering the bottom 27%.
            foreach (var aspect in new[] { 375f / 667f, 440f / 956f, 1024f / 1366f })
            foreach (var covered in new[] { 0.27f, 0.40f, 0.08f })
            {
                float h = 70f, f = 1f - covered;
                float k = DesignFraming.DistanceScale(f);
                var aim = new Vector3(0, h * DesignFraming.AimHeight, 0);
                var pos = aim + new Vector3(h * 0.9f, h * (0.45f - DesignFraming.AimHeight), -h * 1.4f) * k;
                var view = Matrix4x4.TRS(pos, Quaternion.LookRotation(aim - pos), new Vector3(1, 1, -1)).inverse;
                var proj = DesignFraming.ShiftedProjection(Matrix4x4.Perspective(50f, aspect, 0.5f, 200000f), f);
                float bandBottom = 1f - 2f * f;
                foreach (var y in new[] { -0.5f, h })
                {
                    var c = proj * (view * new Vector4(0, y, 0, 1));
                    float ndc = c.y / c.w;
                    Assert.That(ndc, Is.GreaterThan(bandBottom), $"y={y} below panel (aspect {aspect}, covered {covered})");
                    Assert.That(ndc, Is.LessThan(1f), $"y={y} off top (aspect {aspect}, covered {covered})");
                }
            }
        }

        [Test]
        public void StackFillsMostOfTheBand()
        {
            float h = 70f, f = 0.73f, k = DesignFraming.DistanceScale(f);
            var aim = new Vector3(0, h * DesignFraming.AimHeight, 0);
            var pos = aim + new Vector3(h * 0.9f, h * (0.45f - DesignFraming.AimHeight), -h * 1.4f) * k;
            var view = Matrix4x4.TRS(pos, Quaternion.LookRotation(aim - pos), new Vector3(1, 1, -1)).inverse;
            var proj = DesignFraming.ShiftedProjection(Matrix4x4.Perspective(50f, 375f / 667f, 0.5f, 200000f), f);
            float Ndc(float y) { var c = proj * (view * new Vector4(0, y, 0, 1)); return c.y / c.w; }
            Assert.That((Ndc(h) - Ndc(-0.5f)) / (2f * f), Is.GreaterThan(0.7f));
        }

        [TestCase(750f, 1334f, 0f, 2f)]
        [TestCase(1320f, 2868f, 0f, 3f)]
        [TestCase(2064f, 2752f, 96f, 2f)]
        [TestCase(1320f, 2868f, 460f, 3f)]
        public void PixelsPerPointFallback(float w, float h, float dpi, float expected) =>
            Assert.That(DesignFraming.PixelsPerPoint(w, h, dpi), Is.EqualTo(expected));

        [Test]
        public void VisibleFractionIsClamped()
        {
            Assert.That(DesignFraming.VisibleFraction(1000, 730), Is.EqualTo(0.73f).Within(1e-5));
            Assert.That(DesignFraming.VisibleFraction(1000, 100), Is.EqualTo(DesignFraming.MinVisibleFraction));
            Assert.That(DesignFraming.VisibleFraction(0, 100), Is.EqualTo(1f));
        }

        // Ported from #120's PadFramingTests: vehicle + tower + deck (PadGround.DeckRadius) all fit the band above the
        // panel on iPhone, Pro Max and iPad, using main's DesignFraming rig instead of a parallel PadFraming.
        static readonly object[] Devices =
        {
            new object[] { "iPhone", 1290, 2796 },
            new object[] { "ProMax", 1320, 2868 },
            new object[] { "iPad13", 2064, 2752 },
        };

        static System.Collections.Generic.List<Vector3> SliceScene(float h, float r)
        {
            var tower = new System.Collections.Generic.List<Vector3>();
            foreach (var x in new[] { -(r + 3f), -(r + 15f) }) foreach (var y in new[] { 0f, h * 1.08f + 5f }) foreach (var z in new[] { -2f, 20f }) tower.Add(new Vector3(x, y, z));
            return DesignFraming.SubjectPoints(h, r, PadGround.DeckRadius(null), tower);
        }

        [TestCaseSource(nameof(Devices))]
        public void VehicleTowerAndDeckFitAboveDesignPanel(string name, int w, int hpx)
        {
            const float fov = 50f;
            float aspect = (float)w / hpx;
            foreach (var covered in new[] { 0.27f, 0.40f })
            foreach (var (h, r) in new[] { (70f, 6f), (40f, 1.5f), (110f, 9f) })
            {
                float f = 1f - covered, bandBottom = 1f - 2f * f;
                var pts = SliceScene(h, r);
                float k = DesignFraming.FitDistanceScale(pts, h, aspect, fov, f);
                Assert.That(k, Is.GreaterThanOrEqualTo(DesignFraming.DistanceScale(f) - 1e-5f), name);
                float minY = 1, maxY = -1;
                foreach (var p in pts)
                {
                    var n = DesignFraming.Project(p, h, k, aspect, fov, f);
                    Assert.That(n.x, Is.InRange(-1f, 1f), $"{name} h={h} covered={covered} x of {p}");
                    Assert.That(n.y, Is.InRange(bandBottom, 1f), $"{name} h={h} covered={covered} y of {p}");
                    minY = Mathf.Min(minY, n.y); maxY = Mathf.Max(maxY, n.y);
                }
                Assert.That(maxY - minY, Is.GreaterThan(0.5f * 2f * f), $"{name} h={h}: scene should fill the band, not be a speck");
            }
        }

        [Test]
        public void FitFallsBackToDistanceScaleWithoutPoints()
        {
            Assert.That(DesignFraming.FitDistanceScale(null, 70f, 0.46f, 50f, 0.73f), Is.EqualTo(DesignFraming.DistanceScale(0.73f)));
        }

        [Test]
        public void WideDeckPullsCameraBack()
        {
            float f = 0.73f, a = 1290f / 2796f;
            float kSmall = DesignFraming.FitDistanceScale(DesignFraming.SubjectPoints(40f, 2f, 15f), 40f, a, 50f, f);
            float kWide = DesignFraming.FitDistanceScale(DesignFraming.SubjectPoints(40f, 2f, 60f), 40f, a, 50f, f);
            Assert.That(kWide, Is.GreaterThan(kSmall));
        }
    }
}
