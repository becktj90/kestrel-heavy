// Physics' spec (VEHICLE_ENGINEERING_RULES.md "Fairing joint and flare", DECISIONS #71): the fairing's joint is as wide as the stage under it,
// the barrel keeps fairing.diameter, and a conical shoulder of length min(dR / tan 15 deg, 0.5 x cylinder) joins them (Trevor: "like Falcon 9 or
// Atlas V", no step, no ledge). Engine-free: the same FairingShoulder code builds the Unity mesh.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class FairingShoulderTests
    {
        public sealed class Row
        {
            public string Name, Nose;
            public float StageD, FairingD, Length;
            public override string ToString() => Name + " stage " + StageD + " fairing " + FairingD + " L " + Length;
        }

        /// <summary>Why a fairing profile is wrong; empty when it is right. The same checks run on every real design and on the old profile (which must fail).</summary>
        public static List<string> Problems(IList<FairingShoulder.Station> p, float rJoint, float rFairing, float length, string nose)
        {
            var bad = new List<string>();
            if (p.Count < 4) { bad.Add("too few stations"); return bad; }
            if (Math.Abs(p[0].Y) > 1e-5f || p[0].R != 0f) bad.Add("profile does not start on the axis at the joint");
            if (Math.Abs(p[1].Y) > 1e-5f || Math.Abs(p[1].R - rJoint) > 1e-4f) bad.Add("joint diameter " + 2 * p[1].R + " != stage diameter " + 2 * rJoint);
            float widest = 0f, barrelR = 0f, barrelY = length * (1f - FairingShoulder.NoseFraction(nose));
            for (int i = 0; i < p.Count; i++)
            {
                widest = Math.Max(widest, p[i].R);
                if (Math.Abs(p[i].Y - barrelY) < 1e-4f) barrelR = p[i].R;
                if (i > 1 && p[i].Y < p[i - 1].Y - 1e-5f) bad.Add("profile goes down at station " + i);
            }
            if (Math.Abs(barrelR - rFairing) > 1e-4f) bad.Add("barrel diameter " + 2 * barrelR + " != fairing diameter " + 2 * rFairing);
            if (widest > Math.Max(rJoint, rFairing) + 1e-4f) bad.Add("wider than both stage and fairing: " + 2 * widest);
            // No ledge: from the joint up to the barrel, every change in radius is spread over a rising, gently sloped span.
            for (int i = 2; i < p.Count && p[i - 1].Y < barrelY - 1e-4f; i++)
            {
                float dR = p[i].R - p[i - 1].R, dY = p[i].Y - p[i - 1].Y;
                if (Math.Abs(dR) < 1e-6f) continue;
                if (dY < 1e-4f) { bad.Add("ledge: radius steps " + dR + " m over " + dY + " m at height " + p[i - 1].Y); continue; }
                if (Math.Abs(dR / dY) > Math.Tan(30.0 * Math.PI / 180.0) + 1e-4) bad.Add("shoulder steeper than 30 deg (slope " + Math.Abs(dR / dY) + ") at height " + p[i - 1].Y);
            }
            // Between the joint and the barrel the radius moves one way only (no bulge or neck).
            float dir = Math.Sign(rFairing - rJoint);
            for (int i = 2; i < p.Count && p[i].Y <= barrelY + 1e-4f; i++)
                if ((p[i].R - p[i - 1].R) * dir < -1e-5f) bad.Add("radius reverses on the shoulder at station " + i);
            return bad;
        }

        /// <summary>The geometry before this change: the base was the full fairing radius, straight up to the nose.</summary>
        static List<FairingShoulder.Station> OldProfile(float rf, float length, string nose)
        {
            float cyl = length * (1f - FairingShoulder.NoseFraction(nose));
            var p = new List<FairingShoulder.Station> { new FairingShoulder.Station(0, 0), new FairingShoulder.Station(rf, 0), new FairingShoulder.Station(rf, cyl) };
            for (int k = 1; k <= 10; k++)
                p.Add(new FairingShoulder.Station(k == 10 ? 0f : FairingShoulder.NoseRadius(nose, rf, k / 10f), cyl + length * FairingShoulder.NoseFraction(nose) * k / 10f));
            return p;
        }

        [Test]
        public void SkinnyRocket_BaseIsTheStageAndFlaresSmoothly()
        {
            var p = FairingShoulder.Profile(0.4f, 0.6f, 2.2f, "ogive");   // campaign m1: 0.8 m stage, 1.2 m fairing
            Assert.That(Problems(p, 0.4f, 0.6f, 2.2f, "ogive"), Is.Empty);
            Assert.That(p[1].R, Is.EqualTo(0.4f).Within(1e-6f));
            Assert.That(FairingShoulder.RadiusAt(0.4f, 0.6f, 2.2f, "ogive", 0.01f), Is.EqualTo(0.4f).Within(0.005f), "still stage-wide just above the joint");
            Assert.That(FairingShoulder.RadiusAt(0.4f, 0.6f, 2.2f, "ogive", 0.9f), Is.EqualTo(0.6f).Within(1e-5f), "full fairing width above the shoulder");
        }

        [Test]
        public void TheOldProfileIsRejected_SoTheseTestsFailOnTheOldGeometry()
        {
            var old = OldProfile(0.6f, 2.2f, "ogive");
            var why = Problems(old, 0.4f, 0.6f, 2.2f, "ogive");
            Assert.That(why, Is.Not.Empty);
            Assert.That(why[0], Does.Contain("joint diameter"), string.Join("; ", why));
            // It is a real ledge on the stage top: a 0.2 m radius jump with nothing to ease it.
            Assert.That(old[1].R - 0.4f, Is.GreaterThan(0.19f));
        }

        [Test]
        public void FlushFairing_IsUnchangedFromTheOldProfile()
        {
            var now = FairingShoulder.Profile(1.85f, 1.85f, 8f, "ogive");
            var old = OldProfile(1.85f, 8f, "ogive");
            Assert.That(now.Count, Is.EqualTo(old.Count));
            for (int i = 0; i < now.Count; i++)
            {
                Assert.That(now[i].R, Is.EqualTo(old[i].R).Within(1e-5f));
                Assert.That(now[i].Y, Is.EqualTo(old[i].Y).Within(1e-5f));
            }
        }

        [TestCase("ogive"), TestCase("conic"), TestCase("blunt"), TestCase("von-karman")]
        public void EveryNose_FlaresWithoutALedge(string nose)
        {
            foreach (var (rj, rf, len) in new[] { (0.4f, 0.6f, 2.2f), (1.8f, 2.6f, 13f), (1.85f, 1.9f, 8.6f), (2.05f, 1.8f, 3f), (0.75f, 1.4f, 6f), (0.3f, 0.31f, 1f) })
                Assert.That(Problems(FairingShoulder.Profile(rj, rf, len, nose), rj, rf, len, nose), Is.Empty, nose + " " + rj + "->" + rf + " L" + len);
        }

        [Test]
        public void StockPreset_ShoulderIsExactly15Degrees()
        {
            // 5.2 m fairing on a 3.7 m stage (ratio 1.405): dR 0.75 m, cylinder 13 x 0.55 = 7.15 m, so L_sh = 0.75 / tan 15 = 2.80 m < 3.575 m.
            float hs = FairingShoulder.Height(1.85f, 2.6f, 13f, "ogive");
            Assert.That(hs, Is.EqualTo(0.75f / (float)Math.Tan(15.0 * Math.PI / 180.0)).Within(1e-4f));
            Assert.That(FairingShoulder.HalfAngleDeg(1.85f, 2.6f, 13f, 0.45f), Is.EqualTo(15f).Within(0.01f));
            // The skinny m1 rocket hits the 0.5 x cylinder limit: L_sh = 0.605 m, steeper than 15 degrees but under 30.
            Assert.That(FairingShoulder.Height(0.4f, 0.6f, 2.2f, "ogive"), Is.EqualTo(0.5f * 2.2f * 0.55f).Within(1e-5f));
            Assert.That(FairingShoulder.HalfAngleDeg(0.4f, 0.6f, 2.2f, 0.45f), Is.InRange(15f, 30f));
        }

        [Test]
        public void ShoulderIsCarvedFromTheCylinder_SoLengthAndNoseAreUnchanged()
        {
            foreach (var nose in new[] { "ogive", "conic", "blunt", "von-karman" })
            {
                var flare = FairingShoulder.Profile(1.85f, 2.6f, 13f, nose);
                var flush = FairingShoulder.Profile(2.6f, 2.6f, 13f, nose);
                Assert.That(flare[flare.Count - 1].Y, Is.EqualTo(13f).Within(1e-5f));
                // The nose stations (everything from the end of the barrel) are identical, so CG and nose aero do not move.
                int n = 11;
                for (int k = 1; k <= n; k++)
                {
                    Assert.That(flare[flare.Count - k].Y, Is.EqualTo(flush[flush.Count - k].Y).Within(1e-5f), nose);
                    Assert.That(flare[flare.Count - k].R, Is.EqualTo(flush[flush.Count - k].R).Within(1e-5f), nose);
                }
            }
        }

        [Test]
        public void TotalHeightIsUnchanged()
        {
            var p = FairingShoulder.Profile(0.4f, 0.6f, 2.2f, "ogive");
            Assert.That(p[p.Count - 1].Y, Is.EqualTo(2.2f).Within(1e-5f));
            Assert.That(p[p.Count - 1].R, Is.EqualTo(0f));
        }

        [Test]
        public void Layout_RingsFollowTheShoulder()
        {
            var poses = FairingLayout.Build(10f, 0.6f, 2.2f, 0.45f, false, 0.4f);
            Assert.That(poses.Count, Is.GreaterThan(0));
            foreach (var p in poses)
            {
                if (p.Kind != FairingLayout.Ring) continue;
                float h = p.Y - 10f;
                float body = FairingShoulder.RadiusAt(0.4f, 0.6f, 2.2f, "ogive", Math.Max(0f, h));
                if (h < 2.2f * 0.55f) Assert.That(p.Radius, Is.EqualTo(body + 0.03f).Within(0.02f), "ring at " + h + " floats off the shoulder");
            }
            // The base ring is the stage width, not the fairing width.
            foreach (var p in poses) if (p.Kind == FairingLayout.Ring && p.Y - 10f < 0.1f) Assert.That(p.Radius, Is.LessThan(0.5f));
        }

        // ---- Every real starter, template and campaign design ----------------------------------------------------------------

        static string RepoRoot()
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 12 && dir != null; i++, dir = Path.GetDirectoryName(dir))
                if (File.Exists(Path.Combine(dir, "data", "designer-templates.json"))) return dir;
            dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 12 && dir != null; i++, dir = Path.GetDirectoryName(dir))
                if (File.Exists(Path.Combine(dir, "data", "designer-templates.json"))) return dir;
            throw new FileNotFoundException("repo root with data/designer-templates.json not found");
        }

        static void Add(List<Row> rows, string name, object design)
        {
            if (!(design is JsonObject d)) return;
            var stages = d.Arr("stages");
            if (stages == null || stages.Count == 0) return;
            var f = d.Obj("fairing");
            if (f == null) return;
            rows.Add(new Row
            {
                Name = name,
                StageD = (float)((JsonObject)stages[stages.Count - 1]).Num("diameter"),
                FairingD = (float)f.Num("diameter"),
                Length = (float)f.Num("length"),
                Nose = d.Obj("aero")?.Str("nose") ?? "ogive",
            });
        }

        public static List<Row> RealDesigns()
        {
            string root = RepoRoot();
            var rows = new List<Row>();
            foreach (JsonObject t in MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "designer-templates.json"))).Arr("templates"))
                Add(rows, "template:" + t.Str("id"), t.Obj("design"));
            foreach (JsonObject m in MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "board-starters.json"))).Arr("missions"))
            {
                var ds = m.Arr("designs"); if (ds == null) continue;
                for (int i = 0; i < ds.Count; i++) Add(rows, "board:" + m.Str("id") + "#" + i, ds[i]);
            }
            foreach (JsonObject m in MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json"))).Arr("missions"))
            {
                var ds = m.Arr("designs"); if (ds == null) continue;
                for (int i = 0; i < ds.Count; i++) Add(rows, "winner:" + m.Str("id") + "#" + i, ds[i]);
            }
            foreach (var p in MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "presets.json"))).Arr("presets"))
                if (p is JsonObject po) Add(rows, "preset:" + po.Str("id"), po);
            return rows;
        }

        [Test]
        public void EveryRealDesign_JointEqualsTheStage_MaxIsTheFairing_NoLedge()
        {
            var rows = RealDesigns();
            Assert.That(rows.Count, Is.GreaterThanOrEqualTo(40), "starters, templates, campaign winners and presets were not all found");
            int wider = 0; var failures = new List<string>();
            foreach (var r in rows)
            {
                float rj = r.StageD / 2, rf = r.FairingD / 2;
                if (rf > rj + 1e-3f) wider++;
                var p = FairingShoulder.Profile(rj, rf, r.Length, r.Nose);
                var why = Problems(p, rj, rf, r.Length, r.Nose);
                if (why.Count > 0) failures.Add(r + ": " + string.Join("; ", why));
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            Assert.That(wider, Is.GreaterThanOrEqualTo(8), "the data must contain fairings wider than their stage, or this test proves nothing");
        }

        [Test]
        public void EveryRealDesign_ShoulderIs15Degrees_UnlessTheHalfCylinderLimitBites_AndNeverAbove30()
        {
            var failures = new List<string>(); int fifteen = 0, capped = 0, narrower = 0;
            foreach (var r in RealDesigns())
            {
                float rj = r.StageD / 2, rf = r.FairingD / 2, nf = FairingShoulder.NoseFraction(r.Nose);
                float dr = Math.Abs(rf - rj), hs = FairingShoulder.Height(rj, rf, r.Length, nf), cyl = r.Length * (1f - nf);
                if (dr <= 1e-3f) { if (hs != 0f) failures.Add(r + ": flush design has a shoulder"); continue; }
                if (rf < rj) narrower++;
                float want = Math.Min(dr / (float)Math.Tan(15.0 * Math.PI / 180.0), 0.5f * cyl);
                if (Math.Abs(hs - want) > 1e-4f) failures.Add(r + ": shoulder " + hs + " != min(dR/tan15, 0.5 cyl) " + want);
                float ang = FairingShoulder.HalfAngleDeg(rj, rf, r.Length, nf);
                if (hs < 0.5f * cyl - 1e-4f) { fifteen++; if (Math.Abs(ang - 15f) > 0.01f) failures.Add(r + ": half-angle " + ang + " != 15"); }
                else { capped++; if (ang < 15f - 0.01f) failures.Add(r + ": capped shoulder is shallower than 15 deg: " + ang); }
                if (ang > 30f) failures.Add(r + ": half-angle " + ang + " > 30 deg (that is a ledge, not a cone)");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            Assert.That(fifteen, Is.GreaterThanOrEqualTo(3), "some real designs should sit at exactly 15 degrees");
            Assert.That(narrower, Is.GreaterThanOrEqualTo(2), "m20 (fairing narrower than the stage) is in the data");
        }

        [Test]
        public void EveryRealDesign_TheOldProfileHadALedge_WhereTheFairingIsWider()
        {
            int ledges = 0;
            foreach (var r in RealDesigns())
            {
                float rj = r.StageD / 2, rf = r.FairingD / 2;
                bool differs = Math.Abs(rf - rj) > 1e-3f;
                var why = Problems(OldProfile(rf, r.Length, r.Nose), rj, rf, r.Length, r.Nose);
                if (differs) { Assert.That(why, Is.Not.Empty, r + " old profile should fail"); ledges++; }
                else Assert.That(why, Is.Empty, r + " flush design is unchanged: " + string.Join("; ", why));
            }
            Assert.That(ledges, Is.GreaterThanOrEqualTo(8));
        }
    }
}
