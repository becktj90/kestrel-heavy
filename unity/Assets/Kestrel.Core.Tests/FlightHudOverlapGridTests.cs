// The dotnet model grid for the two overlap kinds CI EditMode found on #332 (job 119394354782):
//   1) 335x720, 24 px pads, cruise: the pinned rail (bottom 496) ran into the flex Follow / Cam / Pause row (top 495);
//   2) 1280x800 web (panel 1214x759): the health detail, "one gap under the strip", landed on the landscape health tabs.
// Every portrait size x safe-area pad x HUD mode x banner row x nudge row x panel scale, with the flex tools row
// anywhere from the frame's row up to 8 px higher (its 8 px margin and the buttons' own margins), and every landscape
// panel: the rail fitted by FlightHudLayout.PlotAbove and the popover from FlightHudLayout.HealthPopover never touch
// the stack, in whole pixels.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightHudOverlapGridTests
    {
        static readonly float[] Widths = { 320f, 335f, 360f, 375f, 390f, 393f, 402f, 414f, 428f, 430f, 440f, 744f, 820f, 1032f };
        static readonly float[] Heights = { 568f, 640f, 667f, 720f, 812f, 844f, 874f, 896f, 932f, 956f, 1133f, 1180f, 1376f };
        static readonly float[][] Pads = { new[] { 0f, 0f, 0f }, new[] { 24f, 24f, 24f }, new[] { 20f, 0f, 0f }, new[] { 0f, 20f, 0f }, new[] { 47f, 34f, 0f }, new[] { 62f, 34f, 24f }, new[] { 59f, 34f, 0f } };
        static readonly FlightHudMode[] Modes = { FlightHudMode.Cruise, FlightHudMode.Notes, FlightHudMode.Prompt, FlightHudMode.PromptNotes };
        static readonly float[] Scales = { 1f, 2f, 3f };
        static readonly float[] RowLiftPx = { 0f, 1f, 4f, 5f, 8f };

        static HudBox Px(HudBox b, float upp, float ox, float oy) => new HudBox { X = b.X * upp + ox, Y = b.Y * upp + oy, W = b.W * upp, H = b.H * upp };

        static bool Whole(float v) => Math.Abs(v - Math.Round(v)) < 1e-3;

        [Test]
        public void PortraitRailEndsAboveTheMeasuredDockStackEverywhere()
        {
            int cases = 0, hidden = 0;
            var covered = new HashSet<(bool, bool, bool)>();
            var fails = new List<string>();
            foreach (float w in Widths)
            foreach (float h in Heights)
            {
                if (h <= w) continue;
                foreach (var pad in Pads)
                foreach (var mode in Modes)
                foreach (bool banner in new[] { false, true })
                foreach (bool nudges in new[] { true, false })
                foreach (float upp in Scales)
                {
                    float top = pad[0], bottom = pad[1], side = pad[2];
                    float cw = w - 2f * side, ch = h - top - bottom;
                    // AscentHud.LayoutRail: the frame in the content box, pinned from the padding edge.
                    var f = FlightHudLayout.Place(cw, ch, 0f, 0f, 0f, 0f, mode, banner ? 40f : 0f, nudges);
                    float ox = side * upp, oy = top * upp;
                    var plot = Px(f.Plot, upp, ox, oy);
                    var dock = Px(f.Dock, upp, ox, oy);
                    var row = Px(FlightHudLayout.ToolsRow(f), upp, ox, oy);
                    float gap = (float)Math.Round(FlightHudLayout.GapPt * upp);
                    bool notes = mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes;
                    foreach (float lift in RowLiftPx)
                    {
                        cases++;
                        var flexRow = row; flexRow.Y -= lift;
                        // The notes sheet floats one gap above the real row (AscentHud.PlaceInfoSheet), sized by the
                        // same NotesSheetPt the HUD calls; the grid seats it at its 96 pt ceiling, the tallest it gets.
                        float sheetBottomPx = (float)Math.Round(h * upp - (flexRow.Y - gap));
                        float sheetPt = FlightHudLayout.NotesSheetPt(h, sheetBottomPx / upp, mode == FlightHudMode.PromptNotes);
                        if (notes && (sheetPt > 96f + 1e-3f || sheetPt < 48f - 1e-3f)) fails.Add(w + "x" + h + " " + mode + ": notes sheet " + sheetPt + " pt outside 48..96");
                        var help = notes ? new HudBox { X = dock.X, Y = flexRow.Y - gap - 96f * upp, W = dock.W, H = 96f * upp } : default;
                        float ceiling = Math.Min(flexRow.Y, dock.Y);
                        if (help.Live) ceiling = Math.Min(ceiling, help.Y);
                        covered.Add((notes, banner, nudges));
                        // The rail is seated by the call AscentHud.SeatPlot makes, over the stack it measures.
                        var fit = FlightHudLayout.SeatRail(plot, new[] { flexRow, dock, help }, gap);
                        // Too short to read, the rail is hidden (AscentHud.SeatPlot); hidden, it overlaps nothing.
                        if (!FlightHudLayout.RailShows(fit, upp)) { hidden++; if (!notes) fails.Add(w + "x" + h + " " + mode + " @" + upp + ": rail hidden without the notes"); continue; }
                        string at = w + "x" + h + " pads " + top + "/" + bottom + "/" + side + " " + mode + (banner ? " banner" : "") + (nudges ? "" : " no-nudges") + " @" + upp + "x lift " + lift;
                        if (!Whole(fit.X) || !Whole(fit.Y) || !Whole(fit.W) || !Whole(fit.H)) fails.Add(at + ": not whole pixels " + Fmt(fit));
                        if (fit.Bottom > ceiling - gap + 1e-3f) fails.Add(at + ": rail bottom " + fit.Bottom + " not one gap above " + ceiling);
                        if (fit.Y < plot.Y - 1e-3f || fit.Bottom > plot.Bottom + 1e-3f || fit.Right > plot.Right + 1e-3f) fails.Add(at + ": rail grew " + Fmt(fit) + " from " + Fmt(plot));
                        foreach (var (name, b) in new[] { ("tools row", flexRow), ("dock", dock), ("notes", help), ("health", Px(f.Health, upp, ox, oy)), ("telemetry", Px(f.Telemetry, upp, ox, oy)) })
                            if (b.Live && Hit(fit, b)) fails.Add(at + ": rail " + Fmt(fit) + " overlaps " + name + " " + Fmt(b));
                        if (f.BannerRow && Hit(fit, Px(f.Stages, upp, ox, oy))) fails.Add(at + ": rail overlaps the banner row");
                        
                    }
                }
            }
            Assert.That(cases, Is.GreaterThan(10000));
            // Every portrait case ran with the notes open and closed, the banner row on and off, and the nudge row in and out.
            foreach (bool n in new[] { false, true }) foreach (bool b in new[] { false, true }) foreach (bool u in new[] { false, true })
                Assert.That(covered.Contains((n, b, u)), Is.True, "notes " + n + " banner " + b + " nudges " + u);
            TestContext.WriteLine(cases + " cases, rail stepped aside for the notes in " + hidden);
            Assert.That(hidden, Is.LessThan(cases / 50), "the rail stays up almost everywhere");
            Assert.That(fails, Is.Empty, fails.Count + " of " + cases + ":\n" + string.Join("\n", fails.GetRange(0, Math.Min(40, fails.Count))));
        }

        /// <summary>Landscape web panels: every CSS size, as the scaled panel it gets, and the panel 1:1.</summary>
        static List<float[]> WidePanels()
        {
            // { css w, css h, panel w, panel h }
            var wide = new List<float[]>();
            foreach (var s in new[] { new[] { 960f, 540f }, new[] { 1024f, 768f }, new[] { 1280f, 720f }, new[] { 1280f, 800f }, new[] { 1366f, 768f }, new[] { 1440f, 900f }, new[] { 1920f, 1080f }, new[] { 2560f, 1440f } })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float k = WebLandscapeType.PanelScale(s[0], s[1], rw, rh, m);
                wide.Add(new[] { s[0], s[1], (float)Math.Round(s[0] / k), (float)Math.Round(s[1] / k) });
                wide.Add(new[] { s[0], s[1], s[0], s[1] });
                // The device tests' panel at 1 unit per point (FlightCameraPickerDeviceTests: 1280x800 is 1214x759 at 1x).
                wide.Add(new[] { (float)Math.Round(s[0] / k), (float)Math.Round(s[1] / k), (float)Math.Round(s[0] / k), (float)Math.Round(s[1] / k) });
            }
            return wide;
        }

        /// <summary>
        /// #332 CI (job 119437176077), 1280x800 web (panel 1214x759), notes open: ascent-plot (8,80 264x435) ran into
        /// ascent-help (0,467 1214x96). The grid had no landscape rail case, and the HUD did not do what the model said:
        /// PlaceWide put the notes in the dock or the left rail under the plot, but AscentHud.PlaceInfoSheet then re-pinned
        /// them as the portrait sheet, full width over the dock, and the landscape rail was never refitted. Now PlaceWide always puts them in the left rail and the HUD keeps
        /// that box and seats the rail with SeatRail, the call made here, over every visible stack element, as
        /// AscentHud.LayoutWideAt lays out the frame (face points = panel px / upp, the dock row from DockRowPx).
        /// </summary>
        [Test]
        public void LandscapeRailEndsAboveEveryVisibleStackElementWithNotesOpenOrClosed()
        {
            var fails = new List<string>();
            int cases = 0, open = 0;
            foreach (var p in WidePanels())
            foreach (var mode in Modes)
            {
                cases++;
                bool notes = mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes;
                bool prompt = mode == FlightHudMode.Prompt || mode == FlightHudMode.PromptNotes;
                if (notes) open++;
                float upp = p[2] / p[0];
                float rowPx = (float)Math.Ceiling(Math.Max(FlightHudLayout.ButtonPt, FlightHudLayout.InfoPt) * upp - 0.01f);
                var f = FlightHudLayout.PlaceWide(p[2] / upp, p[3] / upp, 0f, 0f, 0f, 0f, FlightHudLayout.WideMode(prompt, notes), rowPx / upp, FlightHudLayout.ClockMinWidth());
                HudBox Sc(HudBox b) => FlightHudLayout.ToPanel(b, upp, upp);
                string at = p[0] + "x" + p[1] + " (panel " + p[2] + "x" + p[3] + ") " + mode;
                if (notes && !f.Help.Live) fails.Add(at + ": no notes box");
                var stack = new List<(string, HudBox)>
                {
                    ("follow", Sc(f.Follow)), ("camera", Sc(f.Camera)), ("pause", Sc(f.Pause)), ("dock", Sc(f.Dock)),
                    ("notes", Sc(f.Help)), ("quote", Sc(f.Quote)), ("timer", Sc(f.Timer)),
                    ("alternate", Sc(f.Alternate)), ("nudge-", Sc(f.NudgeDown)), ("nudge+", Sc(f.NudgeUp)),
                };
                var boxes = new List<HudBox>();
                foreach (var (_, b) in stack) if (b.Live) boxes.Add(b);
                float gap = (float)Math.Round(FlightHudLayout.GapPt * upp);
                var plot = Sc(f.Plot);
                var fit = FlightHudLayout.SeatRail(plot, boxes, gap);
                if (!FlightHudLayout.RailShows(fit, upp)) { fails.Add(at + ": rail hidden " + Fmt(fit)); continue; }
                if (!Whole(fit.X) || !Whole(fit.Y) || !Whole(fit.W) || !Whole(fit.H)) fails.Add(at + ": not whole pixels " + Fmt(fit));
                if (fit.Y < plot.Y - 1e-3f || fit.Bottom > plot.Bottom + 1e-3f) fails.Add(at + ": rail grew " + Fmt(fit) + " from " + Fmt(plot));
                // Independent of SeatRail: one gap above every visible element in the rail's columns.
                foreach (var (name, b) in stack)
                {
                    if (!b.Live) continue;
                    bool sameColumns = b.X < fit.Right - 0.5f && b.Right > fit.X + 0.5f;
                    if (sameColumns && b.Y > plot.Y && fit.Bottom > b.Y - gap + 1e-3f)
                        fails.Add(at + ": rail bottom " + fit.Bottom + " not one gap above " + name + " " + Fmt(b));
                    if (Hit(fit, b)) fails.Add(at + ": rail " + Fmt(fit) + " overlaps " + name + " " + Fmt(b));
                }
                // The notes box itself (the box the HUD now keeps) covers no control and stays on the panel.
                var help = Sc(f.Help);
                if (help.Live)
                {
                    foreach (var (name, b) in stack)
                        if (name != "notes" && name != "dock" && b.Live && Hit(help, b)) fails.Add(at + ": notes " + Fmt(help) + " overlap " + name + " " + Fmt(b));
                    foreach (var (name, b) in new[] { ("health", Sc(f.Health)), ("telemetry", Sc(f.Telemetry)), ("stages", Sc(f.Stages)) })
                        if (b.Live && Hit(help, b)) fails.Add(at + ": notes overlap " + name);
                    if (help.X < -0.5f || help.Right > p[2] + 0.5f || help.Bottom > p[3] + 0.5f) fails.Add(at + ": notes off the panel " + Fmt(help));
                    if (help.H > 96.5f * upp) fails.Add(at + ": notes taller than the 96 pt sheet");
                }
            }
            Assert.That(open, Is.EqualTo(cases / 2), "every landscape panel ran with the notes open and closed");
            Assert.That(fails, Is.Empty, fails.Count + " of " + cases + ":\n" + string.Join("\n", fails.GetRange(0, Math.Min(40, fails.Count))));
        }

        [Test]
        public void HealthDetailNeverCoversTheTabsOrTheStripAtAnySize()
        {
            var fails = new List<string>();
            float[] popH = { 24f, 40f, 64f, 120f };
            void Check(string at, HudBox health, HudBox telemetry, float screenW, float upp)
            {
                float gap = (float)Math.Round(FlightHudLayout.GapPt * upp);
                var at0 = FlightHudLayout.HealthPopover(health, telemetry, gap,
                    (float)Math.Round(FlightHudLayout.PopoverMinPt * upp), (float)Math.Round(FlightHudLayout.PopoverMaxPt * upp));
                if (!Whole(at0.X) || !Whole(at0.Y) || !Whole(at0.W)) fails.Add(at + ": not whole pixels " + Fmt(at0));
                if (at0.X < -1e-3f || at0.Right > screenW + 1e-3f) fails.Add(at + ": off screen " + Fmt(at0));
                if (at0.W < FlightHudLayout.PopoverMinPt * upp - 1f && at0.W < health.W - 1f) fails.Add(at + ": too narrow " + Fmt(at0));
                foreach (float ph in popH)
                {
                    var box = new HudBox { X = at0.X, Y = at0.Y, W = at0.W, H = ph * upp };
                    if (Hit(box, health)) fails.Add(at + " h" + ph + ": covers the health tabs " + Fmt(box) + " / " + Fmt(health));
                    if (Hit(box, telemetry)) fails.Add(at + " h" + ph + ": covers the readouts " + Fmt(box) + " / " + Fmt(telemetry));
                }
            }
            foreach (float w in Widths)
            foreach (float h in Heights)
            {
                if (h <= w) continue;
                foreach (var pad in Pads)
                foreach (var mode in Modes)
                foreach (float upp in Scales)
                {
                    var f = FlightHudLayout.Place(w - 2f * pad[2], h - pad[0] - pad[1], 0f, 0f, 0f, 0f, mode);
                    Check(w + "x" + h + " " + mode + " @" + upp, Px(f.Health, upp, 0, 0), Px(f.Telemetry, upp, 0, 0), w * upp, upp);
                }
            }
            // Landscape web panels (the scaled panel each CSS size gets) and the CI case, panel 1214 x 759.
            var wide = new List<float[]> { new[] { 1214f, 759f } };
            foreach (var s in new[] { new[] { 960f, 540f }, new[] { 1024f, 768f }, new[] { 1280f, 720f }, new[] { 1280f, 800f }, new[] { 1366f, 768f }, new[] { 1440f, 900f }, new[] { 1920f, 1080f }, new[] { 2560f, 1440f } })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float k = WebLandscapeType.PanelScale(s[0], s[1], rw, rh, m);
                wide.Add(new[] { (float)Math.Round(s[0] / k), (float)Math.Round(s[1] / k) });
                wide.Add(s);
            }
            foreach (var p in wide)
            foreach (var mode in Modes)
            {
                var f = FlightHudLayout.PlaceWide(p[0], p[1], 0f, 0f, 0f, 0f, mode);
                Check("wide " + p[0] + "x" + p[1] + " " + mode, f.Health, f.Telemetry, p[0], 1f);
            }
            Assert.That(fails, Is.Empty, fails.Count + ":\n" + string.Join("\n", fails.GetRange(0, Math.Min(40, fails.Count))));
        }

        // Left / right safe pads (points), whole and fractional: phones, the 24 px device-test pads, the "23.5"
        // CI read off a 24 px pad, odd web insets, and one-sided notches in both directions.
        static readonly float[][] SidePads =
        {
            new[] { 0f, 0f }, new[] { 24f, 24f }, new[] { 23.5f, 23.5f }, new[] { 20.25f, 20.25f }, new[] { 0.5f, 0.5f },
            new[] { 11.75f, 30.6f }, new[] { 47.333f, 0f }, new[] { 0f, 59f }, new[] { 34f, 34f }, new[] { 62f, 62f },
        };

        /// <summary>
        /// #332 CI (job 119410329556), 335x720 with 24 px pads, cruise: flight-pause (248,495 76x44) ended at 324
        /// against a 311.5 safe edge. FlightChrome.StyleTools' row, emulated pixel for pixel: the padded content
        /// box rounded inward, less the side margins, slots from FlightHudLayout.ToolSlots floored to whole pixels,
        /// laid out space-between. Every button is inside the padded safe box, a 44 pt target, a real gap from its
        /// neighbour, and wide enough for its caption by BudgetWidth, at every width, pad and scale.
        /// </summary>
        [Test]
        public void ControlRowStaysInsideThePaddedSafeBoxEverywhere()
        {
            var fails = new List<string>();
            int cases = 0, shortRows = 0;
            var widths = new List<float>(Widths) { 300f, 310f, 1024f, 1180f };
            foreach (float w in widths)
            foreach (var pad in SidePads)
            foreach (float upp in Scales)
            {
                cases++;
                float panel = w * upp, pl = pad[0] * upp, pr = pad[1] * upp;
                // The safe box, exactly (no slack): nothing may cross a pad, even by a fraction of a pixel.
                float safeL = pl, safeR = panel - pr;
                float l = (float)Math.Ceiling(pl - 0.001f), r = (float)Math.Floor(panel - pr + 0.001f);
                float side = (float)Math.Round(FlightHudLayout.SideMarginPt * upp);
                float rowL = l + side, rowR = r - side, rowPx = rowR - rowL;
                float contentPt = (panel - pl - pr) / upp;
                float leftPt = (rowL - pl) / upp;
                var fit = FlightHudLayout.ToolSlots(contentPt, leftPt, leftPt + rowPx / upp);
                if (fit.Short) shortRows++;
                // The short captions are the last resort: never on a real phone row (320 pt up, pads to 24 pt).
                if (fit.Short && w >= 320f && pad[0] <= 24f && pad[1] <= 24f) fails.Add(w + "pt pads " + pad[0] + "/" + pad[1] + " x" + upp + " fell back to the short captions");
                float fw = (float)Math.Floor(fit.FollowW * upp + 0.001f);
                float cw = (float)Math.Floor(fit.CameraW * upp + 0.001f);
                float pw = (float)Math.Floor(fit.PauseW * upp + 0.001f);
                float spare = (rowPx - fw - cw - pw) / 2f;
                var follow = new HudBox { X = rowL, Y = 0f, W = fw, H = 44f * upp };
                var cam = new HudBox { X = rowL + fw + spare, Y = 0f, W = cw, H = 44f * upp };
                var pause = new HudBox { X = rowR - pw, Y = 0f, W = pw, H = 44f * upp };
                string where = w + "pt pads " + pad[0] + "/" + pad[1] + " x" + upp + " font " + fit.FontPt + (fit.Short ? " short" : "");
                if (!Whole(follow.X) || !Whole(pause.X) || !Whole(pause.Right)) fails.Add(where + " row not on whole pixels");
                foreach (var b in new[] { ("follow", follow), ("cam", cam), ("pause", pause) })
                {
                    if (b.Item2.X < safeL - 1e-3f || b.Item2.Right > safeR + 1e-3f)
                        fails.Add(where + " " + b.Item1 + " " + Fmt(b.Item2) + " outside the safe box " + safeL + ".." + safeR);
                    if (b.Item2.W < 44f * upp - 1e-3f) fails.Add(where + " " + b.Item1 + " under 44 pt: " + b.Item2.W);
                }
                float gap = FlightHudLayout.GapPt * upp - 1e-3f;
                if (cam.X - follow.Right < gap || pause.X - cam.Right < gap) fails.Add(where + " no gap: " + Fmt(follow) + Fmt(cam) + Fmt(pause));
                // Captions (the set the row will show) fit their slot by BudgetWidth, inside FlightChrome's 2 pt side
                // paddings and the Following plate's teal edge (UI Toolkit sizes are border-box).
                float room = (2f * 2f + 2f * CountdownStack.FollowEdgePt) * upp;
                var labels = new List<(string, float)>
                {
                    (FlightView.ShortToolCaption(FlightView.FollowLabel, fit.Short), fw),
                    (FlightView.ShortToolCaption(FlightView.FollowingLabel, fit.Short), fw),
                    (FlightHudLayout.PauseCaption, pw),
                };
                foreach (var m in FlightView.Cycle) labels.Add((FlightView.ShortToolCaption(FlightView.Caption(m), fit.Short), cw));
                foreach (var lb in labels)
                    if (FlightHudLayout.BudgetWidth(lb.Item1, fit.FontPt) * upp + room > lb.Item2 + 1e-3f)
                        fails.Add(where + " '" + lb.Item1 + "' does not fit " + lb.Item2 + " px");
                if (fit.FontPt < FlightHudLayout.ToolMinFontPt - 1e-3f) fails.Add(where + " caption under " + FlightHudLayout.ToolMinFontPt + " pt");
                // No side pad, 360 pt and up: Follow and Pause keep clear of the rocket, as the frame
                // (FlightHudLayoutTests) has them. Narrower, the full captions win over the 0.35 / 0.65 rule.
                if (pad[0] == 0f && pad[1] == 0f && w >= 360f && w < 700f)
                {
                    if (follow.Right > panel * 0.35f + 0.6f * upp) fails.Add(where + " follow covers the rocket " + Fmt(follow));
                    if (pause.X < panel * 0.65f - 0.6f * upp) fails.Add(where + " pause covers the rocket " + Fmt(pause));
                }

                // The portrait frame's own row (the content box, no insets) agrees: inside, gapped, and the same slots.
                var f = FlightHudLayout.Place(contentPt, 800f, 0f, 0f, 0f, 0f, FlightHudMode.Cruise, 0f, true);
                if (f.Follow.X < -0.01f || f.Pause.Right > contentPt + 0.01f || f.Camera.X < f.Follow.Right + FlightHudLayout.GapPt - 0.01f
                    || f.Pause.X < f.Camera.Right + FlightHudLayout.GapPt - 0.01f)
                    fails.Add(where + " frame row " + Fmt(f.Follow) + Fmt(f.Camera) + Fmt(f.Pause) + " in " + contentPt);
            }
            Assert.That(fails, Is.Empty, fails.Count + " of " + cases + ":\n" + string.Join("\n", fails.GetRange(0, Math.Min(40, fails.Count))));
            TestContext.WriteLine("control row: " + cases + " rows, " + shortRows + " on the short captions (deep pads or under 320 pt)");
        }

        static bool Hit(HudBox a, HudBox b) =>
            a.X < b.Right - 0.5f && a.Right > b.X + 0.5f && a.Y < b.Bottom - 0.5f && a.Bottom > b.Y + 0.5f;

        static string Fmt(HudBox b) => "(" + b.X + "," + b.Y + " " + b.W + "x" + b.H + ")";
    }
}
