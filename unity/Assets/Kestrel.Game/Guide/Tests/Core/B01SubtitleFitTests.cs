// B01 mission-status calls (PR #495 takes): every wired line's subtitle fits the ascent box whole, with no scroll and
// no clipping, at 335 pt portrait and in web landscape, at every subtitle size, and stays under the 14-word cap.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Game.Guide.Tests
{
    public class B01SubtitleFitTests
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

        static List<JsonObject> B01()
        {
            var root = MiniJson.ParseObject(File.ReadAllText(Find("unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/manifest.json")));
            var list = new List<JsonObject>();
            foreach (JsonObject c in root.Arr("clips")) if (c.Str("batch") == "B01") list.Add(c);
            return list;
        }

        [Test]
        public void EveryB01SubtitleFitsWhole_At335PortraitAndLandscape_AtEverySize()
        {
            var clips = B01();
            Assert.That(clips.Count, Is.EqualTo(91));
            var screens = new[]
            {
                new object[] { "335 portrait", new GuideScreen(335f, 720f, 47f, 34f, 0f, 0f, GuidePacing.AscentHeaderPt, false) },
                new object[] { "web landscape 960x540", new GuideScreen(960f, 540f, 0f, 0f, 0f, 0f, GuidePacing.AscentHeaderPt, true) },
                new object[] { "web landscape 1280x720", new GuideScreen(1280f, 720f, 0f, 0f, 0f, 0f, GuidePacing.AscentHeaderPt, true) },
            };
            foreach (var c in clips)
            {
                string text = c.Str("text"), id = c.Str("id");
                Assert.That(text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length, Is.LessThanOrEqualTo(14), id);
                foreach (SubtitleSize size in new[] { SubtitleSize.Normal, SubtitleSize.Large, SubtitleSize.ExtraLarge })
                {
                    float font = GuidePacing.FontPt(size);
                    foreach (object[] row in screens)
                    {
                        var screen = (GuideScreen)row[1];
                        string where = id + " @ " + row[0] + " " + size;
                        var frames = GuideLayout.Place(screen, new[] { false }, new[] { text }, font, GuidePlace.Ascent);
                        Assert.AreEqual(1, frames.Length, where);
                        Assert.IsFalse(frames[0].Scroll, where + ": scrolls");
                        Assert.IsFalse(GuideLayout.Clipped(frames[0], text), where + ": clipped");
                        Assert.That(frames[0].X + frames[0].W, Is.LessThanOrEqualTo(screen.Width - screen.SafeRight + 0.05f), where);
                    }
                }
            }
        }

        [Test]
        public void EngineOutIsASafetySubtitle_StatusCallsAreChatter()
        {
            Assert.AreEqual(GuidePriority.Safety, NarrationSubtitles.PriorityFor("engine-out"));
            Assert.AreEqual(GuidePriority.Chatter, NarrationSubtitles.PriorityFor("status.ontrack"));
            Assert.AreEqual(GuidePriority.Chatter, NarrationSubtitles.PriorityFor("landing.alt.100"));
        }
    }
}
