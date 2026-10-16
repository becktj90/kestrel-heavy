// Portrait layout: one menu box, no clipped guide line, SKIP clear of the text, crisp glyph scale.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Game.Guide.Tests
{
    public class GuideLayoutTests
    {
        static string Find(string relative)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                string path = Path.Combine(dir.FullName, relative);
                if (File.Exists(path)) return path;
                dir = dir.Parent;
            }
            Assert.Fail("missing " + relative);
            return relative;
        }

        static GlyphTable Glyphs() => GlyphTable.Parse(File.ReadAllText(Find("unity/Assets/Resources/Guide/glyphs.json")));

        [Test]
        public void NormalGlyphsFitAboutThirtyCharactersOnSe()
        {
            var se = Se();
            float textW = GuideLayout.TextWidth(se.ColumnWidth, true, false);
            float glyph = GuidePacing.GlyphPt(GuidePacing.FontPt(SubtitleSize.Normal));
            Assert.AreEqual(7f, glyph);
            Assert.AreEqual(1, GuidePacing.PixelScale(15f));
            Assert.AreEqual(2, GuidePacing.PixelScale(19f));
            Assert.AreEqual(3, GuidePacing.PixelScale(23f));
            float advance = GlyphLayout.Advance(glyph);
            Assert.AreEqual(6f, advance);
            Assert.AreEqual(0f, advance % 1f);
            int chars = (int)Math.Floor(textW / advance);
            Assert.That(chars, Is.InRange(28, 32), "SE text column " + textW);

            var quads = new List<GlyphLayout.Quad>();
            GlyphLayout.Layout("Hangar keeper. g0 is 9.80665 m/s^2.", textW, glyph, Glyphs(), false, quads);
            Assert.Greater(quads.Count, 8);
            for (int i = 0; i < quads.Count; i++)
            {
                Assert.AreEqual(Math.Round(quads[i].X), quads[i].X, 0.001, "x");
                Assert.AreEqual(Math.Round(quads[i].Y), quads[i].Y, 0.001, "y");
                Assert.AreEqual(5f, quads[i].W);
                Assert.AreEqual(7f, quads[i].H);
                Assert.That(quads[i].X + quads[i].W, Is.LessThanOrEqualTo(textW + 0.01f));
            }
        }

        [Test]
        public void LongestGuideLinesDoNotClipOnSeProMaxOrIpad()
        {
            var lines = Lines();
            Assert.That(lines[0].Length, Is.GreaterThanOrEqualTo(80));
            var table = Glyphs();
            var devices = new[]
            {
                new object[] { "iPhone SE", Se() },
                new object[] { "iPhone 16 Pro Max", ProMax() },
                new object[] { "iPad Pro 13", Ipad() },
            };
            foreach (SubtitleSize size in new[] { SubtitleSize.Normal, SubtitleSize.Large, SubtitleSize.ExtraLarge })
            {
                float font = GuidePacing.FontPt(size);
                foreach (object[] row in devices)
                {
                    string name = (string)row[0];
                    var screen = (GuideScreen)row[1];
                    Assert.Greater(screen.Height, screen.Width, name + " is portrait");
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string text = lines[i];
                        var frames = GuideLayout.Place(screen, new[] { true, true }, new[] { text, text }, font, GuidePlace.Menu);
                        Assert.AreEqual(1, frames.Length, name);
                        var frame = frames[0];
                        string where = name + " " + size + " [" + text.Length + "]";
                        Assert.IsFalse(GuideLayout.Clipped(frame, text), where);
                        Assert.IsTrue(GuideLayout.ClearsSkip(frame), where);
                        Assert.That(frame.Font, Is.GreaterThanOrEqualTo(15f), where);
                        Assert.That(frame.Skip, Is.GreaterThanOrEqualTo(44f), where);
                        Assert.That(frame.Glyph % 7f, Is.EqualTo(0f).Within(0.01f), where);
                        float floor = screen.Height - screen.SafeBottom - GuidePacing.MenuFooterPt;
                        Assert.That(frame.Bottom + GuidePacing.ShadowPt, Is.LessThanOrEqualTo(floor + 0.05f), where);
                        Assert.That(frame.Y, Is.GreaterThanOrEqualTo(screen.DockTop - 0.01f), where);
                        AssertQuadsInside(table, text, frame, where);
                    }
                }
            }
        }

        [Test]
        public void MeasuredFooterSitsOnTheHomeKeyWithoutCountingTheSafeAreaTwice()
        {
            var phone = ProMax();
            float button = phone.SafeBottom + 44f;
            var screen = phone.WithFooter(button);
            var frames = GuideLayout.Place(screen, new[] { true }, new[] { "Hangar keeper. g0 is 9.80665 m/s^2." }, 15f, GuidePlace.Menu);
            Assert.AreEqual(1, frames.Length);
            Assert.That(frames[0].Bottom + GuidePacing.ShadowPt, Is.EqualTo(phone.Height - button).Within(0.05f));
            float countedTwice = phone.Height - phone.SafeBottom - button - GuidePacing.ShadowPt;
            Assert.That(frames[0].Bottom, Is.GreaterThan(countedTwice + 1f));
        }

        [Test]
        public void MenuBoxClearsTheHomeKeyAndAscentStillStacksTwo()
        {
            var se = Se();
            var menu = GuideLayout.Place(se, new[] { true, true }, new[] { "First card.", "Second card." }, 15f, GuidePlace.Menu);
            Assert.AreEqual(1, menu.Length);
            Assert.That(menu[0].Bottom + GuidePacing.ShadowPt, Is.LessThanOrEqualTo(se.Height - se.SafeBottom - GuidePacing.MenuFooterPt + 0.05f));

            var ascent = GuideLayout.Place(se, new[] { false, true }, new[] { "Max-Q in five.", "Ease off." }, 15f, GuidePlace.Ascent);
            Assert.AreEqual(2, ascent.Length);
            Assert.That(ascent[0].Y, Is.GreaterThanOrEqualTo(se.DockTop - 0.01f));
        }

        [Test]
        public void DriverSavesSeenLinesAndHidesThrumOnAscent()
        {
            string driver = File.ReadAllText(Find("unity/Assets/Kestrel.Game/Guide/Runtime/GuideBoxDriver.cs"));
            Assert.That(driver, Does.Contain("GuideHost.Save"));
            Assert.That(driver, Does.Contain("GuideSeenWatch"));
            Assert.That(driver, Does.Contain("GuidePopLayout"));
            Assert.That(driver, Does.Not.Contain("mission-scroll"));
            string shell = File.ReadAllText(Find("unity/Assets/Kestrel.Game/Shell/AppShell.cs"));
            Assert.That(shell, Does.Contain("GuidePlace.Menu"));
            string binder = File.ReadAllText(Find("unity/Assets/Kestrel.Slice/Runtime/Guide/GuideBinder.cs"));
            Assert.That(binder, Does.Not.Contain("GuideHooks.Ascent"));
            Assert.That(binder, Does.Not.Contain("FlightBus"));
            Assert.That(binder, Does.Contain("GuidePlace.Menu"));
            Assert.That(binder, Does.Contain("GuidePlace.Ascent"));
            Assert.That(binder, Does.Contain("SetInFlight(true)"));
            Assert.That(driver, Does.Contain("GuideClipPlayer.InFlight"));
        }

        static void AssertQuadsInside(GlyphTable table, string text, GuideFrame frame, string where)
        {
            var quads = new List<GlyphLayout.Quad>();
            GlyphLayout.Layout(text, frame.TextW, frame.Glyph, table, false, quads);
            float maxX = 0f, maxY = 0f;
            for (int i = 0; i < quads.Count; i++)
            {
                float right = quads[i].X + quads[i].W;
                float bottom = quads[i].Y + quads[i].H;
                if (right > maxX) maxX = right;
                if (bottom > maxY) maxY = bottom;
                Assert.AreEqual(Math.Round(quads[i].X), quads[i].X, 0.001, where);
            }
            Assert.That(maxX, Is.LessThanOrEqualTo(frame.TextW + 0.51f), where);
            if (!frame.Scroll)
                Assert.That(maxY, Is.LessThanOrEqualTo(frame.Body + 0.51f), where);
            else
                Assert.That(frame.Body, Is.GreaterThanOrEqualTo(GlyphLayout.LineHeight(frame.Glyph) - 0.01f), where);
        }

        static List<string> Lines()
        {
            var root = MiniJson.ParseObject(File.ReadAllText(Find("data/guide.json")));
            var list = new List<string>();
            var rows = root.Arr("lines");
            Assert.IsNotNull(rows);
            for (int i = 0; i < rows.Count; i++)
            {
                var line = rows[i] as JsonObject;
                if (line == null) continue;
                var cards = line.Arr("cards");
                if (cards == null) continue;
                for (int c = 0; c < cards.Count; c++)
                {
                    var card = cards[c] as JsonObject;
                    if (card == null) continue;
                    Add(list, card.Str("kid"));
                    Add(list, card.Str("eng"));
                }
            }
            list.Sort((a, b) => b.Length.CompareTo(a.Length));
            return list;
        }

        static void Add(List<string> list, string text)
        {
            text = Widen(text);
            if (string.IsNullOrEmpty(text)) return;
            list.Add(text);
        }

        static string Widen(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '{') { sb.Append(text[i]); continue; }
                int j = text.IndexOf('}', i + 1);
                if (j < 0) { sb.Append(text[i]); continue; }
                sb.Append("00000");
                i = j;
            }
            return sb.ToString();
        }

        static GuideScreen Se()
            => GuideScreen.FromPixels(750f, 1334f, 0f, 0f, 750f, 1294f, 2f, 0f, false);

        static GuideScreen ProMax()
            => GuideScreen.FromPixels(1290f, 2796f, 0f, 102f, 1290f, 2796f - 102f - 177f, 3f, 0f, false);

        static GuideScreen Ipad()
            => GuideScreen.FromPixels(2064f, 2752f, 0f, 40f, 2064f, 2752f - 40f - 48f, 2f, 0f, false);
    }
}
