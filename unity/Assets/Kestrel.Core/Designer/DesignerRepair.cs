// One-tap repair from a post-flight card or a designer warning.
// The number that changes is the one the failure named. Speed shortfalls go through DvMarginFix.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;

namespace Kestrel.Core.Designer
{
    public static class DesignerRepair
    {
        public static bool NeedsDeltaV(string kind)
            => kind == "prop-depletion" || kind == "dv-margin";

        public const string DeltaVAdded = "Added propellant or a stage so the speed covers this trip.";
        public const string DeltaVAlready = "This rocket already has the speed for this trip. Check the other warnings.";
        /// <summary>F2: Fix it ran out of steps and the rocket still fails a test flight. It never claims a full fix then.</summary>
        public const string NotFullyFixed = "Fix it could not fully fix this rocket. It still fails {0} of {1} test flights. Lighten the cargo or change the rocket.";

        /// <summary>
        /// F2: the kind the report's Fix it repairs. A flight with no failure event still owes a fix:
        /// a "miss" (short of the goal) is a speed shortfall. A pass has nothing to fix.
        /// The report used to send "" here, which opened Structure and changed nothing.
        /// </summary>
        public static string ReportKind(string failureKind, string debriefKind)
        {
            if (!string.IsNullOrEmpty(failureKind)) return failureKind;
            if (debriefKind == "miss") return "dv-margin";
            if (string.IsNullOrEmpty(debriefKind) || debriefKind == "pass") return "";
            return debriefKind;
        }

        /// <summary>
        /// F2: the designer tab id a fix opens. The fuel load (propMass) lives on Tanks, so a speed or
        /// fuel fix opens Tanks even though FailureMonitor files prop-depletion under "propellants".
        /// </summary>
        public static string FixTab(string kind, string riskTab, string field = null)
        {
            if (NeedsDeltaV(kind) || field == "propMass") return "tanks";
            return riskTab;
        }

        /// <summary>F2: the note after a speed fix. It only says it added something when it did.</summary>
        public static string DeltaVNote(DvFixResult result)
        {
            if (result == null) return "";
            if (result.Solved && result.Changed) return DeltaVAdded;
            if (result.Solved) return DeltaVAlready;
            return result.Reason ?? "";
        }

        /// <summary>
        /// F2: whether the designer's gate Fix it repairs the design. A predicted failure, a speed shortfall or a
        /// cargo that does not fit is repaired (the same <see cref="Fix"/> the report's Fix it runs). A validation
        /// block (schema, or a Block advisory) has no one-tap repair, so Fix it only opens the tab that holds it.
        /// The gate's Fix it used to only switch tabs, so tapping it changed nothing.
        /// </summary>
        public static bool GateRepairs(EngineeringReport report, LaunchGateResult gate)
            => gate != null && gate.state == LaunchGateState.Blocked && !string.IsNullOrEmpty(gate.code)
               && report != null && report.ok && (report.blocks == null || report.blocks.Count == 0);

