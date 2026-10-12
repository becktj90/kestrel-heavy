// One tank on the vehicle: owns its LOD meshes, swaps by camera distance via Kestrel.Kit.LodPolicy (DetailSize.Large,
// hero bias), and supports the cutaway/Lab mode. Wiring into SliceApp/RocketMeshBuilder is Kestrel Arcade's PR.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class TankShellView : MonoBehaviour
    {
        public const float HeroLodBias = 1.5f;
        /// <summary>Cutaway removes the quarter centred on +Z (toward the camera's tower-side key view).</summary>
        public const float CutSector = Mathf.PI / 2;

        public int Quality = KitBudgets.Medium;
        public Camera ViewCamera;
        TankShellSpec _spec; readonly Mesh[] _lod = new Mesh[3]; Mesh _cut;
        InstanceBatch _ribs; List<Matrix4x4> _ribLocal;
        int _current = -2; bool _cutaway; MeshFilter _mf; MeshRenderer _mr;

        public int CurrentLod => _current;
        public bool Cutaway => _cutaway;
        public int RibCount => _ribLocal?.Count ?? 0;

        public void Init(TankShellSpec spec, int quality)
        {
            Release();
            _spec = spec; Quality = quality;
            _mf = GetComponent<MeshFilter>(); _mr = GetComponent<MeshRenderer>();
            _mr.sharedMaterials = TankShellBuilder.Materials();
            for (int i = 0; i < 3; i++) _lod[i] = TankShellBuilder.Build(spec, i);
            _current = -2; SetLod(0);
        }

        /// <summary>Cutaway (2.0, solid) / Detail Lab: LOD0 shell with a quarter removed + real isogrid ribs.</summary>
        public void SetCutaway(bool on)
        {
            _cutaway = on;
            if (on && _cut == null)
            {
                // The sector is [-45, +45] deg around +Z. LatheMesh cuts [from, to) in [0, 2pi), so build the cut at
                // [0, 90) deg and rotate the mesh vertices by -45 deg about Y (baked; the transform is untouched).
                var rot = Matrix4x4.Rotate(Quaternion.Euler(0, -CutSector * 0.5f * Mathf.Rad2Deg, 0));
                var raw = TankShellBuilder.Build(_spec, 0, 0, CutSector);
                _cut = new Mesh { name = "KH_Tank_Cutaway", indexFormat = raw.indexFormat };
                var ci = new CombineInstance[raw.subMeshCount];
                for (int i = 0; i < ci.Length; i++) ci[i] = new CombineInstance { mesh = raw, subMeshIndex = i, transform = rot };
                _cut.CombineMeshes(ci, mergeSubMeshes: false, useMatrices: true);
                Kill(raw);
                _ribLocal = IsogridCutaway.RibMatrices(_spec, 0, CutSector);
                for (int i = 0; i < _ribLocal.Count; i++) _ribLocal[i] = rot * _ribLocal[i];
                _ribs = IsogridCutaway.NewBatch();
            }
            _current = -2;
        }

        void LateUpdate()
        {
            var cam = ViewCamera != null ? ViewCamera : Camera.main;
            if (cam == null || _mf == null) return;
            if (_cutaway)
            {
                if (_mf.sharedMesh != _cut) { _mf.sharedMesh = _cut; _mr.enabled = true; }
                // Ribs follow the moving tank: re-feed world matrices (<= MaxRibs) once per frame. Lab/cutaway only.
                _ribs.Clear();
                var l2w = transform.localToWorldMatrix;
                foreach (var m in _ribLocal) _ribs.Add(l2w * m);
                _ribs.Draw(cam.transform.position, Quality);
                return;
            }
            SetLod(LodPolicy.Select(Quality, DetailSize.Large, Vector3.Distance(cam.transform.position, transform.position), HeroLodBias));
        }

        void SetLod(int lod)
        {
            if (lod == _current) return;
            _current = lod;
            if (lod < 0) { _mr.enabled = false; return; }
            _mr.enabled = true; _mf.sharedMesh = _lod[lod];
        }

        void Release()
        {
            for (int i = 0; i < 3; i++) if (_lod[i] != null) { Kill(_lod[i]); _lod[i] = null; }
            if (_cut != null) { Kill(_cut); _cut = null; }
            if (_ribs != null) { Kill(_ribs.Mesh); _ribs = null; }
            _ribLocal = null;
        }

        void OnDestroy() => Release();

        static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
    }
}
