// The fairing mesh as the flight picture, the designer and the Starter preview draw it (RocketMeshBuilder.BuildParts and Build):
// its base is exactly as wide as the stage under it, the barrel keeps the data fairing diameter, and a cone joins them with no ledge.
// Physics' spec: VEHICLE_ENGINEERING_RULES.md "Fairing joint and flare". Uses only RocketMeshBuilder's public API, so the identical test
// compiles against the old geometry and fails there (proof on the before branch).
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Hangar;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class FairingJointMeshTests
    {
        static Catalog catalog;
        static Catalog C => catalog ??= Catalog.LoadFromRepo(FairingJointTestData.Root);

        /// <summary>Widest radius among vertices at each distinct height (rounded to 0.1 mm), sorted by height.</summary>
        public static List<Vector2> Silhouette(Mesh mesh, float y0, float y1)
        {
            var best = new SortedDictionary<int, float>();
            foreach (var v in mesh.vertices)
            {
                if (v.y < y0 - 1e-4f || v.y > y1 + 1e-4f) continue;
                int key = Mathf.RoundToInt(v.y * 1000f);
                float r = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                if (!best.TryGetValue(key, out var old) || r > old) best[key] = r;
            }
            var list = new List<Vector2>();
            foreach (var kv in best) list.Add(new Vector2(best[kv.Key], kv.Key / 1000f));
            return list;
        }

        static string Check(Design d, Mesh fairingMesh, float y0, float y1)
        {
            float rj = (float)d.stages[d.stages.Count - 1].diameter / 2, rf = (float)d.fairingDiameter / 2;
            float L = (float)d.fairingLength;
            var sil = Silhouette(fairingMesh, y0, y1);
            if (sil.Count < 3) return "fairing mesh has no silhouette";
            // 1. The joint diameter equals the top stage diameter.
            if (Mathf.Abs(sil[0].y - y0) > 2e-3f || Mathf.Abs(sil[0].x - rj) > 2e-3f)
                return "joint diameter " + 2 * sil[0].x + " m at y " + sil[0].y + " != stage diameter " + 2 * rj;
            // 2. The barrel is the data fairing diameter, and nothing is wider than both.
            float barrelY = y0 + L * (1f - RocketMeshBuilder.NoseFraction(d.nose));
            float barrel = 0f, widest = 0f;
            foreach (var s in sil) { widest = Mathf.Max(widest, s.x); if (Mathf.Abs(s.y - barrelY) < 2e-3f) barrel = s.x; }
            if (Mathf.Abs(barrel - rf) > 2e-3f) return "barrel diameter " + 2 * barrel + " != fairing diameter " + 2 * rf;
            if (widest > Mathf.Max(rj, rf) + 2e-3f) return "wider than the stage and the fairing: " + 2 * widest;
            // 3. No ledge: every radius change below the barrel spans real height, on a cone no steeper than 30 degrees.
            float tan30 = Mathf.Tan(30f * Mathf.Deg2Rad);
            for (int i = 1; i < sil.Count && sil[i - 1].y < barrelY - 2e-3f; i++)
            {
                float dR = sil[i].x - sil[i - 1].x, dY = sil[i].y - sil[i - 1].y;
                if (Mathf.Abs(dR) < 1e-4f) continue;
                if (dY < 2e-3f) return "ledge: radius steps " + dR + " m over " + dY + " m at y " + sil[i - 1].y;
                if (Mathf.Abs(dR / dY) > tan30 + 1e-3f) return "shoulder steeper than 30 degrees at y " + sil[i - 1].y;
            }
            return null;
        }

        [Test]
        public void EveryRealDesign_FairingPartMesh_JointIsTheStage_BarrelIsTheData_NoLedge()
        {
            var rows = FairingJointTestData.All();
            Assert.That(rows.Count, Is.GreaterThanOrEqualTo(40));
            var failures = new List<string>();
            foreach (var row in rows)
            {
                var parts = RocketMeshBuilder.BuildParts(C, row.Value);
                bool found = false;
                foreach (var p in parts)
                {
                    if (p.Part != PartRef.Fairing) continue;
                    found = true;
                    var why = Check(row.Value, p.Mesh, p.Y0, p.Y1);
                    if (why != null) failures.Add(row.Key + ": " + why);
                }
                if (!found) failures.Add(row.Key + ": no fairing part");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void EveryRealDesign_FlightMesh_FairingBaseEqualsTheStageTop()
        {
            var failures = new List<string>();
            foreach (var row in FairingJointTestData.All())
            {
                var d = row.Value;
                var built = RocketMeshBuilder.Build(C, d);
                float y0 = built.fairing.BaseY, rj = (float)d.stages[d.stages.Count - 1].diameter / 2;
                float baseR = 0f, cap = Mathf.Max(rj, (float)d.fairingDiameter / 2) + 1e-3f;
                foreach (var v in built.mesh.vertices)
                {
                    float r = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                    if (Mathf.Abs(v.y - y0) < 1e-3f && r <= cap) baseR = Mathf.Max(baseR, r);   // strap-ons sit off the axis, wider than the core
                }
                if (Mathf.Abs(baseR - rj) > 2e-3f) failures.Add(row.Key + ": flight mesh joint radius " + baseR + " != stage radius " + rj);
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        static string PayloadCheck(Design d, List<RocketMeshBuilder.PartMesh> parts)
        {
            var fairing = parts.Find(p => p.Part == PartRef.Fairing);
            var payload = parts.Find(p => p.Part == PartRef.Payload);
            if (fairing == null || payload == null) return "no fairing or payload part";
            float rj = RocketMeshBuilder.JointRadius(d), rf = (float)d.fairingDiameter / 2, L = (float)d.fairingLength;
            float hs = Kestrel.Vehicle.FairingShoulder.Height(rj, rf, L, d.nose);
            float barrelTop = fairing.Y0 + L * (1f - Kestrel.Vehicle.FairingShoulder.NoseFraction(d.nose));
            if (payload.Y0 < fairing.Y0 + hs - 1e-4f) return "payload base " + payload.Y0 + " is inside the shoulder (ends at " + (fairing.Y0 + hs) + ")";
            if (payload.Y1 > barrelTop + 1e-4f && hs > 0f) return "payload top " + payload.Y1 + " is above the barrel (" + barrelTop + ")";
            if (payload.Radius > rf + 1e-4f) return "payload radius " + payload.Radius + " wider than the barrel " + rf;
            return null;
        }

        [Test]
        public void ExplodedPayload_SitsAboveTheShoulder_OnEveryRealDesign_AndOnAFlaredOne()
        {
            var failures = new List<string>();
            int shouldered = 0;
            foreach (var row in FairingJointTestData.All())
            {
                var d = row.Value;
                var parts = RocketMeshBuilder.BuildParts(C, d);
                try { var why = PayloadCheck(d, parts); if (why != null) failures.Add(row.Key + ": " + why); }
                finally { foreach (var p in parts) { UnityEngine.Object.DestroyImmediate(p.Mesh); if (p.Engines != null) UnityEngine.Object.DestroyImmediate(p.Engines); } }

                // The same design with a fairing 1.45 x the stage (under the flare advisory): the shoulder is long, the payload must clear it.
                var flared = Design.FromJson(d.ToJson());
                flared.fairingDiameter = 1.45 * flared.stages[flared.stages.Count - 1].diameter;
                float hs = Kestrel.Vehicle.FairingShoulder.Height(RocketMeshBuilder.JointRadius(flared), (float)flared.fairingDiameter / 2, (float)flared.fairingLength, flared.nose);
                float cyl = (float)flared.fairingLength * (1f - Kestrel.Vehicle.FairingShoulder.NoseFraction(flared.nose));
                if (hs > 0.15f * cyl) shouldered++;
                var fp = RocketMeshBuilder.BuildParts(C, flared);
                try { var why = PayloadCheck(flared, fp); if (why != null) failures.Add(row.Key + " (flared 1.45x): " + why); }
                finally { foreach (var p in fp) { UnityEngine.Object.DestroyImmediate(p.Mesh); if (p.Engines != null) UnityEngine.Object.DestroyImmediate(p.Engines); } }
            }
            Assert.That(shouldered, Is.GreaterThan(0), "no case where the shoulder is longer than the old 0.15 x cylinder payload offset");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void FairingTotalLengthAndStackHeightAreUnchanged()
        {
            foreach (var row in FairingJointTestData.All())
            {
                var d = row.Value;
                var parts = RocketMeshBuilder.BuildParts(C, d);
                foreach (var p in parts)
                    if (p.Part == PartRef.Fairing)
                    {
                        Assert.That(p.Y1 - p.Y0, Is.EqualTo((float)d.fairingLength).Within(1e-3f), row.Key);
                        Assert.That(p.Mesh.bounds.max.y, Is.EqualTo(p.Y1).Within(2e-3f), row.Key + ": the tip moved");
                    }
            }
        }
    }
}