        /// <summary>
        /// F2: the one-tap repair behind every Fix it (report card, gate, risk row). Edits <paramref name="design"/>
        /// in place and says what changed. A speed or fuel kind goes through <see cref="DvMarginFix"/>; anything
        /// else tries the stepper's search (<see cref="Stepper.StepperFix"/>, which may take several moves, e.g.
        /// strength and max-Q throttle together), then the single control the failure named.
        /// With <paramref name="mission"/> (the live mission, the same one FlightDriver flies), "fixed" means fixed: the
        /// repaired rocket must fly FlightBatch seeds 1–5 (<see cref="GateSeeds"/>, the golden gate's seeds, gusts included)
        /// with no failure. Fix it keeps adding strength, one step at a time and at most <see cref="StrengthSteps"/> steps,
        /// while a seed still breaks up; a fuel kind keeps adding margin while a seed runs dry. If that is not enough the
        /// note says so (<see cref="NotFullyFixed"/>). Without a mission (a free build) only the designer's checks apply.
        /// </summary>
        public static RepairOutcome Fix(Catalog catalog, JsonObject design, string kind, CampaignMission mission = null)
        {
            var outcome = new RepairOutcome { Kind = kind ?? "" };
            if (design == null) return outcome;
            if (NeedsDeltaV(kind))
            {
                outcome.DeltaV = true;
                var dv = DvMarginFix.Solve(catalog, design);
                bool changed = Take(design, dv);
                // The budget can say "enough" while the flown test ascent still runs dry (or a hop tops out short).
                // That is the report's "ran out of fuel", and Fix it used to change nothing for it. Ask the solver
                // for a bigger margin until the flight is not short, a few steps at most.
                for (int step = 0; step < FlightSteps && dv != null && dv.Solved
                     && (FlightShort(catalog, design) || FirstFailure(catalog, mission, design) == "prop-depletion"); step++)
                {
                    double margin = double.IsNaN(dv.Margin) ? 0 : dv.Margin;
                    var more = DvMarginFix.Solve(catalog, design, Math.Max(0, margin) + FlightMarginStep);
                    if (more == null || !more.Solved || !more.Changed) break;
                    dv = more;
                    changed |= Take(design, dv);
                }
                // A seed that still breaks up on the way (a gust at max-Q) gets the same strength steps as below.
                var strength = mission != null ? Strengthen(catalog, mission, design) : Edit.None;
                bool fuel = changed;
                changed |= strength.changed;
                if (changed) outcome.Edit = new Edit("Fixed the speed shortfall");
                if (Unfixed(catalog, mission, design, outcome)) return outcome;
                if (strength.changed && !fuel)
                    outcome.Note = strength.what + " " + strength.from + " → " + strength.to;
                else if (dv == null)
                    outcome.Note = "The speed for this trip could not be worked out. Lighten the cargo, raise the budget, or pick a different mission.";
                else if (changed)
                    outcome.Note = DeltaVAdded;
                else
                    outcome.Note = DeltaVNote(dv);
                return outcome;
            }
            var start = mission != null ? JsonMut.CloneObject(design) : null;
            var check = new Stepper.StepCheck
            {
                Step = Stepper.DesignerStep.Look,
                Code = string.IsNullOrEmpty(kind) ? "fix" : kind,
                Gate = Stepper.StepGate.Amber,
            };
            var stepped = Stepper.StepperFix.Apply(catalog, design, check);
            var made = stepped.Edit;
            string said = stepped.Instruction;
            if (!made.changed) made = Apply(catalog, design, kind);
            outcome.Edit = made;
            if (made.changed && !string.IsNullOrEmpty(made.from) && !string.IsNullOrEmpty(made.to))
                outcome.Note = made.what + " " + made.from + " → " + made.to;
            else if (!string.IsNullOrEmpty(said))
                outcome.Note = said;
            else if (kind == "overg" || kind == "payload-g")
                outcome.Note = "The g limit is already under the payload rating.";
            else
                outcome.Note = "That control is already at the safer setting.";
            if (mission == null || FirstFailure(catalog, mission, design) == null)
            {
                outcome.FullyFixed = mission != null;
                return outcome;
            }
            // The designer's checks read Ready but a gate seed still fails (a gust at max-Q). First add strength to the
            // repaired rocket; if that cannot pass all seeds, add strength to the rocket as it was before Fix it (the
            // stepper's other moves, more fuel or engines, can be what the gust catches). Same steps, same seeds.
            var stronger = JsonMut.CloneObject(design);
            var added = Strengthen(catalog, mission, stronger);
            if (added.changed && FirstFailure(catalog, mission, stronger) == null)
            {
                Replace(design, stronger);
                outcome.Edit = made.changed ? made : added;
                outcome.Note = (outcome.Note + " " + added.what + " " + added.from + " → " + added.to).Trim();
                outcome.FullyFixed = true;
                return outcome;
            }
            var plain = JsonMut.CloneObject(start);
            var only = Strengthen(catalog, mission, plain);
            if (only.changed && FirstFailure(catalog, mission, plain) == null)
            {
                Replace(design, plain);
                outcome.Edit = only;
                outcome.Note = only.what + " " + only.from + " → " + only.to;
                outcome.FullyFixed = true;
                return outcome;
            }
            Unfixed(catalog, mission, design, outcome);
            return outcome;
        }

