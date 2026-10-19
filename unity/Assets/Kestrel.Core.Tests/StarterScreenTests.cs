// Starter screen (Phase 1 PR 5): the flag gate, Kestrel UX copy (STARTER_COPY.md), the readiness split
// and the save. Expected values come from the plan, the copy doc and data/campaign.json.
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
        StarterTable table;

        [SetUp]
        public void Load() => table = StarterTable.LoadPlaceholder(Golden.Root);

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
                         StarterScreen.HydrogenHint, StarterScreen.OneStageHint, StarterScreen.BoostersLockedHint, StarterScreen.ResetHint })
            {
                Assert.LessOrEqual(h.Length, StarterScreen.HintMax, h);
                Assert.IsFalse(h.Contains("!"), h);
                foreach (var banned in new[] { "easy", "just", "Just", "Easy" }) Assert.IsFalse(h.Contains(banned), h);
            }
            Assert.AreEqual("Checking this rocket…", StarterScreen.Checking);
            Assert.AreEqual("None", StarterScreen.BoosterText(0));
            Assert.AreEqual("Methane", StarterScreen.OptionText(StarterEngine.Methane));
            Assert.AreEqual("Fuel, 3 of 5", StarterScreen.FuelSpoken(2));
        }

        [Test]
        public void Header_ComesFromMissionData()
        {
            var m1 = new StarterRocket(table, "m1").Mission;
            Assert.AreEqual("KH-1 · First Hop", StarterScreen.Title(m1));
            StringAssert.StartsWith("Hop to ", StarterScreen.Subtitle(m1));
            StringAssert.EndsWith(" km and back", StarterScreen.Subtitle(m1));
            var m6 = new StarterRocket(table, "m6").Mission;
            Assert.IsTrue(StarterScreen.Subtitle(m6).EndsWith("land at the pad", StringComparison.Ordinal), "KH-6 lands back at the pad");
            var m7 = new StarterRocket(table, "m7").Mission;
            Assert.IsTrue(StarterScreen.Subtitle(m7).EndsWith("land on Haven", StringComparison.Ordinal), "KH-7 lands on the Haven barge");
            for (int i = 1; i <= 20; i++)
            {
                var m = new StarterRocket(table, "m" + i).Mission;
                Assert.LessOrEqual(m.Name.Length, 16, "KH-" + i + " name (UX: 16 or fewer)");
                Assert.IsNotEmpty(StarterScreen.Subtitle(m), "KH-" + i);
            }
        }

        [Test]
        public void Hint_StateHintsReplaceTheNormalOne()
        {
            var kh1 = new StarterRocket(table, "m1");
            Assert.AreEqual(StarterScreen.OneStageHint, StarterScreen.Hint(kh1, StarterKnob.None), "KH-1 opens on its locked stages");
            Assert.AreEqual(StarterScreen.OneStageHint, StarterScreen.Hint(kh1, StarterKnob.Stages));
            Assert.AreEqual(StarterScreen.EngineHint, StarterScreen.Hint(kh1, StarterKnob.Engine));
            Assert.AreEqual(StarterScreen.FuelHint, StarterScreen.Hint(kh1, StarterKnob.FuelNotch));

            var kh10 = new StarterRocket(table, "m10");
            Assert.AreEqual(StarterScreen.BoostersLockedHint, StarterScreen.Hint(kh10, StarterKnob.None));
            Assert.AreEqual(StarterScreen.BoostersLockedHint, StarterScreen.Hint(kh10, StarterKnob.Boosters));

            var kh5 = new StarterRocket(table, "m5");
            Assert.AreEqual(StarterScreen.BoostersHint, StarterScreen.Hint(kh5, StarterKnob.Boosters));
            Assert.AreEqual(StarterScreen.StagesHint, StarterScreen.Hint(kh5, StarterKnob.Stages));

            var kh9 = new StarterRocket(table, "m9");
            Assert.IsTrue(StarterScreen.HydrogenAuto(kh9), "KH-9 on: the top stage burns hydrogen");
            Assert.AreEqual(StarterScreen.HydrogenHint, StarterScreen.Hint(kh9, StarterKnob.Engine));
            Assert.IsFalse(StarterScreen.HydrogenAuto(new StarterRocket(table, "m4")));
        }

        [Test]
        public void Readiness_SplitsTheGreenWord_AndNeverSaysReadyWithoutAResult()
        {
            var r = new StarterRocket(table, "m3");
            Assert.IsTrue(StarterScreen.Readiness(r, false, out var badge, out var rest));
            Assert.AreEqual(StarterScreen.ReadyBadge, badge);
            Assert.IsFalse(rest.StartsWith("Ready", StringComparison.Ordinal), rest);
            Assert.IsNotEmpty(rest);
            StringAssert.Contains("kilometres", StarterScreen.Spoken(badge, rest));
            StringAssert.StartsWith("Ready. ", StarterScreen.Spoken(badge, rest));

            Assert.IsFalse(StarterScreen.Readiness(r, true, out badge, out rest), "the host gate is blocked");
            Assert.AreEqual("", badge);
            Assert.AreEqual(StarterScreen.Checking, rest);
            Assert.IsFalse(StarterScreen.Readiness(null, false, out badge, out rest));
            Assert.AreEqual(StarterScreen.Checking, rest);
        }

        [Test]
        public void Save_RoundTripsThroughTheNativeStore()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-starter-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new NativeStore(dir, () => 1000, () => 0.5);
                var r = new StarterRocket(table, "m5");
                Assert.IsFalse(StarterScreen.Restore(store, r), "nothing saved yet");
                r.Boosters = 4;
                r.FuelNotch = 3;
                Assert.IsTrue(StarterScreen.Save(store, r));
                var back = new StarterRocket(table, "m5");
                int changes = 0;
                back.Changed += _ => changes++;
                Assert.IsTrue(StarterScreen.Restore(store, back));
                Assert.AreEqual(r.Choice, back.Choice);
                Assert.AreEqual(1, changes);
                Assert.IsFalse(StarterScreen.Restore(store, new StarterRocket(table, "m6")), "per-mission key");
                Assert.IsFalse(StarterScreen.Save(null, r));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
    }
}
