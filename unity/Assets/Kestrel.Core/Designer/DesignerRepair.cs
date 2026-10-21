// One-tap repair from a post-flight card or a designer warning.
// The number that changes is the one the failure named. Speed shortfalls go through DvMarginFix.
using System;
using Kestrel.Core.Data;
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
        /// </summary>
        public static RepairOutcome Fix(Catalog catalog, JsonObject design, string kind)
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
                for (int step = 0; step < FlightSteps && dv != null && dv.Solved && FlightShort(catalog, design); step++)
                {
                    double margin = double.IsNaN(dv.Margin) ? 0 : dv.Margin;
                    var more = DvMarginFix.Solve(catalog, design, Math.Max(0, margin) + FlightMarginStep);
                    if (more == null || !more.Solved || !more.Changed) break;
                    dv = more;
                    changed |= Take(design, dv);
                }
                if (changed) outcome.Edit = new Edit("Fixed the speed shortfall");
                if (dv == null)
                    outcome.Note = "The speed for this trip could not be worked out. Lighten the cargo, raise the budget, or pick a different mission.";
                else if (changed)
                    outcome.Note = DeltaVAdded;
                else
                    outcome.Note = DeltaVNote(dv);
                return outcome;
            }
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
            return outcome;
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
            if (hop) return orbit.apoKm > 0 && sim.orbit.apoAlt < 0.95 * orbit.apoKm * 1000;
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
        public bool Changed => Edit.changed;
    }
}
