// Portrait flight HUD: SE, Pro Max, and iPad. Nothing overlaps, the plot and the stages panel
// stay translucent and above the stack, and the dock stays below it.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightHudLayoutTests
    {
        static readonly FlightHudMode[] Modes =
        {
            FlightHudMode.Cruise, FlightHudMode.Notes, FlightHudMode.Prompt, FlightHudMode.PromptNotes,
        };

        [Test]
        public void DevicesAreThePortraitSet()
        {
            var seen = new HashSet<string>();
            foreach (var d in FlightHudLayout.PortraitDevices())
            {
                Assert.That(d.H, Is.GreaterThanOrEqualTo(d.W), d.Name + " is portrait");
                seen.Add(d.Name);
            }
            Assert.That(seen, Is.EquivalentTo(new[] { "SE", "Pro Max", "iPad" }));
        }

        [Test]
        public void VerdictFitsTheStagesCard_RawEventIdsDoNot()
        {
            string[] verdicts = { "On track", "On the count", "Heating hard", "Speed is there", "Short on speed", "Climbing", "Vehicle lost", "Skip count" };
            foreach (var d in FlightHudLayout.PortraitDevices())
            {
                var f = FlightHudLayout.Place(d, FlightHudMode.Cruise);
                float inner = f.Stages.W - 8f;
                foreach (var v in verdicts)
                    Assert.That(FlightHudLayout.TextWidth(v, 12f), Is.LessThanOrEqualTo(inner + 0.5f), d.Name + " " + v);
                if (d.Name == "SE")
                {
                    Assert.That(FlightHudLayout.TextWidth("tl.asc.maxq.short T+0:42", 11f), Is.GreaterThan(f.Stages.W), d.Name);
                    Assert.That(FlightHudLayout.TextWidth("tl.ign.release.short", 11f), Is.GreaterThan(inner), d.Name);
                }
            }
        }

        [Test]
        public void PlotAndStagesAreSmallAndTranslucent()
        {
            Assert.That(FlightHudLayout.PlotPt, Is.LessThanOrEqualTo(80f));
            Assert.That(FlightHudLayout.PlotPt, Is.GreaterThan(48f));
            Assert.That(FlightHudLayout.StagesAlpha, Is.InRange(0.16f, 0.42f));
            Assert.That(FlightHudLayout.TrajectoryAlpha, Is.InRange(0f, 0.18f));
            Assert.That(FlightHudLayout.CardFraction, Is.LessThan(0.40f));
        }

        [Test]
        public void ClockFitsItsBand_AndPromptButtonsStayOnScreen()
        {
            var phones = new[]
            {
                new FlightHudLayout.Device("narrow", 335f, 700f, 20f, 0f, 0f, 0f),
                new FlightHudLayout.Device("SE", 375f, 667f, 20f, 0f, 0f, 0f),
                new FlightHudLayout.Device("Pro Max", 440f, 956f, 62f, 34f, 0f, 0f),
                new FlightHudLayout.Device("iPad", 1032f, 1376f, 24f, 20f, 0f, 0f),
            };
            foreach (var d in phones)
            {
                var f = FlightHudLayout.Place(d, FlightHudMode.Prompt);
                AssertClock(f, d.Name);
                Assert.That(f.Primary.Bottom, Is.LessThanOrEqualTo(d.H - d.SafeBottom + 0.6f), d.Name + " throttle");
                Assert.That(f.NudgeDown.Bottom, Is.LessThanOrEqualTo(d.H - d.SafeBottom + 0.6f), d.Name + " nudge");
                Assert.That(f.NudgeUp.Bottom, Is.LessThanOrEqualTo(d.H - d.SafeBottom + 0.6f), d.Name + " nudge");
                Assert.That(f.NudgeDown.H, Is.GreaterThanOrEqualTo(44f), d.Name);
                Assert.That(f.Clock.Overlaps(f.Cells[0]), Is.False, d.Name + " clock/ALT");
            }

            var wide = FlightHudLayout.PlaceWide(1280f, 800f, 0f, 0f, 0f, 0f, FlightHudMode.Prompt);
            AssertClock(wide, "1280x800");
            Assert.That(wide.Clock.Overlaps(wide.Cells[0]), Is.False, "web clock/ALT");
            Assert.That(wide.Primary.Bottom, Is.LessThanOrEqualTo(wide.Dock.Bottom + 0.6f), "web throttle inside dock");
            Assert.That(wide.NudgeDown.Live, Is.True, "web nudge");
            Assert.That(wide.NudgeUp.Bottom, Is.LessThanOrEqualTo(800f + 0.6f), "web nudge on screen");
            Assert.That(wide.Plot.H, Is.GreaterThan(48f), "trajectory rail");
        }

        // #381 iPad frames: the T+ chip read tiny next to the header. The face was sized from the content-sized label's
        // own width, so it chased its box down to the floor; it now comes from the header row (HeaderClockFontPt).
        [TestCase("iPad 13in", 1032f)]
        [TestCase("6.9in", 440f)]
        [TestCase("6.3in", 402f)]
        [TestCase("390x844", 390f)]
        public void HeaderClockFaceComesFromTheRowNotTheLabel(string device, float screenPt)
        {
            float row = screenPt - 2f * 16f;                        // header row inside the HUD side padding
            float font = FlightHudLayout.HeaderClockFontPt(row, FlightHudLayout.StageChipPt);
            Assert.That(font, Is.EqualTo(28f), device + ": full header face");
            Assert.That(font, Is.LessThanOrEqualTo(FlightHudLayout.HeaderPt - 4f), device + ": fits the 36 pt header");
            Assert.That(font, Is.GreaterThanOrEqualTo(2f * FlightHudLayout.ValueFontPt), device + ": reads above the stat strip");
            float box = row - FlightHudLayout.StageChipPt - FlightHudLayout.GapPt;
            Assert.That(FlightHudLayout.TextWidth("T-00:00.0", font), Is.LessThanOrEqualTo(box - 1.5f), device);
            Assert.That(FlightHudLayout.TextWidth("T+ 1234.5", font), Is.LessThanOrEqualTo(box - 1.5f), device);
            // A wider stage chip than the model's takes its own width off the row.
            Assert.That(FlightHudLayout.HeaderClockFontPt(row, row - 60f), Is.LessThan(28f), device + ": crowded row");
            // The old feedback, modelled: face from the label's own width (its text plus 4 pt pads) runs to the floor.
            float chase = 28f;
            for (int i = 0; i < 200; i++) chase = FlightHudLayout.ClockFontPt(FlightHudLayout.TextWidth("T+ 60.0", chase) + 8f, FlightHudLayout.HeaderPt);
            Assert.That(chase, Is.LessThan(font * 0.6f), device + ": self-sized face collapses (the bug)");
        }

        static void AssertClock(FlightHudFrame f, string where)
        {
            float font = FlightHudLayout.ClockFontPt(f.Clock.W, f.Clock.H);
            Assert.That(font, Is.LessThanOrEqualTo(f.Clock.H - 3f), where + " clock face");
            Assert.That(FlightHudLayout.TextWidth("T+ 00.0", font), Is.LessThanOrEqualTo(f.Clock.W - 1.5f), where);
            Assert.That(FlightHudLayout.TextWidth("T+ 11.2", font), Is.LessThanOrEqualTo(f.Clock.W - 1.5f), where);
        }

        [Test]
        public void BandsDoNotOverlapOnSeProMaxOrIpad()
        {
            foreach (var d in FlightHudLayout.PortraitDevices())
            foreach (var mode in Modes)
            {
                var f = FlightHudLayout.Place(d, mode);
                string where = d.Name + " " + d.SafeTop + "/" + d.SafeBottom + " " + mode;
                AssertInside(f, d, where);
                AssertNoOverlap(f, where);
                AssertStackClear(f, d, where);
                AssertTelemetryFits(f, where);
                AssertSameRow(f.Clock, f.Stage, where + " clock/stage");
                Assert.That(f.Cells.Length, Is.EqualTo(4), where);
                for (int i = 1; i < 4; i++)
                    AssertSameRow(f.Cells[0], f.Cells[i], where + " cell " + i);
                Assert.That(f.Info.W, Is.GreaterThanOrEqualTo(44f), where + " info");
                Assert.That(f.Info.H, Is.GreaterThanOrEqualTo(44f), where);
                Assert.That(f.Assist.H, Is.GreaterThanOrEqualTo(44f), where + " assist");
                Assert.That(f.NudgeDown.H, Is.GreaterThanOrEqualTo(44f), where + " nudge");
                Assert.That(f.NudgeUp.W, Is.GreaterThanOrEqualTo(44f), where);
                Assert.That(f.Follow.H, Is.GreaterThanOrEqualTo(44f), where + " follow");
                Assert.That(f.Pause.H, Is.GreaterThanOrEqualTo(44f), where + " pause");
                if (mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes)
                    Assert.That(f.Help.Live, Is.True, where + " notes");
                else
                    Assert.That(f.Help.Live, Is.False, where + " notes stay collapsed");
                if (mode == FlightHudMode.Prompt)
                    Assert.That(f.Quote.Live && f.Timer.Live && f.Primary.Live, Is.True, where);
                if (mode == FlightHudMode.PromptNotes)
                    Assert.That(f.Quote.Live, Is.False, where + " the call sits in the notes");
            }
        }

        /// <summary>
        /// #322 CI 1280x800 engine-out: flight-info (44 x 44 px) stuck out of ascent-medal-row (42 px: 44 CSS pt on a
        /// 0.948 panel). With the rows at 44 px in CSS points every dock row holds it, inside the dock, and the dock
        /// still clears the tools row and the rails.
        /// </summary>
        [Test]
        public void WideDockRowsHoldA44PxButtonOnAScaledPanel()
        {
            foreach (var wh in CameraWide)
            foreach (var mode in Modes)
            {
                float rowPt = 44f * 1280f / 1214f;
                var f = FlightHudLayout.PlaceWide(wh[0], wh[1], 0f, 0f, 0f, 0f, mode, rowPt);
                string at = wh[0] + "x" + wh[1] + " " + mode;
                Assert.That(f.Medal.H, Is.GreaterThanOrEqualTo(rowPt - 0.01f), at + " medal row");
                Assert.That(f.Info.H, Is.GreaterThanOrEqualTo(rowPt - 0.01f), at + " info");
                var rows = new List<(string, HudBox)> { ("medal", f.Medal), ("info", f.Info), ("primary", f.Primary), ("skip", f.Skip), ("assist", f.Assist) };
                if (f.NudgeDown.Live && f.NudgeDown.Y >= f.Dock.Y) rows.Add(("nudge-down", f.NudgeDown));
                if (f.Timer.Live && f.Timer.Y >= f.Dock.Y) rows.Add(("timer", f.Timer));
                if (f.Quote.Live && f.Quote.Y >= f.Dock.Y) rows.Add(("quote", f.Quote));
                foreach (var (name, b) in rows)
                {
                    if (!b.Live) continue;
                    if (name != "timer" && name != "quote") Assert.That(b.H, Is.GreaterThanOrEqualTo(rowPt - 0.01f), at + " " + name);
                    Assert.That(b.Y >= f.Dock.Y - 0.01f && b.Bottom <= f.Dock.Bottom + 0.6f, Is.True, at + " " + name + " inside the dock " + Show(b) + " " + Show(f.Dock));
                }
                for (int i = 0; i < rows.Count; i++)
                    for (int j = i + 1; j < rows.Count; j++)
                        if (rows[i].Item2.Live && rows[j].Item2.Live)
                            Assert.That(rows[i].Item2.Overlaps(rows[j].Item2), Is.False, at + " " + rows[i].Item1 + "/" + rows[j].Item1);
                foreach (var t in new[] { f.Follow, f.Camera, f.Pause, f.Plot, f.Stages })
                    Assert.That(t.Overlaps(f.Dock), Is.False, at + " dock clear of the tools and rails");
            }
        }

        /// <summary>The web panel a CSS viewport gets (WebLandscapeType: ScaleWithScreenSize at the landscape reference).</summary>
        static void DevicePanel(float cssW, float cssH, out float pw, out float ph)
        {
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float s = WebLandscapeType.PanelScale(cssW, cssH, rw, rh, m);
            pw = (float)Math.Round(cssW / s);
            ph = (float)Math.Round(cssH / s);
        }

        static readonly int[][] LandscapeDeviceSizes =
        {
            new[] { 960, 540 }, new[] { 1280, 720 }, new[] { 1280, 800 }, new[] { 1366, 768 }, new[] { 1440, 900 },
            new[] { 1920, 1080 }, new[] { 2560, 1440 },
        };

        /// <summary>
        /// #322 CI 1920x1080: h-t "T+ 1:12.4" needed 64 x 26 px in a 64 x 22 box: the frame was laid out in CSS points on
        /// a 0.667 panel while the faces are panel pixels. On the panel each size really gets, the clock holds every
        /// reading at its fitted face (widths budgeted with MeasureFactor), the engine-out quote is one line without a
        /// cut at 10 pt or more, and the dock rows are 44 pt.
        /// </summary>
        [Test]
        public void LandscapeClockQuoteAndRowsFitOnEveryDevicePanel()
        {
            foreach (var s in LandscapeDeviceSizes)
            foreach (var mode in Modes)
            {
                DevicePanel(s[0], s[1], out float pw, out float ph);
                var f = FlightHudLayout.PlaceWide(pw, ph, 0f, 0f, 0f, 0f, mode);
                string at = s[0] + "x" + s[1] + " (panel " + pw + "x" + ph + ") " + mode;
                float face = FlightHudLayout.ClockFit(f.Clock.W, f.Telemetry.H);
                Assert.That(FlightHudLayout.ClockFits(f.Clock.W, f.Telemetry.H, face), Is.True, at + " clock " + face);
                Assert.That(f.Clock.W, Is.GreaterThanOrEqualTo(FlightHudLayout.ClockMinWidth() - 0.01f), at + " clock box");
                Assert.That(f.Clock.Overlaps(f.Cells[0]), Is.False, at + " clock/ALT");
                if (f.Quote.Live)
                {
                    var q = FlightHudLayout.QuoteLine("Engine 1 pressure drop. Shut it down?", f.Quote.W, FlightHudLayout.QuoteFontPt, FlightHudLayout.QuoteMinFontPt);
                    Assert.That(q.Cut, Is.False, at + " quote whole");
                    Assert.That(q.FontPt * FlightHudLayout.LineHeight, Is.LessThanOrEqualTo(f.Quote.H + 0.01f), at + " quote one line in its row");
                }
                foreach (var b in new[] { f.Medal, f.Info, f.Assist, f.Primary, f.Follow, f.Pause, f.Camera })
                    if (b.Live) Assert.That(b.H, Is.GreaterThanOrEqualTo(FlightHudLayout.ButtonPt - 0.01f), at + " 44 pt row");
                // Whole panel pixels: no two live regions or dock buttons touch after rounding.
                var all = new List<(string, HudBox)>
                {
                    ("clock", f.Clock), ("stage", f.Stage), ("health", f.Health), ("plot", f.Plot), ("stages", f.Stages),
                    ("follow", f.Follow), ("camera", f.Camera), ("pause", f.Pause), ("picker", f.CameraPicker),
                    ("info", f.Info), ("medal", f.Medal), ("help", f.Help), ("quote", f.Quote), ("timer", f.Timer),
                    ("assist", f.Assist), ("primary", f.Primary), ("skip", f.Skip), ("alternate", f.Alternate),
                    ("nudge-down", f.NudgeDown), ("nudge-up", f.NudgeUp),
                };
                for (int i = 0; i < f.Cells.Length; i++) all.Add(("cell" + i, f.Cells[i]));
                HudBox Px(HudBox x) => new HudBox { X = (float)Math.Round(x.X), Y = (float)Math.Round(x.Y), W = (float)Math.Round(x.W), H = (float)Math.Round(x.H) };
                for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                    if (all[i].Item2.Live && all[j].Item2.Live)
                        Assert.That(Px(all[i].Item2).Overlaps(Px(all[j].Item2)), Is.False, at + ": " + all[i].Item1 + " " + Show(all[i].Item2) + " over " + all[j].Item1 + " " + Show(all[j].Item2));
            }
        }

        /// <summary>
        /// #322 CI SE cruise: ascent-actions (8,519 359x148) ran over flight-follow (3,514). AscentHud's flow spacer
        /// reserves FlightHudLayout.DockHeight for the open rows; the frame's dock is exactly that tall, so the tools
        /// row sits a whole GapPt above it. Every portrait size, every mode, the banner in its card and as the 64-char
        /// banner row, the nudge row open and closed; then the whole frame in whole pixels at every panel scale.
        /// </summary>
        [Test]
        public void PortraitDockToolsAndBannerNeverOverlapInWholePixels()
        {
            var fails = new List<string>();
            int failCount = 0, cases = 0;
            var widths = new List<int>();
            for (int w = 320; w <= 440; w += ExhaustivePickerSweep ? 1 : 15) widths.Add(w);
            widths.Add(820); widths.Add(1024); widths.Add(1032);
            foreach (int w in widths)
            for (int h = w > 700 ? 1180 : 568; h <= (w > 700 ? 1376 : 956); h += ExhaustivePickerSweep ? 4 : 64)
            foreach (var inset in (h >= 812 ? NotchedInsets : FlatInsets))
            foreach (var mode in Modes)
            foreach (bool nudges in NudgeRows)
            {
                float bannerRow = FlightHudLayout.BannerRowPt(FlightHudLayout.BannerReserve, w);
                foreach (float banner in bannerRow > 0f ? new[] { 0f, bannerRow } : new[] { 0f })
                {
                    cases++;
                    var f = FlightHudLayout.Place(w, h, inset[0], inset[1], inset[2], inset[3], mode, banner, nudges);
                    bool notes = mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes;
                    bool prompt = mode == FlightHudMode.Prompt || mode == FlightHudMode.PromptNotes;
                    string at = w + "x" + h + " inset " + inset[0] + "/" + inset[1] + " " + mode + " banner " + banner + " nudges " + nudges;
                    float spacer = FlightHudLayout.DockHeight(notes, prompt, FlightHudLayout.ButtonPt, nudges);
                    if (Math.Abs(f.Dock.H - spacer) > 0.01f) Fail(fails, ref failCount, at + ": dock " + f.Dock.H + " vs spacer " + spacer);
                    var tools = FlightHudLayout.ToolsRow(f);
                    if (tools.Bottom > f.Dock.Y - FlightHudLayout.GapPt + 0.01f) Fail(fails, ref failCount, at + ": tools " + Show(tools) + " into dock " + Show(f.Dock));
                    var named = new List<(string, HudBox)>
                    {
                        ("clock", f.Clock), ("stage", f.Stage), ("health", f.Health), ("telemetry", f.Telemetry), ("plot", f.Plot),
                        ("stages", f.Stages), ("dock", f.Dock), ("follow", f.Follow), ("camera", f.Camera), ("pause", f.Pause),
                        ("picker", f.CameraPicker),
                    };
                    var dockKids = new List<(string, HudBox)>
                    {
                        ("info", f.Info), ("medal", f.Medal), ("help", f.Help), ("quote", f.Quote), ("timer", f.Timer),
                        ("assist", f.Assist), ("primary", f.Primary), ("skip", f.Skip), ("alternate", f.Alternate),
                        ("nudge-down", f.NudgeDown), ("nudge-up", f.NudgeUp),
                    };
                    if (!nudges && (f.NudgeDown.Live || f.NudgeUp.Live || f.Alternate.Live)) Fail(fails, ref failCount, at + ": closed nudge row still placed");
                    foreach (var (n, b) in dockKids)
                        if (b.Live && (b.Y < f.Dock.Y - 0.01f || b.Bottom > f.Dock.Bottom + 0.01f)) Fail(fails, ref failCount, at + ": " + n + " leaves the dock");
                    foreach (float u in WholePixelScales)
                    {
                        // Whole pixels, the way AscentHud seats rows (Mathf.Round of top and height).
                        HudBox Px(HudBox b) => new HudBox { X = (float)Math.Round(b.X * u), Y = (float)Math.Round(b.Y * u), W = (float)Math.Round(b.W * u), H = (float)Math.Round(b.H * u) };
                        for (int i = 0; i < named.Count; i++)
                        for (int j = i + 1; j < named.Count; j++)
                        {
                            var a = named[i]; var b = named[j];
                            if (!a.Item2.Live || !b.Item2.Live) continue;
                            // The stages card is translucent and the trajectory rail line art: the picker may not cover
                            // them either (#304's sweep), so every pair is checked.
                            if (Px(a.Item2).Overlaps(Px(b.Item2))) Fail(fails, ref failCount, at + " x" + u + ": " + a.Item1 + " " + Show(a.Item2) + " over " + b.Item1 + " " + Show(b.Item2));
                        }
                        foreach (var c in f.Cells)
                            foreach (var (n, b) in named)
                                if (n != "telemetry" && b.Live && Px(c).Overlaps(Px(b))) Fail(fails, ref failCount, at + " x" + u + ": cell over " + n);
                        for (int i = 0; i < dockKids.Count; i++)
                        for (int j = i + 1; j < dockKids.Count; j++)
                        {
                            var a = dockKids[i]; var b = dockKids[j];
                            if (!a.Item2.Live || !b.Item2.Live) continue;
                            if (Px(a.Item2).Overlaps(Px(b.Item2))) Fail(fails, ref failCount, at + " x" + u + ": " + a.Item1 + " over " + b.Item1);
                        }
                    }
                    if (!FlightHudLayout.BannerFitsRow(banner > 0f ? FlightHudLayout.BannerReserve : FlightHudLayout.BannerReserve, f) && banner > 0f)
                        Fail(fails, ref failCount, at + ": the 64-char banner does not fit its row");
                }
            }
            TestContext.WriteLine("portrait dock/tools/banner sweep: " + cases + " frames");
            AssertNoFailures(fails, failCount, cases, "portrait dock/tools/banner sweep");
        }

        /// <summary>Panel scales the portrait HUD runs at (1x web, 2x / 3x phones, the 308/375 SE capture, 1.25 iPad web).</summary>
        static readonly float[] WholePixelScales = { 1f, 2f, 3f, 308f / 375f, 1.25f };

        // ---- Camera button and picker (CAMERA-PICKER brief) -------------------------------------------

        /// <summary>Portrait: 335 narrow, 390 and 430 iPhones, iPad. Full screen with insets, and the inset-free content box AscentHud lays out.</summary>
        static readonly FlightHudLayout.Device[] CameraPortraits =
        {
            new FlightHudLayout.Device("335x720", 335f, 720f, 20f, 0f, 0f, 0f),
            new FlightHudLayout.Device("335x667", 335f, 667f, 0f, 0f, 0f, 0f),
            new FlightHudLayout.Device("390x844", 390f, 844f, 47f, 34f, 0f, 0f),
            new FlightHudLayout.Device("430x932", 430f, 932f, 59f, 34f, 0f, 0f),
            new FlightHudLayout.Device("iPad 1032x1376", 1032f, 1376f, 24f, 20f, 0f, 0f),
            new FlightHudLayout.Device("iPad 820x1180", 820f, 1180f, 24f, 20f, 0f, 0f),
        };

        /// <summary>Landscape web, as the CSS viewport AscentHud reads and as the 1280x720 panel WebPanelScale makes of it.</summary>
        static readonly float[][] CameraWide =
        {
            new[] { 1280f, 720f }, new[] { 1280f, 800f }, new[] { 1366f, 768f }, new[] { 1920f, 1080f },
        };

        [Test]
        public void WideModeFollowsThePromptAndNotes()
        {
            Assert.That(FlightHudLayout.WideMode(false, false), Is.EqualTo(FlightHudMode.Cruise));
            Assert.That(FlightHudLayout.WideMode(true, false), Is.EqualTo(FlightHudMode.Prompt));
            Assert.That(FlightHudLayout.WideMode(false, true), Is.EqualTo(FlightHudMode.Notes));
            Assert.That(FlightHudLayout.WideMode(true, true), Is.EqualTo(FlightHudMode.PromptNotes));
        }

        /// <summary>
        /// 1280x800 live build: the tools row was laid for the cruise dock and a prompt then grew the dock under
        /// Follow and Pause. The prompt dock does reach the cruise row, so AscentHud re-pins on every mode change;
        /// each mode's own frame keeps Follow, the camera, Pause and the clock clear of its dock.
        /// </summary>
        [Test]
        public void EveryDockModeLiftsTheToolsRowAt1280x800()
        {
            {
                const float w = 1280f, h = 800f;
                var cruise = FlightHudLayout.PlaceWide(w, h, 0f, 0f, 0f, 0f, FlightHudMode.Cruise);
                var prompt = FlightHudLayout.PlaceWide(w, h, 0f, 0f, 0f, 0f, FlightHudMode.Prompt);
                Assert.That(cruise.Follow.Overlaps(prompt.Dock) || cruise.Pause.Overlaps(prompt.Dock), Is.True,
                    "a stale cruise row would sit under the prompt dock (the live bug), so the mode change must re-pin");
                foreach (var mode in Modes)
                {
                    var f = FlightHudLayout.PlaceWide(w, h, 0f, 0f, 0f, 0f, mode);
                    string where = "1280x800 " + mode;
                    foreach (var b in new[] { f.Follow, f.Camera, f.Pause })
                    {
                        Assert.That(b.Overlaps(f.Dock), Is.False, where + " tools row clears the dock");
                        Assert.That(b.Overlaps(f.Clock), Is.False, where + " tools row clears T+");
                        Assert.That(b.Bottom, Is.LessThanOrEqualTo(f.Dock.Y + 0.01f), where + " tools row sits above the dock");
                    }
                    Assert.That(f.Camera.Overlaps(f.Clock), Is.False, where + " T+ is not under the camera button");
                    Assert.That(f.Camera.X, Is.GreaterThan(f.Clock.Right), where + " camera is on the far side from T+");
                    foreach (var row in new[] { f.Primary, f.Skip, f.Assist, f.Alternate, f.NudgeDown, f.NudgeUp, f.Timer, f.Quote, f.Help })
                        if (row.Live)
                            foreach (var b in new[] { f.Follow, f.Camera, f.Pause })
                                Assert.That(b.Overlaps(row), Is.False, where + " prompt rows do not cover the tools row");
                }
            }
        }

        [Test]
        public void FollowCaptionsFitTheFollowButton()
        {
            foreach (var cap in new[] { FlightView.FollowLabel, FlightView.FollowingLabel })
                Assert.That(FlightView.LabelWidth(cap, 13f), Is.LessThanOrEqualTo(FlightHudLayout.FollowWidthPt - 4f), cap + " (FitTool pads 2 pt a side)");
        }

        [Test]
        public void CameraAndPickerClearTheHud_Portrait()
        {
            foreach (var d in CameraPortraits)
            foreach (var mode in Modes)
            {
                var f = FlightHudLayout.Place(d, mode);
                string where = d.Name + " " + mode;
                AssertCamera(f, d.W, d.H, d.SafeTop, d.SafeBottom, d.SafeLeft, d.SafeRight, where);
                Assert.That(f.Camera.Y, Is.EqualTo(f.Follow.Y).Within(0.01f), where + " camera rides the tools row");
                Assert.That(f.Camera.X, Is.GreaterThan(f.Follow.Right), where + " between Follow and Pause");
                Assert.That(f.Camera.Right, Is.LessThan(f.Pause.X), where);
                Assert.That(f.CameraPicker.Bottom, Is.LessThanOrEqualTo(f.Camera.Y + 0.5f), where + " picker opens above the row");
            }
        }

        [Test]
        public void CameraAndPickerClearTheHud_LandscapeWeb()
        {
            foreach (var size in CameraWide)
            foreach (var scaled in new[] { false, true })
            foreach (var mode in Modes)
            {
                float w = scaled ? 1280f : size[0];
                float h = scaled ? 1280f * size[1] / size[0] : size[1];
                var f = FlightHudLayout.PlaceWide(w, h, 0f, 0f, 0f, 0f, mode);
                string where = size[0] + "x" + size[1] + (scaled ? " panel " + w + "x" + h : " css") + " " + mode;
                AssertCamera(f, w, h, 0f, 0f, 0f, 0f, where);
                Assert.That(f.Camera.X > 1f || f.Camera.Y > 1f, Is.True, where + " camera is placed, not at (0,0)");
                Assert.That(f.Camera.Y, Is.EqualTo(f.Pause.Y).Within(0.01f), where + " beside Pause on the tools row");
                Assert.That(f.Camera.Y, Is.EqualTo(f.Follow.Y).Within(0.01f), where);
                var col = FlightHudLayout.RocketColumn(f);
                Assert.That(f.CameraPicker.X, Is.GreaterThanOrEqualTo(col.X - 0.5f), where + " picker opens toward the centre");
                Assert.That(f.CameraPicker.Right, Is.LessThanOrEqualTo(f.Camera.X + 0.5f), where);
                Assert.That(f.CameraPicker.H, Is.LessThanOrEqualTo(FlightHudLayout.ButtonPt + 2f * FlightHudLayout.PadPt + 0.5f), where + " one row of five on a wide screen");
            }
        }

        [Test]
        public void NarrowLandscapeRailStacksTheCameraAbovePause()
        {
            // 960x540: a rail too narrow for Cam and Pause side by side.
            var f = FlightHudLayout.PlaceWide(700f, 420f, 0f, 0f, 0f, 0f, FlightHudMode.Cruise);
            AssertCamera(f, 700f, 420f, 0f, 0f, 0f, 0f, "700x420");
        }

        /// <summary>
        /// CI 2026-10-19, 1280x800: Cam (946..1042) ran 1 px into Pause (1041). Follow, Cam and Pause keep a real
        /// GapPt between them at every landscape CSS width and in every mode, and still do after the CSS boxes
        /// are scaled to the panel (WebLandscapeType.PanelScale, as the device tests make the panel) and each
        /// edge is snapped to the pixel grid the way UI Toolkit rounds layout: no rounding seam closes the gap.
        /// </summary>
        [Test]
        public void ToolsRowKeepsARealGapAtEveryLandscapeWidth()
        {
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            var heights = new[] { 540f, 600f, 720f, 768f, 800f, 900f, 1080f, 1200f, 1440f };
            var modes = new[] { FlightHudMode.Cruise, FlightHudMode.Prompt, FlightHudMode.Notes, FlightHudMode.PromptNotes };
            int checkedRows = 0;
            for (int w = 960; w <= 2560; w++)
            {
                foreach (float h in heights)
                {
                    if (!(w > h)) continue;
                    float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                    float ux = (float)Math.Round(w / s) / w, uy = (float)Math.Round(h / s) / h;
                    foreach (var mode in modes)
                    {
                        var f = FlightHudLayout.PlaceWide(w, h, 0f, 0f, 0f, 0f, mode);
                        string where = w + "x" + h + " " + mode;
                        AssertToolGap(f.Follow, f.Camera, ux, uy, where + " Follow/Cam");
                        AssertToolGap(f.Camera, f.Pause, ux, uy, where + " Cam/Pause");
                        AssertToolGap(f.Follow, f.Pause, ux, uy, where + " Follow/Pause");
                        checkedRows++;
                    }
                }
            }
            Assert.That(checkedRows, Is.GreaterThan(10000));
        }

        [Test]
        public void ToolsRowKeepsARealGapAtEveryPortraitWidth()
        {
            var heights = new[] { 568f, 640f, 667f, 720f, 812f, 844f, 896f, 932f };
            for (int w = 320; w <= 440; w++)
                foreach (float h in heights)
                {
                    var f = FlightHudLayout.Place(w, h, 0f, 0f, 0f, 0f, FlightHudMode.Cruise);
                    string where = w + "x" + h;
                    AssertToolGap(f.Follow, f.Camera, 1f, 1f, where + " Follow/Cam");
                    AssertToolGap(f.Camera, f.Pause, 1f, 1f, where + " Cam/Pause");
                    Assert.That(f.Camera.W, Is.GreaterThanOrEqualTo(FlightView.WidestButton() - 0.01f), where + " caption room");
                }
        }

        /// <summary>Two tools on one row (or stacked) are at least GapPt apart in points, and at least 1 whole pixel apart after scaling and edge snapping.</summary>
        static void AssertToolGap(HudBox a, HudBox b, float ux, float uy, string where)
        {
            Assert.That(a.Live && b.Live, Is.True, where + " placed");
            bool sameRow = a.Y < b.Bottom && b.Y < a.Bottom;
            var (lo, hi) = sameRow ? (a.X <= b.X ? (a, b) : (b, a)) : (a.Y <= b.Y ? (a, b) : (b, a));
            float gapPt = sameRow ? hi.X - lo.Right : hi.Y - lo.Bottom;
            Assert.That(gapPt, Is.GreaterThanOrEqualTo(FlightHudLayout.GapPt - 0.01f), where + " gap in points");
            var pl = FlightHudLayout.ToPanel(lo, ux, uy);
            var ph = FlightHudLayout.ToPanel(hi, ux, uy);
            float loEdge = (float)Math.Round(sameRow ? pl.Right : pl.Bottom, MidpointRounding.AwayFromZero);
            float hiEdge = (float)Math.Round(sameRow ? ph.X : ph.Y, MidpointRounding.AwayFromZero);
            Assert.That(hiEdge - loEdge, Is.GreaterThanOrEqualTo(1f), where + " gap on the pixel grid (" + loEdge + " vs " + hiEdge + ")");
        }

        [Test]
        public void ButtonAndChipCaptionsFit()
        {
            Assert.IsTrue(FlightView.FitsWidth(335f));
            foreach (var mode in FlightView.Cycle)
            {
                string cap = FlightView.Caption(mode);
                Assert.That(cap, Does.StartWith("Cam: "), mode.ToString());
                Assert.That(FlightView.LabelWidth(cap, FlightView.ButtonFontPt), Is.LessThanOrEqualTo(FlightView.WidestButton() - 8f), cap);
                Assert.That(FlightView.LabelWidth(FlightView.Name(mode), FlightView.ChipFontPt), Is.LessThanOrEqualTo(FlightView.WidestChip() - 8f), mode.ToString());
                Assert.That(FlightView.Name(mode).Contains(" "), Is.False, "chips are one word, nothing to wrap mid-word");
            }
            Assert.That(FlightView.Picker, Is.EquivalentTo(FlightView.Cycle));
            Assert.That(FlightView.Picker[0], Is.EqualTo(FlightViewMode.Auto));
            foreach (var d in CameraPortraits)
            {
                var f = FlightHudLayout.Place(d, FlightHudMode.Cruise);
                Assert.That(f.Camera.W, Is.GreaterThanOrEqualTo(FlightView.WidestButton() - 0.01f), d.Name + " caption room");
            }
        }

        // Exhaustive picker sweeps run under dotnet only (unity/dotnet/Kestrel.Core.Tests.csproj defines
        // KH_DOTNET_TESTS; the Unity asmdef does not). Unity's EditMode runner is ~15x slower and has a 180 s
        // per-test timeout, so it runs a sampled grid: every size the device tests use, the edge row shifts,
        // every inset and every scale. Both paths share one loop with no per-case NUnit asserts or string
        // formatting: failures go into a list asserted once at the end.
#if KH_DOTNET_TESTS
        static readonly bool ExhaustivePickerSweep = true;
#else
        static readonly bool ExhaustivePickerSweep = false;
#endif

        /// <summary>Portrait sizes the device tests and fixtures use (FlightCameraPickerDeviceTests, CameraPortraits, PortraitDevices).</summary>
        static readonly int[][] PickerPortraitSamples =
        {
            new[] { 320, 568 }, new[] { 335, 720 }, new[] { 375, 667 }, new[] { 390, 844 },
            new[] { 393, 852 }, new[] { 402, 874 }, new[] { 414, 896 }, new[] { 430, 932 }, new[] { 440, 956 },
        };

        /// <summary>Landscape CSS sizes the device tests use, plus the narrow and wide ends.</summary>
        static readonly int[][] PickerLandscapeSamples =
        {
            new[] { 960, 540 }, new[] { 1280, 720 }, new[] { 1280, 800 }, new[] { 1366, 768 },
            new[] { 1440, 900 }, new[] { 1920, 1080 }, new[] { 2560, 1440 },
        };

        static readonly string[] OtherNames =
        {
            "Clock", "Stage", "Telemetry", "Cell0", "Cell1", "Cell2", "Cell3", "Health", "Plot", "Stages", "Dock", "Info",
            "Medal", "Help", "Quote", "Timer", "Follow", "Pause", "Primary", "Skip", "Assist", "Alternate", "NudgeDown",
            "NudgeUp", "Exit", "Camera", "Banner",
        };

        static void FillOthers(FlightHudFrame f, HudBox[] o)
        {
            o[0] = f.Clock; o[1] = f.Stage; o[2] = f.Telemetry;
            for (int i = 0; i < 4; i++) o[3 + i] = i < f.Cells.Length ? f.Cells[i] : default;
            o[7] = f.Health; o[8] = f.Plot; o[9] = f.Stages; o[10] = f.Dock; o[11] = f.Info; o[12] = f.Medal;
            o[13] = f.Help; o[14] = f.Quote; o[15] = f.Timer; o[16] = f.Follow; o[17] = f.Pause; o[18] = f.Primary;
            o[19] = f.Skip; o[20] = f.Assist; o[21] = f.Alternate; o[22] = f.NudgeDown; o[23] = f.NudgeUp; o[24] = f.Exit;
            o[25] = f.Camera; o[26] = f.Banner;
        }

        const int MaxReported = 40;

        static void Fail(List<string> fails, ref int count, string what)
        {
            count++;
            if (fails.Count < MaxReported) fails.Add(what);
        }

        static void AssertNoFailures(List<string> fails, int failCount, int cases, string sweep)
        {
            Assert.That(cases, Is.GreaterThan(0), sweep + " ran");
            if (failCount > 0)
                Assert.Fail(sweep + ": " + failCount + " failure(s) in " + cases + " cases:\n  " + string.Join("\n  ", fails));
        }

        /// <summary>The dock's last row open (Engineer, a call with an alternate) and closed (Simple cruise).</summary>
        static readonly bool[] NudgeRows = { true, false };

        static readonly float[] PortraitShiftsAll = { -6f, -4f, -1.5f, -1f, 0f, 1f, 4f };
        static readonly float[] PortraitShiftsEdge = { -6f, 0f, 4f };
        static readonly float[] PanelScales = { 1f, 308f / 375f, 1.25f };
        // Home-button phones (568/667 tall) have a 20 pt status bar; notch insets only from 812 pt up.
        static readonly float[][] FlatInsets = { new[] { 0f, 0f, 0f, 0f }, new[] { 20f, 0f, 0f, 0f } };
        static readonly float[][] NotchedInsets = { new[] { 0f, 0f, 0f, 0f }, new[] { 20f, 0f, 0f, 0f }, new[] { 47f, 34f, 0f, 0f }, new[] { 62f, 34f, 0f, 0f } };

        /// <summary>
        /// One portrait size: every inset, every mode, every row shift. Returns the cases checked. The flex row
        /// lands a few points off the frame's row (tools margin vs GapPt, button margins), so the row is shifted.
        /// </summary>
        static int PortraitPickerCases(int w, int h, float[] shifts, HudBox[] others, List<string> fails, ref int failCount)
        {
            int cases = 0;
            float chipW = FlightView.WidestChip() - 0.01f;
            float gap = FlightHudLayout.GapPt - 0.01f;
            var insets = h >= 812 ? NotchedInsets : FlatInsets;
            // #322: the stage banner in its card, and as the full-width banner row the device tests' longest line needs.
            float bannerRow = FlightHudLayout.BannerRowPt(FlightHudLayout.BannerReserve, w);
            var rows = bannerRow > 0f ? new[] { 0f, bannerRow } : new[] { 0f };
            foreach (var inset in insets)
            foreach (var mode in Modes)
            foreach (float banner in rows)
            foreach (bool nudges in NudgeRows)
            {
                var f = FlightHudLayout.Place(w, h, inset[0], inset[1], inset[2], inset[3], mode, banner, nudges);
                // The frame's own picker: a real gap above the frame row.
                if (!f.CameraPicker.Live || f.CameraPicker.Bottom > f.Pause.Y - gap)
                    Fail(fails, ref failCount, w + "x" + h + " inset " + inset[0] + "/" + inset[1] + " " + mode + " banner " + banner + " frame picker " + Show(f.CameraPicker) + " vs Pause " + Show(f.Pause));
                FillOthers(f, others);
                var room = FlightHudLayout.PickerRoom(f);
                var row0 = FlightHudLayout.ToolsRow(f);
                float ceiling = FlightHudLayout.PickerCeiling(f);
                foreach (float dy in shifts)
                {
                    cases++;
                    var row = row0;
                    row.Y += dy;
                    var pause = Shift(f.Pause, dy);
                    var cam = Shift(f.Camera, dy);
                    var follow = Shift(f.Follow, dy);
                    var p = FlightHudLayout.PickerOverRow(room, row, cam.X + cam.W * 0.5f, ceiling);
                    string bad = null;
                    if (!p.Live) bad = "picker not placed";
                    else
                    {
                        bad = ToolClear(p, pause, gap, "Pause") ?? ToolClear(p, cam, gap, "Camera") ?? ToolClear(p, follow, gap, "Follow");
                        for (int i = 0; bad == null && i < others.Length; i++)
                        {
                            // Pause, Follow and Camera are checked against the shifted (real) row above.
                            if (i == 16 || i == 17 || i == 25) continue;
                            if (p.Overlaps(others[i])) bad = "covers " + OtherNames[i] + " " + Show(others[i]);
                        }
                        if (bad == null && (p.X < inset[2] - 0.01f || p.Right > w - inset[3] + 0.01f || p.Y < inset[0] - 0.01f || p.Bottom > h - inset[1] + 0.01f))
                            bad = "leaves the safe area";
                        if (bad == null)
                        {
                            var chips = FlightHudLayout.PickerChips(p);
                            if (chips.Length != 5) bad = chips.Length + " chips";
                            for (int i = 0; bad == null && i < chips.Length; i++)
                                if (chips[i].W < chipW || chips[i].H < 44f - 0.01f || chips[i].Bottom > p.Bottom + 0.01f) bad = "chip " + i + " " + Show(chips[i]);
                        }
                        // Panel px (FlightHudLayout.ToPanel): still clear of Pause after scaling.
                        for (int i = 0; bad == null && i < PanelScales.Length; i++)
                        {
                            float u = PanelScales[i];
                            if (FlightHudLayout.ToPanel(p, u, u).Overlaps(FlightHudLayout.ToPanel(pause, u, u))) bad = "covers Pause in panel x" + u;
                        }
                    }
                    if (bad != null)
                        Fail(fails, ref failCount, w + "x" + h + " inset " + inset[0] + "/" + inset[1] + " " + mode + " banner " + banner + " row " + dy + ": " + bad + " picker " + Show(p));
                }
            }
            return cases;
        }

        static string ToolClear(HudBox p, HudBox tool, float gap, string name)
        {
            if (p.Overlaps(tool)) return "covers " + name + " " + Show(tool);
            bool sameColumn = p.X < tool.Right && p.Right > tool.X;
            if (!sameColumn) return null;
            float g = p.Bottom <= tool.Y ? tool.Y - p.Bottom : p.Y - tool.Bottom;
            return g < gap ? "gap " + g + " to " + name : null;
        }

        /// <summary>
        /// Landscape dock button rows: 44 CSS pt, and 44 px on the 1280x800 device-test panel (1214 px wide: 44 / 0.948
        /// = 46.4 CSS pt, rounded up to whole pixels by AscentHud).
        /// </summary>
        static readonly float[] DockRowPts = { FlightHudLayout.ButtonPt, 44f * 1280f / 1214f };

        /// <summary>One landscape CSS size (and the same aspect on the 1280-wide device-test panel): every inset and mode.</summary>
        static int LandscapePickerCases(float w, float h, HudBox[] others, List<string> fails, ref int failCount)
        {
            int cases = 0;
            if (!(w > h)) return 0;
            var insets = new[] { new[] { 0f, 0f, 0f, 0f }, new[] { 0f, 21f, 47f, 47f } };
            foreach (var inset in insets)
            foreach (var mode in Modes)
            foreach (float rowPt in DockRowPts)
            {
                cases++;
                var f = FlightHudLayout.PlaceWide(w, h, inset[0], inset[1], inset[2], inset[3], mode, rowPt);
                var p = f.CameraPicker;
                string bad = null;
                if (!p.Live) bad = "picker not placed";
                else
                {
                    FillOthers(f, others);
                    for (int i = 0; bad == null && i < others.Length; i++)
                        if (p.Overlaps(others[i])) bad = "covers " + OtherNames[i] + " " + Show(others[i]);
                    if (bad == null && (p.X < inset[2] - 0.01f || p.Right > w - inset[3] + 0.01f || p.Y < inset[0] - 0.01f || p.Bottom > h - inset[1] + 0.01f))
                        bad = "leaves the safe area";
                    float u = 1280f / w;
                    if (bad == null && FlightHudLayout.ToPanel(p, u, u).Overlaps(FlightHudLayout.ToPanel(f.Pause, u, u)))
                        bad = "covers Pause on the 1280 panel";
                }
                if (bad != null)
                    Fail(fails, ref failCount, w + "x" + h + " inset " + inset[2] + " " + mode + " rows " + rowPt + ": " + bad + " picker " + Show(p));
            }
            return cases;
        }

        /// <summary>
        /// CI 2026-10-21 (#304), 335x720 portrait: the open picker covered flight-pause. The frame put its bottom
        /// one GapPt above the frame's tools row, but portrait keeps Follow / Cam / Pause in the flex flow and the
        /// real row sat ~1 px higher. The picker seated against the real row (PickerOverRow, as
        /// AscentHud.SeatRailPicker does) clears Pause, Cam and Follow by a whole GapPt, covers no other HUD box,
        /// stays in the safe area, and keeps five 44 pt chips, also in panel px. dotnet: 320-440 x 568-956
        /// (every width, heights in 4 pt steps) and seven row shifts. Unity: the device sizes, shifts -6 / 0 / +4.
        /// </summary>
        [Test]
        public void OpenPickerNeverCoversPause_PortraitSweep()
        {
            var fails = new List<string>();
            var others = new HudBox[OtherNames.Length];
            int failCount = 0, cases = 0;
            if (ExhaustivePickerSweep)
            {
                for (int w = 320; w <= 440; w++)
                for (int h = 568; h <= 956; h += 4)
                    cases += PortraitPickerCases(w, h, PortraitShiftsAll, others, fails, ref failCount);
                Assert.That(cases, Is.GreaterThan(100000), "the exhaustive sweep ran");
            }
            else
            {
                foreach (var s in PickerPortraitSamples)
                    cases += PortraitPickerCases(s[0], s[1], PortraitShiftsEdge, others, fails, ref failCount);
            }
            TestContext.WriteLine("portrait picker sweep: " + cases + " cases" + (ExhaustivePickerSweep ? " (exhaustive)" : " (sampled)"));
            AssertNoFailures(fails, failCount, cases, "portrait picker sweep");
        }

        /// <summary>
        /// Landscape web: the open picker sits in the rocket column, left of Cam, and never covers Pause, Cam,
        /// Follow or any other HUD box, and stays in the safe area, in every dock mode, with and without insets.
        /// dotnet: every 4th CSS width from 960 to 2560 at ten heights. Unity: the device-test sizes and ends.
        /// </summary>
        [Test]
        public void OpenPickerNeverCoversPause_LandscapeSweep()
        {
            var fails = new List<string>();
            var others = new HudBox[OtherNames.Length];
            int failCount = 0, cases = 0;
            if (ExhaustivePickerSweep)
            {
                var heights = new[] { 540f, 600f, 720f, 759f, 768f, 800f, 900f, 1080f, 1200f, 1440f };
                for (int w = 960; w <= 2560; w += 4)
                foreach (float h in heights)
                    cases += LandscapePickerCases(w, h, others, fails, ref failCount);
                Assert.That(cases, Is.GreaterThan(10000), "the exhaustive sweep ran");
            }
            else
            {
                foreach (var s in PickerLandscapeSamples)
                {
                    cases += LandscapePickerCases(s[0], s[1], others, fails, ref failCount);
                    // The device tests' panel: 1280 wide at the same aspect.
                    cases += LandscapePickerCases(1280f, 1280f * s[1] / s[0], others, fails, ref failCount);
                }
            }
            // AscentHud lays landscape out on the scaled web panel in face points (#322): every device panel.
            foreach (var s in PickerLandscapeSamples)
            {
                DevicePanel(s[0], s[1], out float pw, out float ph);
                cases += LandscapePickerCases(pw, ph, others, fails, ref failCount);
            }
            TestContext.WriteLine("landscape picker sweep: " + cases + " cases" + (ExhaustivePickerSweep ? " (exhaustive)" : " (sampled)"));
            AssertNoFailures(fails, failCount, cases, "landscape picker sweep");
        }

        /// <summary>The sampled grid (what Unity runs) also passes under dotnet, so the two paths cannot drift.</summary>
        [Test]
        public void OpenPickerSampledGridPasses()
        {
            var fails = new List<string>();
            var others = new HudBox[OtherNames.Length];
            int failCount = 0, cases = 0;
            foreach (var s in PickerPortraitSamples)
                cases += PortraitPickerCases(s[0], s[1], PortraitShiftsEdge, others, fails, ref failCount);
            foreach (var s in PickerLandscapeSamples)
            {
                cases += LandscapePickerCases(s[0], s[1], others, fails, ref failCount);
                cases += LandscapePickerCases(1280f, 1280f * s[1] / s[0], others, fails, ref failCount);
            }
            AssertNoFailures(fails, failCount, cases, "sampled picker grid");
        }

        [Test]
        public void PickerOverRowOpensBelowWhenTheRoomAboveIsShort()
        {
            var room = new HudBox { X = 0f, Y = 0f, W = 320f, H = 300f };
            var row = new HudBox { X = 0f, Y = 40f, W = 320f, H = 44f };
            var p = FlightHudLayout.PickerOverRow(room, row, 160f);
            Assert.That(p.Y, Is.GreaterThanOrEqualTo(row.Bottom + FlightHudLayout.GapPt - 0.01f), "below the row");
            Assert.That(p.Overlaps(row), Is.False);
            Assert.That(p.Bottom, Is.LessThanOrEqualTo(room.Bottom + 0.01f));
        }

        static HudBox Shift(HudBox b, float dy) => new HudBox { X = b.X, Y = b.Y + dy, W = b.W, H = b.H };

        static string Show(HudBox b) => "(" + b.X.ToString("0.#") + "," + b.Y.ToString("0.#") + " " + b.W.ToString("0.#") + "x" + b.H.ToString("0.#") + ")";

        static void AssertCamera(FlightHudFrame f, float w, float h, float st, float sb, float sl, float sr, string where)
        {
            Assert.That(f.Camera.Live, Is.True, where + " camera");
            Assert.That(f.Camera.W, Is.GreaterThanOrEqualTo(44f), where + " camera target");
            Assert.That(f.Camera.H, Is.GreaterThanOrEqualTo(44f), where + " camera target");
            Assert.That(f.CameraPicker.Live, Is.True, where + " picker");
            var chips = FlightHudLayout.PickerChips(f.CameraPicker);
            Assert.That(chips.Length, Is.EqualTo(5), where);
            for (int i = 0; i < chips.Length; i++)
            {
                Assert.That(chips[i].W, Is.GreaterThanOrEqualTo(FlightView.WidestChip() - 0.01f), where + " chip " + i);
                Assert.That(chips[i].H, Is.GreaterThanOrEqualTo(44f), where + " chip " + i);
                Assert.That(chips[i].X, Is.GreaterThanOrEqualTo(f.CameraPicker.X - 0.01f), where + " chip " + i + " inside");
                Assert.That(chips[i].Right, Is.LessThanOrEqualTo(f.CameraPicker.Right + 0.01f), where + " chip " + i + " inside");
                Assert.That(chips[i].Bottom, Is.LessThanOrEqualTo(f.CameraPicker.Bottom + 0.01f), where + " chip " + i + " inside");
                for (int j = 0; j < i; j++)
                    Assert.That(chips[i].Overlaps(chips[j]), Is.False, where + " chips " + j + "/" + i);
            }
            foreach (var mine in new[] { ("Camera", f.Camera), ("CameraPicker", f.CameraPicker) })
            {
                var b = mine.Item2;
                Assert.That(b.X, Is.GreaterThanOrEqualTo(sl - 0.5f), where + " " + mine.Item1 + " left");
                Assert.That(b.Right, Is.LessThanOrEqualTo(w - sr + 0.5f), where + " " + mine.Item1 + " right");
                Assert.That(b.Y, Is.GreaterThanOrEqualTo(st - 0.5f), where + " " + mine.Item1 + " top");
                Assert.That(b.Bottom, Is.LessThanOrEqualTo(h - sb + 0.5f), where + " " + mine.Item1 + " bottom");
                foreach (var other in HudOthers(f))
                    Assert.That(b.Overlaps(other.Item2), Is.False, where + " " + mine.Item1 + " covers " + other.Item1);
            }
            Assert.That(f.Camera.Overlaps(f.CameraPicker), Is.False, where + " picker beside its button");
        }

        static IEnumerable<(string, HudBox)> HudOthers(FlightHudFrame f)
        {
            yield return ("Clock", f.Clock);
            yield return ("Stage", f.Stage);
            yield return ("Telemetry", f.Telemetry);
            for (int i = 0; i < f.Cells.Length; i++) yield return ("Cell" + i, f.Cells[i]);
            yield return ("Health", f.Health);
            yield return ("Plot", f.Plot);
            yield return ("Stages", f.Stages);
            yield return ("Dock", f.Dock);
            yield return ("Info", f.Info);
            yield return ("Medal", f.Medal);
            yield return ("Help", f.Help);
            yield return ("Quote", f.Quote);
            yield return ("Timer", f.Timer);
            yield return ("Follow", f.Follow);
            yield return ("Pause", f.Pause);
            yield return ("Primary", f.Primary);
            yield return ("Skip", f.Skip);
            yield return ("Assist", f.Assist);
            yield return ("Alternate", f.Alternate);
            yield return ("NudgeDown", f.NudgeDown);
            yield return ("NudgeUp", f.NudgeUp);
            yield return ("Exit", f.Exit);
        }

        static void AssertInside(FlightHudFrame f, FlightHudLayout.Device d, string where)
        {
            float minX = d.SafeLeft + FlightHudLayout.SideMarginPt - 0.6f;
            float maxX = d.W - d.SafeRight - FlightHudLayout.SideMarginPt + 0.6f;
            float minY = d.SafeTop - 0.6f;
            float maxY = d.H - d.SafeBottom + 0.6f;
            foreach (var b in Boxes(f))
            {
                if (!b.box.Live) continue;
                Assert.That(b.box.X, Is.GreaterThanOrEqualTo(minX), where + " " + b.name);
                Assert.That(b.box.Right, Is.LessThanOrEqualTo(maxX), where + " " + b.name);
                Assert.That(b.box.Y, Is.GreaterThanOrEqualTo(minY), where + " " + b.name);
                Assert.That(b.box.Bottom, Is.LessThanOrEqualTo(maxY), where + " " + b.name);
            }
        }

        static void AssertNoOverlap(FlightHudFrame f, string where)
        {
            var boxes = Boxes(f);
            for (int i = 0; i < boxes.Count; i++)
            for (int j = i + 1; j < boxes.Count; j++)
            {
                if (boxes[i].name == "dock" || boxes[j].name == "dock") continue;
                if (!boxes[i].box.Live || !boxes[j].box.Live) continue;
                Assert.That(boxes[i].box.Overlaps(boxes[j].box), Is.False, where + " " + boxes[i].name + " overlaps " + boxes[j].name);
            }
            foreach (var b in boxes)
            {
                if (!b.box.Live || b.name == "dock" || b.name == "follow" || b.name == "pause") continue;
                if (b.name == "clock" || b.name == "stage" || b.name == "health" || b.name == "plot" || b.name == "stages" || b.name.StartsWith("cell")) continue;
                Assert.That(b.box.Y, Is.GreaterThanOrEqualTo(f.Dock.Y - 0.6f), where + " " + b.name + " left the dock");
                Assert.That(b.box.Bottom, Is.LessThanOrEqualTo(f.Dock.Bottom + 0.6f), where + " " + b.name);
            }
            Assert.That(f.Dock.Overlaps(f.Plot), Is.False, where);
            Assert.That(f.Dock.Overlaps(f.Stages), Is.False, where);
            Assert.That(f.Dock.Overlaps(f.Clock), Is.False, where);
            Assert.That(f.Dock.Overlaps(f.Follow), Is.False, where);
            Assert.That(f.Dock.Overlaps(f.Pause), Is.False, where);
        }

        static void AssertStackClear(FlightHudFrame f, FlightHudLayout.Device d, string where)
        {
            float nose = FlightHudLayout.StackTop * d.H;
            float tail = FlightHudLayout.StackBottom * d.H;
            Assert.That(f.Stages.Bottom, Is.LessThan(nose - 6f), where + " stages cover the stack");
            Assert.That(f.Telemetry.Bottom, Is.LessThan(nose), where);
            Assert.That(f.Dock.Y, Is.GreaterThan(tail + 4f), where + " dock covers the stack");
            Assert.That(f.Health.Y, Is.GreaterThanOrEqualTo(f.Clock.Bottom - 0.6f), where + " health covers the clock");
            Assert.That(f.Telemetry.Y, Is.GreaterThanOrEqualTo(f.Health.Bottom - 0.6f), where + " strip covers health");
            Assert.That(f.Plot.Y, Is.GreaterThanOrEqualTo(f.Telemetry.Bottom - 0.6f), where + " trajectory covers the strip");
            Assert.That(f.Plot.Bottom, Is.LessThanOrEqualTo(f.Follow.Y + 0.6f), where + " trajectory covers follow");
            Assert.That(f.Plot.H, Is.GreaterThan(160f), where + " trajectory runs up the screen");
            Assert.That(f.Plot.Right, Is.LessThanOrEqualTo(d.W * 0.35f + 0.6f), where + " trajectory crosses the rocket");
            Assert.That(f.Stages.X, Is.GreaterThanOrEqualTo(d.W * 0.65f - 0.6f), where + " stages cross the rocket");
            Assert.That(f.Follow.Right, Is.LessThanOrEqualTo(d.W * 0.35f + 0.6f), where + " follow covers the rocket");
            Assert.That(f.Pause.X, Is.GreaterThanOrEqualTo(d.W * 0.65f - 0.6f), where + " pause covers the rocket");
            Assert.That(f.Stages.H, Is.EqualTo(FlightHudLayout.PlotPt), where);
        }

        static void AssertTelemetryFits(FlightHudFrame f, string where)
        {
            float inner = f.Cells[0].W - 2f * FlightHudLayout.CellPadPt;
            Assert.That(inner, Is.GreaterThan(48f), where);
            foreach (var word in FlightHudLayout.TelemetryLabels)
                Assert.That(FlightHudLayout.TextWidth(word, FlightHudLayout.LabelFontPt), Is.LessThan(inner), where + " " + word);
            foreach (var word in FlightHudLayout.ValueSamples)
                Assert.That(FlightHudLayout.TextWidth(word, FlightHudLayout.ValueFontPt), Is.LessThan(inner), where + " " + word);
            Assert.That(FlightHudLayout.TextWidth("Auto-assist on", 12f), Is.LessThan(f.Assist.W - 8f), where);
            Assert.That(FlightHudLayout.TextWidth("Follow rocket", 13f), Is.LessThan(f.Follow.W - 4f), where);
        }

        static void AssertSameRow(HudBox a, HudBox b, string where)
        {
            Assert.That(a.Overlaps(b), Is.False, where);
            float top = a.Y > b.Y ? a.Y : b.Y;
            float bot = a.Bottom < b.Bottom ? a.Bottom : b.Bottom;
            Assert.That(bot - top, Is.GreaterThan(8f), where + " not one row");
        }

        static List<(string name, HudBox box)> Boxes(FlightHudFrame f)
        {
            var list = new List<(string, HudBox)>
            {
                ("clock", f.Clock), ("stage", f.Stage), ("health", f.Health), ("plot", f.Plot), ("stages", f.Stages),
                ("dock", f.Dock), ("info", f.Info), ("medal", f.Medal), ("help", f.Help),
                ("quote", f.Quote), ("timer", f.Timer), ("assist", f.Assist), ("primary", f.Primary),
                ("skip", f.Skip), ("alternate", f.Alternate), ("nudge-down", f.NudgeDown), ("nudge-up", f.NudgeUp),
                ("follow", f.Follow), ("pause", f.Pause),
            };
            // The strip is the union of the cells. Cells are the overlap contract.
            for (int i = 0; i < f.Cells.Length; i++) list.Add(("cell-" + i, f.Cells[i]));
            return list;
        }
    }
}
