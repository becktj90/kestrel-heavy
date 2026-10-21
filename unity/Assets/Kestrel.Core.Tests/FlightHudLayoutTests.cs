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
