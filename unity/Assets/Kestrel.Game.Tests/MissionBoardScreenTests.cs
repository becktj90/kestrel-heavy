// Mission board tree: twenty patch nodes, the act bar, a sheet that stays below the map,
// and player copy with no emoji and no raw ids.
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Game.MissionSelect;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class MissionBoardScreenTests
    {
        static readonly Regex RawId = new Regex(
            @"\b(kid-box|leo-sat|gto-comsat|crew-capsule|lunar-probe|weather-sat|space-telescope|cargo-capsule|cubesat-dispenser|geo-relay|lunar-lander|cubesat|payloadId|maxG|noRedRisks|coastHours|nozzle-expansion|grid-fins|hydrolox-upper|stage-2|redundant-avionics|kh-m\d+|achievable|m\d+)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static string Repo => Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static Campaign Load() => Campaign.Load(Repo);
        static string Json() => File.ReadAllText(Path.Combine(Repo, "data", "campaign.json"));

        static MissionBoardView Board(Career career, IReadOnlyDictionary<string, MissionRecord> records, bool reduce, float w, float h, bool? wide, System.Func<string, bool> canLaunch)
        {
            return MissionBoardScreen.Build(
                Load(), career, records, MissionMapText.ReadPayloadLabels(Json()),
                _ => { }, _ => { }, () => { }, () => { },
                reduce, canLaunch, w, h, wide);
        }

        [Test]
        public void PortraitMapKeepsEveryNodeAboveTheSheet()
        {
            var campaign = Load();
            var board = Board(Career.Start(campaign, 100000), null, false, 375f, 667f, false, id => id == "m1");

            Assert.AreEqual(20, CountNodes(board));
            Assert.That(board.Q<Label>("mission-act").text, Does.Contain("First Light"));
            Assert.AreEqual("Fly KH-1", board.Q<Button>("mission-primary").text);
            Assert.That(board.Q<Label>("mission-customer").text, Does.Contain("Hangar Desk"));
            Assert.That(board.Q<Label>("mission-orbit").text, Does.Contain("Apogee"));
            Assert.IsNotNull(board.Q("mission-sky"));
            Assert.IsNotNull(board.Q("mission-trail"));
            Assert.IsNotNull(board.Q("mission-art"));

            var open = board.Q<Button>("mission-node-m1");
            Assert.IsTrue(open.ClassListContains("mission-node--open"));
            Assert.IsTrue(open.ClassListContains("mission-node--selected"));
            Assert.IsTrue(board.Q("mission-node-m20").parent.ClassListContains("mission-node--locked"));
            Assert.IsNotNull(board.Q("mission-pulse"));

            for (int i = 0; i < 5; i++) Assert.IsNotNull(board.Q("mission-progress-" + i), "segment " + i);

            var frame = board.userData as MissionBoardFrame;
            Assert.IsNotNull(frame);
            Assert.IsFalse(frame.Wide);
            Assert.That(frame.Path.Bottom, Is.LessThanOrEqualTo(frame.Card.Y + 0.6f));
            var path = board.Q("mission-path");
            var card = board.Q("mission-card");
            Assert.That(Top(path) + Height(path), Is.LessThanOrEqualTo(Top(card) + 0.6f));
            Assert.That(Height(board.Q("mission-primary")), Is.GreaterThanOrEqualTo(44f));
            Assert.IsFalse(board.ClassListContains("mission-board--wide"));

            board.SelectMission("m2", true);
            Assert.AreEqual("Clear KH-1 to unlock", board.Q<Button>("mission-primary").text);
            Assert.That(board.Q("mission-sheet-body").style.top.value.value, Is.EqualTo(MissionBoardMotion.SlidePt).Within(0.1f));

            board.SelectMission("m6", false);
            Assert.That(board.Q<Label>("mission-act").text, Does.Contain("Regulars"));
            board.SelectSandbox();
            Assert.AreEqual("Design first", board.Q<Button>("mission-primary").text);
            Assert.That(board.Q<Label>("mission-act").text, Does.Contain("Regulars"));
            board.SelectContracts();
            Assert.AreEqual("Not posted", board.Q<Button>("mission-primary").text);

            Sweep(board);
        }

        [Test]
        public void WideWebPutsTheSheetBesideTheMap()
        {
            var campaign = Load();
            var board = Board(Career.Start(campaign, 100000), null, false, 1280f, 720f, true, id => id == "m1");
            Assert.IsTrue(board.ClassListContains("mission-board--wide"));
            var frame = board.userData as MissionBoardFrame;
            Assert.IsTrue(frame.Wide);
            Assert.That(frame.Card.X, Is.GreaterThan(frame.Path.Right - 0.6f));
            var path = board.Q("mission-path");
            var card = board.Q("mission-card");
            Assert.That(Left(card), Is.GreaterThan(Left(path) + Width(path) - 0.6f));
            Assert.AreEqual("Fly KH-1", board.Q<Button>("mission-primary").text);
            Sweep(board);
        }

        [Test]
        public void EarnedStarsAreTheOnesDrawnAndReduceMotionDoesNotTravel()
        {
            var campaign = Load();
            var career = Career.Start(campaign, 100000);
            career.Cleared = 1;
            var record = new MissionRecord { BestStars = 2, BestScore = 100, Attempts = 1 };
            record.BestMedals.Add("silver");
            var records = new Dictionary<string, MissionRecord> { { "m1", record } };
            var board = Board(career, records, true, 390f, 844f, false, _ => true);

            Assert.AreEqual("Fly KH-2", board.Q<Button>("mission-primary").text);
            int on = 0, total = 0;
            CountPips(board.Q("mission-node-m1").parent, ref on, ref total);
            Assert.AreEqual(3, total);
            Assert.AreEqual(2, on);
            on = 0;
            total = 0;
            CountPips(board.Q("mission-node-m2").parent, ref on, ref total);
            Assert.AreEqual(0, total);

            board.SelectMission("m1", true);
            Assert.That(board.Q<Label>("mission-facts").text, Does.Contain("2 stars"));
            Assert.That(board.Q<Label>("mission-facts").text, Does.Contain("Silver"));
            Assert.That(board.Q("mission-sheet-body").style.top.value.value, Is.EqualTo(0f).Within(0.1f));
            Assert.That(board.Q("mission-pulse").style.opacity.value, Is.EqualTo(0.8f).Within(0.05f));
            Sweep(board);
        }

        [Test]
        public void WithoutAStarterTheOpenMissionAsksForADesign()
        {
            var campaign = Load();
            var board = Board(Career.Start(campaign, 100000), null, false, 440f, 956f, false, _ => false);
            Assert.AreEqual("Design first", board.Q<Button>("mission-primary").text);
            Sweep(board);
        }

        [Test]
        public void EmptyCampaignShowsTheNoteAndNoNodes()
        {
            var board = MissionBoardScreen.Build(null, null, null, null, _ => { }, _ => { }, () => { }, () => { }, false, null, 375f, 667f, false);
            Assert.IsNotNull(board.Q("mission-empty"));
            Assert.IsNull(board.Q("mission-node-m1"));
            Assert.AreEqual("Locked", board.Q<Button>("mission-primary").text);
            Sweep(board);
        }

        static int CountNodes(VisualElement root)
        {
            int n = 0;
            for (int i = 1; i <= 20; i++) if (root.Q("mission-node-m" + i) != null) n++;
            return n;
        }

        static void CountPips(VisualElement el, ref int on, ref int total)
        {
            if (el == null) return;
            if (el is MissionStarPip pip)
            {
                total++;
                if (pip.On) on++;
            }
            int n = el.childCount;
            for (int i = 0; i < n; i++) CountPips(el[i], ref on, ref total);
        }

        static void Sweep(VisualElement root)
        {
            var lines = new List<string>();
            Collect(root, lines);
            Assert.Greater(lines.Count, 0);
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                Assert.That(RawId.IsMatch(line), Is.False, line);
                Assert.That(line, Does.Not.Contain("achievable"));
                Assert.That(Emoji(line), Is.False, line);
            }
        }

        static void Collect(VisualElement el, List<string> lines)
        {
            if (el == null) return;
            if (el is TextElement text && !string.IsNullOrEmpty(text.text)) lines.Add(text.text);
            int n = el.childCount;
            for (int i = 0; i < n; i++) Collect(el[i], lines);
        }

        static bool Emoji(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int c = char.ConvertToUtf32(text, i);
                if (c >= 0x1F000 || (c >= 0x2600 && c <= 0x27BF) || (c >= 0x2B00 && c <= 0x2BFF)) return true;
                if (char.IsSurrogate(text[i])) i++;
            }
            return false;
        }

        static float Left(VisualElement e) => e.style.left.value.value;
        static float Top(VisualElement e) => e.style.top.value.value;
        static float Width(VisualElement e) => e.style.width.value.value;
        static float Height(VisualElement e) => e.style.height.value.value;
    }
}
