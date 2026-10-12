using Kestrel.Pad;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // P0 fix 1: ground/horizon vanished after liftoff and tower legs hung below the deck into the sky.
    public class PadGroundTests
    {
        [Test]
        public void ChaseCameraAtLiftoffIsClampedAboveGround()
        {
            const float h = 70f;
            var cam = Vector3.zero + ChaseCamera.DesiredOffset(Vector3.up, h);
            Assert.That(cam.y, Is.LessThan(PadGround.GroundY), "precondition: raw chase offset is below the ground plane");
            var c = PadGround.ClampAboveGround(cam, PadGround.GroundY);
            Assert.That(c.y, Is.GreaterThanOrEqualTo(PadGround.GroundY + PadGround.MinCameraClearance - 1e-4f));
            Assert.That(c.x, Is.EqualTo(cam.x)); Assert.That(c.z, Is.EqualTo(cam.z));
        }

        [Test]
        public void CameraHighAboveGroundIsUntouched()
        {
            var p = new Vector3(10, 500, -20);
            Assert.That(PadGround.ClampAboveGround(p, -1f), Is.EqualTo(p));
        }

        [TestCase(PadArchetype.Rail), TestCase(PadArchetype.Tower), TestCase(PadArchetype.TowerStrongback)]
        public void DeckCoversEveryGroundedMember(PadArchetype type)
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var L = PadGenerator.Generate(seed, type, new PadFit(70f, 6f, 6f));
                float r = PadGround.DeckRadius(L);
                foreach (var m in L.Members)
                {
                    if (Mathf.Min(m.A.Y, m.B.Y) > 0.5f) continue;
                    Assert.That(Mathf.Sqrt(m.A.X * m.A.X + m.A.Z * m.A.Z), Is.LessThan(r), $"{type} seed {seed} {m.Id}");
                    Assert.That(Mathf.Sqrt(m.B.X * m.B.X + m.B.Z * m.B.Z), Is.LessThan(r), $"{type} seed {seed} {m.Id}");
                }
            }
        }

        [Test]
        public void GroundIsBelowDeckTopAndReachesFarPlane()
        {
            Assert.That(PadGround.GroundY, Is.LessThan(0f), "ground must not be coplanar with the deck top");
            Assert.That(PadGround.Size * 0.5f, Is.GreaterThanOrEqualTo(100000f));
        }
    }
}
