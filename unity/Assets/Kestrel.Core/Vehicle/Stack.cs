// Staging, drag estimate and the headless-sim stage list. Port of web/js/vehicle/stack.js
// plus serialStages()/requiredDeltaV() from web/js/designer/readout.js (design -> sim input).
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Vehicle
{
    public sealed class StackEntry
    {
        public double thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, Cd, throttleMin;
        public string role; public bool parallel, crossfeed; public int count, index = -1;
        public double reserveMass; public double[] throttle; public bool restart;
        public double payloadMass, fairingMass, fairingJettisonAltitudeM; public bool isLast;
        public StackEntry Clone() => (StackEntry)MemberwiseClone();
    }

    public sealed class SerialStage
    {
        public double thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, cd, throttleMin;
        public string role, label; public int index = -1;
        public SimStage ToSim() => new SimStage { thrustSL = thrustSL, thrustVac = thrustVac, ispSL = ispSL, ispVac = ispVac, propMass = propMass, dryMass = dryMass, diameter = diameter, cd = cd, throttleMin = throttleMin };
    }

    public static class Stack
    {
        public static double BaseCd(Design d)
        {
            // v3 catalogs replace the hard-coded terms only when v3 aero fields are present (§2.5).
            if (AeroModel.HasV3Aero(d)) return AeroModel.Cd0(d);
            double cd = DesignMath.NoseShapes.TryGetValue(d.nose ?? "", out var n) ? n : 0.32;
            if (d.fins) cd += 0.04;
            if (d.gridFins) cd += 0.03;
            if (d.StrapOnCount > 0) cd += 0.03 * d.StrapOnCount;
            var top = d.stages[d.stages.Count - 1];
            if (d.fairingDiameter > top.diameter) cd += 0.08 * (d.fairingDiameter / top.diameter - 1);
            cd += InterstageAero.DragCd(d);
            return cd;
        }

        public static double ReferenceDiameter(Design d)
        {
            double core = d.fairingDiameter;
            // JS: Math.max(...stage diameters, fairing || 0)
            double m = double.NegativeInfinity; foreach (var s in d.stages) m = Math.Max(m, s.diameter); core = Math.Max(m, core);
            var so = d.strapOns;
            double area = Math.PI * Math.Pow(core / 2, 2) + (so != null && so.count > 0 ? so.count * Math.PI * Math.Pow(so.diameter / 2, 2) : 0);
            return 2 * Math.Sqrt(area / Math.PI);
        }

        public static List<StackEntry> ToSimStages(Catalog c, Design d)
        {
            var masses = Mass.StageMasses(c, d);
            double Cd = BaseCd(d), diameter = ReferenceDiameter(d);
            var o = new List<StackEntry>();
            var so = d.strapOns;
            if (so != null && so.count > 0)
            {
                var m = Mass.StrapOnMasses(c, d); var e = Point(c, so);
                var strap = new StackEntry { thrustSL = e.thrustSL * so.engineCount * so.count, thrustVac = e.thrustVac * so.engineCount * so.count, ispSL = e.ispSL, ispVac = e.ispVac, propMass = m.prop, dryMass = m.dry, diameter = diameter, Cd = Cd, role = "strapons", parallel = true, crossfeed = so.crossfeed, count = so.count, throttleMin = ThrottleMin(e) };
                ApplyLoadedPropellant(c, strap, so);
                o.Add(strap);
            }
            for (int i = 0; i < d.stages.Count; i++)
            {
                var s = d.stages[i]; var m = masses[i]; var e = Point(c, s); int n = s.engineCount;
                var core = new StackEntry
                {
                    thrustSL = e.thrustSL * n, thrustVac = e.thrustVac * n, ispSL = e.ispSL, ispVac = e.ispVac, propMass = m.usableProp, dryMass = m.dry,
                    diameter = i == 0 ? diameter : Math.Max(s.diameter, d.fairingDiameter), Cd = Cd, role = "core", index = i, reserveMass = m.landingProp,
                    throttle = e.throttle != null ? (double[])e.throttle.Clone() : null, restart = e.restart, throttleMin = ThrottleMin(e),
                };
                ApplyLoadedPropellant(c, core, s);
                o.Add(core);
            }
            var last = o[o.Count - 1];
            last.isLast = true; last.payloadMass = d.payloadMass; last.fairingMass = Mass.FairingMass(d);
            last.fairingJettisonAltitudeM = d.fairingJettisonAltitudeKm * 1000;
            return o;
        }

        const double G0 = 9.80665;
        public static (double total, double[] perStage) IdealDeltaV(List<StackEntry> s)
        {
            var last = s[s.Count - 1];
            double TotalAbove(int k) { double a = 0; for (int j = k; j < s.Count; j++) a = a + s[j].propMass + s[j].dryMass + s[j].reserveMass; return a + last.payloadMass + last.fairingMass; }
            double dv = 0; var per = new double[s.Count];
            for (int k = 0; k < s.Count; k++)
            {
                double m0 = TotalAbove(k), mf = m0 - s[k].propMass, isp = s[k].ispVac;
                if (s[k].parallel && k + 1 < s.Count) { var cc = s[k + 1]; isp = (s[k].thrustVac + cc.thrustVac) / (s[k].thrustVac / s[k].ispVac + cc.thrustVac / cc.ispVac); }
                double dd = isp * G0 * Math.Log(m0 / mf); per[k] = dd; dv += dd;
            }
            return (dv, per);
        }

        // ---- from designer/readout.js ----
        public static double TUNE_LIMIT => Designer.DesignerLimits.Active.tuneLimitPct;
        public const double LAUNCH_LATITUDE = 28.5, LOSS_BUDGET = 1500;

        /// <summary>v3 engineParams or a mixture offset already include tune. The catalog path still applies it here.</summary>
        static EnginePoint Point(Catalog c, StagePart s)
        {
            if (EnginePoint.UsesModel(s)) return EnginePoint.Of(c, s);
            return EnginePoint.Of(c.EngineById[s.engine], s.nozzle);
        }

        static double ThrottleMin(EnginePoint e) => e.throttle != null && e.throttle.Length > 0 ? e.throttle[0] : 0;

        /// <summary>
        /// Matched fuel keeps the catalog bell (goldens stay put). A different load keeps the
        /// engine's mass flow and scales thrust and Isp by the loaded propellant's vacuum Isp.
        /// </summary>
        static void ApplyLoadedPropellant(Catalog c, StackEntry entry, StagePart part)
        {
            if (c == null || entry == null || part == null) return;
            if (!c.PropellantById.TryGetValue(part.propellant ?? "", out var loaded)) return;
            Propellant native = null;
            if (c.EngineById.TryGetValue(part.engine ?? "", out var eng))
                c.PropellantById.TryGetValue(eng.propellant ?? "", out native);
            var bell = PropellantFlight.Scale(loaded, native, entry.thrustSL, entry.thrustVac, entry.ispSL, entry.ispVac);
            entry.thrustSL = bell.ThrustSL;
            entry.thrustVac = bell.ThrustVac;
            entry.ispSL = bell.IspSL;
            entry.ispVac = bell.IspVac;
        }

        static void ApplyTune(StackEntry e, Tune t)
        {
            if (t == null) return;
            double Clamp(double v) => Math.Max(-TUNE_LIMIT, Math.Min(TUNE_LIMIT, double.IsNaN(v) ? 0 : v)) / 100;
            double kt = 1 + Clamp(t.thrustPct), ki = 1 + Clamp(t.ispPct);
            e.thrustVac *= kt; e.thrustSL *= kt; e.ispVac *= ki; e.ispSL *= ki;
        }

        /// <summary>Strap-ons become a combined parallel phase; landing reserve rides as dry mass; payload = payload + fairing.</summary>
        public static (List<SerialStage> stages, double payload) SerialStages(Catalog c, Design d)
        {
            var sim = ToSimStages(c, d).ConvertAll(x => x.Clone());
            foreach (var s in sim)
            {
                var part = s.role == "strapons" ? d.strapOns : d.stages[s.index];
                if (!EnginePoint.UsesModel(part)) ApplyTune(s, part != null ? part.tune : null);
            }
            var last = sim[sim.Count - 1];
            double payload = last.payloadMass + last.fairingMass;
            var o = new List<SerialStage>();
            for (int k = 0; k < sim.Count; k++)
            {
                var s = sim[k];
                if (s.parallel && k + 1 < sim.Count)
                {
                    var cc = sim[k + 1];
                    double mdS = RocketEq.MassFlow(s.thrustVac, s.ispVac), mdC = RocketEq.MassFlow(cc.thrustVac, cc.ispVac);
                    double tB = s.propMass / mdS;
                    double coreBurn = Math.Min(mdC * tB, cc.propMass * 0.95);
                    double thrustVac = s.thrustVac + cc.thrustVac, thrustSL = s.thrustSL + cc.thrustSL;
                    double ispVac = (s.thrustVac * tB + cc.thrustVac * (coreBurn / mdC)) / ((s.propMass + coreBurn) * G0);
                    double mdot = RocketEq.MassFlow(thrustVac, ispVac);
                    double ispSL = thrustSL / (mdot * G0);
                    o.Add(new SerialStage { diameter = s.diameter, cd = s.Cd, thrustVac = thrustVac, thrustSL = thrustSL, ispVac = ispVac, ispSL = ispSL, propMass = s.propMass + coreBurn, dryMass = s.dryMass, role = "strapons+core", label = $"Core + {s.count} strap-ons", throttleMin = Math.Min(s.throttleMin, cc.throttleMin) });
                    o.Add(new SerialStage { diameter = cc.diameter, cd = s.Cd, thrustVac = cc.thrustVac, thrustSL = cc.thrustSL, ispVac = cc.ispVac, ispSL = cc.ispSL, propMass = cc.propMass - coreBurn, dryMass = cc.dryMass + cc.reserveMass, role = "core", index = cc.index, label = Name(d, cc.index), throttleMin = cc.throttleMin });
                    k++; continue;
                }
                o.Add(new SerialStage { diameter = s.diameter, cd = s.Cd, thrustVac = s.thrustVac, thrustSL = s.thrustSL, ispVac = s.ispVac, ispSL = s.ispSL, propMass = s.propMass, dryMass = s.dryMass + s.reserveMass, role = "core", index = s.index, label = Name(d, s.index), throttleMin = s.throttleMin });
            }
            return (o, payload);
        }

        static string Name(Design d, int i) => string.IsNullOrEmpty(d.stages[i].name) ? $"Stage {i + 1}" : d.stages[i].name;

        public struct Required { public OrbitTarget orbit; public double targetAlt, required, vCirc; }

        public static Required RequiredDeltaV(Catalog c, string orbitId, double latitude = LAUNCH_LATITUDE)
        {
            var o = orbitId != null && c.Orbits.TryGetValue(orbitId, out var x) ? x : c.Orbits["leo400"];
            if (o.periKm < 0) { double h = o.apoKm * 1000; return new Required { orbit = o, targetAlt = h, required = Math.Sqrt(2 * G0 * h) + 1000 }; }
            double rp = Gravity.R_EARTH + o.periKm * 1000, ra = Gravity.R_EARTH + o.apoKm * 1000;
            double vCirc = Math.Sqrt(Gravity.MU_EARTH / rp);
            double vPeri = Math.Sqrt(Gravity.MU_EARTH * (2 / rp - 2 / (rp + ra)));
            return new Required { orbit = o, targetAlt = Math.Min(o.periKm, 600) * 1000, required = vPeri - Earth.SiteRotationSpeed(latitude) + LOSS_BUDGET, vCirc = vCirc };
        }

        /// <summary>Sim inputs exactly as the web designer's simReadout() builds them.</summary>
        public static (List<SimStage> stages, SimOptions opts) SimInputs(Catalog c, Design d, double dt = 0.2, double sampleEvery = 0)
        {
            var s = SerialStages(c, d); var req = RequiredDeltaV(c, d.payloadOrbit);
            GuidanceProfile guidance = d.guidance != null && d.guidance.Any ? GuidanceProfile.From(d, req.targetAlt) : null;
            FailureMonitor monitor = FailureMonitor.Engages(d) ? FailureMonitor.Create(c, d, req.targetAlt) : null;
            return (s.stages.ConvertAll(x => x.ToSim()), new SimOptions { payload = s.payload, targetAlt = req.targetAlt, latitude = LAUNCH_LATITUDE, dt = dt, sampleEvery = sampleEvery, tMax = 2500, guidance = guidance, monitor = monitor });
        }
    }
}
