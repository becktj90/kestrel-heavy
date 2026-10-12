// One tank on the vehicle: holds its LOD meshes (shared, ref-counted via SharedMeshes), swaps by camera distance via
// Kestrel.Kit.LodPolicy (DetailSize.Large, hero bias), and supports cutaway/Lab. Every mesh it acquires is released
// on re-Init, on cutaway off, and on OnDestroy/Dispose. ExecuteAlways so OnDestroy also runs in edit mode/tests.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class TankShellView : MonoBehaviour
    {
        public const float HeroLodBias = 1.5f;
        /// <summary>Cutaway removes the quarter centred on +Z (toward the camera's tower-side key view).</summary>
        public const float CutSector = Mathf.PI / 2;

        public int Quality = KitBudgets.Medium;
        public Camera ViewCamera;
        TankShellSpec _spec;
        readonly string[] _lodKey = new string[3]; readonly Mesh[] _lod = new Mesh[3];
        string _cutKey; Mesh _cut; InstanceBatch _ribs; List<Matrix4x4> _ribLocal;
        int _current = -2; bool _cutaway; MeshFilter _mf; MeshRenderer _mr;

        public int CurrentLod => _current;
        public bool Cutaway => _cutaway;
        public int RibCount => _ribLocal?.Count ?? 0;

        public void Init(TankShellSpec spec, int quality)
        {
            Dispose();
            _spec = spec; Quality = quality;
            _mf = GetComponent<MeshFilter>(); _mr = GetComponent<MeshRenderer>();
            _mr.sharedMaterials = TankShellBuilder.Materials();
            for (int i = 0; i < 3; i++)
            {
                int lod = i; _lodKey[i] = $"tank|{spec.Key}|L{lod}";
                _lod[i] = SharedMeshes.Acquire(_lodKey[i], () => TankShellBuilder.Build(spec, lod));
            }
            _current = -2; SetLod(0);
        }

        /// <summary>Cutaway (2.0, solid) / Detail Lab: LOD0 shell with a quarter removed + real isogrid ribs.
        /// Turning it off releases the cutaway mesh and the rib mesh.</summary>
        public void SetCutaway(bool on)
        {
            if (on == _cutaway) return;
            _cutaway = on;
            if (on)
            {
                // Sector [-45, +45] deg around +Z. LatheMesh cuts [from, to) in [0, 2pi): build at [0, 90) deg and bake a
                // -45 deg rotation into the vertices (transform untouched).
                var rot = Matrix4x4.Rotate(Quaternion.Euler(0, -CutSector * 0.5f * Mathf.Rad2Deg, 0));
                var spec = _spec;
                _cutKey = $"tank|{spec.Key}|cut";
                _cut = SharedMeshes.Acquire(_cutKey, () =>
                {
                    // Rotate in place (CombineMeshes would drop empty submeshes, as in #106).
                    var m = TankShellBuilder.Build(spec, 0, 0, CutSector); m.name = "KH_Tank_Cutaway";
                    var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<Vector4>();
                    m.GetVertices(v); m.GetNormals(n); m.GetTangents(t);
                    for (int i = 0; i < v.Count; i++)
                    {
                        v[i] = rot.MultiplyPoint3x4(v[i]); n[i] = rot.MultiplyVector(n[i]);
                        if (i < t.Count) { var d = rot.MultiplyVector(t[i]); t[i] = new Vector4(d.x, d.y, d.z, t[i].w); }
                    }
                    m.SetVertices(v); m.SetNormals(n); if (t.Count == v.Count) m.SetTangents(t); m.RecalculateBounds();
                    return m;
                });
                _ribLocal = IsogridCutaway.RibMatrices(spec, 0, CutSector);
                for (int i = 0; i < _ribLocal.Count; i++) _ribLocal[i] = rot * _ribLocal[i];
                _ribs = IsogridCutaway.NewBatch();
            }
            else ReleaseCutaway();
            _current = -2;
            if (!on && _mf != null) SetLod(0);
        }

        void LateUpdate()
        {
            if (_mf == null) return;
            var cam = ViewCamera != null ? ViewCamera : Camera.main;
            if (cam == null) return;
            if (_cutaway)
            {
                if (_mf.sharedMesh != _cut) { _mf.sharedMesh = _cut; _mr.enabled = true; }
                _ribs.Clear();
                var l2w = transform.localToWorldMatrix;
                foreach (var m in _ribLocal) _ribs.Add(l2w * m);
                _ribs.Draw(cam.transform.position, Quality);
                return;
            }
            SetLod(LodPolicy.Select(Quality, DetailSize.Large, Vector3.Distance(cam.transform.position, transform.position), HeroLodBias));
        }

        /// <summary>LOD switch only swaps shared meshes; nothing is built or destroyed here.</summary>
        void SetLod(int lod)
        {
            if (lod == _current) return;
            _current = lod;
            if (lod < 0) { _mr.enabled = false; return; }
            _mr.enabled = true; _mf.sharedMesh = _lod[lod];
        }

        void ReleaseCutaway()
        {
            if (_cutKey != null) { SharedMeshes.Release(_cutKey); _cutKey = null; _cut = null; }
            if (_ribs != null) { SharedMeshes.Release(IsogridCutaway.RibMeshKey); _ribs = null; }
            _ribLocal = null;
        }

        /// <summary>Releases every mesh this view holds. Safe to call more than once.</summary>
        public void Dispose()
        {
            if (_mf != null) _mf.sharedMesh = null;
            for (int i = 0; i < 3; i++) { if (_lodKey[i] != null) SharedMeshes.Release(_lodKey[i]); _lodKey[i] = null; _lod[i] = null; }
            ReleaseCutaway(); _cutaway = false; _current = -2;
        }

        void OnDestroy() => Dispose();
    }
}
