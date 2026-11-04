// Settings > Show corner character: Thrum's corner portrait is off unless the player turns it on.
// EditMode only. The Settings row is built on plain elements; the driver runs on a real UIDocument.
using System;
using System.Reflection;
using Kestrel.Core.Features;
using Kestrel.Core.Ui;
using Kestrel.Game.Guide;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class CornerCharacterSettingTests
    {
        const string Key = ExperienceSettings.CornerCharacterPref;
        bool hadKey;
        int prevKey;
        int prevMotion;
        GameObject go;

        [SetUp]
        public void CleanDevice()
        {
            hadKey = PlayerPrefs.HasKey(Key);
            prevKey = PlayerPrefs.GetInt(Key, 0);
            prevMotion = PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0);
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.SetInt(MainMenuLayout.ReduceMotionPref, 0);
        }

        [TearDown]
        public void RestoreDevice()
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            go = null;
            GuideSession.Bind(null);
            if (hadKey) PlayerPrefs.SetInt(Key, prevKey); else PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.SetInt(MainMenuLayout.ReduceMotionPref, prevMotion);
            PlayerPrefs.Save();
        }

        [Test]
        public void NoStoredValue_IsOff()
        {
            Assert.That(PlayerPrefs.HasKey(Key), Is.False);
            Assert.That(GuideBoxDriver.SpriteOn(), Is.False);
            Assert.That(PlayerPrefs.HasKey(Key), Is.False, "reading does not write");
        }

        [Test]
        public void ExistingInstall_WithOtherSettingsButNoSpriteKey_IsOff()
        {
            PlayerPrefs.SetInt("kh.settings.tier", PlayerPrefs.GetInt("kh.settings.tier", 0));
            PlayerPrefs.SetInt("kh.settings.subtitles", 1);
            Assert.That(GuideBoxDriver.SpriteOn(), Is.False);
        }

        [Test]
        public void Toggle_Persists_AndReduceMotionIsUntouched()
        {
            ExperienceSettings.SetCornerCharacter(true, PlayerPrefs.SetInt);
            PlayerPrefs.Save();
            Assert.That(GuideBoxDriver.SpriteOn(), Is.True, "read back after a save");
            Assert.That(PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0), Is.EqualTo(0));
            ExperienceSettings.SetCornerCharacter(false, PlayerPrefs.SetInt);
            PlayerPrefs.Save();
            Assert.That(GuideBoxDriver.SpriteOn(), Is.False);
        }

        [Test]
        public void SettingsRow_FlipsThePref_AndRepaintsItsState()
        {
            var full = new VisualElement { name = "full" };
            var safe = new VisualElement { name = "safe" };
            full.Add(AudioBar.Build());
            Assert.DoesNotThrow(() => MainMenuOverlay.Build(full, safe, () => { }, () => { }, () => { }, true));
            Press(full.Q<Button>("home-settings"));

            var row = full.Q<Button>(UiIds.SettingsCornerCharacter);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.text, Is.EqualTo("Show corner character"));
            Assert.That(full.Q<Label>("settings-corner-character-note").text, Is.EqualTo("A little helper in the corner. Off by default."));
            Assert.That(row.style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f), "touch target");
            Assert.That(row.Q<Label>(row.name + "-state").text, Is.EqualTo("Off"));

            Press(row);
            Assert.That(GuideBoxDriver.SpriteOn(), Is.True);
            Assert.That(row.text, Is.EqualTo("Show corner character"), "the label does not change with the state");
            Assert.That(row.Q<Label>(row.name + "-state").text, Is.EqualTo("On"));
            Assert.That(PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0), Is.EqualTo(0), "Reduce motion is separate");

            Press(row);
            Assert.That(GuideBoxDriver.SpriteOn(), Is.False);
            Assert.That(row.Q<Label>(row.name + "-state").text, Is.EqualTo("Off"));
        }

        [Test]
        public void SettingsList_HasTheRow_AndEveryIdIsNamed()
        {
            CollectionAssert.Contains(UiIds.SettingsControls, UiIds.SettingsCornerCharacter);
            Assert.That(UiIds.SettingsControls[UiIds.SettingsControls.Length - 1], Is.EqualTo(UiIds.SettingsClose), "Close stays last");
        }

        [Test]
        public void Off_BuildsNoSprite_AndOnOffOnTakesEffectAtOnce_WithoutErrorsOrAGap()
        {
            var uiGo = new GameObject("kh-corner-character-test");
            go = uiGo;
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = Resources.Load<PanelSettings>("UI/KH_PanelSettings");
            Assert.That(doc.panelSettings, Is.Not.Null);
            var root = doc.rootVisualElement;
            Assert.That(root, Is.Not.Null);
            var content = new VisualElement { name = "content" };
            root.Add(content);
            int before = root.childCount;

            GuideSession.Bind(null);   // so the first build below loads the guide (and logs) every run
            var driver = uiGo.AddComponent<GuideBoxDriver>();
            driver.Document = doc;
            driver.Place = GuidePlace.Menu;

            // Off (default): the driver can run its whole sync and nothing is built.
            Assert.DoesNotThrow(() => { driver.SyncSprite(); driver.SyncSprite(); });
            Assert.That(driver.HasSprite, Is.False);
            Assert.That(root.Q("guide-overlay"), Is.Null);
            Assert.That(root.Q("guide-pop"), Is.Null);
            Assert.That(root.childCount, Is.EqualTo(before), "nothing added where the portrait sat");
            Assert.DoesNotThrow(() => driver.Silence(), "Silence is safe with no portrait");
            Assert.That(uiGo.GetComponents<AudioSource>().Length, Is.EqualTo(0), "off: no blip source");

            // On: the portrait appears at once. In EditMode OnEnable does not run, so this first sync builds the stage
            // and the guide loader logs once; the stage is kept across off and on, so it cannot log again.
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[Kestrel\] guide clips: \d+ found"));
            ExperienceSettings.SetCornerCharacter(true, PlayerPrefs.SetInt);
            Assert.DoesNotThrow(() => driver.SyncSprite());
            Assert.That(driver.HasSprite, Is.True);
            Assert.That(root.Q("guide-overlay"), Is.Not.Null);
            Assert.That(root.Q("guide-pop"), Is.Not.Null);
            Assert.That(root.Q("guide-overlay").pickingMode, Is.EqualTo(PickingMode.Ignore), "the overlay never takes touches or space");

            // Off again: gone at once, the tree is back to what it was.
            ExperienceSettings.SetCornerCharacter(false, PlayerPrefs.SetInt);
            Assert.DoesNotThrow(() => driver.SyncSprite());
            Assert.That(driver.HasSprite, Is.False);
            Assert.That(root.Q("guide-overlay"), Is.Null);
            Assert.That(root.Q("guide-pop"), Is.Null);
            Assert.That(root.childCount, Is.EqualTo(before));
            Assert.That(uiGo.GetComponents<AudioSource>().Length, Is.EqualTo(0), "off again: no blip source");

            // And on once more after being off: one overlay, not two.
            ExperienceSettings.SetCornerCharacter(true, PlayerPrefs.SetInt);
            driver.SyncSprite();
            Assert.That(root.Query("guide-overlay").ToList().Count, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Off_DropsQueuedLines_SoTheyDoNotPileUpIntoABacklog()
        {
            var uiGo = new GameObject("kh-corner-character-backlog-test");
            go = uiGo;
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = Resources.Load<PanelSettings>("UI/KH_PanelSettings");
            Assert.That(doc.panelSettings, Is.Not.Null);
            Assert.That(doc.rootVisualElement, Is.Not.Null);

            GuideSession.Bind(null);
            var driver = uiGo.AddComponent<GuideBoxDriver>();
            driver.Document = doc;
            driver.Place = GuidePlace.Menu;

            // Build the stage with the character on (the guide loader logs once), then switch it off.
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[Kestrel\] guide clips: \d+ found"));
            ExperienceSettings.SetCornerCharacter(true, PlayerPrefs.SetInt);
            driver.SyncSprite();
            Assert.That(driver.HasSprite, Is.True);
            var live = driver.Stage != null ? driver.Stage.Engine as LiveGuideEngine : null;
            Assume.That(live, Is.Not.Null, "needs the real guide script");
            ExperienceSettings.SetCornerCharacter(false, PlayerPrefs.SetInt);
            driver.SyncSprite();
            Assert.That(driver.HasSprite, Is.False);

            // Lines offered while off must not wait for the next time it is turned on.
            bool offered = live.Offer("guide.first", "1", null, "home", false);
            Assume.That(offered, "the first-run line is not allowed on this save");
            Assert.That(live.Pending.Count, Is.GreaterThan(0), "the line is queued before the sync");
            driver.SyncSprite();
            Assert.That(live.Pending.Count, Is.EqualTo(0), "off: the queue is emptied");
            LogAssert.NoUnexpectedReceived();
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
