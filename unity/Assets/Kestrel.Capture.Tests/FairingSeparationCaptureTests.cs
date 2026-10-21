// Real renders of fairing separation for the PR (PlayMode, "screenshots" artifact from unity/CaptureOut).
// Builds the flight picture SliceApp builds (painted HangarRocketView parts, undrawn combined mesh, fairing seam
// cluster, FlightStageShow), flies the real mission sim and renders the nose just before and just after the
// fairing event. Writes fairing-sep-<mission>-before.png and -after.png. Fails on a flat image, and when the
// painted fairing is still drawn after separation or the payload is not.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kestrel.Capture.Tests
{
    public class FairingSeparationCaptureTests
    {
        const int W = 900, H = 1200;

        [UnityTest]
        public IEnumerator FairingSeparation_BeforeAndAfter([Values("m3", "m10")] string id)
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            var catalog = Catalog.LoadFromRepo(root);
            var campaign = Campaign.Load(root);
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
            Design design = null;
            foreach (JsonObject row in file.Arr("missions"))
                if (row.Str("id") == id) design = Design.FromJson((JsonObject)row.Arr("designs")[0]);
            Assert.IsNotNull(design, id);

            var scene = new GameObject("FairingShot");
            try
            {
                var built = RocketMeshBuilder.Build(catalog, design);
                var parts = RocketMeshBuilder.BuildParts(catalog, design);
                var body = Lit(new Color(0.93f, 0.94f, 0.96f));
                var rocket = new GameObject("Rocket");
                rocket.transform.SetParent(scene.transform, false);
                rocket.AddComponent<MeshFilter>().sharedMesh = built.mesh;
                var mr = rocket.AddComponent<MeshRenderer>();
                mr.sharedMaterial = body;
                mr.forceRenderingOff = true;
                var view = new HangarRocketView();
                view.Build(parts, rocket.transform, body);
                view.ApplyLook(null, null);
                var cam = MakeCamera(scene.transform);
                var seams = rocket.AddComponent<Kestrel.Vehicle.StrutCluster>();
                seams.ViewCamera = cam;
                seams.Gate = mr;
                var f = built.fairing;
                seams.Init(Kestrel.Vehicle.FairingLayout.Build(f.BaseY, f.Radius, f.Length, f.NoseFraction, f.Conic), Kestrel.Kit.KitBudgets.Medium);
                var show = rocket.AddComponent<FlightStageShow>();
                show.Prepare(catalog, design, built, null, view, seams);
                show.Arm(null, false);
                view.SetVisible(true);

                float cy = f.BaseY + f.Length * 0.45f;
                float dist = Mathf.Max(f.Length * 2.6f, f.Radius * 10f);
                cam.transform.position = new Vector3(dist * 0.35f, cy + f.Length * 0.15f, -dist);
                cam.transform.LookAt(new Vector3(0f, cy, 0f));

                var setup = MissionFlightSetup.Build(catalog, campaign.ById(id), design, MissionFlightSetup.BatchDt, null, MissionFlightSetup.BatchDt);
                var sim = new AscentSim(setup.Stages, setup.Options);
                while (!sim.Done) sim.Tick();
                var flight = sim.Finish();

                var samples = flight.samples;
                int i = 0;
                double sepT = -1;
                for (; i < samples.Count; i++)
                {
                    var s = samples[i];
                    show.PreviewAt(flight.events, s.t, s.alt, s.v, s.q, 1);
                    view.SetVisible(true);
                    if (show.Parts.FairingSeparated) { sepT = s.t; break; }
                }
                Assert.That(sepT, Is.GreaterThan(0), id + " never separated its fairing");

                // "Before": rebuild the state one sample earlier (Arm resets, then replay up to i - 1).
                show.Arm(null, false);
                view.SetVisible(true);
                for (int k = 0; k < i; k++) { var s = samples[k]; show.PreviewAt(flight.events, s.t, s.alt, s.v, s.q, 1); view.SetVisible(true); }
                yield return null;
                Assert.IsTrue(view.RootOf(PartRef.Fairing).gameObject.activeSelf, id + " painted fairing missing before separation");
                Save(cam, $"fairing-sep-{id}-before");

                // "After": about 80% of the peel, so both halves are clear of the stack and the payload shows.
                double afterT = sepT + FlightStaging.FairingSec * 0.8;
                for (int k = i; k < samples.Count && samples[k].t <= afterT; k++) { var s = samples[k]; show.PreviewAt(flight.events, s.t, s.alt, s.v, s.q, 1); view.SetVisible(true); }
                yield return null;
                Assert.IsFalse(view.RootOf(PartRef.Fairing).gameObject.activeSelf, id + " painted fairing still drawn after separation");
                Assert.IsTrue(view.RootOf(PartRef.Payload).gameObject.activeSelf, id + " payload not revealed");
                Assert.IsTrue(show.FairingHalf(false).gameObject.activeSelf && show.FairingHalf(true).gameObject.activeSelf, id + " halves not shown");
                Assert.IsFalse(seams.enabled, id + " fairing seams still drawn");
                Save(cam, $"fairing-sep-{id}-after");
            }
            finally
            {
                UnityEngine.Object.Destroy(scene);
            }
        }

        static Camera MakeCamera(Transform parent)
        {
            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(parent, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.09f, 0.2f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;
            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(parent, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            return cam;
        }

        static void Save(Camera cam, string name)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            string path = Path.Combine(OutDir(), name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            var px = tex.GetPixels32();
            int distinct = px.Where((_, k) => k % 97 == 0).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count();
            Debug.Log("[Capture] " + name + ": " + path + " colours=" + distinct);
            Assert.That(distinct, Is.GreaterThan(8), name + " capture is flat");
            rt.Release();
            UnityEngine.Object.Destroy(tex);
        }

        static UnityEngine.Material Lit(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var mat = new UnityEngine.Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            return mat;
        }

        static string OutDir()
        {
            var dir = Environment.GetEnvironmentVariable("KH_CAP_OUT");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
