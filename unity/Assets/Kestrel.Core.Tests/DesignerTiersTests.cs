// Designer tiers, delivery step 2 (docs/DESIGNER_TIERS_SPEC.md): the Kid / Builder / Engineer switch (persistence,
// resolution, legacy prefs, the flags that hide Builder / Engineer), the Kid Why? copy (data/designer-tiers.json, one
// card per Starter row, kid wording, at most two sentences, a real Codex lesson), the speed bar and the Starter layout
// with the "?" column, an open Why? card, the speed bar, the X-ray button and the switch at every device size.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kestrel.Core.Codex;
using Kestrel.Core.Designer;
using Kestrel.Core.Features;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignerTiersTests
    {
        sealed class Prefs
        {
            public readonly Dictionary<string, int> Map = new Dictionary<string, int>();
            public int Writes;
            public bool Has(string k) => Map.ContainsKey(k);
            public int Get(string k) => Map.TryGetValue(k, out var v) ? v : 0;
            public void Set(string k, int v) { Map[k] = v; Writes++; }
        }

        static DesignerTierText Text => DesignerTierText.Load(Golden.Root);

        static FeatureFlags Shipped() => FeatureFlags.Load(Golden.Root);

        static FeatureFlags AllTiers()
        {
            var f = Shipped();
            f.SetOverride(FeatureKeys.DesignerTierBuilder, true);
            f.SetOverride(FeatureKeys.DesignerTierEngineer, true);
            return f;
        }

        // ---- tier switch ----

        [Test]
        public void Shipped_OnlyKid_NoSwitch_AndNoFileIsTheSame()
        {
            foreach (var flags in new[] { Shipped(), new FeatureFlags(), null })
            {
                var shown = DesignerTiers.Shown(flags);
                Assert.AreEqual(1, shown.Count);
                Assert.AreEqual(ExperienceTier.Starter, shown[0]);
                Assert.IsFalse(DesignerTiers.ShowSwitch(flags), "Builder / Engineer chips wait for their controls");
            }
            var all = AllTiers();
            var allShown = DesignerTiers.Shown(all);
            Assert.AreEqual(3, allShown.Count);
            for (int k = 0; k < 3; k++) Assert.AreEqual(DesignerTiers.Order[k], allShown[k]);
            Assert.IsTrue(DesignerTiers.ShowSwitch(all));
            var builderOnly = Shipped();
            builderOnly.SetOverride(FeatureKeys.DesignerTierBuilder, true);
            var two = DesignerTiers.Shown(builderOnly);
            Assert.AreEqual(2, two.Count);
            Assert.AreEqual(ExperienceTier.Builder, two[1]);
        }

        [Test]
        public void Resolve_FreshSaveIsKid_LegacyEngineerShowsKidUntilOffered_AndNeverWrites()
        {
            var p = new Prefs();
            Assert.AreEqual(ExperienceTier.Starter, DesignerTiers.Resolve(Shipped(), p.Has, p.Get), "fresh save: Kid");

            // A save from before the tiers: the old engineer toggles read as Engineer (ExperienceSettings).
            foreach (var legacy in new[] { ExperienceSettings.LegacyDesignerEngineerPref, ExperienceSettings.LegacyHangarEngineerPref })
            {
                var old = new Prefs();
                old.Set(legacy, 1);
                old.Writes = 0;
                Assert.AreEqual(ExperienceTier.Engineer, ExperienceSettings.Tier(old.Has, old.Get), legacy);
                Assert.AreEqual(ExperienceTier.Starter, DesignerTiers.Resolve(Shipped(), old.Has, old.Get), legacy + ": Engineer chip hidden, so Kid");
                Assert.AreEqual(ExperienceTier.Engineer, DesignerTiers.Resolve(AllTiers(), old.Has, old.Get), legacy + ": Engineer once offered");
                Assert.AreEqual(0, old.Writes, "resolving never writes");
                Assert.AreEqual(1, old.Get(legacy), "the old toggle is kept for older builds");
            }

            var builder = new Prefs();
            builder.Set(ExperienceSettings.TierPref, 1);
            Assert.AreEqual(ExperienceTier.Starter, DesignerTiers.Resolve(Shipped(), builder.Has, builder.Get));
            Assert.AreEqual(ExperienceTier.Builder, DesignerTiers.Resolve(AllTiers(), builder.Has, builder.Get));
            // A stored 2 with both old prefs off was switched off since (ExperienceSettings): Builder.
            var stale = new Prefs();
            stale.Set(ExperienceSettings.TierPref, 2);
            Assert.AreEqual(ExperienceTier.Builder, DesignerTiers.Resolve(AllTiers(), stale.Has, stale.Get));
        }

        [Test]
        public void Choose_PersistsTheTier_RoundTrips_AndRefusesHiddenChips()
        {
            var p = new Prefs();
            var shipped = Shipped();
            Assert.IsFalse(DesignerTiers.Choose(shipped, ExperienceTier.Engineer, p.Has, p.Get, p.Set), "hidden chip");
            Assert.IsFalse(DesignerTiers.Choose(shipped, ExperienceTier.Builder, p.Has, p.Get, p.Set), "hidden chip");
            Assert.IsEmpty(p.Map, "a hidden chip writes nothing");

            Assert.IsTrue(DesignerTiers.Choose(shipped, ExperienceTier.Starter, p.Has, p.Get, p.Set), "Kid is stored on a tap");
            Assert.AreEqual(0, p.Get(ExperienceSettings.TierPref));
            Assert.IsFalse(DesignerTiers.Choose(shipped, ExperienceTier.Starter, p.Has, p.Get, p.Set), "already Kid: no write");

            var all = AllTiers();
            int changed = 0;
            all.Changed += _ => changed++;
            foreach (var t in new[] { ExperienceTier.Engineer, ExperienceTier.Builder, ExperienceTier.Starter, ExperienceTier.Engineer })
            {
                Assert.IsTrue(DesignerTiers.Choose(all, t, p.Has, p.Get, p.Set), t.ToString());
                Assert.AreEqual(t, DesignerTiers.Resolve(all, p.Has, p.Get), t + " round trip");
                Assert.AreEqual(t, ExperienceSettings.Tier(p.Has, p.Get), t + " in kh.settings.tier");
                Assert.AreEqual(t, all.Tier, t + " mirrored on the flags");
                int eng = t == ExperienceTier.Engineer ? 1 : 0;
                Assert.AreEqual(eng, p.Get(ExperienceSettings.LegacyDesignerEngineerPref), "old builds stay in step");
                Assert.AreEqual(eng, p.Get(ExperienceSettings.LegacyHangarEngineerPref), "old builds stay in step");
            }
            Assert.AreEqual(4, changed);
            // A fresh FeatureFlags reading the same prefs (next boot) gets the stored tier.
            var boot = AllTiers();
            boot.ApplyPrefs(p.Has, p.Get);
            Assert.AreEqual(ExperienceTier.Engineer, boot.Tier);
        }

        // ---- copy ----

        [Test]
        public void Copy_ParsesAndNamesEveryTier()
        {
            var t = Text;
            Assert.AreEqual("Kid", t.TierLabel(ExperienceTier.Starter));
            Assert.AreEqual("Builder", t.TierLabel(ExperienceTier.Builder));
            Assert.AreEqual("Engineer", t.TierLabel(ExperienceTier.Engineer));
            foreach (var k in StarterXray.EventKeys) Assert.IsNotEmpty(t.Event(k), k);
            foreach (var k in new[] { "fairing", "tank", "tankLanding", "tankSolid", "interstage", "engines", "boosters", "boostersLanding" })
            {
                Assert.IsNotEmpty(t.Part(k).Title, k);
                Assert.IsNotEmpty(t.Part(k).Line, k);
                Assert.LessOrEqual(DesignerTierText.Sentences(t.Part(k).Line), 2, k);
            }
            Assert.Throws<FormatException>(() => DesignerTierText.Parse("{\"version\":2}"));
        }

        static readonly Regex Jargon = new Regex(@"\d|\b(isp|twr|delta|m/s|km|thrust-to-weight|impulse|kg|newton)\b", RegexOptions.IgnoreCase);

        [Test]
        public void WhyCards_OnePerStarterRow_KidWording_AtMostTwoSentences_FromACodexLesson()
        {
            var t = Text;
            var codex = CodexBook.Load(Golden.Root);
            var rows = new[] { StarterKnob.Engine, StarterKnob.Stages, StarterKnob.FuelNotch, StarterKnob.Boosters };
            Assert.AreEqual(rows.Length, t.Why.Count, "one card per Starter row, no extras");
            foreach (var k in rows)
            {
                var w = t.WhyFor(k);
                Assert.That(w, Is.Not.Null, k + " has a Why? card");
                Assert.IsNotEmpty(w.Text, k.ToString());
                foreach (var text in w.Texts())
                {
                    int n = DesignerTierText.Sentences(text);
                    Assert.That(n, Is.InRange(1, DesignerTierText.WhyMaxSentences), k + ": " + text);
                    Assert.LessOrEqual(text.Length, DesignerTierText.WhyMaxChars, k + ": " + text);
                    Assert.IsFalse(Jargon.IsMatch(text), k + " kid wording (no numbers or jargon): " + text);
                    Assert.IsFalse(text.Contains("\u2014") || text.Contains("\u2013"), k + " no dashes");
                }
                Assert.That(codex.ById(w.Codex), Is.Not.Null, k + " links a real Codex lesson: " + w.Codex);
                Assert.IsTrue(Array.IndexOf(new[] { "flame", "stages", "fuel", "boosters" }, w.Icon) >= 0, k + " pictogram " + w.Icon);
                Assert.AreEqual(k, DesignerTierText.KnobOf(DesignerTierText.RowOf(k)));
            }
            Assert.AreEqual("An empty tank is dead weight. Two stages drop the big one halfway up, so the top part gets lighter and faster.",
                t.WhyFor(StarterKnob.Stages).Text, "the approved mockup's Stages card");
            StringAssert.Contains(StarterScreen.StagesLabel, t.WhySpokenFor(StarterScreen.StagesLabel));
            // The state hints the hint line carried now live on the cards.
            Assert.AreEqual(t.WhyFor(StarterKnob.Boosters).Locked, t.WhyText(StarterKnob.Boosters, StarterFixture.Rocket("m10")), "KH-10 locked boosters");
            Assert.AreEqual(t.WhyFor(StarterKnob.Boosters).Text, t.WhyText(StarterKnob.Boosters, StarterFixture.Rocket("m5")));
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = StarterFixture.Rocket(m.Id);
                Assert.AreEqual(StarterScreen.HydrogenAuto(r) ? t.WhyFor(StarterKnob.Stages).Hydrogen : t.WhyFor(StarterKnob.Stages).Text,
                    t.WhyText(StarterKnob.Stages, r), m.Id);
            }
            Assert.AreEqual(2, DesignerTierText.Sentences("One. Two!"));
            Assert.AreEqual(3, DesignerTierText.Sentences("One. Two? Three"));
        }

        // ---- speed bar ----

        [Test]
        public void SpeedBar_EveryMissionStarter_MatchesTheReadoutAndTheBadgeIsUntouched()
        {
            var t = Text;
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = StarterFixture.Rocket(m.Id);
                var s = StarterSpeedBar.Of(r, t);
                var c = r.Check();
                string where = m.Id + " " + r.Choice.Key;
                if (c.Blocked || !c.HasNumbers) { Assert.IsFalse(s.Visible, where); continue; }
                Assert.IsTrue(s.Visible, where);
                Assert.AreEqual(c.DeltaV, s.Have, 1e-9, where);
                Assert.That(s.Fill, Is.InRange(0.0, 1.0), where);
                Assert.That(s.NeedAt, Is.InRange(0.0, 1.0), where);
                Assert.AreEqual(s.Have >= s.Need, s.Enough, where);
                Assert.AreEqual(s.Enough, s.Fill >= s.NeedAt - 1e-12, where + " fill passes the marker exactly when enough");
                Assert.AreEqual(s.Enough ? t.SpeedEnough : t.SpeedShort, s.Status, where);
                if (m.TargetKind == "apogee")
                {
                    Assert.AreEqual(t.SpeedHop, s.Label, where);
                    Assert.IsTrue(s.Enough, where + ": the mission's own rocket flies its hop");
                }
                else
                {
                    Assert.AreEqual(c.Required, s.Need, 1e-9, where);
                    Assert.AreEqual(c.Margin >= 0, s.Enough, where + " agrees with the readout margin");
                    Assert.AreEqual(m.TargetKind == "c3" ? t.SpeedEscape : t.SpeedOrbit, s.Label, where);
                }
                StringAssert.StartsWith(s.Label, s.Spoken, where);
            }
            Assert.IsFalse(StarterSpeedBar.Of(StarterFixture.Rocket("m3"), null).Visible, "no copy, no bar");
            // A short setting reads Short.
            var shortR = StarterLayoutTests.Rocket("m7", "kerosene-3-f0-b0");
            var sb = StarterSpeedBar.Of(shortR, t);
            if (shortR.Check().Margin < 0) { Assert.IsFalse(sb.Enough); Assert.AreEqual(t.SpeedShort, sb.Status); }
        }

        // ---- layout ----

        public static IEnumerable<StarterLayoutTests.Size> TierSizes()
        {
            foreach (var s in StarterLayoutTests.Sizes()) yield return s;
            yield return new StarterLayoutTests.Size { Name = "SE 375x667", W = 375, H = 667, Top = 20 };
        }

        public static StarterLayoutInput Input(StarterLayoutTests.Size s, StarterRocket r, DesignerTierText t, StarterKnob why, bool tiers, string readiness = null)
        {
            StarterScreen.Readiness(r, out var badge, out var rest);
            if (readiness != null) rest = readiness;
            var speed = StarterSpeedBar.Of(r, t);
            return new StarterLayoutInput
            {
                Width = s.W, Height = s.H, SafeTop = s.Top, SafeBottom = s.Bottom,
                Wide = s.Wide, ReserveExit = s.Exit, BoostersVisible = r.BoostersVisible,
                Title = StarterScreen.Title(r.Mission), Subtitle = StarterScreen.Subtitle(r.Mission),
                Hint = StarterScreen.Hint(r, StarterKnob.None), Badge = badge, Readiness = rest,
                Help = true, HelpText = t.WhyButton, WhyOpen = why, WhyText = t.WhyText(why, r),
                Speed = speed.Visible, SpeedLabel = speed.Label, SpeedStatus = speed.Status,
                Tiers = tiers ? new[] { t.TierLabel(ExperienceTier.Starter), t.TierLabel(ExperienceTier.Builder), t.TierLabel(ExperienceTier.Engineer) } : null,
                Xray = true, XrayText = t.XrayButton,
            };
        }

        static readonly StarterKnob[] WhyStates = { StarterKnob.None, StarterKnob.Engine, StarterKnob.Stages, StarterKnob.FuelNotch, StarterKnob.Boosters };

        [Test]
        public void Layout_EverySize_EveryWhyCard_WithAndWithoutTheSwitch_NoOverlap_TargetsAndTextFit()
        {
            var t = Text;
            var fails = new List<string>();
            foreach (var s in TierSizes())
                foreach (var c in StarterLayoutTests.Cases())
                    foreach (var why in WhyStates)
                        foreach (bool tiers in new[] { false, true })
                        {
                            var r = StarterLayoutTests.Rocket(c.mission, c.key);
                            var input = Input(s, r, t, why, tiers, c.readiness);
                            var f = StarterLayout.Place(input);
                            string where = s + " " + c.name + " why=" + why + (tiers ? " switch" : "");
                            foreach (var p in StarterLayout.Problems(f, input)) fails.Add(where + ": " + p);
                            foreach (var row in f.Rows())
                                if (!row.Help.Live) fails.Add(where + ": " + row.Key + " has no ?");
                            bool open = why != StarterKnob.None && (why != StarterKnob.Boosters || r.BoostersVisible);
                            if (open != f.Why.Live) fails.Add(where + ": Why card live " + f.Why.Live);
                            if (f.Hint.Live) fails.Add(where + ": the ? cards replace the hint line");
                            if (tiers != (f.Chips.Length == 3)) fails.Add(where + ": chips " + f.Chips.Length);
                            if (input.Speed && !f.SpeedBar.Live && !(open && f.Compact)) fails.Add(where + ": speed bar missing");
                            if (f.SpeedBar.Live && f.SpeedBar.Bottom > f.Readiness.Y + 0.5f) fails.Add(where + ": speed bar under the readiness line");
                            if (!s.Wide)
                            {
                                // The rocket keeps its room while closed (the existing floors), and a window with a card open.
                                // The switch on its own row (portrait web 335 with its EXIT, behind the off-by-default tier flags) is
                                // the one narrow case: closed it keeps 12%, a card open may squeeze the window to a sliver.
                                float floor = open ? (f.Compact ? 0.08f : 0.12f) : (f.Compact ? 0.16f : 0.24f);
                                if (f.Switch.Live && f.Switch.Y > f.Back.Bottom) floor = open ? 0.02f : 0.12f;
                                if (f.Rocket.H < s.H * floor) fails.Add(where + ": rocket " + f.Rocket.H.ToString("0") + " under " + (floor * 100) + "%");
                            }
                            if (!open && !f.Xray.Live) fails.Add(where + ": X-ray button missing");
                        }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        [Test]
        public void Layout_TheWhyCardSitsUnderItsRow_AndTheLaunchRowNeverMoves()
        {
            var t = Text;
            var s = new StarterLayoutTests.Size { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34 };
            var r = StarterLayoutTests.Rocket("m5");
            var closed = StarterLayout.Place(Input(s, r, t, StarterKnob.None, false));
            foreach (var why in new[] { StarterKnob.Engine, StarterKnob.Stages, StarterKnob.FuelNotch, StarterKnob.Boosters })
            {
                var f = StarterLayout.Place(Input(s, r, t, why, false));
                var row = why == StarterKnob.Engine ? f.Engine : why == StarterKnob.Stages ? f.Stages : why == StarterKnob.FuelNotch ? f.Fuel : f.Boosters;
                Assert.AreEqual(row.Row.Bottom + f.GapPt, f.Why.Y, 0.5f, why + " card right under its row");
                Assert.AreEqual(closed.Launch.Y, f.Launch.Y, 0.01f, why + ": Launch stays put");
                Assert.AreEqual(closed.Readiness.Y, f.Readiness.Y, 0.01f, why + ": readiness stays put");
                Assert.AreEqual(f.Why.X, f.Engine.Row.X, 0.01f);
                Assert.AreEqual(f.Why.Right, f.Engine.Row.Right, 0.01f, "the card spans the row");
            }
            // KH-3 has no boosters row: a Boosters card is never placed.
            var kh3 = StarterLayout.Place(Input(s, StarterLayoutTests.Rocket("m3"), t, StarterKnob.Boosters, false));
            Assert.IsFalse(kh3.Why.Live);
        }

        [Test]
        public void Layout_SwitchRightOfBack_ResetMovesIntoTheHeaderOnPhones()
        {
            var t = Text;
            foreach (var s in TierSizes())
            {
                var f = StarterLayout.Place(Input(s, StarterLayoutTests.Rocket("m5"), t, StarterKnob.None, true));
                string where = s.ToString();
                Assert.AreEqual(3, f.Chips.Length, where);
                bool ownRow = f.Switch.Y > f.Back.Bottom;
                if (ownRow) Assert.IsTrue(s.Exit && !s.Wide, where + ": only the portrait web EXIT pushes the switch to its own row");
                else
                {
                    Assert.GreaterOrEqual(f.Chips[0].X, f.Back.Right, where + " right of Back");
                    Assert.AreEqual(f.Back.Y, f.Chips[0].Y, 0.01f, where + " on the top row");
                }
                foreach (var chip in f.Chips) Assert.GreaterOrEqual(chip.W, StarterLayout.Touch, where);
                Assert.IsFalse(f.Reset.Overlaps(f.Switch), where);
                if (f.ResetInHeader) Assert.Greater(f.Reset.Y, f.Back.Bottom, where + " Reset in the header");
                if (s.Wide) Assert.IsFalse(f.ResetInHeader, where);
            }
            var off = StarterLayout.Place(Input(new StarterLayoutTests.Size { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34 }, StarterLayoutTests.Rocket("m5"), t, StarterKnob.None, false));
            Assert.IsFalse(off.Switch.Live);
            Assert.IsFalse(off.ResetInHeader);
        }
    }
}
