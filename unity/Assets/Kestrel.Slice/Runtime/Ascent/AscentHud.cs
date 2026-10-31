// Portrait ascent HUD. Clock and stage chip stay in the flight header. Under them: one telemetry
// strip, then the stages panel on the right. The trajectory is a transparent line up the left
// edge: milestones, a fill, and a marker that move with the flight. Auto-assist, nudges, and the
// medal line share a bottom dock. Long notes sit behind Info.
// Pause, pinch, orbit, and Follow are not touched. Guide copy is not touched.
using System;
using System.Collections.Generic;
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
        // The upper stage's last strip reading, kept while the booster has the strip so it comes straight back.
        readonly string[] ascentKey = new string[CellCount];
        readonly string[] ascentVal = new string[CellCount];
        readonly Color[] ascentInk = new Color[CellCount];
        BoosterHudReadout? booster;
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
            if (healthDetail != null) root.Add(healthDetail);
            hud.RegisterCallback<PointerDownEvent>(OnHudPointerDown);
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
            WatchTools();
            WatchStack();
            Layout(upp);
        }

        /// <summary>
        /// Share of the screen height (0..1, from the top) the HUD's top block covers over the middle of the frame:
        /// the bottom of the telemetry strip (ALT / SPEED / Q / G), measured as laid out. 0 while the HUD is closed or
        /// not laid out. The chase camera keeps the stack's nose under it (ChaseCamera.KeepBelow): the strip is sized in
        /// points and scales with the panel width, so on a 4:3 iPad it reaches 18% down the frame against 12% on a phone.
        /// </summary>
        public float TopCover()
        {
            var strip = hud != null ? hud.Q("ascent-telemetry") : null;
            if (strip == null || strip.panel == null || strip.resolvedStyle.display == DisplayStyle.None) return 0f;
            var top = strip.panel.visualTree.worldBound;
            var sb = strip.worldBound;
            if (!(top.height > 1f) || !(sb.height > 1f) || float.IsNaN(sb.yMax)) return 0f;
            return Mathf.Clamp01((sb.yMax - top.yMin) / top.height);
        }

        /// <summary>
        /// Share of the panel height (0..1) from the bottom edge up to the top of the tools row (Follow, Cam, Pause) as laid
        /// out: the row and everything under it (the flight message, BACK TO DESIGN, the medal card) cover the scene there.
        /// 0 while the HUD is not laid out.
        /// </summary>
        public float BottomCover()
        {
            var tools = hud != null ? hud.Q("flight-tools") : null;
            if (tools == null || tools.panel == null || tools.resolvedStyle.display == DisplayStyle.None) return 0f;
            var top = tools.panel.visualTree.worldBound;
            var tb = tools.worldBound;
            if (!(top.height > 1f) || !(tb.height > 1f) || float.IsNaN(tb.yMin)) return 0f;
            float c = (top.yMax - tb.yMin) / top.height;
            return c > 0f && c < 0.6f ? c : 0f;   // a layout that puts the row up top is not a bottom cover
        }

        public void Close()
        {
            // Hand the tools row (Follow, Cam, Pause) back to the flex flow before the HUD goes.
            ClearWidePins();
            UnwatchTools();
            UnwatchStack();
            hud?.UnregisterCallback<PointerDownEvent>(OnHudPointerDown);
            railFrame = null;
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
            booster = null;
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
            bannerRowPt = RailBannerPt();
            float plotH = bannerRowPt > 0f ? Mathf.Round(Px(bannerRowPt)) : Px(FlightHudLayout.PlotPt);
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
                SizeHealthRow(health.Q(HealthRowName));
                for (int i = 0; healthButtons != null && i < healthButtons.Length; i++)
                {
                    var b = healthButtons[i];
                    if (b == null) continue;
                    float hh = Mathf.Round(Px(FlightHudLayout.HealthPt));
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
            WatchStack();
            FitText();
            // Picker above the HUD (rail pieces and the info sheet are re-added or brought forward above),
            // pause menu above the picker (L3: nothing the HUD adds may draw over the open pause menu).
            FlightChrome.Restack(hud);
        }

        /// <summary>The portrait dock mode LayoutRail last seated the rows for; null before the first rail layout.</summary>
        FlightHudMode? railMode;
        /// <summary>
        /// Portrait: 0 while the stage banner fits the stages card in two lines at 11 pt; otherwise the height (pt) of
        /// the full-width banner row the card becomes (FlightHudLayout.BannerRowPt). Never in landscape.
        /// </summary>
        float bannerRowPt;
        /// <summary>#395: Current banner row height (pt), 0 when the banner fits in the stages card.
        /// The staging callout uses this to sit below the banner instead of overlapping it.</summary>
        public float BannerRowPt => bannerRowPt;
        /// <summary>Whether the dock's last row (Alternate / nudges) was open when LayoutRail last seated the rows.</summary>
        bool railNudges = true;

        /// <summary>The banner row height the current banner text needs at the HUD's content width (0: the card holds it).</summary>
        float RailBannerPt()
        {
            if (hud == null || stages == null || UseWideNow() || !(upp > 0f)) return 0f;
            var verdict = stages.Q<Label>("h-verdict");
            string text = FlightDusk.BannerText(verdict);
            if (string.IsNullOrEmpty(text)) return 0f;
            float w = hud.resolvedStyle.width - hud.resolvedStyle.paddingLeft - hud.resolvedStyle.paddingRight;
            if (float.IsNaN(w) || w < 8f)
            {
                float sw = hud.style.width.keyword == StyleKeyword.Undefined ? hud.style.width.value.value : 0f;
                w = sw;
            }
            if (float.IsNaN(w) || w < 8f) return bannerRowPt;
            return FlightHudLayout.BannerRowPt(text, w / upp);
        }
        /// <summary>The prompt text the dock buttons were last fitted for (refit only when it changes).</summary>
        string fittedFor;

        FlightHudMode LiveMode() => FlightHudLayout.WideMode(shown != default, infoOpen);

        /// <summary>
        /// L1/L2 (1280x800 live build): the strip keys rose above the HUD top, the engine-out quote printed over the
        /// medal line and SHUT DOWN ran under Skip. Every dock row is seated in its own box, the strip labels carry no
        /// default margins, and the text in the strip, the health tabs, the stages banner and the dock buttons is
        /// fitted to its own box.
        /// </summary>
        void FitText()
        {
            FitCells();
            FitQuote();
            var strip = root != null ? root.Q("ascent-telemetry") : null;
            if (strip != null) TextFit.Apply(strip);
            if (health != null) TextFit.Apply(health);
            if (stages != null) TextFit.Apply(stages);
            if (actions != null) TextFit.Apply(actions);
            if (nudgeRow != null && actions != null && nudgeRow.parent != actions) TextFit.Apply(nudgeRow);
        }

        /// <summary>The key and value lines of each strip cell together fit the strip height (L1: "ALT" sat above the top edge).</summary>
        void FitCells()
        {
            var strip = root != null ? root.Q("ascent-telemetry") : null;
            if (strip == null) return;
            float h = strip.style.height.keyword == StyleKeyword.Undefined ? strip.style.height.value.value : 0f;
            if (!(h > 4f)) h = strip.resolvedStyle.height;
            if (float.IsNaN(h) || h < 4f) return;
            float key = Px(FlightHudLayout.LabelFontPt);
            float val = Px(FlightHudLayout.ValueFontPt);
            float need = (key + val) * FlightHudLayout.LineHeight;
            float room = h - Px(2f);
            float k = need > room && need > 0f ? room / need : 1f;
            // Width: the widest reading (FlightHudLayout.ValueSamples, "1234.5 km" class) fits its cell on one line.
            float sw = strip.style.width.keyword == StyleKeyword.Undefined ? strip.style.width.value.value : 0f;
            if (!(sw > 8f)) sw = strip.resolvedStyle.width;
            if (sw > 8f && !float.IsNaN(sw))
            {
                float cellW = (sw - 2f * Px(FlightHudLayout.CellPadPt) - (CellCount - 1) * Px(FlightHudLayout.CellGapPt)) / CellCount;
                float wide = FlightHudLayout.BudgetWidth(FlightHudLayout.WidestValue, val);
                if (wide > cellW - Px(2f) && wide > 0f) k = Math.Min(k, (cellW - Px(2f)) / wide);
            }
            for (int i = 0; i < CellCount; i++)
            {
                if (cellKey[i] != null) cellKey[i].style.fontSize = key * k;
                if (cellVal[i] != null) cellVal[i].style.fontSize = val * k;
            }
        }

        /// <summary>The prompt quote is one line in an 18 pt row: the face steps down to 10 pt, then the line is cut at a word.</summary>
        void FitQuote()
        {
            if (quote == null || string.IsNullOrEmpty(quote.text)) return;
            string full = quote.userData as string ?? quote.text;
            float w = quote.style.width.keyword == StyleKeyword.Undefined ? quote.style.width.value.value : 0f;
            if (!(w > 8f)) w = quote.resolvedStyle.width;
            if (float.IsNaN(w) || w < 8f) return;
            var fit = FlightHudLayout.QuoteLine(full, w / upp, FlightHudLayout.QuoteFontPt, FlightHudLayout.QuoteMinFontPt);
            quote.style.whiteSpace = WhiteSpace.NoWrap;
            // Budgeted exactly (FlightHudLayout.MeasureFactor): no theme margin or padding on the line.
            quote.style.marginLeft = 0; quote.style.marginRight = 0; quote.style.marginTop = 0; quote.style.marginBottom = 0;
            quote.style.paddingLeft = 0; quote.style.paddingRight = 0; quote.style.paddingTop = 0; quote.style.paddingBottom = 0;
            quote.style.fontSize = Px(fit.FontPt);
            quote.text = fit.Text;
            quote.tooltip = full;
        }

        /// <summary>
        /// L3: nothing the HUD adds or re-parents may draw over the pause menu. Cheap per-frame guard: only when the
        /// menu is no longer the last child does it re-run #304's restack (picker, toast, then the pause menu on top).
        /// </summary>
        void KeepPauseOnTop()
        {
            var menu = hud != null ? hud.Q(UiIds.ScreenPause) : null;
            if (menu != null && menu.parent == hud && hud.IndexOf(menu) != hud.childCount - 1) FlightChrome.Restack(hud);
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
            var frame = FlightHudLayout.Place(w / upp, h / upp, 0f, 0f, 0f, 0f, mode, bannerRowPt, nudgeRowOpen);
            railMode = mode;
            railNudges = nudgeRowOpen;
            if (plot.parent != hud) hud.Insert(0, plot);
            // #332 (TestFlight t432): the frame is in the HUD's content box, but absolute offsets start at its padding
            // edge. Pinned without the padding, the rail and the dock sat padTop higher than the flex rows: on a
            // 62 pt notch the dock covered Follow / Cam / Pause (and took their taps) and SECO hid under the strip.
            // CI's 24 px minimum padding hid it. Pins now add the padding, like SeatRailPicker.
            padOrigin = new Vector2(padL, padT);
            plotPinned = Offset(new HudBox
            {
                X = frame.Plot.X * upp,
                Y = frame.Plot.Y * upp,
                W = frame.Plot.W * upp,
                H = frame.Plot.H * upp,
            });
            SeatPlot();
            plot.style.backgroundColor = new Color(0.04f, 0.07f, 0.12f, FlightHudLayout.TrajectoryAlpha);
            plot.Apply(trajectory.At(RecoveryLog.RailClock(flight != null ? flight.SimTime : 0), flight != null ? flight.Altitude : 0, flight != null ? flight.Speed : 0), upp);
            var dock = ScaleBox(frame.Dock);
            Pin(actions, Offset(dock));
            if (actions != null) actions.style.overflow = Overflow.Hidden;
            DressClock(frame.Clock.W, frame.Clock.H);
            SeatMedalRow(ScaleBox(Union(frame.Medal, frame.Info)), dock);
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
            // The camera button stays in the flex tools row; its picker opens above that row as it is really
            // laid out (SeatRailPicker), and again whenever that row moves.
            railFrame = frame;
            SeatRailPicker();
        }

        /// <summary>The HUD's padding (panel px) the portrait pins are offset by: frame boxes are content-box relative.</summary>
        Vector2 padOrigin;
        HudBox Offset(HudBox b) => new HudBox { X = b.X + padOrigin.x, Y = b.Y + padOrigin.y, W = b.W, H = b.H };

        /// <summary>The last portrait frame, for re-seating the picker when the flex tools row moves.</summary>
        FlightHudFrame railFrame;
        VisualElement watchedTools;

        void WatchTools()
        {
            var tools = hud != null ? hud.Q("flight-tools") : null;
            if (tools == watchedTools) return;
            UnwatchTools();
            watchedTools = tools;
            watchedTools?.RegisterCallback<GeometryChangedEvent>(OnToolsMoved);
        }

        void UnwatchTools()
        {
            watchedTools?.UnregisterCallback<GeometryChangedEvent>(OnToolsMoved);
            watchedTools = null;
        }

        void OnToolsMoved(GeometryChangedEvent _)
        {
            SeatPlot();
            if (!widePinned && !UseWideNow()) SeatRailPicker();
        }

        /// <summary>The frame's rail box (panel px, padding-edge relative), before it is fitted to the measured stack.</summary>
        HudBox plotPinned;

        /// <summary>
        /// Pin the rail from the frame (portrait: Place, landscape: PlaceWide), ending one gap above the highest measured
        /// top of every visible dock-stack element under it, in whole pixels (FlightHudLayout.SeatRail, the call the
        /// overlap grid models). CI 335x720 (24 px pads): the frame's rail ended at 496, the flex tools row started at
        /// 495. CI 1280x800 notes open: landscape never refitted, and the rail (bottom 515) ran into the notes (467).
        /// Runs on every layout, on notes open / close, and whenever a stack element moves (WatchStack).
        /// </summary>
        void SeatPlot()
        {
            if (plot == null || hud == null || !plotPinned.Live) return;
            var below = new List<HudBox>();
            var hb = hud.worldBound;
            if (!float.IsNaN(hb.yMin) && !float.IsNaN(hb.xMin))
                foreach (var e in StackElements())
                {
                    if (e == null || e.panel == null || !Visible(e)) continue;
                    var wb = e.worldBound;
                    if (float.IsNaN(wb.yMin) || float.IsNaN(wb.xMin)) continue;
                    below.Add(new HudBox { X = wb.xMin - hb.xMin, Y = wb.yMin - hb.yMin, W = wb.width, H = wb.height });
                }
            var fit = FlightHudLayout.SeatRail(plotPinned, below, Mathf.Round(Px(FlightHudLayout.GapPt)));
            Pin(plot, fit);
            // Too short to read (tiny screen, notes and banner open): the rail steps aside until the stack shrinks.
            plot.style.display = FlightHudLayout.RailShows(fit, upp) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Everything the rail may sit over: the tools, the notes, the call rows, the nudges and the dock.</summary>
        IEnumerable<VisualElement> StackElements()
        {
            if (hud == null) yield break;
            yield return hud.Q(UiIds.FlightFollow);
            yield return hud.Q(UiIds.FlightCamera);
            yield return hud.Q(UiIds.FlightPause);
            yield return help;
            yield return quote;
            yield return bar;
            yield return nudgeRow;
            yield return actions;
        }

        /// <summary>Shown on screen: displayed itself and every ancestor up to the HUD.</summary>
        bool Visible(VisualElement e)
        {
            for (var v = e; v != null; v = v.parent)
            {
                // The inline display first: a sheet just closed is still resolved as shown until the next pass.
                if (v.style.display.keyword == StyleKeyword.Undefined && v.style.display.value == DisplayStyle.None) return false;
                if (v.resolvedStyle.display == DisplayStyle.None) return false;
                if (v == hud) return true;
            }
            return true;
        }

        readonly List<VisualElement> watchedStack = new List<VisualElement>();

        /// <summary>Refit the rail whenever any stack element moves or resizes (notes open, a call, the nudge row).</summary>
        void WatchStack()
        {
            foreach (var e in StackElements())
            {
                if (e == null || watchedStack.Contains(e)) continue;
                watchedStack.Add(e);
                e.RegisterCallback<GeometryChangedEvent>(OnStackMoved);
            }
        }

        void UnwatchStack()
        {
            foreach (var e in watchedStack) e?.UnregisterCallback<GeometryChangedEvent>(OnStackMoved);
            watchedStack.Clear();
        }

        void OnStackMoved(GeometryChangedEvent _)
        {
            SeatPlot();
        }

        /// <summary>
        /// Portrait picker. Follow / Cam / Pause stay in the HUD's flex flow (tools margin, the dock spacer and
        /// the buttons' own margins), so the real row is not exactly the frame's row: CI 335x720 had the picker's
        /// bottom (frame row - GapPt) 1 px inside Pause. Seat it from the measured row instead, one GapPt clear
        /// (FlightHudLayout.PickerOverRow), in the room the frame leaves; fall back to the frame row before the
        /// first layout pass. Frame boxes are in the HUD's content box; absolute offsets start at its padding edge.
        /// </summary>
        void SeatRailPicker()
        {
            if (hud == null || railFrame == null || widePinned || !(upp > 0f)) return;
            // Before the first layout pass the frame is NaN (resolvedStyle): nothing to seat yet.
            if (!FlightHudLayout.PickerRoom(railFrame).Live || !FlightHudLayout.ToolsRow(railFrame).Live) return;
            float padL = hud.resolvedStyle.paddingLeft;
            float padT = hud.resolvedStyle.paddingTop;
            if (float.IsNaN(padL)) padL = 0f;
            if (float.IsNaN(padT)) padT = 0f;
            float ox = padL / upp, oy = padT / upp;
            var room = FlightHudLayout.PickerRoom(railFrame);
            room.X += ox;
            room.Y += oy;
            var row = FlightHudLayout.ToolsRow(railFrame);
            row.X += ox;
            row.Y += oy;
            var cam = railFrame.Camera;
            float anchor = cam.X + cam.W * 0.5f + ox;
            if (MeasuredPt(hud.Q(UiIds.FlightFollow), hud.Q(UiIds.FlightCamera), hud.Q(UiIds.FlightPause), out var real))
                row = real;
            if (MeasuredPt(hud.Q(UiIds.FlightCamera), null, null, out var realCam))
                anchor = realCam.X + realCam.W * 0.5f;
            // The stat strip, health row and stages card ride the flex flow too; the trajectory rail is pinned.
            if (MeasuredPt(stages, hud.Q("ascent-telemetry"), hud.Q("ascent-health"), out var realTop)
                && realTop.Bottom + FlightHudLayout.GapPt > room.Y)
            {
                float bottom = room.Bottom;
                room.Y = realTop.Bottom + FlightHudLayout.GapPt;
                room.H = Math.Max(1f, bottom - room.Y);
            }
            if (MeasuredPt(plot, null, null, out var realPlot) && realPlot.Right + FlightHudLayout.GapPt > room.X)
            {
                float right = room.Right;
                room.X = realPlot.Right + FlightHudLayout.GapPt;
                room.W = Math.Max(1f, right - room.X);
            }
            // Never past the real dock top either (the dock is pinned from the frame, without the padding offset).
            if (MeasuredPt(actions, null, null, out var realDock) && realDock.Y - FlightHudLayout.GapPt < room.Bottom)
                room.H = Math.Max(1f, realDock.Y - FlightHudLayout.GapPt - room.Y);
            float ceiling = FlightHudLayout.PickerCeiling(railFrame) + oy;
            if (railFrame.BannerRow && realTop.Live) ceiling = Math.Max(ceiling, realTop.Bottom + FlightHudLayout.GapPt);
            var box = FlightHudLayout.PickerOverRow(room, row, anchor, ceiling);
            var chips = FlightHudLayout.PickerChips(box);
            for (int i = 0; i < chips.Length; i++) chips[i] = FlightHudLayout.ToPanel(chips[i], upp, upp);
            FlightChrome.SeatPicker(hud, FlightHudLayout.ToPanel(box, upp, upp), chips);
        }

        /// <summary>Union of the shown elements' laid-out rects, HUD-relative, in points. False before layout.</summary>
        bool MeasuredPt(VisualElement a, VisualElement b, VisualElement c, out HudBox box)
        {
            box = default;
            if (hud == null) return false;
            var origin = hud.worldBound;
            if (float.IsNaN(origin.x) || float.IsNaN(origin.y)) return false;
            float x = float.MaxValue, y = float.MaxValue, r = float.MinValue, bt = float.MinValue;
            bool any = false;
            foreach (var e in new[] { a, b, c })
            {
                if (e == null || e.panel == null || e.resolvedStyle.display == DisplayStyle.None) continue;
                var wb = e.worldBound;
                if (float.IsNaN(wb.x) || float.IsNaN(wb.y) || float.IsNaN(wb.width) || float.IsNaN(wb.height)) continue;
                if (wb.width < 0.5f || wb.height < 0.5f) continue;
                any = true;
                x = Math.Min(x, wb.xMin - origin.x);
                y = Math.Min(y, wb.yMin - origin.y);
                r = Math.Max(r, wb.xMax - origin.x);
                bt = Math.Max(bt, wb.yMax - origin.y);
            }
            if (!any) return false;
            box = new HudBox { X = x / upp, Y = y / upp, W = (r - x) / upp, H = (bt - y) / upp };
            return true;
        }

        static HudBox[] ScaleAll(HudBox[] boxes, Func<HudBox, HudBox> scale)
        {
            var o = new HudBox[boxes.Length];
            for (int i = 0; i < boxes.Length; i++) o[i] = scale(boxes[i]);
            return o;
        }

        HudBox ScaleBox(HudBox b) => new HudBox { X = b.X * upp, Y = b.Y * upp, W = b.W * upp, H = b.H * upp };

        /// <summary>A dock button row's height: the 44 pt button (and the info button) in whole panel pixels.</summary>
        float DockRowPx()
        {
            float b = Px(FlightHudLayout.ButtonPt);
            float i = Px(FlightHudLayout.InfoPt);
            return Mathf.Ceil(Math.Max(b, i) - 0.01f);
        }

        /// <summary>
        /// The medal line and the info button. The row is never shorter than the info button plus its own vertical
        /// padding (zeroed here), in whole pixels; the frame budgets the dock for that height.
        /// </summary>
        void SeatMedalRow(HudBox box, HudBox dock)
        {
            var row = actions != null ? actions.Q("ascent-medal-row") : null;
            if (row == null) return;
            row.style.paddingTop = 0;
            row.style.paddingBottom = 0;
            float need = DockRowPx();
            if (box.Live && box.H < need) box.H = need;
            Seat(row, box, dock);
            if (row.style.display == DisplayStyle.None) return;
            row.style.minHeight = need;
            if (row.style.height.value.value < need) row.style.height = need;
            row.style.maxHeight = Math.Max(need, row.style.maxHeight.value.value);
        }

        void Seat(VisualElement row, HudBox box, HudBox dock)
        {
            if (row == null) return;
            if (actions == null || !box.Live || !dock.Live)
            {
                row.style.display = DisplayStyle.None;
                return;
            }
            // Whole pixels: the row's top and height round, so a 44 pt row is never 43.6 px.
            float top = Mathf.Round(box.Y - dock.Y);
            float left = box.X - dock.X;
            float h = Mathf.Round(box.H);
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
            // The frame is laid out in the HUD's face points (panel px / upp), not CSS points. The web panel is scaled
            // (1280x800 CSS is a 1214x759 panel, 1920x1080 and 2560x1440 are 1280x720) while every face, button and
            // pad is sized in upp: in CSS points the 44 pt rows were 41.7 px under a 44 px info button (#322 CI
            // 1280x800), the 36 pt strip was 24 px under a 28 px clock (#322 CI 1920x1080: h-t needed 64 x 26 in a
            // 64 x 22 box), and at 2560x1440 the strip would be 18 px. The CSS insets scale into the same points.
            float fw = pw / upp, fh = ph / upp;
            float sx = fw / cssW, sy = fh / cssH;
            float ux = upp;
            float uy = upp;
            float rowPx = DockRowPx();
            var f = FlightHudLayout.PlaceWide(fw, fh, safeTop * sy, safeBottom * sy, safeLeft * sx, safeRight * sx, mode, rowPx / upp, FlightHudLayout.ClockMinWidth());
            HudBox Sc(HudBox b) => FlightHudLayout.ToPanel(b, ux, uy);
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
            // The rail's frame box; SeatPlot fits it over the stack as laid out (after the pins below, and again
            // whenever a stack element moves).
            plotPinned = Sc(f.Plot);
            Pin(plot, plotPinned);
            Pin(stages, Sc(f.Stages));
            DressClock(f.Clock.W, f.Telemetry.H);

            var tools = hud.Q("flight-tools");
            if (tools != null)
            {
                tools.style.position = Position.Absolute;
                tools.style.left = 0; tools.style.top = 0; tools.style.right = 0; tools.style.bottom = 0;
                tools.style.marginBottom = 0;
                // The portrait row's inward-rounded side margins (FlightChrome.StyleTools) would shift the pins.
                tools.style.marginLeft = 0; tools.style.marginRight = 0;
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
            SeatMedalRow(Sc(Union(f.Medal, f.Info)), dock);
            PlaceRailOrDock(help, f.Help, InDock(f.Help), Sc(f.Help), actions, dock);
            PlaceRailOrDock(quote, f.Quote, InDock(f.Quote), Sc(f.Quote), actions, dock);
            if (quote != null && f.Quote.Live)
                quote.style.whiteSpace = WhiteSpace.Normal;
            PlaceRailOrDock(bar, f.Timer, InDock(f.Timer), Sc(f.Timer), actions, dock);
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
            // The notes sheet is PlaceWide's box (the left rail, under the plot), dressed but not moved; then the rail.
            PlaceInfoSheet();
            WatchStack();
        }

        void PlaceRailOrDock(VisualElement e, HudBox css, bool inDock, HudBox panel, VisualElement dock, HudBox dockBox)
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
                // L2: in the flex flow this row went to the top of the dock, over the medal line.
                if (dock != null && e.parent != dock) dock.Add(e);
                Seat(e, panel, dockBox);
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
            // Give the flex row its own sizes back (FlightChrome.Layout only runs at Bind).
            FlightChrome.StyleTools(hud, upp);
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
        /// Pin a tools-row button to its exact box. FlightChrome sized these with a min-width in points
        /// (WidestButton x upp); on a scaled web panel (1280x800 CSS is a 1214x759 panel, 0.948 units per CSS
        /// point) that min-width beat the pinned width by ~5 units, ate the 3.8-unit GapPt and Cam ran 1 px into
        /// Pause. min = max = the box, and FlightChrome.StyleTool leaves a pinned button's size alone.
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

        /// <summary>Undo PinTool; ClearWidePins then calls FlightChrome.StyleTools for the flex row's own sizes.</summary>
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
            PaintFocus();
            FeedTrajectory(director, alt, speed, q);
            if (plot != null) plot.Apply(trajectory.At(RecoveryLog.RailClock(flight.SimTime), alt, speed), upp);
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
                        ? AscentNotes.EngineerDetail
                        : AscentNotes.AssistDetail;
            }
            else
            {
                var p = prompts[0];
                shown = p.Call;
                if (quote != null)
                {
                    if (!string.Equals(quote.userData as string, p.Quote))
                    {
                        quote.userData = p.Quote;
                        quote.text = p.Quote;
                        FitQuote();
                    }
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
                caption.text = AscentNotes.Caption;
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
            // Portrait has the same problem: the rows were seated for the cruise dock when a call opened. A banner
            // that stops (or starts) fitting the stages card moves the card to (or from) its full-width row.
            else if (!widePinned && railMode.HasValue && (LiveMode() != railMode || nudgeRowOpen != railNudges || Math.Abs(RailBannerPt() - bannerRowPt) > 0.01f)) Layout(upp);
            string key = shown + "|" + (quote != null ? quote.userData as string : "") + "|" + (primary != null ? primary.text : "") + "|" + (alternate != null ? alternate.text : "");
            if (key != fittedFor)
            {
                fittedFor = key;
                FitText();
            }
            KeepPauseOnTop();
        }

        /// <summary>
        /// Device tests and PNG captures: the strip readings, the stages banner and an optional call (the engine-out
        /// prompt) without a running flight. Same rows, same seating and fitting as <see cref="Present"/>.
        /// </summary>
        public void Preview(string altText, string speedText, string verdict, AscentPrompt? call) =>
            Preview(altText, speedText, verdict, call, false, null);

        /// <summary>
        /// <see cref="Preview(string,string,string,AscentPrompt?)"/> with the cruise dock as Present draws it before any
        /// call is decided: <paramref name="medalLine"/> on the medal line (Present: "Flying the plan.") and, with
        /// <paramref name="engineerNudges"/>, the Nudge -1 / +1 row (Present shows it for director.Engineer).
        /// </summary>
        public void Preview(string altText, string speedText, string verdict, AscentPrompt? call, bool engineerNudges, string medalLine)
        {
            if (root == null) return;
            root.style.display = DisplayStyle.Flex;
            if (actions != null) actions.style.display = DisplayStyle.Flex;
            var ink = new Color(0.85f, 0.92f, 0.96f);
            bool prompt = call.HasValue;
            SetCell(0, "ALT", altText, ink);
            SetCell(1, "SPEED", speedText, ink);
            SetCell(2, prompt ? "TWR" : "Q", prompt ? "0.94" : "999 kPa", new Color(0.94f, 0.63f, 0.12f));
            SetCell(3, prompt ? "PROP" : "G", prompt ? "100%" : "12.4", new Color(0.30f, 0.82f, 0.55f));
            PaintFocus();
            var v = stages != null ? stages.Q<Label>("h-verdict") : null;
            if (v != null) v.text = verdict ?? "";
            if (medal != null) medal.text = medalLine ?? "Medal timing: Silver · 1 perfect · 0 good · 0 late";
            if (assist != null) assist.text = "Auto-assist on";
            if (prompt)
            {
                var p = call.Value;
                shown = p.Call;
                if (quote != null) { quote.userData = p.Quote; quote.text = p.Quote; quote.style.display = DisplayStyle.Flex; }
                if (bar != null) bar.style.display = DisplayStyle.Flex;
                if (primary != null) { primary.text = p.Primary; primary.style.display = DisplayStyle.Flex; }
                if (alternate != null) { alternate.text = p.Alternate ?? ""; alternate.style.display = p.ShowAlternate ? DisplayStyle.Flex : DisplayStyle.None; }
                if (skip != null) skip.style.display = DisplayStyle.Flex;
                if (nudgeDown != null) nudgeDown.style.display = DisplayStyle.None;
                if (nudgeUp != null) nudgeUp.style.display = DisplayStyle.None;
                nudgeRowOpen = p.ShowAlternate;
            }
            else
            {
                shown = default;
                if (quote != null) { quote.userData = null; quote.style.display = DisplayStyle.None; }
                if (bar != null) bar.style.display = DisplayStyle.None;
                if (primary != null) primary.style.display = DisplayStyle.None;
                if (alternate != null) alternate.style.display = DisplayStyle.None;
                if (skip != null) skip.style.display = DisplayStyle.None;
                // Present: showNudgeRow = director.Engineer || showAlternate, the nudges themselves for Engineer.
                nudgeRowOpen = engineerNudges;
                if (nudgeDown != null) { nudgeDown.text = "Nudge −1°"; nudgeDown.style.display = engineerNudges ? DisplayStyle.Flex : DisplayStyle.None; }
                if (nudgeUp != null) { nudgeUp.text = "Nudge +1°"; nudgeUp.style.display = engineerNudges ? DisplayStyle.Flex : DisplayStyle.None; }
            }
            PlaceAssist(prompt);
            if (promptRow != null) promptRow.style.display = prompt ? DisplayStyle.Flex : DisplayStyle.None;
            if (assistRow != null) assistRow.style.display = prompt ? DisplayStyle.None : DisplayStyle.Flex;
            if (nudgeRow != null) nudgeRow.style.display = nudgeRowOpen ? DisplayStyle.Flex : DisplayStyle.None;
            StyleStages();
            PlaceDockSpacer();
            Layout(upp);
            fittedFor = null;
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
            // #322 CI SE cruise: the spacer reserved the buttons without the closed nudge row while LayoutRail pinned
            // the frame's dock with it, so the flex tools row (Follow y 514) sat inside the dock (y 519). The spacer
            // reserves exactly the frame's dock (FlightHudLayout.Place with the same mode and nudge row).
            float pt = FlightHudLayout.DockHeight(infoOpen, prompt, FlightHudLayout.ButtonPt, nudgeRowOpen);
            dockSpacer.style.height = Px(pt);
        }

        /// <summary>The clock face for its box in points of the HUD's face scale (upp): 28 -> 13 pt, widths budgeted.</summary>
        void DressClock(float boxW, float boxH)
        {
            var clock = hud != null ? hud.Q<Label>("h-t") : null;
            if (clock == null) return;
            FlightDusk.Clock(clock, upp, FlightHudLayout.ClockFit(boxW, boxH));
            clock.style.marginLeft = 0; clock.style.marginRight = 0; clock.style.marginTop = 0; clock.style.marginBottom = 0;
        }

        void BuildHealth()
        {
            health = new VisualElement { name = "ascent-health", pickingMode = PickingMode.Ignore };
            health.style.flexDirection = FlexDirection.Column;
            health.style.flexShrink = 0;
            health.style.overflow = Overflow.Visible;
            health.style.marginBottom = Px(FlightHudLayout.GapPt);
            var row = new VisualElement { name = HealthRowName, pickingMode = PickingMode.Position };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0;
            SizeHealthRow(row);
            // The tabs draw 28 pt; the row is the 44 pt hit band. A tap in the band above or below a tab opens that tab.
            row.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target != row || healthButtons == null) return;
                var cols = new Rect[healthButtons.Length];
                for (int k = 0; k < healthButtons.Length; k++) cols[k] = healthButtons[k] != null ? healthButtons[k].layout : default;
                int hit = HealthTabAt(e.localPosition.x, cols);
                if (hit >= 0) OnHealth(hit);
            });
            healthButtons = new Button[VehicleHealth.Count];
            var calm = VehicleHealth.Read(new VehicleHealthInput { Fuel01 = 1, QLimitPa = 1, GLimit = 1 });
            for (int i = 0; i < VehicleHealth.Count; i++)
            {
                int index = i;
                var b = new Button(() => OnHealth(index)) { name = UiIds.FlightHealth(i), text = calm[i].Mark };
                b.tooltip = calm[i].Name + ". " + calm[i].Detail;
                b.style.flexGrow = 1;
                b.style.flexShrink = 1;
                b.style.flexBasis = 0;
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
                PaintChip(b, calm[i].Tone, false);
                FlightDusk.Face(b, FlightDusk.BodyFace());
                healthButtons[i] = b;
                row.Add(b);
            }
            // #332 (t432): the detail was pinned 28 pt down inside the health row, i.e. right on the ALT / SPEED / Q / G
            // strip, and cut. It is now a popover under the strip (over the cards, never the readouts), added last to
            // the ascent root so it draws above them, full text, dismissed by the same tab or any tap outside it.
            healthDetail = new Label { name = HealthDetailName, pickingMode = PickingMode.Position };
            healthDetail.style.position = Position.Absolute;
            healthDetail.style.left = 0;
            healthDetail.style.right = 0;
            healthDetail.style.display = DisplayStyle.None;
            healthDetail.style.whiteSpace = WhiteSpace.Normal;
            healthDetail.RegisterCallback<ClickEvent>(_ => CloseHealth());
            health.Add(row);
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
                PaintChip(b, parts[i].Tone, healthOpen == i);
            }
            if (healthDetail == null) return;
            if (healthOpen >= 0 && healthOpen < parts.Length)
            {
                healthDetail.text = parts[healthOpen].Name + ". " + parts[healthOpen].Detail;
                healthDetail.style.display = DisplayStyle.Flex;
                SeatHealthDetail();
            }
            else healthDetail.style.display = DisplayStyle.None;
        }

        public const string HealthDetailName = "ascent-health-detail";

        /// <summary>The chip's plate, label and bottom edge are its health (HealthChip); open adds a cream ring and
        /// changes nothing else (the ring is always there, plate-coloured when closed, so the label never moves). Was: an open tab took the dark alarm plate, so a green Eng or Hull read red.</summary>
        void PaintChip(Button b, VehicleHealthTone tone, bool open)
        {
            var look = HealthChip.Look(tone, open);
            float k = upp > 0.5f ? upp : 1f;
            b.style.backgroundColor = new Color(look.Plate.R, look.Plate.G, look.Plate.B, look.Plate.A);
            b.style.color = new Color(look.Text.R, look.Text.G, look.Text.B, 1f);
            var edge = new Color(look.Edge.R, look.Edge.G, look.Edge.B, 1f);
            var ring = new Color(look.Ring.R, look.Ring.G, look.Ring.B, look.Ring.A);
            float ringW = Mathf.Max(1f, Mathf.Round(look.RingPt * k));
            b.style.borderBottomWidth = Mathf.Max(1f, Mathf.Round(look.EdgePt * k));
            b.style.borderBottomColor = edge;
            b.style.borderTopWidth = ringW; b.style.borderLeftWidth = ringW; b.style.borderRightWidth = ringW;
            b.style.borderTopColor = ring; b.style.borderLeftColor = ring; b.style.borderRightColor = ring;
        }

        /// <summary>The open health popover: one gap under the stat strip, full width, whole pixels, never cut.</summary>
        void SeatHealthDetail()
        {
            if (healthDetail == null || root == null) return;
            FlightDusk.Sentence(healthDetail, upp);
            healthDetail.style.overflow = Overflow.Visible;
            healthDetail.style.height = StyleKeyword.Auto;
            healthDetail.style.maxHeight = StyleKeyword.None;
            healthDetail.style.backgroundColor = FlightDusk.Ink;
            healthDetail.style.paddingLeft = Mathf.Round(Px(8f));
            healthDetail.style.paddingRight = Mathf.Round(Px(8f));
            healthDetail.style.paddingTop = Mathf.Round(Px(6f));
            healthDetail.style.paddingBottom = Mathf.Round(Px(6f));
            float r = Mathf.Round(Px(10f));
            healthDetail.style.borderTopLeftRadius = r; healthDetail.style.borderTopRightRadius = r;
            healthDetail.style.borderBottomLeftRadius = r; healthDetail.style.borderBottomRightRadius = r;
            healthDetail.style.borderBottomWidth = Mathf.Round(Px(2f));
            healthDetail.style.borderBottomColor = FlightDusk.Sun;
            var strip = hud != null ? hud.Q("ascent-telemetry") : null;
            float top = Mathf.Round(Px(FlightHudLayout.HealthPt + FlightHudLayout.GapPt + FlightHudLayout.TelemetryPt + FlightHudLayout.GapPt));
            healthDetail.style.left = 0; healthDetail.style.right = 0; healthDetail.style.width = StyleKeyword.Auto;
            // Measured (the landscape pins move the strip and the tabs), in the root's frame: under both in portrait,
            // beside the tabs and under the strip in landscape (FlightHudLayout.HealthPopover).
            var sb = strip != null ? strip.worldBound : default;
            var hbx = health != null ? health.worldBound : default;
            var rb = root.worldBound;
            if (strip != null && strip.panel != null && sb.height > 1f && !float.IsNaN(sb.yMax) && !float.IsNaN(rb.yMin)
                && health != null && health.panel != null && hbx.height > 1f && !float.IsNaN(hbx.yMax))
            {
                var tabs = new HudBox { X = hbx.xMin - rb.xMin, Y = hbx.yMin - rb.yMin, W = hbx.width, H = hbx.height };
                var cells = new HudBox { X = sb.xMin - rb.xMin, Y = sb.yMin - rb.yMin, W = sb.width, H = sb.height };
                var at = FlightHudLayout.HealthPopover(tabs, cells, Mathf.Round(Px(FlightHudLayout.GapPt)),
                    Mathf.Round(Px(FlightHudLayout.PopoverMinPt)), Mathf.Round(Px(FlightHudLayout.PopoverMaxPt)));
                top = at.Y;
                healthDetail.style.left = at.X;
                healthDetail.style.right = StyleKeyword.Auto;
                healthDetail.style.width = at.W;
            }
            healthDetail.style.top = top;
            healthDetail.BringToFront();
        }

        /// <summary>Health popover state (tests): the open tab, or -1.</summary>
        public int HealthOpen => healthOpen;

        void CloseHealth()
        {
            if (healthOpen < 0) return;
            healthOpen = -1;
            if (healthDetail != null) healthDetail.style.display = DisplayStyle.None;
            if (flight != null) Present(flight);
            else PaintCalmChips(VehicleHealth.Read(new VehicleHealthInput { Fuel01 = 1, QLimitPa = 1, GLimit = 1 }));
        }

        /// <summary>A tap anywhere outside the health tabs closes the open detail (the tabs toggle it themselves).</summary>
        void OnHudPointerDown(PointerDownEvent e) => TapAt(e.target as VisualElement);

        /// <summary>A tap landed on <paramref name="target"/>: outside the health tabs it closes the open detail.
        /// True when it closed it. The HUD's PointerDown handler and the device tests call it.</summary>
        public bool TapAt(VisualElement target)
        {
            if (healthOpen < 0) return false;
            var row = health != null ? health.Q(HealthRowName) : null;
            for (var v = target; v != null; v = v.parent)
                if (v == row) return false;
            CloseHealth();
            return true;
        }

        public const string HealthRowName = "ascent-health-row";

        /// <summary>
        /// L1 tab row: 44 pt tall (FlightHudLayout.HitPt) with negative margins, so the flow still gives it the 28 pt
        /// FlightHudLayout.HealthPt (the stages card, #304's picker and the staging callout do not move).
        /// </summary>
        void SizeHealthRow(VisualElement row)
        {
            if (row == null) return;
            // Whole pixels, so the band and the 28 pt tabs centre without a half-pixel seam.
            float slop = Mathf.Round(Px(FlightHudLayout.HealthSlopPt));
            float band = Mathf.Round(Px(FlightHudLayout.HealthPt)) + 2f * slop;
            row.style.height = band;
            row.style.minHeight = band;
            row.style.marginTop = -slop;
            row.style.marginBottom = -slop;
        }

        /// <summary>The tab whose column holds <paramref name="x"/> (row-local), else the nearest one; -1 with no tabs.</summary>
        public static int HealthTabAt(float x, IList<Rect> columns)
        {
            if (columns == null || columns.Count == 0 || float.IsNaN(x)) return -1;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < columns.Count; i++)
            {
                var c = columns[i];
                if (!(c.width > 0f)) continue;
                if (x >= c.xMin && x <= c.xMax) return i;
                float d = x < c.xMin ? c.xMin - x : x - c.xMax;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void OnHealth(int index)
        {
            healthOpen = healthOpen == index ? -1 : index;
            if (flight != null) Present(flight);
            else if (healthDetail != null)
            {
                // Before the first flight frame (and in the device tests): the calm reading.
                var calm = VehicleHealth.Read(new VehicleHealthInput { Fuel01 = 1, QLimitPa = 1, GLimit = 1 });
                bool open = healthOpen >= 0 && healthOpen < calm.Length;
                if (open) healthDetail.text = calm[healthOpen].Name + ". " + calm[healthOpen].Detail;
                healthDetail.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
                if (open) SeatHealthDetail();
                PaintCalmChips(calm);
            }
        }

        void PaintCalmChips(VehicleHealthPart[] calm)
        {
            for (int i = 0; healthButtons != null && i < healthButtons.Length && i < calm.Length; i++)
                if (healthButtons[i] != null) PaintChip(healthButtons[i], calm[i].Tone, healthOpen == i);
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
                SeatPlot();
                return;
            }
            if (widePinned)
            {
                // Landscape: LayoutWideAt placed the sheet where PlaceWide put it (the left rail under the plot, or
                // hidden when there is no room). #332 CI 1280x800: this used to re-pin it as the portrait sheet, full
                // width over the dock, on top of the rail, Cam and Pause, where neither the frame nor the rail knew it.
                DressInfoSheet(help.style.height.keyword == StyleKeyword.Undefined ? help.style.height.value.value : Px(FlightHudLayout.HelpPt));
                SeatPlot();
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
                float pt = FlightHudLayout.DockHeight(true, prompt, FlightHudLayout.ButtonPt, nudgeRowOpen);
                if (pt < FlightHudLayout.ButtonPt) pt = FlightHudLayout.ButtonPt;
                dock = Px(pt);
            }
            // Portrait: Follow / Cam / Pause sit right above the dock, so the sheet floats above them, never over them.
            float tools = widePinned ? 0f : Px(FlightHudLayout.ButtonPt + FlightHudLayout.GapPt);
            // `bottom` is from the HUD's padding edge; the dock and the tools row sit inside the bottom padding (#332:
            // without it the sheet covered Follow / Cam / Pause by the home-indicator inset).
            float padB = widePinned ? 0f : hud.resolvedStyle.paddingBottom;
            if (float.IsNaN(padB)) padB = 0f;
            float toolsMargin = 0f;
            var toolRow = hud.Q("flight-tools");
            if (!widePinned && toolRow != null && toolRow.style.marginBottom.keyword == StyleKeyword.Undefined)
                toolsMargin = toolRow.style.marginBottom.value.value;
            float bottom = padB + dock + Px(FlightHudLayout.GapPt) + tools + toolsMargin;
            // Once laid out, sit exactly one gap above the real tools row (the flex flow decides where it is).
            if (!widePinned && toolRow != null && toolRow.panel != null && toolRow.resolvedStyle.display != DisplayStyle.None)
            {
                var hb = hud.worldBound; var tb = toolRow.worldBound;
                if (!float.IsNaN(hb.yMax) && !float.IsNaN(tb.yMin) && tb.height > 0.5f)
                    bottom = Mathf.Max(bottom, hb.yMax - tb.yMin + Px(FlightHudLayout.GapPt));
            }
            bottom = Mathf.Round(bottom);
            help.style.bottom = bottom;
            float panelH = hud.resolvedStyle.height;
            if (float.IsNaN(panelH) || panelH < 8f) panelH = 700f * upp;
            // Still under the rocket band where there is room, 48 to 96 pt (the grid models the same call).
            float sheet = FlightHudLayout.NotesSheetPt(panelH / upp, bottom / upp, prompt);
            DressInfoSheet(Mathf.Round(Px(sheet)));
            help.style.minHeight = Mathf.Round(Px(48f));
            SeatPlot();
        }

        /// <summary>Fixed height, and it scrolls: the notes never grow over the rows under them (#332).</summary>
        void DressInfoSheet(float heightPx)
        {
            if (help == null) return;
            help.style.height = heightPx;
            help.style.maxHeight = heightPx;
            help.style.minHeight = heightPx;
            help.style.overflow = Overflow.Hidden;
            help.mode = ScrollViewMode.Vertical;
            help.verticalScrollerVisibility = ScrollerVisibility.Auto;
            help.pickingMode = PickingMode.Position;
            FlightDusk.Frame(help, upp, false);
            FlightDusk.Sentence(detail, upp);
            FlightDusk.Sentence(caption, upp);
            FlightDusk.Sentence(bonus, upp);
            FlightDusk.Sentence(extra, upp);
            help.BringToFront();
            KeepPauseOnTop();
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
            ascentKey[i] = name;
            ascentVal[i] = value;
            ascentInk[i] = color;
            if (booster.HasValue) return;   // the booster has the strip; PaintFocus draws it
            DrawCell(i, name, value, color, "");
        }

        void DrawCell(int i, string name, string value, Color color, string spoken)
        {
            if (cellKey[i] != null) cellKey[i].text = name;
            if (cellVal[i] == null) return;
            cellVal[i].text = value;
            cellVal[i].style.color = color;
            cellVal[i].tooltip = spoken ?? "";
        }

        /// <summary>Which stage the strip reads: the upper stage (ascent) or the booster the camera is following.</summary>
        public FlightHudFocus Focus => booster.HasValue ? FlightHudFocus.Booster : FlightHudFocus.Ascent;

        /// <summary>
        /// SliceApp, every flight frame: the booster's readout while the camera follows it (RecoveryShow.SteerCamera),
        /// null otherwise. Applies at once: the strip reads ALT / DOWN / FUEL / TO DECK and the upper stage's health
        /// tabs hide (visibility, so no row moves); null brings back the last ascent reading and the tabs.
        /// </summary>
        public void SetBooster(BoosterHudReadout? readout)
        {
            booster = readout;
            if (root != null) PaintFocus();
        }

        void PaintFocus()
        {
            bool onBooster = booster.HasValue;
            if (onBooster)
            {
                var r = booster.Value;
                for (int i = 0; i < CellCount && i < BoosterHudReadout.Count; i++)
                {
                    var c = r[i];
                    DrawCell(i, c.Key, c.Value, ToneInk(c.Tone), c.Spoken);
                }
                // The upper stage's tabs (Eng .. Fair) are not the booster's health: close any open note with them.
                if (healthOpen >= 0) healthOpen = -1;
                if (healthDetail != null) healthDetail.style.display = DisplayStyle.None;
            }
            else
            {
                for (int i = 0; i < CellCount; i++)
                    if (ascentKey[i] != null) DrawCell(i, ascentKey[i], ascentVal[i], ascentInk[i], "");
            }
            if (health != null)
            {
                if (BoosterHud.ShowsAscentChrome(Focus)) health.style.visibility = StyleKeyword.Null;
                else health.style.visibility = Visibility.Hidden;
            }
        }

        static Color ToneInk(BoosterHudTone tone)
        {
            switch (tone)
            {
                case BoosterHudTone.Amber: return new Color(0.94f, 0.63f, 0.12f);
                case BoosterHudTone.Green: return new Color(0.30f, 0.82f, 0.55f);
                default: return new Color(0.85f, 0.92f, 0.96f);
            }
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
                cell.style.justifyContent = Justify.Center;
                cell.style.flexBasis = 0;
                cell.style.minWidth = 0;
                cell.style.overflow = Overflow.Hidden;
                if (i < CellCount - 1) cell.style.marginRight = Px(FlightHudLayout.CellGapPt);
                var key = new Label { name = "ascent-cell-" + i + "-k", pickingMode = PickingMode.Ignore };
                key.style.unityTextAlign = TextAnchor.MiddleCenter;
                key.style.whiteSpace = WhiteSpace.NoWrap;
                key.style.color = new Color(0.65f, 0.75f, 0.82f);
                key.style.unityFontStyleAndWeight = FontStyle.Bold;
                NoBox(key);
                var val = new Label { name = "ascent-cell-" + i + "-v", pickingMode = PickingMode.Ignore };
                val.style.unityTextAlign = TextAnchor.MiddleCenter;
                val.style.whiteSpace = WhiteSpace.NoWrap;
                val.style.unityFontStyleAndWeight = FontStyle.Bold;
                NoBox(val);
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
                trajectory.StrapOns = vehicle.StrapOns > 0;
                trajectory.RecoveryMode = string.IsNullOrEmpty(vehicle.RecoveryMode) ? null : vehicle.RecoveryMode;
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
            if (director != null && !double.IsNaN(director.FairingT))
                trajectory.Seen(new TrajectoryEvent { T = director.FairingT, Type = "fairing", AltM = director.FairingAltM, SpeedMps = director.FairingSpeedMps });
            if (flight.Sim != null) trajectory.Burn(flight.Sim.StageIdx + 1, flight.Sim.PropellantFraction);
            trajectory.Sample(flight.SimTime, alt, speed, q);
            // t466: insertion (or a clean cutoff) makes SECO / Orbit the final stop; no wait on a booster stop.
            if (flight.Result != null && !flight.Running && !flight.Lost) trajectory.AscentOver(flight.SimTime);
            // Live marks while the booster flies, on the flight clock (RecoveryLog.FlightMarks). Current alone
            // exists only at touchdown, and its times count from separation.
            var recMarks = RecoveryLog.FlightMarks;
            if (recMarks != null)
            {
                for (int i = 0; i < recMarks.Length; i++)
                {
                    var mark = recMarks[i];
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
            stages.RegisterCallback<GeometryChangedEvent>(OnStagesMoved);
            HideStageSentences();
        }

        /// <summary>The card changed width (card to banner row, or a resize): refit the banner to the new width.</summary>
        void OnStagesMoved(GeometryChangedEvent e)
        {
            if (stages == null || Mathf.Abs(e.oldRect.width - e.newRect.width) < 0.5f) return;
            var verdict = stages.Q<Label>("h-verdict");
            if (verdict != null) FlightDusk.FitBanner(verdict, upp);
        }

        void RestoreStages()
        {
            stages?.UnregisterCallback<GeometryChangedEvent>(OnStagesMoved);
            bannerRowPt = 0f;
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
            // Banner row (portrait, the banner does not fit the card in two lines at 11 pt): the card spans the
            // content width, as tall as the banner, and the gap beside it goes.
            bool row = bannerRowPt > 0f && !widePinned;
            stages.style.width = row ? Length.Percent(100f) : Length.Percent(FlightHudLayout.CardFraction * 100f);
            stages.style.height = row ? Mathf.Round(Px(bannerRowPt)) : Px(FlightHudLayout.PlotPt);
            if (gap != null) gap.style.display = row ? DisplayStyle.None : DisplayStyle.Flex;
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
                string said = FlightDusk.BannerText(verdict);
                bool alarm = said != null && said.IndexOf("lost", StringComparison.OrdinalIgnoreCase) >= 0;
                // L1: "Climbing" ran off the right edge; the banner never outgrows the stages card. Banner fits the
                // face and the line count to the card (FlightDusk.FitBanner); the clip is only a backstop.
                verdict.style.flexShrink = 0;
                verdict.style.overflow = Overflow.Hidden;
                verdict.style.textOverflow = TextOverflow.Clip;
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
                if (b.name == UiIds.FlightSkipCount)
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
            // The rail ends above the notes sheet as it is really laid out (SeatPlot), so it follows the sheet's moves.
            help.RegisterCallback<GeometryChangedEvent>(_ => SeatPlot());
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

        /// <summary>No default label margin or padding: the strip is 36 pt for two lines.</summary>
        static void NoBox(VisualElement e)
        {
            e.style.marginTop = 0; e.style.marginBottom = 0; e.style.marginLeft = 0; e.style.marginRight = 0;
            e.style.paddingTop = 0; e.style.paddingBottom = 0; e.style.paddingLeft = 0; e.style.paddingRight = 0;
            e.style.maxWidth = Length.Percent(100);
            e.style.overflow = Overflow.Hidden;
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
                // Landscape: the notes change PlaceWide's mode (the sheet gets its rail box, the plot shortens): re-pin.
                if (widePinned && UseWideNow()) LayoutWide();
                PlaceInfoSheet();
                FlightChrome.Restack(hud);
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
            // L2: with an auto basis "SHUT DOWN ENGINE 1 (will not hold)" took its text width and ran under Skip.
            b.style.flexBasis = 0;
            b.style.minWidth = 0;
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
                    Unpad(born);
                    labels.Add(born);
                }
                for (int i = 0; i < tags.Length; i++)
                {
                    var label = (Label)labels[i];
                    var mark = i < marks.Length ? marks[i] : default;
                    label.text = string.Join("\n", tags[i].Lines);
                    label.tooltip = label.text;
                    float fontPx = tags[i].FontPt * upp;
                    label.style.fontSize = fontPx;
                    label.style.unityFontStyleAndWeight = mark.Upcoming ? FontStyle.Bold : FontStyle.Normal;
                    label.style.left = tags[i].Box.X * upp;
                    label.style.top = tags[i].Box.Y * upp;
                    label.style.width = tags[i].Box.W * upp;
                    // TrajectoryWatch.Tags budgets font + 3 pt a line, but the HUD face draws a taller line (CI t017: an 11 pt
                    // "Liftoff" measured 34.6 px in a 28.3 px box at the SE). The box takes the drawn line's height; tags
                    // are spaced well apart, and the checks compare the drawn text, not the box. A pure function of the
                    // tag: this runs every frame and inside the rail's GeometryChanged, so it never measures text or reads
                    // resolved style (no layout feedback from the labels).
                    label.style.height = TagHeight(tags[i].Box.H * upp, fontPx, tags[i].Lines.Length);
                    label.style.color = mark.Passed
                        ? new Color(0.45f, 0.86f, 0.62f)
                        : mark.Upcoming
                            ? new Color(1f, 0.82f, 0.35f)
                            : new Color(0.78f, 0.84f, 0.9f, 0.82f);
                }
            }

            /// <summary>Line box per pixel of face: above the HUD face's drawn line (CI: 34.6 px for an 11 pt tag).</summary>
            const float DrawnLineHeight = 1.6f;

            /// <summary>The tag's box, grown to whole lines of <see cref="DrawnLineHeight"/>.</summary>
            static float TagHeight(float boxPx, float fontPx, int lines) =>
                Mathf.Max(boxPx, Mathf.Ceil(Mathf.Max(1, lines) * fontPx * DrawnLineHeight));

            /// <summary>No theme padding or margin: the tag's box is its text box (MissionBoardScreen.Unpad).</summary>
            static void Unpad(VisualElement el)
            {
                el.style.paddingLeft = 0f;
                el.style.paddingRight = 0f;
                el.style.paddingTop = 0f;
                el.style.paddingBottom = 0f;
                el.style.marginLeft = 0f;
                el.style.marginRight = 0f;
                el.style.marginTop = 0f;
                el.style.marginBottom = 0f;
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
