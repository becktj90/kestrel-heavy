// Nav-harness ids for the post-flight report. Same convention as UiIds.Flight.cs: controls are
// <screen>-<control>[-<option>], kebab-case, set as the UI Toolkit element name.
// Not repeated here (one declaration per constant across the UiIds partials):
// - the screen root "screen-report" is UiIds.ScreenReport in UiIds.cs (#300). Until #300 lands, ReportView.RootName
//   carries the same literal;
// - the engine-out prompt buttons are UiIds.FlightPrimary / FlightAlternate and the pause menu is UiIds.Pause*
//   (UiIds.Flight.cs).
namespace Kestrel.Core.Ui
{
    public static partial class UiIds
    {
        // ---- screen-report ---------------------------------------------------------------------
        public const string ReportRetry = "report-retry";
        public const string ReportNext = "report-next";
        public const string ReportHome = "report-home";
        public const string ReportMissions = "report-missions";

        /// <summary>
        /// "Back to designer". TODO(#301 / #311): no element carries this name on main yet. The exit and its route
        /// arrive with #301's ReportActions.Designer / SceneFlow.ReportDesigner hook (or #311's ReportRoutes.BackToDesigner,
        /// which names the button "report-back-to-designer"). Whichever lands first should name the button with this
        /// constant; the footer grid in ReportCardChrome already lays out a sixth exit (FlightHudReportLayoutDeviceTests).
        /// </summary>
        public const string ReportDesigner = "report-designer";

        /// <summary>Fix it: the footer's primary (full-width) exit. Legacy name kept for the deep-link tests.</summary>
        public const string ReportFix = "failure-fix";

        /// <summary>Containers: the body (lines and plots) and the exits grid.</summary>
        public const string ReportBody = "report-scroll";
        public const string ReportFoot = "report-actions";

        /// <summary>The exits on main today that must be on screen, tap targets, without scrolling at every size.</summary>
        public static readonly string[] ReportExits = { ReportFix, ReportRetry, ReportNext, ReportMissions, ReportHome };
    }
}
