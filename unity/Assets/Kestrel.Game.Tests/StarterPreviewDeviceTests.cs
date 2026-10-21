// Starter preview, render level (TestFlight build 204: "When I change the design nothing changes with the rocket";
// the rocket was a sliver on a distant pad). KH-3 click-through, cumulative: default, 3 stages, 1 stage, kerosene,
// max fuel notch, min fuel notch. Each step builds the real vehicle with RocketMeshBuilder (the meshes SliceApp
// draws), checks it changed in the expected direction (taller with more stages, longer tanks with more fuel, a
// different bell finish for kerosene), then frames it with the real StarterPreviewCamera on a Unity Camera at
// 335x720, 390x844, 471x1024 and web 1280x800, against the real StarterLayout preview box, and measures the
// projected height of every mesh vertex with Camera.WorldToViewportPoint (the custom off-axis projection
// included): 50-75% of the preview box, fully inside with margin. With a GPU each step is rendered to
// starter-preview-<size>-<step>.png in UiCapture.OutDir() (CI artifact "ui-captures"): the vehicle, its bells in
// the fuel finish, a deck disc, the area outside the preview box dimmed and the box outlined.
// Batchmode-safe: no LogError; everything created here is destroyed in TearDown.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using Kestrel.Vehicle;
using NUnit.Framework;
using UnityEngine;
using Material = UnityEngine.Material;

namespace Kestrel.Game.Tests
{
    public class StarterPreviewDeviceTests
    {
        struct Size { public string Name; public float W, H, Top, Bottom, Scale; public bool Wide; }

        static IEnumerable<Size> Sizes()
        {
            yield return new Size { Name = "335x720", W = 335, H = 720, Top = 20, Scale = 2 };
            yield return new Size { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34, Scale = 2 };
            yield return new Size { Name = "471x1024", W = 471, H = 1024, Top = 59, Bottom = 34, Scale = 2 };
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float s = WebLandscapeType.PanelScale(1280, 800, rw, rh, m);
            yield return new Size { Name = "1280x800", W = Mathf.Round(1280 / s), H = Mathf.Round(800 / s), Wide = true, Scale = s };
        }

        static readonly string[] Steps = { "default", "3-stages", "1-stage", "kerosene", "max-fuel", "min-fuel" };

        static void ApplyStep(StarterRocket r, string step)
        {
            switch (step)
            {
                case "3-stages": r.Stages = 3; break;
                case "1-stage": r.Stages = 1; break;
                case "kerosene": r.Engine = StarterEngine.Kerosene; break;
                case "max-fuel": r.FuelNotch = StarterRocket.FuelNotches - 1; break;
                case "min-fuel": r.FuelNotch = 0; break;
            }
        }

        static Catalog catalog; static Campaign campaign; static Dictionary<string, JsonObject> bases;
        readonly List<UnityEngine.Object> made = new List<UnityEngine.Object>();
        /// <summary>One size and step: camera, target, scene. Cleared after every capture; the built meshes live in made.</summary>
        readonly List<UnityEngine.Object> scene = new List<UnityEngine.Object>();

        [OneTimeSetUp]
        public void Load()
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            catalog = Catalog.LoadFromRepo(root);
            campaign = Campaign.Load(root);
            bases = StarterBases.Parse(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
        }

        [TearDown]
        public void Clean()
        {
            CleanScene();
            foreach (var o in made) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            made.Clear();
        }

        void CleanScene()
        {
            foreach (var o in scene) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            scene.Clear();
        }

        static StarterRocket Kh3() => new StarterRocket(catalog, campaign.ById("m3"), JsonMut.CloneObject(bases["m3"]));

        static StarterFrame Frame(Size s, StarterRocket r)
        {
            StarterScreen.Readiness(r, out var badge, out var rest);
            return StarterLayout.Place(new StarterLayoutInput
            {
                Width = s.W, Height = s.H, SafeTop = s.Top, SafeBottom = s.Bottom, Wide = s.Wide, ReserveExit = s.Wide,
                BoostersVisible = r.BoostersVisible, Title = StarterScreen.Title(r.Mission), Subtitle = StarterScreen.Subtitle(r.Mission),
                Hint = StarterScreen.Hint(r, StarterKnob.None), Badge = badge, Readiness = rest,
            });
        }

