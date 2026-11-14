// Settings → Restart career: two-tap confirm, then the host wipe callback. EditMode only.
using System.Reflection;
using Kestrel.Core.Ui;
using Kestrel.Game.Audio;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class SettingsRestartCareerTests
    {
        [TearDown]
        public void ClearHook() => MainMenuOverlay.RestartCareer = null;

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
        public void Row_AsksTwice_ThenCallsHost_AndCloses()
        {
            int calls = 0;
            MainMenuOverlay.RestartCareer = () => calls++;
            var full = OpenSettings();
            var row = full.Q<Button>(UiIds.SettingsRestartCareer);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.text, Is.EqualTo(MainMenuOverlay.RestartCareerLabel));
            Assert.That(full.Q<Label>("settings-restart-career-note").text, Is.EqualTo(MainMenuOverlay.RestartCareerNote));
            Assert.That(row.style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f), "touch target");

            Press(row);
            Assert.That(calls, Is.EqualTo(0), "first tap only arms confirm");
            Assert.That(row.text, Is.EqualTo(MainMenuOverlay.RestartCareerConfirm));

            Press(row);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(full.Q(UiIds.ScreenSettings).style.display.value, Is.EqualTo(DisplayStyle.None), "sheet closes after confirm");
        }

        [Test]
        public void ReopeningSettings_ClearsArmedConfirm()
        {
            MainMenuOverlay.RestartCareer = () => { };
            var full = OpenSettings();
            var row = full.Q<Button>(UiIds.SettingsRestartCareer);
            Press(row);
            Assert.That(row.text, Is.EqualTo(MainMenuOverlay.RestartCareerConfirm));
            Press(full.Q<Button>("home-settings"));
            Assert.That(row.text, Is.EqualTo(MainMenuOverlay.RestartCareerLabel), "gear reopen disarms");
        }

        [Test]
        public void SettingsList_IncludesRestart_CloseLast()
        {
            CollectionAssert.Contains(UiIds.SettingsControls, UiIds.SettingsRestartCareer);
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
