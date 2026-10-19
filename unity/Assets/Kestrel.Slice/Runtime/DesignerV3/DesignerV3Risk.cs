// Red-risk launch confirm (§3 / §6): red risks show "Launch anyway"; tapping a chip jumps to its field.
using System.Collections.Generic;
using Kestrel.Core;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Slice;

namespace Kestrel.Slice.DesignerV3
{
    public static class DesignerV3Risk
    {
        public const string LaunchAnywayLabel = "Launch anyway";
        public const string LaunchLabel = "Launch";
        public const string ConfirmTitle = "Red risks on this design";

        public static string LaunchButtonLabel(bool hasRed, bool confirmArmed) =>
            hasRed && !confirmArmed ? LaunchAnywayLabel : LaunchLabel;

        public static bool NeedsConfirm(bool hasRed, bool confirmArmed) => hasRed && !confirmArmed;

        public static bool CanLaunch(bool hasRed, bool confirmArmed) => !hasRed || confirmArmed;

        public static CellState LevelState(string level)
        {
            if (level == "red") return CellState.Bad;
            if (level == "amber") return CellState.Warn;
            if (level == "green") return CellState.Good;
            return CellState.Neutral;
        }

        public static string ChipLabel(FlightRisk risk, bool engineer)
        {
            if (risk == null) return "";
            string plain = ShownText.Plain(risk.kind, risk.kid, risk.engineer);
            if (!engineer) return ShownText.Screen(plain);
            string detail = ShownText.Screen(risk.engineer ?? "");
            if (string.IsNullOrEmpty(detail) || detail == plain || ShownText.Jargon(risk.engineer))
                return ShownText.Screen(plain);
            return ShownText.Screen(plain + " " + detail);
        }

        public static string ChipTooltip(FlightRisk risk)
        {
            if (risk == null) return "Risk";
            string body = !string.IsNullOrEmpty(risk.engineer) ? risk.engineer : (risk.kid ?? "");
            return PlayerText.ForPlayer(body) + ". Tap to open the control.";
        }

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