        sealed class Built
        {
            public string Step; public Design Design; public RocketMeshBuilder.Result R; public List<RocketMeshBuilder.PartMesh> Parts;
            public float Height => R.height;
            public int PartCount => Parts.Count;
            public float Tank0 { get { foreach (var t in R.tanks) if (t.Stage == 0 && !t.StrapOn) return t.BarrelLength; return 0f; } }
            public float TankSum { get { float s = 0; foreach (var t in R.tanks) s += t.BarrelLength; return s; } }
            public string Finish0 => R.engines.Count > 0 ? BellFinish.For(R.engines[0].Propellant(Design)) : "";
            public string Engine0 => R.engines.Count > 0 ? R.engines[0].EngineId : "";
            public float Radius { get { var b = R.mesh.bounds; return Mathf.Max(Mathf.Max(Mathf.Abs(b.min.x), Mathf.Abs(b.max.x)), Mathf.Max(Mathf.Abs(b.min.z), Mathf.Abs(b.max.z))); } }
        }

        List<Built> ClickThrough()
        {
            var r = Kh3();
            var list = new List<Built>();
            foreach (var step in Steps)
            {
                ApplyStep(r, step);
                var d = Design.FromJson(r.BuildDesign());
                var b = new Built { Step = step, Design = d, R = RocketMeshBuilder.Build(catalog, d), Parts = RocketMeshBuilder.BuildParts(catalog, d) };
                made.Add(b.R.mesh);
                foreach (var p in b.Parts) if (p.Mesh != null) made.Add(p.Mesh);
                list.Add(b);
            }
            return list;
        }

        [Test]
        public void Kh3_EveryKnobChange_BuildsADifferentVehicle_InTheExpectedDirection()
        {
            var b = ClickThrough();
            Built Of(string s) => b.Find(x => x.Step == s);
            foreach (var x in b) TestContext.WriteLine($"{x.Step}: height {x.Height:0.0} m, parts {x.PartCount}, stage-1 tank {x.Tank0:0.00} m, tanks {x.TankSum:0.00} m, engine {x.Engine0}, bell finish {x.Finish0}");
            for (int i = 1; i < b.Count; i++)
            {
                bool differs = Mathf.Abs(b[i].Height - b[i - 1].Height) > 0.01f || b[i].PartCount != b[i - 1].PartCount
                    || Mathf.Abs(b[i].TankSum - b[i - 1].TankSum) > 0.01f || b[i].Engine0 != b[i - 1].Engine0;
                Assert.IsTrue(differs, b[i].Step + ": the built vehicle must change");
            }
            Assert.Greater(Of("3-stages").Height, Of("default").Height + 1f, "3 stages: taller");
            Assert.Greater(Of("3-stages").PartCount, Of("default").PartCount, "3 stages: a stage and an interstage more");
            Assert.Less(Of("1-stage").Height, Of("default").Height - 1f, "1 stage: shorter than the default");
            Assert.Less(Of("1-stage").PartCount, Of("default").PartCount, "1 stage: fewer parts");
            Assert.AreNotEqual(Of("1-stage").Engine0, Of("kerosene").Engine0, "kerosene: a kerosene engine");
            Assert.AreEqual(BellFinish.Clean, Of("1-stage").Finish0, "methane bells: clean steel");
            Assert.AreEqual(BellFinish.Sooty, Of("kerosene").Finish0, "kerosene bells: sooty");
            Assert.Greater(Mathf.Abs(Of("kerosene").Tank0 - Of("1-stage").Tank0), 0.05f, "kerosene is denser: the tank length changes");
            Assert.Greater(Of("max-fuel").Tank0, Of("kerosene").Tank0 * 1.1f, "max fuel notch (+20%): a longer tank");
            Assert.Greater(Of("max-fuel").Height, Of("kerosene").Height, "max fuel notch: taller");
            Assert.Less(Of("min-fuel").Tank0, Of("max-fuel").Tank0 * 0.75f, "min fuel notch (-20%): a shorter tank");
            Assert.Less(Of("min-fuel").Height, Of("max-fuel").Height, "min fuel notch: shorter");

            var sooty = BellBuilder.Material(BellFinish.Sooty);
            var clean = BellBuilder.Material(BellFinish.Clean);
            if (sooty != null && clean != null)
            {
                Assert.That(sooty, Is.Not.SameAs(clean), "each fuel draws with its own bell material");
                Assert.IsTrue(sooty.enableInstancing && clean.enableInstancing, "bells stay instanced (one batch per fuel)");
                Assert.Greater(clean.color.grayscale - sooty.color.grayscale, 0.3f, "kerosene vs methane bells read apart");
            }
            else Debug.LogWarning("[StarterPreview] KH_Metal not loadable here; bell materials not compared");
        }

