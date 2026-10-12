// 3-DOF translational ascent. State y = [x, y, z, vx, vy, vz, m] (m, m/s, kg).
// SimulatePlanar seeds z = vz = 0 on the Headless pad and uses that type's air-rate
// (Ω cos φ) and guidance, so the in-plane RK4 steps match the 2-D flight.
// Point-mass gravity. No J2. No wind shear.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Physics
{
    public sealed class Ascent3Dof
    {
        readonly IList<SimStage> stagesIn; readonly SimStage[] st; readonly SimOptions o;
        readonly double[] upperMass; readonly double vSite, omegaAir, kickAngle;
        readonly RK4 rk = new RK4(7); readonly Deriv deriv;
        // 0 x, 1 y, 2 z, 3 vx, 4 vy, 5 vz, 6 m
        public readonly double[] Y = new double[7];
        public int StageIdx { get; private set; }
        public int Mode { get; private set; }
        public double PitchCmd { get; private set; } = Math.PI / 2;
        public bool Thrusting { get; private set; } = true;
        public double T { get; private set; }
        public bool Done { get; private set; }
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public readonly List<SimSample> Samples = new List<SimSample>();
        double maxQ, maxQTime, maxG, lossG, lossD, lossS, dvExp, nextSample, kickStart = -1;
        bool liftedOff; string outcome = "timeout";

        public static SimResult SimulatePlanar(IList<SimStage> stages, SimOptions opts = null)
        {
            var sim = new Ascent3Dof(stages, opts ?? new SimOptions());
            while (sim.Tick()) { }
            return sim.Finish();
        }

        public Ascent3Dof(IList<SimStage> stages, SimOptions opts)
        {
            stagesIn = stages; o = opts; st = Headless.Normalize(stages);
            kickAngle = o.kickAngleDeg * Math.PI / 180;
            upperMass = new double[st.Length + 1];
            upperMass[st.Length] = o.payload;
            for (int i = st.Length - 1; i >= 0; i--) upperMass[i] = upperMass[i + 1] + st[i].propMass + st[i].dryMass;
            vSite = o.rotate ? Earth.SiteRotationSpeed(o.latitude) : 0;
            Y[0] = 0; Y[1] = Gravity.R_EARTH; Y[2] = 0; Y[3] = -vSite; Y[4] = 0; Y[5] = 0; Y[6] = upperMass[0];
            omegaAir = o.rotate ? vSite / Gravity.R_EARTH : 0;
            deriv = Deriv;
            Events.Add(new SimEvent { t = 0, type = "ignition", stage = 1 });
        }

        void Deriv(double t, double[] s, double[] dy)
        {
            double x = s[0], yy = s[1], z = s[2], vx = s[3], vy = s[4], vz = s[5], m = s[6];
            double r2 = x * x + yy * yy + z * z, r = Math.Sqrt(r2), gk = -Gravity.MU_EARTH / (r2 * r);
            double ax = gk * x, ay = gk * yy, az = gk * z, mdot = 0;
            double ux = x / r, uy = yy / r, uz = z / r, tx = -uy, ty = ux, tz = 0;
            double alt = r - Gravity.R_EARTH;
            double p = 0, rho = 0, a = 340;
            if (!o.vacuum && alt < 300e3) { var atm = Atmosphere.At(alt); p = atm.p; rho = atm.rho; a = atm.a; }
            double avx = vx + omegaAir * yy, avy = vy - omegaAir * x, avz = vz;
            double vrel = Math.Sqrt(avx * avx + avy * avy + avz * avz);
            if (rho > 0 && vrel > 0)
            {
                var S = st[Math.Min(StageIdx, st.Length - 1)];
                double D = 0.5 * rho * vrel * vrel * Aero.CdMach(vrel / a, S.cd) * S.area;
                ax -= D * avx / (vrel * m); ay -= D * avy / (vrel * m); az -= D * avz / (vrel * m);
            }
            if (Thrusting && StageIdx < st.Length)
            {
                var S = st[StageIdx];
                double F = S.mdot * Atmosphere.G0 * RocketEq.IspAt(S.ispSL, S.ispVac, p);
                double dx, dyy, dz;
                if (Mode == 2 && vrel > 1) { dx = avx / vrel; dyy = avy / vrel; dz = avz / vrel; }
                else { double c = Math.Cos(PitchCmd), sn = Math.Sin(PitchCmd); dx = c * tx + sn * ux; dyy = c * ty + sn * uy; dz = c * tz + sn * uz; }
                ax += F * dx / m; ay += F * dyy / m; az += F * dz / m; mdot = S.mdot;
            }
            dy[0] = vx; dy[1] = vy; dy[2] = vz; dy[3] = ax; dy[4] = ay; dy[5] = az; dy[6] = -mdot;
        }

        double StageEndMass() => upperMass[StageIdx] - st[StageIdx].propMass;

        public bool Tick()
        {
            if (Done) return false;
            if (!(T < o.tMax)) { Done = true; return false; }
            double x = Y[0], yy = Y[1], z = Y[2], vx = Y[3], vy = Y[4], vz = Y[5], m = Y[6];
            // Planar diagnostics use the same Hypot as Headless. z = 0, so this is the radius.
            double r = JsMath.Hypot(x, yy), alt = r - Gravity.R_EARTH;
            double ux = x / r, uy = yy / r, uz = z / r, tx = -uy, ty = ux, tz = 0;
            double vr = vx * ux + vy * uy + vz * uz, vt = vx * tx + vy * ty + vz * tz;
            double avx = vx + omegaAir * yy, avy = vy - omegaAir * x, avz = vz;
            double vrel = JsMath.Hypot(avx, avy);

            if (Thrusting)
            {
                if (StageIdx == 0)
                {
                    if (Mode == 0 && vrel >= o.kickSpeed) { Mode = 1; kickStart = T; PitchCmd = Math.PI / 2 - kickAngle; }
                    else if (Mode == 1)
                    {
                        double vgam = Math.Atan2(avx * ux + avy * uy + avz * uz, avx * tx + avy * ty + avz * tz);
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

            double dt = o.dt;
            if (Thrusting && StageIdx < st.Length)
            {
                double remain = m - StageEndMass();
                if (remain <= 1e-6)
                {
                    Events.Add(new SimEvent { t = T, type = "burnout", stage = StageIdx + 1, alt = alt, v = JsMath.Hypot(vx, vy) });
                    StageIdx++;
                    if (StageIdx >= st.Length) { Thrusting = false; Mode = 4; return true; }
                    Y[6] = upperMass[StageIdx];
                    Events.Add(new SimEvent { t = T, type = "separation", stage = StageIdx, alt = alt });
                    Events.Add(new SimEvent { t = T, type = "ignition", stage = StageIdx + 1, alt = alt });
                    return true;
                }
                dt = Math.Min(o.dt, remain / st[StageIdx].mdot);
            }

            double p = 0, rho = 0, sound = 340;
            if (!o.vacuum && alt < 300e3) { var atm = Atmosphere.At(alt); p = atm.p; rho = atm.rho; sound = atm.a; }
            double q = 0.5 * rho * vrel * vrel;
            if (q > maxQ) { maxQ = q; maxQTime = T; }
            double gl = Gravity.MU_EARTH / (r * r);
            double v = JsMath.Hypot(vx, vy);
            if (Thrusting && StageIdx < st.Length)
            {
                var S = st[StageIdx];
                double F = S.mdot * Atmosphere.G0 * RocketEq.IspAt(S.ispSL, S.ispVac, p);
                double aT = F / m; dvExp += aT * dt;
                double dx, dyy, dz;
                if (Mode == 2 && vrel > 1) { dx = avx / vrel; dyy = avy / vrel; dz = avz / vrel; }
                else { double c = Math.Cos(PitchCmd), sn = Math.Sin(PitchCmd); dx = c * tx + sn * ux; dyy = c * ty + sn * uy; dz = c * tz + sn * uz; }
                if (v > 1) lossS += aT * (1 - (dx * vx + dyy * vy + dz * vz) / v) * dt;
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
            if (alt1 < 0)
            {
                if (!liftedOff && Thrusting) { Y[0] = 0; Y[1] = Gravity.R_EARTH; Y[2] = 0; Y[3] = -vSite; Y[4] = 0; Y[5] = 0; return true; }
                outcome = "crash"; Events.Add(new SimEvent { t = T, type = "impact" }); Done = true; return false;
            }
            if (Thrusting && Mode == 3 && o.cutoff)
            {
                var orb = Orbit.Planar(Y[0], Y[1], Y[3], Y[4]);
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
            var orb = Orbit.Planar(Y[0], Y[1], Y[3], Y[4]);
            if (outcome == "timeout" && !Thrusting) outcome = orb.periAlt > 120e3 ? "orbit" : "suborbital";
            if (outcome == "orbit") Events.Add(new SimEvent { t = T, type = "orbit", apoAlt = orb.apoAlt, periAlt = orb.periAlt });
            var warnings = new List<string>();
            foreach (var s in st) if (s.warn != null) warnings.Add(s.warn);
            return new SimResult
            {
                outcome = outcome, time = T, events = Events, orbit = orb, maxQ = maxQ, maxQTime = maxQTime, maxG = maxG,
                lossGravity = lossG, lossDrag = lossD, lossSteering = lossS,
                idealDeltaV = Headless.StackDeltaV(stagesIn, o.payload).total, deltaVExpended = dvExp, finalMass = Y[6],
                x = Y[0], y = Y[1], vx = Y[3], vy = Y[4], m = Y[6], samples = Samples, warnings = warnings,
            };
        }
    }
}
