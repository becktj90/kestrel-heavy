using Kestrel.Game.Shell;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class FlightControlsTests
    {
        [Test]
        public void FollowLeavesTheChaseOffsetAlone()
        {
            var chase = ChaseCamera.DesiredOffset(Vector3.up, 70f, 1000f);
            var rig = FlightCameraRig.Following();
            Assert.IsTrue(rig.Follow);
            Assert.AreEqual(chase, FlightCameraRig.Orbit(chase, rig));
        }

        [Test]
        public void DragOrbitsPinchClampsAndATapFollowsAgain()
        {
            var chase = ChaseCamera.DesiredOffset(Vector3.up, 70f, 0f);
            var rig = FlightCameraRig.Begin(FlightCameraRig.Following(), Vector2.zero);
            rig = FlightCameraRig.Drag(rig, new Vector2(4f, 0f));
            Assert.IsTrue(rig.Follow, "inside the slop the camera still follows");
            rig = FlightCameraRig.Drag(rig, new Vector2(90f, -20f));
            Assert.IsFalse(rig.Follow);
            Assert.Greater(Vector3.Distance(FlightCameraRig.Orbit(chase, rig), chase), 0.5f);
            rig = FlightCameraRig.End(rig, new Vector2(90f, -20f));
            Assert.IsFalse(rig.Follow, "a drag lift does not snap back");

            rig = FlightCameraRig.Pinch(rig, 8f);
            Assert.AreEqual(FlightCameraRig.ZoomMax, rig.Zoom);
            rig = FlightCameraRig.Pinch(rig, 0.01f);
            Assert.AreEqual(FlightCameraRig.ZoomMin, rig.Zoom);
            rig = FlightCameraRig.Pinch(rig, float.NaN);
            Assert.AreEqual(FlightCameraRig.ZoomMin, rig.Zoom);

            rig = FlightCameraRig.Begin(rig, new Vector2(10f, 10f));
            rig = FlightCameraRig.End(rig, new Vector2(12f, 11f));
            Assert.IsTrue(rig.Follow, "a tap resumes follow");
            Assert.AreEqual(0f, rig.Yaw);
            Assert.AreEqual(1f, rig.Zoom);
            Assert.AreEqual(chase, FlightCameraRig.Orbit(chase, rig));
        }

        [Test]
        public void PauseFreezesTheSimAndTheMenuHasTheFlightCommands()
        {
            var go = new GameObject("flight-chrome");
            try
            {
                var driver = go.AddComponent<FlightDriver>();
                var hud = new VisualElement { name = "hud" };
                var chrome = go.AddComponent<FlightChrome>();
                chrome.Bind(hud, driver.Hold, () => { }, () => { });
                Assert.AreEqual(44f, FlightChrome.TouchPt);
                Assert.IsNotNull(hud.Q<Button>("pause"));
                Assert.IsNotNull(hud.Q<Button>("follow"));
                Assert.IsNotNull(hud.Q<Button>("resume"));
                Assert.IsNotNull(hud.Q<Button>("restart"));
                Assert.IsNotNull(hud.Q<Button>("exit-home"));
                Assert.IsNotNull(hud.Q<Button>("pause-music"));
                Assert.IsNotNull(hud.Q<Button>("pause-sfx"));
                Assert.IsNotNull(hud.Q<Button>("pause-narration"));
                foreach (var b in hud.Query<Button>().ToList())
                    Assert.IsNotEmpty(b.tooltip, b.name);
                Assert.IsFalse(driver.Held);
                chrome.Show(true);
                driver.Hold(true);
                Assert.IsTrue(chrome.Paused);
                Assert.IsTrue(driver.Held);
                Assert.IsFalse(hud.Q("pause-menu").ClassListContains("hidden"));
                chrome.Show(false);
                driver.Hold(false);
                Assert.IsFalse(driver.Held);
                Assert.IsTrue(chrome.Follow);
                var chase = ChaseCamera.DesiredOffset(Vector3.up, 50f, 0f);
                Assert.AreEqual(chase, chrome.Frame(chase));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ShellListenerCoversHomeAndFlightAndDesignBringsItsOwn()
        {
            Assert.IsTrue(ShellAudio.ShellListenerOn(AppScreen.Home));
            Assert.IsTrue(ShellAudio.ShellListenerOn(AppScreen.Flight));
            Assert.IsFalse(ShellAudio.ShellListenerOn(AppScreen.Design));
        }

        [Test]
        public void HomeAudioBarExposesMusicSoundAndNarration()
        {
            var bar = AudioBar.Build();
            Assert.AreEqual(44f, AudioBar.TouchPt);
            foreach (var name in new[] { "audio-music", "audio-sfx", "audio-narration" })
            {
                var b = bar.Q<Button>(name);
                Assert.IsNotNull(b, name);
                Assert.IsNotEmpty(b.tooltip, name);
                Assert.IsNotEmpty(b.text, name);
            }
        }
    }
}
