// Briefing overlay: portrait column, 44 pt Build it and Back, S4 lines on the labels.
using System.IO;
using System.Text;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class BriefingScreenTests
    {
        static string Repo => Catalog.FindRepoRoot(Directory.GetCurrentDirectory());

        [Test]
        public void ScreenShowsWeatherEyeAndTheTwoRoutes()
        {
            var campaign = Campaign.Load(Repo);
            var ctx = MissionContext.FromCampaign(campaign, "m4");
            var root = BriefingScreen.Build(ctx, () => { }, () => { });
            Assert.AreEqual("briefing-card", root.name);
            Assert.AreEqual("KH-4  Weather Eye", root.Q<Label>("briefing-title").text);
            Assert.AreEqual("Tern-3 · Weather sat · 2,500 kg", root.Q<Label>("briefing-payload").text);
            Assert.AreEqual("12 m3 · Diameter 2 m", root.Q<Label>("briefing-fit").text);
            Assert.AreEqual("Circular orbit 500 km, 51.6°", root.Q<Label>("briefing-target").text);
            Assert.AreEqual("Peak g at most 6", root.Q<Label>("briefing-requirement-0").text);
            Assert.IsNull(root.Q("briefing-requirement-1"));
            Assert.AreEqual("Optional.", root.Q<Label>("briefing-bonus-note").text);
            Assert.AreEqual("Land the booster", root.Q<Label>("briefing-bonus-0").text);
            Assert.AreEqual("Keep peak g under 4", root.Q<Label>("briefing-bonus-1").text);
            Assert.AreEqual("Budget 40,000,000 Kc", root.Q<Label>("briefing-budget").text);
            Assert.AreEqual("Reward 40,000,000 Kc", root.Q<Label>("briefing-reward").text);
            Assert.AreEqual("Gentle off.", root.Q<Label>("briefing-gentle").text);

            var card = MissionBriefing.Load(Repo, 4, 0, ctx.Seed);
            Assert.AreEqual("Wind " + card.WindLine, root.Q<Label>("briefing-wind").text);
            Assert.AreEqual(card.TimeLabel + ", hour " + card.Hour, root.Q<Label>("briefing-time").text);
            Assert.AreEqual("Pad: " + card.Site.Name, root.Q<Label>("briefing-pad").text);
            Assert.AreEqual("Seed " + card.Replay.Label, root.Q<Label>("briefing-seed").text);
            Assert.AreEqual(card.EngineerLine, root.Q<Label>("briefing-engineer").text);
            Assert.AreEqual(card.Goal, root.Q<Label>("briefing-goal").text);

            var build = root.Q<Button>("briefing-build");
            var back = root.Q<Button>("briefing-back");
            Assert.AreEqual("Build it", build.text);
            Assert.AreEqual("Back", back.text);
            Assert.AreEqual("Build it. Opens the designer with this mission.", build.tooltip);
            Assert.AreEqual("Back to the mission list.", back.tooltip);
            Assert.IsTrue(build.ClassListContains("kh-button"));
            Assert.IsTrue(back.ClassListContains("kh-button"));
            Assert.AreEqual(44f, build.style.minHeight.value.value);
            Assert.AreEqual(44f, back.style.minHeight.value.value);
            Assert.AreEqual(Position.Absolute, root.style.position.value);
            AssertPlain(Visible(root));
        }

        [Test]
        public void ShellCardUsesTheSameScreen()
        {
            var ctx = MissionContext.FromCampaign(Campaign.Load(Repo), "m1");
            var root = ScreenStub.BuildBriefing(ctx, () => { }, () => { });
            Assert.AreEqual("Pebble · Sounding box · 50 kg", root.Q<Label>("briefing-payload").text);
            Assert.AreEqual("Apogee 10 km", root.Q<Label>("briefing-target").text);
            Assert.IsNull(root.Q("briefing-requirement-0"));
            Assert.AreEqual("Keep peak g under 6", root.Q<Label>("briefing-bonus-0").text);
            Assert.IsNotNull(root.Q<Button>("briefing-build"));
            Assert.IsNotNull(root.Q<Button>("briefing-back"));
            Assert.AreEqual(44f, root.Q<Button>("briefing-build").style.minHeight.value.value);
            Assert.AreEqual("Gentle on.", root.Q<Label>("briefing-gentle").text);
            var card = MissionBriefing.Load(Repo, 1, 0, ctx.Seed);
            Assert.AreEqual("Wind " + card.WindLine, root.Q<Label>("briefing-wind").text);
            Assert.AreEqual("Pad: " + card.Site.Name, root.Q<Label>("briefing-pad").text);
        }

        [Test]
        public void MissingFlightStillHasAWayBack()
        {
            var root = BriefingScreen.Build(null, () => { }, () => { });
            Assert.AreEqual("Briefing", root.Q<Label>("briefing-title").text);
            Assert.IsNull(root.Q("briefing-build"));
            Assert.IsNull(root.Q("briefing-wind"));
            Assert.AreEqual("Back", root.Q<Button>("briefing-back").text);
            Assert.AreEqual(44f, root.Q<Button>("briefing-back").style.minHeight.value.value);
        }

        static string Visible(VisualElement node)
        {
            var sb = new StringBuilder();
            void Walk(VisualElement e)
            {
                if (e == null) return;
                if (e is TextElement text && !string.IsNullOrEmpty(text.text)) sb.AppendLine(text.text);
                if (!string.IsNullOrEmpty(e.tooltip)) sb.AppendLine(e.tooltip);
                for (int i = 0; i < e.childCount; i++) Walk(e[i]);
            }
            Walk(node);
            return sb.ToString();
        }

        static void AssertPlain(string text)
        {
            string lower = text.ToLowerInvariant();
            foreach (var bad in new[]
            {
                "you missed", "you failed", "you lost", "limited time", "watch an ad", "streak",
                "daily", " buy", "spacex", "nasa", "falcon", "starlink", "soyuz", "ariane",
            })
                StringAssert.DoesNotContain(bad, lower);
        }
    }
}
