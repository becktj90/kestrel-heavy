using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide.Tests
{
    public class GuideBoxViewTests
    {
        static string Manifest()
        {
            string path = "Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/manifest.json";
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path);
        }

        [Test]
        public void StackBuildsAPortraitBoxAndAMissionControlBox()
        {
            var engine = new MockGuideEngine();
            engine.Settings.Speed = SubtitleSpeed.Instant;
            engine.Offer(MockGuideEngine.FirstRun());
            var stage = new GuideStage(engine, NarrationSubtitles.Parse(Manifest()));
            stage.OfferNarration("maxq");
            stage.Tick(0.05f);
            var host = new VisualElement { name = "host" };
            var art = GuideArt.Load();
            var view = new GuideStackView(host, stage)
            {
                Screen = new GuideScreen(375f, 667f, 20f, 0f, 0f, 0f, 0f, false),
                Glyphs = art.Glyphs,
                Atlas = art.Atlas,
                Portrait = art.Portrait,
            };
            view.Sync();

            var stack = host.Q("guide-stack");
            Assert.IsNotNull(stack);
            Assert.AreEqual(PickingMode.Ignore, stack.pickingMode);
            Assert.AreEqual(2, stack.childCount);

            var top = stack[0];
            Assert.AreEqual(PickingMode.Position, top.pickingMode);
            Assert.IsNotNull(top.Q<Button>("guide-skip"));
            Assert.AreEqual("SKIP", top.Q<Button>("guide-skip").text);
            Assert.That(top.Q<Button>("guide-skip").tooltip, Does.Contain("Skip"));
            Assert.AreEqual(DisplayStyle.None, top.Q("guide-portrait").style.display.value);
            Assert.AreEqual("MISSION CONTROL", top.Q<Label>("guide-name").text);

            var thrum = stack[1];
            Assert.AreEqual(DisplayStyle.Flex, thrum.Q("guide-portrait").style.display.value);
            Assert.That(thrum.Q<Label>("guide-name").text, Does.Contain("MABEL THRUM"));
            Assert.That(thrum.Q<Label>("guide-name").text, Does.Contain("HANGAR KEEPER"));
            var body = thrum.Q<BitmapTextElement>("guide-body");
            Assert.IsNotNull(body);
            Assert.That(body.Text, Does.StartWith("Oh! A visitor!"));
            Assert.That(thrum.tooltip, Does.Contain("Thrum, hangar keeper"));
            Assert.AreEqual("1/5", thrum.Q<Label>("guide-index").text);
        }

        [Test]
        public void PopIsAPointFilteredPortraitWithAnOptionalCaption()
        {
            var host = new VisualElement { name = "host" };
            var art = GuideArt.Load();
            var view = new GuidePopView(host)
            {
                Portrait = art.Portrait,
                Glyphs = art.Glyphs,
                Atlas = art.Atlas,
            };
            var screen = new GuideScreen(375f, 667f, 20f, 0f, 0f, 0f, 0f, false);
            var quiet = GuidePopLayout.Place(screen, 1f, true, false);
            view.Show(quiet, 1f, false, "", GuideCaption.Glyph(SubtitleSize.Normal));
            var pop = host.Q("guide-pop");
            Assert.IsNotNull(pop);
            Assert.AreEqual(PickingMode.Position, pop.pickingMode);
            Assert.That(pop.tooltip, Does.Contain("Thrum, hangar keeper"));
            var portrait = pop.Q("guide-portrait");
            Assert.AreEqual(GuidePopLayout.DisplayPt, portrait.style.width.value.value, 0.1f);
            Assert.AreEqual(GuidePopLayout.DisplayPt, portrait.style.height.value.value, 0.1f);
            Assert.AreEqual(DisplayStyle.None, pop.Q("guide-caption").style.display.value);

            var spoken = GuidePopLayout.Place(screen, 1f, true, true);
            view.Show(spoken, 1f, true, "Mind the oil.", GuideCaption.Glyph(SubtitleSize.Normal));
            Assert.AreEqual(DisplayStyle.Flex, pop.Q("guide-caption").style.display.value);
            Assert.AreEqual("Mind the oil.", pop.Q<BitmapTextElement>("guide-caption").Text);
            view.Hide();
            Assert.AreEqual(DisplayStyle.None, pop.style.display.value);
        }

        [Test]
        public void ArtIsPointFilteredAndSixteenPixels()
        {
            var art = GuideArt.Load();
            Assert.IsNotNull(art.Portrait, "thrum.png");
            Assert.IsNotNull(art.Atlas, "atlas.png");
            Assert.IsNotNull(art.Glyphs);
            Assert.AreEqual(FilterMode.Point, art.Portrait.filterMode);
            Assert.AreEqual(FilterMode.Point, art.Atlas.filterMode);
            Assert.AreEqual(16, art.Portrait.width);
            Assert.AreEqual(16, art.Portrait.height);
            Assert.AreEqual(96, art.Glyphs.Count);
        }

        [Test]
        public void SinkSubtitlesLiftoffWithoutAPortrait()
        {
            var stage = new GuideStage(new MockGuideEngine(), NarrationSubtitles.Parse(Manifest()));
            var sink = new NarrationSubtitleSink { Stage = stage };
            sink.OnEvent("liftoff");
            stage.Tick(0.01f);
            var boxes = stage.Models();
            Assert.AreEqual(1, boxes.Length);
            Assert.AreEqual(GuideSpeaker.MissionControl, boxes[0].Speaker);
            Assert.IsFalse(boxes[0].Portrait);
            Assert.IsFalse(string.IsNullOrEmpty(boxes[0].Full));
            sink.OnEvent("flight.end");
            stage.Tick(0.01f);
            Assert.AreEqual(0, stage.Count);
        }
    }
}
