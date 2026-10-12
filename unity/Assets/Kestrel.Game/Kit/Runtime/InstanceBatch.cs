// GPU instancing helper: one mesh + material, many transforms, drawn with Graphics.RenderMeshInstanced in
// chunks of 1023. Use for bolts, beams, valves, light masts, nozzles. One batch == ceil(count / 1023) draw calls
// per pass (shadow casters add one more per light on High).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Kit
{
    public sealed class InstanceBatch
    {
        public readonly Mesh Mesh; public readonly Material Material; public readonly DetailSize Size;
        readonly List<Matrix4x4> _all = new List<Matrix4x4>();
        readonly List<Vector3> _pos = new List<Vector3>();
        readonly Matrix4x4[] _chunk = new Matrix4x4[KitBudgets.MaxInstancesPerBatch];
        public int Count => _all.Count;
        public ShadowCastingMode Shadows = ShadowCastingMode.On;
        public int Layer;

        public InstanceBatch(Mesh mesh, Material material, DetailSize size) { Mesh = mesh; Material = material; Size = size; }

        public void Add(Matrix4x4 m) { _all.Add(m); _pos.Add(m.GetColumn(3)); }
        public void Clear() { _all.Clear(); _pos.Clear(); }

        /// <summary>Draw-call estimate for a visible count (used by BudgetLedger and tests).</summary>
        public static int DrawCallsFor(int visible) => visible <= 0 ? 0 : (visible + KitBudgets.MaxInstancesPerBatch - 1) / KitBudgets.MaxInstancesPerBatch;

        /// <summary>Distance-cull by LodPolicy, then draw. Returns visible instance count.</summary>
        public int Draw(Vector3 cameraPos, int quality, BudgetLedger ledger = null, float lodBias = 1f)
        {
            if (Mesh == null || Material == null) return 0;
            var rp = new RenderParams(Material) { shadowCastingMode = quality <= KitBudgets.Low ? ShadowCastingMode.Off : Shadows, layer = Layer, receiveShadows = true };
            float cull = LodPolicy.CullDistance(quality, Size) * lodBias, cull2 = cull * cull;
            int n = 0, visible = 0;
            for (int i = 0; i < _all.Count; i++)
            {
                if ((_pos[i] - cameraPos).sqrMagnitude > cull2) continue;
                _chunk[n++] = _all[i]; visible++;
                if (n == _chunk.Length) { Graphics.RenderMeshInstanced(rp, Mesh, 0, _chunk, n); n = 0; }
            }
            if (n > 0) Graphics.RenderMeshInstanced(rp, Mesh, 0, _chunk, n);
            ledger?.Add(DrawCallsFor(visible), (long)visible * KitMesh.TriangleCount(Mesh));
            return visible;
        }
    }
}
