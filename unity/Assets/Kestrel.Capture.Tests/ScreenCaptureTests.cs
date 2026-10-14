// PlayMode capture tests (CI screenshots). Each loads a real build scene, lets the runtime builders
// run for 60 frames, then captures normal URP frames plus UI Toolkit panels into a 1179x2556 RT and writes a PNG.
// Output: $KH_CAP_OUT or <project>/CaptureOut. Fails if the image is a single flat colour.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    public class ScreenCaptureTests
    {
        const int W = 1179, H = 2556, Frames = 60;

#if UNITY_EDITOR
        uint priorWidth, priorHeight;

        [UnitySetUp]
        public IEnumerator UsePortraitViewport()
        {
            UnityEditor.PlayModeWindow.GetRenderingResolution(out priorWidth, out priorHeight);
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(W, H, "Kestrel portrait capture");
            for (int i = 0; i < Frames && (Screen.width != W || Screen.height != H); i++) yield return null;
            Assert.AreEqual(W, Screen.width, "Capture UI and camera must use the portrait target width.");
            Assert.AreEqual(H, Screen.height, "Capture UI and camera must use the portrait target height.");
        }

        [UnityTearDown]
        public IEnumerator RestoreViewport()
        {
            if (priorWidth > 0 && priorHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(priorWidth, priorHeight, "Previous capture viewport");
            yield return null;
        }
#endif

        static string OutDir()
        {
            var d = Environment.GetEnvironmentVariable("KH_CAP_OUT");
            if (string.IsNullOrEmpty(d)) d = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
            Directory.CreateDirectory(d);
            return d;
        }

        [UnityTest] public IEnumerator Home() => Shot("Boot", "Home", null);
        [UnityTest] public IEnumerator Designer() => Shot("Design", "Designer", null);
        [UnityTest] public IEnumerator PadCloseup() => Shot("Design", "Pad", new[] { new Vector3(40, 25, -60), new Vector3(0, 30, 0) });
        [UnityTest] public IEnumerator TankCutaway() => Shot("Design", "TankCutaway", null, true);
        // Flight.unity is still a ScreenStub with no camera (flight lives in Design/SliceApp); add a shot when it gets real content.

        static IEnumerator Shot(string scene, string name, Vector3[] cam, bool cutaway = false)
        {
            CaptureCamera.Clear();
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            if (cam != null) CaptureCamera.Override(cam[0], cam[1]);
            for (int i = 0; i < Frames; i++) yield return null;

            if (scene == "Boot")
            {
                var music = Kestrel.Game.Audio.MusicPlayer.Instance;
                Assert.IsNotNull(music, "Shipping Boot must start the original title score.");
                Assert.IsNotNull(music.Clip);
                var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
                    .Count(listener => listener.isActiveAndEnabled);
                Assert.AreEqual(1, listeners, "Home needs exactly one active listener.");
            }
            if (scene == "Design")
            {
                var flight = UnityEngine.Object.FindObjectsByType<Kestrel.Slice.FlightDriver>(FindObjectsSortMode.None).Single();
                var audio = flight.GetComponent<Kestrel.Game.Audio.AudioDirector>();
                Assert.IsNotNull(audio, "Playable scene must attach its audio director.");
                Assert.AreSame(audio, flight.Sink, "Flight events must reach the audio director.");
                Assert.IsNotNull(flight.GetComponent<Kestrel.Game.Audio.NarrationPlayer>());
                var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
                    .Where(listener => listener.isActiveAndEnabled).ToArray();
                Assert.AreEqual(1, listeners.Length, "Playable scene needs exactly one active audio listener.");
                Assert.IsNotNull(listeners[0].GetComponent<Camera>());
                var priorMute = audio.Muted;
                try
                {
                    audio.SetMuted(false);
                    audio.OnEvent("ui");
                    Assert.IsTrue(audio.GetComponentsInChildren<AudioSource>().Any(source => source.clip != null && source.volume > 0f),
                        "A UI cue must load a sound clip into an audible source.");
                }
                finally { audio.SetMuted(priorMute); }
            }

            if (cutaway)
            {
                var app = UnityEngine.Object.FindAnyObjectByType<Kestrel.Slice.SliceApp>();
                app.SetTankCutaway(true);
                yield return null;
                var shells = UnityEngine.Object.FindObjectsByType<Kestrel.Vehicle.TankShellView>(FindObjectsSortMode.None);
                Assert.Greater(shells.Length, 0);
                Assert.IsTrue(shells.All(shell => shell.Cutaway));
                Assert.IsTrue(shells.All(shell => !shell.transform.parent.GetComponent<MeshRenderer>().enabled),
                    "Solid tank geometry must not fill the cutaway sector.");
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var cams = Camera.allCameras.Where(c => c.enabled && c.targetTexture == null).OrderBy(c => c.depth).ToArray();
            // Keep the target attached during normal rendering. Camera.Render() after LateUpdate
            // does not preserve the procedural instancing submissions used by pad and vehicle detail.
            foreach (var c in cams) c.targetTexture = rt;

            // UI Toolkit and the cameras draw into the same target during normal frames.
            var panels = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                .Select(d => d.panelSettings).Where(p => p != null).Distinct().ToArray();
            var oldTargets = panels.Select(p => p.targetTexture).ToArray();
            var oldClear = panels.Select(p => p.clearColor).ToArray();
            foreach (var p in panels) { p.targetTexture = rt; p.clearColor = false; }
            // (WaitForEndOfFrame never fires in batchmode; panels render into targetTexture during the frame.)
            for (int i = 0; i < 3; i++) yield return null;
            foreach (var c in cams) c.targetTexture = null;
            for (int i = 0; i < panels.Length; i++) { panels[i].targetTexture = oldTargets[i]; panels[i].clearColor = oldClear[i]; }

            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = prev; rt.Release();
            CaptureCamera.Clear();

            var path = Path.Combine(OutDir(), name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            var px = tex.GetPixels32();
            int distinct = px.Where((_, i) => i % 97 == 0).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count();
            Debug.Log($"[Capture] {name}: {path} cams={cams.Length} panels={panels.Length} distinctColours={distinct}");
            if (cutaway)
            {
                UnityEngine.Object.FindAnyObjectByType<Kestrel.Slice.SliceApp>().SetTankCutaway(false);
                yield return null;
                Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Kestrel.Vehicle.TankShellView>(FindObjectsSortMode.None)
                    .All(shell => !shell.Cutaway && shell.transform.parent.GetComponent<MeshRenderer>().enabled));
            }
            Assert.Greater(distinct, 8, $"{name} capture is flat colour ({distinct} colours)");
        }
    }
}
