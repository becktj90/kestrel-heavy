// The X-ray cutaway (docs/DESIGNER_TIERS_SPEC.md, "X-ray (all tiers)"): the stack from the nose down, each part with its
// plain line (data/designer-tiers.json) and, for Engineer, its numbers. Tanks carry their oxidizer / fuel split by volume
// (propellants.json mixture ratio and densities) and the landing reserve band (Mass.StageMasses landingProp) only when a
// landing is planned (RecoverySim.Offered on the design's recovery mode, or the mission's booster landing). The flight
// plan strip is TimelineGenerator's planned nominal times (Liftoff, Max-Q, MECO, SECO, Landing), sorted by time.
// Pure C#: the Starter panel draws this model; Kestrel.Core.Tests checks it on every mission starter.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using TL = Kestrel.Core.Timeline;

namespace Kestrel.Core.Designer
{
    public enum XrayPartKind { Fairing, Tank, Interstage, Engines, Boosters }

    public sealed class XrayPart
    {
        public string Id, Title = "", Line = "", Numbers = "";
        public XrayPartKind Kind;
        /// <summary>1 is the bottom stage; 0 for the fairing and the boosters.</summary>
        public int Stage;
        /// <summary>Drawn length, metres (tank barrel + domes, interstage, fairing, bell).</summary>
        public double LengthM;
        /// <summary>Tank only: shares of the tank height, top to bottom. Oxidizer + fuel = 1 (solid = 1 on its own);
        /// the landing reserve is the bottom slice of the load (landing propellant / propellant), 0 unless a landing is planned.</summary>
        public double OxShare, FuelShare, ReserveShare;
        public bool Solid;
        /// <summary>kg.</summary>
        public double PropKg, OxKg, FuelKg, ReserveKg, DryKg, CargoKg, ConeKg;
        public int EngineCount;
    }

    public sealed class XrayEvent
    {
        public string Id, Label, Clock;
        /// <summary>Planned seconds after liftoff.</summary>
        public double T;
    }

    public sealed class XrayModel
    {
        public readonly List<XrayPart> Parts = new List<XrayPart>();
        public readonly List<XrayEvent> Events = new List<XrayEvent>();
        public bool LandingPlanned, BoostersLand;
        public string Pick = "", Plan = "";

        public XrayPart Part(string id)
        {
            foreach (var p in Parts) if (p.Id == id) return p;
            return null;
        }
    }

    public static class StarterXray
    {
        /// <summary>The strip's events in data order (sorted by planned time when built).</summary>
        public static readonly IReadOnlyList<string> EventKeys = new[] { "liftoff", "maxq", "meco", "seco", "landing" };

        public static XrayModel Build(StarterRocket r, DesignerTierText text)
            => r == null ? new XrayModel() : Build(r.Catalog, r.BuildDesign(), r.Mission, text);

