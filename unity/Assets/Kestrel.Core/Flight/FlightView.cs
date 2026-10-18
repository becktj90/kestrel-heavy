// One saved camera. Auto is the altitude-aware chase. The other four are fixed frames.
// The button cycles them mid-flight. The value lives in the save record kh.save.v1.settings.
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public enum FlightViewMode
    {
        Auto = 0,
        Chase = 1,
        Above = 2,
        Below = 3,
        Pad = 4,
    }

    /// <summary>Offset in metres. OnVehicle: X lateral, Y along the vehicle up, Z world-back.
    /// Pad: the same axes from the pad, and the camera does not follow the rocket.</summary>
    public struct FlightViewOffset
    {
        public bool OnVehicle;
        public double X, Y, Z;
        /// <summary>Auto keeps ChaseCamera.DesiredOffset, including the horizon hold.</summary>
        public bool UseChase;
    }

    public static class FlightView
    {
        public const string RecordKey = "kh.save.v1.settings";
        public const string PrefKey = "kh.flight.camera";
        public const float ButtonFontPt = 13f;
        public const float ButtonPadPt = 16f;
        public const float MinButtonPt = 52f;

        public static readonly FlightViewMode[] Cycle =
        {
            FlightViewMode.Auto, FlightViewMode.Chase, FlightViewMode.Above, FlightViewMode.Below, FlightViewMode.Pad,
        };

        public static string Name(FlightViewMode mode)
        {
            switch (mode)
            {
                case FlightViewMode.Chase: return "Chase";
                case FlightViewMode.Above: return "Above";
                case FlightViewMode.Below: return "Below";
                case FlightViewMode.Pad: return "Pad";
                default: return "Auto";
            }
        }

        public static FlightViewMode Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) return FlightViewMode.Auto;
            switch (name.Trim().ToLowerInvariant())
            {
                case "chase": return FlightViewMode.Chase;
                case "above": return FlightViewMode.Above;
                case "below": return FlightViewMode.Below;
                case "pad": return FlightViewMode.Pad;
                default: return FlightViewMode.Auto;
            }
        }

        public static FlightViewMode Next(FlightViewMode mode)
        {
            for (int i = 0; i < Cycle.Length; i++)
                if (Cycle[i] == mode) return Cycle[(i + 1) % Cycle.Length];
            return FlightViewMode.Auto;
        }

        public static JsonObject Data(FlightViewMode mode)
        {
            var o = new JsonObject();
            o.Add("camera", Name(mode).ToLowerInvariant());
            return o;
        }

        public static FlightViewMode FromData(JsonObject data)
        {
            if (data == null) return FlightViewMode.Auto;
            return Parse(data.Str("camera"));
        }

        /// <summary>Vehicle height in metres. Auto defers to the chase camera.</summary>
        public static FlightViewOffset Of(FlightViewMode mode, double height)
        {
            double h = height > 1 ? height : 1;
            switch (mode)
            {
                case FlightViewMode.Chase:
                    return new FlightViewOffset { OnVehicle = true, X = h * 1.35, Y = h * 0.12, Z = -h * 2.7 };
                case FlightViewMode.Above:
                    return new FlightViewOffset { OnVehicle = true, X = 0, Y = h * 1.45, Z = -h * 0.42 };
                case FlightViewMode.Below:
                    return new FlightViewOffset { OnVehicle = true, X = h * 0.28, Y = -h * 0.82, Z = -h * 0.35 };
                case FlightViewMode.Pad:
                    return new FlightViewOffset { OnVehicle = false, X = h * 1.05, Y = h * 0.38, Z = -h * 2.35 };
                default:
                    return new FlightViewOffset { OnVehicle = true, UseChase = true };
            }
        }

        public static float LabelWidth(string label, float fontPt)
        {
            if (string.IsNullOrEmpty(label) || !(fontPt > 0f)) return 0f;
            return label.Length * fontPt * FlightHudLayout.FontAdvance;
        }

        /// <summary>The control is at least as wide as the caption, and the row fits a 335 pt portrait or landscape.</summary>
        public static float ButtonWidth(FlightViewMode mode)
        {
            float text = LabelWidth(Name(mode), ButtonFontPt);
            float w = text + ButtonPadPt;
            if (w < MinButtonPt) w = MinButtonPt;
            return w;
        }

        public static bool FitsWidth(float screenWidthPt)
        {
            float widest = MinButtonPt;
            for (int i = 0; i < Cycle.Length; i++)
            {
                float w = ButtonWidth(Cycle[i]);
                if (w > widest) widest = w;
            }
            float row = FlightHudLayout.SideMarginPt * 2f + FlightHudLayout.FollowWidthPt + FlightHudLayout.GapPt
                + widest + FlightHudLayout.GapPt + FlightHudLayout.PauseWidthPt;
            return row <= screenWidthPt + 0.5f && widest + ButtonPadPt <= screenWidthPt - FlightHudLayout.SideMarginPt * 2f;
        }
    }
}
