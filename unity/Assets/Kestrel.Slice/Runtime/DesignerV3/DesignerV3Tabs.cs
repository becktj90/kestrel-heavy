// Designer v3 tab bar (§6): Propellant · Engines · Tanks · Stages · Aero · Structure ·
// Guidance · Recovery · Payload · Look. Simple view shows a kid subset; Engineer shows every field.
using System;

namespace Kestrel.Slice.DesignerV3
{
    public enum DesignerV3Tab
    {
        Propellant,
        Engines,
        Tanks,
        Stages,
        Aero,
        Structure,
        Guidance,
        Recovery,
        Payload,
        Look,
    }

    public static class DesignerV3Tabs
    {
        public static readonly DesignerV3Tab[] All =
        {
            DesignerV3Tab.Propellant, DesignerV3Tab.Engines, DesignerV3Tab.Tanks, DesignerV3Tab.Stages,
            DesignerV3Tab.Aero, DesignerV3Tab.Structure, DesignerV3Tab.Guidance, DesignerV3Tab.Recovery,
            DesignerV3Tab.Payload, DesignerV3Tab.Look,
        };

        /// <summary>Simple (kid) tabs: engines, tanks, stages, recovery, look. Engineer unlocks the rest.</summary>
        public static readonly DesignerV3Tab[] Simple =
        {
            DesignerV3Tab.Engines, DesignerV3Tab.Tanks, DesignerV3Tab.Stages, DesignerV3Tab.Recovery, DesignerV3Tab.Look,
        };

        public static string Label(DesignerV3Tab tab, bool engineer) => tab switch
        {
            DesignerV3Tab.Propellant => engineer ? "Propellant" : "Fuel mix",
            DesignerV3Tab.Engines => engineer ? "Engines" : "Engines",
            DesignerV3Tab.Tanks => engineer ? "Tanks" : "Tanks",
            DesignerV3Tab.Stages => engineer ? "Stages" : "Stages",
            DesignerV3Tab.Aero => engineer ? "Aero" : "Shape",
            DesignerV3Tab.Structure => engineer ? "Structure" : "Strength",
            DesignerV3Tab.Guidance => engineer ? "Guidance" : "Flight path",
            DesignerV3Tab.Recovery => engineer ? "Recovery" : "Landing",
            DesignerV3Tab.Payload => engineer ? "Payload" : "Cargo",
            DesignerV3Tab.Look => "Look",
            _ => tab.ToString(),
        };

        public static string VoiceOver(DesignerV3Tab tab) => "Tab " + Label(tab, true);

        public static DesignerV3Tab[] Visible(bool engineer) => engineer ? All : Simple;

        public static bool TryParse(string id, out DesignerV3Tab tab)
        {
            if (Enum.TryParse(id, true, out tab)) return true;
            tab = DesignerV3Tab.Engines;
            return false;
        }

        /// <summary>Map a FlightRisk.tab string from FailureModel onto a designer tab.</summary>
        public static DesignerV3Tab FromRiskTab(string riskTab)
        {
            if (string.IsNullOrEmpty(riskTab)) return DesignerV3Tab.Structure;
            switch (riskTab.ToLowerInvariant())
            {
                case "propellant": case "fuel": return DesignerV3Tab.Propellant;
                case "engines": case "engine": return DesignerV3Tab.Engines;
                case "tanks": case "tank": return DesignerV3Tab.Tanks;
                case "stages": case "stage": return DesignerV3Tab.Stages;
                case "aero": case "fins": case "fairing": return DesignerV3Tab.Aero;
                case "structure": case "material": return DesignerV3Tab.Structure;
                case "guidance": case "avionics": return DesignerV3Tab.Guidance;
                case "recovery": case "landing": case "legs": return DesignerV3Tab.Recovery;
                case "payload": return DesignerV3Tab.Payload;
                case "look": return DesignerV3Tab.Look;
                default: return DesignerV3Tab.Structure;
            }
        }
    }
}
