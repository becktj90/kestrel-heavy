// Home LAUNCH picks a starter that fits the next mission, then the pad flies that rocket.
// A shortfall is a warning: the briefing says so, and the flight can still miss.
// TWR under 1 stays a block, so that rocket opens in the designer instead of the pad.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    /// <summary>One ranked starter. The design is a fork; the template document is left alone.</summary>
    public sealed class AutoBuildChoice
    {
        public string TemplateId;
        public string TemplateName;
        public int TemplateOrder;
        public JsonObject Design;
        public bool FitsPayload;
        public bool ReachesOrbit;
        public bool LaunchEnabled;
        public double LiftoffMassKg;
        public double ThrustN;
        public double DeltaV;
        public double RequiredMps;
        public double MarginMps;
        public double LiftoffTwr;
        public double FitScore;
        public string VehicleLine;
        public string ReachLine;
    }

    public static class HomeAutoBuild
    {
        public const double LatitudeDeg = Stack.LAUNCH_LATITUDE;

        public static string OrbitId(string missionId) => "kh-" + (missionId ?? "");

        /// <summary>Catalog key for this mission. Mission data wins; older builds stamped kh- plus the id.</summary>
        public static string OrbitKey(CampaignMission mission)
        {
            if (mission == null) return "";
            if (!string.IsNullOrEmpty(mission.OrbitId)) return mission.OrbitId;
            return OrbitId(mission.Id);
        }

        /// <summary>
        /// Put every campaign target on the catalog. A design opened later, on a catalog Choose never touched,
        /// still resolves payload.orbit. Existing catalog rows are left as authored.
        /// </summary>
        public static void InstallMissionOrbits(Catalog catalog, Campaign campaign)
        {
            if (catalog == null || campaign == null) return;
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var mission = campaign.Missions[i];
                if (mission == null) continue;
                var built = ToOrbit(mission);
                if (string.IsNullOrEmpty(built.id)) continue;
                if (!catalog.Orbits.ContainsKey(built.id)) catalog.Orbits[built.id] = built;
                string legacy = OrbitId(mission.Id);
                if (!string.IsNullOrEmpty(legacy) && legacy != built.id && !catalog.Orbits.ContainsKey(legacy))
                {
                    catalog.Orbits[legacy] = new OrbitTarget
                    {
                        id = legacy,
                        label = built.label,
                        apoKm = built.apoKm,
                        periKm = built.periKm,
                        incDeg = built.incDeg,
                    };
                }
            }
        }

        /// <summary>
        /// Register the mission target on the catalog so Stack, Readout and the flight agree.
        /// Apogee uses a negative periapsis (Stack's suborbital flag). C3 becomes the matching ellipse.
        /// </summary>
        public static OrbitTarget EnsureOrbit(Catalog catalog, CampaignMission mission)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (mission == null) throw new ArgumentNullException(nameof(mission));
            var orbit = ToOrbit(mission);
            catalog.Orbits[orbit.id] = orbit;
            return orbit;
        }

        public static OrbitTarget ToOrbit(CampaignMission mission)
        {
            string id = OrbitKey(mission);
            string kind = mission.TargetKind ?? "";
            if (kind == "apogee")
                return new OrbitTarget { id = id, label = mission.Name ?? id, apoKm = mission.ApoKm, periKm = -1, incDeg = 0 };
            if (kind == "c3")
            {
                double apo = ApoKmForC3(mission.PeriKm, mission.C3Km2PerS2);
                return new OrbitTarget { id = id, label = mission.Name ?? id, apoKm = apo, periKm = mission.PeriKm, incDeg = mission.IncDeg };
            }
            double peri = mission.PeriKm;
            double apoKm = mission.ApoKm;
            if (peri <= 0) peri = apoKm;
            if (apoKm <= 0) apoKm = peri;
            return new OrbitTarget { id = id, label = mission.Name ?? id, apoKm = apoKm, periKm = peri, incDeg = mission.IncDeg };
        }

        /// <summary>Semi-major axis from specific energy. A non-positive C3 is bound; zero C3 stays circular at periapsis.</summary>
        public static double ApoKmForC3(double periKm, double c3Km2PerS2)
        {
            double rp = Gravity.R_EARTH + periKm * 1000;
            if (!(c3Km2PerS2 < 0)) return periKm;
            double c3 = c3Km2PerS2 * 1e6;
            double a = -Gravity.MU_EARTH / c3;
            double ra = 2 * a - rp;
            if (!(ra > rp)) return periKm;
            return (ra - Gravity.R_EARTH) / 1000;
        }

        /// <summary>
        /// Best starter for this mission. Prefers a fit that also reaches, lightest first.
        /// Otherwise the best fit, then the least-bad margin. Always a real fork when a template exists.
        /// </summary>
        public static AutoBuildChoice Choose(Catalog catalog, IReadOnlyList<StarterTemplate> templates, CampaignMission mission)
        {
            if (catalog == null || mission == null || templates == null || templates.Count == 0) return null;
            EnsureOrbit(catalog, mission);
            AutoBuildChoice best = null;
            for (int i = 0; i < templates.Count; i++)
            {
                var t = templates[i];
                if (t == null || t.design == null) continue;
                var choice = Score(catalog, t, mission);
                if (choice == null) continue;
                if (best == null || Better(choice, best)) best = choice;
            }
            if (best != null && !best.ReachesOrbit && best.Design != null)
                best = RepairShortfall(catalog, templates, mission) ?? best;
            return best;
        }

        /// <summary>
        /// None of the starters reach. Try the same tank, stage and engine moves as Fix it,
        /// and keep the lightest repair that finishes with margin at least zero.
        /// </summary>
        static AutoBuildChoice RepairShortfall(Catalog catalog, IReadOnlyList<StarterTemplate> templates, CampaignMission mission)
        {
            AutoBuildChoice repaired = null;
            for (int i = 0; i < templates.Count; i++)
            {
                var t = templates[i];
                if (t == null || t.design == null) continue;
                JsonObject design;
                try { design = HangarEditor.ForkTemplate(t); }
                catch (Exception) { continue; }
                ApplyMission(catalog, design, mission);
                var fix = DvMarginFix.Solve(catalog, design);
                if (fix == null || !fix.Solved || fix.Design == null) continue;
                var choice = new AutoBuildChoice
                {
                    TemplateId = t.id,
                    TemplateName = t.name,
                    TemplateOrder = t.order,
                    Design = fix.Design,
                };
                try
                {
                    var spec = new PayloadSpec { id = mission.PayloadId, volume = mission.VolumeM3, diameter = mission.DiameterM };
                    var fit = PayloadFit.Check(Design.FromJson(fix.Design), spec);
                    choice.FitsPayload = fit.fits;
                    choice.FitScore = fit.volumeMargin + fit.diameterMargin;
                }
                catch (Exception)
                {
                    choice.FitsPayload = false;
                    choice.FitScore = double.NegativeInfinity;
                }
                FillPerformance(catalog, choice);
                if (!choice.ReachesOrbit) continue;
                if (repaired == null || Better(choice, repaired)) repaired = choice;
            }
            return repaired;
        }

        public static string VehicleLine(string name, double liftoffKg, double thrustN, double deltaV)
        {
            string who = string.IsNullOrEmpty(name) ? "Rocket" : name;
            return who + ". " + MassText(liftoffKg) + " liftoff. " + ThrustText(thrustN) + ". " + Math.Round(deltaV).ToString("0", CultureInfo.InvariantCulture) + " m/s.";
        }

        public static string ReachLine(double marginMps)
        {
            if (marginMps < 0)
                return "Short by " + Math.Round(-marginMps).ToString("0", CultureInfo.InvariantCulture) + " m/s. This one can miss.";
            if (marginMps < 150) return "Just enough.";
            return "Enough for this orbit.";
        }

        static AutoBuildChoice Score(Catalog catalog, StarterTemplate template, CampaignMission mission)
        {
            JsonObject design;
            try { design = HangarEditor.ForkTemplate(template); }
            catch (Exception) { return null; }
            ApplyMission(catalog, design, mission);
            var choice = new AutoBuildChoice
            {
                TemplateId = template.id,
                TemplateName = template.name,
                TemplateOrder = template.order,
                Design = design,
            };
            try
            {
                var spec = new PayloadSpec { id = mission.PayloadId, volume = mission.VolumeM3, diameter = mission.DiameterM };
                var fit = PayloadFit.Check(Design.FromJson(design), spec);
                choice.FitsPayload = fit.fits;
                choice.FitScore = fit.volumeMargin + fit.diameterMargin;
            }
            catch (Exception)
            {
                choice.FitsPayload = false;
                choice.FitScore = double.NegativeInfinity;
            }
            FillPerformance(catalog, choice);
            return choice;
        }

        static void FillPerformance(Catalog catalog, AutoBuildChoice choice)
        {
            var design = choice.Design;
            try
            {
                var q = Readout.Quick(catalog, design);
                choice.LiftoffMassKg = q.liftoffMass;
                choice.DeltaV = q.totalDeltaV;
                choice.RequiredMps = q.required;
                choice.MarginMps = q.totalDeltaV - q.required;
                choice.LiftoffTwr = q.liftoffTWR;
                choice.ReachesOrbit = choice.MarginMps >= 0 && !double.IsNaN(choice.MarginMps);
                choice.ThrustN = 0;
                if (q.stages != null && q.stages.Count > 0)
                    choice.ThrustN = q.stages[0].twr * q.stages[0].m0 * Atmosphere.G0;
            }
            catch (Exception)
            {
                choice.ReachesOrbit = false;
                choice.MarginMps = double.NegativeInfinity;
                choice.FitScore = double.IsNegativeInfinity(choice.FitScore) ? choice.FitScore : choice.FitScore - 1e9;
            }
            try
            {
                var report = Validate.Engineering(catalog, design);
                choice.LaunchEnabled = report != null && report.ok && report.blocks.Count == 0;
            }
            catch (Exception) { choice.LaunchEnabled = false; }
            choice.VehicleLine = VehicleLine(choice.TemplateName, choice.LiftoffMassKg, choice.ThrustN, choice.DeltaV);
            choice.ReachLine = ReachLine(choice.MarginMps);
        }

        public static void ApplyMission(Catalog catalog, JsonObject design, CampaignMission mission)
        {
            string orbit = OrbitKey(mission);
            if (!string.IsNullOrEmpty(mission.PayloadId))
                HangarEditor.SetPayloadType(catalog, design, mission.PayloadId);
            var payload = design.Obj("payload");
            if (payload == null)
            {
                payload = new JsonObject();
                JsonMut.Set(design, "payload", payload);
            }
            if (!string.IsNullOrEmpty(mission.PayloadId)) JsonMut.Set(payload, "type", mission.PayloadId);
            HangarEditor.SetPayloadMass(design, mission.MassKg);
            if (!catalog.Orbits.ContainsKey(orbit)) EnsureOrbit(catalog, mission);
            HangarEditor.SetOrbit(catalog, design, orbit);
            if (design.Obj("payload")?.Str("orbit") != orbit) JsonMut.Set(design.Obj("payload"), "orbit", orbit);
            if (Math.Abs(design.Obj("payload").Num("mass") - mission.MassKg) > 0.5 && mission.MassKg > 0)
                JsonMut.Set(design.Obj("payload"), "mass", mission.MassKg);
            JsonMut.Set(design, "missionId", mission.Id ?? "");
            if (mission.BudgetKc > 0) JsonMut.Set(design, "budgetKc", mission.BudgetKc);
        }

        static int Tier(AutoBuildChoice c)
        {
            if (c.FitsPayload && c.ReachesOrbit) return 0;
            if (c.FitsPayload) return 1;
            return 2;
        }

        static bool Better(AutoBuildChoice a, AutoBuildChoice b)
        {
            int ta = Tier(a), tb = Tier(b);
            if (ta != tb) return ta < tb;
            if (ta == 0)
            {
                if (a.LiftoffMassKg != b.LiftoffMassKg) return a.LiftoffMassKg < b.LiftoffMassKg;
                return a.TemplateOrder < b.TemplateOrder;
            }
            if (ta == 1)
            {
                if (a.MarginMps != b.MarginMps) return a.MarginMps > b.MarginMps;
                return a.TemplateOrder < b.TemplateOrder;
            }
            if (a.FitScore != b.FitScore) return a.FitScore > b.FitScore;
            if (a.MarginMps != b.MarginMps) return a.MarginMps > b.MarginMps;
            return a.TemplateOrder < b.TemplateOrder;
        }

        static string MassText(double kg)
        {
            if (double.IsNaN(kg) || double.IsInfinity(kg) || kg <= 0) return "0 kg";
            if (kg >= 1000) return (kg / 1000).ToString("0.0", CultureInfo.InvariantCulture) + " t";
            return Math.Round(kg).ToString("0", CultureInfo.InvariantCulture) + " kg";
        }

        static string ThrustText(double newton)
        {
            if (double.IsNaN(newton) || double.IsInfinity(newton) || newton <= 0) return "0 kN";
            double kn = newton / 1000;
            string n = kn >= 10 ? kn.ToString("0", CultureInfo.InvariantCulture) : kn.ToString("0.0", CultureInfo.InvariantCulture);
            return n + " kN";
        }
    }

    /// <summary>Design the pad should open. Consumed once so a later visit does not relaunch.</summary>
    public static class PadHandoff
    {
        public static JsonObject Pending { get; private set; }
        public static bool FlyOnPad { get; private set; }
        public static bool FromHome { get; private set; }

        public static void Arm(JsonObject design, bool flyNow, bool fromHome)
        {
            Pending = design;
            FlyOnPad = flyNow && design != null;
            FromHome = fromHome && design != null;
        }

        public static bool TryTake(out JsonObject design, out bool flyNow)
        {
            design = Pending;
            flyNow = FlyOnPad && design != null;
            if (design == null) return false;
            Pending = null;
            FlyOnPad = false;
            return true;
        }

        public static void Clear()
        {
            Pending = null;
            FlyOnPad = false;
            FromHome = false;
        }
    }

    /// <summary>
    /// Home buttons. Does not change <see cref="GameSession.Apply"/> for <see cref="RouteId.HomeLaunch"/>.
    /// LAUNCH briefs the next mission and arms the pad. Design first opens the designer with the same load.
    /// </summary>
    public static class HomeRoutes
    {
        public static CampaignMission NextMission(Campaign campaign, int cleared)
        {
            if (campaign == null || campaign.Missions == null || campaign.Missions.Count == 0) return null;
            int index = Math.Max(0, cleared) + 1;
            var open = campaign.Mission(index);
            if (open != null) return open;
            CampaignMission last = null;
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var m = campaign.Missions[i];
                if (last == null || m.Index >= last.Index) last = m;
            }
            return last;
        }

        public static RouteResult Launch(Campaign campaign, int cleared, Catalog catalog, IReadOnlyList<StarterTemplate> templates, MissionBriefing briefing = null)
        {
            var mission = NextMission(campaign, cleared);
            var choice = mission == null ? null : HomeAutoBuild.Choose(catalog, templates, mission);
            if (choice == null || choice.Design == null)
            {
                PadHandoff.Clear();
                GameSession.Clear();
                return GameSession.Apply(RouteId.HomeLaunch);
            }
            var ctx = MissionContext.FromMission(campaign, mission);
            if (ctx == null)
            {
                PadHandoff.Clear();
                GameSession.Clear();
                return GameSession.Apply(RouteId.HomeLaunch);
            }
            ctx.ApplyBriefing(briefing);
            ctx.VehicleLine = choice.VehicleLine;
            ctx.ReachLine = choice.ReachLine;
            ctx.AutoPad = choice.LaunchEnabled;
            PadHandoff.Arm(choice.Design, choice.LaunchEnabled, fromHome: true);
            GameSession.Carry(ctx);
            return new RouteResult(SessionScreens.Missions, SessionScreens.Briefing, ctx);
        }

        public static RouteResult DesignFirst(Campaign campaign, int cleared, Catalog catalog, IReadOnlyList<StarterTemplate> templates)
        {
            var mission = NextMission(campaign, cleared);
            var choice = mission == null ? null : HomeAutoBuild.Choose(catalog, templates, mission);
            if (choice == null || choice.Design == null)
            {
                PadHandoff.Clear();
                return GameSession.Apply(RouteId.HomeHangar);
            }
            var ctx = MissionContext.FromMission(campaign, mission);
            if (ctx == null)
            {
                PadHandoff.Clear();
                return GameSession.Apply(RouteId.HomeHangar);
            }
            ctx.VehicleLine = choice.VehicleLine;
            ctx.ReachLine = choice.ReachLine;
            ctx.AutoPad = false;
            PadHandoff.Arm(choice.Design, flyNow: false, fromHome: false);
            GameSession.Carry(ctx);
            return new RouteResult(SessionScreens.Design, SessionScreens.None, ctx);
        }
    }
}
