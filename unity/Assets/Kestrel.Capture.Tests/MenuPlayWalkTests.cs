// PlayMode walk of the menu shell on a fresh device: empty checkout, Resources fonts,
// settings, missions, codex, patches, and escape. Does not enter a flight.
using System;
using System.Collections;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Game.Audio;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    public class MenuPlayWalkTests
    {
        string _prev;
        string _away;
        string _errors;
        bool _music, _sfx, _voice, _subs, _sandbox;

        [UnitySetUp]
        public IEnumerator EmptyDevice()
        {
            _away = Path.Combine(Path.GetTempPath(), "kh-menu-play-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_away);
            _prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _away);
            _errors = "";
            Application.logMessageReceived += OnLog;
            _music = AudioPrefs.MusicMuted;
            _sfx = AudioPrefs.SfxMuted;
            _voice = AudioPrefs.NarrationMuted;
            _subs = AudioPrefs.SubtitlesOn;
            _sandbox = AudioPrefs.SandboxUnlocked;
            // First launch means no shell yet. A shell left by an earlier test survives the Boot load below (AppShell is
            // DontDestroyOnLoad and the new one destroys itself as a duplicate) but loses its Home view with the old scene,
            // so its SceneFlow already reads Home over an empty root. Clear it and let the destroy land first.
            if (AppShell.Instance != null)
            {
                UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
                yield return null;
            }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreDevice()
        {
            Application.logMessageReceived -= OnLog;
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _prev);
            AudioPrefs.SetMusicMuted(_music);
            AudioPrefs.SetSfxMuted(_sfx);
            AudioPrefs.SetNarrationMuted(_voice);
            AudioPrefs.SetSubtitlesOn(_subs);
            AudioPrefs.SetSandboxUnlocked(_sandbox);
            MenuPrefs.SubtitlesOn = true;
            MenuPrefs.SandboxUnlocked = true;
            if (AppShell.Instance != null)
                UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
            try { if (!string.IsNullOrEmpty(_away) && Directory.Exists(_away)) Directory.Delete(_away, true); }
            catch (Exception) { }
            yield return null;
        }

        void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Exception)
                _errors += condition + "\n";
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator FirstLaunchWalksHomeSettingsMissionsAndBack()
        {
            Assert.That(Resources.Load<Font>("KHFonts/Fraunces-Black"), Is.Not.Null);
            Assert.That(Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium"), Is.Not.Null);

            SceneManager.LoadScene(SceneFlow.BootScene, LoadSceneMode.Single);
            SceneFlow flow = null;
            for (int i = 0; i < 600 && (flow == null || flow.Busy || flow.Current != AppScreen.Home); i++)
            {
                if (flow == null) flow = UnityEngine.Object.FindAnyObjectByType<SceneFlow>();
                yield return null;
            }
            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.Current, Is.EqualTo(AppScreen.Home));
            Assert.That(flow.Busy, Is.False);
            var root = AppShell.Instance.Root;
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Q<Button>("home-codex").enabledSelf, Is.True);

            Click(root.Q<Button>("home-settings"));
            yield return null;
            var sheet = root.Q("screen-settings");
            Assert.That(sheet, Is.Not.Null);
            Assert.That(sheet.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Click(root.Q<Button>("settings-music"));
            Click(root.Q<Button>("settings-sound"));
            Click(root.Q<Button>("settings-voice"));
            Click(root.Q<Button>("settings-subtitles"));
            Click(root.Q<Button>("settings-sandbox"));
            Click(root.Q<Button>("settings-reduce-motion"));
            yield return null;
            Assert.That(AudioPrefs.MusicMuted, Is.EqualTo(!_music));
            Assert.That(AudioPrefs.SfxMuted, Is.EqualTo(!_sfx));
            Assert.That(AudioPrefs.NarrationMuted, Is.EqualTo(!_voice));
            Assert.That(MenuPrefs.SubtitlesOn, Is.EqualTo(!_subs));
            Assert.That(MenuPrefs.SandboxUnlocked, Is.EqualTo(!_sandbox));
            Assert.That(MenuBack.Try(root), Is.True);
            Assert.That(sheet.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(flow.Current, Is.EqualTo(AppScreen.Home));

            Click(root.Q<Button>("home-missions"));
            for (int i = 0; i < 600 && (flow.Busy || flow.Current != AppScreen.Missions); i++)
                yield return null;
            Assert.That(flow.Current, Is.EqualTo(AppScreen.Missions));
            var board = root.Q("mission-board");
            Assert.That(board, Is.Not.Null);
            var node = root.Query<Button>().ToList().Find(b => b.name != null && b.name.StartsWith("mission-node-"));
            if (node != null) Click(node);
            var sandbox = root.Q<Button>("mission-sandbox");
            if (sandbox != null) Click(sandbox);
            yield return null;
            Assert.That(MenuBack.Try(root), Is.True);
            for (int i = 0; i < 600 && (flow.Busy || flow.Current != AppScreen.Home); i++)
                yield return null;
            Assert.That(flow.Current, Is.EqualTo(AppScreen.Home));
            Assert.That(flow.Current, Is.Not.EqualTo(AppScreen.Flight));

            int left = 0;
            var codex = CodexScreen.Build(() => left++);
            AppShell.Instance.Safe.Add(codex);
            yield return null;
            var emptyCodex = codex.Q<Label>("codex-empty");
            if (emptyCodex != null)
                Assert.That(emptyCodex.text, Is.EqualTo(CodexScreen.EmptyLine));
            else
                Assert.That(codex.Q("codex-scroll").childCount, Is.GreaterThan(0));
            Assert.That(MenuBack.Try(root), Is.True);
            Assert.That(left, Is.EqualTo(1));

            int patches = 0;
            var collection = ScreenStub.BuildPatchCollection(() => patches++);
            AppShell.Instance.Safe.Add(collection);
            yield return null;
            Assert.That(collection.Q("patch-kh-1"), Is.Not.Null);
            Assert.That(MenuBack.Try(root), Is.True);
            Assert.That(patches, Is.EqualTo(1));
            Assert.That(flow.Current, Is.Not.EqualTo(AppScreen.Flight));
            Assert.That(_errors, Is.EqualTo(""), _errors);
        }

        static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null);
            using (var ev = NavigationSubmitEvent.GetPooled())
            {
                ev.target = button;
                button.SendEvent(ev);
            }
        }
    }
}
