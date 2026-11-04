// Flight-camera frames of the fairing joint for the PR, before and after (EditMode, CI artifact "ui-captures").
// Renders the painted parts exactly as RocketMeshBuilder.BuildParts makes them (stage grey, fairing white) through a chase-style
// camera, pre-jettison, for m1 (skinny, ratio 1.5), m7, m10, the stock preset (ratio 1.405) and m20 (fairing narrower than the stage).
// Saves fairing-joint-<id>.png. Only public RocketMeshBuilder API is used, so the same file captures the old geometry on the before branch.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Hangar;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

namespace Kestrel.Game.Tests
{
    public class FairingJointCaptureTests
    {
        const int W = 540, H = 720;
        readonly List<Object> made = new List<Object>();
        [TearDown] public void Clean() { foreach (var o in made) if (o != null) { if (o is RenderTexture rt) rt.Release(); Object.DestroyImmediate(o); } made.Clear(); }
        T Track<T>(T o) where T : Object { made.Add(o); return o; }

        static Design DesignFor(string id) => id == "stock" ? FairingJointTestData.Preset("stock") : FairingJointTestData.Winner(id);

        [TestCase("m1"), TestCase("m7"), TestCase("m10"), TestCase("stock"), TestCase("m20")]
        public void FairingJoint_FlightCamera_PreJettison(string id)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("no graphics device (-nographics)");
            var catalog = Catalog.LoadFromRepo(FairingJointTestData.Root);
            var design = DesignFor(id);
            var parts = RocketMeshBuilder.BuildParts(catalog, design);
            var scene = Track(new GameObject("JointShot"));
            float fy0 = 0f, fy1 = 0f, rj = (float)design.stages[design.stages.Count - 1].diameter / 2, rf = (float)design.fairingDiameter / 2;
            foreach (var p in parts)
            {
                if (p.Part == PartRef.Payload || p.Mesh == null) continue;
                bool fair = p.Part == PartRef.Fairing;
                if (fair) { fy0 = p.Y0; fy1 = p.Y1; }
                var go = new GameObject(p.Part.ToString());
                go.transform.SetParent(scene.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = p.Mesh;
                var m = Track(new Material(Resources.Load<Material>("Materials/KH_Body")));
                m.color = fair ? new Color(0.96f, 0.96f, 0.97f) : new Color(0.55f, 0.58f, 0.64f);
                go.AddComponent<MeshRenderer>().sharedMaterial = m;
            }
            Assert.That(fy1, Is.GreaterThan(fy0), "no fairing part");
            float L = fy1 - fy0;
            // Flight chase view: behind and a little below, looking at the fairing and the stage top.
            var camGo = new GameObject("FlightCam"); camGo.transform.SetParent(scene.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.30f, 0.45f, 0.70f);
            cam.fieldOfView = 34f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 3000f; cam.enabled = false;
            var target = new Vector3(0f, fy0 + Mathf.Min(L * 0.5f, 2f * Mathf.Max(rf, rj) + 1f), 0f);
            float span = Mathf.Max(L * 0.9f, Mathf.Max(rf, rj) * 6f);
            float dist = span / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * 1.15f;
            cam.transform.position = target + new Vector3(dist * 0.55f, -dist * 0.05f, -dist * 0.85f);
            cam.transform.LookAt(target);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.transform.SetParent(scene.transform, false);
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.transform.rotation = Quaternion.Euler(35f, -40f, 0f);

            var rt = Track(new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32)); rt.Create();
            var req = new RenderPipeline.StandardRequest { destination = rt };
            if (GraphicsSettings.currentRenderPipeline == null) { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
            else if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else Assert.Ignore("the pipeline takes no render request");

            // Save first, so the frame exists even when the joint assertions below fail on the old geometry.
            var png = UiCapture.Save(rt, "fairing-joint-" + id);
            var prev = RenderTexture.active;
            var tex = Track(new Texture2D(W, H, TextureFormat.RGBA32, false));
            RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = prev;
            int magenta = 0; var px = tex.GetPixels32();
            foreach (var c in px) if (c.r > 170 && c.b > 170 && c.g < 100 && Mathf.Abs(c.r - c.b) < 80) magenta++;
            TestContext.WriteLine($"fairing-joint-{id}: stage D {2 * rj:F2} fairing D {2 * rf:F2} png {png}");
            Assert.That(magenta / (float)px.Length, Is.LessThan(0.002f), "magenta pixels");
            Assert.That(png, Is.Not.Null, "no PNG written");

            // The same geometry rule as the mesh tests, on exactly the mesh just drawn.
            foreach (var p in parts)
                if (p.Part == PartRef.Fairing)
                {
                    var sil = FairingJointMeshTests.Silhouette(p.Mesh, p.Y0, p.Y1);
                    Assert.That(sil[0].x, Is.EqualTo(rj).Within(2e-3f), id + ": fairing base radius must equal the stage radius");
                }
        }
    }
}
