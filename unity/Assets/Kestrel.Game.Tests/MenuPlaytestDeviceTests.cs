// Device-like menu walk for the web play-test: empty KESTREL_REPO_ROOT, Resources fonts,
// phone portrait and a landscape window. Unity runs this; dotnet runs the layout math.
using System;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Game.Audio;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class MenuPlaytestDeviceTests
    {
        string _prev;
        string _away;
        bool _music, _sfx, _voice, _subs, _sandbox;
        int _motion;
        Action _patches;

        [SetUp]
        public void EmptyDevice()
        {
            _away = Path.Combine(Path.GetTempPath(), "kh-playtest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_away);
            _prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _away);
            _music = AudioPrefs.MusicMuted;
            _sfx = AudioPrefs.SfxMuted;
            _voice = AudioPrefs.NarrationMuted;
            _subs = AudioPrefs.SubtitlesOn;
            _sandbox = AudioPrefs.SandboxUnlocked;
            _motion = PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0);
            _patches = MissionBoardScreen.OpenPatches;
            AudioPrefs.SetMusicMuted(false);
            AudioPrefs.SetSfxMuted(false);
            AudioPrefs.SetNarrationMuted(false);
            AudioPrefs.SetSubtitlesOn(true);
            AudioPrefs.SetSandboxUnlocked(true);
            PlayerPrefs.SetInt(MainMenuLayout.ReduceMotionPref, 0);
            PlayerPrefs.Save();
            AudioPrefs.Load();
            MainMenuOverlay.OpenCodex = null;
            MainMenuOverlay.OpenPatches = null;
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
            MissionBoardScreen.OpenPatches = _patches;
            MainMenuOverlay.OpenCodex = null;
            MainMenuOverlay.OpenPatches = null;
            WebLayoutMode.ForceReserveExit = null;
            try { if (!string.IsNullOrEmpty(_away) && Directory.Exists(_away)) Directory.Delete(_away, true); }
            catch (Exception) { }
        }

        [Test]
        public void PortraitAndLandscapeMenusKeepStateAndEntryPoints()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            Assert.That(Resources.Load<Font>("KHFonts/Fraunces-Black"), Is.Not.Null);
            Assert.That(Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium"), Is.Not.Null);
            Assert.That(Resources.Load<Font>("KHFonts/SpaceGrotesk-Bold"), Is.Not.Null);

            var full = new VisualElement { name = "full" };
            var safe = new VisualElement { name = "safe" };
            var root = MainMenuOverlay.Build(full, safe, () => { }, () => { }, () => { }, true);
            Assert.That(full.Q("audio-bar"), Is.Null, "the overlay does not paint the wrapping corner mix");
            Assert.That(root.Q<Label>("mm-record").text, Does.Contain("flights cleared").And.Contain("Kc").And.Contain("patch"));
            Assert.That(root.Q<Button>("home-patches"), Is.Not.Null);

            var thrum = root.Q<Button>("mm-hint");
            Assert.That(thrum, Is.Not.Null);
            Press(thrum);
            Assert.That(full.Q("home-thrum-note"), Is.Not.Null);
            Assert.That(full.Q<Label>("home-thrum-note-body").text, Is.EqualTo(MainMenuCopy.ThrumLine));
            Assert.That(MenuBack.Try(full), Is.True);

            Press(root.Q<Button>("home-codex"));
            Assert.That(full.Q<Label>("home-codex-soon-body").text, Is.EqualTo("Coming soon."));
            Assert.That(MenuBack.Try(full), Is.True);

            Press(full.Q<Button>("home-settings"));
            foreach (var name in new[] { "home-setting-music", "home-setting-sound", "home-setting-voice", "home-setting-subtitles", "home-setting-sandbox" })
            {
                var key = full.Q<Button>(name);
                Assert.That(key.style.whiteSpace.value, Is.EqualTo(WhiteSpace.NoWrap), name);
                Assert.That(key.Q<Label>(name + "-state").text, Is.EqualTo("On"), name);
                Press(key);
                Assert.That(key.Q<Label>(name + "-state").text, Is.EqualTo("Off"), name + " off");
            }
            var motion = full.Q<Button>("home-reduce-motion");
            Assert.That(motion.style.whiteSpace.value, Is.EqualTo(WhiteSpace.NoWrap));
            Assert.That(motion.Q<Label>("home-reduce-motion-state").text, Is.EqualTo("Off"));
            Press(motion);
            Assert.That(motion.Q<Label>("home-reduce-motion-state").text, Is.EqualTo("On"));
            Assert.That(full.Q<Button>("home-setting-music").text, Is.EqualTo("Music off"));
            Assert.That(full.Q<Button>("home-setting-sound").text, Is.EqualTo("Sound off"));
            Assert.That(full.Q<Button>("home-setting-voice").text, Is.EqualTo("Voice off"));
            Assert.That(full.Q<Button>("home-setting-subtitles").text, Is.EqualTo("Subtitles off"));
            Assert.That(full.Q<Button>("home-setting-sandbox").text, Is.EqualTo("Sandbox off"));
            Assert.That(motion.text, Is.EqualTo("Reduce motion on"));

            var again = new VisualElement { name = "full-2" };
            var safe2 = new VisualElement { name = "safe-2" };
            MainMenuOverlay.Build(again, safe2, () => { }, () => { }, () => { }, true);
            Assert.That(again.Q<Button>("home-setting-music").text, Is.EqualTo("Music off"), "music persists");
            Assert.That(again.Q<Label>("home-setting-music-state").text, Is.EqualTo("Off"));
            Assert.That(again.Q<Button>("home-setting-subtitles").text, Is.EqualTo("Subtitles off"));
            Assert.That(again.Q<Button>("home-reduce-motion").text, Is.EqualTo("Reduce motion on"));
            Assert.That(MenuBack.Try(full), Is.True);

            string repo = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            var campaign = Campaign.Load(repo);
            string json = File.ReadAllText(Path.Combine(repo, "data", "campaign.json"));
            int opened = 0;
            MissionBoardScreen.OpenPatches = () => opened++;
            foreach (var size in new[] { new[] { 335f, 700f }, new[] { 375f, 667f }, new[] { 440f, 956f }, new[] { 1032f, 1376f } })
            {
                var board = MissionBoardScreen.Build(
                    campaign, Career.Start(campaign, MissionMapText.MinorPerKc(json)), null,
                    MissionMapText.ReadPayloadLabels(json),
                    _ => { }, _ => { }, () => { }, () => { },
                    true, _ => true, size[0], size[1], false);
                Assert.That(board.Q<Label>("mission-node-code-m1").text, Is.EqualTo("KH-1"), size[0].ToString());
                Assert.That(board.Q<Label>("mission-node-code-m20").text, Is.EqualTo("KH-20"));
                Assert.That(board.Q<Label>("mission-act").style.whiteSpace.value, Is.EqualTo(WhiteSpace.Normal));
                board.SelectMission("m2", false);
                Assert.That(board.Q<Button>("mission-primary").text, Is.EqualTo("Clear KH-1 to unlock"));
                Assert.That(board.Q<Label>("mission-brief").text, Is.Not.Empty);
                Press(board.Q<Button>("mission-art"));
                Assert.That(board.Q("mission-payload-note").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(board.Q<Label>("mission-payload-what").text, Is.Not.Empty);
                Assert.That(MenuBack.Try(board), Is.True);
                Assert.That(board.Q("mission-payload-note").style.display.value, Is.EqualTo(DisplayStyle.None));
                Press(board.Q<Button>("mission-patches"));
                board.SelectContracts();
                string orbit = board.Q<Label>("mission-orbit").text;
                string brief = board.Q<Label>("mission-brief").text;
                Assert.That(brief, Does.Contain("Opens after KH-20"));
                Assert.That(orbit, Does.Not.Contain("Opens after KH-20"));
            }
            Assert.That(opened, Is.EqualTo(4));
        }

        static void Press(Button button) => MenuPlaytestPress.Press(button);
    }

    static class MenuPlaytestPress
    {
        public static void Press(Button button)
        {
            Assert.That(button, Is.Not.Null);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            object clickable = typeof(Button).GetProperty("clickable", flags)?.GetValue(button);
            Assert.That(clickable, Is.Not.Null, button != null ? button.name : "button");
            var field = clickable.GetType().GetField("clicked", flags);
            var type = clickable.GetType();
            while (type != null && field == null)
            {
                field = type.GetField("clicked", flags);
                type = type.BaseType;
            }
            var action = field != null ? field.GetValue(clickable) as Action : null;
            Assert.That(action, Is.Not.Null);
            action();
        }
    }
}
