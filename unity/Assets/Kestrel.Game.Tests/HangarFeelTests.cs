// Hangar feedback (G3 part 3): which haptic plays, what VoiceOver hears, the designer-feel.json patterns,
// the haptics switch, and the shared kid-wording table behind both the U1 chip and the Hangar warnings.
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class HangarFeelTests
    {
        static string Root => Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static FeelState S(bool launch, int blocks = 0, double twr = 1.5, double dv = 9000, double need = 8000, string kid = null)
            => new FeelState { valid = true, launchEnabled = launch, blocks = blocks, liftoffTwr = twr, totalDv = dv, requiredDv = need, firstBlockKid = kid };

        [Test]
        public void Haptics_PayoffWhenReady_WarningOnNewBlock_SnapOtherwise()
        {
            Assert.AreEqual(Haptic.Payoff, HangarFeel.For(S(false, 1), S(true), stepped: true));
            Assert.AreEqual(Haptic.Warning, HangarFeel.For(S(true), S(false, 1), stepped: true));
            Assert.AreEqual(Haptic.Snap, HangarFeel.For(S(true), S(true), stepped: true));
            Assert.AreEqual(Haptic.Snap, HangarFeel.For(S(false, 2), S(false, 1), stepped: true), "fewer blocks is not a warning");
            Assert.AreEqual(Haptic.None, HangarFeel.For(S(true), S(true), stepped: false));
            Assert.AreEqual(Haptic.None, HangarFeel.For(S(true), default, stepped: true), "nothing to feel without a state");
        }

        [Test]
        public void Announce_SaysTheChange_ThePush_AndAGateFlip()
        {
            var step = new EditStep { what = "Stage 1 engines", from = "7", to = "9" };
            Assert.AreEqual("Stage 1 engines, 9. Liftoff push 1.38. Speed 113% of the trip.",
                HangarFeel.Announce(step, S(true, twr: 1.2), S(true, twr: 1.38, dv: 9000, need: 8000)));
            StringAssert.EndsWith("Launch unavailable. Too heavy to lift off.",
                HangarFeel.Announce(step, S(true), S(false, 1, twr: 0.8, kid: "Too heavy to lift off.")));
            StringAssert.EndsWith("Ready to launch.", HangarFeel.Announce(step, S(false, 1), S(true)));
            StringAssert.StartsWith("Undid: Stage 1 engines 7 to 9.", HangarFeel.Announce(step, S(true), S(true), undo: true));
            StringAssert.StartsWith("Redid: Stage 1 engines 7 to 9.", HangarFeel.Announce(step, S(true), S(true), redo: true));
            StringAssert.StartsWith("Added stage 3.", HangarFeel.Announce(new EditStep { what = "Added stage 3" }, S(true), S(true)));
            Assert.AreEqual("", HangarFeel.Announce(null, default, default));
        }

        [Test]
        public void Patterns_ComeFromDesignerFeelJson()
        {
            var p = HangarFeel.ParsePatterns(File.ReadAllText(Path.Combine(Root, "data", "designer-feel.json")));
            CollectionAssert.AreEqual(new[] { 12 }, HangarFeel.Pattern(Haptic.Snap, p));
            CollectionAssert.AreEqual(new[] { 18, 30, 18 }, HangarFeel.Pattern(Haptic.Payoff, p));
            CollectionAssert.AreEqual(new[] { 12 }, HangarFeel.Pattern(Haptic.Warning, p), "warnings reuse the snap pulse");
            Assert.IsEmpty(HangarFeel.Pattern(Haptic.None, p));
            Assert.IsEmpty(HangarFeel.ParsePatterns("{}"));
        }

        [Test]
        public void HapticsSwitch_SilencesPlayback_AnnouncementsAlwaysRecord()
        {
            int prev = PlayerPrefs.GetInt(HangarFeel.HapticsPref, 1);
            try
            {
                PlayerPrefs.SetInt(HangarFeel.HapticsPref, 1);
                HangarNative.Play(Haptic.Payoff);
                Assert.AreEqual(Haptic.Payoff, HangarNative.LastHaptic);
                PlayerPrefs.SetInt(HangarFeel.HapticsPref, 0);
                HangarNative.Play(Haptic.Warning);
                Assert.AreEqual(Haptic.Payoff, HangarNative.LastHaptic, "off means off");
                HangarNative.Announce("Ready to launch.");
                Assert.AreEqual("Ready to launch.", HangarNative.LastAnnouncement);
                Assert.IsFalse(HangarNative.SystemReduceMotion, "no iOS bridge in the Editor");
            }
            finally { PlayerPrefs.SetInt(HangarFeel.HapticsPref, prev); }
        }

        [Test]
        public void FeelState_ReadsTheSession()
        {
            var c = Catalog.LoadFromRepo(Root);
            var h = HangarData.LoadFromRepo(Root);
            var s = new HangarSession(c, h, HangarEditor.ForkTemplate(h.Template("workhorse")));
            var ok = FeelState.Of(s);
            Assert.IsTrue(ok.launchEnabled);
            Assert.AreEqual(0, ok.blocks);
            Assert.Greater(ok.liftoffTwr, 1);
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 1));
            var bad = FeelState.Of(s);
            Assert.IsFalse(bad.launchEnabled);
            Assert.GreaterOrEqual(bad.blocks, 1);
            Assert.AreEqual("Too heavy to lift off. Add engines to the bottom stage or use less fuel.", bad.firstBlockKid);
            Assert.AreEqual(Haptic.Warning, HangarFeel.For(ok, bad, true));
        }

        [Test]
        public void U1Wording_IsTheHangarTable()
        {
            Assert.AreSame(KidWords.Table, DesignerValidation.KidWording);
            Assert.AreEqual(KidWords.DvMarginThin, DesignerValidation.DvMarginThin);
            Assert.AreEqual(KidWords.Fallback, DesignerValidation.KidFallback);
        }
    }
}
