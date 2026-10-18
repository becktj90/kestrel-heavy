// Mission-card layout and the honesty check against the delta-v model.
// KH-1 through KH-10 are the flights the viable designs already pass.
// A later flight is hard when it asks for more speed, more mass, a longer coast,
// or a crew landing those designs have not shown.
using System;

namespace Kestrel.Core.Flight
{
    public static class MissionHonesty
    {
        /// <summary>KH-5 and KH-8 flew 12,000 kg to a low circle.</summary>
        public const double ProvenLeoMassKg = 12000;
        /// <summary>KH-10 flew 7,000 kg to the transfer and brought the boosters home.</summary>
        public const double ProvenGtoMassKg = 7000;
        /// <summary>KH-9 flew 3,000 kg on C3 -2.</summary>
        public const double ProvenC3MassKg = 3000;
        /// <summary>KH-9 coasted 3 hours. Longer than that is unproven boil-off.</summary>
        public const double ProvenCoastH = 3;
        /// <summary>KH-8 holds a 400 km circle at 4.5 g. A lower cap on an orbit is unproven.</summary>
        public const double ProvenMinOrbitalG = 4.5;
        /// <summary>KH-8 flew the crew capsule to 400 km. A higher crew circle steps past that.</summary>
        public const double ProvenCrewApoKm = 400;
        public const double FairingMaxM = 5;
        public const double FairingClearanceM = 0.2;

        public static readonly string[] RequirementIds =
        {
            "margin", "maxG", "strapOns", "landing", "reliability", "noRedRisks", "randomFailures", "coastHours",
        };

        public static bool KnownRequirement(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < RequirementIds.Length; i++)
                if (RequirementIds[i] == id) return true;
            return false;
        }

        /// <summary>achievable when the ask stays inside the proven envelope. hard when it steps past it.</summary>
        public static string Band(CampaignMission mission, double provenC3Mps)
        {
            if (mission == null) return "hard";
            bool hard = false;
            double ask = mission.RequiredDeltaV();
            if (ask > provenC3Mps + 1) hard = true;
            string kind = mission.TargetKind ?? "";
            if (kind == "ellipse" && mission.ApoKm > 10000 && mission.MassKg > ProvenGtoMassKg) hard = true;
            if (kind == "c3" && mission.MassKg > ProvenC3MassKg) hard = true;
            if (kind == "circular" && mission.MassKg > ProvenLeoMassKg) hard = true;
            if (mission.CoastHours > ProvenCoastH) hard = true;
            if (!double.IsNaN(mission.MaxG) && mission.MaxG > 0 && mission.MaxG < ProvenMinOrbitalG && kind != "apogee") hard = true;
            if (mission.LandingRequired && mission.TargetKind == "ellipse" && mission.LandingMode == "rtls" && mission.MassKg > ProvenGtoMassKg) hard = true;
            if (mission.LandingRequired && mission.PayloadId == "crew-capsule") hard = true;
            if (mission.PayloadId == "crew-capsule" && kind == "circular" && mission.ApoKm > ProvenCrewApoKm + 1) hard = true;
            return hard ? "hard" : "achievable";
        }
    }

    public static class CodexTitles
    {
        public static string Title(string id)
        {
            switch (id)
            {
                case "delta-v": return "Delta-v";
                case "twr": return "Thrust-to-weight";
                case "isp": return "Specific impulse";
                case "staging": return "Staging";
                case "max-q": return "Max-Q";
                case "cg-cp": return "Center of mass and center of pressure";
                case "orbits": return "Orbits";
                case "booster-landing": return "Booster landing";
                default: return "";
            }
        }

        public static string Line(string id)
        {
            string title = Title(id);
            return title.Length == 0 ? "" : "Codex · " + title;
        }
    }

    public struct MissionCardFrame
    {
        public HudBox Art, What, Does, Why, Customer, Control, Codex, Build, Back;
        public float ColumnW, ScrollH;
    }

    /// <summary>
    /// Portrait briefing card. Art and the "what it is" line sit in the first screen.
    /// The rest scrolls. Buttons stay 44 pt and out of the scroll.
    /// </summary>
    public static class MissionCardLayout
    {
        public const float MarginPt = 16f;
        public const float GapPt = 8f;
        public const float ArtPt = 96f;
        public const float LinePt = 18f;
        public const float TitlePt = 36f;
        public const float ButtonPt = 44f;
        public const float FontPt = 15f;
        public const float Advance = 0.62f;

        public static MissionCardFrame Place(FlightHudLayout.Device d, string what, string does, string why, string customer, string control, string codex)
        {
            float column = d.W - d.SafeLeft - d.SafeRight - MarginPt * 2f;
            if (column < 120f) column = 120f;
            float x = d.SafeLeft + MarginPt;
            float safeH = d.H - d.SafeTop - d.SafeBottom;
            float buttons = ButtonPt * 2f + GapPt;
            float scrollH = safeH - TitlePt - buttons;
            if (scrollH < ArtPt) scrollH = ArtPt;
            float y = d.SafeTop + TitlePt;

            float art = ArtPt;
            if (art > column) art = column;
            var frame = new MissionCardFrame { ColumnW = column, ScrollH = scrollH };
            frame.Art = Box(x + (column - art) * 0.5f, y, art, art);
            y = frame.Art.Bottom + GapPt;
            frame.What = Block(x, ref y, column, what);
            frame.Does = Block(x, ref y, column, does);
            frame.Why = Block(x, ref y, column, why);
            frame.Customer = Block(x, ref y, column, customer);
            frame.Control = Block(x, ref y, column, control);
            frame.Codex = Block(x, ref y, column, codex);

            float by = d.H - d.SafeBottom - ButtonPt;
            frame.Back = Box(x, by, column, ButtonPt);
            frame.Build = Box(x, by - GapPt - ButtonPt, column, ButtonPt);
            return frame;
        }

        static HudBox Block(float x, ref float y, float column, string text)
        {
            if (string.IsNullOrEmpty(text)) return new HudBox();
            int lines = Lines(text, column);
            float h = lines * LinePt;
            var box = Box(x, y, column, h);
            y = box.Bottom + GapPt;
            return box;
        }

        public static int Lines(string text, float column)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            float width = text.Length * FontPt * Advance;
            if (!(column > 1f)) return 1;
            int n = (int)Math.Ceiling(width / column);
            return n < 1 ? 1 : n;
        }

        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
