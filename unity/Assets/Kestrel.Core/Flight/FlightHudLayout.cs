// Portrait flight HUD. Points, origin top-left.
//
// Trevor's device shots: the trajectory card and the stages panel were one opaque slab over the
// rocket, the telemetry sat on the curve, and the action dock (230 pt min) covered the lower stack.
// This places four bands that do not overlap on SE, Pro Max, and iPad portrait:
//   top    — mission clock and the stage chip
//   strip  — ALT, SPEED, Q, G, one row, text that fits the cell
//   cards  — flight-path plot (left) and stages panel (right), short and translucent, center open
//   dock   — medal timing, auto-assist, nudges; long notes behind the info control
// The chase camera keeps the stack between StackTop and StackBottom of the screen, so the dock
// and the cards stay off the vehicle. Pinch, orbit, and Follow are unchanged.
using System;

namespace Kestrel.Core.Flight
{
    public enum FlightHudMode
    {
        Cruise = 0,
        Notes = 1,
        Prompt = 2,
        PromptNotes = 3,
    }

    public struct HudBox
    {
        public float X, Y, W, H;
        public float Right => X + W;
        public float Bottom => Y + H;
        public bool Live => W > 0.5f && H > 0.5f;

        public bool Overlaps(HudBox o)
        {
            if (!Live || !o.Live) return false;
            return X < o.Right - 0.4f && Right > o.X + 0.4f && Y < o.Bottom - 0.4f && Bottom > o.Y + 0.4f;
        }
    }

    public sealed class FlightHudFrame
    {
        public HudBox Clock, Stage, Telemetry, Plot, Stages, Dock, Info, Medal, Help, Quote, Timer;
        public HudBox Assist, Primary, Skip, Alternate, NudgeDown, NudgeUp, Follow, Pause;
        public HudBox[] Cells = new HudBox[4];
        public FlightHudMode Mode;
    }

    public static class FlightHudLayout
    {
        public const float SideMarginPt = 8f;
        public const float GapPt = 4f;
        public const float PadPt = 4f;
        public const float HeaderPt = 36f;
        public const float TelemetryPt = 36f;
        public const float PlotPt = 72f;
        public const float CardFraction = 0.32f;
        public const float ButtonPt = 44f;
        public const float InfoPt = 44f;
        public const float HelpPt = 40f;
        public const float QuotePt = 18f;
        public const float TimerPt = 6f;
        public const float StageChipPt = 32f;
        public const float FollowWidthPt = 112f;
        public const float PauseWidthPt = 76f;
        public const float PlotAlpha = 0.32f;
        public const float StagesAlpha = 0.32f;
        public const float DockAlpha = 0.90f;
        /// <summary>Chase framing keeps the stack inside this band of the portrait screen (top-left fractions).</summary>
        public const float StackTop = 0.38f;
        public const float StackBottom = 0.62f;
        public const float LabelFontPt = 11f;
        public const float ValueFontPt = 12f;
        public const float FontAdvance = 0.62f;
        public const float CellGapPt = 4f;
        public const float CellPadPt = 4f;
        public const int TelemetryCount = 4;

        public static readonly string[] TelemetryLabels = { "ALT", "SPEED", "Q", "G" };
        public static readonly string[] ValueSamples = { "999.9 km", "0.00 km/s", "999 kPa", "00.0", "100%", "TWR", "PROP" };

        public static float TextWidth(string text, float fontPt)
        {
            if (string.IsNullOrEmpty(text) || !(fontPt > 0f)) return 0f;
            return text.Length * fontPt * FontAdvance;
        }

        public readonly struct Device
        {
            public readonly string Name;
            public readonly float W, H, SafeTop, SafeBottom, SafeLeft, SafeRight;
            public Device(string name, float w, float h, float safeTop, float safeBottom, float safeLeft, float safeRight)
            {
                Name = name; W = w; H = h; SafeTop = safeTop; SafeBottom = safeBottom; SafeLeft = safeLeft; SafeRight = safeRight;
            }
        }

        /// <summary>Portrait fixtures Trevor flies: SE, Pro Max, iPad. Insets are points.</summary>
        public static Device[] PortraitDevices() => new[]
        {
            new Device("SE", 375f, 667f, 20f, 0f, 0f, 0f),
            new Device("SE", 375f, 667f, 0f, 20f, 0f, 0f),
            new Device("Pro Max", 440f, 956f, 62f, 34f, 0f, 0f),
            new Device("iPad", 1032f, 1376f, 24f, 20f, 0f, 0f),
        };

        public static FlightHudFrame Place(Device d, FlightHudMode mode) =>
            Place(d.W, d.H, d.SafeTop, d.SafeBottom, d.SafeLeft, d.SafeRight, mode);

