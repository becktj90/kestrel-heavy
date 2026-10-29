using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Game.Guide.Tests
{
    public class GuideCoreTests
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

        static NarrationSubtitles Clips() => NarrationSubtitles.Parse(File.ReadAllText(Find("unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/manifest.json")));

        [Test]
        public void CreamOnInkClearsTwelveToOne()
        {
            float ratio = GuideColors.Contrast(GuideColors.Cream, GuideColors.Ink);
            Assert.That(ratio, Is.GreaterThanOrEqualTo(GuideColors.MinContrast));
            Assert.That(GuideColors.Contrast(GuideColors.Amber, GuideColors.Ink), Is.GreaterThanOrEqualTo(4.5f));
            Assert.AreEqual(GuideColors.Ink, GuideColors.Hex("#1b1f2a"));
            Assert.AreEqual(GuideColors.Cream, GuideColors.Hex("#faf0d7"));
        }

        [Test]
        public void OwnGlyphAtlasCoversAsciiAndTheGear()
        {
            var table = Glyphs();
            Assert.AreEqual(96, table.Count);
            Assert.AreEqual(96, table.AtlasW);
            Assert.AreEqual(48, table.AtlasH);
            for (int c = 32; c < 127; c++) Assert.IsTrue(table.Contains((char)c), "missing " + (char)c);
            Assert.IsTrue(table.Contains('\u2699'));
            Assert.IsTrue(table.Covers("Oh! A visitor! Mind the oil."));
            Assert.IsFalse(table.Covers("caf\u00e9"));
            Assert.IsTrue(table.TryMask('A', out int index, out byte r0, out _, out _, out _, out _, out _, out _));
            Assert.Greater(index, 0);
            Assert.AreNotEqual(0, r0);
        }

        [Test]
        public void AtlasPngMatchesTheGlyphTable()
        {
            var table = Glyphs();
            byte[] png = PngRgba.Decode(File.ReadAllBytes(Find("unity/Assets/Resources/Guide/atlas.png")), out int w, out int h);
            Assert.AreEqual(table.AtlasW, w);
            Assert.AreEqual(table.AtlasH, h);
            int inked = 0;
            for (int i = 0; i < table.Count; i++)
            {
                int col = i % table.Cols;
                int row = i / table.Cols;
                for (int gy = 0; gy < table.GlyphH; gy++)
                {
                    byte mask = table.Row(i, gy);
                    for (int gx = 0; gx < table.GlyphW; gx++)
                    {
                        bool on = (mask & (1 << (4 - gx))) != 0;
                        int x = col * table.CellW + gx;
                        int y = row * table.CellH + gy;
                        int a = png[((y * w) + x) * 4 + 3];
                        if (on) { Assert.AreEqual(255, a, "glyph " + i + " pixel"); inked++; }
                        else Assert.AreEqual(0, a);
                    }
                }
            }
            Assert.Greater(inked, 200);
        }

        [Test]
        public void PortraitIsSixteenPixelsAndMatchesTheBake()
        {
            var json = MiniJson.ParseObject(File.ReadAllText(Find("unity/Assets/Resources/Guide/portrait.json")));
            var rows = json.Arr("rows");
            Assert.AreEqual(16, rows.Count);
            byte[] png = PngRgba.Decode(File.ReadAllBytes(Find("unity/Assets/Resources/Guide/thrum.png")), out int w, out int h);
            Assert.AreEqual(16, w);
            Assert.AreEqual(16, h);
            var palette = json.Obj("palette");
            int opaque = 0;
            for (int y = 0; y < 16; y++)
            {
                string row = (string)rows[y];
                Assert.AreEqual(16, row.Length);
                for (int x = 0; x < 16; x++)
                {
                    string hex = palette.Str(row[x].ToString());
                    int o = (y * 16 + x) * 4;
                    Assert.AreEqual(Convert.ToInt32(hex.Substring(1, 2), 16), png[o], "r " + x + "," + y);
                    Assert.AreEqual(Convert.ToInt32(hex.Substring(3, 2), 16), png[o + 1]);
                    Assert.AreEqual(Convert.ToInt32(hex.Substring(5, 2), 16), png[o + 2]);
                    Assert.AreEqual(Convert.ToInt32(hex.Substring(7, 2), 16), png[o + 3]);
                    if (png[o + 3] > 0) opaque++;
                }
            }
            Assert.Greater(opaque, 40);
        }

        [Test]
        public void TypewriterSpeedsBlipAndReduceMotion()
        {
            var slow = new Typewriter();
            slow.Start("abcdefghij", SubtitleSpeed.Slow, false, false);
            slow.Tick(3f / GuidePacing.SlowCps);
            Assert.AreEqual(3, slow.Shown);
            Assert.IsTrue(slow.BlipPending);
            slow.Tick(0.01f);
            Assert.IsFalse(slow.BlipPending);

            var muted = new Typewriter();
            muted.Start("abcdefghij", SubtitleSpeed.Normal, false, true);
            muted.Tick(1f);
            Assert.IsFalse(muted.BlipPending);

            var motion = new Typewriter();
            motion.Start("abcdefghij", SubtitleSpeed.Normal, true, false);
            Assert.IsTrue(motion.Done);
            Assert.AreEqual("abcdefghij", motion.Visible);
            motion.Tick(1f);
            Assert.IsFalse(motion.BlipPending);

            var fast = new Typewriter();
            fast.Start("abc", SubtitleSpeed.Fast, false, false);
            fast.Tick(3f / GuidePacing.FastCps);
            Assert.IsTrue(fast.Done);
            Assert.AreEqual(GuidePacing.NormalCps, GuidePacing.Cps(SubtitleSpeed.Normal));
            Assert.AreEqual(GuidePacing.FastCps, GuidePacing.Cps(SubtitleSpeed.Fast));
        }

        [Test]
        public void TapFinishesThenAdvancesAndAscentAutoReads()
        {
            var engine = new MockGuideEngine();
            engine.Settings.Speed = SubtitleSpeed.Normal;
            engine.Offer(new GuideBeat
            {
                Id = "tip",
                Speaker = GuideSpeaker.Thrum,
                Cards = new[] { new GuideCard { Kid = "Hold still." }, new GuideCard { Kid = "Now tap." } },
            });
            var stage = new GuideStage(engine) { Place = GuidePlace.Designer };
            stage.Tick(0.02f);
            Assert.AreEqual(1, stage.Count);
            Assert.IsFalse(stage.Models()[0].Shown.Length == stage.Models()[0].Full.Length);
            stage.Tap(0);
            Assert.AreEqual("Hold still.", stage.Models()[0].Shown);
            Assert.AreEqual("TAP: next", stage.Models()[0].Hint);
            stage.Tick(30f);
            Assert.AreEqual(1, stage.Count, "designer waits for a tap");
            stage.Tap(0);
            Assert.AreEqual("Now tap.", stage.Models()[0].Full);
            stage.Tap(0);
            stage.Tap(0);
            Assert.AreEqual(0, stage.Count);
            Assert.Contains("tip", engine.Seen);

            engine.Offer(new GuideBeat { Id = "asc", Speaker = GuideSpeaker.Thrum, Cards = new[] { new GuideCard { Kid = "Hi" } } });
            stage.Place = GuidePlace.Ascent;
            engine.Settings.Speed = SubtitleSpeed.Instant;
            stage.Tick(0.05f);
            Assert.AreEqual(1, stage.Count);
            stage.Tick(2.2f);
            Assert.AreEqual(1, stage.Count, "stays at least 2.5s");
            stage.Tick(0.4f);
            Assert.AreEqual(0, stage.Count);
        }

        [Test]
        public void CriticalLineWaitsForATap()
        {
            var engine = new MockGuideEngine();
            engine.Settings.Speed = SubtitleSpeed.Instant;
            engine.Offer(new GuideBeat
            {
                Id = "abort",
                Speaker = GuideSpeaker.MissionControl,
                Priority = GuidePriority.Safety,
                Critical = true,
                FromEngine = true,
                Cards = new[] { new GuideCard { Kid = "Hold." } },
            });
            var stage = new GuideStage(engine) { Place = GuidePlace.Ascent };
            stage.Tick(0.1f);
            stage.Tick(20f);
            Assert.AreEqual(1, stage.Count);
            stage.Skip(0);
            Assert.AreEqual(0, stage.Count);
            Assert.Contains("abort", engine.Skipped);
        }

        [Test]
        public void DecisionDropsChatterAndStacksMissionControlAboveThrum()
        {
            var engine = new MockGuideEngine();
            engine.Settings.Speed = SubtitleSpeed.Instant;
            engine.Offer(new GuideBeat { Id = "chat", Speaker = GuideSpeaker.Thrum, Priority = GuidePriority.Chatter, Cards = new[] { new GuideCard { Kid = "A story." } } });
            var stage = new GuideStage(engine) { Place = GuidePlace.Ascent };
            stage.Tick(0.01f);
            Assert.AreEqual(1, stage.Count);
            engine.Offer(new GuideBeat
            {
                Id = "go",
                Speaker = GuideSpeaker.Thrum,
                Priority = GuidePriority.Decision,
                Decision = true,
                Cards = new[] { new GuideCard { Kid = "Throttle down?" } },
            });
            stage.Tick(0.01f);
            var boxes = stage.Models();
            Assert.AreEqual(1, boxes.Length);
            Assert.AreEqual("go", boxes[0].Id);

            engine.Offer(new GuideBeat { Id = "mc", Speaker = GuideSpeaker.MissionControl, Cards = new[] { new GuideCard { Kid = "Max-Q in five." } } });
            stage.Skip(0);
            stage.Tick(0.01f);
            boxes = stage.Models();
            Assert.AreEqual(2, boxes.Length);
            Assert.AreEqual(GuideSpeaker.MissionControl, boxes[0].Speaker);
            Assert.IsFalse(boxes[0].Portrait);
            Assert.AreEqual(GuideSpeaker.Thrum, boxes[1].Speaker);
            Assert.IsTrue(boxes[1].Portrait);
        }

        [Test]
        public void TipsFilterAndSkipForStep()
        {
            var engine = new MockGuideEngine();
            engine.Settings.Speed = SubtitleSpeed.Instant;
            engine.Settings.Tips = TipsMode.Off;
            engine.Settings.StepId = "tanks";
            engine.Offer(new GuideBeat { Id = "chatter", StepId = "tanks", Cards = new[] { new GuideCard { Kid = "Paint is free." } } });
            engine.Offer(new GuideBeat { Id = "warn", Priority = GuidePriority.Failure, Cards = new[] { new GuideCard { Kid = "Too heavy." } } });
            engine.Offer(new GuideBeat { Id = "call", Priority = GuidePriority.Decision, Decision = true, Cards = new[] { new GuideCard { Kid = "Go?" } } });
            var stage = new GuideStage(engine);
            stage.Tick(0.01f);
            var ids = new List<string>();
            foreach (var box in stage.Models()) ids.Add(box.Id);
            Assert.IsFalse(ids.Contains("chatter"));
            Assert.IsFalse(ids.Contains("warn"));
            Assert.IsTrue(ids.Contains("call"));

            engine.Settings.Tips = TipsMode.WarningsOnly;
            stage.Tick(0.01f);
            ids.Clear();
            foreach (var box in stage.Models()) ids.Add(box.Id);
            Assert.IsTrue(ids.Contains("warn"));
            Assert.IsFalse(ids.Contains("chatter"));

            engine.Settings.Tips = TipsMode.On;
            engine.Offer(new GuideBeat { Id = "step-tip", StepId = "tanks", Cards = new[] { new GuideCard { Kid = "Fuel is how far." } } });
            stage.Tick(0.01f);
            stage.SkipStepTips(0);
            Assert.AreEqual("tanks", engine.LastSkippedStep);
            stage.Tick(0.01f);
            foreach (var box in stage.Models()) Assert.AreNotEqual("step-tip", box.Id);
        }

        [Test]
        public void EngineerLineAndTokens()
        {
            var beat = new GuideBeat
            {
                Id = "twr",
                Cards = new[] { new GuideCard { Kid = "Push wins. {twr} bananas.", Eng = "TWR {twr}." } },
                Tokens = new Dictionary<string, string> { { "twr", "1.4" } },
            };
            var kid = GuidePages.Of(beat, false);
            Assert.AreEqual(1, kid.Length);
            Assert.AreEqual("Push wins. 1.4 bananas.", kid[0]);
            var both = GuidePages.Of(beat, true);
            Assert.AreEqual(2, both.Length);
            Assert.AreEqual("TWR 1.4.", both[1]);

            var engine = new MockGuideEngine();
            engine.Settings.Speed = SubtitleSpeed.Instant;
            engine.Settings.Engineer = true;
            engine.Offer(beat);
            var stage = new GuideStage(engine);
            stage.Tick(0.01f);
            Assert.AreEqual(2, stage.Models()[0].Pages);
            engine.Settings.Engineer = false;
            stage.Tick(0.01f);
            Assert.AreEqual(1, stage.Models()[0].Pages);
        }

        [Test]
        public void VoiceOverOnceAndReadAloudOnlyWhenAsked()
        {
            var engine = new MockGuideEngine();
            engine.Settings.Speed = SubtitleSpeed.Instant;
            engine.Offer(MockGuideEngine.FirstRun());
            var voice = new RecordingGuideVoice();
            var stage = new GuideStage(engine) { Voice = voice };
            stage.Tick(0.05f);
            stage.Tick(1f);
            stage.Tick(1f);
            Assert.AreEqual(1, voice.Announcements.Count);
            Assert.AreEqual(0, voice.Spoken.Count);
            Assert.That(voice.Announcements[0], Does.StartWith("Thrum, hangar keeper."));
            Assert.That(voice.Announcements[0], Does.Contain("Card 1 of 5."));
            Assert.AreEqual("1/5", stage.Models()[0].Index);
            Assert.That(stage.Models()[0].Full, Does.StartWith("Oh! A visitor!"));

            engine.Settings.ReadAloud = true;
            stage.Tap(0);
            stage.Tick(0.05f);
            Assert.AreEqual(1, voice.Spoken.Count);
        }

        [Test]
        public void ReadingTimeAndFontSizes()
        {
            Assert.AreEqual(2f, GuidePacing.ReadingSeconds(1), 0.001f);
            Assert.AreEqual(90f / 18f + 1.2f, GuidePacing.ReadingSeconds(90), 0.001f);
            Assert.AreEqual(2.5f, GuidePacing.HoldSeconds(1, 0f), 0.001f);
            Assert.AreEqual(4f, GuidePacing.HoldSeconds(1, 4f), 0.001f);
            Assert.AreEqual(15f, GuidePacing.FontPt(SubtitleSize.Normal));
            Assert.AreEqual(19f, GuidePacing.FontPt(SubtitleSize.Large));
            Assert.AreEqual(23f, GuidePacing.FontPt(SubtitleSize.ExtraLarge));
            Assert.Greater(GuidePacing.HeightCap(false, SubtitleSize.ExtraLarge), GuidePacing.MaxHeightPt);
            Assert.AreEqual(0.04f, GuidePacing.BlipSeconds);
            Assert.GreaterOrEqual(GuidePacing.SkipMinPt, 44f);
        }

        [Test]
        public void BoxesStayAboveTheThumbZoneFromSeToIpad()
        {
            var devices = new[]
            {
                new object[] { "iPhone SE", 750f, 1334f, 0f, 0f, 750f, 1294f, 2f },
                new object[] { "iPhone 16 Pro", 1179f, 2556f, 0f, 102f, 1179f, 2556f - 102f - 177f, 3f },
                new object[] { "iPhone 16 Pro Max", 1290f, 2796f, 0f, 102f, 1290f, 2796f - 102f - 177f, 3f },
                new object[] { "iPad Pro 13", 2064f, 2752f, 0f, 40f, 2064f, 2752f - 40f - 48f, 2f },
            };
            foreach (object[] row in devices)
            {
                string name = (string)row[0];
                var screen = GuideScreen.FromPixels((float)row[1], (float)row[2], (float)row[3], (float)row[4], (float)row[5], (float)row[6], (float)row[7], GuidePacing.AscentHeaderPt, true);
                foreach (SubtitleSize size in new[] { SubtitleSize.Normal, SubtitleSize.Large, SubtitleSize.ExtraLarge })
                {
                    float font = GuidePacing.FontPt(size);
                    var frames = GuideLayout.Place(screen, new[] { false, true }, new[] { "Max-Q in five. Throttle down?", "Air pushes hardest now. Ease off and the tanks stay tidy!" }, font);
                    Assert.AreEqual(2, frames.Length, name);
                    Assert.That(frames[0].Y, Is.GreaterThanOrEqualTo(screen.DockTop - 0.01f), name);
                    Assert.That(frames[0].Font, Is.GreaterThanOrEqualTo(15f), name);
                    Assert.That(frames[0].Skip, Is.GreaterThanOrEqualTo(44f), name);
                    Assert.That(frames[1].Portrait, Is.EqualTo(52f), name);
                    float shadowBottom = frames[1].Bottom + GuidePacing.ShadowPt;
                    Assert.That(shadowBottom, Is.LessThanOrEqualTo(screen.LimitBottom + 0.05f), name + " " + size);
                    Assert.That(frames[0].W, Is.LessThanOrEqualTo(GuidePacing.MaxColumnPt + 0.01f), name);
                    Assert.That(frames[0].X, Is.GreaterThanOrEqualTo(screen.SafeLeft), name);
                }
            }

            var full = GuideScreen.FromPixels(750f, 1334f, 0f, 0f, 750f, 1294f, 2f, 0f, false);
            var one = GuideLayout.Place(full, new[] { true }, new[] { "Oh! A visitor! Mind the oil. I am Thrum. I keep this hangar, and I talk to wrenches." }, 15f);
            Assert.AreEqual(96f, one[0].Portrait);
            Assert.That(one[0].H, Is.LessThanOrEqualTo(GuidePacing.MaxHeightPt + 0.01f));
            Assert.That(one[0].Bottom + GuidePacing.ShadowPt, Is.LessThanOrEqualTo(full.LimitBottom + 0.05f));

            var pad = GuideScreen.FromPixels(2064f, 2752f, 0f, 40f, 2064f, 2752f - 40f - 48f, 2f, 0f, false);
            Assert.AreEqual(600f, pad.ColumnWidth, 0.01f);
        }

        [Test]
        public void PanelFontNeverDropsBelowFifteenPoints()
        {
            Assert.AreEqual(15f, GuideLayout.PanelFont(15f, 375f, 375f), 0.01f);
            float se = GuideLayout.PanelFont(15f, 1179f, 375f);
            Assert.That(se / (1179f / 375f), Is.EqualTo(15f).Within(0.01f));
            Assert.Greater(se, 15f);
        }

        [Test]
        public void EveryNarrationClipHasASubtitle()
        {
            var clips = Clips();
            Assert.AreEqual(167, clips.ClipCount);
            var seen = new HashSet<string>();
            foreach (var clip in clips.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(clip.Text), clip.File);
                Assert.IsTrue(NarrationSubtitles.IsSubtitleChannel(clip.Channel), clip.Channel);
                Assert.Greater(clip.Duration, 0.2f, clip.File);
                seen.Add(clip.File);
            }
            Assert.AreEqual(167, seen.Count);

            Assert.IsTrue(clips.TryNext("liftoff", out var first));
            Assert.AreEqual(GuideSpeaker.MissionControl, first.Speaker);
            Assert.IsFalse(GuideSpeakers.HasPortrait(first.Speaker));
            Assert.IsTrue(clips.TryNext("liftoff", out var second));
            Assert.AreNotEqual(first.Id, second.Id);
            clips.Reset();
            Assert.IsTrue(clips.TryNext("liftoff", out var again));
            Assert.AreEqual(first.Id, again.Id);

            Assert.IsTrue(clips.TryNext("abort-maxq", out var abort));
            Assert.AreEqual(GuidePriority.Safety, abort.Priority);
            Assert.IsTrue(abort.Critical);
            Assert.IsTrue(clips.TryNext("maxq", out var maxq));
            Assert.AreEqual(GuidePriority.Chatter, maxq.Priority);
            Assert.IsFalse(clips.TryNext("nope", out _));
        }

        [Test]
        public void StylesheetIsInkCreamSquareAndUnblurred()
        {
            string uss = File.ReadAllText(Find("unity/Assets/Resources/UI/GuideBox.uss"));
            Assert.That(uss, Does.Contain("#1b1f2a"));
            Assert.That(uss, Does.Contain("#faf0d7"));
            Assert.That(uss, Does.Contain("#e8b15a"));
            Assert.That(uss, Does.Contain("border-top-left-radius: 0"));
            Assert.That(uss, Does.Contain("min-height: 44px"));
            Assert.That(uss, Does.Not.Contain("blur"));
            Assert.That(uss.ToLowerInvariant(), Does.Not.Contain("box-shadow"));
        }

        [Test]
        public void MenuShowsOneBoxAndTheNextWaitsForATap()
        {
            var engine = new MockGuideEngine();
            engine.Settings.Speed = SubtitleSpeed.Instant;
            engine.Offer(new GuideBeat { Id = "first.1", Speaker = GuideSpeaker.Thrum, Cards = new[] { new GuideCard { Kid = "One." } } });
            engine.Offer(new GuideBeat { Id = "first.2", Speaker = GuideSpeaker.Thrum, Cards = new[] { new GuideCard { Kid = "Two." } } });
            var stage = new GuideStage(engine) { Place = GuidePlace.Menu };
            stage.Tick(0.05f);
            Assert.AreEqual(1, stage.Count);
            Assert.AreEqual("first.1", stage.Models()[0].Id);
            stage.Tap(0);
            Assert.AreEqual(1, stage.Count);
            Assert.AreEqual("first.2", stage.Models()[0].Id);
            Assert.AreEqual("1/1", stage.Models()[0].Index);
            stage.Tap(0);
            Assert.AreEqual(0, stage.Count);
        }

        [Test]
        public void SeenWatchIgnoresTheLoadedBaseline()
        {
            var watch = new GuideSeenWatch();
            Assert.IsFalse(watch.Consume(2, false));
            Assert.IsFalse(watch.Consume(2, false));
            Assert.IsTrue(watch.Consume(3, false));
            Assert.IsTrue(watch.Consume(3, true));
            Assert.IsFalse(watch.Consume(3, true));
        }

        [Test]
        public void FirstRunIsFiveCards()
        {
            var beat = MockGuideEngine.FirstRun();
            Assert.AreEqual(5, GuidePages.Of(beat, false).Length);
            Assert.That(beat.Cards[0].Kid, Does.Contain("I talk to wrenches."));
        }
    }

    static class PngRgba
    {
        public static byte[] Decode(byte[] png, out int w, out int h)
        {
            w = h = 0;
            int i = 8;
            var idat = new MemoryStream();
            while (i + 12 <= png.Length)
            {
                int len = ReadInt(png, i);
                i += 4;
                string tag = Encoding.ASCII.GetString(png, i, 4);
                i += 4;
                if (tag == "IHDR") { w = ReadInt(png, i); h = ReadInt(png, i + 4); }
                if (tag == "IDAT") idat.Write(png, i, len);
                i += len + 4;
                if (tag == "IEND") break;
            }
            byte[] z = idat.ToArray();
            var inflated = new MemoryStream();
            using (var ds = new DeflateStream(new MemoryStream(z, 2, z.Length - 6), CompressionMode.Decompress))
                ds.CopyTo(inflated);
            byte[] raw = inflated.ToArray();
            int stride = w * 4;
            var pixels = new byte[h * stride];
            int src = 0;
            var prior = new byte[stride];
            for (int y = 0; y < h; y++)
            {
                int filter = raw[src++];
                var row = new byte[stride];
                for (int x = 0; x < stride; x++)
                {
                    byte v = raw[src++];
                    byte left = x >= 4 ? row[x - 4] : (byte)0;
                    byte up = prior[x];
                    byte ul = x >= 4 ? prior[x - 4] : (byte)0;
                    if (filter == 1) v = (byte)(v + left);
                    else if (filter == 2) v = (byte)(v + up);
                    else if (filter == 3) v = (byte)(v + ((left + up) >> 1));
                    else if (filter == 4) v = (byte)(v + Paeth(left, up, ul));
                    row[x] = v;
                }
                Buffer.BlockCopy(row, 0, pixels, y * stride, stride);
                prior = row;
            }
            return pixels;
        }

        static int ReadInt(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];

        static byte Paeth(byte a, byte b, byte c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }
    }
}
