// Named attach frames on the service tower (D2). Engine-free. D4 umbilical/service arms and later detail
// (lights, risers, slide wires) mount here instead of re-deriving tower geometry.
// Frame: pad space (origin on the vehicle centre line at deck level, +Y up, tower on +Z). Each anchor's
// local +Z points away from the tower face it sits on (for "s" face anchors: toward the vehicle, pad -Z);
// local +Y is world up. Rotation uses the contract Quat (x, y, z, w), same convention as UnityEngine.Quaternion.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public struct TowerAnchor
    {
        /// <summary>Stable id, see <see cref="TowerAnchors"/> for the naming scheme.</summary>
        public string Id;
        public Vec3 Position;
        public Quat Rotation;
        /// <summary>Tower level index (0 = first deck at LevelHeight). -1 = ground, Levels = cap.</summary>
        public int Level;
        /// <summary>Tower face: "s" (vehicle side), "e", "n", "w", or "top".</summary>
        public string Face;
        public override string ToString() => $"{Id} L{Level} {Face} ({Position.X:0.##}, {Position.Y:0.##}, {Position.Z:0.##})";
    }

    /// <summary>
    /// Anchor ids:
    ///  level.{NN}.{s|e|n|w}  centre of each face at deck height of level NN (deck top = (NN+1) × LevelHeight)
    ///  arm.crew              crew-access arm root on the s face (only when the tower has a crew arm)
    ///  arm.{attachId}        swing-arm pivot for a SwingArmRetract umbilical (added by PadGenerator)
    ///  cap.top               lightning spike base on top of the cap frame
    ///  ground.s              foot of the s face, deck level
    /// </summary>
    public sealed class TowerAnchors
    {
        readonly List<TowerAnchor> _list = new List<TowerAnchor>();
        readonly Dictionary<string, int> _ix = new Dictionary<string, int>();
        public readonly TowerParams Params;

        public IReadOnlyList<TowerAnchor> All => _list;
        public int Count => _list.Count;

        /// <summary>Yaw (deg about +Y) whose +Z points out of each face. s = 180 (toward the vehicle).</summary>
        public static float FaceYaw(string face) => face switch { "s" => 180f, "e" => 90f, "n" => 0f, "w" => 270f, _ => 180f };
        public static Quat Yaw(float deg) { double h = deg * Math.PI / 360.0; return new Quat(0, (float)Math.Sin(h), 0, (float)Math.Cos(h)); }

        public TowerAnchors(TowerParams p)
        {
            Params = p ?? throw new ArgumentNullException(nameof(p));
            float hw = p.Width * 0.5f;
            Add("ground.s", new Vec3(0, 0, p.NearFaceZ), "s", -1);
            for (int l = 0; l < p.Levels; l++)
            {
                float y = (l + 1) * p.LevelHeight;
                Add(LevelId(l, "s"), new Vec3(0, y, p.StandOff - hw), "s", l);
                Add(LevelId(l, "e"), new Vec3(hw, y, p.StandOff), "e", l);
                Add(LevelId(l, "n"), new Vec3(0, y, p.StandOff + hw), "n", l);
                Add(LevelId(l, "w"), new Vec3(-hw, y, p.StandOff), "w", l);
            }
            if (p.CrewLevel >= 0 && p.CrewLevel < p.Levels)
                Add(CrewArm, new Vec3(p.CrewArmX, (p.CrewLevel + 1) * p.LevelHeight, p.NearFaceZ), "s", p.CrewLevel);
            Add(CapTop, new Vec3(0, p.Height + p.CapHeight, p.StandOff), "top", p.Levels);
        }

        public const string CrewArm = "arm.crew", CapTop = "cap.top";
        public static string LevelId(int level, string face) => $"level.{level:00}.{face}";
        public static string ArmId(string attachId) => $"arm.{attachId}";

        public void Add(string id, Vec3 pos, string face, int level)
        {
            var a = new TowerAnchor { Id = id, Position = pos, Rotation = Yaw(FaceYaw(face)), Level = level, Face = face };
            if (_ix.TryGetValue(id, out var i)) _list[i] = a; else { _ix[id] = _list.Count; _list.Add(a); }
        }

        public bool Contains(string id) => _ix.ContainsKey(id);
        public bool TryGet(string id, out TowerAnchor a) { if (_ix.TryGetValue(id, out var i)) { a = _list[i]; return true; } a = default; return false; }
        /// <summary>Throws KeyNotFoundException for an unknown id.</summary>
        public TowerAnchor Get(string id) => TryGet(id, out var a) ? a : throw new KeyNotFoundException($"no tower anchor '{id}'");

        /// <summary>Level whose deck is nearest height y (pad space), clamped to the tower.</summary>
        public int LevelNearest(float y) => Math.Max(0, Math.Min(Params.Levels - 1, (int)Math.Round(y / Params.LevelHeight) - 1));

        /// <summary>Anchor on <paramref name="face"/> at the deck nearest height y.</summary>
        public TowerAnchor OnFaceNear(float y, string face = "s") => Get(LevelId(LevelNearest(y), face));
    }
}
