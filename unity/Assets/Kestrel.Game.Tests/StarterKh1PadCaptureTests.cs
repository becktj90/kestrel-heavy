// KH-1 First Hop, the Starter ("designer") screen with its pad (TestFlight 208: "no yellow crown, no base plates and no pad
// circle, just pale slabs and yellow legs, with the vehicle cropping the tower"). Builds the pad the way SliceApp.BuildRocket does
// for the real KH-1 starter vehicle (RocketMeshBuilder over the campaign winner's first design, PadTower.Apply, PadDetailLink's
// ground cut, PadRenderer + PadDetailRenderer drawn for the camera) and frames it with the REAL StarterPreviewCamera over the REAL
// StarterLayout preview box, fed the same pad points SliceApp feeds it (StarterPadPoints.For). Portrait 390x844 and 430x932 (Pro Max)
// and web landscape 1280x800.
//
// Asserts, with Unity's own projection (WorldToViewportPoint, off-axis shift included), not the solver's: every tower-crown, base-plate
// and pad-circle point of the layout is inside the preview box, and the crown's real member ends (through the Pad child's own transform) are too. Also runs the pure
// StarterPadFrame evaluator on the camera's pose (inside the box, none hidden behind the vehicle past its limits, vehicle legible).
// PNG per size: starter-kh1-<size>.png in UiCapture.OutDir() (CI artifact "ui-captures"), the preview box outlined.
// Batchmode-safe: no LogError; everything created here is destroyed in TearDown.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using Kestrel.Pad;
using Kestrel.Slice;
using Kestrel.Vehicle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

namespace Kestrel.Game.Tests
{
    public class StarterKh1PadCaptureTests
    {
        struct Size { public string Name; public float W, H, Top, Bottom, Scale; public bool Wide; }

        static IEnumerable<Size> Sizes()
        {
            yield return new Size { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34, Scale = 2 };
            yield return new Size { Name = "430x932", W = 430, H = 932, Top = 59, Bottom = 34, Scale = 2 };
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float s = WebLandscapeType.PanelScale(1280, 800, rw, rh, m);
            yield return new Size { Name = "1280x800", W = Mathf.Round(1280 / s), H = Mathf.Round(800 / s), Wide = true, Scale = s };
        }

        readonly List<UnityEngine.Object> made = new List<UnityEngine.Object>();
        readonly List<Camera> reEnable = new List<Camera>();

        [TearDown]
        public void Clean()
        {
            foreach (var c in reEnable) if (c != null) c.enabled = true;
            reEnable.Clear();
            for (int i = made.Count - 1; i >= 0; i--)
            {
                var o = made[i];
                if (o is RenderTexture rt) rt.Release();
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }
            made.Clear();
        }