        public static XrayModel Build(Catalog c, JsonObject json, CampaignMission mission, DesignerTierText text)
        {
            var x = new XrayModel();
            if (c == null || json == null) return x;
            text = text ?? new DesignerTierText();
            x.Pick = text.XrayPick ?? "";
            x.Plan = text.XrayPlan ?? "";
            Design d;
            try { d = Design.FromJson(json); }
            catch (Exception) { return x; }
            if (d.stages == null || d.stages.Count == 0) return x;

            var masses = Mass.StageMasses(c, d);
            string mode = d.recoveryMode;
            x.LandingPlanned = RecoverySim.Offered(mode) && masses.Count > 0 && masses[0].landingProp > 0;
            x.BoostersLand = mission != null && mission.LandBoosters && d.StrapOnCount > 0;

            // Fairing (with its cargo) on top.
            var fc = text.Part("fairing");
            x.Parts.Add(new XrayPart
            {
                Id = "fairing", Kind = XrayPartKind.Fairing, Title = fc.Title, Line = fc.Line,
                LengthM = Math.Max(0.5, d.fairingLength), CargoKg = d.payloadMass, ConeKg = Mass.FairingMass(d),
                Numbers = Fill(text.Numbers("fairing"), ("cargo", T(d.payloadMass)), ("cone", T(Mass.FairingMass(d)))),
            });

            for (int i = d.stages.Count - 1; i >= 0; i--)
            {
                var s = d.stages[i];
                var m = masses[i];
                int n = i + 1;
                c.PropellantById.TryGetValue(s.propellant ?? "", out var prop);
                bool solid = prop != null && prop.storability == "solid";
                bool landing = i == 0 && x.LandingPlanned;
                var tank = new XrayPart
                {
                    Id = "tank-" + n, Kind = XrayPartKind.Tank, Stage = n, Solid = solid,
                    LengthM = Math.Max(0.5, DesignMath.SizeTank(c, s.propellant, s.propMass, s.diameter).length),
                    PropKg = m.prop, DryKg = m.dry, ReserveKg = landing ? m.landingProp : 0,
                };
                Split(prop, m.prop, tank);
                tank.ReserveShare = landing && m.prop > 0 ? Clamp01(m.landingProp / m.prop) : 0;
                var tc = text.Part(solid ? "tankSolid" : landing ? "tankLanding" : "tank");
                tank.Title = Stage(tc.Title, n);
                tank.Line = tc.Line;
                tank.Numbers = Fill(text.Numbers("tank"), ("ox", T(tank.OxKg)), ("fuel", T(tank.FuelKg)), ("dry", T(m.dry)))
                    + (landing ? Fill(text.Numbers("landing"), ("land", T(m.landingProp))) : "");
                x.Parts.Add(tank);

                c.EngineById.TryGetValue(s.engine ?? "", out var eng);
                var ec = text.Part("engines");
                x.Parts.Add(new XrayPart
                {
                    Id = "engines-" + n, Kind = XrayPartKind.Engines, Stage = n, Title = Stage(ec.Title, n), Line = ec.Line,
                    EngineCount = Math.Max(1, s.engineCount), LengthM = Math.Max(0.6, s.diameter * 0.45),
                    DryKg = Mass.EnginesMass(c, s),
                    Numbers = Fill(text.Numbers("engines"), ("count", s.engineCount.ToString(CultureInfo.InvariantCulture)), ("engine", eng?.label ?? s.engine ?? "")),
                });

                if (i > 0)
                {
                    var ic = text.Part("interstage");
                    double isKg = InterstageAero.MassKg(c, d, i);
                    x.Parts.Add(new XrayPart
                    {
                        Id = "interstage-" + i, Kind = XrayPartKind.Interstage, Stage = i, Title = ic.Title, Line = ic.Line,
                        LengthM = Math.Max(0.4, InterstageAero.Length(d.stages[i - 1].diameter, s.diameter)), DryKg = isKg,
                        Numbers = Fill(text.Numbers("interstage"), ("mass", T(isKg))),
                    });
                }
            }

            if (d.StrapOnCount > 0)
            {
                var so = Mass.StrapOnMasses(c, d);
                var bc = text.Part(x.BoostersLand ? "boostersLanding" : "boosters");
                x.Parts.Add(new XrayPart
                {
                    Id = "boosters", Kind = XrayPartKind.Boosters, Title = bc.Title, Line = bc.Line,
                    EngineCount = d.StrapOnCount, PropKg = so.prop, DryKg = so.dry,
                    LengthM = Math.Max(0.5, DesignMath.SizeTank(c, d.strapOns.propellant, d.strapOns.propMass, d.strapOns.diameter).length),
                    Numbers = Fill(text.Numbers("boosters"), ("count", d.StrapOnCount.ToString(CultureInfo.InvariantCulture)), ("prop", T(so.eachProp))),
                });
            }

            Plan(c, d, x, text);
            return x;
        }

