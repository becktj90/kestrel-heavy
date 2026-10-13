// Four grid fins, one shared lattice mesh. Hidden with the assembled body.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    [ExecuteAlways]
    public sealed class FinCluster : MonoBehaviour
    {
        public Camera ViewCamera;
        public int Quality = KitBudgets.Medium;
        public Renderer Gate;

        List<FinMount> _mounts = new List<FinMount>();
        InstanceBatch _batch;
        Matrix4x4[] _local = new Matrix4x4[0];
        string _key;
        int _lod = -2;

        public int Lod => _lod;

        public void Init(IReadOnlyList<FinMount> mounts, int quality)
        {
            Dispose();
            Quality = quality;
            _mounts = new List<FinMount>(mounts.Count);
            for (int i = 0; i < mounts.Count; i++) _mounts.Add(mounts[i]);
            _lod = -2;
        }

        void LateUpdate()
        {
            if (_mounts.Count == 0) return;
            if (Gate != null && !Gate.enabled) return;
            var cam = ViewCamera != null ? ViewCamera : Camera.main;
            if (cam == null) return;
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            int lod = LodPolicy.Select(Quality, DetailSize.Medium, dist);
            if (lod < 0) { if (_lod != -1) Release(); _lod = -1; return; }
            if (lod != _lod) Rebuild(lod);
            if (_batch == null) return;
            var l2w = transform.localToWorldMatrix;
            _batch.Clear();
            for (int i = 0; i < _local.Length; i++) _batch.Add(l2w * _local[i]);
            _batch.Draw(cam.transform.position, Quality, null);
        }

        void Rebuild(int lod)
        {
            Release();
            _lod = lod;
            var local = new List<Matrix4x4>(_mounts.Count);
            for (int i = 0; i < _mounts.Count; i++)
            {
                var m = _mounts[i];
                var outw = new Vector3(m.AxisX, 0f, m.AxisZ);
                var rot = Quaternion.FromToRotation(Vector3.right, outw);
                local.Add(Matrix4x4.TRS(new Vector3(m.X, m.Y, m.Z), rot, new Vector3(m.Span, m.Height, m.Thickness)));
            }
            _key = "fin/grid/lod" + lod;
            var mesh = SharedMeshes.Acquire(_key, () => BayBuilder.FromData(FinMesh.Build(lod), "KH_GridFin"));
            _batch = new InstanceBatch(mesh, VehicleLook.Metal(), DetailSize.Medium) { Shadows = UnityEngine.Rendering.ShadowCastingMode.Off };
            _local = local.ToArray();
        }

        void Release()
        {
            if (_key != null) SharedMeshes.Release(_key);
            _key = null;
            _batch = null;
            _local = new Matrix4x4[0];
        }

        void OnDestroy() => Dispose();
        public void Dispose() { Release(); _lod = -2; }
    }
}
