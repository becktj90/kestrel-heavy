// StarterRocket API stub (Phase 1 PR 4 shape, placeholder data). Expected values come from
// data/campaign.json rules and the plan's knob ranges, not from the stub itself.
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterRocketTests
    {
        StarterTable table;

        [SetUp]
        public void Load() => table = StarterTable.LoadPlaceholder(Golden.Root);

        [Test]
        public void Knobs_MatchThePlan()
        {
            CollectionAssert.AreEqual(new[] { -20, -10, 0, 10, 20 }, StarterRocket.FuelPercents);
            CollectionAssert.AreEqual(new[] { 0, 2, 4 }, StarterRocket.BoosterValues);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, StarterRocket.StageValues);
            Assert.AreEqual(2, StarterRocket.DefaultFuelNotch);
            Assert.IsTrue(table.IsPlaceholder);
            for (int i = 1; i <= 20; i++) Assert.IsTrue(table.Has("m" + i), "KH-" + i);
        }

        [Test]
        public void Kh1_IsOneStage_NoBoosters_DefaultFromTheWinner()
        {
            var r = new StarterRocket(table, "m1");
            CollectionAssert.AreEqual(new[] { 1 }, r.StageOptions);
            Assert.IsFalse(r.BoostersVisible);
            Assert.AreEqual(new StarterChoice(StarterEngine.Kerosene, 1, 2, 0), r.Choice);
            Assert.IsTrue(r.CanLaunch);
            var d = r.BuildDesign();
            Assert.AreEqual("m1", d.Str("missionId"));
            Assert.AreEqual(50, d.Obj("payload").Num("mass"), "KH-1 payload from campaign.json");
        }

        [Test]
        public void Knobs_SnapRaiseChanged_AndReset()
        {
            var r = new StarterRocket(table, "m3");
            Assert.AreEqual(StarterEngine.Methane, r.Engine, "KH-3 winner flies methane");
            int changes = 0;
            r.Changed += _ => changes++;
            r.Stages = 1; // not offered on an orbital mission
            Assert.IsTrue(r.LastSnapped);
            Assert.AreEqual(2, r.Stages);
            Assert.AreEqual(0, changes, "snapped back to the same combination");
            r.FuelNotch = 4;
            Assert.AreEqual(20, r.FuelPercent);
            Assert.IsFalse(r.LastSnapped);
            Assert.AreEqual(1, changes);
            r.Boosters = 4; // KH-3 allows no strap-ons
            Assert.AreEqual(0, r.Boosters);
            Assert.IsFalse(r.IsBoostersAllowed(4));
            r.Reset();
            Assert.AreEqual(r.Default, r.Choice);
            Assert.AreEqual(2, changes);
            StringAssert.StartsWith("Ready.", r.ReadinessLine);
        }

        [Test]
        public void Boosters_FollowMissionRules()
        {
            var kh5 = new StarterRocket(table, "m5");
            Assert.IsTrue(kh5.BoostersVisible);
            CollectionAssert.AreEqual(new[] { 0, 2, 4 }, kh5.BoosterOptions);
            var kh10 = new StarterRocket(table, "m10");
            Assert.IsTrue(kh10.BoostersVisible);
            Assert.IsTrue(kh10.BoostersLocked);
            CollectionAssert.AreEqual(new[] { 2 }, kh10.BoosterOptions, "KH-10 is locked to its landing pair");
        }

        [Test]
        public void Save_RoundTrips_AndDesignMapsBack()
        {
            var r = new StarterRocket(table, "m4");
            r.FuelNotch = 0;
            r.Stages = 3;
            var saved = r.ToSave();
            Assert.AreEqual("kh.save.v1.starter.m4", StarterRocket.SaveKey("m4"));
            Assert.IsTrue(Kestrel.Core.Save.NativeStore.CheckKey(StarterRocket.SaveKey("m4")));
            var back = new StarterRocket(table, "m4");
            Assert.IsTrue(back.ApplySave(MiniJson.ParseObject(Kestrel.Core.Save.JsonWriter.Stringify(saved))));
            Assert.AreEqual(r.Choice, back.Choice);
            Assert.IsFalse(new StarterRocket(table, "m5").ApplySave(saved), "another mission's record");

            var fromWinner = new StarterRocket(table, "m4").FromDesign(table.BaseDesign("m4"));
            Assert.IsFalse(fromWinner.Snapped);
            Assert.AreEqual(table.Default("m4"), fromWinner.Choice);
            Assert.AreEqual("m4", fromWinner.Design.Str("missionId"));
        }
    }
}
