using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // Regression: at max-Q (T+46 s, ~800 m/s, several 2 km floating-origin rebases) the chase camera
    // drifted kilometres from the rocket and framed only the ground plane ("flat dark quad").
    public class ChaseCameraTests
    {
        [Test]
        public void CameraStaysOnVehicleThroughRebasesAtMaxQSpeed()
        {
            const float h = 70f, dt = 1f / 60f, speed = 806f, rebase = 2000f;
            var up = new Vector3(0.5f, 0.866f, 0f);            // pitched over ~30 deg
            var dir = up.normalized;
            Vector3 vehicle = Vector3.zero;
            Vector3 offset = ChaseCamera.DesiredOffset(up, h);
            float maxErr = 0; int rebases = 0;
            for (int i = 0; i < 60 * 30; i++)                  // 30 s of flight
            {
                vehicle += dir * speed * dt;
                if (Mathf.Abs(vehicle.x) > rebase || Mathf.Abs(vehicle.y) > rebase) { vehicle = Vector3.zero; rebases++; }
                offset = ChaseCamera.StepOffset(offset, ChaseCamera.DesiredOffset(up, h), 4f, dt);
                var cam = vehicle + offset;
                maxErr = Mathf.Max(maxErr, Vector3.Distance(cam, vehicle + ChaseCamera.DesiredOffset(up, h)));
            }
            Assert.That(rebases, Is.GreaterThan(5));
            Assert.That(maxErr, Is.LessThan(0.01f * h), "camera must not lag or jump on rebase");
        }

        [Test]
        public void OffsetSmoothsTowardDesiredFraming()
        {
            var a = ChaseCamera.DesiredOffset(Vector3.up, 70f);
            var b = ChaseCamera.DesiredOffset(new Vector3(0.7071f, 0.7071f, 0), 70f);
            var o = a;
            for (int i = 0; i < 120; i++) o = ChaseCamera.StepOffset(o, b, 4f, 1f / 60f);
            Assert.That(Vector3.Distance(o, b), Is.LessThan(0.01f * Vector3.Distance(a, b)));
        }
    }
}
