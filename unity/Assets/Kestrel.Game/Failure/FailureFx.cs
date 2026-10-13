// Failure visuals (§3). The break uses the airframe and fairing joint rings already on the
// vehicle. A tumble is a real spin (ω from the §3 hold and q), then a break. A leg fold
// uses the LandingLayout hip/knee/foot stations. Bell flash and plume cut are per engine
// index, the same index BellCluster was given.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Vehicle;

namespace Kestrel.Game.Failure
{
    public struct FailureJoint
    {
        public float Y, Radius;
        public string Source;
    }

    public struct LegPose
    {
        public float HipX, HipY, HipZ, KneeX, KneeY, KneeZ, FootX, FootY, FootZ;
        public float FootRadius;
    }

    public sealed class FailureCue
    {
        public float QPa = FailureModel.TumbleQPa;
        public float AllowableN;
        public FailureJoint[] Joints = Array.Empty<FailureJoint>();
        public int EngineCount = 1;
        public int EngineIndex = -1;
        public float Radius = 1.8f;
        public float AftY = 2f;
        public float Sink;
        public float VLeg = 6f;
    }

    public sealed class FailurePlan
    {
        public string Mode = FailureFx.BreakMode;
        public int JointIndex = -1;
        public float JointY;
        public float Gap;
        public float AngleDeg;
        public double SpinRadPerSec;
        public float PlumeScale = 1f;
        public int EngineIndex = -1;
        public bool Flash;
        public string Tint = "warm";
        public float Fold01;
        public float TipDeg;
        public float FootRadius;
        public float ParticleSpeed;
        public int Burst;
        public float Shake;
    }

    public static class FailureFx
    {
        public const string BreakMode = "break";
        public const string TumbleMode = "tumble";
        public const string BellMode = "bell";
        public const string SputterMode = "sputter";
        public const string GlowMode = "glow";
        public const string FrostMode = "frost";
        public const string FlameoutMode = "flameout";
        public const string BumpMode = "bump";
        public const string FoldMode = "fold";
        public const float TipOverDeg = 72f;
        public const float SnapDeg = 90f;

        public static void AddInterstage(List<FailureJoint> list, float y0, float y1, float r0, float r1)
        {
            var poses = AirframeLayout.Interstage(y0, y1, r0, r1);
            for (int i = 0; i < poses.Count; i++)
            {
                if (poses[i].Kind != AirframeLayout.JointRing) continue;
                list.Add(new FailureJoint { Y = poses[i].Y, Radius = poses[i].Radius, Source = "interstage" });
            }
        }

        public static void AddFairing(List<FailureJoint> list, float baseY, float radius, float length, float noseFraction, bool conic)
        {
            var poses = FairingLayout.Build(baseY, radius, length, noseFraction, conic);
            for (int i = 0; i < poses.Count; i++)
            {
                if (poses[i].Kind != FairingLayout.Ring) continue;
                list.Add(new FailureJoint { Y = poses[i].Y, Radius = poses[i].Radius, Source = "fairing" });
                return;
            }
        }

        /// <summary>MS = F_allow / (q · π · R²) − 1. Larger radius is the weaker joint when the allowable is shared.</summary>
        public static double Margin(float allowableN, float qPa, float radius)
        {
            if (radius <= 0.05f) return double.PositiveInfinity;
            double load = qPa * Math.PI * radius * radius;
            if (load <= 1e-6) return double.PositiveInfinity;
            return allowableN / load - 1.0;
        }

        public static int Weakest(FailureJoint[] joints, float qPa, float allowableN)
        {
            if (joints == null || joints.Length == 0) return -1;
            int best = 0;
            double score = Score(joints[0], qPa, allowableN);
            for (int i = 1; i < joints.Length; i++)
            {
                double s = Score(joints[i], qPa, allowableN);
                if (s < score - 1e-9 || (Math.Abs(s - score) <= 1e-9 && joints[i].Y < joints[best].Y))
                {
                    score = s;
                    best = i;
                }
            }
            return best;
        }

        static double Score(FailureJoint j, float qPa, float allowableN)
        {
            if (allowableN <= 0f || qPa <= 0f) return -j.Radius;
            return Margin(allowableN, qPa, j.Radius);
        }

        public static int Highest(FailureJoint[] joints, string sourceOrNull)
        {
            int best = -1;
            float y = float.NegativeInfinity;
            if (joints == null) return -1;
            for (int i = 0; i < joints.Length; i++)
            {
                if (sourceOrNull != null && joints[i].Source != sourceOrNull) continue;
                if (joints[i].Y > y) { y = joints[i].Y; best = i; }
            }
            return best;
        }

