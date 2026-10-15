// Chase-camera feel that sits beside ChaseCamera. The horizon hold and the floating-origin offset
// stay as they are. This adds look-ahead along the velocity, a spring shake, and a small FOV
// breathe. All of it is metres and degrees, with no engine types.
using System;

namespace Kestrel.Core.Feel
{
    public struct ShakeState
    {
        public double X, Y, Vx, Vy;
    }

    public static class CameraRig
    {
        public const double Omega = 28;
        public const double Zeta = 0.72;
        /// <summary>Look-ahead as a fraction of vehicle height, reached near 180 m/s and held after that.</summary>
        public const double LeadFraction = 0.42;
        /// <summary>Vertical look-ahead is quieter so the horizon hold does not get shoved up the frame.</summary>
        public const double LeadVertical = 0.35;
        public const double FovMach = 0.04;
        public const double FovQ = 0.035;
        public const double PunchFov = 0.04;
        /// <summary>Spring displacement times vehicle height. 1 on the spring is this fraction of the rocket.</summary>
        public const double ShakeScale = 0.12;

        /// <summary>Offset, in metres, ahead of the vehicle along its ground velocity.</summary>
        public static void Lead(double vx, double vy, double height, out double x, out double y)
        {
            double h = height > 1 ? height : 1;
            double sp = Math.Sqrt(vx * vx + vy * vy);
            double reach = h * LeadFraction;
            if (sp < 1e-3) { x = 0; y = 0; return; }
            double scale = reach * (1 - Math.Exp(-sp / 180.0)) / sp;
            x = vx * scale;
            y = vy * scale * LeadVertical;
        }

        /// <summary>1 at rest. Widens a little with Mach and dynamic pressure, tightens while a punch is in.</summary>
        public static double FovScale(double mach, double qNorm, double punch)
        {
            if (double.IsNaN(mach) || mach < 0) mach = 0;
            if (double.IsNaN(qNorm) || qNorm < 0) qNorm = 0;
            if (qNorm > 1.5) qNorm = 1.5;
            if (double.IsNaN(punch) || punch < 0) punch = 0;
            if (punch > 1) punch = 1;
            double widen = 1 + FovMach * (1 - Math.Exp(-mach)) + FovQ * qNorm;
            return widen * (1 - PunchFov * punch);
        }

        public static double DecayPunch(double punch, double dt)
        {
            if (double.IsNaN(punch) || punch < 0) return 0;
            if (double.IsNaN(dt) || dt < 0) dt = 0;
            return punch * Math.Exp(-3.2 * dt);
        }

        /// <summary>Impulse into the shake spring. <paramref name="amp"/> is a velocity kick, not metres.</summary>
        public static void Kick(ref ShakeState s, double amp, double angleRad)
        {
            if (double.IsNaN(amp) || amp < 0) amp = 0;
            if (double.IsNaN(angleRad)) angleRad = 0;
            s.Vx += Math.Cos(angleRad) * amp;
            s.Vy += Math.Sin(angleRad) * amp;
        }

        public static void StepShake(ref ShakeState s, double dt)
        {
            if (double.IsNaN(dt) || dt <= 0) return;
            if (dt > 0.1) dt = 0.1;
            int n = 1;
            double h = dt;
            if (h > 1.0 / 120.0)
            {
                n = (int)Math.Ceiling(h * 120.0);
                if (n > 8) n = 8;
                h = dt / n;
            }
            for (int i = 0; i < n; i++) StepOnce(ref s, h);
            if (s.X * s.X + s.Y * s.Y < 1e-10 && s.Vx * s.Vx + s.Vy * s.Vy < 1e-8)
                s.X = s.Y = s.Vx = s.Vy = 0;
        }

        static void StepOnce(ref ShakeState s, double dt)
        {
            double w = Omega, z = Zeta;
            double ax = -2 * z * w * s.Vx - w * w * s.X;
            double ay = -2 * z * w * s.Vy - w * w * s.Y;
            s.Vx += ax * dt;
            s.Vy += ay * dt;
            s.X += s.Vx * dt;
            s.Y += s.Vy * dt;
        }

        /// <summary>Rotational inertia of the drawn airframe, degrees. The sim pitch is unchanged.</summary>
        public static double StepPitch(double currentDeg, double targetDeg, double rate, double dt)
        {
            if (double.IsNaN(currentDeg)) return targetDeg;
            if (double.IsNaN(targetDeg)) return currentDeg;
            if (double.IsNaN(dt) || dt < 0) dt = 0;
            if (dt > 0.1) dt = 0.1;
            if (double.IsNaN(rate) || rate < 0) rate = 0;
            return currentDeg + (targetDeg - currentDeg) * (1 - Math.Exp(-rate * dt));
        }
    }
}
