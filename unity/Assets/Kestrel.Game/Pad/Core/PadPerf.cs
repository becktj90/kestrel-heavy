// Static perf estimate for the D1 tower (engine-free) so tests can hold it inside KitBudgets.
using Kestrel.Kit;

namespace Kestrel.Pad
{
    public static class PadPerf
    {
        public const int BeamTris = 12, IBeamTris = 36;
        /// <summary>Batches = distinct (mesh, material) pairs among members, plus one per swing arm.</summary>
        public const int ArmBatchesPerArm = 1;

        public static int TrisFor(MemberKind k) => k == MemberKind.Column || k == MemberKind.Girt ? IBeamTris : BeamTris;

        /// <summary>Worst case: everything inside cull range (camera at the pad).</summary>
        public static void Estimate(PadLayout l, BudgetLedger ledger)
        {
            long tris = 0; var kinds = new System.Collections.Generic.HashSet<(bool, MaterialRole)>();
            foreach (var m in l.Members) { tris += TrisFor(m.Kind); kinds.Add((TrisFor(m.Kind) == IBeamTris, m.Material)); }
            foreach (var a in l.Arms) tris += (long)SwingArmTruss.MemberCount(a.MaxLength) * BeamTris;
            int umbilicalDraws = 0;
            if (l.Links != null && l.Links.Count > 0)
            {
                // One combined hose batch and one plate batch, whatever the strand count. Masts reuse the arm beam.
                umbilicalDraws = 2;
                foreach (var u in l.Links)
                {
                    tris += u.Mast.Count * BeamTris;
                    tris += (u.Strands != null ? u.Strands.Length : 1) * 64;   // LOD2 tube, 8 samples × 4 sides
                }
            }
            ledger.Add(kinds.Count + l.Arms.Count * ArmBatchesPerArm + umbilicalDraws, tris);
            if (l.Trench != null)
            {
                var detail = TrenchMeshBuilder.Build(l.Trench, TrenchGrammar.SurfaceRadius(l));
                ledger.Add(detail.DrawCalls, detail.Triangles);
            }
        }
    }
}
