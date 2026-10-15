// Per-step gates. Numbers come from Readout.Quick. Block and warn rows come from Advisories.
// Review's red list is the same filter DesignerV3Risk.Reds uses on FlightRisk.level.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer.Stepper
{
    public static class StepperChecks
    {
        public const double PassTwr = 1.3;
        public const double BlockTwr = 1.0;
        /// <summary>Amber when peak dynamic pressure is above this fraction of q_lim. Equal to the fraction still passes.</summary>
        public const double MaxQAmberFraction = 0.85;

        public static StepCheck Mission(Design design)
        {
            var check = Blank(DesignerStep.Mission, "payload");
            if (design == null || string.IsNullOrEmpty(design.payloadType))
                return Set(check, StepGate.Red, "payload-missing", "payload.type", "payload",
                    "Pick a payload first. The cargo decides the rocket.",
                    "payload.type is empty.");
            if (string.IsNullOrEmpty(design.payloadOrbit))
                return Set(check, StepGate.Red, "orbit-missing", "payload.orbit", "payload",
                    "Pick where this payload is going.",
                    "payload.orbit is empty.");
            check.Gate = StepGate.Pass;
            check.Kid = "The job is set. Next, build a rocket that can do it.";
            check.Engineer = "Payload " + design.payloadType + " to " + design.payloadOrbit + ". A free build counts as a mission.";
            return check;
        }

        public static StepCheck Engines(Catalog catalog, Design design, QuickReadout readout, IList<Advisory> advisories)
        {
            var check = Blank(DesignerStep.Engines, "engines");
            if (readout == null || !Finite(readout.liftoffTWR))
            {
                Set(check, StepGate.Red, "math", "engineCount", "engines",
                    "We couldn't work out the liftoff push.",
                    "Readout.Quick did not return a liftoff TWR.");
            }
            else if (readout.liftoffTWR < BlockTwr)
            {
                Set(check, StepGate.Red, "twr-low", "engineCount", "engines",
                    "Too heavy to lift off. Add engines on the bottom, or take fuel off.",
                    "Liftoff TWR " + F(readout.liftoffTWR, "0.00") + " < 1.0. It must clear 1 to leave the pad.");
                check.Value = readout.liftoffTWR;
                check.Limit = BlockTwr;
            }
            else if (readout.liftoffTWR < PassTwr)
            {
                Set(check, StepGate.Amber, "twr-marginal", "engineCount", "engines",
                    "Slow off the pad. A bit more push helps.",
                    "Liftoff TWR " + F(readout.liftoffTWR, "0.00") + " is under 1.3.");
                check.Value = readout.liftoffTWR;
                check.Limit = PassTwr;
            }
            else
            {
                check.Gate = StepGate.Pass;
                check.Value = readout.liftoffTWR;
                check.Limit = PassTwr;
                check.Kid = "It can lift off.";
                check.Engineer = "Liftoff TWR " + F(readout.liftoffTWR, "0.00") + " ≥ 1.3.";
            }

            double mix = MaxMixture(design);
            if (mix > MixtureOffset.HazardPct)
            {
                if (check.Gate == StepGate.Pass)
                {
                    Set(check, StepGate.Amber, "bad-mixture", "mixtureOffsetPct", "propellants",
                        "The fuel mix is too far off. Put it back near the normal mix.",
                        "Mixture offset " + F(mix, "0.0") + "% is outside ±" + F(MixtureOffset.HazardPct, "0") + "%.");
                    check.Value = mix;
                    check.Limit = MixtureOffset.HazardPct;
                }
                else
                    check.Findings.Add(Finding("bad-mixture", "mixtureOffsetPct", "propellants", "amber",
                        "The fuel mix is too far off.",
                        "Mixture offset " + F(mix, "0.0") + "% is outside ±6%.", WorstMixtureStage(design)));
            }

            if (Overpressure(catalog, design, out int stage))
            {
                if (check.Gate == StepGate.Pass)
                    Set(check, StepGate.Amber, "engine-overpressure", "engineParams.pc", "engines",
                        "The chamber pressure is high. That can pop an engine.",
                        "pc is above pc_rated. Hazard λ = 0.02·max(0, pc/pc_rated−1)² per second.");
                else
                    check.Findings.Add(Finding("engine-overpressure", "engineParams.pc", "engines", "amber",
                        "The chamber pressure is high.",
                        "pc is above pc_rated.", stage));
            }

            ApplyAdvisories(check, advisories, DesignerStep.Engines, true);
            return check;
        }

        public static StepCheck Tanks(QuickReadout readout, IList<Advisory> advisories)
        {
            var check = Blank(DesignerStep.Tanks, "stages");
            if (readout == null || !Finite(readout.margin))
            {
                Set(check, StepGate.Red, "math", "propMass", "propellants",
                    "We couldn't work out the speed for this trip.",
                    "Readout.Quick did not return a Δv margin.");
            }
            else if (readout.margin < 0)
            {
                double shortfall = readout.required > 0 ? -readout.margin * readout.required : 0;
                Set(check, StepGate.Red, "dv-margin", "propMass", "propellants",
                    "Not enough speed for this trip. Add fuel or lighten the cargo.",
                    "Δv margin " + F(readout.margin * 100, "0.0") + "% (" + F(shortfall, "0") + " m/s short of " + F(readout.required, "0") + " m/s).");
                check.Value = readout.margin;
                check.Limit = 0;
            }
            else
            {
                check.Gate = StepGate.Pass;
                check.Value = readout.margin;
                check.Limit = 0;
                check.Kid = "You have enough speed for this trip.";
                check.Engineer = "Δv margin " + F(readout.margin * 100, "0.0") + "% ≥ 0. total " + F(readout.totalDeltaV, "0") + " m/s, required " + F(readout.required, "0") + " m/s.";
                if (MaxQHot(readout))
                {
                    Set(check, StepGate.Amber, "maxq-high", "safetyFactor", "structure",
                        "The air pushes hard on the tanks. A thicker wall or less push low down helps.",
                        "max-Q " + F(readout.maxQ_kPa.Value, "0.0") + " kPa > 0.85·q_lim " + F(readout.qLim_kPa.Value, "0.0") + " kPa.");
                    check.Value = readout.maxQ_kPa.Value;
                    check.Limit = MaxQAmberFraction * readout.qLim_kPa.Value;
                }
                else if (readout.maxQStale)
                    check.Stale = true;
            }
            ApplyAdvisories(check, advisories, DesignerStep.Tanks, true);
            return check;
        }

        public static StepCheck Aero(Catalog catalog, Design design, QuickReadout readout, IList<Advisory> advisories)
        {
            var check = Blank(DesignerStep.Aero, "aero");
            if (readout == null)
            {
                Set(check, StepGate.Red, "math", "fairing", "aero",
                    "We couldn't check the nose or the balance.",
                    "Readout.Quick did not return a fairing or stability result.");
            }
            else if (readout.fairingFits == false || (readout.fairingFits == null && design != null && !string.IsNullOrEmpty(design.payloadType)))
            {
                Set(check, StepGate.Red, "payload-doesnt-fit", "fairing", "payload",
                    "The cargo doesn't fit in the nose. Make the nose longer or wider.",
                    "Fairing fit failed: volume ≤ π(0.45·D_f)²·0.8·L_f and diameter + 0.2 m ≤ D_f.");
            }
            else if (readout.fairingFits == null)
            {
                Set(check, StepGate.Red, "payload-missing", "payload.type", "payload",
                    "Pick a payload before the nose can be checked.",
                    "payload.type is empty, so fairing fit is unknown.");
            }
            else if (Tumble(catalog, design, readout, out string tumbleCode, out string tumbleEngineer))
            {
                Set(check, StepGate.Red, tumbleCode, "fins", "aero",
                    "Your rocket would flip. Add fins, or put the heavy parts higher.",
                    tumbleEngineer);
            }
            else
            {
                check.Gate = StepGate.Pass;
                check.Kid = "The cargo fits, and the rocket stays pointy-end up.";
                check.Engineer = "Fairing fits. Liftoff stability " + F(readout.stabilityCal, "0.00") + " cal.";
                if (readout.minStability.HasValue) check.Value = readout.minStability.Value;
                else check.Value = readout.stabilityCal;
                if (!readout.minCar.HasValue || readout.maxQStale) check.Stale = readout.maxQStale;
            }
            ApplyAdvisories(check, advisories, DesignerStep.Aero, true);
            return check;
        }

        public static StepCheck Recovery(Design design, JsonObject raw, QuickReadout readout, bool landingRequired, IList<Advisory> advisories)
        {
            var check = Blank(DesignerStep.Recovery, "recovery");
            if (!landingRequired)
            {
                Set(check, StepGate.Skip, "recovery-skip", "recoveryMode", "recovery",
                    "This flight does not need a landing. Recovery is skipped.",
                    "Mission does not require landing, so this step auto-skips.");
                ApplyAdvisories(check, advisories, DesignerStep.Recovery, false);
                return check;
            }

            string mode = RecoveryMode(raw, design);
            if (mode == "expendable")
            {
                Set(check, StepGate.Red, "landing-required", "recoveryMode", "recovery",
                    "This mission needs the booster back. Pick a pad or a barge.",
                    "Landing mode is expendable, but the mission requires a landing.");
                ApplyAdvisories(check, advisories, DesignerStep.Recovery, true);
                return check;
            }

            double reserve = readout != null && Finite(readout.landingReserve_mps) ? readout.landingReserve_mps : 0;
            double? need = LandingNeed(design, mode, readout);
            check.Value = reserve;
            if (need == null)
            {
                Set(check, StepGate.Amber, "landing-need-stale", "landingReserve", "recovery",
                    "We don't know the landing fuel yet. You can go on, then check it again.",
                    "Boost-back Δv is unknown until the reference ascent caches MECO speed. RTLS ≈ 2·v_h, barge ≈ 0.3·v_h.");
                check.Stale = true;
            }
            else if (reserve < need.Value)
            {
                Set(check, StepGate.Red, "landing-reserve", "landingReserve", "recovery",
                    "Not enough fuel saved to land. Hold more back, or land closer.",
                    "Landing reserve " + F(reserve, "0") + " m/s < need " + F(need.Value, "0") + " m/s (" + mode + ").");
                check.Limit = need.Value;
            }
            else
            {
                check.Gate = StepGate.Pass;
                check.Limit = need.Value;
                check.Kid = "The landing fuel covers the trip home.";
                check.Engineer = "Landing reserve " + F(reserve, "0") + " m/s ≥ need " + F(need.Value, "0") + " m/s (" + mode + ").";
                if (design == null || !design.legs)
                    Set(check, StepGate.Amber, "landing-hardware", "legs", "recovery",
                        "Saved landing fuel, but there are no legs. Add legs, or the booster falls over.",
                        "Landing is required and landing.legs is false.");
            }
            ApplyAdvisories(check, advisories, DesignerStep.Recovery, true);
            return check;
        }

        public static StepCheck Look()
        {
            var check = Blank(DesignerStep.Look, "look");
            check.Gate = StepGate.Pass;
            check.Kid = "Paint and a name. This does not change how it flies.";
            check.Engineer = "Livery has no physics.";
            return check;
        }

        public static StepCheck Review(QuickReadout readout, bool confirmArmed, out List<FlightRisk> reds, out bool hasRed)
        {
            var check = Blank(DesignerStep.Review, "stages");
            reds = StepperLaunch.Reds(readout?.risks);
            hasRed = reds.Count > 0 || readout?.predictedFailure != null;
            bool stale = readout == null || readout.risks == null;
            check.Stale = stale && !hasRed;
            if (!hasRed)
            {
                check.Gate = StepGate.Pass;
                check.Code = stale ? "risks-stale" : null;
                check.Kid = stale ? "The risk check has not finished. Nothing red is on the list yet." : "No red risks. Ready to launch.";
                check.Engineer = stale ? "Reference ascent is not cached, so the FlightRisk list is empty." : "No FlightRisk with level red.";
                return check;
            }

            FlightRisk first = reds.Count > 0 ? reds[0] : null;
            string kind = first != null ? first.kind : readout.predictedFailure.kind;
            check.Code = string.IsNullOrEmpty(kind) ? "red-risk" : kind;
            check.Field = first != null && !string.IsNullOrEmpty(first.field) ? first.field : "stages";
            check.Tab = first != null && !string.IsNullOrEmpty(first.tab) ? first.tab : "stages";
            check.Kid = first != null && !string.IsNullOrEmpty(first.kid) ? first.kid : "This rocket has a red risk. Look before you launch.";
            check.Engineer = first != null && !string.IsNullOrEmpty(first.engineer) ? first.engineer : StepperLaunch.ConfirmTitle;
            check.Gate = confirmArmed ? StepGate.Pass : StepGate.Amber;
            return check;
        }

        public static bool MissionNeedsLanding(JsonObject design, StepperContext context)
        {
            if (context != null && context.LandingRequired.HasValue) return context.LandingRequired.Value;
            var mission = design?.Obj("mission");
            if (mission != null && mission.ContainsKey("requiresLanding") && mission["requiresLanding"] is bool flag)
                return flag;
            string id = design?.Str("missionId");
            if (string.IsNullOrEmpty(id)) id = mission?.Str("id");
            if (context?.Campaign != null && !string.IsNullOrEmpty(id))
            {
                var found = context.Campaign.ById(id);
                if (found != null) return found.LandingRequired;
            }
            return false;
        }

        /// <summary>recovery.mode, else the top-level recoveryMode string the v3 editor writes. Pad maps to RTLS.</summary>
        public static string RecoveryMode(JsonObject design, Design parsed)
        {
            string mode = null;
            var rec = design?.Obj("recovery");
            if (rec != null && rec.ContainsKey("mode")) mode = rec.Str("mode");
            if (string.IsNullOrEmpty(mode)) mode = design?.Str("recoveryMode");
            if (string.IsNullOrEmpty(mode)) mode = parsed?.recoveryMode;
            return CanonicalMode(mode);
        }

        public static string CanonicalMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return "expendable";
            switch (mode.Trim().ToLowerInvariant())
            {
                case "rtls":
                case "pad":
                case "land":
                case "landing":
                    return "rtls";
                case "barge":
                case "haven":
                    return "barge";
                default:
                    return "expendable";
            }
        }

        public static DesignerStep? StepForAdvisory(Advisory advisory)
        {
            if (advisory == null || string.IsNullOrEmpty(advisory.code)) return null;
            switch (advisory.code)
            {
                case "zero-engines":
                case "twr-low":
                case "twr-marginal":
                case "engines-dont-fit":
                case "vac-engine-pad":
                case "cluster-count":
                case "cluster-overhang":
                case "nozzle-unknown":
                case "engine-out":
                case "weak-upper":
                    return DesignerStep.Engines;
                case "zero-propellant":
                case "schema":
                case "math":
                case "top-heavy":
                case "weak-upper-dv":
                case "dv-margin":
                case "cryo-composite":
                case "interstage-shape":
                    return DesignerStep.Tanks;
                case "fairing-narrow":
                case "payload-doesnt-fit":
                case "heavy-fairing":
                case "unstable":
                case "overstable":
                case "payload-g":
                    return DesignerStep.Aero;
                case "landing-hardware":
                case "rcs-for-landing":
                    return DesignerStep.Recovery;
                case "maxq-high":
                    return DesignerStep.Tanks;
                case "over-budget":
                case "near-budget":
                    return null;
            }
            switch (advisory.tab)
            {
                case "engines":
                case "propellants":
                    return DesignerStep.Engines;
                case "stages":
                case "tanks":
                case "structure":
                    return DesignerStep.Tanks;
                case "aero":
                case "guidance":
                    return DesignerStep.Aero;
                case "recovery":
                    return DesignerStep.Recovery;
                case "payload":
                    return DesignerStep.Mission;
                default:
                    return null;
            }
        }

        public static string FieldFor(string code)
        {
            switch (code)
            {
                case "payload-missing": return "payload.type";
                case "orbit-missing": return "payload.orbit";
                case "twr-low":
                case "twr-marginal":
                case "zero-engines":
                case "engine-out":
                case "engines-dont-fit":
                case "cluster-count":
                case "cluster-overhang":
                case "vac-engine-pad":
                case "weak-upper":
                    return "engineCount";
                case "nozzle-unknown": return "nozzle";
                case "bad-mixture": return "mixtureOffsetPct";
                case "engine-overpressure": return "engineParams.pc";
                case "zero-propellant":
                case "dv-margin":
                case "weak-upper-dv":
                    return "propMass";
                case "cryo-composite": return "material";
                case "maxq-high": return "safetyFactor";
                case "top-heavy":
                case "interstage-shape":
                case "schema":
                    return "stages";
                case "payload-doesnt-fit":
                case "fairing-narrow":
                case "heavy-fairing":
                    return "fairing";
                case "unstable":
                case "overstable":
                case "tumble":
                    return "fins";
                case "payload-g": return "gLimit";
                case "landing-required": return "recoveryMode";
                case "landing-reserve":
                case "landing-need-stale":
                    return "landingReserve";
                case "landing-hardware":
                case "rcs-for-landing":
                    return "legs";
                default: return null;
            }
        }

        static bool MaxQHot(QuickReadout readout)
        {
            if (readout == null || readout.maxQStale || !readout.maxQ_kPa.HasValue || !readout.qLim_kPa.HasValue) return false;
            if (!(readout.qLim_kPa.Value > 0) || !Finite(readout.maxQ_kPa.Value)) return false;
            return readout.maxQ_kPa.Value > MaxQAmberFraction * readout.qLim_kPa.Value;
        }

        static bool Tumble(Catalog catalog, Design design, QuickReadout readout, out string code, out string engineer)
        {
            code = "tumble";
            engineer = "Tumble when S < 0 and CAR < 1.";
            if (readout.minStability.HasValue && readout.minCar.HasValue
                && readout.minStability.Value < 0 && readout.minCar.Value < 1)
            {
                engineer = "S " + F(readout.minStability.Value, "0.00") + " < 0 and CAR " + F(readout.minCar.Value, "0.00") + " < 1.";
                return true;
            }
            if (readout.predictedFailure != null && readout.predictedFailure.kind == "tumble")
            {
                engineer = "Predicted failure is tumble.";
                return true;
            }
            var reds = StepperLaunch.Reds(readout.risks);
            for (int i = 0; i < reds.Count; i++)
            {
                if (reds[i].kind == "tumble")
                {
                    engineer = string.IsNullOrEmpty(reds[i].engineer) ? engineer : reds[i].engineer;
                    return true;
                }
            }
            if (!readout.minCar.HasValue && readout.stabilityCal < 0 && (catalog == null || design == null || !DesignRules.PadSteered(catalog, design)))
            {
                code = "unstable";
                engineer = "Liftoff stability " + F(readout.stabilityCal, "0.00") + " cal < 0 and the pad stage is not engine-steered.";
                return true;
            }
            return false;
        }

        /// <summary>
        /// Need in m/s when Readout saw this landing mode. Null when the ascent has not filled it,
        /// or when the readout still thinks the rocket is expendable.
        /// </summary>
        static double? LandingNeed(Design design, string mode, QuickReadout readout)
        {
            if (readout == null || !readout.landingNeed_mps.HasValue || !Finite(readout.landingNeed_mps.Value)) return null;
            string seen = CanonicalMode(design?.recoveryMode);
            if (seen != mode) return null;
            if (readout.landingNeed_mps.Value == 0 && readout.maxQStale) return null;
            return readout.landingNeed_mps.Value;
        }

        static double MaxMixture(Design design)
        {
            double worst = 0;
            void One(StagePart stage)
            {
                if (stage == null) return;
                double x = Math.Abs(stage.MixtureOffsetPctOrZero);
                if (x > worst) worst = x;
            }
            if (design == null) return 0;
            for (int i = 0; i < design.stages.Count; i++) One(design.stages[i]);
            if (design.StrapOnCount > 0) One(design.strapOns);
            return worst;
        }

        static int WorstMixtureStage(Design design)
        {
            int index = -1;
            double worst = 0;
            if (design == null) return -1;
            for (int i = 0; i < design.stages.Count; i++)
            {
                double x = Math.Abs(design.stages[i].MixtureOffsetPctOrZero);
                if (x > worst) { worst = x; index = i; }
            }
            return index;
        }

        static bool Overpressure(Catalog catalog, Design design, out int stage)
        {
            stage = -1;
            if (catalog == null || design == null) return false;
            for (int i = 0; i < design.stages.Count; i++)
            {
                if (!Overpressure(catalog, design.stages[i])) continue;
                stage = i;
                return true;
            }
            if (design.StrapOnCount > 0 && Overpressure(catalog, design.strapOns)) return true;
            return false;
        }

        static bool Overpressure(Catalog catalog, StagePart part)
        {
            if (part?.engineParams == null || !part.engineParams.pcSet) return false;
            if (!catalog.EngineById.TryGetValue(part.engine ?? "", out var engine)) return false;
            var env = EngineModel.EnvelopeFor(engine);
            if (!(env.pcRated > 0)) return false;
            return EngineModel.OverpressurePerSecond(part.engineParams.pc, env.pcRated) > 0;
        }

        static void ApplyAdvisories(StepCheck check, IList<Advisory> advisories, DesignerStep step, bool allowRaise)
        {
            if (advisories == null) return;
            for (int i = 0; i < advisories.Count; i++)
            {
                var advisory = advisories[i];
                if (StepForAdvisory(advisory) != step) continue;
                if (!allowRaise || check.Gate == StepGate.Skip)
                {
                    check.Findings.Add(FromAdvisory(advisory, Level(advisory)));
                    continue;
                }
                if (advisory.severity == AdvisorySeverity.Block && (check.Gate != StepGate.Red || check.Code == "math"))
                    FillFromAdvisory(check, StepGate.Red, advisory);
                else if (advisory.severity == AdvisorySeverity.Warn && check.Gate == StepGate.Pass)
                    FillFromAdvisory(check, StepGate.Amber, advisory);
                else
                    check.Findings.Add(FromAdvisory(advisory, Level(advisory)));
            }
        }

        static void FillFromAdvisory(StepCheck check, StepGate gate, Advisory advisory)
        {
            string field = FieldFor(advisory.code);
            Set(check, gate, advisory.code, field, string.IsNullOrEmpty(advisory.tab) ? check.Tab : advisory.tab,
                string.IsNullOrEmpty(advisory.kid) ? advisory.headline : advisory.kid,
                advisory.engineer);
            if (advisory.part.kind == PartKind.Stage || advisory.part.kind == PartKind.Interstage)
                check.Stage = advisory.part.index;
        }

        static StepFinding FromAdvisory(Advisory advisory, string level)
        {
            var finding = Finding(advisory.code, FieldFor(advisory.code), advisory.tab, level,
                string.IsNullOrEmpty(advisory.kid) ? advisory.headline : advisory.kid,
                advisory.engineer, -1);
            if (advisory.part.kind == PartKind.Stage || advisory.part.kind == PartKind.Interstage)
                finding.Stage = advisory.part.index;
            return finding;
        }

        static string Level(Advisory advisory)
        {
            if (advisory.severity == AdvisorySeverity.Block) return "red";
            if (advisory.severity == AdvisorySeverity.Warn) return "amber";
            return "note";
        }

        static StepCheck Blank(DesignerStep step, string tab)
            => new StepCheck { Step = step, Tab = tab, Gate = StepGate.Pass };

        static StepCheck Set(StepCheck check, StepGate gate, string code, string field, string tab, string kid, string engineer)
        {
            check.Gate = gate;
            check.Code = code;
            check.Field = field;
            check.Tab = tab;
            check.Kid = kid;
            check.Engineer = engineer;
            return check;
        }

        static StepFinding Finding(string code, string field, string tab, string level, string kid, string engineer, int stage)
            => new StepFinding { Code = code, Field = field, Tab = tab, Level = level, Kid = kid, Engineer = engineer, Stage = stage };

        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        static string F(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
    }
}