        /// <summary>FlightBatch seeds the gate flies (the golden gate's 1–5, gusts included).</summary>
        public const ulong GateSeeds = 5;
        /// <summary>Strength steps Fix it may add while a gate seed still breaks up (DesignerLimits.safetyStep each).</summary>
        public const int StrengthSteps = 8;
        /// <summary>Strength steps of margin added past the first step that flies seeds 1-5 (Arcade: not on a knife-edge).</summary>
        public const int MarginSteps = 1;

        /// <summary>
        /// The first failure kind on FlightBatch seeds 1–<see cref="GateSeeds"/> (the same flight the gate and the goldens
        /// fly), or null when every seed flies clean. Null as well without a mission: there is no gate flight to judge.
        /// </summary>
        public static string FirstFailure(Catalog catalog, CampaignMission mission, JsonObject design)
        {
            if (catalog == null || mission == null || design == null) return null;
            var d = Vehicle.Design.FromJson(design);
            for (ulong seed = 1; seed <= GateSeeds; seed++)
            {
                var f = FlightBatch.Fly(catalog, mission, d, seed);
                if (f.failure != null) return f.failure;
            }
            return null;
        }

        static int FailedSeeds(Catalog catalog, CampaignMission mission, JsonObject design)
        {
            var d = Vehicle.Design.FromJson(design);
            int n = 0;
            for (ulong seed = 1; seed <= GateSeeds; seed++)
                if (FlightBatch.Fly(catalog, mission, d, seed).failure != null) n++;
            return n;
        }

        static bool StrengthHelps(string failure) => failure == "structural-maxq" || failure == "maxq-high";

        /// <summary>
        /// Adds strength one step at a time (at most <see cref="StrengthSteps"/>) while a gate seed breaks up at max-Q.
        /// Stops at once when the seeds pass, when the failure is not one strength fixes, or at the strength maximum.
        /// After the first passing step it adds one more step of margin (<see cref="MarginSteps"/>) if seeds 1-5 still pass.
        /// The edit spans the first step's "from" to the last step's "to".
        /// </summary>
        static Edit Strengthen(Catalog catalog, CampaignMission mission, JsonObject design)
        {
            string from = null, to = null;
            for (int step = 0; step < StrengthSteps; step++)
            {
                if (!StrengthHelps(FirstFailure(catalog, mission, design))) break;
                var e = RaiseSafety(design);
                if (!e.changed) break;
                if (from == null) from = e.from;
                to = e.to;
            }
            if (from == null) return Edit.None;
            // Arcade: one more step of margin past the first strength that flies seeds 1-5, so the repair is not on a
            // knife-edge for later seeds. Kept only if seeds 1-5 still fly clean with it (more strength is more mass).
            for (int step = 0; step < MarginSteps && FirstFailure(catalog, mission, design) == null; step++)
            {
                var margin = JsonMut.CloneObject(design);
                var e = RaiseSafety(margin);
                if (!e.changed || FirstFailure(catalog, mission, margin) != null) break;
                Replace(design, margin);
                to = e.to;
            }
            return new Edit("Strength", from, to);
        }

        /// <summary>Sets the honest note when a gate seed still fails after every step. True when it did.</summary>
        static bool Unfixed(Catalog catalog, CampaignMission mission, JsonObject design, RepairOutcome outcome)
        {
            if (mission == null) return false;
            int failed = FailedSeeds(catalog, mission, design);
            outcome.FullyFixed = failed == 0;
            if (failed == 0) return false;
            outcome.Note = string.Format(System.Globalization.CultureInfo.InvariantCulture, NotFullyFixed, failed, GateSeeds);
            return true;
        }

