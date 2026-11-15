// Settings > Commentary (Full / Calls only / Off, kh.audio.commentary) and Time warp (Auto / Off, kh.flight.timewarp).
// EditMode only, built on plain elements like CornerCharacterSettingTests.
using System;
using System.Reflection;
using Kestrel.Core.Features;
using Kestrel.Core.Flight;
using Kestrel.Core.Flight.Narration;
using Kestrel.Core.Ui;
using Kestrel.Game.Audio;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class SettingsCommentaryTimeWarpTests
    {
        static readonly string[] Keys =
        {
            Commentary.Pref, ExperienceSettings.LegacyNarrationMutePref, TimeWarpSetting.Pref,
            MainMenuLayout.ReduceFlashingPref, MainMenuLayout.HapticsPref,
        };
        bool[] had;
        int[] prev;

        [SetUp]
        public void CleanDevice()
        {
            had = new bool[Keys.Length];
            prev = new int[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                had[i] = PlayerPrefs.HasKey(Keys[i]);
                prev[i] = PlayerPrefs.GetInt(Keys[i], 0);
                PlayerPrefs.DeleteKey(Keys[i]);
            }
        }

        [TearDown]
        public void RestoreDevice()
        {
            for (int i = 0; i < Keys.Length; i++)
                if (had[i]) PlayerPrefs.SetInt(Keys[i], prev[i]); else PlayerPrefs.DeleteKey(Keys[i]);
            PlayerPrefs.Save();
        }

        VisualElement OpenSettings()
        {
            var full = new VisualElement { name = "full" };
            var safe = new VisualElement { name = "safe" };
            full.Add(AudioBar.Build());
            Assert.DoesNotThrow(() => MainMenuOverlay.Build(full, safe, () => { }, () => { }, () => { }, true));
            Press(full.Q<Button>("home-settings"));
            return full;
        }

        [Test]
        public void FreshSave_RowsReadFullAndAuto()
        {
            var full = OpenSettings();
            var c = full.Q<Button>(UiIds.SettingsCommentary);
            var w = full.Q<Button>(UiIds.SettingsTimeWarp);
            Assert.That(c, Is.Not.Null);
            Assert.That(w, Is.Not.Null);
            Assert.That(c.text, Is.EqualTo("Commentary"));
            Assert.That(w.text, Is.EqualTo("Time warp"));
            Assert.That(c.Q<Label>(c.name + "-state").text, Is.EqualTo("Full"));
            Assert.That(w.Q<Label>(w.name + "-state").text, Is.EqualTo("Auto"));
            Assert.That(full.Q<Label>("settings-time-warp-note").text, Is.EqualTo("Speeds up quiet parts of the flight."));
            Assert.That(full.Q<Label>("settings-commentary-note").text, Is.EqualTo(ExperienceSettings.CommentaryNote));
            Assert.That(c.style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f), "touch target");
            Assert.That(w.style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f), "touch target");
            StringAssert.StartsWith("Commentary: Full.", c.tooltip);
            StringAssert.StartsWith("Time warp: Auto.", w.tooltip);
            Assert.That(PlayerPrefs.HasKey(TimeWarpSetting.Pref), Is.False, "opening Settings does not write");
        }

        [Test]
        public void CommentaryRow_CyclesFullCallsOff_AndRoundTrips()
        {
            var full = OpenSettings();
            var c = full.Q<Button>(UiIds.SettingsCommentary);
            var state = c.Q<Label>(c.name + "-state");
            var voice = full.Q<Button>(UiIds.SettingsVoice);
            Press(c);
            Assert.That(AudioPrefs.CommentaryMode, Is.EqualTo(CommentaryMode.CallsOnly));
            Assert.That(state.text, Is.EqualTo("Calls only"));
            StringAssert.StartsWith("Commentary: Calls only.", c.tooltip);
            Press(c);
            Assert.That(AudioPrefs.CommentaryMode, Is.EqualTo(CommentaryMode.Off));
            Assert.That(state.text, Is.EqualTo("Off"));
            Assert.That(voice.Q<Label>(voice.name + "-state").text, Is.EqualTo("Off"), "Voice repaints with it");
            Press(c);
            Assert.That(AudioPrefs.CommentaryMode, Is.EqualTo(CommentaryMode.Full));
            Assert.That(state.text, Is.EqualTo("Full"));
            Assert.That(c.text, Is.EqualTo("Commentary"), "the label does not change with the state");
            Assert.That(OpenSettings().Q<Button>(UiIds.SettingsCommentary).Q<Label>(UiIds.SettingsCommentary + "-state").text, Is.EqualTo("Full"), "a fresh sheet reads it back");
        }

        [Test]
        public void TimeWarpRow_FlipsAutoOff_AndRoundTrips()
        {
            var full = OpenSettings();
            var w = full.Q<Button>(UiIds.SettingsTimeWarp);
            var state = w.Q<Label>(w.name + "-state");
            Press(w);
            Assert.That(PlayerPrefs.GetInt(TimeWarpSetting.Pref, 0), Is.EqualTo((int)TimeWarpMode.Off));
            Assert.That(state.text, Is.EqualTo("Off"));
            StringAssert.StartsWith("Time warp: Off.", w.tooltip);
            Assert.That(OpenSettings().Q<Button>(UiIds.SettingsTimeWarp).Q<Label>(UiIds.SettingsTimeWarp + "-state").text, Is.EqualTo("Off"), "a fresh sheet reads it back");
            Press(w);
            Assert.That(TimeWarpSetting.Resolve(PlayerPrefs.HasKey(TimeWarpSetting.Pref), PlayerPrefs.GetInt(TimeWarpSetting.Pref, 0)), Is.EqualTo(TimeWarpMode.Auto));
            Assert.That(state.text, Is.EqualTo("Auto"));
        }

        [Test]
        public void SettingsList_HasBothRows_CloseLast()
        {
            CollectionAssert.Contains(UiIds.SettingsControls, UiIds.SettingsCommentary);
            CollectionAssert.Contains(UiIds.SettingsControls, UiIds.SettingsTimeWarp);
            CollectionAssert.Contains(UiIds.SettingsControls, UiIds.SettingsReduceFlashing);
            CollectionAssert.Contains(UiIds.SettingsControls, UiIds.SettingsHaptics);
            Assert.That(UiIds.SettingsControls[UiIds.SettingsControls.Length - 1], Is.EqualTo(UiIds.SettingsClose));
        }

        [Test]
        public void ReduceFlashingAndHapticsRows_TogglePrefs()
        {
            PlayerPrefs.DeleteKey(MainMenuLayout.ReduceFlashingPref);
            PlayerPrefs.DeleteKey(MainMenuLayout.HapticsPref);
            var full = OpenSettings();
            var flash = full.Q<Button>(UiIds.SettingsReduceFlashing);
            var hap = full.Q<Button>(UiIds.SettingsHaptics);
            Assert.That(flash, Is.Not.Null);
            Assert.That(hap, Is.Not.Null);
            Assert.That(hap.Q<Label>(hap.name + "-state").text, Is.EqualTo("On"), "haptics default on");
            Press(flash);
            Assert.That(PlayerPrefs.GetInt(MainMenuLayout.ReduceFlashingPref, 0), Is.EqualTo(1));
            Assert.That(flash.Q<Label>(flash.name + "-state").text, Is.EqualTo("On"));
            Press(hap);
            Assert.That(PlayerPrefs.GetInt(MainMenuLayout.HapticsPref, 1), Is.EqualTo(0));
            Assert.That(hap.Q<Label>(hap.name + "-state").text, Is.EqualTo("Off"));
        }

        static void Press(Button button)
        {
            Assert.That(button, Is.Not.Null);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            object clickable = typeof(Button).GetProperty("clickable", flags)?.GetValue(button);
            Assert.That(clickable, Is.Not.Null, button.name);
            FieldInfo field = null;
            for (var type = clickable.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField("clicked", flags);
            Assert.That(field, Is.Not.Null, "clickable.clicked");
            var act = field.GetValue(clickable) as Action;
            Assert.That(act, Is.Not.Null, button.name);
            act();
        }
    }
}
