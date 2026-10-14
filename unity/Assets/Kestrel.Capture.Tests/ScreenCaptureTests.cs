// PlayMode capture tests (CI screenshots). Each loads a real build scene, lets the runtime builders
// run for 60 frames, then renders every camera plus UI Toolkit panels into a 1179x2556 RT and writes a PNG.
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
        // Flight.unity is still a ScreenStub with no camera (flight lives in Design/SliceApp); add a shot when it gets real content.

        static IEnumerator Shot(string scene, string name, Vector3[] cam)
        {
            CaptureCamera.Clear();
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            if (cam != null) CaptureCamera.Override(cam[0], cam[1]);
            for (int i = 0; i < Frames; i++) yield return null;

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

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var cams = Camera.allCameras.Where(c => c.enabled && c.targetTexture == null).OrderBy(c => c.depth).ToArray();
            foreach (var c in cams) { c.targetTexture = rt; c.Render(); c.targetTexture = null; }

            // UI Toolkit: draw panels over the 3D frame for one frame.
            var panels = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                .Select(d => d.panelSettings).Where(p => p != null).Distinct().ToArray();
            var oldTargets = panels.Select(p => p.targetTexture).ToArray();
            var oldClear = panels.Select(p => p.clearColor).ToArray();
            foreach (var p in panels) { p.targetTexture = rt; p.clearColor = false; }
            // (WaitForEndOfFrame never fires in batchmode; panels render into targetTexture during the frame.)
            for (int i = 0; i < 3; i++) yield return null;
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
            Assert.Greater(distinct, 8, $"{name} capture is flat colour ({distinct} colours)");
        }
    }
}