        [Test]
        public void Kh3_EveryStep_IsFramedBig_InsideThePreviewBox_OnEverySize()
        {
            var b = ClickThrough();
            var fails = new List<string>();
            foreach (var size in Sizes())
            {
                var r = Kh3();
                double prevDistance = double.NaN; float prevHeight = float.NaN;
                foreach (var x in b)
                {
                    ApplyStep(r, x.Step);
                    var frame = Frame(size, r);
                    var (cam, preview, rt) = Rig(size);
                    preview.SetVehicle(0f, x.Height, x.Radius);
                    preview.SetPreview(frame.Rocket, size.W, size.H);
                    preview.Snap();
                    string where = size.Name + " " + x.Step;
                    if (!preview.Pose.Valid) { fails.Add(where + ": no pose"); continue; }
                    Measure(cam, x, size, out float top, out float bottom, out float left, out float right);
                    float fill = (bottom - top) / frame.Rocket.H;
                    float my = frame.Rocket.H * 0.02f, mx = frame.Rocket.W * 0.02f;
                    TestContext.WriteLine($"{where}: fill {fill:0.000} of the {frame.Rocket.W:0}x{frame.Rocket.H:0} pt preview, vehicle {left:0}-{right:0} x {top:0}-{bottom:0} pt, distance {preview.Pose.Distance:0.0} m");
                    if (fill < 0.50f || fill > 0.75f) fails.Add(where + ": projected height " + fill.ToString("0.000") + " of the preview box");
                    if (top < frame.Rocket.Y + my || bottom > frame.Rocket.Bottom - my || left < frame.Rocket.X + mx || right > frame.Rocket.Right - mx)
                        fails.Add(where + $": not inside the preview box with margin (vehicle {left:0}-{right:0} x {top:0}-{bottom:0}, box {frame.Rocket.X:0}-{frame.Rocket.Right:0} x {frame.Rocket.Y:0}-{frame.Rocket.Bottom:0})");
                    if (!double.IsNaN(prevDistance) && Mathf.Abs(x.Height - prevHeight) > 1f && Math.Abs(preview.Pose.Distance - prevDistance) < 0.5)
                        fails.Add(where + ": the camera did not reframe after the vehicle changed height");
                    prevDistance = preview.Pose.Distance; prevHeight = x.Height;
                    Capture(cam, rt, x, size, frame, "starter-preview-" + size.Name + "-" + x.Step);
                    CleanScene();
                }
            }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        (Camera cam, StarterPreviewCamera preview, RenderTexture rt) Rig(Size s)
        {
            int w = Mathf.Max(8, Mathf.RoundToInt(s.W * s.Scale)), h = Mathf.Max(8, Mathf.RoundToInt(s.H * s.Scale));
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "kh-starter-preview" };
            scene.Add(rt);
            var go = new GameObject("kh-starter-preview-cam") { hideFlags = HideFlags.HideAndDontSave };
            scene.Add(go);
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;   // rendered by hand
            cam.fieldOfView = 50f; cam.nearClipPlane = 0.5f; cam.farClipPlane = 20000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.15f, 0.27f);
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;
            var preview = go.AddComponent<StarterPreviewCamera>();
            preview.ReduceMotion = true;
            return (cam, preview, rt);
        }

