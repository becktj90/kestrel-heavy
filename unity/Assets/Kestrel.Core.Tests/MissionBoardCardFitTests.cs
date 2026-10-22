// t461 (TestFlight 204 campaign map, ~471x1024): the mission card clipped "Park a 20 kg satellite in a 2__ km orbit"
// and the Need / Difficulty rows with no way to scroll, "Act I · First Light" lost its second line under the act bar,
// and Patches sat on the KH-2 node. The layout math behind the fix, at the five sizes the device tests draw (335x720,
// 375x667, 430x932, 471x1024 portrait and 1280x800 web), at literal points, under a 59/34 notch and in the iOS shell
// panel (375x667 reference matched to height), for every card the board can show.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissionBoardCardFitTests
    {
        static readonly string[] Acts = { "Act I · First Light", "Act II · Regulars", "Act III · Heavy", "Act IV · Far Shore" };

        struct Size
        {
            public string Name;
            public float W, H, Top, Bottom, Left, Right;
            public bool Wide;
        }

        static IEnumerable<Size> Sizes()
        {
            foreach (var (w, h) in new[] { (335f, 720f), (375f, 667f), (430f, 932f), (471f, 1024f) })
            {
                string n = w + "x" + h;
                yield return new Size { Name = n, W = w, H = h };
                yield return new Size { Name = n + " notch", W = w, H = h, Top = 59f, Bottom = 34f };
                var panel = HomePhoneLayout.Device(w, h, 59f, 34f);
                yield return new Size { Name = n + " shell panel", W = panel.W, H = panel.H, Top = panel.Top, Bottom = panel.Bottom };
            }
            // Trevor's t461 frame: the 471x1024 shot's game area is a ~329x667 panel.
            yield return new Size { Name = "t461 329x667 panel", W = 329f, H = 667f };
            yield return new Size { Name = "1280x800 web", W = 1280f, H = 800f, Wide = true };
            yield return new Size { Name = "1280x800 web FULL", W = 1280f, H = 800f, Top = HomeCornerLayout.ExitH + 4f, Wide = true };
        }

        static List<MissionBoardCopy> Copies()
        {
            var campaign = Campaign.Load(Golden.Root);
            var list = new List<MissionBoardCopy>();
            foreach (var m in campaign.Missions)
            {
                string act = Acts[Math.Min(3, (m.Index - 1) / 5)];
                list.Add(MissionBoardCopy.Of(MissionBoardFacts.Of(m, MissionBoardAccess.Open, 0, null, 0, 0, true), act));
                list.Add(MissionBoardCopy.Of(MissionBoardFacts.Of(m, MissionBoardAccess.Locked, 0, null, 0, 0, false), act));
                list.Add(MissionBoardCopy.Of(MissionBoardFacts.Of(m, MissionBoardAccess.Cleared, 3, new[] { "gold", "silver", "bronze" }, 123456, 12, true), act));
            }
            list.Add(MissionBoardCopy.Of(MissionBoardFacts.Sandbox(), Acts[0]));
            list.Add(MissionBoardCopy.Of(MissionBoardFacts.Contracts(true, "KH-21 and up. Seeded on this device. No clock and no expiry. No contract is posted yet.", 20), Acts[0]));
            list.Add(MissionBoardCopy.Of(MissionBoardFacts.Contracts(false, "No contract is posted yet.", 20), Acts[0]));
            return list;
        }

        static MissionBoardFrame Frame(Size d, List<MissionBoardCopy> copies) =>
            MissionBoardLayout.Place(d.W, d.H, d.Top, d.Bottom, d.Left, d.Right, d.Wide, copies);

        static bool Whole(float v) => Math.Abs(v - Math.Round(v)) < 0.001f;

        static void AssertWhole(HudBox b, string what)
        {
            Assert.That(Whole(b.X) && Whole(b.Y) && Whole(b.W) && Whole(b.H), Is.True,
                what + " not on whole points " + b.X + "," + b.Y + " " + b.W + "x" + b.H);
        }

        [Test]
        public void EveryCardFitsTheSheetOrScrollsWithTheKeysPinned()
        {
            var copies = Copies();
            foreach (var d in Sizes())
            {
                var f = Frame(d, copies);
                string where = d.Name;
                float minY = d.Top - 0.01f, maxY = d.H - d.Bottom + 0.01f;
                foreach (var (name, key) in new[] { ("contracts", f.Contracts), ("sandbox", f.Sandbox), ("primary", f.Primary) })
                {
                    Assert.That(key.H, Is.GreaterThanOrEqualTo(44f), where + " " + name);
                    Assert.That(key.Y, Is.GreaterThanOrEqualTo(f.Body.Bottom + MissionBoardLayout.Gap - 0.01f), where + " " + name + " under the body");
                    Assert.That(key.Bottom, Is.LessThanOrEqualTo(Math.Min(maxY, f.Card.Bottom) + 0.01f), where + " " + name + " inside");
                    Assert.That(key.Y, Is.GreaterThanOrEqualTo(minY), where + " " + name);
                    Assert.That(key.X, Is.GreaterThanOrEqualTo(d.Left - 0.01f), where + " " + name);
                    Assert.That(key.Right, Is.LessThanOrEqualTo(d.W - d.Right + 0.01f), where + " " + name);
                }
                Assert.That(f.Body.Y, Is.GreaterThanOrEqualTo(f.Card.Y), where);
                Assert.That(f.Body.H, Is.GreaterThanOrEqualTo(40f), where + " body viewport");
                AssertWhole(f.Card, where + " card");
                AssertWhole(f.Body, where + " body");
                AssertWhole(f.Path, where + " path");

                // The sheet takes the tallest card when the map keeps its minimum; otherwise the map is at its minimum.
                float tallest = 0f;
                foreach (var c in copies) tallest = Math.Max(tallest, MissionBoardLayout.SheetNeed(f.Card.W, c));
                if (!d.Wide && f.Card.H + 0.5f < tallest)
                    Assert.That(f.Path.H, Is.LessThanOrEqualTo(MissionBoardLayout.MinPath + 1f), where + " sheet short of its copy while the map has room");

                foreach (var c in copies)
                {
                    var b = MissionBoardLayout.Body(f, c);
                    string w2 = where + " " + c.Name;
                    // Every block is its lines times a whole-pixel line box: the full brief, both fact rows.
                    Assert.That(b.Name.H, Is.EqualTo(b.NameLines * MissionBoardLayout.LineBox(MissionBoardLayout.NamePt)).Within(0.01f), w2);
                    Assert.That(b.Brief.H, Is.EqualTo(b.BriefLines * MissionBoardLayout.LineBox(MissionBoardLayout.BodyPt)).Within(0.01f), w2);
                    Assert.That(b.Facts.H, Is.EqualTo(b.FactsLines * MissionBoardLayout.LineBox(MissionBoardLayout.BodyPt)).Within(0.01f), w2);
                    Assert.That(b.BriefLines, Is.EqualTo(MissionBoardLayout.Lines(c.Brief, b.Brief.W, MissionBoardLayout.BodyPt)), w2);
                    Assert.That(b.FactsLines, Is.GreaterThanOrEqualTo(2), w2 + " Need and Difficulty rows");
                    Assert.That(MissionBoardLayout.LineBox(MissionBoardLayout.BodyPt), Is.EqualTo((float)Math.Ceiling(MissionBoardLayout.BodyPt * 1.3f)));
                    foreach (var (name, box) in new[] { ("art", b.Art), ("name", b.Name), ("customer", b.Customer), ("orbit", b.Orbit), ("brief", b.Brief), ("facts", b.Facts) })
                    {
                        AssertWhole(box, w2 + " " + name);
                        Assert.That(box.X, Is.GreaterThanOrEqualTo(-0.01f), w2 + " " + name);
                        Assert.That(box.Right, Is.LessThanOrEqualTo(f.Body.W + 0.01f), w2 + " " + name);
                        Assert.That(box.Bottom, Is.LessThanOrEqualTo(b.ContentH + 0.01f), w2 + " " + name + " past the content");
                    }
                    var blocks = new[] { ("art", b.Art), ("name", b.Name), ("customer", b.Customer), ("orbit", b.Orbit), ("brief", b.Brief), ("facts", b.Facts) };
                    for (int i = 0; i < blocks.Length; i++)
                        for (int j = i + 1; j < blocks.Length; j++)
                            Assert.That(blocks[i].Item2.Overlaps(blocks[j].Item2), Is.False, w2 + " " + blocks[i].Item1 + " on " + blocks[j].Item1);
                    // Fits, or scrolls by exactly the overflow (the ScrollView's range is ContentH - ViewH).
                    Assert.That(b.ViewH, Is.EqualTo(f.Body.H).Within(0.01f), w2);
                    if (b.Scrolls) Assert.That(b.Overflow, Is.EqualTo(b.ContentH - f.Body.H).Within(0.01f), w2);
                    else Assert.That(b.ContentH, Is.LessThanOrEqualTo(f.Body.H + 0.5f), w2);
                }
            }
        }

        [Test]
        public void TrevorsShotSizesShowTheWholeKh3CardWithoutScrolling()
        {
            var copies = Copies();
            var campaign = Campaign.Load(Golden.Root);
            var kh3 = MissionBoardCopy.Of(MissionBoardFacts.Of(campaign.Mission(3), MissionBoardAccess.Open, 0, null, 0, 0, true), Acts[0]);
            Assert.That(kh3.Brief, Does.Contain("20 kg"));
            var whole = new HashSet<string> { "335x720", "375x667", "430x932", "430x932 notch", "471x1024", "471x1024 notch", "t461 329x667 panel", "1280x800 web", "1280x800 web FULL" };
            foreach (var d in Sizes())
            {
                if (!whole.Contains(d.Name)) continue;
                var f = Frame(d, copies);
                var b = MissionBoardLayout.Body(f, kh3);
                Assert.That(b.Scrolls, Is.False, d.Name + " KH-3 needs " + b.ContentH + " in a " + f.Body.H + " body");
            }
        }

        [Test]
        public void HeaderRowsHoldTheActTitleAndPatchesAboveTheMap()
        {
            var copies = Copies();
            foreach (var d in Sizes())
            {
                var f = Frame(d, copies);
                string where = d.Name;
                float lb = MissionBoardLayout.LineBox(f.ActFontPt);
                Assert.That(f.ActFontPt, Is.GreaterThanOrEqualTo(MissionBoardLayout.ActMinPt), where);
                Assert.That(f.ActLines, Is.InRange(1, 2), where);
                Assert.That(f.Act.H, Is.EqualTo(f.ActLines * lb).Within(0.01f), where + " act line boxes");
                Assert.That(f.Kicker.H, Is.EqualTo(MissionBoardLayout.LineBox(MissionBoardLayout.KickerPt)).Within(0.01f), where);
                foreach (var act in Acts)
                    Assert.That(MissionBoardLayout.Lines(act, f.Act.W, f.ActFontPt), Is.LessThanOrEqualTo(f.ActLines), where + " " + act);
                Assert.That(f.Act.Y, Is.GreaterThanOrEqualTo(f.Kicker.Bottom - 0.01f), where);
                Assert.That(f.Progress.Y, Is.GreaterThanOrEqualTo(f.Act.Bottom - 0.01f), where + " act bar under the title");
                foreach (var (name, box) in new[] { ("kicker", f.Kicker), ("act", f.Act), ("progress", f.Progress), ("home", f.Home), ("patches", f.Patches), ("thrum", f.Thrum) })
                {
                    AssertWhole(box, where + " " + name);
                    Assert.That(box.Bottom, Is.LessThanOrEqualTo(f.Path.Y + 0.01f), where + " " + name + " reaches into the map");
                    Assert.That(box.Y, Is.GreaterThanOrEqualTo(d.Top - 0.01f), where + " " + name);
                    Assert.That(box.X, Is.GreaterThanOrEqualTo(d.Left - 0.01f), where + " " + name);
                    Assert.That(box.Right, Is.LessThanOrEqualTo(d.W - d.Right + 0.01f), where + " " + name);
                }
                var head = new[] { ("kicker", f.Kicker), ("act", f.Act), ("progress", f.Progress), ("home", f.Home), ("patches", f.Patches), ("thrum", f.Thrum) };
                for (int i = 0; i < head.Length; i++)
                    for (int j = i + 1; j < head.Length; j++)
                    {
                        if (head[i].Item1 == "kicker" && head[j].Item1 == "act") continue;
                        Assert.That(head[i].Item2.Overlaps(head[j].Item2), Is.False, where + " " + head[i].Item1 + " on " + head[j].Item1);
                    }
                Assert.That(f.Patches.W, Is.GreaterThanOrEqualTo(44f), where);
                Assert.That(f.Patches.H, Is.GreaterThanOrEqualTo(44f), where);
                Assert.That(f.Patches.Overlaps(f.Path), Is.False, where + " Patches over the map");
                Assert.That(f.Patches.Overlaps(f.Card), Is.False, where + " Patches over the card");
                // "Patches" (13 pt) fits its key.
                Assert.That(FlightHudLayout.BudgetWidth("Patches", 13f), Is.LessThanOrEqualTo(f.Patches.W - 8f), where);
            }
        }
    }
}
