// Panel rings, seams, an accent band and decal blocks. Ink and accent are Resources materials
// (the accent is a clone of KH_Body so the shader variant stays in the player). No stripe texture.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    [ExecuteAlways]
    public sealed class LiveryCluster : MonoBehaviour
    {
        public Camera ViewCamera;
        public int Quality = KitBudgets.Medium;
        public Renderer Gate;
        public Color Accent = new Color(0.10f, 0.24f, 0.43f);

        List<LiveryMark> _marks = new List<LiveryMark>();
        readonly List<Group> _groups = new List<Group>();
        Material _accent;
        bool _ownAccent;
        int _lod = -2;

        sealed class Group
        {
            public string Key;
            public InstanceBatch Batch;
            public Matrix4x4[] Local;
        }

        public int Lod => _lod;

        public void Init(IReadOnlyList<LiveryMark> marks, int quality, Color accent)
        {
            Dispose();
            Quality = quality;
            Accent = accent;
            _marks = new List<LiveryMark>(marks.Count);
            for (int i = 0; i < marks.Count; i++) _marks.Add(marks[i]);
            _lod = -2;
        }

        void LateUpdate()
        {
            if (_marks.Count == 0) return;
            if (Gate != null && !Gate.enabled) return;
            var cam = ViewCamera != null ? ViewCamera : Camera.main;
            if (cam == null) return;
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            int lod = LodPolicy.Select(Quality, DetailSize.Small, dist);
            if (lod < 0) { if (_lod != -1) Release(); _lod = -1; return; }
            if (lod != _lod) Rebuild(lod);
            var l2w = transform.localToWorldMatrix;
            var eye = cam.transform.position;
            for (int g = 0; g < _groups.Count; g++)
            {
                var group = _groups[g];
                group.Batch.Clear();
                for (int i = 0; i < group.Local.Length; i++) group.Batch.Add(l2w * group.Local[i]);
                group.Batch.Draw(eye, Quality, null);
            }
        }

        void Rebuild(int lod)
        {
            Release();
            _lod = lod;
            var ink = VehicleLook.Ink();
            _accent = AccentMaterial();
            var order = new List<string>();
            var locals = new Dictionary<string, List<Matrix4x4>>();
            for (int i = 0; i < _marks.Count; i++)
            {
                var m = _marks[i];
                if (!LiveryLayout.Draws(m.Kind, lod)) continue;
                string key = m.Kind + (LiveryLayout.Accent(m.Kind) ? "/accent" : "/ink");
                var axis = new Vector3(m.AxisX, m.AxisY, m.AxisZ);
                var rot = Quaternion.FromToRotation(Vector3.up, axis.sqrMagnitude < 1e-6f ? Vector3.up : axis);
                var local = Matrix4x4.TRS(new Vector3(m.X, m.Y, m.Z), rot, new Vector3(m.Sx, m.Sy, m.Sz));
                if (!locals.TryGetValue(key, out var list))
                {
                    list = new List<Matrix4x4>();
                    locals[key] = list;
                    order.Add(key);
                }
                list.Add(local);
            }
            for (int i = 0; i < order.Count; i++)
            {
                string key = order[i];
                string kind = key.Substring(0, key.IndexOf('/'));
                var mat = LiveryLayout.Accent(kind) ? _accent : ink;
                var mesh = SharedMeshes.Acquire("livery/" + key + "/lod" + lod, () => MeshFor(kind, lod));
                var batch = new InstanceBatch(mesh, mat, DetailSize.Small) { Shadows = UnityEngine.Rendering.ShadowCastingMode.Off };
                _groups.Add(new Group { Key = "livery/" + key + "/lod" + lod, Batch = batch, Local = locals[key].ToArray() });
            }
        }

        static Mesh MeshFor(string kind, int lod)
        {
            if (kind == LiveryLayout.Ring) return BayBuilder.FromData(LiveryMesh.Torus(lod), "KH_PanelRing");
            if (kind == LiveryLayout.Decal) return BayBuilder.FromData(LiveryMesh.Box(), "KH_Decal");
            int sides = lod <= 0 ? 6 : 4;
            return BayBuilder.FromData(BayMesh.OpenCylinder(sides), "KH_LiveryTube");
        }

        Material AccentMaterial()
        {
            var src = VehicleLook.Body();
            if (src == null) { _ownAccent = false; return VehicleLook.Metal(); }
            var m = new Material(src) { color = Accent, name = "KH_LiveryAccent" };
            m.enableInstancing = true;
            _ownAccent = true;
            return m;
        }

        void Release()
        {
            for (int i = 0; i < _groups.Count; i++) SharedMeshes.Release(_groups[i].Key);
            _groups.Clear();
            if (_ownAccent && _accent != null) Destroy(_accent);
            _accent = null;
            _ownAccent = false;
        }

        void OnDestroy() => Dispose();
        public void Dispose() { Release(); _lod = -2; }
    }
}
