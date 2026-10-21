// Starter controls build the real rocket (Phase 1 PR 5 rework). Each control is replayed onto the mission's
// campaign winner with the full designer's HangarEditor ops, so Launch flies exactly what the controls say.
// Expected values come from data/golden/campaign-winners.json, data/campaign.json and data/engines.json.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterRocketTests
    {
        static StarterRocket R(string id) => StarterFixture.Rocket(id);
        static string Snap(JsonObject d) => HangarSession.Snapshot(d);
        static List<object> Stages(JsonObject d) => d.Arr("stages");
        static JsonObject Stage(JsonObject d, int i) => (JsonObject)d.Arr("stages")[i];

        [Test]
        public void Bases_AreTheCampaignWinners_AndTheShippedCopyMatchesTheGolden()
        {
            string golden = File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json"));
            string shipped = File.ReadAllText(Path.Combine(Golden.Root, "data", "campaign-winners.json"));
            Assert.AreEqual(golden, shipped, "data/campaign-winners.json ships in Resources/KestrelData (CI copies data/*.json); keep it identical to the golden");
            var bases = StarterBases.Parse(shipped);
            Assert.AreEqual(20, bases.Count);
            foreach (var m in StarterFixture.Campaign.Missions) Assert.IsTrue(bases.ContainsKey(m.Id), m.Id);
        }

        [Test]
        public void DefaultSetting_IsTheWinnerAsHomeStampsIt()
        {
            StarterFixture.EnsureBases();
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = R(m.Id);
                var stamped = StarterBases.For(m.Id);
                HomeAutoBuild.ApplyMission(StarterFixture.Catalog, stamped, m);
                Assert.AreEqual(Snap(stamped), Snap(r.BuildDesign()), m.Id + ": the default flies the winner unchanged");
                Assert.AreEqual(r.Default, r.Choice, m.Id);
                Assert.AreEqual(StarterRocket.DefaultFuelNotch, r.FuelNotch);
            }
            var kh3 = R("m3");
            Assert.AreEqual(StarterEngine.Methane, kh3.Engine, "KH-3's winner burns methane");
            Assert.AreEqual(2, kh3.Stages);
            Assert.AreEqual("kerosene-3-f2-b2", R("m10").Choice.Key, "KH-10: three stages and the landing pair");
        }

        [Test]
        public void Stages_RemoveTheMiddleThenTheTop_AndAddAMiddleStage()
        {
            var r = R("m5"); // kerolox 360 t + hydrolox 30 t
            var two = r.BuildDesign();
            r.Stages = 1;
            var one = r.BuildDesign();
            Assert.AreEqual(1, Stages(one).Count);
            foreach (var k in new[] { "propellant", "engine", "engineCount", "propMass", "diameter" })
                Assert.AreEqual(Stage(two, 0)[k], Stage(one, 0)[k], "the first stage keeps its " + k + " (HangarEditor.RemoveStage only normalises)");
            r.Stages = 3;
            var three = r.BuildDesign();
            Assert.AreEqual(3, Stages(three).Count);
            Assert.AreEqual("kerolox", Stage(three, 1).Str("propellant"), "a new middle stage copies the first stage's fuel (HangarEditor.AddStage)");
            Assert.AreEqual(Math.Round(360000 * 0.25), Stage(three, 1).Num("propMass"), "a quarter of the stage below");
            Assert.AreEqual("hydrolox", Stage(three, 2).Str("propellant"), "the hydrogen top stays on top");

            var kh10 = R("m10"); // three stages: kerolox, kerolox-vac, hydrolox
            kh10.Stages = 2;
            var d = kh10.BuildDesign();
            Assert.AreEqual(2, Stages(d).Count);
            Assert.AreEqual("gg-kerolox", Stage(d, 0).Str("engine"));
            Assert.AreEqual("hydrolox", Stage(d, 1).Str("propellant"), "3 → 2 drops the middle stage");
        }

        [Test]
        public void Fuel_ScalesEveryStageByTheNotch()
        {
            var r = R("m5");
            var mid = r.BuildDesign();
            for (int notch = 0; notch < StarterRocket.FuelNotches; notch++)
            {
                r.FuelNotch = notch;
                var d = r.BuildDesign();
                double k = 1 + StarterRocket.FuelPercents[notch] / 100.0;
                for (int i = 0; i < Stages(d).Count; i++)
                    Assert.AreEqual(Math.Round(Stage(mid, i).Num("propMass") * k), Stage(d, i).Num("propMass"), "notch " + notch + " stage " + (i + 1));
            }
            Assert.AreEqual(new[] { -20, -10, 0, 10, 20 }, StarterRocket.FuelPercents);
        }

        [Test]
        public void Engine_SwapsTheFuel_KeepsTheHydrogenTop_AndAboutTheSamePush()
        {
            var cat = StarterFixture.Catalog;
            var r = R("m5");
            var kero = r.BuildDesign();
            r.Engine = StarterEngine.Methane;
            var meth = r.BuildDesign();
            Assert.AreEqual("methalox", Stage(meth, 0).Str("propellant"));
            Assert.AreEqual("methalox", cat.EngineById[Stage(meth, 0).Str("engine")].propellant, "engine matches the fuel");
            Assert.AreEqual("hydrolox", Stage(meth, 1).Str("propellant"), "the hydrogen upper is not a control");
            Assert.AreEqual(Snap(Stage(kero, 1)), Snap(Stage(meth, 1)));
            double Push(JsonObject d) => cat.EngineById[Stage(d, 0).Str("engine")].thrustVac * Stage(d, 0).Num("engineCount");
            Assert.That(Push(meth) / Push(kero), Is.InRange(0.5, 1.6), "about the same total push");
            Assert.IsTrue(StarterRocket.Fits(cat, Stage(meth, 0)), "the new engines fit under the stage");

            var kh3 = R("m3"); // methalox first stage
            kh3.Engine = StarterEngine.Kerosene;
            Assert.AreEqual("kerolox", Stage(kh3.BuildDesign(), 0).Str("propellant"));
        }

        [Test]
        public void Boosters_FollowTheMissionRules()
        {
            var kh5 = R("m5");
            Assert.IsTrue(kh5.BoostersVisible);
            Assert.IsFalse(kh5.BoostersLocked);
            foreach (int n in StarterRocket.BoosterValues)
            {
                kh5.Boosters = n;
                Assert.AreEqual(n, (int)kh5.BuildDesign().Obj("strapOns").Num("count"), "KH-5 strap-ons " + n);
            }
            var kh10 = R("m10");
            Assert.IsTrue(kh10.BoostersVisible);
            Assert.IsTrue(kh10.BoostersLocked);
            kh10.Boosters = 0;
            Assert.AreEqual(2, kh10.Boosters, "KH-10 flies its landing pair");
            Assert.AreEqual(2, (int)kh10.BuildDesign().Obj("strapOns").Num("count"));
            var kh3 = R("m3");
            Assert.IsFalse(kh3.BoostersVisible);
            kh3.Boosters = 4;
            Assert.AreEqual(0, kh3.Boosters);
        }

        [Test]
        public void ControlsRaiseChangedOnce_AreDeterministic_AndResetGoesBack()
        {
            var r = R("m5");
            int changes = 0;
            r.Changed += _ => changes++;
            r.Stages = 3;
            r.Stages = 3;
            Assert.AreEqual(1, changes, "same value: no change");
            var a = Snap(r.BuildDesign());
            Assert.AreEqual(a, Snap(R("m5").BuildDesign(new StarterChoice(StarterEngine.Kerosene, 3, 2, 0))), "same setting, same JSON");
            r.Reset();
            Assert.AreEqual(2, changes);
            Assert.AreEqual(r.Default, r.Choice);
            r.Stages = 7;
            Assert.AreEqual(r.Default.Stages, r.Stages, "out of range is ignored");
        }

        [Test]
        public void Save_RoundTripsTheSetting_AndTheBuiltRocket()
        {
            var r = R("m19");
            r.Engine = StarterEngine.Methane; r.Stages = 2; r.FuelNotch = 4;
            var saved = MiniJson.ParseObject(HangarSession.Snapshot(r.ToSave()));
            Assert.AreEqual(2, (int)saved.Num("v"));
            var back = R("m19");
            Assert.IsTrue(back.ApplySave(saved));
            Assert.AreEqual(r.Choice, back.Choice);
            Assert.AreEqual(Snap(r.BuildDesign()), Snap(back.BuildDesign()));
            Assert.IsFalse(R("m18").ApplySave(saved), "another mission's record");
            var v1 = MiniJson.ParseObject("{\"v\":1,\"mission\":\"m19\",\"engine\":\"kerosene\",\"stages\":3,\"fuelNotch\":1,\"boosters\":2}");
            Assert.IsTrue(back.ApplySave(v1), "the dropped placeholder's v1 records still read");
            Assert.AreEqual("kerosene-3-f1-b2", back.Choice.Key);
        }

        [Test]
        public void EverySetting_OnEveryMission_Builds_OnlyTooHeavyCanBlock()
        {
            // The mapping must never invent a schema, fit or cargo block; only Validate's twr-low (a rocket that
            // cannot leave the pad) may stop Launch. Everything else launches, short or not.
            var bad = new List<string>();
            int combos = 0, heavy = 0;
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = R(m.Id);
                foreach (var e in StarterRocket.EngineOrder)
                    foreach (int s in StarterRocket.StageValues)
                        for (int f = 0; f < StarterRocket.FuelNotches; f++)
                            foreach (int b in StarterRocket.BoosterValues)
                            {
                                if (!r.IsBoostersAllowed(b)) continue;
                                var d = r.BuildDesign(new StarterChoice(e, s, f, b));
                                combos++;
                                Assert.AreEqual(s, Stages(d).Count, m.Id);
                                Assert.AreEqual(m.MassKg, d.Obj("payload").Num("mass"), m.Id + " payload locked to the mission");
                                var c = StarterRocket.CheckDesign(StarterFixture.Catalog, d, m);
                                if (!c.Blocked) continue;
                                if (c.BlockCode == "twr-low") { heavy++; continue; }
                                bad.Add(m.Id + " " + new StarterChoice(e, s, f, b).Key + ": " + c.BlockCode + " " + c.BlockReason);
                            }
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
            Assert.AreEqual(720, combos, "20 missions x 2 x 3 x 5 x their booster choices");
            Assert.Less(heavy, combos / 20, "too heavy is rare");
        }
    }
}
