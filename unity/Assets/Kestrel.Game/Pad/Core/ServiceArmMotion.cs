// Retraction clocks for service arms. Times come from UmbilicalTimeline (contract), not from tower geometry.
// Swing arms and the crew-access arm leave before T-0. Tail masts drop at liftoff. Lanyards pull at first motion.
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public struct ServicePose
    {
        public UmbilicalState State;
        public float Retract;     // 0 mated, 1 fully away
        public float YawDeg;
        public float PitchDeg;
        public float Gap;
        public Vec3 Head;         // connector end (arm tip or mast head)
        public Vec3 PadPlate;
        public Vec3 HoseTower;
        public Vec3 HoseVehicle;
    }

    public static class ServiceArmMotion
    {
        /// <summary>Crew/access arm starts back at the mission-timeline pad.arms call (T−130 s) and is clear in 8 s.</summary>
        public const double AccessRetractT = -130.0;
        public const float AccessRetractSeconds = 8f;
        public const float AccessRetractYaw = 95f;

        public static float RetractSeconds(ReleaseStyle r) => r == ReleaseStyle.SwingArmRetract ? 8f : 1.5f;

        /// <summary>Fold every event for <paramref name="id"/> with time &lt;= <paramref name="t"/> through the shared state table.</summary>
        public static void Sample(IReadOnlyList<UmbilicalEvent> events, string id, double t,
            out UmbilicalState state, out double sinceRetract, out double sinceDisconnect)
        {
            state = UmbilicalState.Detached;
            double retractAt = double.NaN, discAt = double.NaN;
            if (events != null)
                foreach (var e in events)
                {
                    if (e.T > t) break;                 // Build() sorts by time
                    if (e.AttachId != id) continue;
                    state = UmbilicalStates.Apply(state, e.Kind);
                    if (e.Kind == UmbilicalEventKind.Disconnect && state == UmbilicalState.Released) discAt = e.T;
                    if (e.Kind == UmbilicalEventKind.ArmRetractStart) retractAt = e.T;
                    if (e.Kind == UmbilicalEventKind.Abort && state == UmbilicalState.Connected) { retractAt = double.NaN; discAt = double.NaN; }
                }
            bool moving = state == UmbilicalState.Released || state == UmbilicalState.Retracted;
            sinceRetract = !moving || double.IsNaN(retractAt) ? 0 : t - retractAt;
            sinceDisconnect = !moving || double.IsNaN(discAt) ? 0 : t - discAt;
        }

        public static ServicePose At(ServiceLink link, UmbilicalState state, double sinceRetract, double sinceDisconnect)
        {
            float seconds = RetractSeconds(link.Release);
            float u = state == UmbilicalState.Retracted ? 1f
                : state == UmbilicalState.Released ? SwingArmSolver.RetractProgress(sinceRetract, seconds) : 0f;
            var swingState = state == UmbilicalState.Flowing ? UmbilicalState.Connected : state;
            float yaw = link.Rigid == ServiceRigid.Swing
                ? SwingArmSolver.YawFor(new SwingArmSpec { RetractYawDeg = link.RetractYawDeg, RetractSeconds = seconds }, link.MatedYaw, swingState, sinceRetract)
                : link.MatedYaw;
            float pitch = link.Rigid == ServiceRigid.Mast || link.Rigid == ServiceRigid.Hinge ? u * link.DropDeg : 0f;
            Vec3 head = link.Rigid == ServiceRigid.Swing ? Tip(link.Anchor, yaw, link.Length)
                : pitch == 0f ? link.MastHead
                : ContractMath.RotateAbout(link.Anchor, link.HingeAxis, link.MastHead, pitch);
            float gap = QuickDisconnect.Separation(link.PlateRadius, state, sinceDisconnect);
            var padPlate = QuickDisconnect.PadPlate(link.Plate, link.Outward, gap);
            if (u > 0f) padPlate = ContractMath.Lerp(padPlate, head, u);

            Vec3 hoseT, hoseV;
            if (link.Rigid == ServiceRigid.Swing)
            {
                hoseT = ContractMath.Lerp(link.Anchor, head, 0.62f);
                hoseV = ContractMath.Lerp(link.Plate, head, u > 0.15f ? u : 0.15f);
            }
            else if (link.Rigid == ServiceRigid.None)
            {
                hoseT = link.Anchor;
                var dropped = ContractMath.Lerp(link.Plate, link.Anchor, u * 0.75f);
                dropped.Y -= u * 0.8f;
                hoseV = u > 0f ? dropped : link.Plate;
            }
            else
            {
                hoseT = ContractMath.Lerp(link.Anchor, head, 0.15f);
                hoseV = ContractMath.Lerp(link.Plate, head, u);
            }
            return new ServicePose
            {
                State = state, Retract = u, YawDeg = yaw, PitchDeg = pitch, Gap = gap,
                Head = head, PadPlate = padPlate, HoseTower = hoseT, HoseVehicle = hoseV,
            };
        }

        public static Vec3 Tip(Vec3 anchor, float yawDeg, float length)
        {
            double a = yawDeg * System.Math.PI / 180.0;
            return new Vec3(anchor.X + (float)System.Math.Sin(a) * length, anchor.Y, anchor.Z + (float)System.Math.Cos(a) * length);
        }

        /// <summary>0 before T−130, then a smoothstep swing. NaN (sequence not running) stays mated.</summary>
        public static float AccessYaw(double missionT)
        {
            if (double.IsNaN(missionT)) return 0f;
            return AccessRetractYaw * SwingArmSolver.RetractProgress(missionT - AccessRetractT, AccessRetractSeconds);
        }

        /// <summary>Yaw a point about +Y through <paramref name="pivot"/>. Positive yaw matches Unity Euler Y.</summary>
        public static Vec3 YawAbout(Vec3 pivot, Vec3 p, float yawDeg)
        {
            double a = yawDeg * System.Math.PI / 180.0;
            float c = (float)System.Math.Cos(a), s = (float)System.Math.Sin(a);
            float x = p.X - pivot.X, z = p.Z - pivot.Z;
            return new Vec3(pivot.X + c * x + s * z, p.Y, pivot.Z - s * x + c * z);
        }
    }
}
