// Per-frame flight presentation. The ascent integrator is not in this file: samples go in,
// camera lead, shake, FOV, airframe lag, HUD risk and a debris cloud come out.
using System;

namespace Kestrel.Core.Feel
{
    public struct FlightSample
    {
        public bool Running, Lost, HasAttitude;
        public double Altitude, Speed, Mach, DynPressure, ThrustFraction, Downrange;
        public double AttitudeDeg, VehicleHeight, QLimitPa, DvMargin, Health01, Fuel01;
        public int Stage;
    }

    public sealed class FlightPresenter
    {
        /// <summary>Same arm and drop as EngineMix, so the camera punch lands with the max-q sting.</summary>
        public const double MaxQArmPa = 15000;
        public const double MaxQDrop = 0.98;
        public const double PitchRate = 4.2;
        public const double PitchRateReduced = 200;

        public ShakeState Shake;
        public DebrisField Debris;
        public double Punch, Flash, Pitch = double.NaN;
        public double LeadX, LeadY, ShakeX, ShakeY, FovScale = 1;
        public double QPeak;
        public int Stage = -1;
        public int HapticMs;
        public bool HapticHard;
        public bool Began, Lost, QCue;
        public string Band = "nominal";
        public string Call = "NOMINAL";
        public string Line = "";

        public FlightPresenter(int debrisCapacity)
        {
            Debris = new DebrisField(debrisCapacity > 0 ? debrisCapacity : 1, 2.5);
        }

        public void Reset()
        {
            Shake = default;
            Punch = Flash = LeadX = LeadY = ShakeX = ShakeY = QPeak = 0;
            FovScale = 1;
            Pitch = double.NaN;
            Stage = -1;
            HapticMs = 0;
            HapticHard = false;
            Began = Lost = QCue = false;
            Band = "nominal";
            Call = "NOMINAL";
            Line = "";
            Debris.Clear();
        }

        public void Step(in FlightSample s, double dt, bool reduced)
        {
            HapticMs = 0;
            HapticHard = false;
            if (double.IsNaN(dt) || dt < 0) dt = 0;
            if (dt > 0.1) dt = 0.1;

            if (s.Running && !Began)
            {
                Began = true;
                Stage = s.Stage;
                Cue("liftoff", reduced);
            }
            else if (s.Running && s.Stage != Stage)
            {
                Stage = s.Stage;
                Cue("stage", reduced);
            }

            if (s.DynPressure > QPeak) QPeak = s.DynPressure;
            if (!QCue && QPeak >= MaxQArmPa && s.DynPressure < QPeak * MaxQDrop)
            {
                QCue = true;
                Cue("maxq", reduced);
            }

            if (s.Lost && !Lost)
            {
                Lost = true;
                Cue("breakup", reduced);
                double rad = s.AttitudeDeg * Math.PI / 180.0;
                double vx = s.Speed * Math.Cos(rad);
                double vy = s.Speed * Math.Sin(rad);
                Debris.Burst(s.Downrange, s.Altitude, vx, vy, reduced ? 8 : 28, 180, 0.04, 0x5EED);
            }

            if (Debris.Live > 0) Debris.Step(dt, 9.81, 0.35);

            if (s.HasAttitude)
            {
                double rate = reduced ? PitchRateReduced : PitchRate;
                Pitch = CameraRig.StepPitch(Pitch, s.AttitudeDeg, rate, dt);
            }

            double aim = double.IsNaN(Pitch) ? s.AttitudeDeg : Pitch;
            double ar = aim * Math.PI / 180.0;
            if (reduced || s.Speed < 1)
            {
                LeadX = 0;
                LeadY = 0;
            }
            else
            {
                CameraRig.Lead(s.Speed * Math.Cos(ar), s.Speed * Math.Sin(ar), s.VehicleHeight, out LeadX, out LeadY);
            }

            Punch = CameraRig.DecayPunch(Math.Max(0, Punch), dt);
            if (!reduced) CameraRig.StepShake(ref Shake, dt);
            else Shake = default;
            double h = s.VehicleHeight > 1 ? s.VehicleHeight : 1;
            ShakeX = Shake.X * h * CameraRig.ShakeScale;
            ShakeY = Shake.Y * h * CameraRig.ShakeScale;

            double qNorm = s.QLimitPa > 1 ? s.DynPressure / s.QLimitPa : 0;
            if (qNorm < 0) qNorm = 0;
            FovScale = reduced ? 1 : CameraRig.FovScale(s.Mach, qNorm, Punch);

            if (reduced) Flash = 0;
            else
            {
                Flash *= Math.Exp(-4 * dt);
                if (Flash < 1e-3) Flash = 0;
            }

            double health = s.Lost ? 0 : s.Health01;
            var risk = HudRisk.Evaluate(health, s.ThrustFraction, s.Fuel01, s.DynPressure, s.QLimitPa, s.DvMargin);
            Band = risk.Band;
            Call = risk.Call;
            Line = risk.Line;
        }

        void Cue(string name, bool reduced)
        {
            var c = FlightCues.For(name, reduced);
            if (!reduced) CameraRig.Kick(ref Shake, c.Shake * 16, CueAngle(name));
            if (c.Punch > Punch) Punch = c.Punch;
            if (c.Flash > Flash) Flash = c.Flash;
            HapticMs = (int)c.HapticMs;
            HapticHard = c.Hard;
        }

        static double CueAngle(string name)
        {
            switch (name)
            {
                case "liftoff": return 1.15;
                case "maxq": return 0.35;
                case "stage": return 2.2;
                case "breakup": return 0.8;
                default: return 0.6;
            }
        }
    }
}
