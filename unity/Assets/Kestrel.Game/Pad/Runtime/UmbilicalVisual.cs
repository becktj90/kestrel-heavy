// Visible umbilical: sagging hose bundle, two quick-disconnect plates, and a mast/hinge when the swing-arm
// truss is not already drawing the arm. Procedural meshes only (kit tube + a unit box plate).
using System.Collections.Generic;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using UnityEngine;

namespace Kestrel.Pad
{
    public sealed class UmbilicalVisual : MonoBehaviour, IUmbilicalConnector
    {
        public ServiceLink Link { get; private set; }
        public SwingArm Arm { get; private set; }
        public string AttachId => Link?.AttachId;
        public UmbilicalType Type => Link != null ? Link.Type : default;
        public UmbilicalState State { get; private set; } = UmbilicalState.Detached;
        public float Retract { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float Gap { get; private set; }
        /// <summary>Swing-arm trusses already have a connector. This visual is the connector for every other style.</summary>
        public bool IsConnector => Arm == null;

        readonly List<Mesh> _hoses = new List<Mesh>();
        readonly List<InstanceBatch> _hoseBatches = new List<InstanceBatch>();
        InstanceBatch _mast, _plates;
        readonly List<Matrix4x4> _mastLocal = new List<Matrix4x4>();
        readonly List<Matrix4x4> _plateLocal = new List<Matrix4x4>();
        double _missionT = double.NaN, _retractAt = double.NaN, _discAt = double.NaN;
        float _retractRealtime, _discRealtime, _builtKey = float.NaN;
        Matrix4x4 _builtFor;

        /// <summary>The cull class of everything an umbilical draws: the tower members' own (PadRenderer).</summary>
        public const DetailSize PadCull = DetailSize.Large;

        public void Init(ServiceLink link, SwingArm arm, Mesh beam)
        {
            Link = link; Arm = arm;
            // #332: hoses, masts and plates cull with the tower they hang from (PadRenderer's members are Large). At Small
            // the hoses went at 200-500 m while the rail stayed, and at Medium the mast before the hoses' anchor.
            _mast = new InstanceBatch(beam, MaterialLibrary.Get(MaterialRole.SafetyYellowSteel), PadCull);
            _plates = new InstanceBatch(PlateMesh(), MaterialLibrary.Get(MaterialRole.Galvanized), PadCull);
            State = UmbilicalState.Detached;
        }

        public bool Connect(AttachPoint target, LocalPose vehicleToPad)
        {
            if (Link == null || target == null || target.Id != Link.AttachId) return false;
            var plate = PadGenerator.ToPad(vehicleToPad, target.Pose.Position);
            var outward = ContractMath.Rotate(ContractMath.Mul(vehicleToPad.Rotation, target.Pose.Rotation), new Vec3(0, 0, 1));
            float dx = plate.X - Link.Anchor.X, dz = plate.Z - Link.Anchor.Z;
            float reach = Mathf.Sqrt(dx * dx + dz * dz);
            if (Link.AnchorId != null && reach > 40f) return false;
            UmbilicalRig.Retarget(Link, plate, outward);
            State = UmbilicalStates.Apply(UmbilicalState.Detached, UmbilicalEventKind.Connect);
            _retractAt = _discAt = double.NaN; _builtKey = float.NaN;
            Sync();
            return true;
        }

        public void OnUmbilicalEvent(in UmbilicalEvent e)
        {
            if (Link == null || e.AttachId != Link.AttachId) return;
            State = UmbilicalStates.Apply(State, e.Kind);
            if (e.Kind == UmbilicalEventKind.Disconnect && State == UmbilicalState.Released) { _discAt = e.T; _discRealtime = Time.time; }
            if (e.Kind == UmbilicalEventKind.ArmRetractStart && State == UmbilicalState.Released) { _retractAt = e.T; _retractRealtime = Time.time; }
            if (e.Kind == UmbilicalEventKind.Abort && State == UmbilicalState.Connected) _retractAt = _discAt = double.NaN;
            _builtKey = float.NaN;
        }

        public void SetMissionTime(double t) => _missionT = t;

        double Since(double eventT, float realtime) =>
            double.IsNaN(eventT) ? 0 : double.IsNaN(_missionT) ? Time.time - realtime : _missionT - eventT;

        public void Sync()
        {
            if (Link == null) return;
            var state = Arm != null ? Arm.State : State;
            double sinceR = Since(_retractAt, _retractRealtime), sinceD = Since(_discAt, _discRealtime);
            var pose = ServiceArmMotion.At(Link, state, sinceR, sinceD);
            float yaw = Arm != null ? Arm.CurrentYaw : pose.YawDeg;
            if (Arm != null && Link.Rigid == ServiceRigid.Swing)
            {
                pose.Head = ServiceArmMotion.Tip(Link.Anchor, yaw, Arm.Length);
                pose.HoseTower = ContractMath.Lerp(Link.Anchor, pose.Head, 0.62f);
                pose.HoseVehicle = ContractMath.Lerp(Link.Plate, pose.Head, pose.Retract > 0.15f ? pose.Retract : 0.15f);
                if (pose.Retract > 0f)
                    pose.PadPlate = ContractMath.Lerp(QuickDisconnect.PadPlate(Link.Plate, Link.Outward, pose.Gap), pose.Head, pose.Retract);
            }
            Retract = pose.Retract; Yaw = yaw; Pitch = pose.PitchDeg; Gap = pose.Gap;
            float key = Mathf.Round(Retract * 40f) + Mathf.Round(yaw * 2f) * 0.01f + Mathf.Round(Pitch * 2f);
            var world = transform.localToWorldMatrix;
            if (key == _builtKey && world == _builtFor) return;
            if (key == _builtKey) { _builtFor = world; Rebind(world); return; }
            _builtKey = key; _builtFor = world;
            Rebuild(pose, world);
        }

        void Rebind(Matrix4x4 world)
        {
            for (int i = 0; i < _hoseBatches.Count; i++) { _hoseBatches[i].Clear(); _hoseBatches[i].Add(world); }
            _mast.Clear(); foreach (var l in _mastLocal) _mast.Add(world * l);
            _plates.Clear(); foreach (var l in _plateLocal) _plates.Add(world * l);
        }

        void Rebuild(ServicePose pose, Matrix4x4 world)
        {
            foreach (var m in _hoses) if (m != null) DestroyImmediate(m);
            _hoses.Clear(); _hoseBatches.Clear();
            if (Link.Strands != null)
            {
                int samples = 8;
                var pts = new Vec3[samples + 1];
                var path = new Vector3[samples + 1];
                foreach (var s in Link.Strands)
                {
                    HoseBundle.Sample(pose.HoseTower, pose.HoseVehicle, s.Slack, s.Lateral, samples, pts);
                    for (int i = 0; i < path.Length; i++) path[i] = pts[i].ToUnity();
                    var mesh = KitMesh.Tube(path, s.Radius, 4, "UmbilicalHose");
                    _hoses.Add(mesh);
                    var batch = new InstanceBatch(mesh, MaterialLibrary.Get(s.Material), PadCull);
                    batch.Add(world); _hoseBatches.Add(batch);
                }
            }
            _mastLocal.Clear(); _mast.Clear();
            foreach (var mem in Link.Mast)
            {
                var spun = mem;
                spun.A = ContractMath.RotateAbout(Link.Anchor, Link.HingeAxis, mem.A, pose.PitchDeg);
                spun.B = ContractMath.RotateAbout(Link.Anchor, Link.HingeAxis, mem.B, pose.PitchDeg);
                var local = PadRenderer.MemberMatrix(spun);
                _mastLocal.Add(local); _mast.Add(world * local);
            }
            _plateLocal.Clear(); _plates.Clear();
            var skin = PlateMatrix(Link.Plate, Link.Outward, Link.PlateRadius);
            var pad = PlateMatrix(pose.PadPlate, Link.Outward, Link.PlateRadius * 0.92f);
            _plateLocal.Add(skin); _plateLocal.Add(pad);
            _plates.Add(world * skin); _plates.Add(world * pad);
        }

        static Matrix4x4 PlateMatrix(Vec3 center, Vec3 outward, float radius)
        {
            var dir = outward.ToUnity();
            if (dir.sqrMagnitude < 1e-8f) dir = Vector3.back;
            var rot = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            return Matrix4x4.TRS(center.ToUnity(), rot, new Vector3(radius * 2f, QuickDisconnect.Thickness, radius * 2f));
        }

        public int Draw(Vector3 cam, int quality, BudgetLedger ledger)
        {
            if (_plates == null) return 0;
            int n = 0;
            foreach (var b in _hoseBatches) n += b.Draw(cam, quality, ledger);
            n += _mast.Draw(cam, quality, ledger);
            n += _plates.Draw(cam, quality, ledger);
            return n;
        }

        public void Shutdown()
        {
            foreach (var m in _hoses) if (m != null) DestroyImmediate(m);
            _hoses.Clear(); _hoseBatches.Clear();
        }

        void OnDestroy() => Shutdown();

        static Mesh _sharedPlate;
        static Mesh PlateMesh()
        {
            if (_sharedPlate != null) return _sharedPlate;
            _sharedPlate = KitMesh.Beam(1f, 1f);
            _sharedPlate.name = "UmbilicalPlate";
            return _sharedPlate;
        }
    }
}