        /// <summary>Oxidizer above fuel, split by volume: mass ox = prop * MR / (1 + MR). Solids are one grain.</summary>
        static void Split(Propellant p, double propKg, XrayPart t)
        {
            if (p == null || t.Solid || !(p.mixtureRatio > 0) || !(p.fuelDensity > 0) || !(p.oxDensity > 0))
            {
                t.FuelKg = propKg; t.OxKg = 0; t.FuelShare = 1; t.OxShare = 0;
                return;
            }
            double mr = p.mixtureRatio.Value;
            t.OxKg = propKg * mr / (1 + mr);
            t.FuelKg = propKg - t.OxKg;
            double vo = t.OxKg / p.oxDensity, vf = t.FuelKg / p.fuelDensity;
            double v = vo + vf;
            if (!(v > 0)) { t.FuelShare = 0.5; t.OxShare = 0.5; return; }
            t.OxShare = vo / v;
            t.FuelShare = 1 - t.OxShare;
        }

        static void Plan(Catalog c, Design d, XrayModel x, DesignerTierText text)
        {
            TL.Timeline tl;
            try
            {
                string rec = !x.LandingPlanned ? "expendable" : d.recoveryMode == "barge" ? "droneship" : "rtls";
                tl = TL.TimelineGenerator.Generate(c, d, TL.Pad.For(c, d), new TL.Mission { mode = "standard", recovery = rec }, 1);
            }
            catch (Exception) { return; }
            if (tl?.events == null) return;
            double? liftoff = null, maxq = null, meco = null, seco = null, landing = null;
            foreach (var e in tl.events)
            {
                if (e?.id == null) continue;
                if (e.id == "term.liftoff") liftoff = e.nominal;
                else if (e.id == "asc.maxq") maxq = e.nominal;
                else if (e.id == "asc.meco" || e.id == "asc.burnout") meco = meco.HasValue ? Math.Min(meco.Value, e.nominal) : e.nominal;
                else if ((e.id.StartsWith("stg.seco.", StringComparison.Ordinal) && e.id.EndsWith(".1", StringComparison.Ordinal))
                         || e.id.StartsWith("stg.cutoff.", StringComparison.Ordinal))
                    seco = seco.HasValue ? Math.Max(seco.Value, e.nominal) : e.nominal;
                else if (e.id == "rec.landing" && x.LandingPlanned) landing = e.nominal;
            }
            if (!liftoff.HasValue) return;
            Add(x, text, "liftoff", 0);
            if (maxq.HasValue) Add(x, text, "maxq", maxq.Value - liftoff.Value);
            if (meco.HasValue) Add(x, text, "meco", meco.Value - liftoff.Value);
            if (seco.HasValue) Add(x, text, "seco", seco.Value - liftoff.Value);
            if (landing.HasValue) Add(x, text, "landing", landing.Value - liftoff.Value);
            x.Events.Sort((a, b) => a.T.CompareTo(b.T));
        }

        static void Add(XrayModel x, DesignerTierText text, string key, double t)
        {
            if (double.IsNaN(t) || double.IsInfinity(t) || t < 0) return;
            string label = text.Event(key);
            if (string.IsNullOrEmpty(label)) return;
            x.Events.Add(new XrayEvent { Id = key, Label = label, T = t, Clock = Clock(t) });
        }

        /// <summary>"T+2:31": whole seconds after liftoff.</summary>
        public static string Clock(double t)
        {
            int s = (int)Math.Round(Math.Max(0, t), MidpointRounding.AwayFromZero);
            int h = s / 3600, m = (s % 3600) / 60, sec = s % 60;
            var inv = CultureInfo.InvariantCulture;
            return h > 0 ? "T+" + h.ToString(inv) + ":" + m.ToString("00", inv) + ":" + sec.ToString("00", inv)
                         : "T+" + m.ToString(inv) + ":" + sec.ToString("00", inv);
        }

        static string Stage(string template, int n) => (template ?? "").Replace("{n}", n.ToString(CultureInfo.InvariantCulture));

        static string T(double kg) => (kg / 1000.0).ToString(kg >= 10000 ? "0" : "0.0", CultureInfo.InvariantCulture);

        static string Fill(string template, params (string key, string value)[] values)
        {
            string s = template ?? "";
            foreach (var (k, v) in values) s = s.Replace("{" + k + "}", v ?? "");
            return s;
        }

        static double Clamp01(double v) => double.IsNaN(v) ? 0 : v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
