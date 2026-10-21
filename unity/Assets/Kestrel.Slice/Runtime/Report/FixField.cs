// "Fix it" deep link (§3). The tab and field match FailureMonitor.BuildRisks so the
// card and the pre-launch risk list point at the same designer control.
using Kestrel.Core.Ui;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class FixField
    {
        public readonly string Kind, Tab, Field, TabLabel, FieldLabel, Uri, VoiceOver;
        public static FixField Pending { get; private set; }
        public static event System.Action<FixField> OnApply;

        FixField(string kind, string tab, string field, string tabLabel, string fieldLabel)
        {
            Kind = kind; Tab = tab; Field = field; TabLabel = tabLabel; FieldLabel = fieldLabel;
            Uri = "kestrel://design?tab=" + tab + "&field=" + field;
            VoiceOver = "Fix it. Opens " + tabLabel + ", " + fieldLabel + ".";
        }

        public static FixField For(string kind)
        {
            switch (kind)
            {
                case "structural-maxq": return new FixField(kind, "structure", "safetyFactor", "Structure", "Safety factor");
                case "overg": return new FixField(kind, "guidance", "gLimit", "Guidance", "g limit");
                case "tumble": return new FixField(kind, "aero", "fins", "Aero", "Fins");
                case "engine-overpressure": return new FixField(kind, "engines", "engineParams.pc", "Engines", "Chamber pressure");
                case "bad-mixture": return new FixField(kind, "propellants", "mixtureOffsetPct", "Propellant", "Mixture");
                case "thermal": return new FixField(kind, "aero", "nose", "Aero", "Nose");
                case "cryo-material": return new FixField(kind, "tanks", "material", "Tanks", "Tank material");
                case "prop-depletion": return new FixField(kind, "propellants", "propMass", "Propellant", "Propellant load");
                // F2: a miss with no failure event (DesignerRepair.ReportKind) is a speed shortfall. Same link as
                // prop-depletion (FailureMonitor and validation file both under "propellants"). The designer still
                // opens Tanks, where propMass lives: DesignerRepair.FixTab maps it in ApplyReportedFix.
                case "dv-margin": return new FixField(kind, "propellants", "propMass", "Propellant", "Propellant load");
                case "separation-collision": return new FixField(kind, "stages", "sepImpulse", "Stages", "Separation push");
                case "leg-collapse": return new FixField(kind, "recovery", "legs.kind", "Recovery", "Landing legs");
                case "recovery-fuel": return new FixField(kind, "recovery", "landingReserve", "Recovery", "Landing reserve");
                case "recovery-fast": return new FixField(kind, "recovery", "legs.kind", "Recovery", "Landing legs");
                case "recovery-tilt": return new FixField(kind, "recovery", "landingReserve", "Recovery", "Landing reserve");
                case "recovery-miss": return new FixField(kind, "recovery", "recoveryMode", "Recovery", "Where it comes down");
                case "engine-out": return new FixField(kind, "engines", "engineCount", "Engines", "Engine count");
                default: return new FixField(kind ?? "", "structure", "safetyFactor", "Structure", "Safety factor");
            }
        }

        /// <summary>v2 designer controls that already exist. v3 tabs land later; the field id is still the link.</summary>
        public static string ControlId(string field)
        {
            if (field == "engineCount" || field == "engineParams.pc") return "engines";
            if (field == "propMass" || field == "mixtureOffsetPct") return "prop";
            return field;
        }

        /// <summary>Show the design panel and focus the control this failure asked for.</summary>
        public static void Apply(VisualElement root, FixField link)
        {
            if (root == null || link == null) return;
            Pending = link;
            var card = root.Q(ReportView.RootName);
            if (card != null) card.RemoveFromHierarchy();
            var design = root.Q("design");
            if (design != null)
            {
                design.RemoveFromClassList("hidden");
                design.AddToClassList("fix-target");
                design.tooltip = link.VoiceOver;
            }
            var hud = root.Q(UiIds.ScreenFlight);
            if (hud != null) hud.AddToClassList("hidden");
            var el = root.Q(ControlId(link.Field)) ?? root.Q(link.Field);
            if (el != null)
            {
                el.AddToClassList("fix-target");
                el.tooltip = link.VoiceOver;
                el.Focus();
            }
            var applied = OnApply;
            if (applied != null) applied(link);
        }
    }
}
