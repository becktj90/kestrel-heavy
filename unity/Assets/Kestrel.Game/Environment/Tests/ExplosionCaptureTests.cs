// A skinny rocket (campaign m1: one 0.8 m stage) is lost in flight and the explosion is rendered through a flight-style camera.
// Trevor's TestFlight screenshot showed a huge flat magenta disc where the blast should be. This drives the real path
// (CinematicFlightSink.OnFailure -> CinematicEvents -> ExplosionFx / ShockwaveFx / DebrisFx), steps the particles, and renders
// three frames across the blast: the breakup, the fireball, the smoke. PNGs: explosion-skinny-<n>-<name>.png via UiCapture ->
// CaptureOut/ui, CI artifact "ui-captures". Fails on magenta pixels, on a screen-filling bright flash, and on a frame with no fire.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Material = UnityEngine.Material;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Tests;
using Kestrel.Kit;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Environment.Tests
{
    public class ExplosionCaptureTests
    {
        const int W = 540, H = 960;
        readonly List<Object> made = new List<Object>();

        [TearDown]
        public void Clean()
        {
            ExplosionFx.QualityOverride = -1; ExplosionFx.ViewCamera = null;
            foreach (var o in made) if (o != null) { if (o is RenderTexture rt) rt.Release(); Object.DestroyImmediate(o); }
            made.Clear();
            CinematicEvents.ClearSubscribers(); CinematicEvents.ResetSinkState(); FlashGovernor.Reset();
        }

        T Track<T>(T o) where T : Object { made.Add(o); return o; }

        static Design Skinny(string root)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
            foreach (JsonObject row in file.Arr("missions"))
                if (row.Str("id") == "m1") return Design.FromJson((JsonObject)row.Arr("designs")[0]);
            Assert.Fail("no m1 winner"); return null;
        }

        static void Count(Texture2D tex, out float magenta, out float white, out float fire)
        {
            var px = tex.GetPixels32();
            int m = 0, w = 0, f = 0;
            foreach (var c in px)
            {
                if (c.r > 170 && c.b > 170 && c.g < 100 && Mathf.Abs(c.r - c.b) < 80) m++;
                if (c.r > 235 && c.g > 235 && c.b > 235) w++;
                if (c.r > 190 && c.g > 50 && c.g < 215 && c.b < 120 && c.r - c.b > 110) f++;
            }
            magenta = m / (float)px.Length; white = w / (float)px.Length; fire = f / (float)px.Length;
        }

        [Test]
        public void SkinnyRocketBlowingUpRendersFireDebrisAndSmokeNeverMagenta()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { Assert.Ignore("no graphics device (-nographics)"); }
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            var catalog = Catalog.LoadFromRepo(root);
            var design = Skinny(root);
            float widest = 0f;
            foreach (var st in design.stages) widest = Mathf.Max(widest, (float)st.diameter);
            Assert.That(widest, Is.LessThanOrEqualTo(1.0f), "m1 is the skinny design");

            ExplosionFx.QualityOverride = 2;
            CinematicEvents.ClearSubscribers(); CinematicEvents.ResetSinkState(); FlashGovernor.Reset();
            var scene = Track(new GameObject("ExplosionShot"));
            var fx = scene.AddComponent<ExplosionFx>(); fx.Subscribe();
            var sw = scene.AddComponent<ShockwaveFx>(); sw.Subscribe();
            var debris = scene.AddComponent<DebrisFx>(); debris.Subscribe();

            // The rocket as flown, standing at 3000 m, base at the origin of the scene anchor.
            var built = RocketMeshBuilder.Build(catalog, design);
            var rocket = new GameObject("Rocket");
            rocket.transform.SetParent(scene.transform, false);
            rocket.AddComponent<MeshFilter>().sharedMesh = built.mesh;
            var body = Track(new Material(MaterialLibrary.Lit()));
            body.color = new Color(0.9f, 0.91f, 0.94f);
            rocket.AddComponent<MeshRenderer>().sharedMaterial = body;
            var b = built.mesh.bounds;
            float len = b.size.y;
            var center = rocket.transform.TransformPoint(b.center);

            var camGo = new GameObject("FlightCam");
            camGo.transform.SetParent(scene.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.30f, 0.45f, 0.70f);   // daytime sky, never magenta-like
            cam.fieldOfView = 40f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 4000f;
            float dist = Mathf.Max(len * 2.0f, 9f);
            cam.transform.position = center + new Vector3(dist * 0.25f, -len * 0.1f, -dist);   // flight chase: behind and a little below
            cam.transform.LookAt(center);
            cam.enabled = false;
            ExplosionFx.ViewCamera = cam;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(scene.transform, false);
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.transform.rotation = Quaternion.Euler(40f, -35f, 0f);

            // The real hook, with the vehicle radius the sim wiring (FlightDriver) derives from the design.
            var sink = new CinematicFlightSink { Anchor = () => center, Reduce = () => false, VehicleRadiusM = FlightSinkMath.VehicleRadiusM(widest) };
            var V = new FlightVec(0, 3000);
            sink.OnFailure("structural-maxq", "q", -1, V, V, 63.0);

            // The pools are hidden objects the fx components own (not children of this scene): step exactly those systems.
            var systems = new List<ParticleSystem>();
            foreach (ExplosionLayer layer in System.Enum.GetValues(typeof(ExplosionLayer)))
            {
                var lr = fx.LayerRenderer(layer);
                if (lr != null) systems.Add(lr.GetComponent<ParticleSystem>());
            }
            var spark = debris.SparkRenderer;
            if (spark != null) systems.Add(spark.GetComponent<ParticleSystem>());
            systems.RemoveAll(x => x == null);
            Assert.That(systems.Count, Is.GreaterThanOrEqualTo(4), "the explosion pools exist");
            float t = 0f, sawFire = 0f;
            var plan = fx.LastPlan;
            Assert.That(plan.Fire, Is.GreaterThan(0), "the failure planned no fire");
            Assert.That(plan.Radius, Is.LessThan(16f));

            float[] at = { 0.12f, 0.55f, 1.5f };
            string[] names = { "breakup", "fireball", "smoke" };
            for (int f = 0; f < at.Length; f++)
            {
                while (t < at[f])
                {
                    const float dt = 1f / 30f;
                    foreach (var ps in systems) ps.Simulate(dt, false, false, true);
                    fx.Tick(dt); sw.Tick(dt); debris.Tick(dt);
                    t += dt;
                }
                rocket.SetActive(f == 0);   // the airframe is gone once it has broken up
                var rt = Track(new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32));
                rt.Create();
                var req = new RenderPipeline.StandardRequest { destination = rt };
                if (GraphicsSettings.currentRenderPipeline == null) { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
                else if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
                else { Assert.Ignore("the pipeline takes no render request"); }

                var prev = RenderTexture.active;
                var tex = Track(new Texture2D(W, H, TextureFormat.RGBA32, false));
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                Count(tex, out float magenta, out float white, out float fire);
                int live = 0; foreach (var ps in systems) live += ps.particleCount;
                TestContext.WriteLine($"explosion frame {names[f]}: live particles {live}");
                TestContext.WriteLine($"explosion frame {names[f]}: magenta {magenta:P2} white {white:P2} fire {fire:P2}");
                Assert.That(magenta, Is.LessThan(0.002f), names[f] + ": magenta pixels, a missing material or shader");
                Assert.That(white, Is.LessThan(0.30f), names[f] + ": a bright flash covers more than 30% of the screen");
                sawFire = Mathf.Max(sawFire, fire);
                var png = UiCapture.Save(rt, $"explosion-skinny-{f + 1}-{names[f]}");
                Assert.That(png, Is.Not.Null, names[f] + ": no PNG written");
            }
            Assert.That(sawFire, Is.GreaterThan(0.002f), "no fire-coloured pixels in any frame: the explosion is not visible");
        }
    }
}
