// Headless 2-D (equatorial-plane, inertial, Earth-centred) ascent simulator.
// State y = [x, y, vx, vy, m] (m, m/s, kg). Fixed-step RK4, no per-step allocations.
// Faithful port of web/js/physics/headless.js (same guidance, same step-size rule, same events).
// AscentSim exposes the loop one iteration at a time (Tick) so a render loop can drive it;
// Headless.Simulate runs it to completion exactly like the JS simulate().
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kestrel.Core.Physics
{
    public sealed class SimStage
    {
        public double thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, cd = 0.3;
        // derived by Normalize
        public double mdot, area; public string warn;
        public SimStage Clone() => (SimStage)MemberwiseClone();
    }

    public sealed class SimOptions
    {
        public double payload = 0, targetAlt = 200e3, latitude = 28.5, dt = 0.05, tMax = 3000;
        public double kickSpeed = 60, kickAngleDeg = 4, sampleEvery = 0;
        public bool vacuum = false, rotate = true, cutoff = true;
    }

    public sealed class SimEvent
    {
        public double t; public string type; public int? stage; public double? alt, v, apoAlt, periAlt;
        public override string ToString() => $"T+{t:F1} {type}{(stage.HasValue ? " S" + stage : "")}";
    }

    public struct SimSample { public double t, alt, v, vrel, q, m, pitch; public int stage, mode; }

    public sealed class SimResult
    {
        public string outcome; public double time; public List<SimEvent> events; public OrbitElements orbit;
        public double maxQ, maxQTime, maxQAlt, maxG, lossGravity, lossDrag, lossSteering, idealDeltaV, deltaVExpended, finalMass;
        public double x, y, vx, vy, m;
        public List<SimSample> samples; public List<string> warnings;
        /// <summary>First integrator step with geometric altitude &gt;= 100 km. Observational: it does not change the step or the event list.</summary>
        public double? KarmanTime;
    }

    public static class Headless
    {
        public static SimStage[] Normalize(IList<SimStage> stages)
        {
            var o = new SimStage[stages.Count];
            for (int i = 0; i < o.Length; i++)
            {
                var s = stages[i].Clone();
                s.mdot = RocketEq.MassFlow(s.thrustVac, s.ispVac);
                double impliedSL = s.mdot * Atmosphere.G0 * s.ispSL;
                s.warn = s.thrustSL > 0 && Math.Abs(impliedSL - s.thrustSL) / s.thrustSL > 0.05
                    ? $"stage {i + 1}: thrustSL {s.thrustSL.ToString(CultureInfo.InvariantCulture)} N inconsistent with Isp ratio (implied {Math.Round(impliedSL).ToString(CultureInfo.InvariantCulture)} N)" : null;
                s.area = Aero.RefArea(s.diameter);
                o[i] = s;
            }
            return o;
        }

        /// <summary>Ideal (vacuum) Δv per stage and total for the stack plus payload.</summary>
        public static (double[] perStage, double total) StackDeltaV(IList<SimStage> stages, double payload = 0)
        {
            var st = Normalize(stages);
            var per = new double[st.Length]; double total = 0;
            for (int i = 0; i < st.Length; i++)
            {
                double above = payload;
                for (int j = i + 1; j < st.Length; j++) above += st[j].propMass + st[j].dryMass;
                double m0 = above + st[i].propMass + st[i].dryMass;
                per[i] = RocketEq.DeltaV(st[i].ispVac, m0, m0 - st[i].propMass);
            }
            foreach (var d in per) total += d; // JS reduce((a,b)=>a+b, 0): same left-to-right order
            return (per, total);
        }

        public static SimResult Simulate(IList<SimStage> stages, SimOptions opts = null)
        {
            var sim = new AscentSim(stages, opts ?? new SimOptions());
            while (sim.Tick()) { }
            return sim.Finish();
        }

        /// <summary>Generic two-stage medium-lift archetype (kerosene/LOX, ~550 t GLOW).</summary>
        public static SimStage[] MediumLifter() => new[]
        {
            new SimStage { thrustSL = 7.6e6, thrustVac = 8.23e6, ispSL = 282, ispVac = 311, propMass = 411000, dryMass = 25600, diameter = 3.7, cd = 0.3 },
            new SimStage { thrustSL = 0, thrustVac = 981000, ispSL = 200, ispVac = 348, propMass = 107500, dryMass = 4500, diameter = 3.7, cd = 0.3 },
        };
    }

    /// <summary>The simulate() loop as a resumable object. One Tick == one JS while-iteration.</summary>
    public sealed class AscentSim
    {
        readonly IList<SimStage> stagesIn; readonly SimStage[] st; readonly SimOptions o;
        readonly double[] upperMass; readonly double vSite, omegaAir, kickAngle;
        readonly RK4 rk = new RK4(5); readonly Deriv deriv;
        public readonly double[] Y = new double[5];
        public int StageIdx { get; private set; }
        public int Mode { get; private set; }            // 0 vertical, 1 kick, 2 gravity turn, 3 closed-loop, 4 coast
        public double PitchCmd { get; private set; } = Math.PI / 2;
        public bool Thrusting { get; private set; } = true;
        public double T { get; private set; }
        public bool Done { get; private set; }
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public readonly List<SimSample> Samples = new List<SimSample>();
        public int StageCount => st.Length;
        double maxQ, maxQTime, maxQAlt, maxG, lossG, lossD, lossS, dvExp, nextSample, kickStart = -1;
        bool liftedOff, sawKarman; double karmanT; string outcome = "timeout";
        /// <summary>
        /// Climbing, and escape-speed dynamic pressure at this altitude is below the recorded peak.
        /// Observational, like <see cref="SimResult.maxQAlt"/>: it does not change the RK4 step.
        /// A full <see cref="Headless.Simulate"/> ignores it. The designer reference ascent stops here.
        /// </summary>
        public bool MaxQSealed { get; private set; }

        /// <summary>Last diagnostics for HUD use (ambient pressure Pa, dynamic pressure Pa, Mach, thrust N).</summary>
        public double LastP, LastQ, LastMach, LastThrust;

        public AscentSim(IList<SimStage> stages, SimOptions opts)
        {
            stagesIn = stages; o = opts; st = Headless.Normalize(stages);
            kickAngle = o.kickAngleDeg * Math.PI / 180;
            upperMass = new double[st.Length + 1];
            upperMass[st.Length] = o.payload;
            for (int i = st.Length - 1; i >= 0; i--) upperMass[i] = upperMass[i + 1] + st[i].propMass + st[i].dryMass;
            vSite = o.rotate ? Earth.SiteRotationSpeed(o.latitude) : 0;
            Y[0] = 0; Y[1] = Gravity.R_EARTH; Y[2] = -vSite; Y[3] = 0; Y[4] = upperMass[0];
            omegaAir = o.rotate ? vSite / Gravity.R_EARTH : 0;
            deriv = Deriv;
            Events.Add(new SimEvent { t = 0, type = "ignition", stage = 1 });
        }

        void Deriv(double t, double[] s, double[] dy)
        {
            double x = s[0], yy = s[1], vx = s[2], vy = s[3], m = s[4];
            double r2 = x * x + yy * yy, r = Math.Sqrt(r2), gk = -Gravity.MU_EARTH / (r2 * r);
            double ax = gk * x, ay = gk * yy, mdot = 0;
            double ux = x / r, uy = yy / r, tx = -uy, ty = ux;
            double alt = r - Gravity.R_EARTH;
            double p = 0, rho = 0, a = 340;
            if (!o.vacuum && alt < 300e3) { var atm = Atmosphere.At(alt); p = atm.p; rho = atm.rho; a = atm.a; }
            double avx = vx + omegaAir * yy, avy = vy - omegaAir * x;
            double vrel = Math.Sqrt(avx * avx + avy * avy);
            if (rho > 0 && vrel > 0)
            {
                var S = st[Math.Min(StageIdx, st.Length - 1)];
                double D = 0.5 * rho * vrel * vrel * Aero.CdMach(vrel / a, S.cd) * S.area;
                ax -= D * avx / (vrel * m); ay -= D * avy / (vrel * m);
            }
            if (Thrusting && StageIdx < st.Length)
            {
                var S = st[StageIdx];
                double F = S.mdot * Atmosphere.G0 * RocketEq.IspAt(S.ispSL, S.ispVac, p);
                double dx, dyy;
                if (Mode == 2 && vrel > 1) { dx = avx / vrel; dyy = avy / vrel; }
                else { double c = Math.Cos(PitchCmd), sn = Math.Sin(PitchCmd); dx = c * tx + sn * ux; dyy = c * ty + sn * uy; }
                ax += F * dx / m; ay += F * dyy / m; mdot = S.mdot;
            }
            dy[0] = vx; dy[1] = vy; dy[2] = ax; dy[3] = ay; dy[4] = -mdot;
        }

        double StageEndMass() => upperMass[StageIdx] - st[StageIdx].propMass;

        /// <summary>Run one loop iteration. Returns false when the run has ended (call Finish()).</summary>
        public bool Tick()
        {
            if (Done) return false;
            if (!(T < o.tMax)) { Done = true; return false; }
            double x = Y[0], yy = Y[1], vx = Y[2], vy = Y[3], m = Y[4];
            double r = JsMath.Hypot(x, yy), alt = r - Gravity.R_EARTH;
            double ux = x / r, uy = yy / r, tx = -uy, ty = ux;
            double vr = vx * ux + vy * uy, vt = vx * tx + vy * ty;
            double avx = vx + omegaAir * yy, avy = vy - omegaAir * x;
            double vrel = JsMath.Hypot(avx, avy);

            // ---- guidance ----
            if (Thrusting)
            {
                if (StageIdx == 0)
                {
                    if (Mode == 0 && vrel >= o.kickSpeed) { Mode = 1; kickStart = T; PitchCmd = Math.PI / 2 - kickAngle; }
                    else if (Mode == 1)
                    {
                        double vgam = Math.Atan2(avx * ux + avy * uy, avx * tx + avy * ty);
                        if (T - kickStart > 3 && vgam <= PitchCmd + 0.002) Mode = 2;
                    }
                }
                else
                {
                    Mode = 3;
                    double g = Gravity.MU_EARTH / (r * r) - vt * vt / r;
                    double vrCmd = Math.Max(-150, Math.Min(250, (o.targetAlt - alt) / 40));
                    var S = st[StageIdx];
                    var atmG = Atmosphere.At(Math.Min(alt, 300e3));
                    double aT = S.mdot * Atmosphere.G0 * RocketEq.IspAt(S.ispSL, S.ispVac, o.vacuum ? 0 : atmG.p) / m;
                    double sinP = Math.Max(-0.6, Math.Min(0.95, (g + 0.03 * (vrCmd - vr)) / aT));
                    PitchCmd = Math.Asin(sinP);
                }
            }

            // ---- step size: land exactly on burnout ----
            double dt = o.dt;
            if (Thrusting && StageIdx < st.Length)
            {
                double remain = m - StageEndMass();
                if (remain <= 1e-6)
                {
                    Events.Add(new SimEvent { t = T, type = "burnout", stage = StageIdx + 1, alt = alt, v = JsMath.Hypot(vx, vy) });
                    StageIdx++;
                    if (StageIdx >= st.Length) { Thrusting = false; Mode = 4; return true; }
                    Y[4] = upperMass[StageIdx];
                    Events.Add(new SimEvent { t = T, type = "separation", stage = StageIdx, alt = alt });
                    Events.Add(new SimEvent { t = T, type = "ignition", stage = StageIdx + 1, alt = alt });
                    return true;
                }
                dt = Math.Min(o.dt, remain / st[StageIdx].mdot);
            }

            // ---- diagnostics ----
            double p = 0, rho = 0, sound = 340;
            if (!o.vacuum && alt < 300e3) { var atm = Atmosphere.At(alt); p = atm.p; rho = atm.rho; sound = atm.a; }
            double q = 0.5 * rho * vrel * vrel;
            // Observational, like KarmanTime: recording the altitude does not change the step.
            if (q > maxQ) { maxQ = q; maxQTime = T; maxQAlt = alt; }
            // Same sample. Escape speed is the fastest a bound ascent reaches; once that ceiling
            // is under the peak and the vehicle is climbing, later ticks on this run do not raise max-Q
            // for the shipped presets (the reference ascent checks that against a full Simulate).
            if (!MaxQSealed && maxQ > 0 && vr > 0 && 0.5 * rho * (2 * Gravity.MU_EARTH / r) < maxQ)
                MaxQSealed = true;
            double gl = Gravity.MU_EARTH / (r * r);
            double v = JsMath.Hypot(vx, vy);
            LastP = p; LastQ = q; LastMach = vrel / sound; LastThrust = 0;
            if (Thrusting && StageIdx < st.Length)
            {
                var S = st[StageIdx];
                double F = S.mdot * Atmosphere.G0 * RocketEq.IspAt(S.ispSL, S.ispVac, p);
                LastThrust = F;
                double aT = F / m; dvExp += aT * dt;
                double dx, dyy;
                if (Mode == 2 && vrel > 1) { dx = avx / vrel; dyy = avy / vrel; }
                else { double c = Math.Cos(PitchCmd), sn = Math.Sin(PitchCmd); dx = c * tx + sn * ux; dyy = c * ty + sn * uy; }
                if (v > 1) lossS += aT * (1 - (dx * vx + dyy * vy) / v) * dt;
                double Dm = rho > 0 ? 0.5 * rho * vrel * vrel * Aero.CdMach(vrel / sound, S.cd) * S.area / m : 0;
                lossD += Dm * dt;
                if (v > 1) lossG += gl * (vr / v) * dt;
                double gload = (aT - Dm) / Atmosphere.G0; if (gload > maxG) maxG = gload;
            }
            if (o.sampleEvery > 0 && T >= nextSample)
            {
                Samples.Add(new SimSample { t = T, alt = alt, v = v, vrel = vrel, q = q, m = m, stage = StageIdx + 1, pitch = PitchCmd, mode = Mode });
                nextSample += o.sampleEvery;
            }

            rk.Step(deriv, T, Y, dt);
            T += dt;

            double r1 = JsMath.Hypot(Y[0], Y[1]), alt1 = r1 - Gravity.R_EARTH;
            if (!liftedOff && alt1 > 1) { liftedOff = true; Events.Add(new SimEvent { t = T, type = "liftoff" }); }
            if (!sawKarman && alt1 >= UpperAtmosphere.KarmanM) { sawKarman = true; karmanT = T; }
            if (alt1 < 0)
            {
                if (!liftedOff && Thrusting) { Y[0] = 0; Y[1] = Gravity.R_EARTH; Y[2] = -vSite; Y[3] = 0; return true; }
                outcome = "crash"; Events.Add(new SimEvent { t = T, type = "impact" }); Done = true; return false;
            }
            if (Thrusting && Mode == 3 && o.cutoff)
            {
                var orb = Orbit.Planar(Y[0], Y[1], Y[2], Y[3]);
                if (orb.periAlt >= Math.Min(o.targetAlt, alt1) - 10e3 && orb.e < 1)
                {
                    Thrusting = false; Mode = 4;
                    Events.Add(new SimEvent { t = T, type = "cutoff", stage = StageIdx + 1, alt = alt1 });
                }
            }
            if (!Thrusting) { Done = true; return false; }
            return true;
        }

        public SimResult Finish()
        {
            var orb = Orbit.Planar(Y[0], Y[1], Y[2], Y[3]);
            if (outcome == "timeout" && !Thrusting) outcome = orb.periAlt > 120e3 ? "orbit" : "suborbital";
            if (outcome == "orbit") Events.Add(new SimEvent { t = T, type = "orbit", apoAlt = orb.apoAlt, periAlt = orb.periAlt });
            var warnings = new List<string>();
            foreach (var s in st) if (s.warn != null) warnings.Add(s.warn);
            return new SimResult
            {
                outcome = outcome, time = T, events = Events, orbit = orb, maxQ = maxQ, maxQTime = maxQTime, maxQAlt = maxQAlt, maxG = maxG,
                lossGravity = lossG, lossDrag = lossD, lossSteering = lossS,
                idealDeltaV = Headless.StackDeltaV(stagesIn, o.payload).total, deltaVExpended = dvExp, finalMass = Y[4],
                x = Y[0], y = Y[1], vx = Y[2], vy = Y[3], m = Y[4], samples = Samples, warnings = warnings,
                KarmanTime = sawKarman ? (double?)karmanT : null,
            };
        }
    }
}
