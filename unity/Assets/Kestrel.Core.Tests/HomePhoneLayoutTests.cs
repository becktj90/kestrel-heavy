// Phone-column Home (TestFlight 156/158, iPhone portrait): no block on another, every line inside its box.
// Sizes are the shell panel each device really gets (375x667 reference matched to height), with a save present
// (CONTINUE in the top band), credits shown and a long career record.
using System;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HomePhoneLayoutTests
    {
        static readonly (string name, float w, float h, float top, float bottom)[] Devices =
        {
            ("375x667 SE", 375f, 667f, 20f, 0f),
            ("390x844", 390f, 844f, 47f, 34f),
            ("393x852 Pro", 393f, 852f, 59f, 34f),
            ("430x932 Pro Max", 430f, 932f, 59f, 34f),
            ("820x1180 iPad portrait", 820f, 1180f, 24f, 20f),
        };

        const string LongRecord = "20 flights cleared · 888.88M Kc · 20 patches";
        const string Thrum = "Oh! A visitor. Mind the oil. I am Thrum. I keep this hangar, and I talk to wrenches.";

        static HomePhoneFrame Solve(int i, string record = LongRecord)
        {
            var d = Devices[i];
            return HomePhoneLayout.Solve(HomePhoneLayout.Device(d.w, d.h, d.top, d.bottom), true, record, "KH-3 · Tiny Orbit", Thrum);
        }

        [Test]
        public void IphonesAreANarrowShellPanel()
        {
            var s = HomePhoneLayout.Device(390f, 844f, 47f, 34f);
            Assert.That(s.H, Is.EqualTo(667f));
            Assert.That(s.W, Is.EqualTo(390f * 667f / 844f).Within(0.01f), "match-height: a 390x844 phone is ~308 wide");
            Assert.That(HomePhoneLayout.Device(375f, 667f, 20f, 0f).W, Is.EqualTo(375f).Within(0.01f));
        }

        [Test]
        public void HomeBlocksDoNotOverlapAndStayInTheSafeColumn()
        {
            for (int i = 0; i < Devices.Length; i++)
            {
                var f = Solve(i);
                string n = Devices[i].name;
                var blocks = new (string, HomeRect)[]
                {
                    ("credits+CONTINUE", f.Bar), ("gear", f.Gear), ("title", f.Title), ("stats", f.Stats), ("LAUNCH", f.Launch),
                    ("DESIGN FIRST", f.Design), ("Missions", f.Missions), ("Codex", f.Codex), ("Thrum", f.Thrum), ("guide pop", f.Pop),
                };
                for (int a = 0; a < blocks.Length; a++)
                    for (int b = a + 1; b < blocks.Length; b++)
                        Assert.That(blocks[a].Item2.Overlaps(blocks[b].Item2), Is.False,
                            $"{n}: {blocks[a].Item1} {R(blocks[a].Item2)} on {blocks[b].Item1} {R(blocks[b].Item2)}");
                foreach (var (id, r) in blocks)
                {
                    if (id == "gear") continue;   // the gear corner is panel-relative (HomeCornerLayout), checked below
                    Assert.That(r.X, Is.GreaterThanOrEqualTo(f.Safe.X - 0.01f), n + " " + id + " left");
                    Assert.That(r.Right, Is.LessThanOrEqualTo(f.Safe.Right + 0.01f), n + " " + id + " right");
                    Assert.That(r.Y, Is.GreaterThanOrEqualTo(f.Safe.Y - 0.01f), n + " " + id + " top");
                    Assert.That(r.Bottom, Is.LessThanOrEqualTo(f.Safe.Bottom + 0.01f), n + " " + id + " bottom");
                }
                Assert.That(f.Gear.Y, Is.EqualTo(f.Safe.Y).Within(0.01f), n + ": gear sits in the credits band, not the status bar");
                Assert.That(f.Gear.Right, Is.LessThanOrEqualTo(f.Safe.Right + 2.5f), n + " gear inside the column edge");
                Assert.That(f.Bar.Bottom, Is.LessThanOrEqualTo(f.Eyebrow.Y + 0.01f), n + ": credits band clears the eyebrow");
                Assert.That(f.Hero, Is.GreaterThanOrEqualTo(f.Pop.H + 8f), n + ": room for the rocket and the guide pop under the title");
            }
        }

        [Test]
        public void HomeTextFitsItsBoxes()
        {
            for (int i = 0; i < Devices.Length; i++)
            {
                var f = Solve(i);
                var t = f.Type;
                string n = Devices[i].name;
                float col = f.Stats.W;
                // Credits chip + CONTINUE share the band at a readable size.
                float bandNeed = HomeTypeMetrics.Need(HomeFace.GroteskMedium, "99999 credits", 14f)
                    + HomeTypeMetrics.Need(HomeFace.FrauncesBold, "CONTINUE", 14f) + 24f + 2f + 8f + 2f;
                Assert.That(bandNeed, Is.LessThanOrEqualTo(f.Bar.W), n + " credits and CONTINUE at 14 px in " + f.Bar.W);
                // Stats: exactly two lines, each on one line.
                float statInner = col - 2f * HomePhoneLayout.StatsBorder - 2f * HomePhoneLayout.StatsPadX;
                Assert.That(HomeTypeMetrics.Fits(HomeFace.GroteskMedium, HomePhoneLayout.FuelLine, f.StatFont, statInner), n + " pills");
                Assert.That(f.RecordLines, Is.EqualTo(1), n + " record on one line: " + f.Record + " @" + f.StatFont);
                Assert.That(f.Stats.H, Is.GreaterThanOrEqualTo(2f * f.StatFont * HomePhoneLayout.LineH + 2f * HomePhoneLayout.StatsBorder - 0.01f), n + " two lines in the card");
                Assert.That(f.StatFont, Is.GreaterThanOrEqualTo(12f), n);
                // LAUNCH and DESIGN FIRST hold every wrapped caption line.
                float launchContent = f.Launch.H - HomePhoneLayout.LaunchBorderV - 2f * HomePhoneLayout.LaunchPadV(t.Scale);
                Assert.That(launchContent, Is.GreaterThanOrEqualTo(t.LaunchLabel * HomePhoneLayout.LineH + f.LaunchCaptionLines * t.LaunchCaption * HomePhoneLayout.LineH - 0.01f), n + " LAUNCH caption");
                Assert.That(HomeTypeMetrics.Fits(HomeFace.FrauncesBlack, "LAUNCH", t.LaunchLabel, col - 2f * HomePhoneLayout.LaunchPadH(t.Scale) - HomePhoneLayout.LaunchChromeX, 0.10f), n + " LAUNCH label");
                float designContent = f.Design.H - 2f * HomePhoneLayout.DesignBorder - 2f * HomePhoneLayout.DesignPadY;
                int capLines = HomeTypeMetrics.Lines(HomeFace.GroteskMedium, "Your rocket, this mission.", t.DesignCaption, col - HomePhoneLayout.DesignChromeX);
                Assert.That(designContent, Is.GreaterThanOrEqualTo(t.DesignLabel * HomePhoneLayout.LineH + capLines * t.DesignCaption * HomePhoneLayout.LineH - 0.01f), n + " DESIGN FIRST caption not clipped");
                Assert.That(HomeTypeMetrics.Fits(HomeFace.FrauncesBold, "DESIGN FIRST", t.DesignLabel, col - HomePhoneLayout.DesignChromeX, 0.03f), n + " DESIGN FIRST label");
                // Tiles: name clear of glyph and lamp, captions above the bottom edge.
                foreach (var (name, live, caption) in new[] { ("Missions", true, "Pick any flight you've unlocked."), ("Codex", false, "Parts, places and flight notes.") })
                {
                    float size = HomePhoneLayout.TileName(t, col, name, live);
                    float avail = HomePhoneLayout.TileInner(t, col) - HomePhoneLayout.TileIcon - (live ? HomePhoneLayout.TileLamp : 0f);
                    Assert.That(HomeTypeMetrics.Need(HomeFace.FrauncesBold, name, size, 0.03f), Is.LessThanOrEqualTo(avail), n + " " + name + " beside its glyph and lamp");
                    int lines = HomeTypeMetrics.Lines(HomeFace.GroteskMedium, caption, t.TileCaption, HomePhoneLayout.TileInner(t, col));
                    float content = f.Missions.H - 2f * HomePhoneLayout.TileBorder - 2f * t.Pad - Math.Max(22f, t.TileName * HomePhoneLayout.LineH) - 3f;
                    Assert.That(content - lines * t.TileCaption * HomePhoneLayout.LineH, Is.GreaterThanOrEqualTo(HomePhoneLayout.TileAir - 0.01f), n + " " + name + " caption off the bottom edge");
                }
                // Thrum: the name row and every line of what the strip shows (the whole line, or the short teaser
                // when the whole one needs three lines; the tap note keeps the whole line).
                Assert.That(f.ThrumShown, Is.EqualTo(Thrum).Or.EqualTo(HomePhoneLayout.ThrumShort), n);
                int thrumLines = HomeTypeMetrics.Lines(HomeFace.GroteskMedium, f.ThrumShown, t.Hint, HomePhoneLayout.ThrumInner(col));
                Assert.That(thrumLines, Is.LessThanOrEqualTo(2), n + " Thrum strip is two lines at most");
                Assert.That(f.Thrum.H - 2f * HomePhoneLayout.ThrumPadY, Is.GreaterThanOrEqualTo((1 + thrumLines) * t.Hint * HomePhoneLayout.LineH - 0.01f), n + " Thrum is not cut mid-line");
            }
        }

        [Test]
        public void RecordWritesKilocreditsCompactly()
        {
            Assert.That(HomeRecord.Line(2, 8878502L * 100000L, 100000, 2), Is.EqualTo("2 flights cleared · 8.88M Kc · 2 patches"));
            Assert.That(HomeRecord.Kc(34100), Is.EqualTo("34.1k Kc"));
            Assert.That(HomeRecord.Kc(850), Is.EqualTo("850 Kc"));
            Assert.That(HomeRecord.Kc(1250000000L), Is.EqualTo("1.25B Kc"));
            Assert.That(HomeRecord.Short("2 flights cleared · 8.88M Kc · 2 patches"), Is.EqualTo("2 cleared · 8.88M Kc · 2 patches"));
            Assert.That(HomeRecord.Short("1 flight cleared · 1 Kc · 1 patch"), Is.EqualTo("1 cleared · 1 Kc · 1 patch"));
        }

        [Test]
        public void WordWrapBreaksAtSpaces()
        {
            float one = HomeTypeMetrics.Need(HomeFace.GroteskMedium, "Pick any flight you've unlocked.", 14f);
            Assert.That(HomeTypeMetrics.Lines(HomeFace.GroteskMedium, "Pick any flight you've unlocked.", 14f, one + 1f), Is.EqualTo(1));
            Assert.That(HomeTypeMetrics.Lines(HomeFace.GroteskMedium, "Pick any flight you've unlocked.", 14f, one * 0.6f), Is.EqualTo(2));
            Assert.That(HomeTypeMetrics.Width(HomeFace.FrauncesBlack, "KESTREL", 100f, 0.04f), Is.EqualTo(HomeWideText.Kestrel).Within(0.5f), "tables match the menu metrics");
            Assert.That(HomeTypeMetrics.Width(HomeFace.GroteskMedium, "Next flight, then the pad.", 100f), Is.EqualTo(HomeWideText.LaunchCaption).Within(0.5f));
            string cut = HomeTypeMetrics.Clip(HomeFace.GroteskMedium, Thrum, 14f, 120f, 2);
            Assert.That(cut, Does.EndWith("..."));
            Assert.That(HomeTypeMetrics.Lines(HomeFace.GroteskMedium, cut, 14f, 120f), Is.LessThanOrEqualTo(2));
        }

        static string R(HomeRect r) => $"({r.X:0.#},{r.Y:0.#} {r.W:0.#}x{r.H:0.#})";
    }
}
