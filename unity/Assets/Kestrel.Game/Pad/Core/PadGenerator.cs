// Seeded pad generator (U6 G2/G3 spec, built as D1 per PAD_VEHICLE_DETAIL_AUDIT §10 Q1/Q3).
// One generator, three archetypes; same (seed, archetype, vehicle fit) => identical pad, member for member.
// Output is engine-free (members + element graph + swing-arm specs); Pad/Runtime instances it.
// Generic fictional layouts from text knowledge only (see docs/REFERENCE_LEDGER.md); no real facility.
using System;
using System.Collections.Generic;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    /// <summary>Rail = small solids (no tower, lanyard umbilical). Tower = service tower + swing arms.
    /// TowerStrongback = heavy pad: tower + arms + a hinged strongback lattice on the far side.</summary>
    public enum PadArchetype { Rail, Tower, TowerStrongback }

    public struct PadFit
    {
        public float VehicleHeight, VehicleRadius;
        public float Clearance;              // tower face to vehicle skin, m
        /// <summary>Optional skin profile: distance (m) from the vehicle axis to its skin along pad +Z (toward the
        /// tower) at height y. Null = VehicleRadius everywhere. Lets arms reach a core that is narrower than the
        /// overall half-width (side boosters sit off the tower axis). Not part of Hash (geometry follows it).</summary>
        public Func<float, float> SkinRadiusAt;
        public PadFit(float h, float r, float clearance = 6f) { VehicleHeight = h; VehicleRadius = r; Clearance = clearance; SkinRadiusAt = null; }
        public float SkinAt(float y) { if (SkinRadiusAt == null) return VehicleRadius; float s = SkinRadiusAt(y); return float.IsNaN(s) || s < 0 ? VehicleRadius : Math.Min(s, VehicleRadius); }
        /// <summary>Gap left between a fixed access arm's tip and the skin.</summary>
        public const float ArmSkinGap = 0.6f;
    }

    /// <summary>Deterministic xorshift32; no System.Random (its sequence is not guaranteed across runtimes).</summary>
    public struct PadRng
    {
        uint _s;
        public PadRng(int seed) { _s = (uint)seed * 2654435761u ^ 0x9E3779B9u; if (_s == 0) _s = 1; Next(); }
        public uint Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return _s; }
        public float Range(float a, float b) => a + (b - a) * (Next() / 4294967296f);
        public int Range(int a, int bExclusive) => a + (int)(Next() % (uint)Math.Max(1, bExclusive - a));
    }

    public sealed class PadLayout
    {
        public int Seed; public PadArchetype Archetype; public PadFit Fit;
        public TowerParams Tower;            // null for Rail
        public readonly List<Member> Members = new List<Member>();
        public readonly List<SwingArmSpec> Arms = new List<SwingArmSpec>();
        public ElementGraph Graph = new ElementGraph();
        public float DeckRadius;
        /// <summary>Named tower attach frames (null for Rail). See TowerAnchors and docs/UMBILICAL_CONTRACT.md.</summary>
        public TowerAnchors Anchors;

        /// <summary>Stable fingerprint for golden tests and the generation cache key.</summary>
        public ulong Hash()
        {
            ulong h = 1469598103934665603UL;
            void F(float v) { h ^= (ulong)BitConverter.SingleToInt32Bits(v) & 0xffffffffUL; h *= 1099511628211UL; }
            void S(string s) { foreach (var c in s) { h ^= c; h *= 1099511628211UL; } }
            S(Archetype.ToString()); F(DeckRadius);
            foreach (var m in Members) { S(m.Id); F(m.A.X); F(m.A.Y); F(m.A.Z); F(m.B.X); F(m.B.Y); F(m.B.Z); }
            foreach (var a in Arms) { S(a.AttachId); F(a.Pivot.X); F(a.Pivot.Y); F(a.Pivot.Z); }
            return h;
        }
    }

    public static class PadGenerator
    {
        public const int Version = 2;   // bump when output for a given seed changes (invalidates caches/goldens)

        /// <summary>Same rule as Kestrel.Core.Timeline.Pad.For: tower for stages >= 3 m across, strongback for liquid + tower.</summary>
        public static PadArchetype ArchetypeFor(string corePadId, bool strongback) =>
            corePadId == "pad.rail" ? PadArchetype.Rail : strongback ? PadArchetype.TowerStrongback : PadArchetype.Tower;

        /// <param name="attach">Vehicle attach points (vehicle frame). Swing arms are made for SwingArmRetract points; null = generic defaults.</param>
        public static PadLayout Generate(int seed, PadArchetype type, PadFit fit, IReadOnlyList<AttachPoint> attach = null)
        {
            var rng = new PadRng(seed);
            var L = new PadLayout { Seed = seed, Archetype = type, Fit = fit };
            L.DeckRadius = Math.Max(12f, fit.VehicleRadius * 4f) * rng.Range(1.0f, 1.3f);
            var site = L.Graph.Add(TowerLayout.SiteId, ElementCategory.Site, null, "pad", $"archetype:{type}");
            site.Center = new Vec3(0, 0, 0); site.Size = new Vec3(L.DeckRadius * 2, 1, L.DeckRadius * 2); site.DisplayName = "Launch pad";

            if (type == PadArchetype.Rail) { BuildRail(L, ref rng); return L; }

            var tp = new TowerParams
            {
                Width = (float)Math.Round(rng.Range(8f, 12f) * 2) / 2,
                LevelHeight = (float)Math.Round(rng.Range(5.5f, 6.5f) * 2) / 2,
                CapHeight = rng.Range(3f, 6f), Seed = seed,
            };
            if (type == PadArchetype.TowerStrongback) tp.Width += 2f;
            tp.Levels = Math.Max(6, (int)Math.Ceiling(fit.VehicleHeight * 1.05f / tp.LevelHeight));
            tp.StandOff = fit.VehicleRadius + fit.Clearance + tp.Width * 0.5f;
            // D2: crew-access arm one level below the vehicle tip, spike on the cap.
            tp.CrewLevel = Math.Max(0, Math.Min(tp.Levels - 1, (int)Math.Floor(fit.VehicleHeight * 0.85f / tp.LevelHeight) - 1));
            float crewY = (tp.CrewLevel + 1) * tp.LevelHeight;
            tp.CrewArmReach = Math.Max(1f, tp.NearFaceZ - (fit.SkinAt(crewY) + PadFit.ArmSkinGap));
            tp.CrewArmX = 0f;
            tp.SpikeHeight = tp.CapHeight * 1.5f;
            L.Tower = tp;
            var tower = new TowerLayout(tp, L.Graph);
            L.Members.AddRange(tower.Members);
            L.Anchors = tower.Anchors;

            var pts = attach ?? DefaultAttachPoints(fit, type);
            foreach (var p in pts)
            {
                if (p.Release != ReleaseStyle.SwingArmRetract) continue;
                var spec = new SwingArmSpec
                {
                    AttachId = p.Id, Type = p.Type,
                    Pivot = SwingArmSolver.PivotFor(tp, p.Pose.Position),
                    RetractYawDeg = rng.Range(80f, 110f) * (rng.Next() % 2 == 0 ? 1 : -1),
                    MaxLength = fit.Clearance + fit.VehicleRadius + 4f + (fit.VehicleRadius - fit.SkinAt(p.Pose.Position.Y)),
                };
                L.Arms.Add(spec);
                L.Anchors.Add(TowerAnchors.ArmId(p.Id), spec.Pivot, "s", L.Anchors.LevelNearest(spec.Pivot.Y));
                var e = L.Graph.Add($"tower.arm.{p.Id}", ElementCategory.SwingArm, LevelIdAt(tp, p.Pose.Position.Y), "steel", "swing-arm", $"umbilical:{UmbilicalIds.Short(p.Type)}", $"attach:{p.Id}");
                float len = spec.Pivot.Z - p.Pose.Position.Z;
                e.Center = new Vec3(spec.Pivot.X, spec.Pivot.Y, spec.Pivot.Z - len * 0.5f); e.Size = new Vec3(spec.Width, spec.Depth, len);
                e.DisplayName = $"Swing arm ({UmbilicalIds.Short(p.Type)})";
            }
            if (type == PadArchetype.TowerStrongback) BuildStrongback(L, ref rng);
            BuildMasts(L, ref rng);
            return L;
        }

        /// <summary>PAD_DETAIL_GRAMMAR §1: free-standing tapered square lattice lightning masts, clear of the
        /// tower (+Z) side, symmetric about the vehicle axis with ±10° jitter. Generic proportions only.</summary>
        /// <summary>Pad-local bearings (deg, 0 = tower +Z, clockwise toward +X) for lightning masts: flanking the tower
        /// and behind it, clear of the plume exits (90/270 ±25°, §6) and of the default design camera, which looks from
        /// bearing ≈ 180–240 (see DefaultViewBearingDeg). Order fills the symmetric pair first.</summary>
        public static readonly float[] MastSlots = { 40f, 320f, 130f, 0f };
        public const float MastJitterDeg = 8f;
        /// <summary>Pad-local bearing of the Slice design camera (PadTower yaw + DesignFraming rig), for tests and placement.</summary>
        public const float DefaultViewBearingDeg = 208f;

        static void BuildMasts(PadLayout L, ref PadRng rng)
        {
            int n = L.Archetype == PadArchetype.TowerStrongback ? rng.Range(3, 5) : L.Archetype == PadArchetype.Tower ? rng.Range(2, 4) : rng.Range(0, 3);
            float hv = L.Fit.VehicleHeight, hm = hv * rng.Range(1.15f, 1.6f), spike = hm * rng.Range(0.06f, 0.12f), hl = hm - spike;
            float wb = hm / rng.Range(9f, 14f), wt = wb * rng.Range(0.35f, 0.6f);
            float r = Math.Max(hv * 0.5f * rng.Range(1.0f, 1.8f), L.DeckRadius + wb);
            if (L.Tower != null) r = Math.Max(r, L.Tower.StandOff + L.Tower.Width * 0.75f + wb);   // slot 0 stands clear behind the tower
            for (int i = 0; i < n; i++)
            {
                // spread over the back/side arc away from the tower; tower sits at bearing 0 (+Z)
                float bearing = MastSlots[i % MastSlots.Length] + rng.Range(-MastJitterDeg, MastJitterDeg);
                double br = bearing * Math.PI / 180.0;
                float cx = r * (float)Math.Sin(br), cz = r * (float)Math.Cos(br);
                string id = $"mast.{i}";
                var e = L.Graph.Add(id, ElementCategory.Structure, TowerLayout.SiteId, "steel", "lightning-mast", "lattice");
                e.Center = new Vec3(cx, hm * 0.5f, cz); e.Size = new Vec3(wb, hm, wb); e.DisplayName = $"Lightning mast {i + 1}";
                float mean = (wb + wt) * 0.5f * 1.1f; int bays = Math.Max(10, Math.Min(22, (int)Math.Round(hl / mean)));
                Vec3 C(int c, float y) { float w = (wb + (wt - wb) * (y / hl)) * 0.5f; return new Vec3(cx + ((c == 0 || c == 3) ? -w : w), y, cz + ((c == 0 || c == 1) ? -w : w)); }
                void M(string mid, MemberKind k, Vec3 a, Vec3 b) => L.Members.Add(new Member { Id = mid, Kind = k, A = a, B = b, ParentId = id, Material = MaterialRole.Galvanized });
                for (int b = 0; b < bays; b++)
                {
                    float y0 = hl * b / bays, y1 = hl * (b + 1) / bays;
                    for (int c = 0; c < 4; c++)
                    {
                        int d = (c + 1) % 4;
                        M($"{id}.b{b:00}.leg{c}", MemberKind.Mast, C(c, y0), C(c, y1));
                        if (b % 2 == 1) M($"{id}.b{b:00}.h{c}", MemberKind.Brace, C(c, y1), C(d, y1));
                        M($"{id}.b{b:00}.x{c}.0", MemberKind.Brace, C(c, y0), C(d, y1));
                        M($"{id}.b{b:00}.x{c}.1", MemberKind.Brace, C(d, y0), C(c, y1));
                    }
                    if (b > 0 && b % 5 == 0) { M($"{id}.b{b:00}.plat.0", MemberKind.Deck, C(0, y0), C(2, y0)); }
                }
                M($"{id}.spike", MemberKind.Mast, new Vec3(cx, hl, cz), new Vec3(cx, hm, cz));
                L.Members[L.Members.Count - 1] = new Member { Id = $"{id}.spike", Kind = MemberKind.Mast, A = new Vec3(cx, hl, cz), B = new Vec3(cx, hm, cz), ParentId = id, Material = MaterialRole.Insulation };
            }
        }

        static string LevelIdAt(TowerParams tp, float y) => $"tower.l{Math.Max(0, Math.Min(tp.Levels - 1, (int)(y / tp.LevelHeight))):00}";

        /// <summary>Generic attach points (vehicle frame, +Z toward the tower) used until the vehicle publishes its own.</summary>
        public static List<AttachPoint> DefaultAttachPoints(PadFit fit, PadArchetype type)
        {
            var r = new List<AttachPoint>();
            if (type == PadArchetype.Rail) return r;
            void A(int stage, UmbilicalType t, float frac) => r.Add(new AttachPoint
            {
                Id = UmbilicalIds.Make(stage, t, 0), Type = t, Stage = stage, PlateRadius = 0.4f, Release = ReleaseStyle.SwingArmRetract,
                Pose = new LocalPose(new Vec3(0, fit.VehicleHeight * frac, fit.SkinAt(fit.VehicleHeight * frac)), Quat.Identity),
            });
            A(0, UmbilicalType.Oxidizer, 0.30f); A(1, UmbilicalType.Fuel, 0.70f); A(1, UmbilicalType.PowerData, 0.85f);
            return r;
        }

        static void BuildRail(PadLayout L, ref PadRng rng)
        {
            // Inclined launch rail on a short A-frame, offset on +Z (same side a tower would be).
            float len = Math.Max(8f, L.Fit.VehicleHeight * rng.Range(1.1f, 1.3f));
            float tilt = rng.Range(2f, 6f) * (float)Math.PI / 180f;
            float z0 = L.Fit.VehicleRadius + 0.6f;
            var rail = L.Graph.Add("rail", ElementCategory.Structure, TowerLayout.SiteId, "steel", "launch-rail");
            rail.Center = new Vec3(0, len * 0.5f, z0); rail.Size = new Vec3(2, len, 2); rail.DisplayName = "Launch rail";
            var top = new Vec3(0, len * (float)Math.Cos(tilt), z0 - len * (float)Math.Sin(tilt));
            AddM(L, "rail", "rail.beam", MemberKind.Column, new Vec3(0, 0, z0), top, MaterialRole.PaintedSteel, "rail");
            int legs = rng.Range(2, 4);
            for (int i = 0; i < legs; i++)
            {
                float y = len * (i + 1) / (legs + 1);
                AddM(L, "rail", $"rail.leg.{i}", MemberKind.Brace, new Vec3(0, 0, z0 + y * 0.6f), new Vec3(0, y, z0 - y * (float)Math.Tan(tilt)), MaterialRole.SafetyYellowSteel, "brace");
            }
        }

        static void BuildStrongback(PadLayout L, ref PadRng rng)
        {
            // Planar lattice on the far side (-Z), hinged at the deck; the vehicle sits between it and the tower.
            float h = L.Fit.VehicleHeight * rng.Range(0.85f, 0.95f), w = Math.Max(3f, L.Fit.VehicleRadius * 1.6f);
            float z = -(L.Fit.VehicleRadius + rng.Range(1.0f, 2.0f)); int bays = Math.Max(4, (int)(h / 5f));
            var sb = L.Graph.Add("strongback", ElementCategory.Structure, TowerLayout.SiteId, "steel", "strongback", "hinged");
            sb.Center = new Vec3(0, h * 0.5f, z); sb.Size = new Vec3(w, h, 1.5f); sb.DisplayName = "Strongback";
            float x = w * 0.5f;
            for (int i = 0; i < bays; i++)
            {
                float y0 = h * i / bays, y1 = h * (i + 1) / bays;
                AddM(L, "strongback", $"strongback.b{i:00}.col.l", MemberKind.Column, new Vec3(-x, y0, z), new Vec3(-x, y1, z), MaterialRole.PaintedSteel, "column");
                AddM(L, "strongback", $"strongback.b{i:00}.col.r", MemberKind.Column, new Vec3(x, y0, z), new Vec3(x, y1, z), MaterialRole.PaintedSteel, "column");
                AddM(L, "strongback", $"strongback.b{i:00}.girt", MemberKind.Girt, new Vec3(-x, y1, z), new Vec3(x, y1, z), MaterialRole.PaintedSteel, "girt");
                AddM(L, "strongback", $"strongback.b{i:00}.br", MemberKind.Brace, i % 2 == 0 ? new Vec3(-x, y0, z) : new Vec3(x, y0, z), i % 2 == 0 ? new Vec3(x, y1, z) : new Vec3(-x, y1, z), MaterialRole.PaintedSteel, "brace");
            }
        }

        static void AddM(PadLayout L, string parent, string id, MemberKind k, Vec3 a, Vec3 b, MaterialRole mat, params string[] tags)
        {
            L.Members.Add(new Member { Id = id, Kind = k, A = a, B = b, ParentId = parent, Material = mat });
            var t = new string[tags.Length + 2]; tags.CopyTo(t, 0); t[tags.Length] = "steel"; t[tags.Length + 1] = "member";
            var e = L.Graph.Add(id, ElementCategory.Member, parent, t);
            e.Center = new Vec3((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);
            e.Size = new Vec3(Math.Abs(b.X - a.X) + 0.4f, Math.Abs(b.Y - a.Y) + 0.4f, Math.Abs(b.Z - a.Z) + 0.4f);
        }

        /// <summary>Vehicle-frame point -> pad frame (rotation then translation).</summary>
        public static Vec3 ToPad(LocalPose vehicleToPad, Vec3 p)
        {
            var q = vehicleToPad.Rotation; float x = q.X, y = q.Y, z = q.Z, w = q.W;
            float tx = 2 * (y * p.Z - z * p.Y), ty = 2 * (z * p.X - x * p.Z), tz = 2 * (x * p.Y - y * p.X);
            var r = new Vec3(p.X + w * tx + (y * tz - z * ty), p.Y + w * ty + (z * tx - x * tz), p.Z + w * tz + (x * ty - y * tx));
            return new Vec3(r.X + vehicleToPad.Position.X, r.Y + vehicleToPad.Position.Y, r.Z + vehicleToPad.Position.Z);
        }
    }
}
