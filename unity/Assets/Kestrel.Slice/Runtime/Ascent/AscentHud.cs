// Portrait ascent HUD. Clock and stage chip stay in the flight header. Under them: one telemetry
// strip, then the stages panel on the right. The trajectory is a transparent line up the left
// edge: milestones, a fill, and a marker that move with the flight. Auto-assist, nudges, and the
// medal line share a bottom dock. Long notes sit behind Info.
// Pause, pinch, orbit, and Follow are not touched. Guide copy is not touched.
using System;
using System.Globalization;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class AscentHud
    {
        const int CellCount = 4;

        VisualElement hud, root, cards, gap, actions, nudgeRow, promptRow, assistRow, dockSpacer;
        ScrollView help;
        VisualElement stages, webcastHome;
        int webcastHomeIndex = -1;
        Label medal, quote, detail, caption, bonus, extra;
        Label[] cellKey = new Label[CellCount];
        Label[] cellVal = new Label[CellCount];
        Button primary, alternate, skip, assist, nudgeDown, nudgeUp, info;
        VisualElement health;
        Label healthDetail;
        Button[] healthButtons;
        int healthOpen = -1;
        VisualElement marker, green, bar;
        TrajectoryRailView plot;
        readonly TrajectoryWatch trajectory = new TrajectoryWatch();
        int fedEvents;
        AscentCall shown;
        FlightDriver flight;
        float upp = 1f;
        bool infoOpen, nudgeRowOpen = true;

        public void Open(VisualElement flightHud, float unitsPerPoint)
        {
            Close();
            hud = flightHud;
            upp = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            if (hud == null) return;
            var grid = hud.Q("hud-grid");
            if (grid != null) grid.style.display = DisplayStyle.None;
            root = new VisualElement { name = "ascent-hud", pickingMode = PickingMode.Ignore };
            root.style.flexShrink = 0;
            BuildHealth();
            BuildStrip();
            BuildCards();
            BuildActions();
            var band = hud.Q("hud-band");
            if (band != null) hud.Insert(hud.IndexOf(band) + 1, root);
            else hud.Insert(0, root);
            dockSpacer = new VisualElement { name = "ascent-dock-spacer", pickingMode = PickingMode.Ignore };
            var pause = hud.Q(UiIds.ScreenPause);
            if (pause != null) hud.Insert(hud.IndexOf(pause), actions);
            else hud.Add(actions);
            if (pause != null) hud.Insert(hud.IndexOf(pause), dockSpacer);
            else hud.Add(dockSpacer);
            pause?.BringToFront();
            var tools = hud.Q("flight-tools");
            if (tools != null) tools.pickingMode = PickingMode.Ignore;
            Layout(upp);
        }

        public void Close()
        {
            // Hand the tools row (Follow, Cam, Pause) back to the flex flow before the HUD goes.
            ClearWidePins();
            infoOpen = false;
            healthOpen = -1;
            if (help != null && help.parent != null) help.RemoveFromHierarchy();
            if (hud != null) hud.EnableInClassList("hud-info", false);
            RestoreStages();
            var grid = hud != null ? hud.Q("hud-grid") : null;
            if (grid != null) grid.style.display = StyleKeyword.Null;
            if (root != null && root.parent != null) root.RemoveFromHierarchy();
            if (actions != null && actions.parent != null) actions.RemoveFromHierarchy();
            if (dockSpacer != null && dockSpacer.parent != null) dockSpacer.RemoveFromHierarchy();
            if (plot != null && plot.parent != null) plot.RemoveFromHierarchy();
            plot = null;
            trajectory.Reset();
            fedEvents = 0;
            root = null;
            actions = null;
            dockSpacer = null;
            hud = null;
        }

        bool widePinned;
        /// <summary>The dock mode the landscape pins were laid for; null when not pinned.</summary>
        FlightHudMode? wideMode;
        /// <summary>The dock mode the landscape pins were last laid for (tests).</summary>
        public FlightHudMode? WideMode => wideMode;

        /// <summary>
        /// Device tests only: lay out as landscape web at this CSS viewport (zero insets) or as portrait,
        /// instead of reading Screen and the browser. Null leaves the live checks.
        /// </summary>
        public static bool? ForceWide;
        public static float ForceCssW, ForceCssH;

        static bool UseWideNow() => ForceWide ?? WebLayoutMode.UseWide(Screen.width, Screen.height);

        public void Layout(float unitsPerPoint)
        {
            upp = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            if (root == null) return;
            if (!UseWideNow()) ClearWidePins();
            float gapPx = Px(FlightHudLayout.GapPt);
            float plotH = Px(FlightHudLayout.PlotPt);
            root.style.marginTop = gapPx;
            var strip = root.Q("ascent-telemetry");
            if (strip != null)
            {
                strip.style.height = Px(FlightHudLayout.TelemetryPt);
                strip.style.marginBottom = gapPx;
                strip.style.paddingLeft = Px(FlightHudLayout.CellPadPt);
                strip.style.paddingRight = Px(FlightHudLayout.CellPadPt);
                strip.style.borderTopLeftRadius = Px(10f);
                strip.style.borderTopRightRadius = Px(10f);
                strip.style.borderBottomLeftRadius = Px(10f);
                strip.style.borderBottomRightRadius = Px(10f);
            }
            for (int i = 0; i < CellCount; i++)
            {
                if (cellKey[i] != null) cellKey[i].style.fontSize = Px(FlightHudLayout.LabelFontPt);
                if (cellVal[i] != null) cellVal[i].style.fontSize = Px(FlightHudLayout.ValueFontPt);
            }
            if (cards != null) cards.style.height = plotH;
            if (plot != null)
            {
                float r = Px(12f);
                plot.style.borderTopLeftRadius = r;
                plot.style.borderTopRightRadius = r;
                plot.style.borderBottomLeftRadius = r;
                plot.style.borderBottomRightRadius = r;
            }
            StyleStages();
            if (actions == null) return;
            actions.style.paddingLeft = Px(FlightHudLayout.PadPt);
            actions.style.paddingRight = Px(FlightHudLayout.PadPt);
            actions.style.paddingTop = Px(FlightHudLayout.PadPt);
            actions.style.paddingBottom = Px(FlightHudLayout.PadPt);
            float radius = Px(16f);
            actions.style.borderTopLeftRadius = radius;
            actions.style.borderTopRightRadius = radius;
            StyleButton(primary);
            StyleButton(alternate);
            StyleButton(skip);
            StyleButton(assist);
            StyleButton(nudgeDown);
            StyleButton(nudgeUp);
            if (info != null)
            {
                float s = Px(FlightHudLayout.InfoPt);
                info.style.width = s;
                info.style.minWidth = s;
                info.style.height = s;
                info.style.minHeight = s;
                info.style.fontSize = Px(16f);
                info.style.marginTop = 0;
                info.style.marginLeft = Px(FlightHudLayout.GapPt);
            }
            if (medal != null) medal.style.fontSize = Px(13f);
            var medalRow = actions.Q("ascent-medal-row");
            if (medalRow != null)
            {
                medalRow.style.height = Px(FlightHudLayout.ButtonPt);
                medalRow.style.minHeight = Px(FlightHudLayout.ButtonPt);
                medalRow.style.maxHeight = Px(FlightHudLayout.ButtonPt);
            }
            if (quote != null)
            {
                quote.style.fontSize = Px(13f);
                quote.style.height = Px(FlightHudLayout.QuotePt);
                quote.style.marginTop = Px(FlightHudLayout.GapPt);
            }
            if (bar != null)
            {
                bar.style.height = Px(FlightHudLayout.TimerPt);
                bar.style.marginTop = Px(FlightHudLayout.GapPt);
            }
            if (health != null)
            {
                health.style.marginBottom = Px(FlightHudLayout.GapPt);
                for (int i = 0; healthButtons != null && i < healthButtons.Length; i++)
                {
                    var b = healthButtons[i];
                    if (b == null) continue;
                    float hh = Px(FlightHudLayout.HealthPt);
                    b.style.height = hh;
                    b.style.minHeight = hh;
                    b.style.fontSize = Px(11f);
                    FlightDusk.Face(b, FlightDusk.BodyFace());
                }
                if (healthDetail != null) FlightDusk.Sentence(healthDetail, upp);
            }
            if (detail != null) detail.style.fontSize = Px(12f);
            if (caption != null) caption.style.fontSize = Px(12f);
            if (bonus != null) bonus.style.fontSize = Px(12f);
            if (extra != null) extra.style.fontSize = Px(12f);
            if (promptRow != null) promptRow.style.marginTop = Px(FlightHudLayout.GapPt);
            if (assistRow != null) assistRow.style.marginTop = Px(FlightHudLayout.GapPt);
            if (nudgeRow != null) nudgeRow.style.marginTop = Px(FlightHudLayout.GapPt);
            PlaceDockSpacer();
            if (UseWideNow()) LayoutWide();
            else LayoutRail();
            PlaceInfoSheet();
            hud.Q(UiIds.ScreenPause)?.BringToFront();
        }

        /// <summary>Portrait: the tested rail, inside the HUD's already-padded content box.</summary>
        void LayoutRail()
        {
            if (plot == null || hud == null) return;
            float padL = hud.resolvedStyle.paddingLeft;
            float padT = hud.resolvedStyle.paddingTop;
            float padR = hud.resolvedStyle.paddingRight;
            float padB = hud.resolvedStyle.paddingBottom;
            float w = hud.resolvedStyle.width - padL - padR;
            float h = hud.resolvedStyle.height - padT - padB;
            if (w < 8f || h < 8f || !(upp > 0f)) return;
            bool prompt = shown != default;
            bool notes = infoOpen;
            var mode = prompt && notes ? FlightHudMode.PromptNotes
                : prompt ? FlightHudMode.Prompt
                : notes ? FlightHudMode.Notes
                : FlightHudMode.Cruise;
            var frame = FlightHudLayout.Place(w / upp, h / upp, 0f, 0f, 0f, 0f, mode);
            if (plot.parent != hud) hud.Insert(0, plot);
            Pin(plot, new HudBox
            {
                X = frame.Plot.X * upp,
                Y = frame.Plot.Y * upp,
                W = frame.Plot.W * upp,
                H = frame.Plot.H * upp,
            });
            plot.style.backgroundColor = new Color(0.04f, 0.07f, 0.12f, FlightHudLayout.TrajectoryAlpha);
            plot.Apply(trajectory.At(flight != null ? flight.SimTime : 0, flight != null ? flight.Altitude : 0, flight != null ? flight.Speed : 0), upp);
            var dock = ScaleBox(frame.Dock);
            Pin(actions, dock);
            if (actions != null) actions.style.overflow = Overflow.Hidden;
            DressClock(frame.Clock.W, frame.Clock.H);
            Seat(actions != null ? actions.Q("ascent-medal-row") : null, ScaleBox(Union(frame.Medal, frame.Info)), dock);
            Seat(quote, ScaleBox(frame.Quote), dock);
            Seat(bar, ScaleBox(frame.Timer), dock);
            if (prompt)
            {
                Seat(promptRow, ScaleBox(Union(frame.Primary, frame.Skip, frame.Assist)), dock);
                if (assistRow != null) assistRow.style.display = DisplayStyle.None;
            }
            else
            {
                if (promptRow != null) promptRow.style.display = DisplayStyle.None;
                Seat(assistRow, ScaleBox(frame.Assist), dock);
            }
            Seat(nudgeRow, ScaleBox(Union(frame.Alternate, frame.NudgeDown, frame.NudgeUp)), dock);
            // The camera button stays in the flex tools row; its picker opens above that row.
            FlightChrome.SeatPicker(hud, ScaleBox(frame.CameraPicker), ScaleAll(FlightHudLayout.PickerChips(frame.CameraPicker), ScaleBox));
        }

        static HudBox[] ScaleAll(HudBox[] boxes, Func<HudBox, HudBox> scale)
        {
            var o = new HudBox[boxes.Length];
            for (int i = 0; i < boxes.Length; i++) o[i] = scale(boxes[i]);
            return o;
        }

        HudBox ScaleBox(HudBox b) => new HudBox { X = b.X * upp, Y = b.Y * upp, W = b.W * upp, H = b.H * upp };

        void Seat(VisualElement row, HudBox box, HudBox dock)
        {
            if (row == null) return;
            if (actions == null || !box.Live || !dock.Live)
            {
                row.style.display = DisplayStyle.None;
                return;
            }
            float top = box.Y - dock.Y;
            float left = box.X - dock.X;
            float h = box.H;
            float w = box.W;
            if (top < 0f) { h += top; top = 0f; }
            if (left < 0f) { w += left; left = 0f; }
            if (top + h > dock.H) h = dock.H - top;
            if (left + w > dock.W) w = dock.W - left;
            if (h < 8f || w < 8f)
            {
                row.style.display = DisplayStyle.None;
                return;
            }
            if (row.parent != actions) actions.Add(row);
            row.style.display = DisplayStyle.Flex;
            row.style.position = Position.Absolute;
            row.style.left = left;
            row.style.top = top;
            row.style.width = w;
            row.style.height = h;
            row.style.marginTop = 0;
            row.style.marginBottom = 0;
            row.style.marginLeft = 0;
            row.style.maxHeight = h;
            row.style.minHeight = 0;
        }

        static HudBox Union(params HudBox[] boxes)
        {
            float x = float.MaxValue, y = float.MaxValue, right = 0f, bottom = 0f;
            bool any = false;
            for (int i = 0; i < boxes.Length; i++)
            {
                var box = boxes[i];
                if (!box.Live) continue;
                any = true;
                if (box.X < x) x = box.X;
                if (box.Y < y) y = box.Y;
                if (box.Right > right) right = box.Right;
                if (box.Bottom > bottom) bottom = box.Bottom;
            }
            if (!any) return default;
            return new HudBox { X = x, Y = y, W = right - x, H = bottom - y };
        }

        /// <summary>Landscape web: stat strip across the top, plot and stages in the side rails, dock under the rocket.</summary>
        void LayoutWide()
        {
            if (ForceWide == true && ForceCssW > 8f && ForceCssH > 8f)
            {
                LayoutWideAt(ForceCssW, ForceCssH, 0f, 0f, 0f, 0f);
                return;
            }
            WebViewport.Css(out float cssW, out float cssH);
            if (cssW < 8f || cssH < 8f) return;
            float perX = Screen.width > 1 ? Screen.width / cssW : 1f;
            float perY = Screen.height > 1 ? Screen.height / cssH : 1f;
            var safe = Screen.safeArea;
            float safeTop = (Screen.height - safe.yMax) / perY;
            float safeBottom = safe.yMin / perY;
            float safeLeft = safe.xMin / perX;
            float safeRight = (Screen.width - safe.xMax) / perX;
            LayoutWideAt(cssW, cssH, safeTop, safeBottom, safeLeft, safeRight);
        }

        /// <summary>
        /// Landscape pins for a CSS viewport. Public so the device tests can drive it at 1280x720,
        /// 1366x768 and 1920x1080 without a real browser window.
        /// </summary>
        public void LayoutWideAt(float cssW, float cssH, float safeTop, float safeBottom, float safeLeft, float safeRight)
        {
            float pw = hud != null ? hud.resolvedStyle.width : 0f;
            float ph = hud != null ? hud.resolvedStyle.height : 0f;
            // Before the first layout pass (or off-panel in a test) the inline size is the panel size.
            if (!(pw >= 8f) && hud != null && hud.style.width.keyword == StyleKeyword.Undefined) pw = hud.style.width.value.value;
            if (!(ph >= 8f) && hud != null && hud.style.height.keyword == StyleKeyword.Undefined) ph = hud.style.height.value.value;
            if (!(pw >= 8f) || !(ph >= 8f) || root == null) return;
            bool prompt = shown != default;
            var mode = FlightHudLayout.WideMode(prompt, infoOpen);
            var f = FlightHudLayout.PlaceWide(cssW, cssH, safeTop, safeBottom, safeLeft, safeRight, mode);
            float ux = pw / cssW;
            float uy = ph / cssH;
            HudBox Sc(HudBox b) => new HudBox { X = b.X * ux, Y = b.Y * uy, W = b.W * ux, H = b.H * uy };
            bool InDock(HudBox b) => b.Live && b.Y >= f.Dock.Y - 1f && b.Bottom <= f.Dock.Bottom + 1.5f;

            var band = hud.Q("hud-band");
            var telemetry = Sc(f.Telemetry);
            Pin(band, telemetry);
            if (band != null)
            {
                band.pickingMode = PickingMode.Ignore;
                band.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
            var topRow = hud.Q(className: "hud-top");
            if (topRow != null)
            {
                topRow.style.position = Position.Absolute;
                topRow.style.left = 0; topRow.style.top = 0; topRow.style.right = 0; topRow.style.bottom = 0;
            }
            var clock = Sc(f.Clock);
            var stage = Sc(f.Stage);
            Pin(hud.Q("h-t"), new HudBox { X = clock.X - telemetry.X, Y = 0, W = clock.W, H = telemetry.H });
            Pin(hud.Q("h-stage"), new HudBox { X = stage.X - telemetry.X, Y = (telemetry.H - stage.H) * 0.5f, W = stage.W, H = stage.H });

            root.style.position = Position.Absolute;
            root.style.left = 0; root.style.top = 0; root.style.right = 0; root.style.bottom = 0;
            root.style.marginTop = 0;
            if (cards != null)
            {
                cards.style.position = Position.Absolute;
                cards.style.left = 0; cards.style.top = 0; cards.style.right = 0; cards.style.bottom = 0;
                cards.style.height = StyleKeyword.Auto;
            }
            float cellX = f.Cells[0].X;
            float cellR = f.Cells[f.Cells.Length - 1].Right;
            var cellBox = Sc(new HudBox { X = cellX, Y = f.Telemetry.Y, W = cellR - cellX, H = f.Telemetry.H });
            Pin(root.Q("ascent-telemetry"), cellBox);
            Pin(health, Sc(f.Health));
            if (health != null) health.style.overflow = Overflow.Visible;
            Pin(plot, Sc(f.Plot));
            Pin(stages, Sc(f.Stages));
            DressClock(f.Clock.W, f.Clock.H);

            var tools = hud.Q("flight-tools");
            if (tools != null)
            {
                tools.style.position = Position.Absolute;
                tools.style.left = 0; tools.style.top = 0; tools.style.right = 0; tools.style.bottom = 0;
                tools.style.marginBottom = 0;
            }
            PinTool(hud.Q(UiIds.FlightFollow), Sc(f.Follow));
            PinTool(hud.Q(UiIds.FlightCamera), Sc(f.Camera));
            PinTool(hud.Q(UiIds.FlightPause), Sc(f.Pause));
            // flight-tools is a full-HUD box that ignores picking; keep it (and the picker) above the
            // later-inserted ascent root so taps reach the buttons.
            tools?.BringToFront();
            var chips = FlightHudLayout.PickerChips(f.CameraPicker);
            for (int i = 0; i < chips.Length; i++) chips[i] = Sc(chips[i]);
            FlightChrome.SeatPicker(hud, Sc(f.CameraPicker), chips);
            Pin(actions, Sc(f.Dock));
            if (actions != null) actions.style.overflow = Overflow.Hidden;
            if (dockSpacer != null) dockSpacer.style.height = 0;

            var dock = Sc(f.Dock);
            Seat(actions != null ? actions.Q("ascent-medal-row") : null, Sc(Union(f.Medal, f.Info)), dock);
            PlaceRailOrDock(help, f.Help, InDock(f.Help), Sc(f.Help), actions);
            PlaceRailOrDock(quote, f.Quote, InDock(f.Quote), Sc(f.Quote), actions);
            if (quote != null && f.Quote.Live)
                quote.style.whiteSpace = WhiteSpace.Normal;
            PlaceRailOrDock(bar, f.Timer, InDock(f.Timer), Sc(f.Timer), actions);
            if (prompt)
            {
                Seat(promptRow, Sc(Union(f.Primary, f.Skip, f.Assist)), dock);
                if (assistRow != null) assistRow.style.display = DisplayStyle.None;
            }
            else
            {
                if (promptRow != null) promptRow.style.display = DisplayStyle.None;
                Seat(assistRow, Sc(f.Assist), dock);
            }
            var nudges = Union(f.Alternate, f.NudgeDown, f.NudgeUp);
            if (InDock(f.NudgeDown) || InDock(f.NudgeUp))
                Seat(nudgeRow, Sc(nudges), dock);
            else if (nudges.Live)
            {
                if (hud != null && nudgeRow != null && nudgeRow.parent != hud) hud.Add(nudgeRow);
                Pin(nudgeRow, Sc(nudges));
            }
            else if (nudgeRow != null)
                nudgeRow.style.display = DisplayStyle.None;
            widePinned = true;
            wideMode = mode;
        }

        void PlaceRailOrDock(VisualElement e, HudBox css, bool inDock, HudBox panel, VisualElement dock)
        {
            if (e == null) return;
            if (!css.Live)
            {
                e.style.display = DisplayStyle.None;
                return;
            }
            e.style.display = DisplayStyle.Flex;
            if (inDock)
            {
                if (dock != null && e.parent != dock) dock.Add(e);
                e.style.position = StyleKeyword.Null;
                e.style.left = StyleKeyword.Null;
                e.style.top = StyleKeyword.Null;
                e.style.height = panel.H;
                e.style.maxHeight = panel.H;
                e.style.marginTop = 0;
                return;
            }
            if (hud != null && e.parent != hud) hud.Add(e);
            Pin(e, panel);
        }

        static void FitDockRow(VisualElement row, float height)
        {
            if (row == null) return;
            if (height <= 0.5f)
            {
                row.style.display = DisplayStyle.None;
                return;
            }
            row.style.display = DisplayStyle.Flex;
            row.style.height = height;
            row.style.minHeight = height;
            row.style.maxHeight = height;
            row.style.marginTop = 0;
        }

        void ClearWidePins()
        {
            if (!widePinned) return;
            widePinned = false;
            wideMode = null;
            Unpin(hud != null ? hud.Q("hud-band") : null);
            Unpin(hud != null ? hud.Q("h-t") : null);
            Unpin(hud != null ? hud.Q("h-stage") : null);
            var topRow = hud != null ? hud.Q(className: "hud-top") : null;
            Unpin(topRow);
            if (root != null)
            {
                Unpin(root);
                root.style.marginTop = StyleKeyword.Null;
                Unpin(root.Q("ascent-telemetry"));
            }
            Unpin(cards);
            Unpin(health);
            if (health != null && root != null && health.parent != root) root.Insert(0, health);
            Unpin(plot);
            Unpin(stages);
            var tools = hud != null ? hud.Q("flight-tools") : null;
            Unpin(tools);
            if (tools != null) tools.style.marginBottom = StyleKeyword.Null;
            // Landscape brought the row to the front; portrait needs it back above the webcast block.
            var webcast = hud != null ? hud.Q("webcast") : null;
            if (tools != null && webcast != null && tools.parent == hud && webcast.parent == hud
                && hud.IndexOf(tools) > hud.IndexOf(webcast))
                hud.Insert(hud.IndexOf(webcast), tools);
            UnpinTool(hud != null ? hud.Q(UiIds.FlightFollow) : null);
            UnpinTool(hud != null ? hud.Q(UiIds.FlightCamera) : null);
            UnpinTool(hud != null ? hud.Q(UiIds.FlightPause) : null);
            Unpin(actions);
            if (actions != null) actions.style.overflow = StyleKeyword.Null;
            ReturnToDock(help);
            ReturnToDock(quote);
            ReturnToDock(bar);
            ShowRow(actions != null ? actions.Q("ascent-medal-row") : null);
            ShowRow(promptRow);
            ShowRow(assistRow);
            ShowRow(nudgeRow);
        }

        void ReturnToDock(VisualElement e)
        {
            if (e == null || actions == null) return;
            if (e.parent != actions) actions.Add(e);
            e.style.display = StyleKeyword.Null;
            Unpin(e);
            e.style.maxHeight = StyleKeyword.Null;
        }

        static void ShowRow(VisualElement row)
        {
            if (row == null) return;
            row.style.display = StyleKeyword.Null;
            Unpin(row);
            row.style.minHeight = StyleKeyword.Null;
            row.style.maxHeight = StyleKeyword.Null;
        }

        /// <summary>
        /// Pin a tools-row button to its exact box. FlightChrome sizes these with min-width in its own units;
        /// on a scaled web panel (1280x800 CSS) that min-width outgrew the pinned box and Cam ran into Pause.
        /// </summary>
        static void PinTool(VisualElement e, HudBox b)
        {
            Pin(e, b);
            if (e == null || !b.Live) return;
            e.style.minWidth = b.W;
            e.style.maxWidth = b.W;
            e.style.minHeight = b.H;
            e.style.maxHeight = b.H;
            e.style.marginLeft = 0;
            e.style.marginRight = 0;
            e.style.flexShrink = 0;
        }

        static void Pin(VisualElement e, HudBox b)
        {
            if (e == null || !b.Live) return;
            e.style.position = Position.Absolute;
            e.style.left = b.X;
            e.style.top = b.Y;
            e.style.width = b.W;
            e.style.height = b.H;
            e.style.right = StyleKeyword.Auto;
            e.style.bottom = StyleKeyword.Auto;
            e.style.marginTop = 0;
            e.style.marginBottom = 0;
            e.style.flexGrow = 0;
        }

        /// <summary>Undo PinTool; FlightChrome.Layout restores its own min-width on the next portrait pass.</summary>
        void UnpinTool(VisualElement e)
        {
            Unpin(e);
            if (e == null) return;
            e.style.minWidth = StyleKeyword.Null;
            e.style.maxWidth = StyleKeyword.Null;
            e.style.minHeight = StyleKeyword.Null;
            e.style.maxHeight = StyleKeyword.Null;
            e.style.marginLeft = StyleKeyword.Null;
            e.style.marginRight = StyleKeyword.Null;
            e.style.flexShrink = StyleKeyword.Null;
        }

        static void Unpin(VisualElement e)
        {
            if (e == null) return;
            e.style.position = StyleKeyword.Null;
            e.style.left = StyleKeyword.Null;
            e.style.top = StyleKeyword.Null;
            e.style.width = StyleKeyword.Null;
            e.style.height = StyleKeyword.Null;
            e.style.right = StyleKeyword.Null;
            e.style.bottom = StyleKeyword.Null;
            e.style.flexGrow = StyleKeyword.Null;
        }

        public void Present(FlightDriver driver)
        {
            flight = driver;
            if (root == null || flight == null) return;
            var director = flight.Decisions;
            if (director == null)
            {
                root.style.display = DisplayStyle.None;
                if (actions != null) actions.style.display = DisplayStyle.None;
                return;
            }
            root.style.display = DisplayStyle.Flex;
            if (actions != null) actions.style.display = DisplayStyle.Flex;
            var inv = CultureInfo.InvariantCulture;
            double alt = director.LiveAlt > 0 ? director.LiveAlt : flight.Altitude;
            double speed = director.LiveSpeed > 0 ? director.LiveSpeed : flight.Speed;
            double q = director.LiveQ > 0 ? director.LiveQ : flight.DynPressure;
            double g = director.LiveG;
            var prompts = director.OpenPrompts();
            bool anomaly = prompts.Length > 0 && prompts[0].Call == AscentCall.EngineOut;
            string speedText = speed >= 1000
                ? (speed / 1000.0).ToString("0.00", inv) + " km/s"
                : speed.ToString("0", inv) + " m/s";
            SetCell(0, "ALT", (alt / 1000.0).ToString("0.0", inv) + " km", new Color(0.85f, 0.92f, 0.96f));
            SetCell(1, "SPEED", speedText, new Color(0.85f, 0.92f, 0.96f));
            if (anomaly)
            {
                SetCell(2, "TWR", director.LiveTwr.ToString("0.0", inv), new Color(0.94f, 0.63f, 0.12f));
                SetCell(3, "PROP", (director.LiveProp * 100.0).ToString("0", inv) + "%", new Color(0.30f, 0.82f, 0.55f));
            }
            else
            {
                SetCell(2, "Q", (q / 1000.0).ToString("0", inv) + " kPa", new Color(0.94f, 0.63f, 0.12f));
                SetCell(3, "G", g.ToString("0.0", inv), new Color(0.30f, 0.82f, 0.55f));
            }
            FeedTrajectory(director, alt, speed, q);
            if (plot != null) plot.Apply(trajectory.At(flight.SimTime, alt, speed), upp);
            var score = director.Score();
            if (medal != null)
                medal.text = score.Decided > 0 ? "Medal timing: " + score.Summary : "Flying the plan.";
            bool prompt = prompts.Length > 0;
            if (!prompt)
            {
                shown = default;
                if (quote != null) quote.style.display = DisplayStyle.None;
                if (bar != null) bar.style.display = DisplayStyle.None;
                if (primary != null) primary.style.display = DisplayStyle.None;
                if (alternate != null) alternate.style.display = DisplayStyle.None;
                if (skip != null) skip.style.display = DisplayStyle.None;
                if (detail != null)
                    detail.text = director.Engineer
                        ? "Engineer nudge offsets the planned pitch. The amber path is that offset."
                        : "Auto-assist flies the timed calls. Tap one for a gold medal.";
            }
            else
            {
                var p = prompts[0];
                shown = p.Call;
                if (quote != null)
                {
                    quote.text = p.Quote;
                    quote.style.display = infoOpen ? DisplayStyle.None : DisplayStyle.Flex;
                }
                if (bar != null) bar.style.display = DisplayStyle.Flex;
                if (detail != null) detail.text = p.Eyebrow + "  ·  " + p.Detail;
                if (primary != null)
                {
                    primary.style.display = DisplayStyle.Flex;
                    primary.text = p.Primary;
                    primary.tooltip = p.Quote + " " + p.Primary;
                    primary.style.backgroundColor = p.Call == AscentCall.EngineOut || p.Call == AscentCall.FuelBalance
                        ? new Color(0.94f, 0.63f, 0.08f)
                        : new Color(0.23f, 0.83f, 0.96f);
                }
                if (alternate != null)
                {
                    alternate.style.display = p.ShowAlternate ? DisplayStyle.Flex : DisplayStyle.None;
                    alternate.text = p.Alternate ?? "";
                    alternate.tooltip = p.Alternate ?? "";
                }
                if (skip != null) skip.style.display = DisplayStyle.Flex;
                if (green != null)
                {
                    green.style.left = Length.Percent((float)(p.GreenStart * 100));
                    green.style.width = Length.Percent(Mathf.Max(4f, (float)(p.GreenEnd - p.GreenStart) * 100f));
                }
                if (marker != null) marker.style.left = Length.Percent(Mathf.Clamp01((float)p.Marker) * 100f);
            }
            if (assist != null)
            {
                assist.text = director.AutoAssist ? "Auto-assist on" : "Auto-assist";
                assist.tooltip = director.AutoAssist
                    ? "Auto-assist is on. The planned calls fly themselves. Tap to take over."
                    : "Auto-assist is off. Tap to let the planned calls fly themselves.";
            }
            PlaceAssist(prompt);
            if (promptRow != null) promptRow.style.display = prompt ? DisplayStyle.Flex : DisplayStyle.None;
            if (assistRow != null) assistRow.style.display = prompt ? DisplayStyle.None : DisplayStyle.Flex;
            bool showAlternate = prompt && prompts[0].ShowAlternate;
            bool showNudgeRow = director.Engineer || showAlternate;
            if (nudgeRow != null) nudgeRow.style.display = showNudgeRow ? DisplayStyle.Flex : DisplayStyle.None;
            if (nudgeDown != null) nudgeDown.style.display = director.Engineer ? DisplayStyle.Flex : DisplayStyle.None;
            if (nudgeUp != null) nudgeUp.style.display = director.Engineer ? DisplayStyle.Flex : DisplayStyle.None;
            nudgeRowOpen = showNudgeRow;
            if (nudgeDown != null) nudgeDown.text = "Nudge −1°";
            GuideBinder.Prompts(flight);
            if (nudgeUp != null) nudgeUp.text = "Nudge +1°";
            if (caption != null)
                caption.text = "Perfect window = green. Auto-assist does this for you in Simple. Cyan is the plan. Amber is the nudged path.";
            if (bonus != null) bonus.text = BonusLine(score);
            if (extra != null) extra.text = ExtraLine(director);
            if (info != null)
            {
                info.tooltip = infoOpen ? "Hide the flight notes." : "Flight notes. Timing, the path colours, and the extra readouts.";
                info.style.backgroundColor = infoOpen
                    ? new Color(1f, 0.69f, 0f, 0.95f)
                    : new Color(0.1f, 0.14f, 0.2f, 0.72f);
                info.style.color = infoOpen ? new Color(0.08f, 0.08f, 0.1f) : Color.white;
            }
            PaintHealth(score);
            StyleStages();
            PlaceDockSpacer();
            PlaceInfoSheet();
            // A call opening or closing changes the landscape dock height. Re-pin on the change so the taller
            // prompt dock never sits over Follow, the camera button or Pause (1280x800 live build).
            if (widePinned && UseWideNow() && FlightHudLayout.WideMode(shown != default, infoOpen) != wideMode)
                LayoutWide();
        }

        void PlaceAssist(bool prompt)
        {
            if (assist == null) return;
            var host = prompt ? promptRow : assistRow;
            if (host != null && assist.parent != host)
            {
                assist.RemoveFromHierarchy();
                host.Add(assist);
            }
            assist.style.flexGrow = 1;
            assist.style.marginTop = 0;
            assist.style.marginRight = prompt ? Px(FlightHudLayout.GapPt) : 0;
        }

        void PlaceDockSpacer()
        {
            if (dockSpacer == null || hud == null) return;
            // Landscape pins the dock; a flow spacer there would push the rows out of their seats.
            if (widePinned) { dockSpacer.style.height = 0; return; }
            bool prompt = shown != default;
            // Notes float above the dock. The spacer only reserves the buttons.
            float pt = FlightHudLayout.DockHeight(false, prompt);
            if (!nudgeRowOpen) pt -= FlightHudLayout.ButtonPt + FlightHudLayout.GapPt;
            dockSpacer.style.height = Px(pt);
        }

        void DressClock(float boxW, float boxH)
        {
            var clock = hud != null ? hud.Q<Label>("h-t") : null;
            if (clock == null) return;
            FlightDusk.Clock(clock, upp, FlightHudLayout.ClockFontPt(boxW, boxH));
        }

        void BuildHealth()
        {
            health = new VisualElement { name = "ascent-health", pickingMode = PickingMode.Ignore };
            health.style.flexDirection = FlexDirection.Column;
            health.style.flexShrink = 0;
            health.style.overflow = Overflow.Visible;
            health.style.marginBottom = Px(FlightHudLayout.GapPt);
            var row = new VisualElement { name = "ascent-health-row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Stretch;
            row.style.flexShrink = 0;
            row.style.height = Px(FlightHudLayout.HealthPt);
            healthButtons = new Button[VehicleHealth.Count];
            var calm = VehicleHealth.Read(new VehicleHealthInput { Fuel01 = 1, QLimitPa = 1, GLimit = 1 });
            for (int i = 0; i < VehicleHealth.Count; i++)
            {
                int index = i;
                var b = new Button(() => OnHealth(index)) { name = UiIds.FlightHealth(i), text = calm[i].Mark };
                b.tooltip = calm[i].Name + ". " + calm[i].Detail;
                b.style.flexGrow = 1;
                b.style.flexShrink = 1;
                b.style.minWidth = 0;
                b.style.marginRight = i < VehicleHealth.Count - 1 ? Px(FlightHudLayout.GapPt) : 0;
                b.style.marginLeft = 0;
                b.style.marginTop = 0;
                b.style.marginBottom = 0;
                b.style.paddingLeft = 2;
                b.style.paddingRight = 2;
                b.style.paddingTop = 0;
                b.style.paddingBottom = 0;
                b.style.unityTextAlign = TextAnchor.MiddleCenter;
                b.style.whiteSpace = WhiteSpace.NoWrap;
                b.style.overflow = Overflow.Hidden;
                b.style.borderTopWidth = 0;
                b.style.borderRightWidth = 0;
                b.style.borderLeftWidth = 0;
                b.style.borderBottomWidth = 2;
                b.style.backgroundColor = FlightDusk.Ink;
                b.style.color = FlightDusk.Tone(calm[i].Tone);
                b.style.borderBottomColor = FlightDusk.Tone(calm[i].Tone);
                FlightDusk.Face(b, FlightDusk.BodyFace());
                healthButtons[i] = b;
                row.Add(b);
            }
            healthDetail = new Label { name = "ascent-health-detail", pickingMode = PickingMode.Ignore };
            healthDetail.style.position = Position.Absolute;
            healthDetail.style.left = 0;
            healthDetail.style.right = 0;
            healthDetail.style.top = Px(FlightHudLayout.HealthPt);
            healthDetail.style.display = DisplayStyle.None;
            healthDetail.style.whiteSpace = WhiteSpace.Normal;
            health.Add(row);
            health.Add(healthDetail);
            root.Add(health);
        }

        void PaintHealth(AscentFlightScore score)
        {
            if (healthButtons == null || flight == null || flight.Decisions == null) return;
            var director = flight.Decisions;
            var vehicle = director.Vehicle;
            double fuel = director.LiveProp;
            if (double.IsNaN(fuel) && flight.Sim != null) fuel = flight.Sim.PropellantFraction;
            string kind = "";
            if (flight.Lost && flight.Result != null && flight.Result.failure != null)
                kind = flight.Result.failure.kind ?? "";
            double alt = director.LiveAlt > 0 ? director.LiveAlt : flight.Altitude;
            bool fairingOff = vehicle != null && vehicle.HasFairing && alt >= vehicle.JettisonAltM;
            bool engineOut = false;
            var prompts = director.OpenPrompts();
            if (prompts != null)
                for (int i = 0; i < prompts.Length; i++)
                    if (prompts[i].Call == AscentCall.EngineOut) engineOut = true;
            var parts = VehicleHealth.Read(new VehicleHealthInput
            {
                QPa = director.LiveQ > 0 ? director.LiveQ : flight.DynPressure,
                QLimitPa = vehicle != null ? vehicle.QLimPa : 0,
                Fuel01 = fuel,
                GLoad = director.LiveG,
                GLimit = vehicle != null ? vehicle.GLimit : 0,
                Thrust01 = flight.ThrustFraction,
                ExpectThrust = flight.Sim != null && flight.Sim.Thrusting,
                EngineOut = engineOut,
                Lost = flight.Lost,
                LossKind = kind,
                HasFairing = vehicle != null && vehicle.HasFairing,
                FairingOff = fairingOff,
                PayloadHot = score != null && score.PayloadDamaged,
            });
            for (int i = 0; i < healthButtons.Length && i < parts.Length; i++)
            {
                var b = healthButtons[i];
                if (b == null) continue;
                b.text = parts[i].Mark;
                b.tooltip = parts[i].Name + ". " + parts[i].Detail;
                var tone = FlightDusk.Tone(parts[i].Tone);
                b.style.color = tone;
                b.style.borderBottomColor = tone;
                b.style.backgroundColor = healthOpen == i ? FlightDusk.AlarmInk : FlightDusk.Ink;
            }
            if (healthDetail == null) return;
            if (healthOpen >= 0 && healthOpen < parts.Length)
            {
                healthDetail.text = parts[healthOpen].Name + ". " + parts[healthOpen].Detail;
                healthDetail.style.display = DisplayStyle.Flex;
                FlightDusk.Sentence(healthDetail, upp);
                healthDetail.style.backgroundColor = FlightDusk.Ink;
                healthDetail.style.paddingLeft = 6f * upp;
                healthDetail.style.paddingRight = 6f * upp;
                healthDetail.style.paddingTop = 2f * upp;
                healthDetail.style.paddingBottom = 2f * upp;
            }
            else healthDetail.style.display = DisplayStyle.None;
        }

        void OnHealth(int index)
        {
            healthOpen = healthOpen == index ? -1 : index;
            if (flight != null) Present(flight);
            else if (healthDetail != null)
                healthDetail.style.display = healthOpen >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Flight notes sit in a scroll view above the button dock, not inside it.
        /// The dock's overflow clips anything drawn above its own box.
        /// </summary>
        void PlaceInfoSheet()
        {
            if (help == null || hud == null) return;
            if (!infoOpen)
            {
                help.style.display = DisplayStyle.None;
                return;
            }
            if (help.parent != hud) hud.Add(help);
            help.style.display = DisplayStyle.Flex;
            help.style.position = Position.Absolute;
            float padL = hud.resolvedStyle.paddingLeft;
            float padR = hud.resolvedStyle.paddingRight;
            if (float.IsNaN(padL)) padL = 0;
            if (float.IsNaN(padR)) padR = 0;
            help.style.left = padL;
            help.style.right = padR;
            help.style.top = StyleKeyword.Auto;
            help.style.width = StyleKeyword.Auto;
            bool prompt = shown != default;
            float dock = actions != null ? actions.resolvedStyle.height : float.NaN;
            if (float.IsNaN(dock) || dock < 8f)
            {
                float pt = FlightHudLayout.DockHeight(false, prompt);
                if (!nudgeRowOpen) pt -= FlightHudLayout.ButtonPt + FlightHudLayout.GapPt;
                if (pt < FlightHudLayout.ButtonPt) pt = FlightHudLayout.ButtonPt;
                dock = Px(pt);
            }
            help.style.bottom = dock + Px(FlightHudLayout.GapPt);
            float panelH = hud.resolvedStyle.height;
            if (float.IsNaN(panelH) || panelH < 8f) panelH = 700f * upp;
            float sheet = FlightHudLayout.InfoViewPt(panelH / upp, 0f, prompt);
            help.style.height = Px(sheet);
            help.style.maxHeight = Px(sheet);
            help.style.minHeight = Px(48f);
            FlightDusk.Frame(help, upp, false);
            FlightDusk.Sentence(detail, upp);
            FlightDusk.Sentence(caption, upp);
            FlightDusk.Sentence(bonus, upp);
            FlightDusk.Sentence(extra, upp);
            help.BringToFront();
        }

        string ExtraLine(AscentDirector director)
        {
            var inv = CultureInfo.InvariantCulture;
            string line = director.FlightCode
                + "  ·  MACH " + flight.Mach.ToString("0.00", inv)
                + "  ·  THR " + (flight.ThrustFraction * 100.0).ToString("0", inv) + "%"
                + "  ·  DR " + (flight.Downrange / 1000.0).ToString("0.0", inv) + " km";
            var pulse = hud != null ? hud.Q<Label>("h-pulse") : null;
            var callout = hud != null ? hud.Q<Label>("h-callout") : null;
            if (pulse != null && !string.IsNullOrEmpty(pulse.text)) line = pulse.text + "\n" + line;
            if (callout != null && !string.IsNullOrEmpty(callout.text) && callout.text != "Tap an event.")
                line = callout.text + "\n" + line;
            if (infoOpen && shown != default && quote != null && !string.IsNullOrEmpty(quote.text))
                line = quote.text + "\n" + line;
            return line;
        }

        static string BonusLine(AscentFlightScore score)
        {
            if (score == null) return "";
            string bits = "";
            if (score.AltitudeHit) bits += "Altitude target  ·  ";
            if (score.SpeedHit) bits += "Speed target  ·  ";
            if (score.FuelHit) bits += "Fuel-efficient ascent  ·  ";
            if (score.CleanStaging) bits += "Clean staging  ·  ";
            if (score.PeakGHit) bits += "Gentle ride  ·  ";
            if (score.PayloadDamaged) bits += "Payload heat is past the limit  ·  ";
            if (bits.Length == 0) return score.Decided > 0 ? "Medal timing: " + score.Summary : "";
            return bits + score.Summary;
        }

        void SetCell(int i, string name, string value, Color color)
        {
            if (cellKey[i] != null) cellKey[i].text = name;
            if (cellVal[i] == null) return;
            cellVal[i].text = value;
            cellVal[i].style.color = color;
        }

        void BuildStrip()
        {
            var strip = new VisualElement { name = "ascent-telemetry", pickingMode = PickingMode.Ignore };
            strip.style.flexDirection = FlexDirection.Row;
            strip.style.alignItems = Align.Center;
            strip.style.backgroundColor = new Color(0.03f, 0.06f, 0.11f, 0.40f);
            strip.style.flexShrink = 0;
            for (int i = 0; i < CellCount; i++)
            {
                var cell = new VisualElement { name = "ascent-cell-" + i, pickingMode = PickingMode.Ignore };
                cell.style.flexGrow = 1;
                cell.style.flexShrink = 1;
                cell.style.alignItems = Align.Center;
                cell.style.overflow = Overflow.Hidden;
                if (i < CellCount - 1) cell.style.marginRight = Px(FlightHudLayout.CellGapPt);
                var key = new Label { name = "ascent-cell-" + i + "-k", pickingMode = PickingMode.Ignore };
                key.style.unityTextAlign = TextAnchor.MiddleCenter;
                key.style.whiteSpace = WhiteSpace.NoWrap;
                key.style.color = new Color(0.65f, 0.75f, 0.82f);
                key.style.unityFontStyleAndWeight = FontStyle.Bold;
                var val = new Label { name = "ascent-cell-" + i + "-v", pickingMode = PickingMode.Ignore };
                val.style.unityTextAlign = TextAnchor.MiddleCenter;
                val.style.whiteSpace = WhiteSpace.NoWrap;
                val.style.unityFontStyleAndWeight = FontStyle.Bold;
                cell.Add(key);
                cell.Add(val);
                cellKey[i] = key;
                cellVal[i] = val;
                strip.Add(cell);
            }
            root.Add(strip);
        }

        void BuildCards()
        {
            cards = new VisualElement { name = "ascent-cards", pickingMode = PickingMode.Ignore };
            cards.style.flexDirection = FlexDirection.Row;
            cards.style.flexShrink = 0;
            plot = new TrajectoryRailView { name = "ascent-plot", pickingMode = PickingMode.Ignore };
            plot.style.position = Position.Absolute;
            plot.style.backgroundColor = new Color(0.04f, 0.07f, 0.12f, FlightHudLayout.TrajectoryAlpha);
            plot.style.overflow = Overflow.Visible;
            gap = new VisualElement { name = "ascent-gap", pickingMode = PickingMode.Ignore };
            gap.style.flexGrow = 1;
            AdoptStages();
            cards.Add(gap);
            cards.Add(stages);
            root.Add(cards);
            if (hud != null) hud.Insert(0, plot);
        }

        void FeedTrajectory(AscentDirector director, double alt, double speed, double q)
        {
            var vehicle = director != null ? director.Vehicle : null;
            if (vehicle != null)
            {
                trajectory.HasFairing = vehicle.HasFairing;
                trajectory.Stages = vehicle.Stages > 0 ? vehicle.Stages : 1;
                if (vehicle.JettisonAltM > 0) trajectory.FairingAltM = vehicle.JettisonAltM;
            }
            trajectory.Recovery = RecoveryLog.Active || (RecoveryLog.Current != null && RecoveryLog.Current.Attempted);
            if (flight.Sim != null && flight.Sim.Events != null)
            {
                var evs = flight.Sim.Events;
                if (fedEvents > evs.Count) fedEvents = 0;
                while (fedEvents < evs.Count)
                {
                    var ev = evs[fedEvents++];
                    if (ev == null) continue;
                    trajectory.Seen(new TrajectoryEvent
                    {
                        T = ev.t,
                        Type = ev.type,
                        Stage = ev.stage ?? 0,
                        AltM = ev.alt ?? alt,
                        SpeedMps = ev.v ?? speed,
                    });
                }
            }
            trajectory.Sample(flight.SimTime, alt, speed, q);
            var rec = RecoveryLog.Current;
            if (rec != null && rec.Marks != null)
            {
                for (int i = 0; i < rec.Marks.Length; i++)
                {
                    var mark = rec.Marks[i];
                    trajectory.RecoveryPhase(RecoveryWord(mark.Phase), mark.T, mark.Alt, mark.Speed);
                }
            }
        }

        static string RecoveryWord(RecoveryPhase phase)
        {
            switch (phase)
            {
                case RecoveryPhase.Boostback: return "boostback";
                case RecoveryPhase.Entry: return "entry";
                case RecoveryPhase.Landing:
                case RecoveryPhase.Down: return "landing";
                default: return "";
            }
        }

        void AdoptStages()
        {
            var cast = hud.Q("webcast");
            if (cast != null && cast.parent != null)
            {
                webcastHome = cast.parent;
                webcastHomeIndex = webcastHome.IndexOf(cast);
                cast.RemoveFromHierarchy();
                stages = cast;
            }
            else stages = new VisualElement { name = "ascent-stages" };
            stages.pickingMode = PickingMode.Position;
            HideStageSentences();
        }

        void RestoreStages()
        {
            if (stages != null && webcastHome != null && stages.name == "webcast")
            {
                if (stages.parent != null) stages.RemoveFromHierarchy();
                ClearCardStyle(stages);
                int i = webcastHomeIndex;
                if (i < 0 || i > webcastHome.childCount) webcastHome.Add(stages);
                else webcastHome.Insert(i, stages);
                var pulse = stages.Q("h-pulse");
                if (pulse != null) pulse.style.display = StyleKeyword.Null;
                var callout = stages.Q("h-callout");
                if (callout != null) callout.style.display = StyleKeyword.Null;
            }
            webcastHome = null;
            webcastHomeIndex = -1;
            stages = null;
        }

        static void ClearCardStyle(VisualElement e)
        {
            e.style.width = StyleKeyword.Auto;
            e.style.height = StyleKeyword.Auto;
            e.style.flexGrow = StyleKeyword.Null;
            e.style.flexShrink = StyleKeyword.Null;
            e.style.backgroundColor = StyleKeyword.Null;
            e.style.overflow = StyleKeyword.Null;
            e.style.marginTop = StyleKeyword.Null;
            e.style.marginBottom = StyleKeyword.Null;
            e.style.marginLeft = StyleKeyword.Null;
            e.style.marginRight = StyleKeyword.Null;
            e.style.paddingTop = StyleKeyword.Null;
            e.style.paddingBottom = StyleKeyword.Null;
            e.style.paddingLeft = StyleKeyword.Null;
            e.style.paddingRight = StyleKeyword.Null;
            e.style.borderTopLeftRadius = StyleKeyword.Null;
            e.style.borderTopRightRadius = StyleKeyword.Null;
            e.style.borderBottomLeftRadius = StyleKeyword.Null;
            e.style.borderBottomRightRadius = StyleKeyword.Null;
        }

        void StyleStages()
        {
            if (stages == null) return;
            stages.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            stages.style.width = Length.Percent(FlightHudLayout.CardFraction * 100f);
            stages.style.height = Px(FlightHudLayout.PlotPt);
            stages.style.flexGrow = 0;
            stages.style.flexShrink = 1;
            stages.style.minWidth = 0;
            stages.style.overflow = Overflow.Hidden;
            stages.style.marginTop = 0;
            stages.style.marginBottom = 0;
            stages.style.marginLeft = 0;
            stages.style.paddingTop = Px(4f);
            stages.style.paddingBottom = Px(4f);
            stages.style.paddingLeft = Px(4f);
            stages.style.paddingRight = Px(4f);
            float r = Px(12f);
            stages.style.borderTopLeftRadius = r;
            stages.style.borderTopRightRadius = r;
            stages.style.borderBottomLeftRadius = r;
            stages.style.borderBottomRightRadius = r;
            var verdict = stages.Q<Label>("h-verdict");
            if (verdict != null)
            {
                bool alarm = verdict.text != null && verdict.text.IndexOf("lost", StringComparison.OrdinalIgnoreCase) >= 0;
                FlightDusk.Banner(verdict, upp, alarm);
                verdict.style.display = string.IsNullOrEmpty(verdict.text) ? DisplayStyle.None : DisplayStyle.Flex;
            }
            var seq = stages.Q("h-seq");
            if (seq != null)
            {
                seq.Clear();
                seq.style.display = DisplayStyle.None;
            }
            stages.Query<Button>().ForEach(b =>
            {
                if (b.name == "skip-count")
                {
                    b.style.fontSize = Px(11f);
                    b.style.whiteSpace = WhiteSpace.Normal;
                    b.style.overflow = Overflow.Hidden;
                    b.style.minHeight = Px(22f);
                    return;
                }
                b.style.display = DisplayStyle.None;
            });
            HideStageSentences();
        }

        void HideStageSentences()
        {
            if (stages == null) return;
            var pulse = stages.Q("h-pulse");
            if (pulse != null) pulse.style.display = DisplayStyle.None;
            var callout = stages.Q("h-callout");
            if (callout != null) callout.style.display = DisplayStyle.None;
        }

        void BuildActions()
        {
            actions = new VisualElement { name = "ascent-actions" };
            actions.style.position = Position.Absolute;
            actions.style.left = 0;
            actions.style.right = 0;
            actions.style.bottom = 0;
            actions.style.backgroundColor = new Color(0.03f, 0.05f, 0.09f, FlightHudLayout.DockAlpha);
            var medalRow = new VisualElement { name = "ascent-medal-row" };
            medalRow.style.flexDirection = FlexDirection.Row;
            medalRow.style.alignItems = Align.Center;
            medalRow.style.overflow = Overflow.Hidden;
            medalRow.style.flexShrink = 0;
            medal = new Label { name = "ascent-medal", pickingMode = PickingMode.Ignore };
            medal.style.color = new Color(0.85f, 0.95f, 0.9f);
            medal.style.whiteSpace = WhiteSpace.Normal;
            medal.style.overflow = Overflow.Hidden;
            medal.style.flexGrow = 1;
            medal.style.flexShrink = 1;
            medal.style.unityFontStyleAndWeight = FontStyle.Bold;
            info = Action(UiIds.FlightInfo, () => OnInfo());
            info.text = "i";
            info.tooltip = "Flight notes. Timing, the path colours, and the extra readouts.";
            medalRow.Add(medal);
            medalRow.Add(info);
            help = new ScrollView(ScrollViewMode.Vertical) { name = "ascent-help" };
            help.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            help.verticalScrollerVisibility = ScrollerVisibility.Auto;
            help.AddToClassList("flight-help");
            help.style.display = DisplayStyle.None;
            detail = Note("ascent-detail", new Color(0.78f, 0.84f, 0.9f));
            caption = Note("ascent-caption", new Color(0.65f, 0.75f, 0.82f));
            bonus = Note("ascent-bonus", new Color(0.65f, 0.78f, 0.72f));
            extra = Note("ascent-extra", new Color(0.7f, 0.78f, 0.86f));
            help.Add(detail);
            help.Add(caption);
            help.Add(bonus);
            help.Add(extra);
            quote = new Label { name = "ascent-quote", pickingMode = PickingMode.Ignore };
            quote.style.color = Color.white;
            quote.style.whiteSpace = WhiteSpace.Normal;
            quote.style.overflow = Overflow.Hidden;
            quote.style.unityFontStyleAndWeight = FontStyle.Bold;
            bar = new VisualElement { name = "ascent-timer", pickingMode = PickingMode.Ignore };
            bar.style.backgroundColor = new Color(0.15f, 0.2f, 0.28f);
            bar.style.borderTopLeftRadius = 3;
            bar.style.borderTopRightRadius = 3;
            bar.style.borderBottomLeftRadius = 3;
            bar.style.borderBottomRightRadius = 3;
            green = new VisualElement { name = "ascent-green", pickingMode = PickingMode.Ignore };
            green.style.position = Position.Absolute;
            green.style.top = 0;
            green.style.bottom = 0;
            green.style.backgroundColor = new Color(0.2f, 0.85f, 0.55f);
            marker = new VisualElement { name = "ascent-marker", pickingMode = PickingMode.Ignore };
            marker.style.position = Position.Absolute;
            marker.style.top = -2;
            marker.style.bottom = -2;
            marker.style.width = 3;
            marker.style.backgroundColor = Color.white;
            bar.Add(green);
            bar.Add(marker);
            primary = Action(UiIds.FlightPrimary, () => Press(true));
            primary.style.backgroundColor = new Color(0.23f, 0.83f, 0.96f);
            primary.style.color = new Color(0.03f, 0.08f, 0.12f);
            alternate = Action(UiIds.FlightAlternate, () => Press(false));
            alternate.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.2f);
            alternate.style.color = new Color(0.95f, 0.35f, 0.42f);
            alternate.style.borderTopWidth = 2;
            alternate.style.borderRightWidth = 2;
            alternate.style.borderBottomWidth = 2;
            alternate.style.borderLeftWidth = 2;
            alternate.style.borderTopColor = new Color(0.89f, 0.23f, 0.29f);
            alternate.style.borderRightColor = new Color(0.89f, 0.23f, 0.29f);
            alternate.style.borderBottomColor = new Color(0.89f, 0.23f, 0.29f);
            alternate.style.borderLeftColor = new Color(0.89f, 0.23f, 0.29f);
            skip = Action(UiIds.FlightSkip, OnSkip);
            skip.text = "Skip";
            skip.tooltip = "Skip. The autopilot takes the planned action. This is not a fail.";
            assist = Action(UiIds.FlightAssist, OnAssist);
            assist.text = "Auto-assist";
            promptRow = new VisualElement { name = "ascent-prompt-row" };
            promptRow.style.flexDirection = FlexDirection.Row;
            primary.style.flexGrow = 1;
            skip.style.flexGrow = 1;
            primary.style.marginRight = Px(FlightHudLayout.GapPt);
            skip.style.marginRight = Px(FlightHudLayout.GapPt);
            promptRow.Add(primary);
            promptRow.Add(skip);
            assistRow = new VisualElement { name = "ascent-assist-row" };
            assistRow.Add(assist);
            nudgeRow = new VisualElement { name = "ascent-nudge" };
            nudgeRow.style.flexDirection = FlexDirection.Row;
            nudgeDown = Action(UiIds.FlightNudgeDown, () => Nudge(-AscentDecisions.NudgeStepDeg));
            nudgeUp = Action(UiIds.FlightNudgeUp, () => Nudge(AscentDecisions.NudgeStepDeg));
            nudgeDown.style.flexGrow = 1;
            nudgeUp.style.flexGrow = 1;
            alternate.style.flexGrow = 1;
            alternate.style.marginRight = Px(FlightHudLayout.GapPt);
            nudgeDown.style.marginRight = Px(FlightHudLayout.GapPt);
            nudgeDown.tooltip = "Pitch the planned kick down one degree.";
            nudgeUp.tooltip = "Pitch the planned kick up one degree.";
            nudgeRow.Add(alternate);
            nudgeRow.Add(nudgeDown);
            nudgeRow.Add(nudgeUp);
            actions.Add(medalRow);
            actions.Add(help);
            actions.Add(quote);
            actions.Add(bar);
            actions.Add(promptRow);
            actions.Add(assistRow);
            actions.Add(nudgeRow);
        }

        static Label Note(string name, Color color)
        {
            var label = new Label { name = name, pickingMode = PickingMode.Ignore };
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 2;
            return label;
        }

        void Press(bool accept)
        {
            if (shown == default || flight == null || flight.Decisions == null) return;
            flight.Decisions.Choose(shown, accept);
            Present(flight);
        }

        void OnSkip()
        {
            if (shown == default || flight == null || flight.Decisions == null) return;
            flight.Decisions.Skip(shown);
            Present(flight);
        }

        void OnAssist()
        {
            if (flight == null || flight.Decisions == null) return;
            flight.Decisions.AutoAssist = !flight.Decisions.AutoAssist;
            Present(flight);
        }

        void OnInfo()
        {
            infoOpen = !infoOpen;
            if (hud != null) hud.EnableInClassList("hud-info", infoOpen);
            if (flight != null) Present(flight);
            else
            {
                PlaceDockSpacer();
                PlaceInfoSheet();
                if (hud != null) hud.Q(UiIds.ScreenPause)?.BringToFront();
            }
        }

        void Nudge(double delta)
        {
            if (flight == null || flight.Decisions == null) return;
            flight.Decisions.NudgePitch(delta);
            Present(flight);
        }

        void StyleButton(Button b)
        {
            if (b == null) return;
            float h = Px(FlightHudLayout.ButtonPt);
            b.style.minHeight = h;
            b.style.height = h;
            b.style.fontSize = Px(12f);
            b.style.marginTop = 0;
            b.style.marginRight = Px(FlightHudLayout.GapPt);
            b.style.paddingLeft = Px(4f);
            b.style.paddingRight = Px(4f);
            b.style.whiteSpace = WhiteSpace.Normal;
            b.style.overflow = Overflow.Hidden;
            float r = Px(10f);
            b.style.borderTopLeftRadius = r;
            b.style.borderTopRightRadius = r;
            b.style.borderBottomLeftRadius = r;
            b.style.borderBottomRightRadius = r;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.flexShrink = 1;
        }

        static Button Action(string name, Action click)
        {
            var b = new Button(click) { name = name };
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.borderTopWidth = 0;
            b.style.borderRightWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            b.style.color = Color.white;
            b.style.backgroundColor = new Color(0.1f, 0.14f, 0.2f, 0.95f);
            return b;
        }

        float Px(float pt) => AscentLayout.Px(pt, upp);

        sealed class TrajectoryRailView : VisualElement
        {
            TrajectoryPlan plan = new TrajectoryPlan();
            float upp = 1f;
            readonly VisualElement labels;

            public TrajectoryRailView()
            {
                pickingMode = PickingMode.Ignore;
                generateVisualContent += OnPaint;
                RegisterCallback<GeometryChangedEvent>(_ => RefreshLabels());
                labels = new VisualElement { name = "trajectory-labels", pickingMode = PickingMode.Ignore };
                labels.style.position = Position.Absolute;
                labels.style.left = 0;
                labels.style.top = 0;
                labels.style.right = 0;
                labels.style.bottom = 0;
                Add(labels);
            }

            public void Apply(TrajectoryPlan next, float unitsPerPoint)
            {
                plan = next ?? new TrajectoryPlan();
                upp = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
                RefreshLabels();
                MarkDirtyRepaint();
            }

            void RefreshLabels()
            {
                var box = contentRect;
                var marks = plan != null ? plan.Marks : null;
                if (marks == null || box.width < 4f || box.height < 4f)
                {
                    labels.style.display = DisplayStyle.None;
                    return;
                }
                labels.style.display = DisplayStyle.Flex;
                var rail = new HudBox { X = 0, Y = 0, W = box.width / upp, H = box.height / upp };
                var tags = TrajectoryWatch.Tags(rail, marks);
                while (labels.childCount > tags.Length) labels.RemoveAt(labels.childCount - 1);
                while (labels.childCount < tags.Length)
                {
                    var born = new Label { name = "trajectory-mark", pickingMode = PickingMode.Ignore };
                    born.style.position = Position.Absolute;
                    born.style.whiteSpace = WhiteSpace.Normal;
                    born.style.overflow = Overflow.Hidden;
                    labels.Add(born);
                }
                for (int i = 0; i < tags.Length; i++)
                {
                    var label = (Label)labels[i];
                    var mark = i < marks.Length ? marks[i] : default;
                    label.text = string.Join("\n", tags[i].Lines);
                    label.tooltip = label.text;
                    label.style.fontSize = tags[i].FontPt * upp;
                    label.style.left = tags[i].Box.X * upp;
                    label.style.top = tags[i].Box.Y * upp;
                    label.style.width = tags[i].Box.W * upp;
                    label.style.height = tags[i].Box.H * upp;
                    label.style.unityFontStyleAndWeight = mark.Upcoming ? FontStyle.Bold : FontStyle.Normal;
                    label.style.color = mark.Passed
                        ? new Color(0.45f, 0.86f, 0.62f)
                        : mark.Upcoming
                            ? new Color(1f, 0.82f, 0.35f)
                            : new Color(0.78f, 0.84f, 0.9f, 0.82f);
                }
            }

            void OnPaint(MeshGenerationContext ctx)
            {
                var box = contentRect;
                if (box.width < 8f || box.height < 8f || plan == null) return;
                float x = 8f;
                float top = 8f;
                float bottom = box.height - 8f;
                float span = bottom - top;
                if (span < 12f) return;
                var paint = ctx.painter2D;
                paint.strokeColor = new Color(0.02f, 0.04f, 0.08f, 0.92f);
                paint.lineWidth = 8f;
                paint.BeginPath();
                paint.MoveTo(new Vector2(x, bottom));
                paint.LineTo(new Vector2(x, top));
                paint.Stroke();
                paint.strokeColor = new Color(0.93f, 0.97f, 1f, 0.94f);
                paint.lineWidth = 3.5f;
                paint.BeginPath();
                paint.MoveTo(new Vector2(x, bottom));
                paint.LineTo(new Vector2(x, top));
                paint.Stroke();

                float progress = plan.Progress;
                if (progress < 0f) progress = 0f;
                else if (progress > 1f) progress = 1f;
                float yMark = bottom - progress * span;
                paint.strokeColor = new Color(0.35f, 0.90f, 0.72f, 0.95f);
                paint.lineWidth = 3f;
                paint.BeginPath();
                paint.MoveTo(new Vector2(x, bottom));
                paint.LineTo(new Vector2(x, yMark));
                paint.Stroke();

                var marks = plan.Marks;
                if (marks != null)
                {
                    for (int i = 0; i < marks.Length; i++)
                    {
                        float y = bottom - marks[i].Along * span;
                        float tick = marks[i].Upcoming ? 16f : 10f;
                        paint.lineWidth = marks[i].Upcoming ? 3f : 2f;
                        paint.strokeColor = marks[i].Passed
                            ? new Color(0.45f, 0.86f, 0.62f, 0.95f)
                            : marks[i].Upcoming
                                ? new Color(1f, 0.82f, 0.35f, 1f)
                                : new Color(1f, 1f, 1f, 0.38f);
                        paint.BeginPath();
                        paint.MoveTo(new Vector2(x - 1f, y));
                        paint.LineTo(new Vector2(x + tick, y));
                        paint.Stroke();
                    }
                }

                paint.fillColor = Color.white;
                paint.BeginPath();
                paint.MoveTo(new Vector2(x, yMark - 6f));
                paint.LineTo(new Vector2(x + 6f, yMark));
                paint.LineTo(new Vector2(x, yMark + 6f));
                paint.LineTo(new Vector2(x - 6f, yMark));
                paint.ClosePath();
                paint.Fill();
            }
        }
    }
}
