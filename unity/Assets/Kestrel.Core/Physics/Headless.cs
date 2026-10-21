// Headless 2-D (equatorial-plane, inertial, Earth-centred) ascent simulator.
// State y = [x, y, vx, vy, m] (m, m/s, kg). Fixed-step RK4, no per-step allocations.
// Faithful port of web/js/physics/headless.js (same guidance, same step-size rule, same events).
// AscentSim exposes the loop one iteration at a time (Tick) so a render loop can drive it;
// Headless.Simulate runs it to completion exactly like the JS simulate().
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Physics
{
    public sealed class SimStage
    {
        public double thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, cd = 0.3, throttleMin;
        // derived by Normalize
        public double mdot, area; public string warn;
        public SimStage Clone() => (SimStage)MemberwiseClone();
    }

    public sealed class SimOptions
    {
        public double payload = 0, targetAlt = 200e3, latitude = 28.5, dt = 0.05, tMax = 3000;
        public double kickSpeed = 60, kickAngleDeg = 4, sampleEvery = 0;
        public bool vacuum = false, rotate = true, cutoff = true;
        /// <summary>Null keeps the built-in kick. A profile adds its pitch program and throttle caps.</summary>
        public Flight.GuidanceProfile guidance;
        /// <summary>Null leaves the flight on the v2 path: no failure checks, identical steps.</summary>
        public Flight.FailureMonitor monitor;
        /// <summary>Null is calm air. Drag, q and alpha stay on the nominal path.</summary>
        public Flight.SeededWind wind;
        /// <summary>
        /// Fairing jettison altitude in metres. 0 emits nothing, so existing event lists stay put.
        /// A positive value records one "fairing" event after max-Q, once altitude has reached it.
        /// </summary>
        public double fairingJettisonAltM;
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
        public Flight.FailureEvent failure;
        public List<Flight.FlightRisk> risks;
        public double minStability = double.NaN, minCar = double.NaN, qLimPa = double.NaN;
        /// <summary>
        /// Drag-aware peak of a ballistic coast (periapsis below ground), metres: <see cref="AscentSim.CoastApexAlt"/>
        /// after a clean cutoff. NaN for an orbit, a failure, a crash, or a run still thrusting. Score apogee trips with
        /// <see cref="Flight.MissionScore.FlownApoAltM"/>, not the vacuum Kepler apoapsis at cutoff.
        /// </summary>
        public double coastApexAlt = double.NaN;
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

        public static SimResult Simulate(IList<SimStage> stages, SimOptions opts = null, Flight.IFlightSimSink sink = null)
        {
            var sim = new AscentSim(stages, opts ?? new SimOptions());
            sim.Sink = sink;
            while (sim.Tick()) { }
            return sim.Finish();
        }

        /// <summary>
        /// The built-in kick, including its 3 s dwell, stays unless the profile changes the pitch program.
        /// A max-Q or g throttle cap still applies on that path. 100% leaves the scale at 1.
        /// </summary>
        public static bool LegacyPitch(GuidanceProfile g)
        {
            if (g == null) return true;
            if (g.useAltitudeKick || g.turnShapeSet) return false;
            return g.pitchKickDeg == GuidanceProfile.DefaultKickDeg;
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
        /// <summary>Peak q is confirmed once it falls below this fraction of the running maximum. The hook reports that maximum.</summary>
        public const double MaxQConfirm = 0.98;
        double maxQ, maxQTime, maxQAlt, maxG, lossG, lossD, lossS, dvExp, nextSample, kickStart = -1, thrustScale = 1, catchPitch;
        bool turned;
        bool liftedOff, sawKarman, maxQPassed, fairingSent, ignitionHooked; double karmanT; string outcome = "timeout";
        FailureEvent playedBreak;
        FlightVec pad;

        /// <summary>Last diagnostics for HUD use (ambient pressure Pa, dynamic pressure Pa, Mach, thrust N).</summary>
        public double LastP, LastQ, LastMach, LastThrust;
        /// <summary>Running loss totals (m/s). Read by the ascent HUD. Writing them does not feed the step.</summary>
        public double LossGravity => lossG;
        public double LossDrag => lossD;
        public double LossSteering => lossS;
        /// <summary>
        /// Player or autopilot nudge. 1 and 0 leave the step untouched, so a sim that never
        /// sets these matches the golden path. Throttle scale multiplies the guidance scale.
        /// Pitch offset is radians added to the command, the same channel as the kick.
        /// </summary>
        public double LiveThrottleScale = 1;
        public double LivePitchOffsetRad;
        /// <summary>Observer. Null leaves the step alone. The sim does not read the sink.</summary>
        public Flight.IFlightSimSink Sink { get; set; }

        /// <summary>
        /// Propellant left in the burning stage, 1 full to 0 empty. NaN while coasting or after cutoff:
        /// there is no live tank measurement then, and a HUD must not invent one.
        /// </summary>
        public double PropellantFraction
        {
            get
            {
                if (!Thrusting || StageIdx < 0 || StageIdx >= st.Length || !(st[StageIdx].propMass > 0)) return double.NaN;
                double frac = (Y[4] - StageEndMass()) / st[StageIdx].propMass;
                if (double.IsNaN(frac) || frac < 0) return 0;
                if (frac > 1) return 1;
                return frac;
            }
        }

        public AscentSim(IList<SimStage> stages, SimOptions opts)
        {
            stagesIn = stages; o = opts; st = Headless.Normalize(stages);
            kickAngle = o.kickAngleDeg * Math.PI / 180;
            upperMass = new double[st.Length + 1];
            upperMass[st.Length] = o.payload;
            for (int i = st.Length - 1; i >= 0; i--) upperMass[i] = upperMass[i + 1] + st[i].propMass + st[i].dryMass;
            vSite = o.rotate ? Earth.SiteRotationSpeed(o.latitude) : 0;
            Y[0] = 0; Y[1] = Gravity.R_EARTH; Y[2] = -vSite; Y[3] = 0; Y[4] = upperMass[0];
            pad = new FlightVec(Y[0], Y[1]);
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
            if (o.wind != null && !o.vacuum && alt < 300e3)
                vrel = o.wind.Retarget(alt, tx, ty, ref avx, ref avy, vrel);
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
                mdot = S.mdot;
                if (thrustScale != 1) { F *= thrustScale; mdot *= thrustScale; }
                double dx, dyy;
                if (Mode == 2 && vrel > 1) { dx = avx / vrel; dyy = avy / vrel; }
                else { double c = Math.Cos(PitchCmd), sn = Math.Sin(PitchCmd); dx = c * tx + sn * ux; dyy = c * ty + sn * uy; }
                ax += F * dx / m; ay += F * dyy / m;
            }
            dy[0] = vx; dy[1] = vy; dy[2] = ax; dy[3] = ay; dy[4] = -mdot;
        }

        double StageEndMass() => upperMass[StageIdx] - st[StageIdx].propMass;

        /// <summary>Run one loop iteration. Returns false when the run has ended (call Finish()).</summary>
        public bool Tick()
        {
            if (Done) return false;
            EnsureIgnition();
            if (!(T < o.tMax)) { Done = true; return false; }
            double x = Y[0], yy = Y[1], vx = Y[2], vy = Y[3], m = Y[4];
            double r = JsMath.Hypot(x, yy), alt = r - Gravity.R_EARTH;
            double ux = x / r, uy = yy / r, tx = -uy, ty = ux;
            double vr = vx * ux + vy * uy, vt = vx * tx + vy * ty;
            double avx = vx + omegaAir * yy, avy = vy - omegaAir * x;
            double vrel = JsMath.Hypot(avx, avy);
            if (o.wind != null && !o.vacuum && alt < 300e3)
                vrel = o.wind.Retarget(alt, tx, ty, ref avx, ref avy, vrel);

            // ---- guidance ----
            if (Thrusting)
            {
                thrustScale = o.monitor != null ? o.monitor.ThrustFactor : 1;
                if (Headless.LegacyPitch(o.guidance))
                {
                    bool holdKick = HoldingMaxQPitch(alt, m, vr, vrel);
                    if (StageIdx == 0)
                    {
                        if (Mode == 0 && vrel >= o.kickSpeed) { Mode = 1; kickStart = T; PitchCmd = Math.PI / 2 - kickAngle; }
                        else if (Mode == 1)
                        {
                            double vgam = Math.Atan2(avx * ux + avy * uy, avx * tx + avy * ty);
                            if (turned && holdKick)
                            {
                                // Stay on the angle caught when the path first sagged. Folding
                                // catchPitch down to this tick's vgam releases the hold after
                                // one step and the nose ratchets into the dive.
                                PitchCmd = catchPitch;
                            }
                            else if (turned)
                                Mode = 2;
                            else if (T - kickStart > 3 && vgam <= PitchCmd + 0.002)
                            {
                                catchPitch = vgam;
                                turned = true;
                                Mode = 2;
                            }
                        }
                        else if (Mode == 2 && holdKick)
                        {
                            // Follow the turn unless the path sags under the angle we caught.
                            // A cut thrust that keeps chasing that sag dives and q rises.
                            // PitchCmd stays on the kick until that freeze, so an uncapped
                            // flight still records the golden kick angle.
                            double vgam = Math.Atan2(avx * ux + avy * uy, avx * tx + avy * ty);
                            if (vgam < catchPitch - 0.002)
                            {
                                PitchCmd = catchPitch;
                                Mode = 1;
                            }
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
                    if (o.guidance != null && (o.guidance.maxQThrottleSet || o.guidance.gLimitSet))
                    {
                        double cap = o.guidance.Command(Guide(alt, m, vr, Gravity.MU_EARTH / (r * r) - vt * vt / r, vrel)).throttle;
                        if (cap != 1) thrustScale *= cap;
                    }
                }
                else
                {
                    double flight = Math.Atan2(avx * ux + avy * uy, avx * tx + avy * ty);
                    var cmd = o.guidance.Command(Guide(alt, m, vr, Gravity.MU_EARTH / (r * r) - vt * vt / r, vrel, flight));
                    PitchCmd = cmd.pitchRad;
                    if (StageIdx > 0) Mode = 3;
                    else if (Mode == 2) Mode = 1;
                    if (cmd.throttle != 1) thrustScale *= cmd.throttle;
                }
                if (LiveThrottleScale != 1)
                {
                    double s = LiveThrottleScale;
                    if (double.IsNaN(s) || s <= 0) s = 1;
                    else if (s > 1) s = 1;
                    if (s != 1) thrustScale *= s;
                }
                if (LivePitchOffsetRad != 0 && !double.IsNaN(LivePitchOffsetRad))
                {
                    double dp = LivePitchOffsetRad;
                    double lim = 10 * Math.PI / 180.0;
                    if (dp > lim) dp = lim;
                    else if (dp < -lim) dp = -lim;
                    PitchCmd += dp;
                }
            }

            // ---- step size: land exactly on burnout ----
            double dt = o.dt;
            if (Thrusting && StageIdx < st.Length)
            {
                double remain = m - StageEndMass();
                if (remain <= 1e-6)
                {
                    double spd = JsMath.Hypot(vx, vy);
                    Events.Add(new SimEvent { t = T, type = "burnout", stage = StageIdx + 1, alt = alt, v = spd });
                    bool fail = false;
                    if (o.monitor != null)
                    {
                        bool last = StageIdx >= st.Length - 1;
                        if (o.monitor.OnEmpty(T, alt, last, o.targetAlt)) fail = true;
                        if (!last && o.monitor.OnSeparation(T, alt, Dynamic(alt, vrel), StageIdx, st[StageIdx].dryMass, upperMass[StageIdx + 1])) fail = true;
                    }
                    if (fail) { Fail(alt, spd); return true; }
                    StageIdx++;
                    if (StageIdx >= st.Length) { Thrusting = false; Mode = 4; return true; }
                    Y[4] = upperMass[StageIdx];
                    Events.Add(new SimEvent { t = T, type = "separation", stage = StageIdx, alt = alt });
                    Events.Add(new SimEvent { t = T, type = "ignition", stage = StageIdx + 1, alt = alt });
                    if (Sink != null)
                    {
                        Sink.OnStageSeparation(StageIdx, Pos(), Vel());
                        Sink.OnIgnition(StageIdx + 1, T);
                    }
                    return true;
                }
                double flow = st[StageIdx].mdot;
                if (thrustScale != 1) flow *= thrustScale;
                dt = Math.Min(o.dt, flow > 0 ? remain / flow : o.dt);
            }

            // ---- diagnostics ----
            double p = 0, rho = 0, sound = 340;
            if (!o.vacuum && alt < 300e3) { var atm = Atmosphere.At(alt); p = atm.p; rho = atm.rho; sound = atm.a; }
            double q = 0.5 * rho * vrel * vrel;
            // Observational, like KarmanTime: recording the altitude does not change the step.
            if (q > maxQ) { maxQ = q; maxQTime = T; maxQAlt = alt; }
            else if (!maxQPassed && maxQ > 0 && q < maxQ * MaxQConfirm)
            {
                maxQPassed = true;
                Sink?.OnMaxQ(maxQ, maxQTime);
            }
            double gl = Gravity.MU_EARTH / (r * r);
            double v = JsMath.Hypot(vx, vy);
            LastP = p; LastQ = q; LastMach = vrel / sound; LastThrust = 0;
            if (o.monitor != null && Watch(alt, q, m, vrel, p, rho, sound, avx, avy, ux, uy, tx, ty, dt))
                Fail(alt, v);
            if (Thrusting && StageIdx < st.Length)
            {
                var S = st[StageIdx];
                double F = S.mdot * Atmosphere.G0 * RocketEq.IspAt(S.ispSL, S.ispVac, p);
                if (thrustScale != 1) F *= thrustScale;
                LastThrust = F;
                double aT = F / m; dvExp += aT * dt;
                double dx, dyy;
                if (Mode == 2 && vrel > 1) { dx = avx / vrel; dyy = avy / vrel; }
                else { double c = Math.Cos(PitchCmd), sn = Math.Sin(PitchCmd); dx = c * tx + sn * ux; dyy = c * ty + sn * uy; }
                double cos = v > 1 ? (dx * vx + dyy * vy) / v : 0;
                double Dm = rho > 0 ? 0.5 * rho * vrel * vrel * Aero.CdMach(vrel / sound, S.cd) * S.area / m : 0;
                LossAccount.Add(ref lossG, ref lossD, ref lossS, gl, vr, v, aT, cos, Dm, dt);
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
            if (!liftedOff && alt1 > 1)
            {
                liftedOff = true;
                Events.Add(new SimEvent { t = T, type = "liftoff" });
                Sink?.OnLiftoff(T, pad);
            }
            if (!sawKarman && alt1 >= UpperAtmosphere.KarmanM) { sawKarman = true; karmanT = T; }
            if (!fairingSent && o.fairingJettisonAltM > 0 && maxQPassed && alt1 >= o.fairingJettisonAltM)
            {
                fairingSent = true;
                double spd = JsMath.Hypot(Y[2], Y[3]);
                Events.Add(new SimEvent { t = T, type = "fairing", alt = alt1, v = spd });
                Sink?.OnFairingSeparation(T, Pos(), Vel());
            }
            if (alt1 < 0)
            {
                if (!liftedOff && Thrusting) { Y[0] = 0; Y[1] = Gravity.R_EARTH; Y[2] = -vSite; Y[3] = 0; return true; }
                double sink = r1 > 0 ? -(Y[2] * Y[0] + Y[3] * Y[1]) / r1 : 0;
                if (o.monitor != null && outcome != "failure")
                {
                    if (o.monitor.OnImpact(T, sink)) Fail(0, sink);
                }
                if (outcome != "failure") outcome = "crash";
                Events.Add(new SimEvent { t = T, type = "impact" });
                double impactSpeed = sink > 0 ? sink : JsMath.Hypot(Y[2], Y[3]);
                Sink?.OnImpact(impactSpeed, Pos());
                Done = true; return false;
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
            // Debris after a real failure coasts until impact or tMax. A clean cutoff still ends the run.
            if (!Thrusting && outcome != "failure") { Done = true; return false; }
            return true;
        }

        /// <summary>
        /// End the played flight at a breakup so the explosion can play.
        /// Headless.Simulate never calls this, so a recorded failure there still coasts to impact.
        /// </summary>
        public void BreakUp(FailureEvent ev)
        {
            EnsureIgnition();
            if (ev != null)
            {
                playedBreak = ev;
                if (o.monitor != null && o.monitor.Event == null) o.monitor.Event = ev;
            }
            if (outcome != "failure")
            {
                double r = JsMath.Hypot(Y[0], Y[1]);
                Fail(r - Gravity.R_EARTH, JsMath.Hypot(Y[2], Y[3]));
            }
            Done = true;
        }

        void Fail(double alt, double speed)
        {
            if (outcome == "failure") return;
            outcome = "failure";
            Thrusting = false;
            thrustScale = 0;
            Mode = 4;
            int stage = StageIdx < st.Length ? StageIdx + 1 : st.Length;
            Events.Add(new SimEvent { t = T, type = "failure", stage = stage, alt = alt, v = speed });
            if (Sink == null) return;
            var ev = o.monitor != null ? o.monitor.Event : null;
            if (ev == null) ev = playedBreak;
            string kind = ev != null && ev.kind != null ? ev.kind : "failure";
            string cause = ev != null && ev.cause != null ? ev.cause : "";
            int engine = ev != null ? ev.engineIndex : -1;
            Sink.OnFailure(kind, cause, engine, Pos(), Vel(), T);
            if (FlightEngineMode.IsEngine(kind)) Sink.OnEngineFailure(engine, FlightEngineMode.Of(kind));
        }

        void EnsureIgnition()
        {
            if (ignitionHooked) return;
            ignitionHooked = true;
            Sink?.OnIgnition(1, 0);
        }

        FlightVec Pos() => new FlightVec(Y[0], Y[1]);
        FlightVec Vel() => new FlightVec(Y[2], Y[3]);

        /// <summary>
        /// Hold the kick while a max-Q cap is actually cutting thrust. The gravity turn
        /// resumes when q leaves the bucket or the engine cannot throttle any lower.
        /// </summary>
        bool HoldingMaxQPitch(double alt, double mass, double vr, double vrel)
        {
            if (StageIdx != 0 || o.guidance == null) return false;
            double q = Dynamic(alt, vrel);
            if (!o.guidance.HoldKickThroughMaxQ(q)) return false;
            double r = alt + Gravity.R_EARTH;
            var cmd = o.guidance.Command(Guide(alt, mass, vr, Gravity.MU_EARTH / (r * r), vrel));
            return cmd.throttle < 1;
        }

        double Dynamic(double alt, double vrel)
        {
            if (o.vacuum || !(alt < 300e3) || !(vrel > 0)) return 0;
            return 0.5 * Atmosphere.At(alt).rho * vrel * vrel;
        }

        GuidanceState Guide(double alt, double m, double vr, double g, double vrel, double flight = 0)
        {
            double p = 0, rho = 0;
            if (!o.vacuum && alt < 300e3) { var atm = Atmosphere.At(alt); p = atm.p; rho = atm.rho; }
            double F = 0, tMin = 0;
            if (StageIdx < st.Length)
            {
                var S = st[StageIdx];
                F = S.mdot * Atmosphere.G0 * RocketEq.IspAt(S.ispSL, S.ispVac, p);
                tMin = S.throttleMin;
            }
            return new GuidanceState
            {
                alt = alt, vrel = vrel, flightPathRad = flight, vr = vr, g = g, q = 0.5 * rho * vrel * vrel,
                mass = m, thrustN = F, throttleMin = tMin, stageIndex = StageIdx,
            };
        }

        /// <summary>True when this step must stop thrusting. Predict records the hit and returns false.</summary>
        bool Watch(double alt, double q, double m, double vrel, double p, double rho, double sound, double avx, double avy, double ux, double uy, double tx, double ty, double dt)
        {
            int si = StageIdx < st.Length ? StageIdx : st.Length - 1;
            if (si < 0) si = 0;
            var body = st[si];
            double propFrac = 1;
            if (Thrusting && StageIdx < st.Length && st[StageIdx].propMass > 0)
            {
                propFrac = (Y[4] - StageEndMass()) / st[StageIdx].propMass;
                if (propFrac < 0) propFrac = 0;
                else if (propFrac > 1) propFrac = 1;
            }
            double aAxial = 0;
            if (Thrusting && StageIdx < st.Length && m > 0)
            {
                var S = st[StageIdx];
                double F = S.mdot * Atmosphere.G0 * RocketEq.IspAt(S.ispSL, S.ispVac, p);
                if (thrustScale != 1) F *= thrustScale;
                double Dm = rho > 0 ? 0.5 * rho * vrel * vrel * Aero.CdMach(vrel / sound, S.cd) * S.area / m : 0;
                aAxial = F / m - Dm;
            }
            double alpha = 0;
            if (Thrusting && Mode != 2 && vrel > 1)
            {
                double c = Math.Cos(PitchCmd), sn = Math.Sin(PitchCmd);
                double dx = c * tx + sn * ux, dyy = c * ty + sn * uy;
                double cosA = (dx * avx + dyy * avy) / vrel;
                if (cosA > 1) cosA = 1;
                else if (cosA < -1) cosA = -1;
                alpha = Math.Acos(cosA);
            }
            var step = new FailureMonitor.Step
            {
                t = T, dt = dt, alt = alt, qPa = q, accel = aAxial, alphaRad = alpha,
                mass = m, area = body.area, cd = body.cd, stageIndex = StageIdx, propFraction = propFrac,
                thrusting = Thrusting, targetAlt = o.targetAlt,
            };
            return o.monitor.Consider(step);
        }

        /// <summary>
        /// The flown peak of a ballistic coast: keeps integrating a copy of the state after a clean cutoff with the
        /// same gravity, drag, rotation and wind as the ascent until the radial rate turns down. Above the drag
        /// ceiling (300 km) or in vacuum the rest of the climb is Kepler, so it returns the vacuum apoapsis there.
        /// NaN while thrusting or after a failure. Observational: the sim's own state, events and result are untouched.
        /// </summary>
        public double CoastApexAlt()
        {
            if (Thrusting || outcome == "failure" || outcome == "crash") return double.NaN;
            var s = (double[])Y.Clone();
            double t = T, dt = o.dt > 0 ? o.dt : 0.05;
            for (int i = 0; i < 400000; i++)
            {
                double r = JsMath.Hypot(s[0], s[1]), alt = r - Gravity.R_EARTH;
                double vr = (s[2] * s[0] + s[3] * s[1]) / r;
                if (vr <= 0) return alt;
                if (o.vacuum || alt >= 300e3) return Orbit.Planar(s[0], s[1], s[2], s[3]).apoAlt;
                rk.Step(deriv, t, s, dt);
                t += dt;
            }
            return JsMath.Hypot(s[0], s[1]) - Gravity.R_EARTH;
        }

        public SimResult Finish()
        {
            EnsureIgnition();
            if (!maxQPassed && maxQ > 0)
            {
                maxQPassed = true;
                Sink?.OnMaxQ(maxQ, maxQTime);
            }
            var orb = Orbit.Planar(Y[0], Y[1], Y[2], Y[3]);
            if (outcome == "timeout" && !Thrusting) outcome = orb.periAlt > 120e3 ? "orbit" : "suborbital";
            if (outcome == "orbit") Events.Add(new SimEvent { t = T, type = "orbit", apoAlt = orb.apoAlt, periAlt = orb.periAlt });
            var warnings = new List<string>();
            foreach (var s in st) if (s.warn != null) warnings.Add(s.warn);
            var result = new SimResult
            {
                outcome = outcome, time = T, events = Events, orbit = orb, maxQ = maxQ, maxQTime = maxQTime, maxQAlt = maxQAlt, maxG = maxG,
                lossGravity = lossG, lossDrag = lossD, lossSteering = lossS,
                idealDeltaV = Headless.StackDeltaV(stagesIn, o.payload).total, deltaVExpended = dvExp, finalMass = Y[4],
                x = Y[0], y = Y[1], vx = Y[2], vy = Y[3], m = Y[4], samples = Samples, warnings = warnings,
                KarmanTime = sawKarman ? (double?)karmanT : null,
            };
            if (orb.periAlt < 0) result.coastApexAlt = CoastApexAlt();
            if (o.monitor != null)
            {
                o.monitor.Finish(maxQ);
                result.failure = o.monitor.Event;
                result.risks = o.monitor.Risks;
                result.minStability = o.monitor.MinStability;
                result.minCar = o.monitor.MinCar;
                result.qLimPa = o.monitor.QLimPa;
            }
            if (result.failure == null && playedBreak != null) result.failure = playedBreak;
            return result;
        }
    }
}
