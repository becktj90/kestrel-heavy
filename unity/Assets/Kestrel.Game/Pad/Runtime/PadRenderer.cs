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
        public readonly List<UmbilicalVisual> Umbilicals = new List<UmbilicalVisual>();
        /// <summary>Fingerprint of the vehicle attach points this pad was built for. 0 = generator defaults.</summary>
        public int AttachStamp;
        public readonly BudgetLedger Ledger = new BudgetLedger();
        /// <summary>-1 = follow QualitySettings (clamped to Low/Medium/High).</summary>
        public int QualityOverride = -1;
        public Camera ViewCamera;

        readonly Dictionary<(MemberKind, MaterialRole), InstanceBatch> _batches = new Dictionary<(MemberKind, MaterialRole), InstanceBatch>();
        readonly Dictionary<(MemberKind, MaterialRole), List<Matrix4x4>> _local = new Dictionary<(MemberKind, MaterialRole), List<Matrix4x4>>();
        readonly List<Mesh> _meshes = new List<Mesh>();
        readonly List<CrewSlot> _crew = new List<CrewSlot>();
        IReadOnlyList<UmbilicalEvent> _events;
        IReadOnlyList<AttachPoint> _points;
        double _seqPrev = double.NaN;
        float _crewYaw;
        Matrix4x4 _builtFor;

        struct CrewSlot { public Member Rest; public MemberKind Kind; public MaterialRole Mat; public int Index; }

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
                if (mem.ParentId == "tower.crewarm")
                    _crew.Add(new CrewSlot { Rest = mem, Kind = key.Item1, Mat = mem.Material, Index = _local[key].Count - 1 });
            }
            var beam = MeshFor(MemberKind.ArmChord);
            foreach (var spec in layout.Arms)
            {
                var go = new GameObject($"Arm {spec.AttachId}"); go.transform.SetParent(transform, false);
                var arm = go.AddComponent<SwingArm>(); arm.Init(spec, beam); Arms.Add(arm);
            }
            foreach (var link in layout.Links)
            {
                var go = new GameObject($"Umbilical {link.AttachId}"); go.transform.SetParent(transform, false);
                var vis = go.AddComponent<UmbilicalVisual>();
                SwingArm follow = null;
                foreach (var a in Arms) if (a.AttachId == link.AttachId) follow = a;
                vis.Init(link, follow, beam); Umbilicals.Add(vis);
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
        public void Tick(double missionT)
        {
            foreach (var a in Arms) a.Tick(missionT);
            foreach (var u in Umbilicals) u.SetMissionTime(missionT);
        }

        /// <summary>Remember the attach points so <see cref="AdvanceTo"/> can replay the contract timeline.</summary>
        public void BindSequence(IReadOnlyList<AttachPoint> points)
        {
            _points = points;
            _events = points == null || points.Count == 0 ? null : UmbilicalTimeline.Build(points);
            _seqPrev = double.NaN;
        }

        /// <summary>Back to the mated pose. The next <see cref="AdvanceTo"/> replays every event up to that time.</summary>
        public void ResetSequence()
        {
            _seqPrev = double.NegativeInfinity;
            _crewYaw = float.NaN;
            ApplyCrew(double.NaN);
            if (_points == null) return;
            foreach (var a in Arms)
                foreach (var p in _points)
                    if (p.Id == a.AttachId) a.Connect(p, UmbilicalRig.Identity);
            foreach (var u in Umbilicals)
                foreach (var p in _points)
                    if (p.Id == u.AttachId) u.Connect(p, UmbilicalRig.Identity);
        }

        /// <summary>Apply every umbilical event in (previous, t] and pose the arms. t is seconds from T-0.</summary>
        public void AdvanceTo(double t)
        {
            if (double.IsNaN(_seqPrev)) _seqPrev = double.NegativeInfinity;
            if (_events != null)
                foreach (var e in _events)
                {
                    if (e.T <= _seqPrev) continue;
                    if (e.T > t) break;
                    foreach (var a in Arms) if (a.AttachId == e.AttachId) a.OnUmbilicalEvent(e);
                    foreach (var u in Umbilicals) if (u.AttachId == e.AttachId) u.OnUmbilicalEvent(e);
                }
            _seqPrev = t;
            Tick(t);
            ApplyCrew(t);
            foreach (var u in Umbilicals) u.Sync();
        }

        /// <summary>Crew-access arm yaw about its tower anchor. 0 until the countdown reaches T−130 s.</summary>
        public float CrewYaw => _crewYaw;

        void ApplyCrew(double missionT)
        {
            if (_crew.Count == 0 || Layout?.Anchors == null) return;
            if (!Layout.Anchors.TryGet(TowerAnchors.CrewArm, out var pivot)) return;
            float yaw = ServiceArmMotion.AccessYaw(missionT);
            if (Mathf.Approximately(yaw, _crewYaw)) return;
            _crewYaw = yaw;
            foreach (var s in _crew)
            {
                var m = s.Rest;
                m.A = ServiceArmMotion.YawAbout(pivot.Position, s.Rest.A, yaw);
                m.B = ServiceArmMotion.YawAbout(pivot.Position, s.Rest.B, yaw);
                _local[(s.Kind, s.Mat)][s.Index] = MemberMatrix(m);
            }
            _builtFor = default;
        }

        void LateUpdate()
        {
            if (Layout == null) return;
            if (!double.IsNaN(_seqPrev)) ApplyCrew(_seqPrev);
            RefreshMatrices();
            foreach (var u in Umbilicals) u.Sync();
            var cam = ViewCamera != null ? ViewCamera : Camera.main; if (cam == null) return;
            int q = Quality; Ledger.Reset(); var p = cam.transform.position;
            foreach (var b in _batches.Values) b.Draw(p, q, Ledger);
            foreach (var a in Arms) a.Draw(p, q, Ledger);
            foreach (var u in Umbilicals) u.Draw(p, q, Ledger);
        }

        public void Clear()
        {
            foreach (var a in Arms) if (a != null) DestroyImmediate(a.gameObject);
            Arms.Clear();
            foreach (var u in Umbilicals) if (u != null) DestroyImmediate(u.gameObject);
            Umbilicals.Clear(); _crew.Clear(); _events = null; _points = null; _seqPrev = double.NaN; _crewYaw = 0f;
            _batches.Clear(); _local.Clear();
            foreach (var m in _meshes) if (m != null) DestroyImmediate(m);
            _meshes.Clear(); Layout = null;
        }

        void OnDestroy() => Clear();
    }
}
