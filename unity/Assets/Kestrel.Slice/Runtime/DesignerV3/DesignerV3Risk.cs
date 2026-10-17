// Red-risk launch confirm (§3 / §6): red risks show "Launch anyway"; tapping a chip jumps to its field.
using System.Collections.Generic;
using Kestrel.Core;
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
            string body = engineer && !string.IsNullOrEmpty(risk.engineer) ? risk.engineer : risk.kid;
            if (string.IsNullOrEmpty(body)) body = risk.kid ?? "";
            return PlayerText.ForPlayer(body);
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
