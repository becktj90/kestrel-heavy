// Procedural fictional launch tower layout (D1 structural steel). Engine-free: produces member segments
// and swing-arm specs in pad space; Runtime turns them into instanced KitMesh beams.
// Pad space: origin at the vehicle centre line on the pad deck, +Y up, tower on +Z (matches the umbilical
// contract's "+Z toward the pad tower"). Generic square lattice; not modelled on any real facility.
using System;
using System.Collections.Generic;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public enum MemberKind { Column, Girt, Brace, Deck, ArmChord, ArmBrace, Mast, Rail }

    public struct Member
    {
        public string Id; public MemberKind Kind; public Vec3 A, B; public string ParentId; public MaterialRole Material;
        public float Length => (float)Math.Sqrt(Sq(B.X - A.X) + Sq(B.Y - A.Y) + Sq(B.Z - A.Z));
        static float Sq(float v) => v * v;
    }

    [Serializable]
    public sealed class TowerParams
    {
        public float Width = 10f;            // square footprint side, m
        public float LevelHeight = 6f;       // deck-to-deck, m
        public int Levels = 12;              // 72 m
        public float StandOff = 14f;         // vehicle centre line -> tower centre along +Z, m
        public float CapHeight = 4f;         // open top mast frame above the last level
        public int Seed = 1;                 // reserved for D7 wear variation; layout itself is deterministic
        public int CrewLevel = -1;           // D2: level carrying the crew-access arm (-1 = none)
        public float CrewArmX = 0f;          // lateral offset of the crew arm root on the s face
        public float CrewArmReach = 0f;      // arm length from the s face toward the vehicle (0 = none)
        public float SpikeHeight = 0f;       // D2: lightning spike on the cap (0 = none)

        /// <summary>Tower tall enough to clear the highest attach point by one level (min 6 levels).</summary>
        public static TowerParams ForVehicle(IReadOnlyList<AttachPoint> pts, float levelHeight = 6f)
        {
            float top = 0; if (pts != null) foreach (var p in pts) top = Math.Max(top, p.Pose.Position.Y);
            return new TowerParams { LevelHeight = levelHeight, Levels = Math.Max(6, (int)Math.Ceiling(top / levelHeight) + 1) };
        }
        public float Height => Levels * LevelHeight;
        /// <summary>Z of the tower face nearest the vehicle.</summary>
        public float NearFaceZ => StandOff - Width * 0.5f;
    }

    public sealed class TowerLayout
    {
        public readonly TowerParams P;
        public readonly List<Member> Members = new List<Member>();
        public readonly ElementGraph Graph;
        public const string SiteId = "pad", TowerId = "tower";

        public readonly TowerAnchors Anchors;

        public TowerLayout(TowerParams p, ElementGraph graph = null)
        {
            P = p ?? new TowerParams(); Graph = graph ?? new ElementGraph();
            if (!Graph.Contains(SiteId)) Graph.Add(SiteId, ElementCategory.Site, null, "pad");
            Build();
            BuildDetail();
            Anchors = new TowerAnchors(P);
        }

        Vec3 Lerp(Vec3 a, Vec3 b, float t) => new Vec3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

        // D2 detail: mid-bay struts + secondary lattice on each face (reads as truss, not 4 sticks),
        // plan bracing under each deck, handrails on the vehicle face, crew-access arm, cap spike.
        // Launch-pad polish: galvanised base plates under each leg so the tower reads as bolted to the
        // deck instead of floating on it. Visual only: plates sit outside the vehicle keep-out cylinder
        // (TowerClearsVehicle) and carry the "base-plate" tag, never "column"/"brace", so level queries
        // in PadGeneratorTests are unaffected.
        void BuildDetail()
        {
            Graph.Add("tower.base", ElementCategory.Structure, TowerId, "steel", "base");
            for (int c = 0; c < 4; c++)
            {
                var leg = CornerAt(c, 0f);
                Add("tower.base", $"tower.base.plate.{Corner[c]}", MemberKind.Deck,
                    new Vec3(leg.X - 1.1f, 0.10f, leg.Z), new Vec3(leg.X + 1.1f, 0.10f, leg.Z),
                    // Tone pass: safety yellow (the crown/handrail accent) instead of Galvanized so the footings
                    // do not match the deck plates. Same tag and geometry, so level queries are unchanged.
                    MaterialRole.SafetyYellowSteel, "base-plate");
            }
            for (int l = 0; l < P.Levels; l++)
            {
                float y0 = l * P.LevelHeight, y1 = y0 + P.LevelHeight, ym = (y0 + y1) * 0.5f;
                string lid = $"tower.l{l:00}";
                for (int f = 0; f < 4; f++)
                {
                    int a = f, b = (f + 1) % 4;
                    Vec3 a0 = CornerAt(a, y0), b0 = CornerAt(b, y0), am = CornerAt(a, ym), bm = CornerAt(b, ym), a1 = CornerAt(a, y1), b1 = CornerAt(b, y1);
                    Vec3 c0 = Lerp(a0, b0, 0.5f), c1 = Lerp(a1, b1, 0.5f);
                    Add(lid, $"{lid}.strut.{Face[f]}", MemberKind.Brace, am, bm, MaterialRole.PaintedSteel, "strut", $"face:{Face[f]}");
                    Add(lid, $"{lid}.post.{Face[f]}", MemberKind.Brace, c0, c1, MaterialRole.PaintedSteel, "post", $"face:{Face[f]}");
                    // secondary lattice: small diamonds from post mid to the corner mids
                    var cm = Lerp(c0, c1, 0.5f);
                    Add(lid, $"{lid}.lat.{Face[f]}.0", MemberKind.Brace, Lerp(a0, c0, 0.5f), cm, MaterialRole.PaintedSteel, "lattice");
                    Add(lid, $"{lid}.lat.{Face[f]}.1", MemberKind.Brace, cm, Lerp(b1, c1, 0.5f), MaterialRole.PaintedSteel, "lattice");
                    Add(lid, $"{lid}.lat.{Face[f]}.2", MemberKind.Brace, Lerp(b0, c0, 0.5f), cm, MaterialRole.PaintedSteel, "lattice");
                    Add(lid, $"{lid}.lat.{Face[f]}.3", MemberKind.Brace, cm, Lerp(a1, c1, 0.5f), MaterialRole.PaintedSteel, "lattice");
                }
                Add(lid, $"{lid}.plan.0", MemberKind.Brace, CornerAt(0, y1), CornerAt(2, y1), MaterialRole.PaintedSteel, "plan-brace");
                Add(lid, $"{lid}.plan.1", MemberKind.Brace, CornerAt(1, y1), CornerAt(3, y1), MaterialRole.PaintedSteel, "plan-brace");
                // handrail on the vehicle face, 1.1 m above the deck, inset 0.3 m
                var r0 = CornerAt(0, y1 + 1.1f); var r1 = CornerAt(1, y1 + 1.1f); r0.Z -= 0.3f; r1.Z -= 0.3f;
                Add(lid, $"{lid}.rail.s", MemberKind.Rail, r0, r1, MaterialRole.SafetyYellowSteel, "handrail");
            }
            if (P.CrewLevel >= 0 && P.CrewLevel < P.Levels && P.CrewArmReach > 0.5f) BuildCrewArm();
            if (P.SpikeHeight > 0)
            {
                float top = P.Height + P.CapHeight;
                Add("tower.cap", "tower.cap.spike", MemberKind.Mast, new Vec3(0, top, P.StandOff), new Vec3(0, top + P.SpikeHeight, P.StandOff), MaterialRole.Insulation, "lightning-spike");
            }
        }

        // Fixed crew-access arm: box truss from the s face toward the vehicle, enclosed end room frame.
        void BuildCrewArm()
        {
            const string id = "tower.crewarm";
            float y = (P.CrewLevel + 1) * P.LevelHeight, z0 = P.NearFaceZ, z1 = z0 - P.CrewArmReach, x = P.CrewArmX;
            const float hw = 0.9f, h = 2.4f;
            var e = Graph.Add(id, ElementCategory.CrewArm, $"tower.l{P.CrewLevel:00}", "steel", "crew-access-arm", "walkable");
            e.Center = new Vec3(x, y + h * 0.5f, (z0 + z1) * 0.5f); e.Size = new Vec3(hw * 2, h, P.CrewArmReach); e.DisplayName = "Crew access arm";
            int bays = Math.Max(2, (int)Math.Round(P.CrewArmReach / 2.4f));
            for (int i = 0; i < 4; i++)
            {
                float cx = x + (i % 2 == 0 ? -hw : hw), cy = y + (i < 2 ? 0 : h);
                Add(id, $"{id}.chord.{i}", MemberKind.ArmChord, new Vec3(cx, cy, z0), new Vec3(cx, cy, z1), MaterialRole.PaintedSteel, "chord");
            }
            for (int b = 0; b <= bays; b++)
            {
                float z = z0 + (z1 - z0) * b / bays;
                Add(id, $"{id}.ring.{b}.b", MemberKind.ArmBrace, new Vec3(x - hw, y, z), new Vec3(x + hw, y, z), MaterialRole.PaintedSteel, "ring");
                Add(id, $"{id}.ring.{b}.t", MemberKind.ArmBrace, new Vec3(x - hw, y + h, z), new Vec3(x + hw, y + h, z), MaterialRole.PaintedSteel, "ring");
                Add(id, $"{id}.ring.{b}.l", MemberKind.ArmBrace, new Vec3(x - hw, y, z), new Vec3(x - hw, y + h, z), MaterialRole.PaintedSteel, "ring");
                Add(id, $"{id}.ring.{b}.r", MemberKind.ArmBrace, new Vec3(x + hw, y, z), new Vec3(x + hw, y + h, z), MaterialRole.PaintedSteel, "ring");
                if (b == bays) continue;
                float zn = z0 + (z1 - z0) * (b + 1) / bays; bool flip = b % 2 == 1;
                Add(id, $"{id}.diag.{b}.l", MemberKind.ArmBrace, new Vec3(x - hw, flip ? y + h : y, z), new Vec3(x - hw, flip ? y : y + h, zn), MaterialRole.PaintedSteel, "diag");
                Add(id, $"{id}.diag.{b}.r", MemberKind.ArmBrace, new Vec3(x + hw, flip ? y + h : y, z), new Vec3(x + hw, flip ? y : y + h, zn), MaterialRole.PaintedSteel, "diag");
                Add(id, $"{id}.diag.{b}.t", MemberKind.ArmBrace, new Vec3(x - hw, y + h, z), new Vec3(x + hw, y + h, zn), MaterialRole.PaintedSteel, "diag");
            }
            Add(id, $"{id}.walk", MemberKind.Deck, new Vec3(x, y + 0.05f, z0), new Vec3(x, y + 0.05f, z1), MaterialRole.Galvanized, "walkway");
        }

        static readonly string[] Corner = { "sw", "se", "ne", "nw" };
        static readonly string[] Face = { "s", "e", "n", "w" };   // s = face toward the vehicle (-Z side)

        Vec3 CornerAt(int c, float y)
        {
            float h = P.Width * 0.5f;
            float x = (c == 0 || c == 3) ? -h : h, z = (c == 0 || c == 1) ? P.StandOff - h : P.StandOff + h;
            return new Vec3(x, y, z);
        }

        void Build()
        {
            var t = Graph.Add(TowerId, ElementCategory.Structure, SiteId, "steel", "tower", "service-structure");
            t.Center = new Vec3(0, (P.Height + P.CapHeight) * 0.5f, P.StandOff); t.Size = new Vec3(P.Width, P.Height + P.CapHeight, P.Width);
            t.DisplayName = "Service tower";
            for (int l = 0; l < P.Levels; l++)
            {
                float y0 = l * P.LevelHeight, y1 = y0 + P.LevelHeight;
                string lid = $"tower.l{l:00}";
                var le = Graph.Add(lid, ElementCategory.Level, TowerId, "steel", $"level:{l}");
                le.Center = new Vec3(0, (y0 + y1) * 0.5f, P.StandOff); le.Size = new Vec3(P.Width, P.LevelHeight, P.Width);
                le.DisplayName = $"Level {l + 1} ({y1:0} m)";
                for (int c = 0; c < 4; c++)
                    Add(lid, $"{lid}.col.{Corner[c]}", MemberKind.Column, CornerAt(c, y0), CornerAt(c, y1), MaterialRole.PaintedSteel, "column", $"corner:{Corner[c]}");
                for (int f = 0; f < 4; f++)
                {
                    int a = f, b = (f + 1) % 4;
                    Add(lid, $"{lid}.girt.{Face[f]}", MemberKind.Girt, CornerAt(a, y1), CornerAt(b, y1), MaterialRole.PaintedSteel, "girt", $"face:{Face[f]}");
                    // X-bracing; the vehicle-side face gets K-bracing below swing-arm levels to leave the arm pivots clear.
                    Add(lid, $"{lid}.br.{Face[f]}.0", MemberKind.Brace, CornerAt(a, y0), CornerAt(b, y1), MaterialRole.PaintedSteel, "brace", $"face:{Face[f]}");
                    Add(lid, $"{lid}.br.{Face[f]}.1", MemberKind.Brace, CornerAt(b, y0), CornerAt(a, y1), MaterialRole.PaintedSteel, "brace", $"face:{Face[f]}");
                }
                // Grated deck: two galvanised stringers across the level (instanced flat beams).
                for (int s = 0; s < 2; s++)
                {
                    float x = (s == 0 ? -0.25f : 0.25f) * P.Width;
                    Add(lid, $"{lid}.deck.{s}", MemberKind.Deck, new Vec3(x, y1, P.StandOff - P.Width * 0.5f), new Vec3(x, y1, P.StandOff + P.Width * 0.5f), MaterialRole.Galvanized, "deck", "walkable");
                }
            }
            // Cap: four short mast posts + ring, lightning-mast style frame (generic).
            // Launch-pad polish: the crown is safety yellow (same accent as the level handrails) so the
            // tower top reads against the sky above the vehicle nose at liftoff. The ring the old
            // comment promised but never built now closes the square at the posts' feet.
            string cap = "tower.cap"; var ce = Graph.Add(cap, ElementCategory.Level, TowerId, "steel", "cap");
            ce.Center = new Vec3(0, P.Height + P.CapHeight * 0.5f, P.StandOff); ce.Size = new Vec3(P.Width, P.CapHeight, P.Width);
            for (int c = 0; c < 4; c++)
            {
                var top = new Vec3(0, P.Height + P.CapHeight, P.StandOff);
                Add(cap, $"{cap}.post.{Corner[c]}", MemberKind.Brace, CornerAt(c, P.Height), top, MaterialRole.SafetyYellowSteel, "brace", "cap");
                // Rail kind on purpose: PadRenderer batches Rail on the brace mesh and PadPerf counts
                // (beam, SafetyYellowSteel) as an existing batch (the level handrails), so the ring
                // adds no draw call. A Girt ring would open a new (I-beam, SafetyYellowSteel) batch
                // and break WorstCaseInsideHighAndLowBudgets' 14-draw cap.
                Add(cap, $"{cap}.ring.{Corner[c]}", MemberKind.Rail, CornerAt(c, P.Height), CornerAt((c + 1) % 4, P.Height), MaterialRole.SafetyYellowSteel, "cap-ring");
            }
        }

        public void Add(string parent, string id, MemberKind k, Vec3 a, Vec3 b, MaterialRole mat, params string[] tags)
        {
            Members.Add(new Member { Id = id, Kind = k, A = a, B = b, ParentId = parent, Material = mat });
            var e = Graph.Add(id, k == MemberKind.Deck ? ElementCategory.Deck : ElementCategory.Member, parent, Concat(tags, "steel", "member"));
            e.Center = new Vec3((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f, (a.Z + b.Z) * 0.5f);
            const float pad = 0.4f;
            e.Size = new Vec3(Math.Abs(b.X - a.X) + pad, Math.Abs(b.Y - a.Y) + pad, Math.Abs(b.Z - a.Z) + pad);
        }

        static string[] Concat(string[] a, params string[] b) { var r = new string[(a?.Length ?? 0) + b.Length]; a?.CopyTo(r, 0); b.CopyTo(r, a?.Length ?? 0); return r; }

        public int CountKind(MemberKind k) { int n = 0; foreach (var m in Members) if (m.Kind == k) n++; return n; }
    }
}
