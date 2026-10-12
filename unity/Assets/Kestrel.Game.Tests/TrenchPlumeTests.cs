using Kestrel.Pad;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class TrenchPlumeTests
    {
        [Test]
        public void SurfaceRadiusMatchesDeckDisc()
        {
            var layout = PadGenerator.Generate(3, PadArchetype.Tower, new PadFit(70f, 2.5f, 6f));
            Assert.That(TrenchGrammar.SurfaceRadius(layout), Is.EqualTo(PadGround.DeckRadius(layout)).Within(1e-4f));
        }

        [Test]
        public void AimingTheCloudLeavesPlumePhysicsAlone()
        {
            var root = new GameObject("plume");
            var pad = new GameObject("pad");
            try
            {
                var pc = PlumeController.Create(root.transform, Vector3.zero, 14f, null);
                pc.Configure(70f, 6f);
                pc.AttachGroundCloud(pad.transform, 18f, 80f);
                var cloud = pc.Emitters[3];
                float speedMin = cloud.main.startSpeed.constantMin;
                float speedMax = cloud.main.startSpeed.constantMax;
                float lifeMin = cloud.main.startLifetime.constantMin;
                float lifeMax = cloud.main.startLifetime.constantMax;
                float sizeMin = cloud.main.startSize.constantMin;
                float sizeMax = cloud.main.startSize.constantMax;
                bool emission = cloud.emission.enabled;
                float rate = cloud.emission.rateOverTime.constant;
                bool cloudCollision = cloud.collision.enabled;
                var velocity = cloud.velocityOverLifetime;
                bool velocityOn = velocity.enabled;
                var body = pc.Body;
                float bounce = body.collision.bounce.constant;
                float dampen = body.collision.dampen.constant;
                float lifeLoss = body.collision.lifetimeLoss.constant;
                var bodyPos = body.transform.localPosition;
                var corePos = pc.Emitters[0].transform.localPosition;
                float bodySpeed = body.main.startSpeed.constant;
                float cloudY = cloud.transform.localPosition.y;

                var layout = PadGenerator.Generate(4, PadArchetype.Tower, new PadFit(70f, 2.5f, 6f));
                var mouths = TrenchGrammar.Mouths(layout.Trench, TrenchGrammar.SurfaceRadius(layout));
                var centers = new Vector3[mouths.Count];
                var dirs = new Vector3[mouths.Count];
                var width = new float[mouths.Count];
                var height = new float[mouths.Count];
                for (int i = 0; i < mouths.Count; i++)
                {
                    centers[i] = new Vector3(mouths[i].X, mouths[i].Y, mouths[i].Z);
                    dirs[i] = new Vector3(mouths[i].DirX, 0f, mouths[i].DirZ);
                    width[i] = mouths[i].Width;
                    height[i] = mouths[i].Height;
                }
                pc.AimGroundCloudAtMouths(centers, dirs, width, height);

                Assert.That(cloud.main.startSpeed.constantMin, Is.EqualTo(speedMin));
                Assert.That(cloud.main.startSpeed.constantMax, Is.EqualTo(speedMax));
                Assert.That(cloud.main.startLifetime.constantMin, Is.EqualTo(lifeMin));
                Assert.That(cloud.main.startLifetime.constantMax, Is.EqualTo(lifeMax));
                Assert.That(cloud.main.startSize.constantMin, Is.EqualTo(sizeMin));
                Assert.That(cloud.main.startSize.constantMax, Is.EqualTo(sizeMax));
                Assert.That(cloud.emission.enabled, Is.EqualTo(emission));
                Assert.That(cloud.emission.rateOverTime.constant, Is.EqualTo(rate));
                Assert.That(cloud.collision.enabled, Is.EqualTo(cloudCollision));
                Assert.That(cloud.velocityOverLifetime.enabled, Is.EqualTo(velocityOn));
                Assert.That(body.collision.bounce.constant, Is.EqualTo(bounce));
                Assert.That(body.collision.dampen.constant, Is.EqualTo(dampen));
                Assert.That(body.collision.lifetimeLoss.constant, Is.EqualTo(lifeLoss));
                Assert.That(body.transform.localPosition, Is.EqualTo(bodyPos));
                Assert.That(pc.Emitters[0].transform.localPosition, Is.EqualTo(corePos));
                Assert.That(body.main.startSpeed.constant, Is.EqualTo(bodySpeed));
                Assert.That(cloud.transform.localPosition.y, Is.EqualTo(cloudY).Within(1e-4f));
                Assert.That(cloud.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(cloud.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Mesh));
                Assert.That(cloud.shape.mesh, Is.Not.Null);
                Assert.That(cloud.shape.mesh.vertexCount, Is.GreaterThanOrEqualTo(mouths.Count * 4));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(pad);
            }
        }

        [Test]
        public void GroundCutFollowsTheTrenchAndAimsTheCloud()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var tower = new GameObject("PadTower");
            var pad = new GameObject("Pad");
            var plume = new GameObject("plume");
            Mesh cut = null;
            try
            {
                pad.transform.SetParent(tower.transform, false);
                pad.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                ground.transform.localScale = new Vector3(PadGround.Size / 10f, 1f, PadGround.Size / 10f);
                var pc = PlumeController.Create(plume.transform, Vector3.zero, 8f, null);
                pc.Configure(70f, 2.5f);
                pc.AttachGroundCloud(tower.transform, 18f, 80f);
                var cloud = pc.Emitters[3];
                float speedMin = cloud.main.startSpeed.constantMin;
                float lifeLoss = pc.Body.collision.lifetimeLoss.constant;
                var layout = PadGenerator.Generate(5, PadArchetype.TowerStrongback, new PadFit(70f, 2.5f, 6f));
                Assert.That(deck.GetComponent<MeshRenderer>().enabled, Is.True);
                PadDetailLink.Apply(pc, tower.transform, ground.transform, deck.transform, layout);
                Assert.That(deck.GetComponent<MeshRenderer>().enabled, Is.False, "the generated slab replaces the cylinder");
                cut = ground.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(cut, Is.Not.Null);
                Assert.That(cut.name, Is.EqualTo(PadDetailLink.GroundMeshName));
                Assert.That(cut.vertexCount, Is.GreaterThan(8));
                Assert.That(cloud.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Mesh));
                Assert.That(cloud.shape.mesh, Is.Not.Null);
                Assert.That(cloud.main.startSpeed.constantMin, Is.EqualTo(speedMin));
                Assert.That(pc.Body.collision.lifetimeLoss.constant, Is.EqualTo(lifeLoss));
                Assert.That(cloud.transform.localPosition.y, Is.GreaterThan(0f));
            }
            finally
            {
                if (cut != null && cut.name == PadDetailLink.GroundMeshName) Object.DestroyImmediate(cut);
                Object.DestroyImmediate(ground);
                Object.DestroyImmediate(deck);
                Object.DestroyImmediate(tower);
                Object.DestroyImmediate(plume);
            }
        }
    }
}
