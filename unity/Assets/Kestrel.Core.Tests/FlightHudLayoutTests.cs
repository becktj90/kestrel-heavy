// Portrait flight HUD: SE, Pro Max, and iPad. Nothing overlaps, the plot and the stages panel
// stay translucent and above the stack, and the dock stays below it.
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
                if (b.name == "clock" || b.name == "stage" || b.name == "plot" || b.name == "stages" || b.name.StartsWith("cell")) continue;
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
                ("clock", f.Clock), ("stage", f.Stage), ("plot", f.Plot), ("stages", f.Stages),
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
