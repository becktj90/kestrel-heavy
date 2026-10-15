// Guided designer stepper (spec §2–§3, work package S1).
// Seven steps. Structure folds into Tanks and Guidance folds into Aero.
// Gates are pass, amber ("Next anyway"), red (blocked, "Fix it"), or skip.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;

namespace Kestrel.Core.Designer.Stepper
{
    public enum DesignerStep
    {
        Mission = 1,
        Engines = 2,
        Tanks = 3,
        Aero = 4,
        Recovery = 5,
        Look = 6,
        Review = 7,
    }

    public enum DesignerDepth
    {
        Simple = 0,
        Engineer = 1,
    }

    /// <summary>Guided steps, or the existing ten-tab view. Both edit the same design.</summary>
    public enum StepperView
    {
        Guided = 0,
        AllParts = 1,
    }

    public enum StepGate
    {
        /// <summary>Next is enabled.</summary>
        Pass = 0,
        /// <summary>Next anyway, after a confirm. Review uses this for "Launch anyway".</summary>
        Amber = 1,
        /// <summary>Next is disabled. The fix button focuses <see cref="StepCheck.Field"/>.</summary>
        Red = 2,
        /// <summary>Not on the path. Recovery uses this when the mission needs no landing.</summary>
        Skip = 3,
    }

    /// <summary>One segment of the seven-step progress bar.</summary>
    public enum StepMark
    {
        Locked = 0,
        Current = 1,
        Done = 2,
        Warn = 3,
        Failed = 4,
        Skipped = 5,
        /// <summary>Unlocked and not the current step. All parts uses this for a step the player has not visited.</summary>
        Open = 6,
    }

    public sealed class StepDef
    {
        public DesignerStep Step;
        public int Number;
        public string Title;
        public string ShortName;
        public string Goal;
        public string Highlight;
        public string[] SimpleControls;
        public string[] EngineerControls;
        public string[] FailureKinds;
        public string[] StatKeys;
    }

    public sealed class StepFinding
    {
        public string Code;
        public string Field;
        public string Tab;
        public string Level;
        public string Kid;
        public string Engineer;
        public int Stage = -1;
    }

    public sealed class StepCheck
    {
        public DesignerStep Step;
        public StepGate Gate = StepGate.Pass;
        public string Code;
        public string Field;
        public string Tab;
        public string Kid;
        public string Engineer;
        public bool Stale;
        public int Stage = -1;
        public double Value;
        public double Limit;
        public readonly List<StepFinding> Findings = new List<StepFinding>();
    }

    /// <summary>
    /// One evaluation of every step. Built from <see cref="Readout.Quick"/>, <see cref="Hangar.Advisories"/>,
    /// and the same red-risk launch rules as DesignerV3Risk.
    /// </summary>
    public sealed class StepperReport
    {
        public QuickReadout Readout;
        public List<Advisory> Advisories = new List<Advisory>();
        public StepCheck[] Checks = new StepCheck[8];
        public List<FlightRisk> RedRisks = new List<FlightRisk>();
        public bool HasRedRisk;
        public bool NeedsLaunchConfirm;
        public bool CanLaunch;
        public string LaunchLabel;
        public bool LandingRequired;

        public StepCheck this[DesignerStep step] => Checks[(int)step];
    }

    /// <summary>What Back, Next, Fix it, or Launch should do for the current step.</summary>
    public sealed class StepperAction
    {
        public bool Allowed;
        public bool NeedsConfirm;
        public bool FixIt;
        /// <summary>Review only. True when the button may start the flight (confirm already armed, or no red risk).</summary>
        public bool Launches;
        public string Label;
        public string FocusField;
        public string FocusTab;
        public DesignerStep? Destination;
    }

    /// <summary>
    /// Optional inputs for <see cref="StepperModel.Evaluate"/>. Gates do not change between Simple and Engineer.
    /// Depth is only the auto-assist default, which lives on <see cref="StepperProgress"/>.
    /// </summary>
    public sealed class StepperContext
    {
        public DesignerDepth Depth = DesignerDepth.Simple;
        /// <summary>Mirrors DesignerV3Risk confirm state. Armed means the player already chose Launch anyway.</summary>
        public bool LaunchConfirmArmed;
        public Campaign Campaign;
        /// <summary>When set, wins over the campaign mission and the design flag.</summary>
        public bool? LandingRequired;
    }

    /// <summary>
    /// Launch-button rules copied from Kestrel.Slice.DesignerV3.DesignerV3Risk.
    /// Core cannot reference Slice. S2 should keep these three predicates in lockstep with that type:
    /// LaunchButtonLabel, NeedsConfirm, CanLaunch.
    /// </summary>
    public static class StepperLaunch
    {
        public const string LaunchAnywayLabel = "Launch anyway";
        public const string LaunchLabel = "Launch";
        public const string ConfirmTitle = "Red risks on this design";

        public static string LaunchButtonLabel(bool hasRed, bool confirmArmed) =>
            hasRed && !confirmArmed ? LaunchAnywayLabel : LaunchLabel;

        public static bool NeedsConfirm(bool hasRed, bool confirmArmed) => hasRed && !confirmArmed;

        public static bool CanLaunch(bool hasRed, bool confirmArmed) => !hasRed || confirmArmed;

        public static List<FlightRisk> Reds(IList<FlightRisk> risks)
        {
            var list = new List<FlightRisk>();
            if (risks == null) return list;
            for (int i = 0; i < risks.Count; i++)
                if (risks[i] != null && risks[i].level == "red") list.Add(risks[i]);
            return list;
        }
    }
}
