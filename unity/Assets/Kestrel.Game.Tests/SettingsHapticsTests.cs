// DECISIONS #100: Settings > Haptics (On / Off). One switch, kh.hangar.haptics, for the Hangar taps and the liftoff
// rumble, kick and Max-Q buzz. EditMode only, on plain elements like SettingsCommentaryTimeWarpTests.
using System.Reflection;
using Kestrel.Core.Feel;
using Kestrel.Core.Ui;
using Kestrel.Game.Audio;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class SettingsHapticsTests
    {
        bool had;
        int prev;

        [SetUp]
        public void CleanDevice()
        {
            had = PlayerPrefs.HasKey(LiftoffHaptics.Pref);
            prev = PlayerPrefs.GetInt(LiftoffHaptics.Pref, 1);
            PlayerPrefs.DeleteKey(LiftoffHaptics.Pref);
        }

        [TearDown]
        public void RestoreDevice()
        {
            if (had) PlayerPrefs.SetInt(LiftoffHaptics.Pref, prev); else PlayerPrefs.DeleteKey(LiftoffHaptics.Pref);
            PlayerPrefs.Save();
        }

        static VisualElement OpenSettings()
        {
            var full = new VisualElement { name = "full" };
            var safe = new VisualElement { name = "safe" };
            full.Add(AudioBar.Build());
            Assert.DoesNotThrow(() => MainMenuOverlay.Build(full, safe, () => { }, () => { }, () => { }, true));
            Press(full.Q<Button>("home-settings"));
            return full;
        }

        [Test]
        public void FreshSave_ReadsOn_AndATapTurnsBothHangarAndFlightHapticsOff()
        {
            var full = OpenSettings();
            var row = full.Q<Button>(UiIds.SettingsHaptics);
            Assert.That(row, Is.Not.Null);
            var state = row.Q<Label>(row.name + "-state");
            Assert.That(row.text, Is.EqualTo("Haptics"));
            Assert.That(state.text, Is.EqualTo("On"), "no stored value is on, as the Hangar switch always read it");
            Assert.That(row.style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f), "touch target");
            Press(row);
            Assert.That(PlayerPrefs.GetInt(LiftoffHaptics.Pref, 1), Is.EqualTo(0));
            Assert.That(state.text, Is.EqualTo("Off"));
            Assert.IsFalse(Kestrel.Slice.HangarNative.HapticsOn, "the Hangar reads the same switch");
            Assert.IsFalse(Kestrel.Slice.FlightHaptics.Enabled, "and so does the flight");
            Assert.That(OpenSettings().Q<Button>(UiIds.SettingsHaptics).Q<Label>(UiIds.SettingsHaptics + "-state").text, Is.EqualTo("Off"), "a fresh sheet reads it back");
            Press(row);
            Assert.IsTrue(Kestrel.Slice.FlightHaptics.Enabled);
        }

        [Test]
        public void SettingsList_HasTheRow_CloseLast()
        {
            Assert.IsTrue(System.Array.IndexOf(UiIds.SettingsControls, UiIds.SettingsHaptics) >= 0);
            Assert.That(UiIds.SettingsControls[UiIds.SettingsControls.Length - 1], Is.EqualTo(UiIds.SettingsClose));
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
            var act = field.GetValue(clickable) as System.Action;
            Assert.That(act, Is.Not.Null, button.name);
            act();
        }
    }
}
