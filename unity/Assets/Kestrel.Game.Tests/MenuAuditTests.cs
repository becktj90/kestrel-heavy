// First-launch walk of Home, the mission board, patches, settings, codex and back.
// KESTREL_REPO_ROOT is an empty directory. Fonts come from Resources. Campaign rows
// are loaded by walking the test process cwd, which the game code under test does not do.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Game.Audio;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class MenuAuditTests
    {
        const long FirstBuildMs = 4000;
        const long SecondBuildMs = 1500;

        string _prev;
        string _away;
        bool _music, _sfx, _voice, _subs, _sandbox;
        int _motion;

        [SetUp]
        public void EmptyDevice()
        {
            _away = Path.Combine(Path.GetTempPath(), "kh-menu-audit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_away);
            _prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _away);
            _music = AudioPrefs.MusicMuted;
            _sfx = AudioPrefs.SfxMuted;
            _voice = AudioPrefs.NarrationMuted;
            _subs = AudioPrefs.SubtitlesOn;
            _sandbox = AudioPrefs.SandboxUnlocked;
            _motion = PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0);
            AudioPrefs.SetMusicMuted(false);
            AudioPrefs.SetSfxMuted(false);
            AudioPrefs.SetNarrationMuted(false);
            AudioPrefs.SetSubtitlesOn(true);
            AudioPrefs.SetSandboxUnlocked(true);
            PlayerPrefs.SetInt(MainMenuLayout.ReduceMotionPref, 0);
            PlayerPrefs.Save();
            AudioPrefs.Load();
        }

        [TearDown]
        public void RestoreDevice()
        {
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _prev);
            AudioPrefs.SetMusicMuted(_music);
            AudioPrefs.SetSfxMuted(_sfx);
            AudioPrefs.SetNarrationMuted(_voice);
            AudioPrefs.SetSubtitlesOn(_subs);
            AudioPrefs.SetSandboxUnlocked(_sandbox);
            PlayerPrefs.SetInt(MainMenuLayout.ReduceMotionPref, _motion);
            PlayerPrefs.Save();
            MenuPrefs.SubtitlesOn = AudioPrefs.SubtitlesOn;
            MenuPrefs.SandboxUnlocked = AudioPrefs.SandboxUnlocked;
            try { if (!string.IsNullOrEmpty(_away) && Directory.Exists(_away)) Directory.Delete(_away, true); }
            catch (Exception) { }
        }

        [Test]
        public void FreshSaveWalksEveryMenuScreenWithoutThrowing()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            Assert.That(ScreenStub.RepoRoot(), Is.Null);
            Assert.That(Resources.Load<Font>("KHFonts/Fraunces-Black"), Is.Not.Null);
            Assert.That(Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium"), Is.Not.Null);
            Assert.DoesNotThrow(() => ScreenStub.LoadCampaign());

            string repo = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            var campaign = Campaign.Load(repo);
            string json = File.ReadAllText(Path.Combine(repo, "data", "campaign.json"));
            var labels = MissionMapText.ReadPayloadLabels(json);
            long minor = MissionMapText.MinorPerKc(json);
            Assert.That(campaign.Missions.Count, Is.EqualTo(20));

            WalkBuildCost(campaign, labels);
            WalkHome();
            WalkBoard(campaign, labels, minor, 0);
            WalkBoard(campaign, labels, minor, 10);
            WalkBoard(campaign, labels, minor, 20);
            WalkPatches(campaign);
            WalkCodex();
            WalkBriefingAndReport();
            WalkLegacyAndCorruptSaves(campaign, minor);
        }

        void WalkHome()
        {
            int launch = 0, design = 0, missions = 0;
            var full = new VisualElement { name = "full" };
            var safe = new VisualElement { name = "safe" };
            full.Add(AudioBar.Build());
            VisualElement root = null;
            Assert.DoesNotThrow(() =>
                root = MainMenuOverlay.Build(full, safe, () => launch++, () => design++, () => missions++, true));
            Press(root.Q<Button>("home-launch"));
            Press(root.Q<Button>("home-design"));
            Press(root.Q<Button>("home-missions"));
            Assert.That(launch, Is.EqualTo(1));
            Assert.That(design, Is.EqualTo(1));
            Assert.That(missions, Is.EqualTo(1));

            var codex = root.Q<Button>("home-codex");
            Assert.That(codex.enabledSelf, Is.False);
            Assert.That(codex.tooltip, Does.Contain("Coming soon"));

            var gear = full.Q<Button>("home-settings");
            Assert.That(gear.style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f));
            var sheet = full.Q("home-settings-sheet");
            Assert.That(sheet.style.display.value, Is.EqualTo(DisplayStyle.None));
            Press(gear);
            Assert.That(sheet.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            Toggle("home-setting-music", "Music", () => AudioPrefs.MusicMuted, full);
            Toggle("home-setting-sound", "Sound", () => AudioPrefs.SfxMuted, full);
            Toggle("home-setting-voice", "Voice", () => AudioPrefs.NarrationMuted, full);
            Assert.That(full.Q<Button>("audio-music").text, Is.EqualTo(AudioPrefs.Caption("Music", AudioPrefs.MusicMuted)));
            Assert.That(full.Q<Button>("audio-sfx").text, Is.EqualTo(AudioPrefs.Caption("Sound", AudioPrefs.SfxMuted)));
            Assert.That(full.Q<Button>("audio-narration").text, Is.EqualTo(AudioPrefs.Caption("Voice", AudioPrefs.NarrationMuted)));

            var subs = full.Q<Button>("home-setting-subtitles");
            Assert.That(subs.text, Is.EqualTo("Subtitles on"));
            Press(subs);
            Assert.That(AudioPrefs.SubtitlesOn, Is.False);
            Assert.That(MenuPrefs.SubtitlesOn, Is.False);
            Assert.That(subs.text, Is.EqualTo("Subtitles off"));
            Press(subs);
            Assert.That(MenuPrefs.SubtitlesOn, Is.True);

            var sandbox = full.Q<Button>("home-setting-sandbox");
            Assert.That(sandbox.text, Is.EqualTo("Sandbox on"));
            Press(sandbox);
            Assert.That(AudioPrefs.SandboxUnlocked, Is.False);
            Assert.That(MenuPrefs.SandboxUnlocked, Is.False);
            Press(sandbox);
            Assert.That(MenuPrefs.SandboxUnlocked, Is.True);

            var motion = full.Q<Button>("home-reduce-motion");
            Press(motion);
            Assert.That(motion.text, Is.EqualTo("Reduce motion on"));
            Press(motion);
            Assert.That(motion.text, Is.EqualTo("Reduce motion"));

            foreach (var name in new[]
            {
                "home-setting-music", "home-setting-sound", "home-setting-voice",
                "home-setting-subtitles", "home-setting-sandbox", "home-reduce-motion", "home-settings-close",
            })
            {
                var key = full.Q<Button>(name);
                Assert.That(key.style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f), name);
                Assert.That(key.style.fontSize.value.value, Is.GreaterThanOrEqualTo(14f), name);
            }
            foreach (var name in new[] { "audio-music", "audio-sfx", "audio-narration" })
            {
                var key = full.Q<Button>(name);
                Assert.That(key.style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f), name);
                Assert.That(key.style.fontSize.value.value, Is.GreaterThanOrEqualTo(14f), name);
            }

            int before = launch;
            Assert.That(MenuBack.Try(full), Is.True);
            Assert.That(sheet.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(launch, Is.EqualTo(before), "closing settings stays on Home");
        }

        void Toggle(string name, string label, Func<bool> muted, VisualElement full)
        {
            var key = full.Q<Button>(name);
            bool start = muted();
            Assert.That(key.text, Is.EqualTo(AudioPrefs.Caption(label, start)));
            Press(key);
            Assert.That(muted(), Is.EqualTo(!start));
            Assert.That(key.text, Is.EqualTo(AudioPrefs.Caption(label, !start)));
            Press(key);
            Assert.That(muted(), Is.EqualTo(start));
            Assert.That(key.text, Is.EqualTo(AudioPrefs.Caption(label, start)));
        }

        void WalkBoard(Campaign campaign, IReadOnlyDictionary<string, PayloadLabel> labels, long minor, int cleared)
        {
            var career = Career.Start(campaign, minor);
            career.Cleared = cleared;
            int home = 0, launches = 0, designs = 0, sandboxes = 0;
            MissionBoardView board = null;
            Assert.DoesNotThrow(() =>
            {
                board = MissionBoardScreen.Build(
                    campaign, career, null, labels,
                    _ => launches++, _ => designs++, () => sandboxes++, () => home++,
                    true, _ => true, 375f, 667f, false);
            });
            Assert.That(CountNodes(board), Is.EqualTo(20), "cleared " + cleared);
            var kicker = board.Q<Label>("mission-kicker");
            Assert.That(kicker.style.fontSize.value.value, Is.GreaterThanOrEqualTo(14f));
            Assert.That(kicker.style.height.value.value, Is.GreaterThanOrEqualTo(16f));
            Assert.That(board.Q<Button>("mission-home").style.height.value.value, Is.GreaterThanOrEqualTo(44f));
            Assert.That(board.Q<Button>("mission-primary").style.height.value.value, Is.GreaterThanOrEqualTo(44f));
            Assert.That(board.Q<Button>("mission-contracts").style.width.value.value, Is.GreaterThanOrEqualTo(44f));
            Assert.That(board.Q<Button>("mission-sandbox").style.width.value.value, Is.GreaterThanOrEqualTo(44f));
            Assert.That(board.Q("mission-art"), Is.Not.Null);

            for (int i = 1; i <= 20; i++)
            {
                string id = "m" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Assert.DoesNotThrow(() => board.SelectMission(id, false), id + " cleared " + cleared);
                var node = board.Q<Button>("mission-node-" + id);
                Assert.That(node, Is.Not.Null, id);
                Assert.That(node.style.width.value.value, Is.GreaterThanOrEqualTo(44f), id);
                Assert.That(board.Q("mission-node-art-" + id), Is.Not.Null, id);
                var access = MissionMapBuilder.AccessFor(i, cleared);
                if (access == MissionAccess.Locked)
                {
                    Assert.That(node.parent.ClassListContains("mission-node--locked"), Is.True, id);
                    Assert.That(board.Q<Button>("mission-primary").text, Is.EqualTo("Locked"), id);
                }
                else if (access == MissionAccess.Open)
                {
                    Assert.That(node.ClassListContains("mission-node--open"), Is.True, id);
                    Assert.That(board.Q<Button>("mission-primary").text, Does.StartWith("Fly "), id);
                }
                else
                {
                    Assert.That(node.ClassListContains("mission-node--cleared"), Is.True, id);
                    Assert.That(board.Q<Button>("mission-primary").text, Does.StartWith("Fly "), id);
                }
                Assert.That(board.Q<Label>("mission-name").text, Is.Not.Empty, id);
                Assert.That(board.Q<Label>("mission-customer").text, Is.Not.Empty, id);
                Assert.That(board.Q<Label>("mission-brief").text, Is.Not.Empty, id);
                Assert.That(board.Q<Label>("mission-name").style.fontSize.value.value, Is.GreaterThanOrEqualTo(14f), id);
            }

            Assert.DoesNotThrow(() => board.SelectContracts());
            Assert.That(board.Q<Button>("mission-primary").text, Is.Not.Empty);
            Assert.DoesNotThrow(() => board.SelectSandbox());
            Assert.That(board.Q<Button>("mission-primary").text, Is.EqualTo("Design first"));

            if (cleared == 0)
            {
                MenuPrefs.SandboxUnlocked = false;
                var locked = MissionBoardScreen.Build(
                    campaign, career, null, labels,
                    _ => { }, _ => { }, () => { }, () => { },
                    true, _ => true, 440f, 956f, false);
                var sandbox = locked.Q<Button>("mission-sandbox");
                Assert.That(sandbox.enabledSelf, Is.False);
                Assert.That(sandbox.tooltip, Does.Contain("Settings"));
                Assert.DoesNotThrow(() => locked.SelectSandbox());
                Assert.That(locked.Q<Button>("mission-primary").text, Is.EqualTo("Design first"));
                MenuPrefs.SandboxUnlocked = true;

                var wide = MissionBoardScreen.Build(
                    campaign, career, null, labels,
                    _ => { }, _ => { }, () => { }, () => { },
                    true, _ => true, 1440f, 900f, true);
                var frame = wide.userData as MissionBoardFrame;
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.Wide, Is.True);
                Inside(frame.Home, 1440f, 900f, "1440 home");
                Inside(frame.Card, 1440f, 900f, "1440 card");
                Inside(frame.Primary, 1440f, 900f, "1440 primary");
                var phone = MissionBoardScreen.Build(
                    campaign, career, null, labels,
                    _ => { }, _ => { }, () => { }, () => { },
                    true, _ => true, 844f, 390f, true);
                var phoneFrame = phone.userData as MissionBoardFrame;
                Inside(phoneFrame.Home, 844f, 390f, "844 home");
                Inside(phoneFrame.Card, 844f, 390f, "844 card");
                Inside(phoneFrame.Sandbox, 844f, 390f, "844 sandbox");
                Assert.That(phoneFrame.Sandbox.W, Is.GreaterThanOrEqualTo(44f));
                Assert.That(phoneFrame.Contracts.W, Is.GreaterThanOrEqualTo(44f));
            }

            Assert.That(MenuBack.Try(board), Is.True, "cleared " + cleared);
            Assert.That(home, Is.EqualTo(1), "cleared " + cleared);
            Assert.That(launches, Is.EqualTo(0));
            Assert.That(designs, Is.EqualTo(0));
            Assert.That(sandboxes, Is.EqualTo(0));
        }

        void WalkPatches(Campaign campaign)
        {
            var lockedCareer = Career.Start(campaign, 100);
            lockedCareer.Cleared = 0;
            int backs = 0;
            VisualElement fresh = null;
            Assert.DoesNotThrow(() => fresh = PatchCollectionScreen.Build(campaign, lockedCareer, null, () => backs++));
            Assert.That(PatchCollectionScreen.ChromeFor(1, 0, null), Is.EqualTo(PatchChrome.Open));
            Assert.That(PatchCollectionScreen.ChromeFor(2, 0, null), Is.EqualTo(PatchChrome.Locked));
            Assert.That(fresh.Q("patch-lock-2"), Is.Not.Null);
            Assert.That(fresh.Q("patch-lock-1"), Is.Null);
            Assert.That(fresh.Q<Label>("patch-name-1").style.fontSize.value.value, Is.GreaterThanOrEqualTo(14f));
            Assert.That(fresh.Q<Button>("patches-back").style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f));

            var medal = new MissionRecord();
            medal.BestMedals.Add("gold");
            var records = new Dictionary<string, MissionRecord> { { "m7", medal } };
            var earned = Career.Start(campaign, 100);
            earned.Cleared = 20;
            Assert.That(PatchCollectionScreen.ChromeFor(7, 20, medal), Is.EqualTo(PatchChrome.Medal));
            Assert.That(PatchCollectionScreen.ChromeFor(7, 20, new MissionRecord()), Is.EqualTo(PatchChrome.Open));
            Assert.That(PatchCollectionScreen.ChromeFor(20, 20, null), Is.EqualTo(PatchChrome.Open));
            VisualElement gold = null;
            Assert.DoesNotThrow(() => gold = PatchCollectionScreen.Build(campaign, earned, records, () => { }));
            Assert.That(gold.Q("patch-kh-7"), Is.Not.Null);
            Assert.That(gold.Q("patch-lock-7"), Is.Null);
            Assert.That(gold.Q("patch-lock-20"), Is.Null);

            Assert.That(MenuBack.Try(fresh), Is.True);
            Assert.That(backs, Is.EqualTo(1));
        }

        void WalkCodex()
        {
            int backs = 0;
            VisualElement codex = null;
            Assert.DoesNotThrow(() => codex = CodexScreen.Build(() => backs++));
            var empty = codex.Q<Label>("codex-empty");
            if (empty != null)
            {
                Assert.That(empty.text, Is.EqualTo(CodexScreen.EmptyLine));
                Assert.That(empty.style.fontSize.value.value, Is.GreaterThanOrEqualTo(14f));
            }
            else
            {
                Assert.That(codex.Q("codex-scroll").childCount, Is.GreaterThan(0));
            }
            Assert.That(codex.Q<Label>("codex-title").style.fontSize.value.value, Is.GreaterThanOrEqualTo(14f));
            Assert.That(codex.Q<Button>("codex-back").style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f));
            Assert.That(MenuBack.Try(codex), Is.True);
            Assert.That(backs, Is.EqualTo(1));
        }

        void WalkBriefingAndReport()
        {
            int backs = 0, homes = 0;
            var ctx = MissionContext.Free();
            VisualElement brief = null;
            Assert.DoesNotThrow(() => brief = ScreenStub.BuildBriefing(ctx, () => { }, () => backs++));
            Assert.That(brief.Q<Button>("briefing-back").style.fontSize.value.value, Is.GreaterThanOrEqualTo(14f));
            Assert.That(MenuBack.Try(brief), Is.True);
            Assert.That(backs, Is.EqualTo(1));

            VisualElement report = null;
            Assert.DoesNotThrow(() => report = ScreenStub.BuildReport(ctx, () => { }, () => { }, () => { }, () => homes++));
            Assert.That(MenuBack.Try(report), Is.True);
            Assert.That(homes, Is.EqualTo(1));

            int mapHome = 0;
            VisualElement map = null;
            Assert.DoesNotThrow(() => map = MissionSelectScreen.Build(null, _ => { }, () => mapHome++));
            Assert.That(MenuBack.Try(map), Is.True);
            Assert.That(mapHome, Is.EqualTo(1));
        }

        void WalkLegacyAndCorruptSaves(Campaign campaign, long minor)
        {
            string legacyDir = Path.Combine(Path.GetTempPath(), "kh-menu-legacy-" + Guid.NewGuid().ToString("N"));
            string corruptDir = Path.Combine(Path.GetTempPath(), "kh-menu-corrupt-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(legacyDir);
            Directory.CreateDirectory(corruptDir);
            try
            {
                var legacy = new NativeStore(legacyDir, () => 1_700_000_000_000L, () => 0.25);
                string bare = "{\"bankMinor\":8800,\"cleared\":4}";
                File.WriteAllText(Path.Combine(legacyDir, CareerStore.CareerKey + ".json"), bare);
                CareerStore migrated = null;
                Assert.DoesNotThrow(() => migrated = CareerStore.Load(legacy, campaign, minor));
                Assert.That(migrated.MigratedFromProgress, Is.True);
                Assert.That(migrated.Career.BankMinor, Is.EqualTo(8800L));
                Assert.That(migrated.Career.Cleared, Is.EqualTo(4));
                Assert.That(File.ReadAllText(Path.Combine(legacyDir, CareerStore.CareerKey + ".json.bak")), Is.EqualTo(bare));
                var rewritten = MiniJson.ParseObject(File.ReadAllText(Path.Combine(legacyDir, CareerStore.CareerKey + ".json")));
                Assert.That(rewritten.Num("schema"), Is.EqualTo(1d));
                Assert.That(rewritten.Str("kind"), Is.EqualTo("career"));

                var corrupt = new NativeStore(corruptDir, () => 1_700_000_000_000L, () => 0.25);
                File.WriteAllText(Path.Combine(corruptDir, CareerStore.CareerKey + ".json"), "{not json");
                CareerStore fresh = null;
                Assert.DoesNotThrow(() => fresh = CareerStore.Load(corrupt, campaign, minor));
                Assert.That(fresh.Career.Cleared, Is.EqualTo(0));
                Assert.That(File.ReadAllText(Path.Combine(corruptDir, CareerStore.CareerKey + ".json.bak")), Is.EqualTo("{not json"));
            }
            finally
            {
                try { Directory.Delete(legacyDir, true); } catch (Exception) { }
                try { Directory.Delete(corruptDir, true); } catch (Exception) { }
            }
        }

        void WalkBuildCost(Campaign campaign, IReadOnlyDictionary<string, PayloadLabel> labels)
        {
            var career = Career.Start(campaign, 100);
            var firstPatches = TimeOf(() => PatchCollectionScreen.Build(campaign, career, null, () => { }));
            var secondPatches = TimeOf(() => PatchCollectionScreen.Build(campaign, career, null, () => { }));
            var home = TimeOf(() =>
            {
                var full = new VisualElement();
                var safe = new VisualElement();
                MainMenuOverlay.Build(full, safe, () => { }, () => { }, () => { }, true);
            });
            var board = TimeOf(() => MissionBoardScreen.Build(
                campaign, career, null, labels, _ => { }, _ => { }, () => { }, () => { }, true, _ => false, 375f, 667f, false));
            var codex = TimeOf(() => CodexScreen.Build(() => { }));

            Assert.That(home.Ms, Is.LessThan(FirstBuildMs), "Home build " + home);
            Assert.That(board.Ms, Is.LessThan(FirstBuildMs), "mission board build " + board);
            Assert.That(codex.Ms, Is.LessThan(FirstBuildMs), "codex build " + codex);
            Assert.That(firstPatches.Ms, Is.LessThan(FirstBuildMs), "patches first build " + firstPatches);
            Assert.That(secondPatches.Ms, Is.LessThan(SecondBuildMs), "patches second build " + secondPatches);
            Assert.That(secondPatches.Alloc, Is.LessThan(firstPatches.Alloc),
                "patch texture cache did not shrink allocations. first " + firstPatches + " second " + secondPatches);
        }

        readonly struct Sample
        {
            public readonly long Ms, Alloc;
            public Sample(long ms, long alloc) { Ms = ms; Alloc = alloc; }
            public override string ToString() => Ms + " ms, " + Alloc + " bytes";
        }

        static Sample TimeOf(Action build)
        {
            GC.Collect();
            long before = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            build();
            watch.Stop();
            long alloc = GC.GetAllocatedBytesForCurrentThread() - before;
            return new Sample(watch.ElapsedMilliseconds, alloc);
        }

        static void Inside(HudBox box, float w, float h, string what)
        {
            Assert.That(box.X, Is.GreaterThanOrEqualTo(-0.6f), what);
            Assert.That(box.Y, Is.GreaterThanOrEqualTo(-0.6f), what);
            Assert.That(box.Right, Is.LessThanOrEqualTo(w + 0.6f), what);
            Assert.That(box.Bottom, Is.LessThanOrEqualTo(h + 0.6f), what);
            Assert.That(box.W, Is.GreaterThan(1f), what);
            Assert.That(box.H, Is.GreaterThan(1f), what);
        }

        static int CountNodes(VisualElement root)
        {
            int n = 0;
            root.Query<Button>().ForEach(b =>
            {
                if (b.name != null && b.name.StartsWith("mission-node-")) n++;
            });
            return n;
        }

        static void Press(Button button)
        {
            Assert.That(button, Is.Not.Null);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            object clickable = typeof(Button).GetProperty("clickable", flags)?.GetValue(button);
            Assert.That(clickable, Is.Not.Null, button != null ? button.name : "button");
            var field = clickable.GetType().GetField("clicked", flags);
            if (field == null)
            {
                var type = clickable.GetType();
                while (type != null && field == null)
                {
                    field = type.GetField("clicked", flags);
                    type = type.BaseType;
                }
            }
            Assert.That(field, Is.Not.Null, "clickable.clicked");
            var act = field.GetValue(clickable) as Action;
            Assert.That(act, Is.Not.Null, button.name);
            act();
        }
    }
}
