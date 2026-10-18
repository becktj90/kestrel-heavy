// Mission board: portrait timeline over a featured card, wide WebGL side by side,
// and player copy with real numbers and no raw ids.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissionBoardLayoutTests
    {
        static readonly Regex RawId = new Regex(
            @"\b(kid-box|leo-sat|gto-comsat|crew-capsule|lunar-probe|weather-sat|space-telescope|cargo-capsule|cubesat-dispenser|geo-relay|lunar-lander|cubesat|payloadId|maxG|noRedRisks|coastHours|nozzle-expansion|grid-fins|hydrolox-upper|stage-2|redundant-avionics|kh-m\d+|achievable|m\d+)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        [Test]
        public void PortraitBoardsFitSeProMaxIpadAndTheNarrowPanel()
        {
            var sizes = new List<Size>();
            foreach (var d in FlightHudLayout.PortraitDevices())
                sizes.Add(new Size(d.Name, d.W, d.H, d.SafeTop, d.SafeBottom, d.SafeLeft, d.SafeRight));
            // Scale-with-screen-size, match height: Pro Max's panel is narrower than SE.
            sizes.Add(new Size("Pro Max panel", 307f, 640f, 36f, 20f, 8f, 8f));
            sizes.Add(new Size("iPad panel", 500f, 640f, 16f, 12f, 8f, 8f));

            foreach (var d in sizes)
            {
                Assert.That(WebLayoutMode.UseWide(d.W, d.H, false), Is.False, d.Name + " iOS stays portrait");
                var frame = MissionBoardLayout.Place(d.W, d.H, d.Top, d.Bottom, d.Left, d.Right, wide: false);
                AssertBoard(frame, d, wide: false);
            }
        }

        [Test]
        public void WideWebIsSideBySideListAndDetail()
        {
            foreach (var d in Wide)
            {
                Assert.That(WebLayoutMode.UseWide(d.W, d.H, true), Is.True, d.Name);
                Assert.That(WebLayoutMode.UseWide(d.W, d.H, false), Is.False, d.Name + " iOS landscape stays a column");
                var frame = MissionBoardLayout.Place(d.W, d.H, d.Top, d.Bottom, d.Left, d.Right, wide: true);
                AssertBoard(frame, d, wide: true);
                Assert.That(frame.Path.Right, Is.LessThanOrEqualTo(frame.Card.X + 0.6f), d.Name + " list covers the card");
                Assert.That(frame.Card.W, Is.GreaterThan(frame.Path.W * 0.7f), d.Name + " detail column");
                Assert.That(frame.Art.Right, Is.LessThanOrEqualTo(frame.Name.X + 0.6f), d.Name + " portrait covers the title");
            }
        }

        [Test]
        public void NodesStayInThePathAndTheSheetDoesNotCoverThem()
        {
            var sizes = new List<Size>();
            foreach (var d in FlightHudLayout.PortraitDevices())
                sizes.Add(new Size(d.Name, d.W, d.H, d.SafeTop, d.SafeBottom, d.SafeLeft, d.SafeRight));
            sizes.Add(new Size("Pro Max panel", 307f, 640f, 36f, 20f, 8f, 8f));
            foreach (var d in Wide) sizes.Add(d);

            foreach (var d in sizes)
            {
                bool wide = d.W > d.H;
                var frame = MissionBoardLayout.Place(d.W, d.H, d.Top, d.Bottom, d.Left, d.Right, wide);
                if (!wide)
                    Assert.That(frame.Path.Bottom, Is.LessThanOrEqualTo(frame.Card.Y + 0.6f), d.Name + " sheet covers the map");
                var nodes = MissionBoardLayout.Nodes(frame.Path.W, 20);
                float contentH = MissionBoardLayout.ContentHeight(20);
                Assert.AreEqual(20, nodes.Length);
                for (int i = 0; i < nodes.Length; i++)
                {
                    var n = nodes[i];
                    Assert.That(n.X - MissionBoardLayout.NodeR, Is.GreaterThanOrEqualTo(0f), d.Name + " node " + i);
                    Assert.That(n.X + MissionBoardLayout.NodeR, Is.LessThanOrEqualTo(frame.Path.W + 0.6f), d.Name + " node " + i);
                    Assert.That(n.Y - MissionBoardLayout.NodeR, Is.GreaterThanOrEqualTo(0f), d.Name);
                    Assert.That(n.Y + MissionBoardLayout.NodeR + MissionBoardLayout.StarRow, Is.LessThanOrEqualTo(contentH + 0.6f), d.Name);
                    if (i > 0)
                    {
                        float dx = n.X - nodes[i - 1].X;
                        float dy = n.Y - nodes[i - 1].Y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        Assert.That(dist, Is.GreaterThan(MissionBoardLayout.NodeR * 2f + 8f), d.Name + " nodes overlap");
                    }
                }
                // A node scrolled to the bottom of the viewport still ends above the sheet.
                float nodeBottom = frame.Path.Bottom;
                if (!wide) Assert.That(nodeBottom, Is.LessThanOrEqualTo(frame.Card.Y + 0.6f), d.Name);
            }

            var bar = new HudBox { X = 10f, Y = 20f, W = 200f, H = 8f };
            HudBox prev = default;
            for (int i = 0; i < MissionBoardLayout.ActSlots; i++)
            {
                var seg = MissionBoardLayout.Segment(bar, i, MissionBoardLayout.ActSlots);
                Assert.That(seg.X, Is.GreaterThanOrEqualTo(bar.X - 0.1f));
                Assert.That(seg.Right, Is.LessThanOrEqualTo(bar.Right + 0.6f));
                Assert.That(seg.W, Is.GreaterThan(8f));
                if (i > 0) Assert.That(seg.X, Is.GreaterThanOrEqualTo(prev.Right - 0.1f));
                prev = seg;
            }
        }

        [Test]
        public void SquareStaysAColumn()
        {
            Assert.That(WebLayoutMode.IsLandscape(844f, 844f), Is.False);
            var frame = MissionBoardLayout.Place(844f, 844f, 0f, 0f, 0f, 0f, wide: false);
            Assert.That(frame.Wide, Is.False);
            Assert.That(frame.Card.X, Is.EqualTo(frame.Path.X).Within(0.6f));
            Assert.That(frame.Path.Bottom, Is.LessThanOrEqualTo(frame.Card.Y + 0.6f));
        }

        [Test]
        public void ReduceMotionSnapsAndTheSlideEasesOut()
        {
            Assert.That(MissionBoardMotion.Blend(0f, true), Is.EqualTo(1f));
            Assert.That(MissionBoardMotion.Slide(0f, true), Is.EqualTo(0f));
            Assert.That(MissionBoardMotion.Blend(0f, false), Is.EqualTo(0f));
            Assert.That(MissionBoardMotion.Blend(1f, false), Is.EqualTo(1f));
            float mid = MissionBoardMotion.Blend(0.5f, false);
            Assert.That(mid, Is.GreaterThan(0.5f));
            Assert.That(mid, Is.LessThan(1f));
            Assert.That(MissionBoardMotion.Slide(0f, false), Is.EqualTo(MissionBoardMotion.SlidePt));
            Assert.That(MissionBoardMotion.Slide(1f, false), Is.EqualTo(0f));
        }

        [Test]
        public void EveryFlightShowsRealNumbersAndNoRawIds()
        {
            var campaign = Campaign.Load(Golden.Root);
            Assert.AreEqual(20, campaign.Missions.Count);
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var mission = campaign.Missions[i];
                var locked = MissionBoardFacts.Of(mission, MissionBoardAccess.Locked, 0, null, 0, 0, false);
                var open = MissionBoardFacts.Of(mission, MissionBoardAccess.Open, 0, null, 0, 0, false);
                var launch = MissionBoardFacts.Of(mission, MissionBoardAccess.Open, 0, null, 0, 0, true);
                var cleared = MissionBoardFacts.Of(mission, MissionBoardAccess.Cleared, 2, new[] { "gold", "bronze" }, 12400, 3, true);

                Assert.That(locked.Primary, Is.EqualTo(mission.Index <= 1 ? "Locked" : "Clear KH-" + (mission.Index - 1) + " to unlock"), mission.Id);
                Assert.That(locked.Briefing, Does.Not.EqualTo(""), mission.Id);
                Assert.That(locked.CanGo, Is.False, mission.Id);
                Assert.That(open.Primary, Is.EqualTo("Design first"), mission.Id);
                Assert.That(launch.Primary, Is.EqualTo("Fly KH-" + mission.Index), mission.Id);
                Assert.That(cleared.Primary, Is.EqualTo("Fly KH-" + mission.Index), mission.Id);
                Assert.That(open.Need, Does.Contain("m/s"), mission.Id);
                Assert.That(cleared.Medal, Is.EqualTo("Gold"), mission.Id);
                Assert.That(cleared.Best, Does.Contain("12,400"), mission.Id);
                Assert.That(cleared.Best, Does.Contain("2 stars"), mission.Id);
                Assert.That(open.Best, Does.Contain("no flight yet"), mission.Id);

                Assert.That(open.Mass, Does.Contain(MissionBoardFacts.Number(mission.MassKg) + " kg"), mission.Id);
                Assert.That(open.Orbit, Does.Not.EqualTo(""), mission.Id);
                Assert.That(open.Briefing, Does.Match(@"\d"), mission.Id + " " + open.Briefing);
                Assert.That(open.Customer, Does.Not.EqualTo(""), mission.Id);
                string difficulty = mission.Band == "hard" ? "Hard" : "Steady";
                Assert.That(open.Difficulty, Does.Contain(difficulty), mission.Id);
                Assert.That(open.Title, Does.StartWith("KH-" + mission.Index), mission.Id);
                Assert.That(open.Form, Is.Not.EqualTo("").And.Not.EqualTo(null));
                Assert.That(open.Visible(), Does.Not.Contain(open.Form), "form id is not drawn");

                Sweep(mission.Id + " locked", locked);
                Sweep(mission.Id + " open", open);
                Sweep(mission.Id + " launch", launch);
                Sweep(mission.Id + " cleared", cleared);
            }

            var first = campaign.Mission(1);
            Assert.AreEqual("Apogee 10 km", MissionBoardFacts.OrbitLine(first));
            Assert.AreEqual("Hangar Desk", campaign.Mission(1).Customer);
            Assert.That(MissionBoardFacts.Of(first, MissionBoardAccess.Open, 0, null, 0, 0, false).Customer, Does.Contain("Hangar Desk"));
            Assert.AreEqual("Hard", MissionBoardFacts.DifficultyWord(campaign.Mission(20).Band));
            Assert.AreEqual("Steady", MissionBoardFacts.DifficultyWord(campaign.Mission(1).Band));

            Sweep("sandbox", MissionBoardFacts.Sandbox());
            Sweep("contracts locked", MissionBoardFacts.Contracts(false, "No contract is posted yet.", 20));
            Sweep("contracts open", MissionBoardFacts.Contracts(true, "KH-21 and up. Seeded on this device. No clock and no expiry. No contract is posted yet.", 20));
            var sand = MissionBoardFacts.Sandbox();
            Assert.AreEqual("Design first", sand.Primary);
            Assert.That(sand.CanGo, Is.True);
            var jobs = MissionBoardFacts.Contracts(true, "No contract is posted yet.", 20);
            Assert.AreEqual("Not posted", jobs.Primary);
            Assert.That(jobs.CanGo, Is.False);
        }

        static void AssertBoard(MissionBoardFrame frame, Size d, bool wide)
        {
            string where = d.Name + (wide ? " wide" : " portrait");
            Assert.That(frame.Wide, Is.EqualTo(wide), where);
            Assert.That(frame.Home.H, Is.GreaterThanOrEqualTo(44f), where + " home");
            Assert.That(frame.Home.W, Is.GreaterThanOrEqualTo(44f), where + " home");
            Assert.That(frame.Contracts.H, Is.GreaterThanOrEqualTo(44f), where + " contracts");
            Assert.That(frame.Sandbox.H, Is.GreaterThanOrEqualTo(44f), where + " sandbox");
            Assert.That(frame.Primary.H, Is.GreaterThanOrEqualTo(44f), where + " primary");
            Assert.That(frame.Primary.W, Is.GreaterThan(80f), where + " primary width");
            Assert.That(frame.Path.H, Is.GreaterThanOrEqualTo(72f), where + " path");
            Assert.That(frame.Art.W, Is.GreaterThanOrEqualTo(40f), where + " portrait");
            Assert.That(frame.Art.H, Is.EqualTo(frame.Art.W).Within(0.6f), where + " square portrait");
            Assert.That(frame.Art.Right, Is.LessThanOrEqualTo(frame.Name.X + 0.6f), where + " portrait covers the name");
            Assert.That(frame.Progress.H, Is.GreaterThanOrEqualTo(6f), where + " act bar");
            Assert.That(frame.Thrum.W, Is.EqualTo(MissionBoardLayout.ThrumPt).Within(0.1f), where);
            if (SharesY(frame.Progress, frame.Thrum))
                Assert.That(frame.Progress.Right, Is.LessThanOrEqualTo(frame.Thrum.X + 0.6f), where + " bar covers Thrum");
            if (SharesY(frame.Title, frame.Thrum))
                Assert.That(frame.Title.Right, Is.LessThanOrEqualTo(frame.Thrum.X + 0.6f), where + " title covers Thrum");
            if (SharesY(frame.Home, frame.Thrum))
                Assert.That(frame.Home.Right, Is.LessThanOrEqualTo(frame.Thrum.X + 0.6f), where + " home covers Thrum");
            Assert.That(frame.Primary.Y, Is.GreaterThanOrEqualTo(frame.Thrum.Bottom - 0.6f), where + " primary covers Thrum");

            var boxes = Blocks(frame);
            NoOverlap(boxes, where);
            Inside(boxes, d, where);
            InsideChild("art", frame.Art, frame.Card, where);
            InsideChild("name", frame.Name, frame.Card, where);
            InsideChild("facts", frame.Facts, frame.Card, where);
            InsideChild("brief", frame.Brief, frame.Card, where);
            InsideChild("primary", frame.Primary, frame.Card, where);
            Assert.That(frame.Art.Bottom, Is.LessThanOrEqualTo(frame.Primary.Y + 0.6f), where + " portrait covers the button");
            Assert.That(frame.Brief.Bottom, Is.LessThanOrEqualTo(frame.Primary.Y + 0.6f), where + " briefing covers the button");
            if (!wide)
                Assert.That(frame.Path.Bottom, Is.LessThanOrEqualTo(frame.Card.Y + 0.6f), where + " path covers the card");
        }

        static List<(string name, HudBox box)> Blocks(MissionBoardFrame f)
        {
            return new List<(string, HudBox)>
            {
                ("home", f.Home), ("title", f.Title), ("progress", f.Progress), ("path", f.Path), ("card", f.Card),
                ("art", f.Art), ("name", f.Name), ("facts", f.Facts), ("brief", f.Brief),
                ("primary", f.Primary), ("contracts", f.Contracts), ("sandbox", f.Sandbox), ("thrum", f.Thrum),
            };
        }

        static bool SkipPair(string a, string b)
        {
            if (Contains(a, b, "card", "art")) return true;
            if (Contains(a, b, "card", "name")) return true;
            if (Contains(a, b, "card", "facts")) return true;
            if (Contains(a, b, "card", "brief")) return true;
            if (Contains(a, b, "card", "primary")) return true;
            if (Contains(a, b, "card", "contracts")) return true;
            if (Contains(a, b, "card", "sandbox")) return true;
            if (Contains(a, b, "header", "home")) return true;
            if (Contains(a, b, "header", "title")) return true;
            return false;
        }

        static bool Contains(string a, string b, string parent, string child) =>
            (a == parent && b == child) || (b == parent && a == child);

        static bool SharesY(HudBox a, HudBox b) => a.Bottom > b.Y + 0.6f && b.Bottom > a.Y + 0.6f;

        static void NoOverlap(List<(string name, HudBox box)> boxes, string where)
        {
            for (int i = 0; i < boxes.Count; i++)
            for (int j = i + 1; j < boxes.Count; j++)
            {
                if (SkipPair(boxes[i].name, boxes[j].name)) continue;
                if (!boxes[i].box.Live || !boxes[j].box.Live) continue;
                Assert.That(boxes[i].box.Overlaps(boxes[j].box), Is.False,
                    where + " " + boxes[i].name + " overlaps " + boxes[j].name
                    + " " + boxes[i].box.X + "," + boxes[i].box.Y + " " + boxes[i].box.W + "x" + boxes[i].box.H
                    + " vs " + boxes[j].box.X + "," + boxes[j].box.Y + " " + boxes[j].box.W + "x" + boxes[j].box.H);
            }
        }

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

        static void InsideChild(string name, HudBox child, HudBox parent, string where)
        {
            Assert.That(child.X, Is.GreaterThanOrEqualTo(parent.X - 0.6f), where + " " + name);
            Assert.That(child.Right, Is.LessThanOrEqualTo(parent.Right + 0.6f), where + " " + name);
            Assert.That(child.Y, Is.GreaterThanOrEqualTo(parent.Y - 0.6f), where + " " + name);
            Assert.That(child.Bottom, Is.LessThanOrEqualTo(parent.Bottom + 0.6f), where + " " + name);
        }

        static void Sweep(string where, MissionBoardFacts card)
        {
            foreach (string line in card.Visible())
            {
                Assert.That(PlayerText.Leak(line), Is.False, where + " " + line);
                Assert.That(RawId.IsMatch(line), Is.False, where + " " + line);
                Assert.That(line, Does.Not.Contain("achievable"));
            }
        }
    }
}