        public static FlightHudFrame Place(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, FlightHudMode mode)
        {
            float left = safeLeft + SideMarginPt;
            float right = width - safeRight - SideMarginPt;
            float contentW = right - left;
            if (contentW < 1f) contentW = 1f;
            bool notes = mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes;
            bool prompt = mode == FlightHudMode.Prompt || mode == FlightHudMode.PromptNotes;
            bool quote = prompt && !notes;

            float dockH = DockHeight(notes, prompt);
            float dockTop = height - safeBottom - dockH;

            var f = new FlightHudFrame { Mode = mode };
            f.Clock = Box(left, safeTop, Math.Max(44f, contentW - StageChipPt - GapPt), HeaderPt);
            f.Stage = Box(right - StageChipPt, safeTop + (HeaderPt - StageChipPt) * 0.5f, StageChipPt, StageChipPt);

            float telY = safeTop + HeaderPt + GapPt;
            f.Telemetry = Box(left, telY, contentW, TelemetryPt);
            float cellW = (contentW - CellGapPt * (TelemetryCount - 1)) / TelemetryCount;
            f.Cells = new HudBox[TelemetryCount];
            for (int i = 0; i < TelemetryCount; i++)
                f.Cells[i] = Box(left + i * (cellW + CellGapPt), telY, cellW, TelemetryPt);

            float cardsY = telY + TelemetryPt + GapPt;
            float cardW = contentW * CardFraction;
            f.Plot = Box(left, cardsY, cardW, PlotPt);
            f.Stages = Box(right - cardW, cardsY, cardW, PlotPt);

            f.Dock = Box(left, dockTop, contentW, dockH);
            float innerL = left + PadPt;
            float innerW = contentW - 2f * PadPt;
            float y = dockTop + PadPt;
            f.Info = Box(innerL + innerW - InfoPt, y + (ButtonPt - InfoPt) * 0.5f, InfoPt, InfoPt);
            f.Medal = Box(innerL, y, innerW - InfoPt - GapPt, ButtonPt);
            y = Next(y, ButtonPt);
            if (notes)
            {
                f.Help = Box(innerL, y, innerW, HelpPt);
                y = Next(y, HelpPt);
            }
            if (quote)
            {
                f.Quote = Box(innerL, y, innerW, QuotePt);
                y = Next(y, QuotePt);
            }
            if (prompt)
            {
                f.Timer = Box(innerL, y, innerW, TimerPt);
                y = Next(y, TimerPt);
                float col3 = (innerW - 2f * GapPt) / 3f;
                f.Primary = Box(innerL, y, col3, ButtonPt);
                f.Skip = Box(innerL + col3 + GapPt, y, col3, ButtonPt);
                f.Assist = Box(innerL + 2f * (col3 + GapPt), y, col3, ButtonPt);
                y = Next(y, ButtonPt);
                f.Alternate = Box(innerL, y, col3, ButtonPt);
                f.NudgeDown = Box(innerL + col3 + GapPt, y, col3, ButtonPt);
                f.NudgeUp = Box(innerL + 2f * (col3 + GapPt), y, col3, ButtonPt);
            }
            else
            {
                f.Assist = Box(innerL, y, innerW, ButtonPt);
                y = Next(y, ButtonPt);
                float col = (innerW - GapPt) * 0.5f;
                f.NudgeDown = Box(innerL, y, col, ButtonPt);
                f.NudgeUp = Box(innerL + col + GapPt, y, col, ButtonPt);
            }

            float toolsY = dockTop - GapPt - ButtonPt;
            float followW = FollowWidthPt;
            float centerL = width * 0.35f;
            if (left + followW > centerL - GapPt) followW = Math.Max(ButtonPt, centerL - GapPt - left);
            f.Follow = Box(left, toolsY, followW, ButtonPt);
            float pauseX = right - PauseWidthPt;
            float centerR = width * 0.65f;
            if (pauseX < centerR + GapPt) pauseX = centerR + GapPt;
            f.Pause = Box(pauseX, toolsY, PauseWidthPt, ButtonPt);
            return f;
        }

        public static float DockHeight(bool notes, bool prompt)
        {
            bool quote = prompt && !notes;
            float h = PadPt + ButtonPt;
            if (notes) h += GapPt + HelpPt;
            if (quote) h += GapPt + QuotePt;
            if (prompt) h += GapPt + TimerPt;
            h += GapPt + ButtonPt;
            h += GapPt + ButtonPt;
            h += PadPt;
            return h;
        }

        static float Next(float y, float h) => y + h + GapPt;

        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
