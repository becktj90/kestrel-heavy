// Designer tiers ids (docs/DESIGNER_TIERS_SPEC.md, delivery step 2): the tier switch, the "?" Why? buttons and card,
// the speed bar and the X-ray view on the Starter screen. Ids carry values, not indexes: starter-why-stages,
// starter-tier-engineer, starter-xray-part-tank-1, starter-xray-event-maxq.
using Kestrel.Core.Designer;
using Kestrel.Core.Features;

namespace Kestrel.Core.Ui
{
    public static partial class UiIds
    {
        /// <summary>The Kid / Builder / Engineer switch (hidden while only Kid is offered).</summary>
        public const string StarterTierSwitch = "starter-tier-switch";
        /// <summary>The open Why? card, its pictogram and its text.</summary>
        public const string StarterWhyCard = "starter-why-card";
        public const string StarterWhyIcon = "starter-why-icon";
        public const string StarterWhyText = "starter-why-text";
        /// <summary>The speed bar: its label, Enough / Short word, the track, the fill and the need marker.</summary>
        public const string StarterSpeed = "starter-speed";
        public const string StarterSpeedLabel = "starter-speed-label";
        public const string StarterSpeedStatus = "starter-speed-status";
        public const string StarterSpeedTrack = "starter-speed-track";
        public const string StarterSpeedFill = "starter-speed-fill";
        public const string StarterSpeedNeed = "starter-speed-need";
        /// <summary>X-ray: the button in the rocket window, the full-screen view and its way back.</summary>
        public const string StarterXrayButton = "starter-xray";
        public const string StarterXray = "starter-xray-view";
        public const string StarterXrayClose = "starter-xray-close";
        public const string StarterXrayTitle = "starter-xray-title";
        public const string StarterXrayCard = "starter-xray-card";
        public const string StarterXrayPlan = "starter-xray-plan";

        public static string StarterTierChip(ExperienceTier t) => "starter-tier-" + ExperienceSettings.TierId(t);
        public static string StarterWhy(StarterKnob k) => "starter-why-" + (DesignerTierText.RowOf(k) ?? "none");
        public static string StarterXrayPart(string partId) => "starter-xray-part-" + partId;
        public static string StarterXrayEvent(string eventId) => "starter-xray-event-" + eventId;
    }
}
