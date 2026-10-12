// Pad-side umbilical connector (contract: Kestrel.Kit.Contracts.IUmbilicalConnector). One arm per attach point.
// The GameObject sits at the pivot (pad space) and yaws about +Y; the truss is instanced from arm-local members.
using System.Collections.Generic;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using UnityEngine;

namespace Kestrel.Pad
{
    public sealed class SwingArm : MonoBehaviour, IUmbilicalConnector
    {
        public SwingArmSpec Spec { get; private set; }
        public string AttachId => Spec?.AttachId;
        public UmbilicalType Type => Spec != null ? Spec.Type : default;
        public UmbilicalState State { get; private set; } = UmbilicalState.Detached;
        public float MatedYaw { get; private set; } = 180f;   // toward the vehicle from a tower on +Z
        public float Length { get; private set; }
        public string LastError { get; private set; }

        InstanceBatch _batch; readonly List<Matrix4x4> _local = new List<Matrix4x4>();
        double _retractStartT = double.NaN, _missionT = double.NaN; float _retractStartRealtime;
        Matrix4x4 _builtFor;

        public void Init(SwingArmSpec spec, Mesh member)
        {
            Spec = spec; transform.localPosition = spec.Pivot.ToUnity();
            _batch = new InstanceBatch(member, MaterialLibrary.Get(MaterialRole.SafetyYellowSteel), DetailSize.Medium);
            SetLength(Mathf.Clamp(spec.MaxLength * 0.6f, spec.MinLength, spec.MaxLength));
            ApplyYaw();
        }

        void SetLength(float len)
        {
            Length = len; _local.Clear(); _builtFor = default;
            foreach (var m in SwingArmTruss.Build(Spec.AttachId, len, Spec.Width, Spec.Depth)) _local.Add(PadRenderer.MemberMatrix(m));
        }

        public bool Connect(AttachPoint target, LocalPose vehicleToPad)
        {
            if (Spec == null || target == null || target.Id != Spec.AttachId) { LastError = "attach id mismatch"; return false; }
            var padPos = PadGenerator.ToPad(vehicleToPad, target.Pose.Position);
            // Pad-space plate position relative to this arm's parent (the pad root).
            var sol = SwingArmSolver.Solve(Spec, padPos);
            if (!sol.Ok) { LastError = sol.Reason; return false; }
            MatedYaw = sol.YawDeg; SetLength(SwingArmSolver.MatedLength(Spec, sol));
            State = UmbilicalStates.Apply(State, UmbilicalEventKind.Connect); _retractStartT = double.NaN; ApplyYaw();
            return true;
        }

        public void OnUmbilicalEvent(in UmbilicalEvent e)
        {
            if (Spec == null || e.AttachId != Spec.AttachId) return;
            State = UmbilicalStates.Apply(State, e.Kind);
            if (e.Kind == UmbilicalEventKind.ArmRetractStart && State == UmbilicalState.Released) { _retractStartT = e.T; _retractStartRealtime = Time.time; }
            if (e.Kind == UmbilicalEventKind.Abort) _retractStartT = double.NaN;
            ApplyYaw();
        }

        public void Tick(double missionT) { _missionT = missionT; ApplyYaw(); }

        double SinceRetract()
        {
            if (double.IsNaN(_retractStartT)) return 0;
            return double.IsNaN(_missionT) ? Time.time - _retractStartRealtime : _missionT - _retractStartT;
        }

        public float CurrentYaw => SwingArmSolver.YawFor(Spec, MatedYaw, State, SinceRetract());

        void ApplyYaw() { if (Spec != null) transform.localRotation = Quaternion.Euler(0, CurrentYaw, 0); }

        void Update() { if (State == UmbilicalState.Released) ApplyYaw(); }

        public int Draw(Vector3 cam, int q, BudgetLedger ledger)
        {
            if (_batch == null) return 0;
            var w = transform.localToWorldMatrix;
            if (w != _builtFor) { _builtFor = w; _batch.Clear(); foreach (var l in _local) _batch.Add(w * l); }
            return _batch.Draw(cam, q, ledger);
        }

        public int MemberCount => _local.Count;
    }
}
