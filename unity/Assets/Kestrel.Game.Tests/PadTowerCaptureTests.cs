// The launch tower beside the stack, drawn for real from the design camera's side of the pad (owner feedback on an
// iPhone recording of KH-1 First Hop: "The tower needs more detail like an actual tower on a launch pad"; the rail
// pad drew one flat grey slab). Builds the pad as SliceApp.BuildRocket does (PadTower.Apply with the TankShell plates
// of a two-stage liquid stack), a stand-in vehicle, a sun, and renders a 390x844 portrait frame per stack:
// First Hop and Tiny Orbit on Rail pads (the rail now rides a compact service tower), a 48 m Tower pad and a 90 m
// strongback pad. Asserts the pad has its tower, drew inside the Low tier budget, and the frame holds the vehicle
// and the tower top. PNGs: pad-tower-<stack>.png via UiCapture -> CaptureOut/ui, CI artifact "ui-captures".
using System.Collections.Generic;
using System.Reflection;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using Kestrel.Pad;
using Kestrel.Slice;
using Kestrel.Vehicle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Tests
{
    public class PadTowerCaptureTests
    {
        const int W = 390, H = 844;
        readonly List<Object> made = new List<Object>();

        [TearDown]
        public void Clean()
        {
            for (int i = made.Count - 1; i >= 0; i--)
            {
                var o = made[i];
                if (o is RenderTexture rt) rt.Release();
                if (o != null) Object.DestroyImmediate(o);
            }
            made.Clear();
        }

        [TestCase("first-hop", 4.5f, 0.9f, PadArchetype.Rail)]
        [TestCase("tiny-orbit", 22f, 0.6f, PadArchetype.Rail)]
        [TestCase("tower-48", 48f, 1.6f, PadArchetype.Tower)]
        [TestCase("strongback-90", 90f, 3.6f, PadArchetype.TowerStrongback)]
        public void TowerReadsAsALatticeServiceStructure(string name, float h, float r, PadArchetype type)
        {
            var root = new GameObject("kh-pad-tower-" + name) { hideFlags = HideFlags.HideAndDontSave };
            made.Add(root);
            var vehicle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(vehicle.GetComponent<Collider>());
            vehicle.transform.SetParent(root.transform, false);
            vehicle.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            vehicle.transform.localScale = new Vector3(2f * r, h * 0.5f, 2f * r);
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(deck.GetComponent<Collider>());
            deck.transform.SetParent(root.transform, false);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root.transform, false);
            sun.type = LightType.Directional; sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(38f, 150f, 0f);
            var towerRoot = new GameObject("PadTower").transform;
            towerRoot.SetParent(root.transform, false);

            var plates = new List<AttachPoint>();
            float lower = h * 0.55f, upper = h * 0.25f;
            plates.AddRange(TankShellUmbilicals.ForStage(TankShellSpec.Default(r * 1.005f, lower, 11UL), 0, 0, new Vec3(0, 1f, 0), true, true, true, ReleaseStyle.TailServiceMastDrop));
            plates.AddRange(TankShellUmbilicals.ForStage(TankShellSpec.Default(r * 1.005f, upper, 29UL), 1, 0, new Vec3(0, lower + 3f, 0), true, true, true, ReleaseStyle.SwingArmRetract));
            var pad = PadTower.Apply(towerRoot, h, r, type, PadTower.DefaultSeed, plates);
            Assert.That(pad, Is.Not.Null, name + ": PadTower.Apply built no pad");
            var tp = pad.Layout.Tower;
            Assert.That(tp, Is.Not.Null, name + ": no service tower beside the stack");
            float deckR = PadGround.DeckRadius(pad.Layout);
            deck.transform.localScale = new Vector3(2f * deckR, -PadGround.GroundY * 0.5f, 2f * deckR);
            deck.transform.localPosition = new Vector3(0f, PadGround.GroundY * 0.5f, 0f);

            // The design camera's side of the pad (PadGenerator.DefaultViewBearingDeg, pad frame), framing the stack and
            // the whole tower including its spike.
            var padT = pad.transform;
            float top = tp.Height + tp.CapHeight + tp.SpikeHeight;
            var towerC = padT.TransformPoint(new Vector3(0f, 0f, tp.StandOff));
            var centre = new Vector3(towerC.x * 0.5f, top * 0.5f, towerC.z * 0.5f);
            float b = PadGenerator.DefaultViewBearingDeg * Mathf.Deg2Rad;
            var dir = padT.TransformDirection(new Vector3(Mathf.Sin(b), 0f, Mathf.Cos(b))).normalized;
            const float fov = 40f;
            float aspect = (float)W / H, t = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float span = tp.StandOff + tp.Width * 0.5f + r;
            float dist = Mathf.Max(top * 0.62f / t, span * 0.62f / (t * aspect)) + span;
            var camGo = new GameObject("DesignCamera");
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false; cam.fieldOfView = fov; cam.aspect = aspect; cam.nearClipPlane = 0.3f; cam.farClipPlane = 4000f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.74f, 0.88f, 1f);
            cam.transform.position = centre + dir * dist + Vector3.up * top * 0.12f;
            cam.transform.LookAt(centre, Vector3.up);

            // Frame: the nose, the base and the tower's spike tip are on screen.
            foreach (var p in new[] { new Vector3(0f, h, 0f), Vector3.zero, padT.TransformPoint(new Vector3(0f, top, tp.StandOff)) })
            {
                var v = cam.WorldToViewportPoint(p);
                Assert.That(v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f, Is.True, name + ": " + p + " off frame at " + v);
            }

            pad.ViewCamera = cam;
            pad.QualityOverride = KitBudgets.Low;
            var late = typeof(PadRenderer).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            late?.Invoke(pad, null);
            Assert.That(pad.Ledger.DrawCalls, Is.GreaterThan(0), name + ": the pad drew nothing for the design camera");
            Assert.That(pad.Ledger.Within(KitBudgets.Low), Is.True, name + " " + pad.Ledger.Report(KitBudgets.Low));

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var rt = new RenderTexture(W * 2, H * 2, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            made.Add(rt);
            var req = new RenderPipeline.StandardRequest { destination = rt };
            if (GraphicsSettings.currentRenderPipeline == null) { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
            else if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else { Debug.LogWarning("pad-tower: the pipeline takes no render request; no capture"); return; }
            var png = UiCapture.Save(rt, "pad-tower-" + name);
            if (png != null) Assert.That(System.IO.File.Exists(png), Is.True, name + " capture written");
        }
    }
}
