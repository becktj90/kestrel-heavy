// Draws a generated PadLayout with the shared kit: members grouped into InstanceBatches by (section, material),
// distance-culled per LodPolicy, swing arms as SwingArm connectors. No GameObject per member.
using System.Collections.Generic;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using UnityEngine;

namespace Kestrel.Pad
{
    [DisallowMultipleComponent]
    public sealed class PadRenderer : MonoBehaviour
    {
        public const float ColumnFlange = 0.45f, ColumnDepth = 0.5f, Web = 0.04f, GirtFlange = 0.3f, GirtDepth = 0.35f, Brace = 0.2f, DeckW = 0.9f, DeckT = 0.08f, ArmMember = 0.14f;

        public PadLayout Layout { get; private set; }
        /// <summary>Identity of the vehicle mesh the layout was fitted to (VehicleSkin.MeshKey); 0 = none. Callers
        /// compare it to decide whether a rebuild is needed when only the mesh changed.</summary>
        public long SourceMeshKey;
        public readonly List<SwingArm> Arms = new List<SwingArm>();
        public readonly BudgetLedger Ledger = new BudgetLedger();
        /// <summary>-1 = follow QualitySettings (clamped to Low/Medium/High).</summary>
        public int QualityOverride = -1;
        public Camera ViewCamera;

        readonly Dictionary<(MemberKind, MaterialRole), InstanceBatch> _batches = new Dictionary<(MemberKind, MaterialRole), InstanceBatch>();
        readonly Dictionary<(MemberKind, MaterialRole), List<Matrix4x4>> _local = new Dictionary<(MemberKind, MaterialRole), List<Matrix4x4>>();
        readonly List<Mesh> _meshes = new List<Mesh>();
        Matrix4x4 _builtFor;

        public int Quality => QualityOverride >= 0 ? Mathf.Clamp(QualityOverride, 0, 2) : Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, 2);
        public IReadOnlyDictionary<(MemberKind, MaterialRole), InstanceBatch> Batches => _batches;

        public void Build(PadLayout layout)
        {
            Clear(); Layout = layout;
            var meshFor = new Dictionary<MemberKind, Mesh>();
            Mesh MeshFor(MemberKind k)
            {
                if (meshFor.TryGetValue(k, out var m)) return m;
                m = k switch
                {
                    MemberKind.Column => KitMesh.IBeam(ColumnFlange, ColumnDepth, Web),
                    MemberKind.Girt => KitMesh.IBeam(GirtFlange, GirtDepth, Web),
                    MemberKind.Deck => KitMesh.Beam(DeckW, DeckT),
                    MemberKind.ArmChord or MemberKind.ArmBrace => KitMesh.Beam(ArmMember, ArmMember),
                    _ => KitMesh.Beam(Brace, Brace),
                };
                meshFor[k] = m; _meshes.Add(m); return m;
            }
            foreach (var mem in layout.Members)
            {
                var key = (BatchKind(mem.Kind), mem.Material);
                if (!_batches.ContainsKey(key))
                {
                    _batches[key] = new InstanceBatch(MeshFor(key.Item1), MaterialLibrary.Get(mem.Material), mem.Kind == MemberKind.Deck ? DetailSize.Medium : DetailSize.Large);
                    _local[key] = new List<Matrix4x4>();
                }
                _local[key].Add(MemberMatrix(mem));
            }
            foreach (var spec in layout.Arms)
            {
                var go = new GameObject($"Arm {spec.AttachId}"); go.transform.SetParent(transform, false);
                var arm = go.AddComponent<SwingArm>(); arm.Init(spec, MeshFor(MemberKind.ArmChord)); Arms.Add(arm);
            }
            _builtFor = default; RefreshMatrices();
        }

        /// <summary>Kinds that share a mesh share a batch (D2: masts/rails ride the brace mesh, arm braces the chord mesh).</summary>
        public static MemberKind BatchKind(MemberKind k) => k switch
        {
            MemberKind.Mast or MemberKind.Rail => MemberKind.Brace,
            MemberKind.ArmBrace => MemberKind.ArmChord,
            _ => k,
        };

        /// <summary>Unit +Y beam stretched from A to B. Pad-local.</summary>
        public static Matrix4x4 MemberMatrix(Member m)
        {
            Vector3 a = m.A.ToUnity(), b = m.B.ToUnity(), d = b - a; float len = d.magnitude;
            var rot = len > 1e-5f ? Quaternion.FromToRotation(Vector3.up, d / len) : Quaternion.identity;
            return Matrix4x4.TRS(a, rot, new Vector3(1, len, 1));
        }

        /// <summary>Re-bake world matrices only when the root moved (floating-origin rebase, Apply).</summary>
        void RefreshMatrices()
        {
            var w = transform.localToWorldMatrix; if (w == _builtFor) return; _builtFor = w;
            foreach (var kv in _batches) { kv.Value.Clear(); foreach (var l in _local[kv.Key]) kv.Value.Add(w * l); }
        }

        /// <summary>Drive arm retract animation from mission time (seconds from T-0). Optional; arms also self-time.</summary>
        public void Tick(double missionT) { foreach (var a in Arms) a.Tick(missionT); }

        void LateUpdate()
        {
            if (Layout == null) return;
            RefreshMatrices();
            var cam = ViewCamera != null ? ViewCamera : Camera.main; if (cam == null) return;
            int q = Quality; Ledger.Reset(); var p = cam.transform.position;
            foreach (var b in _batches.Values) b.Draw(p, q, Ledger);
            foreach (var a in Arms) a.Draw(p, q, Ledger);
        }

        public void Clear()
        {
            foreach (var a in Arms) if (a != null) DestroyImmediate(a.gameObject);
            Arms.Clear(); _batches.Clear(); _local.Clear();
            foreach (var m in _meshes) if (m != null) DestroyImmediate(m);
            _meshes.Clear(); Layout = null;
        }

        void OnDestroy() => Clear();
    }
}
