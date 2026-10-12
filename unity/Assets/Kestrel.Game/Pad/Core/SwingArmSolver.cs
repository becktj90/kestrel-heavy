// Swing-arm reach + motion (engine-free). An arm hinges on a vertical axis at the tower's vehicle-side face
// and yaws in the horizontal plane. Connect aims it at the attach plate; retract swings it away toward the tower.
using System;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    [Serializable]
    public sealed class SwingArmSpec
    {
        public string AttachId; public UmbilicalType Type;
        public Vec3 Pivot;                 // pad space, on the tower face
        public float MinLength = 3f, MaxLength = 16f;
        public float MaxVerticalOffset = 1.5f;   // plate may sit this far above/below the arm deck
        public float RetractYawDeg = 95f;        // swing from mated yaw toward the tower side
        public float RetractSeconds = 8f;        // matches UmbilicalTimeline (SwingArm: ArmRetractDone = d + 8)
        public float Width = 1.6f, Depth = 1.8f; // truss box section
    }

    public struct ArmSolution { public bool Ok; public float YawDeg, Length; public string Reason; }

    public static class SwingArmSolver
    {
        /// <summary>Yaw is degrees about +Y from +X toward -Z... expressed as Unity yaw: arm points along
        /// (sin yaw, 0, cos yaw). Toward the vehicle from a tower on +Z is yaw 180.</summary>
        public static ArmSolution Solve(SwingArmSpec s, Vec3 target)
        {
            float dx = target.X - s.Pivot.X, dz = target.Z - s.Pivot.Z, dy = target.Y - s.Pivot.Y;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (Math.Abs(dy) > s.MaxVerticalOffset) return new ArmSolution { Reason = $"vertical offset {dy:0.00} m > {s.MaxVerticalOffset}" };
            if (len > s.MaxLength) return new ArmSolution { Reason = $"reach {len:0.00} m > {s.MaxLength}" };
            if (len < s.MinLength) return new ArmSolution { Reason = $"reach {len:0.00} m < {s.MinLength}" };
            return new ArmSolution { Ok = true, Length = len, YawDeg = (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI) };
        }

        /// <summary>Retract progress 0..1 (smoothstep) given seconds since ArmRetractStart.</summary>
        public static float RetractProgress(double since, float seconds)
        {
            if (since <= 0) return 0; if (since >= seconds) return 1;
            float u = (float)(since / seconds); return u * u * (3 - 2 * u);
        }

        /// <summary>Yaw for a state. Retracted arms always sit fully swung; Released arms animate; else mated yaw.</summary>
        public static float YawFor(SwingArmSpec s, float matedYaw, UmbilicalState st, double sinceRetract) => st switch
        {
            UmbilicalState.Retracted => matedYaw + s.RetractYawDeg,
            UmbilicalState.Released => matedYaw + s.RetractYawDeg * RetractProgress(sinceRetract, s.RetractSeconds),
            UmbilicalState.Detached => matedYaw + s.RetractYawDeg,
            _ => matedYaw
        };

        /// <summary>Pivot for an attach point: on the tower's vehicle-side face, offset sideways to the plate's X, same height.</summary>
        public static Vec3 PivotFor(TowerParams p, Vec3 targetPad)
        {
            float h = p.Width * 0.5f - 0.5f; float x = Math.Max(-h, Math.Min(h, targetPad.X));
            return new Vec3(x, targetPad.Y, p.NearFaceZ);
        }
    }
}
