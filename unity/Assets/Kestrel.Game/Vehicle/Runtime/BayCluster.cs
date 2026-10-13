// Instanced turbopumps, feed lines, ducts, valves and actuators for one vehicle. One mesh per part kind.
// Hidden with the assembled body so the exploded hangar view does not draw them twice.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    [ExecuteAlways]
    public sealed class BayCluster : MonoBehaviour
    {
        public Camera ViewCamera;
        public int Quality = KitBudgets.Medium;
        public Renderer Gate;

        List<BellMount> _mounts = new List<BellMount>();
        readonly List<Group> _groups = new List<Group>();
        int _lod = -2;

        sealed class Group
        {
            public string Key;
            public InstanceBatch Batch;
            public Matrix4x4[] Local;
        }

        public int GroupCount => _groups.Count;
        public int Lod => _lod;

        public void Init(IReadOnlyList<BellMount> mounts, int quality)
        {
            Dispose();
            Quality = quality;
            _mounts = new List<BellMount>(mounts.Count);
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
            int lod = LodPolicy.Select(Quality, DetailSize.Small, dist);
            if (lod < 0) { if (_lod != -1) ReleaseGroups(); _lod = -1; return; }
            if (lod != _lod) Rebuild(lod);
            var l2w = transform.localToWorldMatrix;
            var eye = cam.transform.position;
            for (int g = 0; g < _groups.Count; g++)
            {
                var group = _groups[g];
                group.Batch.Clear();
                var local = group.Local;
                for (int i = 0; i < local.Length; i++) group.Batch.Add(l2w * local[i]);
                group.Batch.Draw(eye, Quality, null);
            }
        }

        void Rebuild(int lod)
        {
            ReleaseGroups();
            _lod = lod;
            var mat = BayBuilder.Material();
            var order = new List<string>();
            var locals = new Dictionary<string, List<Matrix4x4>>();
            var kinds = new Dictionary<string, string>();
            for (int i = 0; i < _mounts.Count; i++)
            {
                var m = _mounts[i];
                if (!(m.SkinRadius > 0.05f)) continue;
                if (!BellSpec.TryParseCycle(m.Cycle, out var cycle)) continue;
                var spec = new BellSpec
                {
                    EngineId = string.IsNullOrEmpty(m.EngineId) ? "engine" : m.EngineId,
                    Cycle = cycle,
                    ExpansionRatio = m.ExpansionRatio,
                    ExitDiameter = m.ExitDiameter,
                    VehicleSeed = m.Seed
                };
                if (spec.Validate().Count > 0) continue;
                var placed = BellVariation.Place(spec, m.Stage, m.Slot);
                var gimbal = Quaternion.Euler(placed.GimbalXDeg, placed.RollDeg, placed.GimbalZDeg);
                var origin = new Vector3(m.X, m.Y, m.Z);
                var poses = EngineBayLayout.Poses(spec, m.Gimbal, m.SkinRadius);
                for (int p = 0; p < poses.Count; p++)
                {
                    var pose = poses[p];
                    if (!BayMesh.Draws(pose.Kind, lod)) continue;
                    string key = BayMesh.Key(pose.Kind, lod);
                    var axis = new Vector3(pose.AxisX, pose.AxisY, pose.AxisZ);
                    var rot = gimbal * Quaternion.FromToRotation(Vector3.up, axis);
                    var pos = origin + gimbal * new Vector3(pose.X, pose.Y, pose.Z);
                    var local = Matrix4x4.TRS(pos, rot, new Vector3(pose.Radius, pose.Length, pose.Radius));
                    if (!locals.TryGetValue(key, out var list))
                    {
                        list = new List<Matrix4x4>();
                        locals[key] = list;
                        kinds[key] = pose.Kind;
                        order.Add(key);
                    }
                    list.Add(local);
                }
            }
            for (int i = 0; i < order.Count; i++)
            {
                string key = order[i];
                string kind = kinds[key];
                var mesh = SharedMeshes.Acquire(key, () => BayBuilder.Build(kind, lod));
                if (mesh == null || mesh.vertexCount == 0) { SharedMeshes.Release(key); continue; }
                var batch = new InstanceBatch(mesh, mat, DetailSize.Small) { Shadows = UnityEngine.Rendering.ShadowCastingMode.Off };
                _groups.Add(new Group { Key = key, Batch = batch, Local = locals[key].ToArray() });
            }
        }

        void ReleaseGroups()
        {
            for (int i = 0; i < _groups.Count; i++) SharedMeshes.Release(_groups[i].Key);
            _groups.Clear();
        }

        void OnDestroy() => Dispose();

        public void Dispose()
        {
            ReleaseGroups();
            _lod = -2;
        }
    }
}
