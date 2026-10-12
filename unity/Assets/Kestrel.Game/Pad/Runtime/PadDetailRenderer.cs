// Draws the flame trench, deflector and deck detail. Four shared kit materials, no child
// GameObjects, culled with the large-detail distance so a phone does not keep them forever.
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Pad
{
    [DisallowMultipleComponent]
    public sealed class PadDetailRenderer : MonoBehaviour
    {
        Mesh _concrete, _refractory, _galvanized, _markings;

        public Mesh MeshFor(MaterialRole role)
        {
            switch (role)
            {
                case MaterialRole.Concrete: return _concrete;
                case MaterialRole.Refractory: return _refractory;
                case MaterialRole.Galvanized: return _galvanized;
                case MaterialRole.WarningStripes: return _markings;
                default: return null;
            }
        }

        /// <summary>The shared kit material. Callers must not instantiate a copy.</summary>
        public Material MaterialFor(MaterialRole role) => MaterialLibrary.Get(role);

        public void Build(PadLayout layout)
        {
            ClearMeshes();
            if (layout == null || layout.Trench == null) return;
            var built = TrenchMeshBuilder.Build(layout.Trench, TrenchGrammar.SurfaceRadius(layout));
            _concrete = PadMeshUpload.Upload(built.Concrete, "KH_PadConcrete");
            _refractory = PadMeshUpload.Upload(built.Refractory, "KH_PadRefractory");
            _galvanized = PadMeshUpload.Upload(built.Galvanized, "KH_PadGrating");
            _markings = PadMeshUpload.Upload(built.Markings, "KH_PadMarks");
        }

        void LateUpdate()
        {
            if (_concrete == null && _refractory == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            int q = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, 2);
            float cull = LodPolicy.CullDistance(q, DetailSize.Large);
            if ((cam.transform.position - transform.position).sqrMagnitude > cull * cull) return;
            var matrix = transform.localToWorldMatrix;
            var shadows = q <= KitBudgets.Low ? ShadowCastingMode.Off : ShadowCastingMode.On;
            Draw(_concrete, MaterialRole.Concrete, matrix, shadows);
            Draw(_refractory, MaterialRole.Refractory, matrix, shadows);
            Draw(_galvanized, MaterialRole.Galvanized, matrix, shadows);
            Draw(_markings, MaterialRole.WarningStripes, matrix, shadows);
        }

        void Draw(Mesh mesh, MaterialRole role, Matrix4x4 matrix, ShadowCastingMode shadows)
        {
            if (mesh == null) return;
            var mat = MaterialLibrary.Get(role);
            if (mat == null) return;
            var rp = new RenderParams(mat)
            {
                shadowCastingMode = shadows,
                layer = gameObject.layer,
                receiveShadows = true,
                lightProbeUsage = LightProbeUsage.Off,
            };
            Graphics.RenderMesh(rp, mesh, 0, matrix);
        }

        public void ClearMeshes()
        {
            Kill(_concrete); Kill(_refractory); Kill(_galvanized); Kill(_markings);
            _concrete = _refractory = _galvanized = _markings = null;
        }

        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        void OnDestroy() => ClearMeshes();
    }
}
