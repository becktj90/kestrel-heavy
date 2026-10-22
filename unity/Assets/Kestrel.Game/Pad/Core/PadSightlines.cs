// Pad structure as capsules for the flight camera (#348): every layout member and every umbilical mast/arm beam,
// pad space, with a radius that covers the drawn section (PadRenderer: I-beam columns 0.45 x 0.5 m, girts
// 0.3 x 0.35 m, braces 0.2 m, deck planks 0.9 m, arm members 0.14 m). SliceApp turns them into world space for
// FlightViewPose (Below: clear the near plane, see past them); the EditMode framing tests use the same list.
// Lamp poles are not members: PadLighting.PoleTooClose skips the ones in a low view's foreground.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public static class PadSightlines
    {
        public struct Capsule { public Vec3 A, B; public float Radius; public string Id; }

        public static float RadiusFor(MemberKind k) => k switch
        {
            MemberKind.Column => 0.36f,
            MemberKind.Girt => 0.24f,
            MemberKind.Deck => 0.46f,
            MemberKind.Rail => 0.3f,
            MemberKind.ArmChord or MemberKind.ArmBrace => 0.1f,
            _ => 0.15f,
        };

        public static List<Capsule> Capsules(PadLayout L)
        {
            var list = new List<Capsule>();
            if (L == null) return list;
            foreach (var m in L.Members) list.Add(new Capsule { A = m.A, B = m.B, Radius = RadiusFor(m.Kind), Id = m.Id });
            foreach (var link in L.Links)
                if (link.Mast != null)
                    foreach (var m in link.Mast) list.Add(new Capsule { A = m.A, B = m.B, Radius = RadiusFor(m.Kind), Id = m.Id });
            return list;
        }

        /// <summary>Lamp poles as capsules (deck to fixture), for sight checks that honour PoleTooClose.</summary>
        public static List<Capsule> Poles(IReadOnlyList<LightSpec> plan, float camX, float camZ)
        {
            var list = new List<Capsule>();
            if (plan == null) return list;
            foreach (var s in plan)
            {
                if (s.Kind != LightKind.Pole || PadLighting.PoleTooClose(s, camX, camZ)) continue;
                list.Add(new Capsule { A = new Vec3(s.Position.X, 0f, s.Position.Z), B = s.Position, Radius = 0.25f, Id = s.Id });
            }
            return list;
        }
    }
}