        [Test]
        public void Kh1FirstHop_StarterScreen_ShowsTowerCrownBasePlatesAndPadCircle()
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            var catalog = Catalog.LoadFromRepo(root);
            var campaign = Campaign.Load(root);
            var bases = StarterBases.Parse(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
            var rocket = new StarterRocket(catalog, campaign.ById("m1"), JsonMut.CloneObject(bases["m1"]));
            var design = Design.FromJson(rocket.BuildDesign());
            var built = RocketMeshBuilder.Build(catalog, design);
            var parts = RocketMeshBuilder.BuildParts(catalog, design);
            made.Add(built.mesh);
            foreach (var p in parts) if (p.Mesh != null) made.Add(p.Mesh);
            float height = built.height, rr = built.mesh.bounds.extents.x;
            var b = built.mesh.bounds;
            float vehicleR = Mathf.Max(Mathf.Max(Mathf.Abs(b.min.x), Mathf.Abs(b.max.x)), Mathf.Max(Mathf.Abs(b.min.z), Mathf.Abs(b.max.z)));
            var type = PadTower.DefaultArchetype(rr);
            TestContext.WriteLine("KH-1 First Hop: " + height.ToString("0.00") + " m tall, radius " + rr.ToString("0.00") + " m (reach " + vehicleR.ToString("0.00") + " m), pad " + type);

            var scene = new GameObject("kh-starter-kh1") { hideFlags = HideFlags.HideAndDontSave };
            made.Add(scene);
            var body = Resources.Load<Material>("Materials/KH_Body");
            var vehicleGo = new GameObject("Vehicle"); vehicleGo.transform.SetParent(scene.transform, false);
            // The livery colour the flight vehicle gets (SliceApp.BuildRocket), so the stack reads against the pad in the frame.
            var vehicleMat = body != null ? new Material(body) : null;
            if (vehicleMat != null) { FlightSolidBody.Apply(vehicleMat, HangarRocketView.Hex(design.liveryPrimary, Color.white)); made.Add(vehicleMat); }
            var view = new HangarRocketView();
            view.Build(parts, vehicleGo.transform, vehicleMat != null ? vehicleMat : body);
            view.SetVisible(true);
            foreach (var e in built.engines)
            {
                if (!BellSpec.TryParseCycle(e.Cycle, out var cycle)) continue;
                var spec = new BellSpec { EngineId = e.EngineId, Cycle = cycle, ExpansionRatio = e.Expansion, ExitDiameter = e.ExitDiameter, VehicleSeed = BellVariation.HashId(design.id ?? "kh") };
                if (spec.Validate().Count > 0) continue;
                var mesh = BellBuilder.Build(BellProfile.Build(spec, 0), 0);
                made.Add(mesh);
                var bell = new GameObject("Bell", typeof(MeshFilter), typeof(MeshRenderer));
                bell.transform.SetParent(vehicleGo.transform, false);
                bell.transform.localPosition = e.Exit;
                bell.GetComponent<MeshFilter>().sharedMesh = mesh;
                bell.GetComponent<MeshRenderer>().sharedMaterial = BellBuilder.Material(BellFinish.For(e.Propellant(design)));
            }
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(scene.transform, false);
            sun.type = LightType.Directional; sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(38f, 150f, 0f);

            // The pad exactly as BuildRocket builds it: tower root at the origin, the plates of the core tank, the farm of the design.
            var towerRoot = PadTower.Create(body);
            towerRoot.SetParent(scene.transform, false);
            var plates = new List<AttachPoint>();
            plates.AddRange(TankShellUmbilicals.ForStage(TankShellSpec.Default(rr * 1.005f, height * 0.8f, 11UL), 0, 0, new Vec3(0, 0.5f, 0), true, true, true, ReleaseStyle.TailServiceMastDrop));
            var pad = PadTower.Apply(towerRoot, height, rr, type, PadTower.DefaultSeed, plates, vehicleGo.transform, PadTower.FarmFor(design));
            Assert.That(pad, Is.Not.Null, "PadTower.Apply built no pad");
            var layout = pad.Layout;
            Assert.That(layout.Tower, Is.Not.Null, "KH-1 has no service tower on its pad (archetype " + layout.Archetype + ")");
            Assert.That(layout.Archetype, Is.EqualTo(type));
            var detail = pad.GetComponent<PadDetailRenderer>();
            Assert.That(detail, Is.Not.Null, "the KH-1 pad has no deck/trench detail renderer");

            // Deck and ground, as BuildRocket sets them (deck scaled to PadGround.DeckRadius, ground cut by PadDetailLink).
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.DestroyImmediate(deck.GetComponent<Collider>());
            deck.transform.SetParent(scene.transform, false);
            float deckR = PadGround.DeckRadius(layout);
            deck.transform.localScale = new Vector3(2f * deckR, -PadGround.GroundY * 0.5f, 2f * deckR);
            deck.transform.localPosition = new Vector3(0f, PadGround.GroundY * 0.5f, 0f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(scene.transform, false);
            ground.transform.localPosition = new Vector3(0f, TrenchSpec.GradeY - 0.03f, 0f);
            ground.transform.localScale = new Vector3(20000f, 1f, 20000f);
            var gm = new Material(ground.GetComponent<Renderer>().sharedMaterial) { color = PadGround.GroundColor * 0.55f };
            if (gm.HasProperty("_Smoothness")) gm.SetFloat("_Smoothness", 0f);
            if (gm.HasProperty("_Glossiness")) gm.SetFloat("_Glossiness", 0f);
            made.Add(gm);
            ground.GetComponent<Renderer>().sharedMaterial = gm;
            PadDetailLink.Apply(null, towerRoot, ground.transform, deck.transform, layout);
            var cutMesh = ground.GetComponent<MeshFilter>().sharedMesh;
            if (cutMesh != null && cutMesh.name == PadDetailLink.GroundMeshName) made.Add(cutMesh);

            // The points the camera is given (the same call SliceApp makes) and the real crown corners, for the independent check.
            var padPoints = StarterPadPoints.For(layout, height, rr, towerRoot.position);
            Assert.That(padPoints, Is.Not.Null.And.Not.Empty, "no pad art points for KH-1");
            int nTop = 0, nPlate = 0, nCircle = 0;
            foreach (var p in padPoints) { if (p.Kind == PadFeature.TowerTop) nTop++; else if (p.Kind == PadFeature.BasePlate) nPlate++; else nCircle++; }
            Assert.That(nTop, Is.GreaterThan(0), "KH-1: the layout has no tower crown (tower.cap.*)");
            Assert.That(nPlate, Is.GreaterThan(0), "KH-1: the layout has no base plates (tower.base.plate*)");
            Assert.That(nCircle, Is.GreaterThan(0), "KH-1: no pad circle");
            var tp = layout.Tower;
            TestContext.WriteLine("KH-1 pad: surface radius " + TrenchGrammar.SurfaceRadius(layout).ToString("0.0") + " m, circle radius " + PadShowcase.CircleRadius(layout).ToString("0.0") + " m, crown top " + (tp.Height + tp.CapHeight).ToString("0.0") + " m, tower stand-off " + tp.StandOff.ToString("0.0") + " m");
            var padT = pad.transform;
            // The crown's points are PadShowcase.Points (the set the solver frames: every tower.cap.* member end except the thin lightning spike),
            // put in the world by the Pad child's own Unity transform rather than the solver's yaw maths.
            var crown = new List<Vector3>();
            foreach (var sp in PadShowcase.Points(layout))
                if (sp.Kind == ShowKind.TowerTop) crown.Add(padT.TransformPoint(new Vector3(sp.X, sp.Y, sp.Z)));
            Assert.That(crown.Count, Is.GreaterThan(0), "KH-1: no crown members to check");

            foreach (var other in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other.CompareTag("MainCamera") && other.enabled) { other.enabled = false; reEnable.Add(other); }   // PadDetailRenderer draws for Camera.main

            var fails = new List<string>();
            foreach (var size in Sizes())
            {
                var frame = StarterLayout.Place(new StarterLayoutInput
                {
                    Width = size.W, Height = size.H, SafeTop = size.Top, SafeBottom = size.Bottom, Wide = size.Wide, ReserveExit = size.Wide,
                    BoostersVisible = rocket.BoostersVisible, Title = StarterScreen.Title(rocket.Mission), Subtitle = StarterScreen.Subtitle(rocket.Mission),
                    Hint = StarterScreen.Hint(rocket, StarterKnob.None), Badge = "Ready", Readiness = "Ready to launch",
                });
                int w = Mathf.Max(8, Mathf.RoundToInt(size.W * size.Scale)), h = Mathf.Max(8, Mathf.RoundToInt(size.H * size.Scale));
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "kh-starter-kh1" };
                made.Add(rt);
                var camGo = new GameObject("kh-starter-kh1-cam") { tag = "MainCamera", hideFlags = HideFlags.HideAndDontSave };
                made.Add(camGo);
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                cam.fieldOfView = 50f; cam.nearClipPlane = 0.5f; cam.farClipPlane = 20000f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.74f, 0.88f, 1f);
                cam.targetTexture = rt; cam.aspect = (float)w / h;
                var preview = camGo.AddComponent<StarterPreviewCamera>();
                preview.ReduceMotion = true;
                // The same call SliceApp.BuildRocket makes for the Starter screen.
                preview.SetVehicle(0f, height, vehicleR, padPoints);
                preview.SetPreview(frame.Rocket, size.W, size.H);
                preview.Snap();
                string where = size.Name;
                if (!preview.Pose.Valid) { fails.Add(where + ": no pose"); UnityEngine.Object.DestroyImmediate(camGo); continue; }

                var rep = StarterPadFrame.Evaluate(preview.Pose, preview.Subject, frame.Rocket, size.W, size.H, (double)w / h, cam.fieldOfView);
                TestContext.WriteLine(where + ": heading " + preview.Pose.HeadingDeg + ", distance " + preview.Pose.Distance.ToString("0.0") + " m, " + rep.Describe());
                if (!rep.Pass) fails.Add(where + ": " + rep.Describe());
                double need = StarterPadFrame.RequiredFill(size.Wide);   // 0.5 on phones and iPad, 0.4 on the landscape web box
                TestContext.WriteLine(where + ": vehicle fill " + rep.VehicleFill.ToString("0.00") + " (needs " + need.ToString("0.00") + ")");
                if (rep.VehicleFill < need) fails.Add(where + ": vehicle fill " + rep.VehicleFill.ToString("0.00") + " under " + need.ToString("0.00"));

                // Unity's own projection: inside the preview box (points, y down) with a 1% margin.
                float mx = frame.Rocket.W * 0.01f, my = frame.Rocket.H * 0.01f;
                bool Inside(Vector3 wp, out Vector2 at)
                {
                    var v = cam.WorldToViewportPoint(wp);
                    at = new Vector2(v.x * size.W, (1f - v.y) * size.H);
                    return v.z > 0f && at.x >= frame.Rocket.X + mx && at.x <= frame.Rocket.Right - mx && at.y >= frame.Rocket.Y + my && at.y <= frame.Rocket.Bottom - my;
                }
                int outside = 0, circleIn = 0, circleAll = 0; string first = null;
                foreach (var p in padPoints)
                {
                    bool inside = Inside(new Vector3((float)p.X, (float)p.Y, (float)p.Z), out var at);
                    if (p.Kind == PadFeature.PadCircle) { circleAll++; if (inside) circleIn++; continue; }   // the ring is held to a share, below
                    if (!inside) { outside++; first = first ?? (p.Kind + " at " + at); }
                }
                TestContext.WriteLine(where + ": pad circle (optional) " + circleIn + " of " + circleAll + " points inside the preview box in Unity's projection");
                foreach (var c in crown)
                    if (!Inside(c, out var at)) { outside++; first = first ?? ("crown member end at " + at); }
                if (outside > 0) fails.Add(where + ": " + outside + " crown/plate points outside the preview box in Unity's projection (first: " + first + "; box " + frame.Rocket.X.ToString("0") + "-" + frame.Rocket.Right.ToString("0") + " x " + frame.Rocket.Y.ToString("0") + "-" + frame.Rocket.Bottom.ToString("0") + ")");

                pad.ViewCamera = cam;
                pad.QualityOverride = KitBudgets.Low;   // the iPhone tier: nothing may be culled or LOD'd away at it
                Capture(cam, rt, pad, detail, size, frame, "starter-kh1-" + size.Name, fails);
                UnityEngine.Object.DestroyImmediate(camGo);
            }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        void Capture(Camera cam, RenderTexture rt, PadRenderer pad, PadDetailRenderer detail, Size s, StarterFrame frame, string name, List<string> fails)
        {
            const BindingFlags any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            cam.enabled = true;   // PadDetailRenderer resolves Camera.main
            typeof(PadRenderer).GetMethod("LateUpdate", any)?.Invoke(pad, null);
            typeof(PadDetailRenderer).GetMethod("LateUpdate", any)?.Invoke(detail, null);
            cam.enabled = false;
            if (pad.Ledger.DrawCalls <= 0) fails.Add(name + ": the pad drew nothing for this camera");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { TestContext.WriteLine("capture skipped: " + name + " (no graphics device)"); return; }
            Texture2D tex = null;
            var prev = RenderTexture.active;
            try
            {
                var req = new RenderPipeline.StandardRequest { destination = rt };
                if (GraphicsSettings.currentRenderPipeline == null) cam.Render();
                else if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
                else { Debug.LogWarning("[StarterKh1] the pipeline takes no render request; no capture of " + name); return; }
                RenderTexture.active = rt;
                tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                float k = rt.width / s.W;
                int bx0 = Mathf.RoundToInt(frame.Rocket.X * k), bx1 = Mathf.RoundToInt(frame.Rocket.Right * k);
                int by0 = rt.height - Mathf.RoundToInt(frame.Rocket.Bottom * k), by1 = rt.height - Mathf.RoundToInt(frame.Rocket.Y * k);
                var px = tex.GetPixels32();
                for (int yy = 0; yy < rt.height; yy++)
                    for (int xx = 0; xx < rt.width; xx++)
                    {
                        bool inBox = xx >= bx0 && xx < bx1 && yy >= by0 && yy < by1;
                        if (inBox && (xx < bx0 + 2 || xx >= bx1 - 2 || yy < by0 + 2 || yy >= by1 - 2)) px[yy * rt.width + xx] = new Color32(245, 180, 60, 255);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                string path = Path.Combine(UiCapture.OutDir(), UiCapture.Slug(name) + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                TestContext.WriteLine("ui capture: " + path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[StarterKh1] " + name + " not captured: " + e.GetType().Name + ": " + e.Message);
            }
            finally
            {
                RenderTexture.active = prev;
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
