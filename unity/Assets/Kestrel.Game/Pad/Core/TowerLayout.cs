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
    public enum MemberKind { Column, Girt, Brace, Deck, ArmChord, ArmBrace }

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

        public TowerLayout(TowerParams p, ElementGraph graph = null)
        {
            P = p ?? new TowerParams(); Graph = graph ?? new ElementGraph();
            if (!Graph.Contains(SiteId)) Graph.Add(SiteId, ElementCategory.Site, null, "pad");
            Build();
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
            string cap = "tower.cap"; var ce = Graph.Add(cap, ElementCategory.Level, TowerId, "steel", "cap");
            ce.Center = new Vec3(0, P.Height + P.CapHeight * 0.5f, P.StandOff); ce.Size = new Vec3(P.Width, P.CapHeight, P.Width);
            for (int c = 0; c < 4; c++)
            {
                var top = new Vec3(0, P.Height + P.CapHeight, P.StandOff);
                Add(cap, $"{cap}.post.{Corner[c]}", MemberKind.Brace, CornerAt(c, P.Height), top, MaterialRole.PaintedSteel, "brace", "cap");
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
