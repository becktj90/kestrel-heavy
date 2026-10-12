// Box-truss members for a swing arm in arm-local space: pivot at origin, arm along +Z (Unity forward), deck at y=0.
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public static class SwingArmTruss
    {
        public const float Bay = 2f;
        public static int Bays(float length) => System.Math.Max(1, (int)System.Math.Ceiling(length / Bay));
        /// <summary>4 chords + per bay (4 ring girts + 2 side diagonals) + end ring.</summary>
        public static int MemberCount(float length) => 4 + Bays(length) * 6 + 4;

        public static List<Member> Build(string id, float length, float width, float depth)
        {
            var r = new List<Member>(); float x = width / 2; int n = Bays(length); float bay = length / n;
            Vec3 C(int c, float z) => new Vec3((c == 0 || c == 3) ? -x : x, c < 2 ? 0 : depth, z);
            void M(string s, MemberKind k, Vec3 a, Vec3 b) => r.Add(new Member { Id = $"{id}.{s}", Kind = k, A = a, B = b, ParentId = id, Material = Kestrel.Kit.MaterialRole.SafetyYellowSteel });
            for (int c = 0; c < 4; c++) M($"chord.{c}", MemberKind.ArmChord, C(c, 0), C(c, length));
            for (int i = 0; i <= n; i++)
            {
                if (i == n) { for (int c = 0; c < 4; c++) M($"end.{c}", MemberKind.ArmBrace, C(c, length), C((c + 1) % 4, length)); break; }
                float z0 = i * bay, z1 = z0 + bay;
                for (int c = 0; c < 4; c++) M($"b{i:00}.ring.{c}", MemberKind.ArmBrace, C(c, z0), C((c + 1) % 4, z0));
                M($"b{i:00}.diag.l", MemberKind.ArmBrace, C(0, z0), C(3, z1));
                M($"b{i:00}.diag.r", MemberKind.ArmBrace, C(1, z0), C(2, z1));
            }
            return r;
        }
    }
}