        public static int Lowest(FailureJoint[] joints)
        {
            int best = -1;
            float y = float.PositiveInfinity;
            if (joints == null) return -1;
            for (int i = 0; i < joints.Length; i++)
                if (joints[i].Y < y) { y = joints[i].Y; best = i; }
            return best;
        }

        /// <summary>ω = (π / 0.5 s) · clamp(q / 20 kPa, 0.5, 2). Reduce Motion snaps instead of spinning.</summary>
        public static double SpinRadPerSec(double qPa, bool reduceMotion)
        {
            if (reduceMotion) return 0;
            double q = qPa / FailureModel.TumbleQPa;
            if (q < 0.5) q = 0.5;
            if (q > 2) q = 2;
            return Math.PI / FailureModel.TumbleHoldS * q;
        }

        public static float PlumeScale(string mode, int engines)
        {
            if (mode == FlameoutMode || mode == BreakMode || mode == FrostMode) return 0f;
            if (mode == GlowMode || mode == FoldMode) return 1f;
            if (mode == SputterMode) return 0.45f;
            if (mode == TumbleMode || mode == BumpMode) return 0.35f;
            if (engines <= 1) return 0f;
            return (engines - 1f) / engines;
        }

        public static bool BellFlash(string kind, bool reduceMotion)
        {
            if (reduceMotion) return false;
            return kind == "engine-overpressure" || kind == "engine-out";
        }

        /// <summary>2 Hz damped offset, under the 3-per-second flash ceiling. Zero when Reduce Motion is on.</summary>
        public static float ShakeMeters(float age, bool reduceMotion)
        {
            if (reduceMotion || age <= 0f) return 0f;
            return 0.12f * (float)Math.Exp(-3.0 * age) * (float)Math.Sin(age * 2.0 * Math.PI * 2.0);
        }

        public static float ParticleSpeed(bool reduceMotion) => reduceMotion ? 0f : 4f;

        public static int BurstCount(string mode, bool reduceMotion)
        {
            if (mode == FlameoutMode || mode == FoldMode) return 0;
            return reduceMotion ? 12 : 24;
        }

        /// <summary>fold = clamp((sink − v_leg) / v_leg, 0, 1). The foot moves from LandingLayout's deployed station to the hip.</summary>
        public static float FoldAmount(float sink, float vLeg)
        {
            if (!(vLeg > 0f) || sink <= vLeg) return 0f;
            float u = (sink - vLeg) / vLeg;
            return u > 1f ? 1f : u;
        }

        public static void FoldedLegs(float radius, float aftY, float fold01, LegPose[] into)
        {
            if (into == null || into.Length < LandingLayout.Count) return;
            if (fold01 < 0f) fold01 = 0f;
            if (fold01 > 1f) fold01 = 1f;
            var deployed = LandingLayout.Legs(radius, aftY);
            int leg = 0;
            for (int i = 0; i + 2 < deployed.Count && leg < into.Length; i += 3)
            {
                Endpoints(deployed[i], out var hip, out var kneeA);
                Endpoints(deployed[i + 1], out var kneeB, out var foot);
                var knee = kneeA.y > kneeB.y ? kneeA : kneeB;
                float footR = Len2(foot.x, foot.z);
                float hipR = Len2(hip.x, hip.z);
                float shownR = footR + (hipR - footR) * fold01;
                float shownY = foot.y + (knee.y * 0.15f - foot.y) * fold01;
                float ax = footR > 1e-4f ? foot.x / footR : 0.7f;
                float az = footR > 1e-4f ? foot.z / footR : 0.7f;
                into[leg++] = new LegPose
                {
                    HipX = hip.x, HipY = hip.y, HipZ = hip.z,
                    KneeX = knee.x, KneeY = knee.y, KneeZ = knee.z,
                    FootX = ax * shownR, FootY = shownY, FootZ = az * shownR,
                    FootRadius = shownR,
                };
            }
        }

