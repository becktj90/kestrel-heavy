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
    }
}
