// Effective engine-cluster footprint for the plume (follow-up to #119). SliceApp used
// sqrt(stage-0 engine count) x exit diameter, which ignored strap-on boosters that also light at T0
// (Stack.ToSimStages: strap-ons are the first, parallel phase, burning alongside the core from liftoff).
// #119 v3 capture: the area-equivalent diameter (sqrt N x d) still read as one engine-wide column (~0.2x the
// 3-core cluster) once the soft sprite falloff is applied, so the footprint is now the bounding diameter of
// every nozzle lit at liftoff (the merged jets cover the whole cluster), never less than the area-equivalent.
using System.Collections.Generic;
using Kestrel.Core.Vehicle;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class PlumeCluster
    {
        /// <summary>Engines lit at T0: core stage 0 plus every strap-on's engines. Upper stages air-start later.</summary>
        public static int LiftoffEngineCount(Design d)
        {
            if (d == null || d.stages == null || d.stages.Count == 0) return 0;
            int n = Mathf.Max(0, d.stages[0].engineCount);
            if (d.StrapOnCount > 0) n += d.strapOns.count * Mathf.Max(0, d.strapOns.engineCount);
            return n;
        }

        /// <summary>sqrt(total nozzle exit area / pi) * 2 for n nozzles of diameter exitD.</summary>
        public static float AreaEquivalentDiameter(int n, float exitD) => Mathf.Sqrt(Mathf.Max(0, n)) * Mathf.Max(0f, exitD);

        /// <summary>Diameter of the circle (about the vehicle axis) enclosing every lit nozzle exit.</summary>
        public static float BoundingDiameter(IReadOnlyList<Vector3> exits, float exitD)
        {
            if (exits == null || exits.Count == 0) return exitD;
            float r = 0f;
            foreach (var e in exits) r = Mathf.Max(r, Mathf.Sqrt(e.x * e.x + e.z * e.z));
            return 2f * r + exitD;
        }

        /// <summary>Effective cluster footprint fed to PlumeController / PlumeShape (min 1 m).</summary>
        public static float Footprint(int litEngines, float exitD, IReadOnlyList<Vector3> exits)
        {
            float area = AreaEquivalentDiameter(litEngines, exitD);
            if (exits != null && exits.Count > 0) area = Mathf.Max(area, BoundingDiameter(exits, exitD));
            return Mathf.Max(1f, area);
        }
    }
}
