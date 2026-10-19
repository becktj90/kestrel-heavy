// Starter designer screen ids (feat(designer-starter), PR 5) for the navigation harness. The root is
// UiIds.ScreenStarter ("screen-starter", in UiIds.cs). Option ids carry the option's value, not its index:
// starter-engine-kerosene, starter-stages-2, starter-fuel-3 (notch 3 of 5), starter-boosters-4.
using System.Collections.Generic;
using System.Globalization;

namespace Kestrel.Core.Ui
{
    public static partial class UiIds
    {
        // ---- Starter: the one primary button and the ways out ------------------------------------
        /// <summary>The one primary button: bottom of the sheet (portrait), bottom-right of the side panel (web).</summary>
        public const string StarterLaunch = "starter-launch";
        /// <summary>Top-left. Back to the briefing (Missions when there is none).</summary>
        public const string StarterBack = "starter-back";
        /// <summary>Back to the mission's rocket.</summary>
        public const string StarterReset = "starter-reset";

        // ---- Starter controls --------------------------------------------------------------------
        public const string StarterEngine = "starter-engine";
        public const string StarterEngineKerosene = "starter-engine-kerosene";
        public const string StarterEngineMethane = "starter-engine-methane";
        public const string StarterStages = "starter-stages";
        public const string StarterStages1 = "starter-stages-1";
        public const string StarterStages2 = "starter-stages-2";
        public const string StarterStages3 = "starter-stages-3";
        /// <summary>The fuel slider (5 notches). Each notch is <see cref="StarterFuelNotch"/>(1..5).</summary>
        public const string StarterFuel = "starter-fuel";
        /// <summary>Hidden on missions without strap-ons; locked on KH-10/KH-19.</summary>
        public const string StarterBoosters = "starter-boosters";
        public const string StarterBoosters0 = "starter-boosters-0";
        public const string StarterBoosters2 = "starter-boosters-2";
        public const string StarterBoosters4 = "starter-boosters-4";

        // ---- Starter read-only elements (not tappable) -------------------------------------------
        public const string StarterRocket = "starter-rocket";
        public const string StarterTitle = "starter-title";
        public const string StarterSubtitle = "starter-subtitle";
        public const string StarterHint = "starter-hint";
        public const string StarterBadge = "starter-badge";
        public const string StarterReadiness = "starter-readiness";
        public const string StarterSheet = "starter-sheet";

        public static string StarterEngineOption(global::Kestrel.Core.Designer.StarterEngine e) => StarterEngine + "-" + global::Kestrel.Core.Designer.StarterRocket.EngineId(e);
        public static string StarterStagesOption(int stages) => StarterStages + "-" + stages.ToString(CultureInfo.InvariantCulture);
        /// <summary>Notch 1..5 (the fuel slider reads "Fuel, 3 of 5").</summary>
        public static string StarterFuelNotch(int notch1To5) => StarterFuel + "-" + notch1To5.ToString(CultureInfo.InvariantCulture);
        public static string StarterBoostersOption(int count) => StarterBoosters + "-" + count.ToString(CultureInfo.InvariantCulture);

        /// <summary>Every interactive Starter control, in reading order. Booster options exist on every
        /// mission but are hidden where the mission has no strap-ons.</summary>
        public static readonly string[] StarterControls =
        {
            StarterBack, StarterReset,
            StarterEngineKerosene, StarterEngineMethane,
            StarterStages1, StarterStages2, StarterStages3,
            StarterFuel + "-1", StarterFuel + "-2", StarterFuel + "-3", StarterFuel + "-4", StarterFuel + "-5",
            StarterBoosters0, StarterBoosters2, StarterBoosters4,
            StarterLaunch,
        };

        /// <summary>Where each Starter way out goes (for the harness): Back → briefing (Missions without one),
        /// Launch → flight. Reset and the controls stay on <see cref="ScreenStarter"/>.</summary>
        public static readonly IReadOnlyDictionary<string, string> StarterExits = new Dictionary<string, string>
        {
            { StarterBack, ScreenBriefing },
            { StarterLaunch, ScreenFlight },
        };
    }
}
