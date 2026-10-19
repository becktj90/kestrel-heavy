// Nav-harness ids for the flight HUD (screen-flight) and its pause menu (screen-pause).
// Convention: screen roots are screen-<id>; controls are <screen>-<control>[-<option>], kebab-case,
// set as the UI Toolkit element name. Other screens add their own UiIds.<Screen>.cs part.
using Kestrel.Core.Flight;

namespace Kestrel.Core.Ui
{
    public static partial class UiIds
    {
        // ---- screen-flight ---------------------------------------------------------------------
        public const string ScreenFlight = "screen-flight";

        /// <summary>Tools row: Follow rocket | Cam: &lt;mode&gt; | Pause.</summary>
        public const string FlightFollow = "flight-follow";
        public const string FlightCamera = "flight-camera";
        public const string FlightPause = "flight-pause";

        /// <summary>Camera picker chips (inside flight-camera-picker).</summary>
        public const string FlightCameraAuto = "flight-camera-auto";
        public const string FlightCameraAbove = "flight-camera-above";
        public const string FlightCameraBelow = "flight-camera-below";
        public const string FlightCameraChase = "flight-camera-chase";
        public const string FlightCameraPad = "flight-camera-pad";
        /// <summary>The picker panel and the C-key toast (containers, not controls).</summary>
        public const string FlightCameraPicker = "flight-camera-picker";
        public const string FlightCameraToast = "flight-camera-toast";

        /// <summary>"BACK TO DESIGN" after the flight ends: the in-HUD way out of screen-flight.</summary>
        public const string FlightExit = "flight-exit";
        /// <summary>Webcast "Skip count" during the countdown.</summary>
        public const string FlightSkipCount = "flight-skip-count";

        /// <summary>Ascent dock: flight notes, decision prompt, assist and pitch nudges.</summary>
        public const string FlightInfo = "flight-info";
        public const string FlightPrimary = "flight-primary";
        public const string FlightAlternate = "flight-alternate";
        public const string FlightSkip = "flight-skip";
        public const string FlightAssist = "flight-assist";
        public const string FlightNudgeDown = "flight-nudge-down";
        public const string FlightNudgeUp = "flight-nudge-up";

        /// <summary>Launch-health strip cells: flight-health-0 .. flight-health-(n-1).</summary>
        public const string FlightHealthPrefix = "flight-health-";
        public static string FlightHealth(int index) => FlightHealthPrefix + index;

        public static string FlightCameraChip(FlightViewMode mode)
        {
            switch (mode)
            {
                case FlightViewMode.Above: return FlightCameraAbove;
                case FlightViewMode.Below: return FlightCameraBelow;
                case FlightViewMode.Chase: return FlightCameraChase;
                case FlightViewMode.Pad: return FlightCameraPad;
                default: return FlightCameraAuto;
            }
        }

        // ---- screen-pause (the in-flight pause menu) ------------------------------------------
        public const string ScreenPause = "screen-pause";
        public const string PauseResume = "pause-resume";
        public const string PauseRestart = "pause-restart";
        public const string PauseExitHome = "pause-exit-home";
        public const string PauseMusic = "pause-music";
        public const string PauseSfx = "pause-sfx";
        public const string PauseNarration = "pause-narration";

        /// <summary>Every flight and pause control id, for the harness and the uniqueness test.</summary>
        public static readonly string[] FlightControls =
        {
            FlightFollow, FlightCamera, FlightPause,
            FlightCameraAuto, FlightCameraAbove, FlightCameraBelow, FlightCameraChase, FlightCameraPad,
            FlightExit, FlightSkipCount, FlightInfo, FlightPrimary, FlightAlternate, FlightSkip, FlightAssist,
            FlightNudgeDown, FlightNudgeUp,
            PauseResume, PauseRestart, PauseExitHome, PauseMusic, PauseSfx, PauseNarration,
        };
    }
}