        static void Replace(JsonObject design, JsonObject from)
        {
            design.Clear();
            design.Order.Clear();
            foreach (var k in from.Order) design.Add(k, JsonMut.Clone(from[k]));
        }

        /// <summary>Extra Δv margin asked for per step when the flown ascent is still short.</summary>
        public const double FlightMarginStep = 0.05;
        public const int FlightSteps = 6;

        static bool Take(JsonObject design, DvFixResult dv)
        {
            if (dv == null || !dv.Solved || !dv.Changed || dv.Design == null) return false;
            design.Clear();
            design.Order.Clear();
            foreach (var k in dv.Design.Order) design.Add(k, JsonMut.Clone(dv.Design[k]));
            HangarEditor.Normalize(design);
            return true;
        }

        /// <summary>
        /// The designer's test ascent (<see cref="ReferenceAscent.Simulate"/>) is short on fuel: it runs dry before the
        /// target on an orbit trip, or tops out under FlightBatch's -5% apogee band on a hop. A hop coasts after burnout,
        /// so its depletion event is not used.
        /// </summary>
        public static bool FlightShort(Catalog catalog, JsonObject design)
        {
            if (catalog == null || design == null) return false;
            string orbitId = design.Obj("payload")?.Str("orbit");
            OrbitTarget orbit = null;
            if (orbitId != null) catalog.Orbits.TryGetValue(orbitId, out orbit);
            Physics.SimResult sim;
            try { sim = ReferenceAscent.Simulate(catalog, design); }
            catch (Exception) { return false; }
            if (sim == null) return false;
            bool hop = orbit != null && orbit.periKm < 0;
            if (hop) return orbit.apoKm > 0 && MissionScore.FlownApoAltM(sim) < 0.95 * orbit.apoKm * 1000;
            return sim.failure != null && sim.failure.kind == "prop-depletion";
        }

        public static Edit Apply(Catalog catalog, JsonObject design, string kind)
        {
            if (design == null || string.IsNullOrEmpty(kind)) return Edit.None;
            switch (kind)
            {
                case "overg":
                case "payload-g":
                    return LowerGLimit(catalog, design);
                case "tumble":
                    return HangarEditor.SetFins(design, true);
                case "structural-maxq":
                case "maxq-high":
                    return RaiseSafety(design);
                default:
                    return Edit.None;
            }
        }

        static Edit LowerGLimit(Catalog catalog, JsonObject design)
        {
            double cap = 8;
            if (catalog != null)
            {
                var payload = design.Obj("payload");
                string type = payload != null ? payload.Str("type") : null;
                if (type != null && catalog.PayloadById.TryGetValue(type, out var pl) && pl.maxG > 0 && pl.maxG < cap)
                    cap = pl.maxG;
            }
            double target = Math.Max(1.5, cap - 0.5);
            var g = design.Obj("guidance");
            if (g == null)
            {
                g = new JsonObject();
                JsonMut.Set(design, "guidance", g);
            }
            double current = g.ContainsKey("gLimit") ? g.Num("gLimit") : cap;
            if (current <= target + 1e-6) return Edit.None;
            JsonMut.Set(g, "gLimit", target);
            return new Edit("g limit", current.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                target.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
        }

        static Edit RaiseSafety(JsonObject design)
        {
            double step = DesignerLimits.Active.safetyStep;
            if (!(step > 0)) step = 0.05;
            double current = design.Num("safetyFactor", 1.4);
            return HangarEditor.SetSafetyFactor(design, current + step);
        }
    }

    /// <summary>What a Fix it did: the edit (unchanged when there was nothing to change) and the line the player reads.</summary>
    public sealed class RepairOutcome
    {
        public string Kind = "";
        public Edit Edit = Edit.None;
        public string Note = "";
        /// <summary>The speed path ran (<see cref="DvMarginFix"/>); its note is player text that still goes through PlayerText.</summary>
        public bool DeltaV;
        /// <summary>F2: a mission was given and the repaired rocket flies every gate seed with no failure.</summary>
        public bool FullyFixed;
        public bool Changed => Edit.changed;
    }
}
