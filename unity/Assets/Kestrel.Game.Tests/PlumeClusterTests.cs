using System.Collections.Generic;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // Cluster-diameter follow-up to #119: count every engine lit at liftoff, strap-on boosters included.
    public class PlumeClusterTests
    {
        static Design Heavy() => new Design
        {
            id = "test-heavy",
            stages = new List<StagePart>
            {
                new StagePart { engineCount = 9, diameter = 3.7 },   // core, lights at T0
                new StagePart { engineCount = 1, diameter = 3.7 },   // upper, air-starts: not counted
            },
            strapOns = new StagePart { count = 2, engineCount = 9, diameter = 3.7 },
        };

        [Test]
        public void HeavyCountsCoreAndBoostersButNotUpperStage()
        {
            Assert.That(PlumeCluster.LiftoffEngineCount(Heavy()), Is.EqualTo(27));
            var single = Heavy(); single.strapOns = null;
            Assert.That(PlumeCluster.LiftoffEngineCount(single), Is.EqualTo(9));
        }

        [Test]
        public void HeavyFootprintIsWiderThanCoreOnlyAndInsideNozzleBounds()
        {
            const float exitD = 0.9f;
            // Three 3.7 m cores side by side, 9 nozzles each (centre + ring of 8).
            var exits = new List<Vector3>();
            foreach (var cx in new[] { -3.8f, 0f, 3.8f })
            {
                exits.Add(new Vector3(cx, 0, 0));
                for (int j = 0; j < 8; j++) { float a = j * Mathf.PI / 4; exits.Add(new Vector3(cx + Mathf.Cos(a) * 1.4f, 0, Mathf.Sin(a) * 1.4f)); }
            }
            float oldCoreOnly = Mathf.Sqrt(9) * exitD;
            float f = PlumeCluster.Footprint(PlumeCluster.LiftoffEngineCount(Heavy()), exitD, exits);
            float bound = PlumeCluster.BoundingDiameter(exits, exitD);
            Assert.That(f, Is.EqualTo(bound).Within(1e-4f), "covers every lit nozzle, boosters included");
            Assert.That(f, Is.GreaterThanOrEqualTo(Mathf.Sqrt(27) * exitD), "never below the area-equivalent of 27 nozzles");
            Assert.That(f, Is.GreaterThan(3f * oldCoreOnly), "3-core heavy: ~3x the old single-core column");
            // Body built from it is wide enough (lead: was ~0.2x the cluster).
            var L = PlumeShape.ComputeLayout(1f, 1f, f, 6f, 70f, 20f);
            Assert.That(PlumeShape.BodyVisibleWidth(L), Is.GreaterThanOrEqualTo(0.9f * bound));
        }

        [Test]
        public void FootprintNeverBelowAreaEquivalentAndFloored()
        {
            var exits = new List<Vector3> { new Vector3(0.3f, 0, 0), new Vector3(-0.3f, 0, 0) };
            Assert.That(PlumeCluster.Footprint(50, 1f, exits), Is.EqualTo(Mathf.Sqrt(50f)).Within(1e-4f));
            Assert.That(PlumeCluster.Footprint(1, 0.3f, null), Is.EqualTo(1f));
        }
    }
}
