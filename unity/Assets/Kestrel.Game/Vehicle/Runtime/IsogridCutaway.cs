// Real isogrid rib geometry, ONLY for cutaway and the Vehicle Detail Lab (approved: normal map elsewhere).
// Ribs are kit beams (KitMesh.Beam) drawn through one kit InstanceBatch on the inner skin of the removed sector.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    public static class IsogridCutaway
    {
        public const int MaxRibs = 3000;
        public const float RibWidth = 0.012f, RibDepth = 0.03f;

        /// <summary>Kit batch for rib instances (one mesh, one kit material, instanced).</summary>
        public static InstanceBatch NewBatch() => new InstanceBatch(KitMesh.Beam(RibWidth, RibDepth), MaterialLibrary.Get(MaterialRole.PaintedSteel), DetailSize.Small);

        /// <summary>Tank-local rib matrices for the barrel sector [cutFrom, cutTo) radians. Empty if the tank has no
        /// barrel. The cell grows until the rib count fits MaxRibs.</summary>
        public static List<Matrix4x4> RibMatrices(TankShellSpec s, float cutFrom, float cutTo)
        {
            var list = new List<Matrix4x4>();
            if (s.BarrelLength <= 1e-4f || cutTo <= cutFrom) return list;
            float R = s.Radius - RibDepth * 0.5f, width = (cutTo - cutFrom) * s.Radius;
            var edges = new List<(double u0, double v0, double u1, double v1)>();
            double cell = s.IsogridCell;
            while (!Isogrid.CutawayEdges(width, s.BarrelLength, cell, MaxRibs, edges)) cell *= 1.25;

            foreach (var e in edges)
            {
                Vector3 a = OnSkin(e.u0, e.v0, R, s.Radius, cutFrom, s.BarrelBottom), b = OnSkin(e.u1, e.v1, R, s.Radius, cutFrom, s.BarrelBottom);
                Vector3 mid = (a + b) * 0.5f, outward = new Vector3(mid.x, 0, mid.z).normalized, dir = b - a;
                float len = dir.magnitude; if (len < 1e-5f) continue;
                // Beam is along +Y with depth on Z: point +Y along the rib and +Z out of the skin.
                var rot = Quaternion.LookRotation(outward, dir / len);
                list.Add(Matrix4x4.TRS(a, rot, new Vector3(1, len, 1)));
            }
            return list;
        }

        static Vector3 OnSkin(double u, double v, float r, float skinR, float az0, float y0)
        {
            float a = az0 + (float)(u / skinR);
            return new Vector3(r * Mathf.Sin(a), y0 + (float)v, r * Mathf.Cos(a));
        }
    }
}
