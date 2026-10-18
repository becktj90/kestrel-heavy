// The patch is a small hook on the mission list and the briefing. The collection is its own screen.
using System.Collections.Generic;
using System.Linq;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class MissionPatchScreenTests
    {
        static string Repo => Catalog.FindRepoRoot(System.IO.Directory.GetCurrentDirectory());

        [Test]
        public void MissionRowsCarryAPatchAndTheCollectionOpens()
        {
            var campaign = Campaign.Load(Repo);
            var career = Career.Start(campaign, 100000);
            var root = MissionSelectScreen.Build(campaign, career, null, MissionMapText.ReadPayloadLabels(System.IO.File.ReadAllText(System.IO.Path.Combine(Repo, "data", "campaign.json"))), _ => { }, () => { });
            Assert.IsNotNull(root.Q<MissionPatch>("mission-m1-patch"));
            Assert.IsNotNull(root.Q<Button>("missions-patches"));
            Assert.AreEqual(44f, root.Q<Button>("missions-patches").style.minHeight.value.value);
            Assert.IsNull(root.Q(PatchCollectionScreen.RootName));

            MissionSelectScreen.ShowPatches(root, campaign, career, null);
            var sheet = root.Q(PatchCollectionScreen.RootName);
            Assert.IsNotNull(sheet);
            Assert.AreEqual(MissionPatchArt.CampaignCount, sheet.Query<MissionPatch>().ToList().Count);
            Assert.AreEqual("Hop to it.", sheet.Q<Label>("patch-motto-1").text);
            Assert.IsNotNull(sheet.Q<Label>("patch-anatomy-1"));
            Assert.AreEqual(PatchCollectionScreen.LockedLine, sheet.Q<Label>("patch-lock-2").text);
            Assert.IsNull(sheet.Q("patch-motto-2"));
            Assert.AreEqual(44f, sheet.Q<Button>("patches-back").style.minHeight.value.value);

            MissionSelectScreen.ShowPatches(root, campaign, career, null);
            Assert.IsNull(root.Q(PatchCollectionScreen.RootName));
        }

        [Test]
        public void BriefingShowsMottoAndAnatomy()
        {
            var ctx = MissionContext.FromCampaign(Campaign.Load(Repo), "m4");
            var root = BriefingScreen.Build(ctx, () => { }, () => { });
            Assert.IsNotNull(root.Q<MissionPatch>("briefing-patch"));
            Assert.AreEqual("The tern sees the weather.", root.Q<Label>("briefing-motto").text);
            Assert.That(root.Q<Label>("briefing-anatomy").text, Does.Contain("orbit ring"));
            Assert.AreEqual(148f, root.Q<MissionPatch>("briefing-patch").style.width.value.value);
        }

        [Test]
        public void ClearedMedalUsesTheGoldEdge()
        {
            var records = new Dictionary<string, MissionRecord>();
            var row = new MissionRecord();
            row.BestMedals.Add("land");
            records["m7"] = row;
            Assert.AreEqual(PatchChrome.Medal, PatchCollectionScreen.ChromeFor(7, 7, records["m7"]));
            Assert.AreEqual(PatchChrome.Open, PatchCollectionScreen.ChromeFor(7, 7, new MissionRecord()));
            // Seven clears leave KH-8 next, so it stays open. KH-9 is still locked.
            Assert.AreEqual(PatchChrome.Open, PatchCollectionScreen.ChromeFor(8, 7, null));
            Assert.AreEqual(PatchChrome.Locked, PatchCollectionScreen.ChromeFor(9, 7, null));
            Assert.AreEqual(PatchChrome.Open, PatchCollectionScreen.ChromeFor(1, 0, null));
        }
    }
}
