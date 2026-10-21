// One saved camera. Auto is the altitude-aware chase. The other four are fixed frames.
// The button reads "Cam: <mode>" and opens a five-chip picker; C on web cycles without it.
// The value lives in the save record kh.save.v1.settings. Poses: FlightViewPose.
using Kestrel.Core.Designer;
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

        public const string CaptionPrefix = "Cam: ";
        public const float ChipFontPt = 13f;
        public const float ChipPadPt = 16f;
        public const float MinChipPt = 56f;
        /// <summary>Seconds the C-key toast stays up.</summary>
        public const float ToastSec = 1f;

        /// <summary>Picker chips, in the brief's reading order. C and the old tap cycle use <see cref="Cycle"/>.</summary>
        public static readonly FlightViewMode[] Picker =
        {
            FlightViewMode.Auto, FlightViewMode.Above, FlightViewMode.Below, FlightViewMode.Chase, FlightViewMode.Pad,
        };

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

        /// <summary>The button caption. "Auto" alone did not read as a camera control.</summary>
        public static string Caption(FlightViewMode mode) => CaptionPrefix + Name(mode);

        /// <summary>The C-key toast.</summary>
        public static string Toast(FlightViewMode mode) => "Camera: " + Name(mode);

        /// <summary>Follow button label while the camera is chasing the rocket.</summary>
        public const string FollowingLabel = "Following";
        /// <summary>Follow button label when tapping it would change the framing.</summary>
        public const string FollowLabel = "Follow rocket";

        /// <summary>Every view but Pad rides with the rocket. Pad is the fixed ground camera.</summary>
        public static bool Chases(FlightViewMode mode) => mode != FlightViewMode.Pad;

        /// <summary>The camera follows the rocket: no manual orbit, and a view that rides with it.</summary>
        public static bool Following(bool noOrbit, FlightViewMode mode) => noOrbit && Chases(mode);

        /// <summary>
        /// The view Follow lands on. Follow clears the manual orbit and, from the fixed Pad view, goes back
        /// to Auto (the chase). From any chasing view it keeps the player's choice.
        /// </summary>
        public static FlightViewMode FollowView(FlightViewMode mode) => Chases(mode) ? mode : FlightViewMode.Auto;

        /// <summary>
        /// The Follow button toggles (#332, Trevor: "tapping Follow does nothing"). While following, a tap lets go:
        /// the camera stays on the ground (Pad). Not following, a tap goes back to the last chasing view the
        /// player had (or Auto), and the orbit is cleared.
        /// </summary>
        public static FlightViewMode FollowToggle(FlightViewMode mode, bool following, FlightViewMode lastChase) =>
            following ? FlightViewMode.Pad : Chases(lastChase) ? lastChase : FlightViewMode.Auto;

        /// <summary>"Following" when tapping would change nothing, "Follow rocket" when it would.</summary>
        public static string FollowCaption(bool following) => following ? FollowingLabel : FollowLabel;

        public static string FollowTip(bool following) => following
            ? "The camera is following the rocket. Tap to let go and watch from the pad."
            : "Follow the rocket: the camera glides back behind it.";

        public static string Tip(FlightViewMode mode) => "Camera: " + Name(mode) + ". Tap to pick a view. C cycles on a keyboard.";

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

        /// <summary>
        /// Write the camera onto a settings record without dropping the other fields.
        /// The prefs mirror shares <see cref="RecordKey"/>.
        /// </summary>
        public static JsonObject Merge(JsonObject existing, FlightViewMode mode)
        {
            var next = existing != null ? JsonMut.CloneObject(existing) : new JsonObject();
            JsonMut.Set(next, "camera", Name(mode).ToLowerInvariant());
            return next;
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
            float text = LabelWidth(Caption(mode), ButtonFontPt);
            float w = text + ButtonPadPt;
            if (w < MinButtonPt) w = MinButtonPt;
            return w;
        }

        /// <summary>The widest caption, so the button does not change size as the mode changes.</summary>
        public static float WidestButton()
        {
            float widest = MinButtonPt;
            for (int i = 0; i < Cycle.Length; i++)
            {
                float w = ButtonWidth(Cycle[i]);
                if (w > widest) widest = w;
            }
            return widest;
        }

        public static float ChipWidth(FlightViewMode mode)
        {
            float w = LabelWidth(Name(mode), ChipFontPt) + ChipPadPt;
            return w < MinChipPt ? MinChipPt : w;
        }

        public static float WidestChip()
        {
            float widest = MinChipPt;
            for (int i = 0; i < Picker.Length; i++)
            {
                float w = ChipWidth(Picker[i]);
                if (w > widest) widest = w;
            }
            return widest;
        }

        public static bool FitsWidth(float screenWidthPt)
        {
            float widest = WidestButton();
            float row = FlightHudLayout.SideMarginPt * 2f + FlightHudLayout.FollowWidthPt + FlightHudLayout.GapPt
                + widest + FlightHudLayout.GapPt + FlightHudLayout.PauseWidthPt;
            return row <= screenWidthPt + 0.5f && widest + ButtonPadPt <= screenWidthPt - FlightHudLayout.SideMarginPt * 2f;
        }
    }
}
