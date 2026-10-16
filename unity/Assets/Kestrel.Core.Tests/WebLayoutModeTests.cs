// Landscape web frames for the desktop sizes Trevor actually opens, plus a phone turned sideways.
// Portrait math (FlightHudLayout.Place, iOS) is not on this path.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class WebLayoutModeTests
    {
        struct Size
        {
            public string Name;
            public float W, H, Top, Bottom, Left, Right;
            public Size(string name, float w, float h, float top, float bottom, float left, float right)
            { Name = name; W = w; H = h; Top = top; Bottom = bottom; Left = left; Right = right; }
        }

        static readonly Size[] Wide =
        {
            new Size("1280x720", 1280f, 720f, 0f, 0f, 0f, 0f),
            new Size("1920x1080", 1920f, 1080f, 0f, 0f, 0f, 0f),
            new Size("1440x900", 1440f, 900f, 0f, 0f, 0f, 0f),
            new Size("phone landscape", 844f, 390f, 0f, 21f, 47f, 47f),
        };

        static readonly FlightHudMode[] Modes =
        {
            FlightHudMode.Cruise, FlightHudMode.Notes, FlightHudMode.Prompt, FlightHudMode.PromptNotes,
        };

        [Test]
        public void WideModeIsWebLandscapeOnly()
        {
            Assert.That(WebLayoutMode.UseWide(1920f, 1080f, false), Is.False, "iOS landscape must stay on the phone layout");
            Assert.That(WebLayoutMode.UseWide(390f, 844f, true), Is.False, "portrait web keeps the phone column");
            Assert.That(WebLayoutMode.UseWide(844f, 844f, true), Is.False, "square counts as portrait");
            Assert.That(WebLayoutMode.UseWide(1280f, 720f, true), Is.True);
            Assert.That(WebLayoutMode.UseWide(844f, 390f, true), Is.True);
            Assert.That(WebLayoutMode.IsLandscape(1032f, 1376f), Is.False);
        }

        [Test]
        public void DesktopAndPhoneLandscapeDoNotOverlap()
        {
            foreach (var d in Wide)
            {
                var home = WebHomeLayout.Place(d.W, d.H, d.Top, d.Bottom, d.Left, d.Right);
                string where = d.Name;
                Assert.That(home.Wide, Is.True, where);
                AssertSideBySideHome(home, d, where);
                NoOverlap(HomeBoxes(home), where + " home");
                Inside(HomeBoxes(home), d, where + " home");

                var design = WebDesignerLayout.Place(d.W, d.H, d.Top, d.Bottom, d.Left, d.Right);
                Assert.That(design.Wide, Is.True, where);
                Assert.That(design.Parts.Right, Is.LessThanOrEqualTo(design.Rocket.X + 0.6f), where + " parts cover the rocket");
                Assert.That(design.Rocket.Right, Is.LessThanOrEqualTo(design.RightPanel.X + 0.6f), where + " readouts cover the rocket");
                Assert.That(design.Rocket.W, Is.GreaterThan(d.W * 0.2f), where + " rocket column");
                float rocketMid = design.Rocket.X + design.Rocket.W * 0.5f;
                Assert.That(rocketMid, Is.EqualTo(d.W * 0.5f).Within(d.W * 0.08f), where + " rocket not centred");
                Assert.That(design.Launch.H, Is.GreaterThanOrEqualTo(44f), where + " launch target");
                NoOverlap(DesignBoxes(design), where + " designer");
                Inside(DesignBoxes(design), d, where + " designer");

                foreach (var mode in Modes)
                {
                    var hud = FlightHudLayout.PlaceWide(d.W, d.H, d.Top, d.Bottom, d.Left, d.Right, mode);
                    string hudWhere = where + " " + mode;
                    AssertHud(hud, d, mode, hudWhere);
                }
            }
        }

        [Test]
        public void PortraitWebStaysAPhoneColumn()
        {
            var home = WebHomeLayout.Place(390f, 844f, 47f, 34f, 0f, 0f);
            Assert.That(home.Wide, Is.False);
            Assert.That(home.Menu.W, Is.GreaterThan(300f), "menu still spans the phone");
            Assert.That(home.Launch.W, Is.GreaterThan(home.Menu.W * 0.9f));
            Assert.That(home.Rocket.Bottom, Is.LessThanOrEqualTo(home.Menu.Y + 0.6f));
            NoOverlap(HomeBoxes(home), "portrait home");

            var design = WebDesignerLayout.Place(390f, 844f, 47f, 34f, 0f, 0f);
            Assert.That(design.Wide, Is.False);
            Assert.That(design.Rocket.Bottom, Is.LessThanOrEqualTo(design.RightPanel.Y + 0.6f));
            NoOverlap(DesignBoxes(design), "portrait designer");
        }

        static void AssertSideBySideHome(WebHomeFrame home, Size d, string where)
        {
            Assert.That(home.Menu.X, Is.GreaterThanOrEqualTo(home.Rocket.Right - 0.6f), where + " tiles cover the rocket");
            Assert.That(home.Stats.Right, Is.LessThanOrEqualTo(home.Rocket.X + 0.6f), where + " stats cover the rocket");
            float mid = home.Rocket.X + home.Rocket.W * 0.5f;
            Assert.That(mid, Is.EqualTo(d.W * 0.5f).Within(d.W * 0.08f), where + " rocket not centred");
            Assert.That(home.Launch.X, Is.EqualTo(home.Menu.X).Within(0.6f), where);
            Assert.That(home.Codex.X, Is.EqualTo(home.Menu.X).Within(0.6f), where + " tiles leave the column");
            Assert.That(home.Missions.Bottom, Is.LessThanOrEqualTo(home.Codex.Y + 0.6f), where);
            Assert.That(home.Launch.H, Is.GreaterThanOrEqualTo(36f), where);
            Assert.That(home.Title.Bottom, Is.LessThanOrEqualTo(home.Rocket.Y + 0.6f), where + " title covers the rocket");
        }

        static void AssertHud(FlightHudFrame f, Size d, FlightHudMode mode, string where)
        {
            Assert.That(f.Telemetry.W, Is.GreaterThan(d.W * 0.45f), where + " stat strip");
            Assert.That(f.Telemetry.Bottom, Is.LessThanOrEqualTo(f.Plot.Y + 0.6f), where + " strip covers the plot");
            Assert.That(f.Cells.Length, Is.EqualTo(4), where);
            for (int i = 1; i < 4; i++)
                Assert.That(f.Cells[i].Y, Is.EqualTo(f.Cells[0].Y).Within(0.6f), where + " cells are not one strip");
            Assert.That(f.Plot.Right, Is.LessThanOrEqualTo(f.Stages.X + 0.6f), where);
            var rocket = FlightHudLayout.RocketColumn(f);
            Assert.That(rocket.W, Is.GreaterThan(d.W * 0.2f), where + " rocket column");
            Assert.That(f.Dock.Y, Is.GreaterThanOrEqualTo(d.H * FlightHudLayout.StackBottom - 0.6f), where + " dock covers the rocket");
            Assert.That(f.Info.W, Is.GreaterThanOrEqualTo(44f), where);
            Assert.That(f.Follow.H, Is.GreaterThanOrEqualTo(44f), where);
            Assert.That(f.Pause.H, Is.GreaterThanOrEqualTo(44f), where);
            Assert.That(f.Exit.Live, Is.True, where);
            if (mode == FlightHudMode.Prompt || mode == FlightHudMode.PromptNotes)
                Assert.That(f.Primary.Live && f.Assist.Live && f.NudgeDown.Live, Is.True, where);
            else
                Assert.That(f.Assist.Live && f.NudgeDown.Live, Is.True, where);
            if (mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes)
                Assert.That(f.Help.Live, Is.True, where);
            float inner = f.Cells[0].W - 2f * FlightHudLayout.CellPadPt;
            Assert.That(inner, Is.GreaterThan(36f), where + " cell");
            foreach (var word in FlightHudLayout.TelemetryLabels)
                Assert.That(FlightHudLayout.TextWidth(word, FlightHudLayout.LabelFontPt), Is.LessThan(inner), where + " " + word);

            var boxes = HudBoxes(f);
            boxes.Add(("rocket", rocket));
            NoOverlap(boxes, where);
            Inside(boxes, d, where);
        }

        static List<(string name, HudBox box)> HomeBoxes(WebHomeFrame f)
        {
            return new List<(string, HudBox)>
            {
                ("rocket", f.Rocket), ("menu", f.Menu), ("title", f.Title), ("stats", f.Stats),
                ("launch", f.Launch), ("design", f.Design), ("missions", f.Missions), ("codex", f.Codex),
                ("gear", f.Gear), ("exit", f.Exit), ("audio", f.Audio), ("hint", f.Hint),
            };
        }

        static List<(string name, HudBox box)> DesignBoxes(WebDesignerFrame f)
        {
            return new List<(string, HudBox)>
            {
                ("rocket", f.Rocket), ("parts", f.Parts), ("readouts", f.Readouts), ("launch", f.Launch),
                ("exit", f.Exit), ("home", f.Home),
            };
        }

        static List<(string name, HudBox box)> HudBoxes(FlightHudFrame f)
        {
            var list = new List<(string, HudBox)>
            {
                ("clock", f.Clock), ("stage", f.Stage), ("telemetry", f.Telemetry), ("plot", f.Plot),
                ("stages", f.Stages), ("dock", f.Dock), ("info", f.Info), ("medal", f.Medal),
                ("help", f.Help), ("quote", f.Quote), ("timer", f.Timer), ("assist", f.Assist),
                ("primary", f.Primary), ("skip", f.Skip), ("alternate", f.Alternate),
                ("nudge-down", f.NudgeDown), ("nudge-up", f.NudgeUp), ("follow", f.Follow),
                ("pause", f.Pause), ("exit", f.Exit),
            };
            for (int i = 0; i < f.Cells.Length; i++) list.Add(("cell-" + i, f.Cells[i]));
            return list;
        }

        static void NoOverlap(List<(string name, HudBox box)> boxes, string where)
        {
            for (int i = 0; i < boxes.Count; i++)
            for (int j = i + 1; j < boxes.Count; j++)
            {
                if (SkipPair(boxes[i].name, boxes[j].name)) continue;
                if (!boxes[i].box.Live || !boxes[j].box.Live) continue;
                Assert.That(boxes[i].box.Overlaps(boxes[j].box), Is.False,
                    where + " " + boxes[i].name + " overlaps " + boxes[j].name);
            }
        }

        static bool SkipPair(string a, string b)
        {
            // Parents contain their children. The rocket column is asserted against the rails separately.
            if (ContainsPair(a, b, "menu", "launch")) return true;
            if (ContainsPair(a, b, "menu", "design")) return true;
            if (ContainsPair(a, b, "menu", "missions")) return true;
            if (ContainsPair(a, b, "menu", "codex")) return true;
            if (ContainsPair(a, b, "menu", "hint")) return true;
            if (ContainsPair(a, b, "menu", "stats")) return true;
            if (ContainsPair(a, b, "telemetry", "clock")) return true;
            if (ContainsPair(a, b, "telemetry", "stage")) return true;
            if (ContainsPair(a, b, "telemetry", "cell-0")) return true;
            if (ContainsPair(a, b, "telemetry", "cell-1")) return true;
            if (ContainsPair(a, b, "telemetry", "cell-2")) return true;
            if (ContainsPair(a, b, "telemetry", "cell-3")) return true;
            if (ContainsPair(a, b, "dock", "info")) return true;
            if (ContainsPair(a, b, "dock", "medal")) return true;
            if (ContainsPair(a, b, "dock", "help")) return true;
            if (ContainsPair(a, b, "dock", "quote")) return true;
            if (ContainsPair(a, b, "dock", "timer")) return true;
            if (ContainsPair(a, b, "dock", "assist")) return true;
            if (ContainsPair(a, b, "dock", "primary")) return true;
            if (ContainsPair(a, b, "dock", "skip")) return true;
            if (ContainsPair(a, b, "dock", "alternate")) return true;
            if (ContainsPair(a, b, "dock", "nudge-down")) return true;
            if (ContainsPair(a, b, "dock", "nudge-up")) return true;
            return false;
        }

        static bool ContainsPair(string a, string b, string parent, string child) =>
            (a == parent && b == child) || (b == parent && a == child);

        static void Inside(List<(string name, HudBox box)> boxes, Size d, string where)
        {
            float minX = d.Left - 0.6f;
            float maxX = d.W - d.Right + 0.6f;
            float minY = d.Top - 0.6f;
            float maxY = d.H - d.Bottom + 0.6f;
            foreach (var b in boxes)
            {
                if (!b.box.Live) continue;
                Assert.That(b.box.X, Is.GreaterThanOrEqualTo(minX), where + " " + b.name);
                Assert.That(b.box.Right, Is.LessThanOrEqualTo(maxX), where + " " + b.name);
                Assert.That(b.box.Y, Is.GreaterThanOrEqualTo(minY), where + " " + b.name);
                Assert.That(b.box.Bottom, Is.LessThanOrEqualTo(maxY), where + " " + b.name);
            }
        }
    }
}