        /// <summary>Screen-point bounds (y down) of every vertex of the built vehicle plus the bell exit rings on the deck.</summary>
        static void Measure(Camera cam, Built x, Size s, out float top, out float bottom, out float left, out float right)
        {
            top = left = float.PositiveInfinity; bottom = right = float.NegativeInfinity;
            var pts = new List<Vector3>(x.R.mesh.vertices);
            foreach (var p in x.Parts)
            {
                if (p.Mesh == null) continue;
                var vs = p.Mesh.vertices;
                if (p.Instances == null) pts.AddRange(vs);
                else foreach (var at in p.Instances) foreach (var v in vs) pts.Add(v + at);
            }
            foreach (var e in x.R.engines)
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6f;
                    pts.Add(e.Exit + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * e.ExitDiameter * 0.5f);
                }
            foreach (var p in pts)
            {
                var v = cam.WorldToViewportPoint(p);
                float px = v.x * s.W, py = (1f - v.y) * s.H;
                top = Mathf.Min(top, py); bottom = Mathf.Max(bottom, py); left = Mathf.Min(left, px); right = Mathf.Max(right, px);
            }
        }

        void Capture(Camera cam, RenderTexture rt, Built x, Size s, StarterFrame frame, string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                TestContext.WriteLine("capture skipped: " + name + " (no graphics device)");
                return;
            }
            var body = Resources.Load<Material>("Materials/KH_Body");
            var root = new GameObject("kh-starter-preview-vehicle") { hideFlags = HideFlags.HideAndDontSave };
            scene.Add(root);
            var view = new HangarRocketView();
            view.Build(x.Parts, root.transform, body);
            view.SetVisible(true);
            foreach (var e in x.R.engines)
            {
                if (!BellSpec.TryParseCycle(e.Cycle, out var cycle)) continue;
                var spec = new BellSpec { EngineId = e.EngineId, Cycle = cycle, ExpansionRatio = e.Expansion, ExitDiameter = e.ExitDiameter, VehicleSeed = BellVariation.HashId(x.Design.id ?? "kh") };
                if (spec.Validate().Count > 0) continue;
                var mesh = BellBuilder.Build(BellProfile.Build(spec, 0), 0);
                scene.Add(mesh);
                var bell = new GameObject("Bell", typeof(MeshFilter), typeof(MeshRenderer));
                bell.transform.SetParent(root.transform, false);
                bell.transform.localPosition = e.Exit;
                bell.GetComponent<MeshFilter>().sharedMesh = mesh;
                bell.GetComponent<MeshRenderer>().sharedMaterial = BellBuilder.Material(BellFinish.For(e.Propellant(x.Design)));
            }
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deck.hideFlags = HideFlags.HideAndDontSave;
            scene.Add(deck);
            var col = deck.GetComponent<Collider>(); if (col != null) UnityEngine.Object.DestroyImmediate(col);
            float deckR = Mathf.Max(8f, x.Radius * 4f);
            deck.transform.localScale = new Vector3(2f * deckR, 0.25f, 2f * deckR);
            deck.transform.position = new Vector3(0f, -0.25f, 0f);
            if (body != null)
            {
                var deckMat = new Material(body) { color = new Color(0.55f, 0.55f, 0.53f) };
                scene.Add(deckMat);
                deck.GetComponent<Renderer>().sharedMaterial = deckMat;
            }
            var sun = new GameObject("kh-starter-preview-sun") { hideFlags = HideFlags.HideAndDontSave };
            scene.Add(sun);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            Texture2D tex = null;
            var prev = RenderTexture.active;
            try
            {
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                // Dim everything outside the preview box (the sheet, header and corners) and outline the box, whole pixels.
                float k = rt.width / s.W;
                int bx0 = Mathf.RoundToInt(frame.Rocket.X * k), bx1 = Mathf.RoundToInt(frame.Rocket.Right * k);
                int by0 = rt.height - Mathf.RoundToInt(frame.Rocket.Bottom * k), by1 = rt.height - Mathf.RoundToInt(frame.Rocket.Y * k);
                var px = tex.GetPixels32();
                for (int yy = 0; yy < rt.height; yy++)
                    for (int xx = 0; xx < rt.width; xx++)
                    {
                        bool inBox = xx >= bx0 && xx < bx1 && yy >= by0 && yy < by1;
                        bool edge = inBox && (xx < bx0 + 2 || xx >= bx1 - 2 || yy < by0 + 2 || yy >= by1 - 2);
                        int i = yy * rt.width + xx;
                        if (edge) px[i] = new Color32(245, 180, 60, 255);
                        else if (!inBox) { var c = px[i]; px[i] = new Color32((byte)(c.r * 0.35f), (byte)(c.g * 0.35f), (byte)(c.b * 0.35f), 255); }
                    }
                tex.SetPixels32(px);
                tex.Apply();
                string path = Path.Combine(UiCapture.OutDir(), UiCapture.Slug(name) + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                TestContext.WriteLine("ui capture: " + path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[StarterPreview] " + name + " not captured: " + e.GetType().Name + ": " + e.Message);
            }
            finally
            {
                RenderTexture.active = prev;
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
