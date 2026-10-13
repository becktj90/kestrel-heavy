// Instanced bells for one vehicle. One mesh per (engine type, vehicle seed, LOD); each engine is a
// matrix (roll + a fraction of a degree of rest gimbal). Drawn only while the assembled rocket is visible,
// so the exploded hangar view does not double up.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    [ExecuteAlways]
    public sealed class BellCluster : MonoBehaviour
    {
        public Camera ViewCamera;
        public int Quality = KitBudgets.Medium;
        /// <summary>Assembled body renderer. When the slice hides it (exploded view), the bells hide too.</summary>
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
            int lod = LodPolicy.Select(Quality, DetailSize.Medium, dist, BellBuilder.HeroLodBias);
            if (lod < 0) lod = 2;   // the bell stays on screen with the vehicle
            if (lod != _lod) Rebuild(lod);
            var l2w = transform.localToWorldMatrix;
            var eye = cam.transform.position;
            for (int g = 0; g < _groups.Count; g++)
            {
                var group = _groups[g];
                group.Batch.Clear();
                var local = group.Local;
                for (int i = 0; i < local.Length; i++) group.Batch.Add(l2w * local[i]);
                group.Batch.Draw(eye, Quality, null, BellBuilder.HeroLodBias);
            }
        }

        void Rebuild(int lod)
        {
            ReleaseGroups();
            _lod = lod;
            var order = new List<string>();
            var locals = new Dictionary<string, List<Matrix4x4>>();
            var contours = new Dictionary<string, BellContour>();
            for (int i = 0; i < _mounts.Count; i++)
            {
                var m = _mounts[i];
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
                var contour = BellProfile.Build(spec, lod);
                var inst = BellVariation.Place(spec, m.Stage, m.Slot);
                var rot = Quaternion.Euler(inst.GimbalXDeg, inst.RollDeg, inst.GimbalZDeg);
                var local = Matrix4x4.TRS(new Vector3(m.X, m.Y, m.Z), rot, Vector3.one);
                if (!locals.TryGetValue(contour.MeshKey, out var list))
                {
                    list = new List<Matrix4x4>();
                    locals[contour.MeshKey] = list;
                    contours[contour.MeshKey] = contour;
                    order.Add(contour.MeshKey);
                }
                list.Add(local);
            }
            var mat = BellBuilder.Material();
            for (int i = 0; i < order.Count; i++)
            {
                string key = order[i];
                var mesh = SharedMeshes.Acquire(key, () => BellBuilder.Build(contours[key], lod));
                var batch = new InstanceBatch(mesh, mat, DetailSize.Medium) { Shadows = UnityEngine.Rendering.ShadowCastingMode.On };
                var local = locals[key].ToArray();
                _groups.Add(new Group { Key = key, Batch = batch, Local = local });
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
