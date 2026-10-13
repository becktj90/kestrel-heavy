// Propulsion physics (§2.2). Catalog bells stay the reference point.
// Chamber pressure, expansion, throttle, gimbal, mixture offset and tune move that point.
// With no v3 inputs, EnginePoint does not call this: the tabulated bell is unchanged.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Vehicle
{
    public readonly struct EngineEnvelope
    {
        public readonly double pcMin, pcRated, epsMin, epsMax, cycleMassFactor;
        public EngineEnvelope(double pcMin, double pcRated, double epsMin, double epsMax, double cycleMassFactor)
        {
            this.pcMin = pcMin; this.pcRated = pcRated; this.epsMin = epsMin; this.epsMax = epsMax; this.cycleMassFactor = cycleMassFactor;
        }
    }

    /// <summary>One resolved bell: forces in N, Isp in s, mass in kg, exit diameter in m, gimbal in deg, hazard per second.</summary>
    public struct EngineResult
    {
        public double thrustSL, thrustVac, ispSL, ispVac, mass, exitDiameter;
        public double throttleMin, throttleMax, gimbalDeg;
        public double hazardRate;
        public bool flowSeparation;
        /// <summary>Multiplier on catalog reliability from a positive tune. 1 is nominal.</summary>
        public double tuneReliability;
        public bool mixtureInstability;
    }

    public static class EngineModel
    {
        public const double G0 = Atmosphere.G0;
        public const double SeaLevelPa = Atmosphere.P0;
        /// <summary>§2.2 states p_e &lt; 0.4·p_a and does not give γ. Isentropic γ = 1.20 closes p_e.</summary>
        public const double ExhaustGamma = 1.20;
        public const double EpsIspCoeff = 0.08;
        public const double PcIspExponent = 0.02;
        public const double PcMassExponent = 0.5;
        public const double OverpressureCoeff = 0.02;
        public const double PcOverRated = 1.15;
        public const double SeparationPressureRatio = 0.4;
        public const double SeparationIspFactor = 0.85;
        public const double DeepThrottle = 0.40;
        public const double DeepThrottleMassPerTen = 0.01;
        public const double GimbalMassPerDeg = 0.004;
        public const double TuneReliabilityPerPct = 0.005;

        public static double CycleMassFactor(string cycle)
        {
            switch (cycle)
            {
                case "pressure-fed": return 0.8;
                case "gas-generator": return 1.0;
                case "expander": return 1.05;
                case "staged-combustion": return 1.15;
                case "full-flow": return 1.25;
                default: return 1.0; // solid and hybrid are not in the §2.2 table
            }
        }

        /// <summary>Isp_vac = Isp_ref·(1+0.08·ln(ε/ε_ref))·(pc/pc_ref)^0.02</summary>
        public static double IspVac(double ispRef, double eps, double epsRef, double pc, double pcRef)
        {
            if (!(ispRef > 0) || !(eps > 0) || !(epsRef > 0) || !(pc > 0) || !(pcRef > 0)) return ispRef;
            return ispRef * (1 + EpsIspCoeff * Math.Log(eps / epsRef)) * Math.Pow(pc / pcRef, PcIspExponent);
        }

        /// <summary>mass = mass_ref·(k/k_ref)·(pc/pc_ref)^0.5. k_ref is the factor already baked into mass_ref.</summary>
        public static double ScaledMass(double massRef, double pc, double pcRef, double kCycle, double kAtRef)
        {
            if (!(massRef > 0) || !(pc > 0) || !(pcRef > 0)) return massRef;
            double k = kAtRef > 0 ? kCycle / kAtRef : 1;
            return massRef * k * Math.Pow(pc / pcRef, PcMassExponent);
        }

        /// <summary>λ = 0.02·max(0, pc/pc_rated−1)² per second.</summary>
        public static double OverpressurePerSecond(double pc, double pcRated)
        {
            if (!(pcRated > 0)) return 0;
            double x = Math.Max(0, pc / pcRated - 1);
            return OverpressureCoeff * x * x;
        }

        /// <summary>M_ctrl = N·F·sin(δ)·L_arm. F in newtons, arm in metres, result in N·m.</summary>
        public static double ControlMoment(double count, double thrustN, double gimbalDeg, double armM)
            => count * thrustN * Math.Sin(gimbalDeg * Math.PI / 180.0) * armM;

        /// <summary>Isp_SL = Isp_vac − p_a·A_e/(ṁ·g0), with ṁ = F_vac/(Isp_vac·g0) and A_e = π D²/4.</summary>
        public static double SeaLevelIsp(double ispVac, double thrustVac, double exitDiameter)
        {
            if (!(ispVac > 0) || !(thrustVac > 0) || !(exitDiameter > 0)) return 0;
            double area = Math.PI * 0.25 * exitDiameter * exitDiameter;
            double mdot = thrustVac / (ispVac * G0);
            return ispVac - SeaLevelPa * area / (mdot * G0);
        }

        public static double ExitPressurePa(double pcBar, double eps)
        {
            if (!(pcBar > 0) || !(eps > 0)) return 0;
            double mach = ExitMach(eps);
            double g = ExhaustGamma;
            double peOverPc = Math.Pow(1 + 0.5 * (g - 1) * mach * mach, -g / (g - 1));
            return pcBar * 1e5 * peOverPc;
        }

        public static bool FlowSeparates(double pcBar, double eps)
            => ExitPressurePa(pcBar, eps) < SeparationPressureRatio * SeaLevelPa;

        public static EngineResult Resolve(Engine engine, string nozzle, double pc, double eps, double throttleMin, double gimbalDeg, double mixtureOffsetPct, Tune tune)
        {
            if (engine == null) return new EngineResult { tuneReliability = 1 };
            var bell = BellOf(engine, nozzle);
            var env = EnvelopeFor(engine);
            double pcRef = engine.pc > 0 ? engine.pc : env.pcRated;
            double epsRef = bell.expansionRatio > 0 ? bell.expansionRatio : engine.expansionRatio;
            pc = ClampPc(double.IsNaN(pc) ? pcRef : pc, env);
            eps = ClampEps(double.IsNaN(eps) ? epsRef : eps, env);

            double tMax = ThrottleMax(engine);
            double tRef = ThrottleMinCatalog(engine);
            throttleMin = ClampThrottle(double.IsNaN(throttleMin) ? tRef : throttleMin, tMax);
            double gMax = engine.gimbal;
            if (gMax < 0) gMax = 0;
            gimbalDeg = ClampGimbal(double.IsNaN(gimbalDeg) ? gMax : gimbalDeg, gMax);

            if (double.IsNaN(mixtureOffsetPct)) mixtureOffsetPct = 0;
            double mix = MixtureOffset.IspFactor(mixtureOffsetPct);
            double kt = 1, ki = 1, reliability = 1;
            if (tune != null)
            {
                double tp = ClampTune(tune.thrustPct), ip = ClampTune(tune.ispPct);
                kt = 1 + tp / 100.0;
                ki = 1 + ip / 100.0;
                reliability = 1 - TuneReliabilityPerPct * (Math.Max(0, tp) + Math.Max(0, ip));
                if (reliability < 0) reliability = 0;
            }

            double kRef = CycleMassFactor(engine.cycle);
            double ispVacPhys = IspVac(bell.ispVac, eps, epsRef, pc, pcRef);
            double thrustVacPhys = bell.thrustVac * (pcRef > 0 ? pc / pcRef : 1);
            double exit = bell.exitDiameter * Math.Sqrt(epsRef > 0 ? eps / epsRef : 1);
            bool sep = FlowSeparates(pc, eps);
            bool sepRef = FlowSeparates(pcRef, epsRef);
            double ispSlRaw = Separated(SeaLevelIsp(ispVacPhys, thrustVacPhys, exit), sep);
            double ispSlRef = Separated(SeaLevelIsp(bell.ispVac, bell.thrustVac, bell.exitDiameter), sepRef);
            // The catalog sea-level bell is the anchor. The §2.2 expression supplies the change off that bell,
            // so a resolved point whose pc and ε are still the reference reproduces the tabulated numbers.
            double ispSL = bell.ispSL + (ispSlRaw - ispSlRef);
            double thrustSlRaw = ThrustFromIsp(thrustVacPhys, ispVacPhys, ispSlRaw);
            double thrustSlRef = ThrustFromIsp(bell.thrustVac, bell.ispVac, ispSlRef);
            double thrustSL = bell.thrustSL + (thrustSlRaw - thrustSlRef);

            double mass = ScaledMass(bell.mass, pc, pcRef, env.cycleMassFactor, kRef);
            mass *= 1 + GimbalMassPerDeg * (gimbalDeg - gMax);
            mass *= 1 + DeepThrottleMass(throttleMin, tRef);
            if (mass < 0) mass = 0;

            ispVacPhys *= mix * ki;
            ispSL *= mix * ki;
            thrustVacPhys *= kt;
            thrustSL *= kt;
            if (ispVacPhys < 0) ispVacPhys = 0;
            if (ispSL < 0) ispSL = 0;
            if (thrustSL < 0) thrustSL = 0;

            return new EngineResult
            {
                thrustSL = thrustSL, thrustVac = thrustVacPhys, ispSL = ispSL, ispVac = ispVacPhys,
                mass = mass, exitDiameter = exit,
                throttleMin = throttleMin, throttleMax = tMax, gimbalDeg = gimbalDeg,
                hazardRate = OverpressurePerSecond(pc, env.pcRated),
                flowSeparation = sep,
                tuneReliability = reliability,
                mixtureInstability = MixtureOffset.InstabilityHazard(mixtureOffsetPct),
            };
        }

        public static EngineEnvelope EnvelopeFor(Engine engine)
        {
            if (engine != null && Table().TryGetValue(engine.id ?? "", out var env)) return env;
            return Synthesize(engine);
        }

        static double Separated(double ispSl, bool separated) => separated ? ispSl * SeparationIspFactor : ispSl;

        static double ThrustFromIsp(double thrustVac, double ispVac, double ispSl)
        {
            if (!(ispVac > 0) || !(thrustVac > 0)) return 0;
            return thrustVac * ispSl / ispVac;
        }

        static double DeepThrottleMass(double throttleMin, double throttleRef)
        {
            double deeper = Math.Max(0, DepthBelowForty(throttleMin) - DepthBelowForty(throttleRef));
            return DeepThrottleMassPerTen * (deeper / 0.10);
        }

        static double DepthBelowForty(double throttle) => Math.Max(0, DeepThrottle - throttle);

        static double ClampPc(double pc, EngineEnvelope env)
        {
            double hi = env.pcRated * PcOverRated;
            if (env.pcMin > 0 && pc < env.pcMin) pc = env.pcMin;
            if (hi > 0 && pc > hi) pc = hi;
            return pc;
        }

        static double ClampEps(double eps, EngineEnvelope env)
        {
            if (env.epsMin > 0 && eps < env.epsMin) eps = env.epsMin;
            if (env.epsMax > 0 && eps > env.epsMax) eps = env.epsMax;
            return eps;
        }

        static double ClampGimbal(double deg, double max)
        {
            if (deg < 0) deg = 0;
            if (deg > max) deg = max;
            return deg;
        }

        static double ClampThrottle(double throttleMin, double throttleMax)
        {
            if (!(throttleMax > 0)) throttleMax = 1;
            if (throttleMin > throttleMax) throttleMin = throttleMax;
            if (!(throttleMin > 0)) throttleMin = Math.Min(throttleMax, 1e-4);
            return throttleMin;
        }

        static double ClampTune(double pct)
        {
            double lim = DesignerLimits.Active.tuneLimitPct;
            if (double.IsNaN(pct)) return 0;
            return Math.Max(-lim, Math.Min(lim, pct));
        }

        static double ThrottleMax(Engine e) => e.throttle != null && e.throttle.Length > 1 ? e.throttle[1] : 1;
        static double ThrottleMinCatalog(Engine e) => e.throttle != null && e.throttle.Length > 0 ? e.throttle[0] : ThrottleMax(e);

        struct Bell
        {
            public double thrustSL, thrustVac, ispSL, ispVac, mass, exitDiameter, expansionRatio;
        }

        static Bell BellOf(Engine e, string nozzleId)
        {
            if (!string.IsNullOrEmpty(nozzleId) && nozzleId != "default" && e.nozzles != null)
            {
                for (int i = 0; i < e.nozzles.Count; i++)
                {
                    var n = e.nozzles[i];
                    if (n.id != nozzleId) continue;
                    return new Bell
                    {
                        thrustSL = n.thrustSL, thrustVac = n.thrustVac, ispSL = n.ispSL, ispVac = n.ispVac,
                        mass = n.mass, exitDiameter = n.exitDiameter, expansionRatio = n.expansionRatio,
                    };
                }
            }
            return new Bell
            {
                thrustSL = e.thrustSL, thrustVac = e.thrustVac, ispSL = e.ispSL, ispVac = e.ispVac,
                mass = e.mass, exitDiameter = e.diameter, expansionRatio = e.expansionRatio,
            };
        }

        static EngineEnvelope Synthesize(Engine e)
        {
            double pc = e != null && e.pc > 0 ? e.pc : 1;
            double eps = e != null && e.expansionRatio > 0 ? e.expansionRatio : 8;
            bool vacuum = e == null || e.thrustSL <= 0;
            double cap = vacuum ? 250 : 120;
            return new EngineEnvelope(pc * 0.5, pc, Math.Min(8, eps), Math.Max(cap, eps), CycleMassFactor(e != null ? e.cycle : null));
        }

        static Dictionary<string, EngineEnvelope> table;
        static bool tableLoaded;

        static Dictionary<string, EngineEnvelope> Table()
        {
            if (tableLoaded) return table;
            tableLoaded = true;
            table = new Dictionary<string, EngineEnvelope>();
            string root = RepoRoot();
            if (root == null) return table;
            string path = Path.Combine(root, "data", "engines.json");
            if (!File.Exists(path)) return table;
            var doc = MiniJson.ParseObject(File.ReadAllText(path));
            var rows = doc.Arr("engines");
            if (rows == null) return table;
            foreach (JsonObject o in rows)
            {
                string id = o.Str("id");
                if (string.IsNullOrEmpty(id)) continue;
                if (o.NumOrNull("pcRated") == null || o.NumOrNull("cycleMassFactor") == null) continue;
                table[id] = new EngineEnvelope(o.Num("pcMin"), o.Num("pcRated"), o.Num("epsMin"), o.Num("epsMax"), o.Num("cycleMassFactor"));
            }
            return table;
        }

        static string RepoRoot()
        {
            var env = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            string[] starts = { !string.IsNullOrEmpty(env) ? env : Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            for (int i = 0; i < starts.Length; i++)
            {
                try { return Catalog.FindRepoRoot(starts[i]); }
                catch (DirectoryNotFoundException) { }
            }
            return null;
        }

        static double ExitMach(double eps)
        {
            // Supersonic branch of the isentropic area ratio. ε(M=1) = 1.
            double lo = 1.0000001, hi = 80;
            for (int i = 0; i < 60; i++)
            {
                double mid = 0.5 * (lo + hi);
                if (AreaRatio(mid) < eps) lo = mid;
                else hi = mid;
            }
            return 0.5 * (lo + hi);
        }

        static double AreaRatio(double mach)
        {
            double g = ExhaustGamma;
            double inner = (2 / (g + 1)) * (1 + 0.5 * (g - 1) * mach * mach);
            return (1 / mach) * Math.Pow(inner, (g + 1) / (2 * (g - 1)));
        }
    }
}
