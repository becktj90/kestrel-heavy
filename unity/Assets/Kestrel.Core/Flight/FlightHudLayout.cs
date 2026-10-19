// Portrait flight HUD. Points, origin top-left.
//
// Trevor's device shots: the trajectory card and the stages panel were one opaque slab over the
// rocket, the telemetry sat on the curve, and the action dock (230 pt min) covered the lower stack.
// This places four bands that do not overlap on SE, Pro Max, and iPad portrait:
//   top    — mission clock and the stage chip
//   strip  — ALT, SPEED, Q, G, one row, text that fits the cell
//   cards  — stages panel on the right, short and translucent; the trajectory is a
//            transparent line up the left edge, with the center left open
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
        public HudBox Clock, Stage, Health, Telemetry, Plot, Stages, Dock, Info, Medal, Help, Quote, Timer;
        public HudBox Assist, Primary, Skip, Alternate, NudgeDown, NudgeUp, Follow, Pause, Exit;
        /// <summary>The "Cam: Auto" button, and where its five-chip picker opens (it is closed by default).</summary>
        public HudBox Camera, CameraPicker;
        public HudBox[] Cells = new HudBox[4];
        public FlightHudMode Mode;
    }

    public static class FlightHudLayout
    {
        public const float SideMarginPt = 8f;
        public const float GapPt = 4f;
        public const float PadPt = 4f;
        public const float HeaderPt = 36f;
        public const float HealthPt = 28f;
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
        /// <summary>The trajectory is a line. A filled slab behind it read as a grey panel.</summary>
        public const float TrajectoryAlpha = 0f;
        public const float RailMaxPt = 120f;
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

        /// <summary>
        /// Clock face that fits "T+ 00.0" and "T-00:00.0" inside the band. The 56 px
        /// stylesheet size spills out of the strip and covers the ALT cell.
        /// </summary>
        public static float ClockFontPt(float boxW, float boxH)
        {
            float font = 28f;
            float limit = boxH > 8f ? boxH - 4f : font;
            if (font > limit) font = limit;
            if (!(boxW > 8f)) return font > 13f ? font : 13f;
            while (font > 13f && (
                TextWidth("T+ 00.0", font) > boxW - 2f ||
                TextWidth("T-00:00.0", font) > boxW - 2f))
                font -= 0.5f;
            return font;
        }

        /// <summary>
        /// Height of the flight-notes sheet that sits above the button dock.
        /// It stays under the rocket band and scrolls when the copy is longer.
        /// </summary>
        public static float InfoViewPt(float screenH, float safeBottom, bool prompt)
        {
            if (!(screenH > 1f)) return HelpPt;
            float buttons = DockHeight(false, prompt);
            float dockTop = screenH - Math.Max(0f, safeBottom) - buttons;
            float ceiling = screenH * StackBottom + 8f;
            float room = dockTop - ceiling - GapPt;
            if (room < 48f) room = 48f;
            if (room > 96f) room = 96f;
            return room;
        }

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
            float healthY = safeTop + HeaderPt + GapPt;
            f.Health = Box(left, healthY, contentW, HealthPt);

            float telY = healthY + HealthPt + GapPt;
            f.Telemetry = Box(left, telY, contentW, TelemetryPt);
            float cellW = (contentW - CellGapPt * (TelemetryCount - 1)) / TelemetryCount;
            f.Cells = new HudBox[TelemetryCount];
            for (int i = 0; i < TelemetryCount; i++)
                f.Cells[i] = Box(left + i * (cellW + CellGapPt), telY, cellW, TelemetryPt);

            float cardsY = telY + TelemetryPt + GapPt;
            float cardW = contentW * CardFraction;
            float toolsY = dockTop - GapPt - ButtonPt;
            float railTop = cardsY;
            float railBot = toolsY - GapPt;
            if (railBot < railTop + 96f) railBot = railTop + 96f;
            float maxRight = width * 0.34f;
            float railW = RailMaxPt;
            if (left + railW > maxRight) railW = Math.Max(72f, maxRight - left);
            f.Plot = Box(left, railTop, railW, railBot - railTop);
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

            float followW = FollowWidthPt;
            float centerL = width * 0.35f;
            if (left + followW > centerL - GapPt) followW = Math.Max(ButtonPt, centerL - GapPt - left);
            f.Follow = Box(left, toolsY, followW, ButtonPt);
            float pauseX = right - PauseWidthPt;
            float centerR = width * 0.65f;
            if (pauseX < centerR + GapPt) pauseX = centerR + GapPt;
            f.Pause = Box(pauseX, toolsY, PauseWidthPt, ButtonPt);

            // Camera sits centred in the gap between Follow and Pause, the way the flex row lays it out.
            float camW = FlightView.WidestButton();
            float gapL = f.Follow.Right + GapPt;
            float gapR = f.Pause.X - GapPt;
            if (camW > gapR - gapL) camW = Math.Max(ButtonPt, gapR - gapL);
            f.Camera = Box((gapL + gapR - camW) * 0.5f, toolsY, camW, ButtonPt);

            // The picker opens above the tools row, right of the trajectory rail and under the stages card.
            float pickL = Math.Max(f.Plot.Right, left) + GapPt;
            float pickTop = Math.Max(f.Stages.Bottom, f.Telemetry.Bottom) + GapPt;
            var size = PickerSize(right - pickL, toolsY - GapPt - pickTop);
            float px = f.Camera.X + f.Camera.W * 0.5f - size.W * 0.5f;
            if (px + size.W > right) px = right - size.W;
            if (px < pickL) px = pickL;
            float py = toolsY - GapPt - size.H;
            if (py < pickTop) py = pickTop;
            f.CameraPicker = Box(px, py, size.W, size.H);
            return f;
        }

        /// <summary>
        /// Picker footprint for the room it has: as many chip columns as fit (one row of five on a wide
        /// screen, three by two at 335 pt), every chip a 44 pt target.
        /// </summary>
        public static HudBox PickerSize(float roomW, float roomH)
        {
            int n = FlightView.Picker.Length;
            float chip = FlightView.WidestChip();
            int cols = (int)Math.Floor((roomW - 2f * PadPt + GapPt) / (chip + GapPt));
            if (cols > n) cols = n;
            if (cols < 1) cols = 1;
            int rows = (n + cols - 1) / cols;
            // Too short for that many rows: go wider if the width allows it (never narrower than one chip).
            while (rows > 1 && rows * ButtonPt + (rows - 1) * GapPt + 2f * PadPt > roomH && cols < n)
            {
                cols++;
                rows = (n + cols - 1) / cols;
            }
            float w = cols * chip + (cols - 1) * GapPt + 2f * PadPt;
            float h = rows * ButtonPt + (rows - 1) * GapPt + 2f * PadPt;
            return new HudBox { W = w, H = h };
        }

        /// <summary>Chip boxes inside an open picker, in <see cref="FlightView.Picker"/> order, row by row.</summary>
        public static HudBox[] PickerChips(HudBox picker)
        {
            int n = FlightView.Picker.Length;
            var chips = new HudBox[n];
            if (!picker.Live) return chips;
            float chip = FlightView.WidestChip();
            int cols = (int)Math.Floor((picker.W - 2f * PadPt + GapPt) / (chip + GapPt) + 0.01f);
            if (cols > n) cols = n;
            if (cols < 1) cols = 1;
            float cw = (picker.W - 2f * PadPt - (cols - 1) * GapPt) / cols;
            for (int i = 0; i < n; i++)
            {
                int c = i % cols, r = i / cols;
                chips[i] = Box(picker.X + PadPt + c * (cw + GapPt), picker.Y + PadPt + r * (ButtonPt + GapPt), cw, ButtonPt);
            }
            return chips;
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

        /// <summary>
        /// Landscape web HUD. The stat strip runs across the top, the plot and the stages panel
        /// sit in side rails, and the rocket column between them stays clear down to a short dock.
        /// Portrait <see cref="Place(float, float, float, float, float, float, FlightHudMode)"/> is unchanged.
        /// </summary>
        public static FlightHudFrame PlaceWide(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, FlightHudMode mode)
        {
            if (!(width > height))
                return Place(width, height, safeTop, safeBottom, safeLeft, safeRight, mode);

            float edge = SideMarginPt;
            safeTop = Math.Max(0f, safeTop);
            safeBottom = Math.Max(0f, safeBottom);
            safeLeft = Math.Max(0f, safeLeft);
            safeRight = Math.Max(0f, safeRight);

            float exitW = HomeCornerLayout.ExitW;
            float exitH = HomeCornerLayout.ExitH;
            var exit = Box(width - safeRight - edge - exitW, safeTop, exitW, exitH);

            float left = safeLeft + edge;
            float right = width - safeRight - edge;
            float stripRight = exit.X - GapPt;
            float stripW = Math.Max(1f, stripRight - left);
            float stripH = Math.Max(HeaderPt, TelemetryPt);
            float stripY = safeTop;

            bool notes = mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes;
            bool prompt = mode == FlightHudMode.Prompt || mode == FlightHudMode.PromptNotes;
            bool quote = prompt && !notes;

            // Keep the dock below the chase-camera stack (62% of the screen) so the rocket stays visible.
            float stackFloor = height * StackBottom;
            float maxDock = height - safeBottom - stackFloor - GapPt;
            float wantDock = DockHeight(notes, prompt);
            float oneRow = PadPt + ButtonPt + PadPt;
            float dockH = wantDock;
            if (dockH > maxDock) dockH = Math.Max(oneRow, maxDock);
            if (dockH > height - safeBottom - stripY - stripH - 80f)
                dockH = Math.Max(oneRow, height - safeBottom - stripY - stripH - 80f);
            float dockTop = height - safeBottom - dockH;

            float railTop = Math.Max(stripY + stripH, exit.Bottom) + GapPt;
            float railBot = dockTop - GapPt;

            float innerW = Math.Max(1f, right - left);
            float minCenter = Math.Max(120f, width * 0.28f);
            float railW = innerW * 0.22f;
            if (railW < 132f) railW = 132f;
            if (railW > 320f) railW = 320f;
            if (left + railW * 2f + minCenter > right)
                railW = Math.Max(96f, (innerW - minCenter) * 0.5f);

            var f = new FlightHudFrame { Mode = mode, Exit = exit };
            f.Telemetry = Box(left, stripY, stripW, stripH);
            float clockW = Math.Min(120f, Math.Max(64f, stripW * 0.12f));
            f.Clock = Box(left, stripY, clockW, stripH);
            f.Stage = Box(stripRight - StageChipPt, stripY + (stripH - StageChipPt) * 0.5f, StageChipPt, StageChipPt);
            float cellsL = f.Clock.Right + GapPt;
            float cellsR = f.Stage.X - GapPt;
            float cellW = (Math.Max(4f, cellsR - cellsL) - CellGapPt * (TelemetryCount - 1)) / TelemetryCount;
            f.Cells = new HudBox[TelemetryCount];
            for (int i = 0; i < TelemetryCount; i++)
                f.Cells[i] = Box(cellsL + i * (cellW + CellGapPt), stripY, Math.Max(1f, cellW), stripH);

            f.Dock = Box(left, dockTop, innerW, dockH);
            PackDock(f, notes, prompt, quote);

            float toolsH = ButtonPt;
            bool helpInRail = notes && !Contains(f.Dock, f.Help);
            bool quoteInRail = quote && !Contains(f.Dock, f.Quote);
            float leftExtra = (helpInRail ? HelpPt + GapPt : 0f) + (quoteInRail ? QuotePt + GapPt : 0f);

            float followY = railBot - toolsH;
            f.Health = Box(left, railTop, railW, HealthPt);
            float plotY = f.Health.Bottom + GapPt;
            float plotH = followY - GapPt - leftExtra - plotY;
            if (plotH < 48f) plotH = 48f;
            f.Plot = Box(left, plotY, railW, plotH);
            float yL = f.Plot.Bottom + GapPt;
            if (helpInRail)
            {
                f.Help = Box(left, yL, railW, HelpPt);
                yL = f.Help.Bottom + GapPt;
            }
            if (quoteInRail)
            {
                f.Quote = Box(left, yL, railW, QuotePt);
                yL = f.Quote.Bottom + GapPt;
            }
            f.Follow = Box(left, yL, railW, toolsH);

            bool nudgesInRail = prompt && !Contains(f.Dock, f.NudgeDown);
            // Camera shares the Pause row when the rail is wide enough; a narrow rail stacks it above Pause.
            float camW = FlightView.WidestButton();
            bool camBeside = railW >= camW + GapPt + PauseWidthPt;
            float rightExtra = (nudgesInRail ? ButtonPt + GapPt : 0f) + (camBeside ? 0f : toolsH + GapPt);
            float stagesH = followY - GapPt - rightExtra - railTop;
            if (stagesH < 48f) stagesH = 48f;
            f.Stages = Box(right - railW, railTop, railW, stagesH);
            float yR = f.Stages.Bottom + GapPt;
            if (nudgesInRail)
            {
                float col = (railW - 2f * GapPt) / 3f;
                f.Alternate = Box(f.Stages.X, yR, col, ButtonPt);
                f.NudgeDown = Box(f.Stages.X + col + GapPt, yR, col, ButtonPt);
                f.NudgeUp = Box(f.Stages.X + 2f * (col + GapPt), yR, col, ButtonPt);
                yR += ButtonPt + GapPt;
            }
            if (camBeside)
            {
                f.Camera = Box(right - railW, yR, camW, toolsH);
                f.Pause = Box(f.Camera.Right + GapPt, yR, right - f.Camera.Right - GapPt, toolsH);
            }
            else
            {
                f.Camera = Box(right - railW, yR, railW, toolsH);
                f.Pause = Box(right - railW, yR + toolsH + GapPt, railW, toolsH);
            }

            // The picker opens toward the centre: left of the button, inside the rocket column, above the dock.
            var open = RocketColumn(f);
            var size = PickerSize(open.W, open.H);
            float px = f.Camera.X - GapPt - size.W;
            if (px + size.W > open.Right) px = open.Right - size.W;
            if (px < open.X) px = open.X;
            float py = f.Camera.Bottom - size.H;
            if (py + size.H > open.Bottom) py = open.Bottom - size.H;
            if (py < open.Y) py = open.Y;
            f.CameraPicker = Box(px, py, Math.Min(size.W, open.W), Math.Min(size.H, open.H));
            return f;
        }

        static void PackDock(FlightHudFrame f, bool notes, bool prompt, bool quote)
        {
            float inL = f.Dock.X + PadPt;
            float inW = f.Dock.W - 2f * PadPt;
            float y = f.Dock.Y + PadPt;
            float maxY = f.Dock.Bottom - PadPt;
            bool Room(float h) => y + h <= maxY + 0.6f;

            if (!Room(ButtonPt)) return;
            f.Info = Box(inL + inW - InfoPt, y + (ButtonPt - InfoPt) * 0.5f, InfoPt, InfoPt);
            f.Medal = Box(inL, y, Math.Max(44f, inW - InfoPt - GapPt), ButtonPt);
            y += ButtonPt + GapPt;

            if (!Room(ButtonPt)) return;
            if (prompt)
            {
                float col = (inW - 2f * GapPt) / 3f;
                f.Primary = Box(inL, y, col, ButtonPt);
                f.Skip = Box(inL + col + GapPt, y, col, ButtonPt);
                f.Assist = Box(inL + 2f * (col + GapPt), y, col, ButtonPt);
            }
            else
            {
                float col = (inW - 2f * GapPt) / 3f;
                f.Assist = Box(inL, y, col, ButtonPt);
                f.NudgeDown = Box(inL + col + GapPt, y, col, ButtonPt);
                f.NudgeUp = Box(inL + 2f * (col + GapPt), y, col, ButtonPt);
            }
            y += ButtonPt + GapPt;
            if (prompt && Room(ButtonPt))
            {
                float col = (inW - 2f * GapPt) / 3f;
                f.Alternate = Box(inL, y, col, ButtonPt);
                f.NudgeDown = Box(inL + col + GapPt, y, col, ButtonPt);
                f.NudgeUp = Box(inL + 2f * (col + GapPt), y, col, ButtonPt);
                y += ButtonPt + GapPt;
            }
            if (prompt && Room(TimerPt))
            {
                f.Timer = Box(inL, y, inW, TimerPt);
                y += TimerPt + GapPt;
            }
            if (quote && Room(QuotePt))
            {
                f.Quote = Box(inL, y, inW, QuotePt);
                y += QuotePt + GapPt;
            }
            if (notes && Room(HelpPt))
                f.Help = Box(inL, y, inW, HelpPt);
        }

        static bool Contains(HudBox outer, HudBox inner)
        {
            if (!inner.Live || !outer.Live) return false;
            return inner.X >= outer.X - 0.6f && inner.Right <= outer.Right + 0.6f
                && inner.Y >= outer.Y - 0.6f && inner.Bottom <= outer.Bottom + 0.6f;
        }

        /// <summary>Open column between the side rails, under the stat strip and above the dock.</summary>
        public static HudBox RocketColumn(FlightHudFrame f)
        {
            float x = f.Plot.Right + GapPt;
            float y = f.Telemetry.Bottom + GapPt;
            float r = f.Stages.X - GapPt;
            float b = f.Dock.Y - GapPt;
            return Box(x, y, Math.Max(1f, r - x), Math.Max(1f, b - y));
        }
    }
}
