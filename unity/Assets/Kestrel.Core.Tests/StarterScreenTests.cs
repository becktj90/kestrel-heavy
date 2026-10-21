// Starter screen (Phase 1 PR 5): the flag gate, Kestrel UX copy (STARTER_COPY.md), the readiness split
// and the save. Expected values come from the plan, the copy doc, data/campaign.json and the real readout.
using System;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Features;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterScreenTests
    {
        static StarterRocket R(string id) => StarterFixture.Rocket(id);

        /// <summary>A twr-low setting one fuel notch from flying (KH-6, kerosene, three stages, full fuel).</summary>
        public const string TooHeavyKey = "kerosene-3-f4-b0";

        /// <summary>Arcade's condition for keeping the twr-low block (16 of 720 settings at this commit, on KH-6 to KH-9, KH-17 and KH-18): the plain message shows and
        /// fits at 335 px, Launch is off, every control stays live, and one notch less fuel clears it on this screen.</summary>
        [Test]
        public void TooHeavy_At335_MessageFits_LaunchOff_ControlsLive_LessFuelClearsIt()
        {
            var heavy = StarterLayoutTests.Rocket("m6", TooHeavyKey);
            Assert.IsFalse(StarterScreen.Readiness(heavy, out var badge, out var rest));
            Assert.AreEqual("twr-low", heavy.Check().BlockCode);
            Assert.AreEqual(StarterScreen.HeavyBadge, badge);
            Assert.AreEqual("Too heavy to leave the pad. Try less fuel.", rest);
            Assert.IsFalse(heavy.CanLaunch, "Launch off");

            // On screen and fitting at 335x667 (the layout model's own text-fit, overlap and safe-area checks).
            var input = new StarterLayoutInput
            {
                Width = 335, Height = 667, BoostersVisible = heavy.BoostersVisible,
                Title = StarterScreen.Title(heavy.Mission), Subtitle = StarterScreen.Subtitle(heavy.Mission),
                Hint = StarterScreen.Hint(heavy, StarterKnob.None), Badge = badge, Readiness = rest,
            };
            var frame = StarterLayout.Place(input);
            Assert.That(StarterLayout.Problems(frame, input), Is.Empty, "335x667 too heavy");
            Assert.IsTrue(frame.Readiness.Live, "the message has a box on screen");
            Assert.LessOrEqual(frame.Readiness.Right, 335f + 0.5f);
            Assert.LessOrEqual(frame.Readiness.Bottom, 667f + 0.5f);

            // Every control is still live: each value the mission offers is accepted from the blocked state.
            foreach (var e in StarterRocket.EngineOrder) Assert.IsTrue(heavy.IsEngineAllowed(e), "engine " + e);
            foreach (var n in StarterRocket.StageValues) Assert.IsTrue(heavy.IsStagesAllowed(n), "stages " + n);
            for (int k = 0; k < StarterRocket.FuelNotches; k++) Assert.IsTrue(heavy.IsFuelNotchAllowed(k), "fuel " + k);
            Assert.IsFalse(heavy.BoostersLocked, "KH-6 boosters are not locked");

            // Lowering fuel there clears the block and turns Launch back on.
            heavy.FuelNotch = 3;
            Assert.AreEqual("kerosene-3-f3-b0", heavy.Choice.Key);
            Assert.IsFalse(heavy.Check().Blocked, "one notch less fuel lifts off");
            Assert.IsTrue(heavy.CanLaunch);
            Assert.IsTrue(StarterScreen.Readiness(heavy, out badge, out rest));
            Assert.AreNotEqual(StarterScreen.HeavyBadge, badge);
        }

        [Test]
        public void DefaultFlags_OpenStarter_AndTheFullDesignerToggleRestoresTheExistingOne()
        {
            // data/features.json (loaded at boot by #300) ships every designer.* flag off. The built-in
            // defaults stay on, so a build without the file keeps the existing designer.
            Assert.IsFalse(StarterScreen.UseStarter(new FeatureFlags()), "built-in defaults: existing designer");
            var flags = FeatureFlags.Load(Golden.Root);
            Assert.IsTrue(StarterScreen.UseStarter(flags), "data/features.json: Starter");
            foreach (var k in FeatureKeys.All)
                if (k.StartsWith("designer.", StringComparison.Ordinal)) Assert.IsFalse(StarterScreen.Shows(flags, k), k);

            int changes = 0;
            flags.Changed += _ => changes++;
            flags.ShowFullDesigner = true;
            Assert.AreEqual(1, changes);
            Assert.IsFalse(StarterScreen.UseStarter(flags));
            foreach (var k in FeatureKeys.All)
                if (k.StartsWith("designer.", StringComparison.Ordinal)) Assert.IsTrue(StarterScreen.Shows(flags, k), k);
            flags.ShowFullDesigner = false;
            Assert.IsTrue(StarterScreen.UseStarter(flags));

            // One screen flag is enough for the existing designer; its pill and Undo/Redo still follow their own flags.
            flags.SetOverride(FeatureKeys.DesignerStepper, true);
            Assert.IsFalse(StarterScreen.UseStarter(flags));
            Assert.IsTrue(StarterScreen.Shows(flags, FeatureKeys.DesignerStepper));
            Assert.IsFalse(StarterScreen.Shows(flags, FeatureKeys.DesignerFixPill));
            Assert.IsFalse(StarterScreen.Shows(flags, FeatureKeys.DesignerUndoRedo));
            flags.ClearOverride(FeatureKeys.DesignerStepper);
            Assert.IsTrue(StarterScreen.UseStarter(flags));

            // A data file that switches a flag on also opens the existing designer.
            var parsed = FeatureFlags.Parse("{\"tier\":\"starter\",\"flags\":{\"designer.free\":true}}");
            Assert.IsFalse(StarterScreen.UseStarter(parsed));
        }

        [Test]
        public void Copy_FitsTheUxLimits()
        {
            foreach (var l in new[] { StarterScreen.EngineLabel, StarterScreen.StagesLabel, StarterScreen.FuelLabel, StarterScreen.BoostersLabel })
                Assert.LessOrEqual(l.Length, StarterScreen.LabelMax, l);
            foreach (var h in new[] { StarterScreen.EngineHint, StarterScreen.StagesHint, StarterScreen.FuelHint, StarterScreen.BoostersHint,
                         StarterScreen.HydrogenHint, StarterScreen.BoostersLockedHint, StarterScreen.ResetHint })
            {
                Assert.LessOrEqual(h.Length, StarterScreen.HintMax, h);
                Assert.IsFalse(h.Contains("!"), h);
                foreach (var banned in new[] { "easy", "just", "Just", "Easy" }) Assert.IsFalse(h.Contains(banned), h);
            }
            Assert.AreEqual("None", StarterScreen.BoosterText(0));
            Assert.AreEqual("Methane", StarterScreen.OptionText(StarterEngine.Methane));
            Assert.AreEqual("Fuel, 3 of 5", StarterScreen.FuelSpoken(2));
        }

        [Test]
        public void Header_ComesFromMissionData()
        {
            var m1 = R("m1").Mission;
            Assert.AreEqual("KH-1 · First Hop", StarterScreen.Title(m1));
            StringAssert.StartsWith("Hop to ", StarterScreen.Subtitle(m1));
            StringAssert.EndsWith(" km and back", StarterScreen.Subtitle(m1));
            var m6 = R("m6").Mission;
            Assert.IsTrue(StarterScreen.Subtitle(m6).EndsWith("land at the pad", StringComparison.Ordinal), "KH-6 lands back at the pad");
            var m7 = R("m7").Mission;
            Assert.IsTrue(StarterScreen.Subtitle(m7).EndsWith("land on Haven", StringComparison.Ordinal), "KH-7 lands on the Haven barge");
            for (int i = 1; i <= 20; i++)
            {
                var m = R("m" + i).Mission;
                Assert.LessOrEqual(m.Name.Length, 16, "KH-" + i + " name (UX: 16 or fewer)");
                Assert.IsNotEmpty(StarterScreen.Subtitle(m), "KH-" + i);
            }
        }

        [Test]
        public void Hint_StateHintsReplaceTheNormalOne()
        {
            var kh1 = R("m1");
            Assert.AreEqual(StarterScreen.EngineHint, StarterScreen.Hint(kh1, StarterKnob.None), "KH-1: one kerosene stage, no state hint");
            Assert.AreEqual(StarterScreen.StagesHint, StarterScreen.Hint(kh1, StarterKnob.Stages));
            Assert.AreEqual(StarterScreen.FuelHint, StarterScreen.Hint(kh1, StarterKnob.FuelNotch));

            var kh10 = R("m10");
            Assert.AreEqual(StarterScreen.BoostersLockedHint, StarterScreen.Hint(kh10, StarterKnob.None));
            Assert.AreEqual(StarterScreen.BoostersLockedHint, StarterScreen.Hint(kh10, StarterKnob.Boosters));

            var kh5 = R("m5");
            Assert.AreEqual(StarterScreen.BoostersHint, StarterScreen.Hint(kh5, StarterKnob.Boosters));
            Assert.IsTrue(StarterScreen.HydrogenAuto(kh5), "KH-5's winner has a hydrogen top stage");
            Assert.AreEqual(StarterScreen.HydrogenHint, StarterScreen.Hint(kh5, StarterKnob.Stages));
            kh5.Stages = 1;
            Assert.IsFalse(StarterScreen.HydrogenAuto(kh5), "one stage: no hydrogen upper");
            Assert.AreEqual(StarterScreen.StagesHint, StarterScreen.Hint(kh5, StarterKnob.Stages));
        }

        [Test]
        public void Readiness_IsTheRealReadout_PlainInformation_NeverABlockerBeyondHardBlocks()
        {
            // The KH-3 winner: the readout has margin to spare, so the green word and the target.
            var r = R("m3");
            Assert.IsTrue(StarterScreen.Readiness(r, out var badge, out var rest));
            Assert.AreEqual(StarterScreen.ReadyBadge, badge);
            Assert.AreEqual("Reaches 200 km with fuel to spare.", rest);
            Assert.AreEqual("Ready. Reaches 200 kilometres with fuel to spare.", StarterScreen.Spoken(badge, rest));
            Assert.Greater(r.Check().Margin, StarterScreen.TightMargin);

            // KH-7 with one notch less fuel and three stages: short on the readout, and Launch stays on.
            var shortOne = StarterLayoutTests.Rocket("m7", "kerosene-3-f0-b0");
            Assert.IsTrue(StarterScreen.Readiness(shortOne, out badge, out rest), "short is information, not a block");
            Assert.AreEqual(StarterScreen.ShortBadge, badge);
            StringAssert.StartsWith("About ", rest);
            StringAssert.Contains("% short of 35,786 km", rest);
            Assert.IsTrue(shortOne.CanLaunch, "a rocket that falls short still launches");
            Assert.Less(shortOne.Check().Margin, 0);

            // KH-6 methane, three stages, full fuel: Validate's hard twr-low block. The one plain reason, Launch off.
            var heavy = StarterLayoutTests.Rocket("m6", "methane-3-f4-b0");
            Assert.IsFalse(StarterScreen.Readiness(heavy, out badge, out rest));
            Assert.IsFalse(heavy.CanLaunch);
            Assert.AreEqual("twr-low", heavy.Check().BlockCode);
            Assert.AreEqual(StarterScreen.HeavyBadge, badge);
            Assert.AreEqual("Too heavy to leave the pad. Try less fuel.", rest);

            // KH-1/KH-2 hops never show the readout's orbital "short" (M-10): the winner reads Ready, and
            // other settings compare with the rocket that flew the hop.
            foreach (var id in new[] { "m1", "m2" })
            {
                var hop = R(id);
                Assert.IsTrue(StarterScreen.Readiness(hop, out badge, out rest), id);
                Assert.AreEqual(StarterScreen.ReadyBadge, badge, id);
                StringAssert.StartsWith("Hops to ", rest);
                hop.FuelNotch = 0;
                StarterScreen.Readiness(hop, out badge, out rest);
                Assert.AreNotEqual(StarterScreen.ShortBadge, badge, id);
                StringAssert.Contains("may fall short", rest);
                hop.FuelNotch = 4;
                StarterScreen.Readiness(hop, out badge, out rest);
                StringAssert.Contains("may overshoot", rest);
            }
            Assert.IsFalse(StarterScreen.Readiness(null, out badge, out rest));
        }

        [Test]
        public void EveryMissionWinner_ReadsAsTheReadoutSays_AndCanLaunch()
        {
            // With B-04 (#303) every campaign winner builds, so the default Starter rocket always launches.
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = R(m.Id);
                Assert.IsTrue(r.CanLaunch, m.Id + ": " + r.Check().BlockReason);
                Assert.IsTrue(StarterScreen.Readiness(r, out var badge, out var rest), m.Id);
                Assert.IsNotEmpty(badge, m.Id);
                Assert.IsNotEmpty(rest, m.Id);
                Assert.AreNotEqual(StarterScreen.ShortBadge, badge, m.Id + " the winner is never shown short");
            }
        }

        [Test]
        public void Save_RoundTripsThroughTheNativeStore()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-starter-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new NativeStore(dir, () => 1000, () => 0.5);
                var r = R("m5");
                Assert.IsFalse(StarterScreen.Restore(store, r), "nothing saved yet");
                r.Engine = StarterEngine.Methane;
                r.Boosters = 4;
                r.FuelNotch = 3;
                Assert.IsTrue(StarterScreen.Save(store, r));
                var back = R("m5");
                int changes = 0;
                back.Changed += _ => changes++;
                Assert.IsTrue(StarterScreen.Restore(store, back));
                Assert.AreEqual(r.Choice, back.Choice);
                Assert.AreEqual(Kestrel.Core.Hangar.HangarSession.Snapshot(r.BuildDesign()), Kestrel.Core.Hangar.HangarSession.Snapshot(back.BuildDesign()), "the restored setting builds the same rocket");
                Assert.AreEqual(1, changes);
                Assert.IsFalse(StarterScreen.Restore(store, R("m6")), "per-mission key");
                Assert.IsFalse(StarterScreen.Save(null, r));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
    }
}
