// Instanced tubes for interstage structure and raceways. One mesh, many matrices.
// Hidden with the assembled body so exploded view does not draw them twice.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    [ExecuteAlways]
    public sealed class StrutCluster : MonoBehaviour
    {
        public Camera ViewCamera;
        public int Quality = KitBudgets.Medium;
        public Renderer Gate;

        List<StrutPose> _poses = new List<StrutPose>();
        InstanceBatch _batch;
        Matrix4x4[] _local = new Matrix4x4[0];
        string _key;
        int _lod = -2;

        public int Lod => _lod;
        public int PoseCount => _local.Length;

        public void Init(IReadOnlyList<StrutPose> poses, int quality)
        {
            Dispose();
            Quality = quality;
            _poses = new List<StrutPose>(poses.Count);
            for (int i = 0; i < poses.Count; i++) _poses.Add(poses[i]);
            _lod = -2;
        }

        void LateUpdate()
        {
            if (_poses.Count == 0) return;
            if (Gate != null && !Gate.enabled) return;
            var cam = ViewCamera != null ? ViewCamera : Camera.main;
            if (cam == null) return;
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            int lod = LodPolicy.Select(Quality, DetailSize.Medium, dist);
            if (lod < 0) { if (_lod != -1) Release(); _lod = -1; return; }
            if (lod != _lod) Rebuild(lod);
            if (_batch == null || _local.Length == 0) return;
            var l2w = transform.localToWorldMatrix;
            _batch.Clear();
            for (int i = 0; i < _local.Length; i++) _batch.Add(l2w * _local[i]);
            _batch.Draw(cam.transform.position, Quality, null);
        }

        void Rebuild(int lod)
        {
            Release();
            _lod = lod;
            var local = new List<Matrix4x4>();
            for (int i = 0; i < _poses.Count; i++)
            {
                var p = _poses[i];
                if (!AirframeLayout.Draws(p.Kind, lod)) continue;
                var axis = new Vector3(p.AxisX, p.AxisY, p.AxisZ);
                var rot = Quaternion.FromToRotation(Vector3.up, axis);
                local.Add(Matrix4x4.TRS(new Vector3(p.X, p.Y, p.Z), rot, new Vector3(p.Radius, p.Length, p.Radius)));
            }
            if (local.Count == 0) return;
            _key = "airframe/tube/lod" + lod;
            int sides = AirframeLayout.Sides(lod);
            var mesh = SharedMeshes.Acquire(_key, () => BayBuilder.FromData(BayMesh.OpenCylinder(sides), "KH_Airframe"));
            var mat = VehicleLook.Metal();
            _batch = new InstanceBatch(mesh, mat, DetailSize.Medium) { Shadows = UnityEngine.Rendering.ShadowCastingMode.Off };
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

        public void Dispose()
        {
            Release();
            _lod = -2;
        }
    }
}