        public static FailurePlan Plan(string kind, string cause, FailureCue cue, float age, bool reduceMotion)
        {
            if (cue == null) cue = new FailureCue();
            if (age < 0f) age = 0f;
            var plan = new FailurePlan
            {
                ParticleSpeed = ParticleSpeed(reduceMotion),
                Shake = ShakeMeters(age, reduceMotion),
                EngineIndex = ClampEngine(cue),
            };
            switch (kind)
            {
                case "overg":
                    plan.Mode = BreakMode;
                    plan.JointIndex = Highest(cue.Joints, null);
                    plan.Tint = "warm";
                    break;
                case "tumble":
                    plan.Mode = TumbleMode;
                    plan.JointIndex = Weakest(cue.Joints, cue.QPa, cue.AllowableN);
                    plan.SpinRadPerSec = SpinRadPerSec(cue.QPa, reduceMotion);
                    plan.AngleDeg = reduceMotion ? SnapDeg : (float)(plan.SpinRadPerSec * age * 180.0 / Math.PI);
                    plan.Tint = "warm";
                    break;
                case "engine-overpressure":
                case "engine-out":
                    plan.Mode = BellMode;
                    plan.Flash = BellFlash(kind, reduceMotion);
                    plan.Tint = plan.Flash ? "white" : "dim";
                    break;
                case "bad-mixture":
                    plan.Mode = SputterMode;
                    plan.Tint = "green";
                    break;
                case "thermal":
                    plan.Mode = GlowMode;
                    plan.JointIndex = cause == "base-heating" ? Lowest(cue.Joints) : Highest(cue.Joints, null);
                    plan.Tint = "warm";
                    break;
                case "cryo-material":
                    plan.Mode = FrostMode;
                    plan.JointY = 0f;
                    plan.Tint = "frost";
                    break;
                case "prop-depletion":
                    plan.Mode = FlameoutMode;
                    plan.Tint = "none";
                    break;
                case "separation-collision":
                    plan.Mode = BumpMode;
                    plan.JointIndex = Highest(cue.Joints, "interstage");
                    if (plan.JointIndex < 0) plan.JointIndex = Highest(cue.Joints, null);
                    plan.SpinRadPerSec = SpinRadPerSec(cue.QPa, reduceMotion);
                    plan.Tint = "warm";
                    break;
                case "leg-collapse":
                    plan.Mode = FoldMode;
                    plan.Fold01 = ShownFold(FoldAmount(cue.Sink, cue.VLeg), age, reduceMotion);
                    plan.TipDeg = TipOverDeg * plan.Fold01;
                    plan.AngleDeg = -plan.TipDeg;
                    plan.Tint = "none";
                    var legs = new LegPose[LandingLayout.Count];
                    FoldedLegs(cue.Radius, cue.AftY, plan.Fold01, legs);
                    plan.FootRadius = legs[0].FootRadius;
                    break;
                default:
                    plan.Mode = BreakMode;
                    plan.JointIndex = cause == "hot-staging" ? Highest(cue.Joints, "interstage")
                        : cause == "common-bulkhead" ? Lowest(cue.Joints)
                        : Weakest(cue.Joints, cue.QPa, cue.AllowableN);
                    plan.Tint = "warm";
                    break;
            }
            if (plan.JointIndex >= 0 && cue.Joints != null && plan.JointIndex < cue.Joints.Length)
                plan.JointY = cue.Joints[plan.JointIndex].Y;
            plan.Gap = Gap(plan.Mode, age, reduceMotion);
            plan.PlumeScale = PlumeScale(plan.Mode, cue.EngineCount);
            plan.Burst = BurstCount(plan.Mode, reduceMotion);
            if (plan.Mode == BumpMode)
                plan.AngleDeg = reduceMotion ? 40f : (age < 0.2f ? 0f : (float)(SpinRadPerSec(cue.QPa, false) * (age - 0.2f) * 180.0 / Math.PI));
            if (plan.Mode == TumbleMode && !reduceMotion && age < FailureModel.TumbleHoldS)
                plan.Gap = 0f;
            return plan;
        }

        public static float Gap(string mode, float age, bool reduceMotion)
        {
            if (mode == BreakMode) return reduceMotion ? 6f : Math.Min(age * 8f, 10f);
            if (mode == TumbleMode)
            {
                if (reduceMotion) return 6f;
                if (age < FailureModel.TumbleHoldS) return 0f;
                return Math.Min((age - (float)FailureModel.TumbleHoldS) * 8f, 10f);
            }
            if (mode == BumpMode)
            {
                if (reduceMotion) return 4f;
                if (age < 0.2f) return -age * 4f;
                return Math.Min((age - 0.2f) * 8f, 8f);
            }
            return 0f;
        }

        static float ShownFold(float fold01, float age, bool reduceMotion)
        {
            if (reduceMotion) return fold01;
            float u = age / 0.35f;
            if (u > 1f) u = 1f;
            return fold01 * u;
        }

        static int ClampEngine(FailureCue cue)
        {
            if (cue.EngineCount <= 0) return -1;
            if (cue.EngineIndex < 0 || cue.EngineIndex >= cue.EngineCount) return 0;
            return cue.EngineIndex;
        }

        struct V3 { public float x, y, z; }

        static void Endpoints(StrutPose p, out V3 a, out V3 b)
        {
            float hx = p.AxisX * p.Length * 0.5f, hy = p.AxisY * p.Length * 0.5f, hz = p.AxisZ * p.Length * 0.5f;
            a = new V3 { x = p.X - hx, y = p.Y - hy, z = p.Z - hz };
            b = new V3 { x = p.X + hx, y = p.Y + hy, z = p.Z + hz };
        }

        static float Len2(float x, float z) => (float)Math.Sqrt(x * x + z * z);
    }
}
