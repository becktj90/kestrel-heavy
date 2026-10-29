// Phase 1 PR 3: one designer tier replaces kh.designer.v3.engineer, kh.hangar.engineer and
// SliceApp.EngineerEnabled. These tests pin the migration and that nothing changes for an
// existing player: the old engineer answer (GuideHost.EngineerDepth before this PR was
// v3 != 0 || hangar != 0) and the old layer resolution must come out the same.
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Features;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ExperienceSettingsTests
    {
        sealed class Prefs
        {
            public readonly Dictionary<string, int> Map = new Dictionary<string, int>();
            public bool Has(string k) => Map.ContainsKey(k);
            public int Get(string k) => Map.TryGetValue(k, out var v) ? v : 0;
            public void Set(string k, int v) => Map[k] = v;
        }

        static readonly int?[] States = { null, 0, 1 };

        [Test]
        public void FreshSave_IsTodaysDefault()
        {
            var p = new Prefs();
            Assert.AreEqual(ExperienceTier.Starter, ExperienceSettings.Tier(p.Has, p.Get));
            Assert.IsFalse(ExperienceSettings.IsEngineer(p.Has, p.Get));
            Assert.IsFalse(ExperienceSettings.ShowFullDesigner(p.Has, p.Get), "Show full designer is off by default");
            var flags = FeatureFlags.Load(Golden.Root);
            flags.ApplyPrefs(p.Has, p.Get);
            // PR 6 (home-simple) and PR 5 (designer-starter): a fresh save gets the Starter Home and the Starter designer (home.* and designer.* off).
            foreach (var k in FeatureKeys.All) Assert.AreEqual(!k.StartsWith("home.") && !k.StartsWith("designer."), flags.IsOn(k), k);
            Assert.IsEmpty(p.Map, "reading never writes");
        }

        [Test]
        public void EveryOldPrefCombination_KeepsItsEngineerAnswer_AndLayers()
        {
            foreach (var v3 in States)
                foreach (var hangar in States)
                    foreach (var tier in new int?[] { null, 0, 1, 2 })
                    {
                        var p = new Prefs();
                        if (v3.HasValue) p.Set("kh.designer.v3.engineer", v3.Value);
                        if (hangar.HasValue) p.Set("kh.hangar.engineer", hangar.Value);
                        if (tier.HasValue) p.Set("kh.settings.tier", tier.Value);
                        string where = $"v3 {v3} hangar {hangar} tier {tier}";
                        bool old = (v3 ?? 0) != 0 || (hangar ?? 0) != 0;
                        Assert.AreEqual(old, ExperienceSettings.IsEngineer(p.Has, p.Get), where);
                        var t = ExperienceSettings.Tier(p.Has, p.Get);
                        Assert.AreEqual(old, t == ExperienceTier.Engineer, where);
                        if (!old) Assert.AreEqual((tier ?? 0) >= 1 ? ExperienceTier.Builder : ExperienceTier.Starter, t, where);
                        foreach (int orbits in new[] { 0, 1, 3 })
                            foreach (var layer in new[] { "simple", "standard", "engineer", null })
                                Assert.AreEqual(Experience.Resolve(orbits, old, layer), Experience.Resolve(orbits, t == ExperienceTier.Engineer, layer), where + " " + layer);
                    }
        }

        [Test]
        public void LayerNames_MapToTiers()
        {
            Assert.AreEqual(ExperienceTier.Starter, ExperienceSettings.FromLayer("simple"));
            Assert.AreEqual(ExperienceTier.Builder, ExperienceSettings.FromLayer("standard"));
            Assert.AreEqual(ExperienceTier.Engineer, ExperienceSettings.FromLayer("engineer"));
            Assert.AreEqual(ExperienceTier.Starter, ExperienceSettings.FromLayer(null));
            foreach (ExperienceTier t in new[] { ExperienceTier.Starter, ExperienceTier.Builder, ExperienceTier.Engineer })
            {
                Assert.AreEqual(t, ExperienceSettings.FromLayer(ExperienceSettings.Layer(t)));
                Assert.IsTrue(ExperienceSettings.TryTier(ExperienceSettings.TierId(t), out var back));
                Assert.AreEqual(t, back);
            }
        }

        [Test]
        public void SetTier_WritesTheTierAndKeepsOldBuildsInStep()
        {
            var p = new Prefs();
            ExperienceSettings.SetTier(ExperienceTier.Engineer, p.Set);
            Assert.AreEqual(1, p.Get("kh.designer.v3.engineer"));
            Assert.AreEqual(1, p.Get("kh.hangar.engineer"));
            Assert.AreEqual(ExperienceTier.Engineer, ExperienceSettings.Tier(p.Has, p.Get));
            ExperienceSettings.SetTier(ExperienceTier.Builder, p.Set);
            Assert.AreEqual(0, p.Get("kh.designer.v3.engineer"));
            Assert.AreEqual(ExperienceTier.Builder, ExperienceSettings.Tier(p.Has, p.Get));
            ExperienceSettings.SetShowFullDesigner(true, p.Set);
            var flags = new FeatureFlags();
            flags.ApplyPrefs(p.Has, p.Get);
            Assert.IsTrue(flags.ShowFullDesigner);
            Assert.AreEqual(ExperienceTier.Builder, flags.Tier, "Show full designer does not change the tier");
        }

        [Test]
        public void SettingsFile_CarriesTheNewKeys_AndStillTheOldOnes()
        {
            CollectionAssert.Contains(SettingsMirror.Keys, ExperienceSettings.TierPref);
            CollectionAssert.Contains(SettingsMirror.Keys, ExperienceSettings.ShowFullDesignerPref);
            CollectionAssert.Contains(SettingsMirror.Keys, ExperienceSettings.LegacyDesignerEngineerPref);
            CollectionAssert.Contains(SettingsMirror.Keys, ExperienceSettings.LegacyHangarEngineerPref);
            var p = new Prefs();
            p.Set("kh.settings.tier", 1);
            p.Set("kh.settings.fullDesigner", 1);
            var data = SettingsMirror.Capture(p.Has, p.Get);
            Assert.AreEqual(1.0, System.Convert.ToDouble(data["kh.settings.tier"]));
            Assert.AreEqual(1.0, System.Convert.ToDouble(data["kh.settings.fullDesigner"]));
        }

        // ---- Settings > Show corner character (Thrum's corner portrait) ----

        [Test]
        public void CornerCharacter_IsOffWithNoStoredValue_AndReadingNeverWrites()
        {
            var p = new Prefs();
            Assert.IsFalse(ExperienceSettings.CornerCharacter(p.Has, p.Get));
            Assert.IsEmpty(p.Map, "reading never writes");
            Assert.IsFalse(ExperienceSettings.CornerCharacter(null, null), "no prefs at all is off");
        }

        [Test]
        public void CornerCharacter_ExistingInstallIsOff_WhateverElseItStored()
        {
            // A save from a build before this setting: every other pref on, this key never written.
            var p = new Prefs();
            foreach (var k in SettingsMirror.Keys) if (k != ExperienceSettings.CornerCharacterPref) p.Set(k, 1);
            Assert.IsFalse(p.Has(ExperienceSettings.CornerCharacterPref));
            Assert.IsFalse(ExperienceSettings.CornerCharacter(p.Has, p.Get));
            // Only a stored 1 turns it on; a stray 0, 2 or -1 does not.
            foreach (int v in new[] { 0, 2, -1 })
            {
                p.Set(ExperienceSettings.CornerCharacterPref, v);
                Assert.IsFalse(ExperienceSettings.CornerCharacter(p.Has, p.Get), "stored " + v);
            }
        }

        [Test]
        public void CornerCharacter_ToggleSurvivesAReload_AndTheSettingsFile()
        {
            var p = new Prefs();
            ExperienceSettings.SetCornerCharacter(true, p.Set);
            Assert.AreEqual(1, p.Get("kh.settings.cornerCharacter"));
            var reloaded = new Prefs();
            foreach (var kv in p.Map) reloaded.Set(kv.Key, kv.Value);
            Assert.IsTrue(ExperienceSettings.CornerCharacter(reloaded.Has, reloaded.Get));

            // The WebGL path: the settings file carries the key and puts it back on a fresh device.
            CollectionAssert.Contains(SettingsMirror.Keys, ExperienceSettings.CornerCharacterPref);
            var data = SettingsMirror.Capture(p.Has, p.Get);
            Assert.AreEqual(1.0, System.Convert.ToDouble(data["kh.settings.cornerCharacter"]));

            ExperienceSettings.SetCornerCharacter(false, p.Set);
            Assert.AreEqual(0, p.Get("kh.settings.cornerCharacter"));
            Assert.IsFalse(ExperienceSettings.CornerCharacter(p.Has, p.Get));
        }

        [Test]
        public void CornerCharacter_DoesNotTouchTierOrReduceMotion()
        {
            var p = new Prefs();
            p.Set("kh.a11y.reduceMotion", 1);
            ExperienceSettings.SetCornerCharacter(true, p.Set);
            Assert.AreEqual(1, p.Get("kh.a11y.reduceMotion"));
            Assert.AreEqual(ExperienceTier.Starter, ExperienceSettings.Tier(p.Has, p.Get));
            Assert.IsFalse(ExperienceSettings.ShowFullDesigner(p.Has, p.Get));
            Assert.AreEqual(2, p.Map.Count, "only the corner key was added");
        }
    }
}
