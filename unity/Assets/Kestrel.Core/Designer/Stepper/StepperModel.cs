// Stepper model. Evaluates the seven gates, moves Back/Next, and remembers which steps were opened.
// Does not run a reference ascent. Max-Q, CAR, and landing need stay stale until something else caches Readout.Reference.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer.Stepper
{
    public static class StepperModel
    {
        public static StepperReport Evaluate(Catalog catalog, JsonObject design, StepperContext context = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var report = new StepperReport();
            for (int i = 1; i <= StepperCatalog.Count; i++)
                report.Checks[i] = new StepCheck { Step = (DesignerStep)i };

            Design parsed = null;
            if (design != null)
            {
                try { parsed = Design.FromJson(design); }
                catch (Exception) { parsed = null; }
            }
            if (parsed != null) report.Finless = !parsed.fins;

            EngineeringReport engineering = null;
            if (design != null)
            {
                try { engineering = Validate.Engineering(catalog, design); }
                catch (Exception) { engineering = null; }
            }
            QuickReadout readout = engineering?.readout;
            if (readout == null && design != null)
            {
                try { readout = Readout.Quick(catalog, design); }
                catch (Exception) { readout = null; }
            }
            report.Readout = readout;

            try { report.Advisories = Advisories.From(engineering, null, design) ?? new List<Advisory>(); }
            catch (Exception) { report.Advisories = new List<Advisory>(); }

            bool landing = StepperChecks.MissionNeedsLanding(design, context);
            report.LandingRequired = landing;
            bool armed = context != null && context.LaunchConfirmArmed;

            report.Checks[(int)DesignerStep.Mission] = StepperChecks.Mission(parsed);
            report.Checks[(int)DesignerStep.Engines] = StepperChecks.Engines(catalog, parsed, readout, report.Advisories);
            report.Checks[(int)DesignerStep.Tanks] = StepperChecks.Tanks(readout, report.Advisories);
            report.Checks[(int)DesignerStep.Aero] = StepperChecks.Aero(catalog, parsed, readout, report.Advisories);
            report.Checks[(int)DesignerStep.Recovery] = StepperChecks.Recovery(parsed, design, readout, landing, report.Advisories);
            report.Checks[(int)DesignerStep.Look] = StepperChecks.Look();
            report.Checks[(int)DesignerStep.Review] = StepperChecks.Review(readout, armed, out var reds, out bool hasRed);
            report.RedRisks = reds ?? new List<FlightRisk>();
            report.HasRedRisk = hasRed;
            report.NeedsLaunchConfirm = StepperLaunch.NeedsConfirm(hasRed, armed);
            report.CanLaunch = StepperLaunch.CanLaunch(hasRed, armed);
            report.LaunchLabel = StepperLaunch.LaunchButtonLabel(hasRed, armed);
            return report;
        }

        public static DesignerStep? NextActive(DesignerStep current, StepperReport report)
        {
            for (int n = (int)current + 1; n <= StepperCatalog.Count; n++)
            {
                var step = (DesignerStep)n;
                if (report == null || report[step].Gate != StepGate.Skip) return step;
            }
            return null;
        }

        public static DesignerStep? PreviousActive(DesignerStep current, StepperReport report)
        {
            for (int n = (int)current - 1; n >= 1; n--)
            {
                var step = (DesignerStep)n;
                if (report == null || report[step].Gate != StepGate.Skip) return step;
            }
            return null;
        }

        /// <summary>If the current step auto-skipped, move forward to the next step that is on the path.</summary>
        public static bool Settle(StepperProgress progress, StepperReport report)
        {
            if (progress == null || report == null) return false;
            if (report[progress.Current].Gate != StepGate.Skip) return false;
            var dest = NextActive(progress.Current, report) ?? PreviousActive(progress.Current, report);
            if (dest == null) return false;
            Arrive(progress, dest.Value);
            return true;
        }

        public static StepperAction Forward(StepperProgress progress, StepperReport report)
        {
            if (progress == null || report == null) return new StepperAction { Label = "Next" };
            var check = report[progress.Current];
            if (progress.Current == DesignerStep.Review)
            {
                return new StepperAction
                {
                    Allowed = true,
                    NeedsConfirm = report.NeedsLaunchConfirm,
                    Launches = report.CanLaunch,
                    Label = report.LaunchLabel,
                    FocusField = check.Field,
                    FocusTab = check.Tab,
                };
            }
            if (check.Gate == StepGate.Red)
            {
                return new StepperAction
                {
                    Allowed = false,
                    FixIt = true,
                    Label = "Fix it",
                    FocusField = check.Field,
                    FocusTab = check.Tab,
                };
            }
            var dest = NextActive(progress.Current, report);
            bool amber = check.Gate == StepGate.Amber;
            string name = dest.HasValue ? StepperCatalog.Get(dest.Value).ShortName : "Next";
            return new StepperAction
            {
                Allowed = dest.HasValue,
                NeedsConfirm = amber,
                Label = amber ? "Next anyway" : "Next: " + name,
                Destination = dest,
                FocusField = check.Field,
                FocusTab = check.Tab,
            };
        }

        public static StepperAction Back(StepperProgress progress, StepperReport report)
        {
            var action = new StepperAction { Label = "Back" };
            if (progress == null || report == null) return action;
            var dest = PreviousActive(progress.Current, report);
            action.Destination = dest;
            action.Allowed = dest.HasValue;
            return action;
        }

        public static bool TryAdvance(StepperProgress progress, StepperReport report, bool confirmAccepted)
        {
            var action = Forward(progress, report);
            if (!action.Allowed || action.Launches || action.Destination == null) return false;
            if (action.NeedsConfirm && !confirmAccepted) return false;
            Arrive(progress, action.Destination.Value);
            return true;
        }

        public static bool TryBack(StepperProgress progress, StepperReport report)
        {
            var action = Back(progress, report);
            if (!action.Allowed || action.Destination == null) return false;
            Arrive(progress, action.Destination.Value);
            return true;
        }

        public static bool CanJump(StepperProgress progress, StepperReport report, DesignerStep target)
        {
            if (progress == null) return false;
            if (!StepperCatalog.TryStep((int)target, out _)) return false;
            if (progress.View == StepperView.AllParts) return true;
            if (target == progress.Current) return true;
            if (progress.Visited.Contains(target)) return true;
            if (report != null && report[target].Gate == StepGate.Skip) return true;
            return false;
        }

        /// <summary>
        /// F3: what a step chip does when tapped. On the Review step its chip reads "Launch", so it launches through
        /// <see cref="Forward"/>: the same confirm and label as the Launch button, and the designer's launch gate behind
        /// the bridge. Any other chip opens its step when <see cref="CanJump"/> allows it. The current non-Review step,
        /// a skipped step, a locked step and the all-parts view do nothing.
        /// </summary>
        public static SegmentTap Tap(StepperProgress progress, StepperReport report, DesignerStep step)
        {
            if (progress == null || report == null || progress.View != StepperView.Guided) return SegmentTap.None;
            if (!StepperCatalog.TryStep((int)step, out _)) return SegmentTap.None;
            if (report[step].Gate == StepGate.Skip) return SegmentTap.None;
            if (step == progress.Current)
            {
                if (step != DesignerStep.Review) return SegmentTap.None;
                var action = Forward(progress, report);
                return action.Launches || action.NeedsConfirm ? SegmentTap.Launch : SegmentTap.None;
            }
            return CanJump(progress, report, step) ? SegmentTap.Open : SegmentTap.None;
        }

        public static bool TryJump(StepperProgress progress, StepperReport report, DesignerStep target)
        {
            if (!CanJump(progress, report, target)) return false;
            Arrive(progress, target);
            return true;
        }

        /// <summary>Later steps whose gate or code changed are marked. The edited step itself is not.</summary>
        public static void NoteEdit(StepperProgress progress, DesignerStep edited, StepperReport before, StepperReport after)
        {
            if (progress == null || before == null || after == null) return;
            for (int n = (int)edited + 1; n <= StepperCatalog.Count; n++)
            {
                var step = (DesignerStep)n;
                var left = before[step];
                var right = after[step];
                if (left == null || right == null) continue;
                if (left.Gate != right.Gate || left.Code != right.Code)
                    progress.Affected.Add(step);
            }
        }

        public static StepMark Mark(StepperProgress progress, StepperReport report, DesignerStep step)
        {
            var gate = report != null ? report[step].Gate : StepGate.Pass;
            if (gate == StepGate.Skip) return StepMark.Skipped;
            if (progress != null && progress.View == StepperView.Guided && step == progress.Current) return StepMark.Current;
            if (progress != null && progress.Affected.Contains(step)) return StepMark.Warn;
            bool visited = progress != null && progress.Visited.Contains(step);
            bool unlocked = progress == null || progress.View == StepperView.AllParts || visited || (progress.View == StepperView.Guided && step == progress.Current);
            if (!unlocked) return StepMark.Locked;
            if (gate == StepGate.Red) return StepMark.Failed;
            if (gate == StepGate.Amber) return StepMark.Warn;
            if (visited) return StepMark.Done;
            return StepMark.Open;
        }

        public static StepMark[] Marks(StepperProgress progress, StepperReport report)
        {
            var marks = new StepMark[StepperCatalog.Count];
            for (int i = 0; i < marks.Length; i++)
                marks[i] = Mark(progress, report, (DesignerStep)(i + 1));
            return marks;
        }

        static void Arrive(StepperProgress progress, DesignerStep step)
        {
            progress.Current = step;
            progress.Visited.Add(step);
            progress.Affected.Remove(step);
        }
    }
}
