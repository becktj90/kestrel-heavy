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
        readonly List<Camera> reEnable = new List<Camera>();
        (AmbientMode mode, Color light)? savedAmbient;

        [TearDown]
        public void Clean()
        {
            foreach (var c in reEnable) if (c != null) c.enabled = true;
            reEnable.Clear();
            if (savedAmbient.HasValue) { RenderSettings.ambientMode = savedAmbient.Value.mode; RenderSettings.ambientLight = savedAmbient.Value.light; savedAmbient = null; }
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
            sun.type = LightType.Directional; sun.intensity = 1.0f;
            sun.transform.rotation = Quaternion.Euler(38f, 150f, 0f);
            // A calm flat ambient (restored in Clean) so concrete and ground keep their tone instead of clipping to white.
            savedAmbient = (RenderSettings.ambientMode, RenderSettings.ambientLight);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.46f, 0.50f, 0.56f, 1f);
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

        // ---- Wide / top / circle / base frames of one tower pad (deterministic poses, both a landscape and a portrait size) ----
        //
        // The frames show the four pad features the launch-pad polish adds, so a reviewer can SEE them, not only trust
        // the harness: the tower top (crown, ring, spike), the base plates under the four legs, the painted pad circle
        // on the deck, and the apron seam (where the apron terrace meets the plinth). The feature positions are derived
        // from the generated layout here (tower params and TrenchGrammar.SurfaceRadius), not from any one PR's geometry
        // API, so this test compiles and passes on a branch without the polish: the frames then simply show the bare
        // tower, plinth and ground at those places. The constants below mirror the polish in TowerLayout.BuildDetail
        // (plate 2.2 m x 0.10 m up) and TrenchMeshBuilder.PadCircle / Apron (circle 0.4 x detail radius at y 0.13;
        // apron 8 m wide, top at y -0.80, seam at the surface radius).
        const float CircleFactor = 0.4f, CircleY = 0.13f, ApronWidth = 8f, ApronTopY = -0.80f, PlateHalf = 1.1f, PlateDepth = 0.45f, PlateY = 0.10f;
        const float FrameMargin = 0.06f;

        [TestCase(1280, 800)]
        [TestCase(430, 932)]
        public void TowerPadFramesTowerTopBasePlatesPadCircleAndApronSeam(int w, int h)
        {
            const float stackH = 48f, stackR = 1.6f;
            string size = w + "x" + h;
            var root = new GameObject("kh-pad-tower-wide-" + size) { hideFlags = HideFlags.HideAndDontSave };
            made.Add(root);
            var vehicle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(vehicle.GetComponent<Collider>());
            vehicle.transform.SetParent(root.transform, false);
            vehicle.transform.localPosition = new Vector3(0f, stackH * 0.5f, 0f);
            vehicle.transform.localScale = new Vector3(2f * stackR, stackH * 0.5f, 2f * stackR);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root.transform, false);
            sun.type = LightType.Directional; sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(38f, 150f, 0f);
            var towerRoot = new GameObject("PadTower").transform;
            towerRoot.SetParent(root.transform, false);

            var plates = new List<AttachPoint>();
            float lower = stackH * 0.55f, upper = stackH * 0.25f;
            plates.AddRange(TankShellUmbilicals.ForStage(TankShellSpec.Default(stackR * 1.005f, lower, 11UL), 0, 0, new Vec3(0, 1f, 0), true, true, true, ReleaseStyle.TailServiceMastDrop));
            plates.AddRange(TankShellUmbilicals.ForStage(TankShellSpec.Default(stackR * 1.005f, upper, 29UL), 1, 0, new Vec3(0, lower + 3f, 0), true, true, true, ReleaseStyle.SwingArmRetract));
            var pad = PadTower.Apply(towerRoot, stackH, stackR, PadArchetype.Tower, PadTower.DefaultSeed, plates);
            Assert.That(pad, Is.Not.Null, "PadTower.Apply built no pad");
            var layout = pad.Layout;
            var tp = layout.Tower;
            Assert.That(tp, Is.Not.Null, "no service tower beside the stack");
            var detailRenderer = pad.GetComponent<PadDetailRenderer>();
            Assert.That(detailRenderer, Is.Not.Null, "the pad has no deck/trench detail renderer");
            Assert.That(detailRenderer.MeshFor(MaterialRole.Concrete), Is.Not.Null, "no concrete deck mesh to frame");

            // Ground under the pad: a flat quad a hair below the grade the apron wall stands on, so the terrace edge reads.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(0f, TrenchSpec.GradeY - 0.03f, 0f);
            // Cut the trench opening and flame ponds out of it exactly as PadDetailLink does in the game
            // (SiteView.SetGroundOpenings). An uncut quad sits above the channel floor and fills the trench with
            // flat ground, which read as dark wedges where the channel and apron should show.
            ground.transform.localRotation = Quaternion.identity;
            ground.transform.localScale = Vector3.one;
            // Pad-local polygons go through the pad transform into the ground's local space (PadDetailLink.ToMetres).
            IList<Vec2> ToGround(IList<Vec2> poly)
            {
                var o = new List<Vec2>(poly.Count);
                foreach (var p in poly)
                {
                    var l = ground.transform.InverseTransformPoint(pad.transform.TransformPoint(new Vector3(p.X, 0f, p.Y)));
                    o.Add(new Vec2(l.x, l.z));
                }
                return o;
            }
            var groundHoles = new List<IList<Vec2>>();
            if (layout.Trench != null)
            {
                groundHoles.Add(ToGround(TrenchGrammar.GroundOpening(layout.Trench)));
                foreach (var pond in TrenchGrammar.PondPolys(layout.Trench)) groundHoles.Add(ToGround(pond));
            }
            var groundMesh = PadMeshUpload.Upload(PolyCut.Frame(450f, groundHoles, 0f), "KH_CaptureGroundCut");
            Assert.That(groundMesh, Is.Not.Null, "ground cut mesh");
            made.Add(groundMesh);
            ground.GetComponent<MeshFilter>().sharedMesh = groundMesh;
            var gr = ground.GetComponent<Renderer>();
            var gm = new Material(gr.sharedMaterial) { color = PadGround.GroundColor * 0.55f };
            if (gm.HasProperty("_Smoothness")) gm.SetFloat("_Smoothness", 0f);
            if (gm.HasProperty("_Glossiness")) gm.SetFloat("_Glossiness", 0f);
            made.Add(gm);
            gr.sharedMaterial = gm;

            // Feature points, pad-local then world. Wide frames hold all four; close frames hold their own feature.
            var padT = pad.transform;
            float surface = Mathf.Max(8f, TrenchGrammar.SurfaceRadius(layout));
            float detail = Mathf.Min(surface, TrenchMeshBuilder.DetailRadiusCap);
            float circleR = detail * CircleFactor;
            float tw = tp.Width * 0.5f, topY = tp.Height + tp.CapHeight;
            var top = new List<Vector3>();
            var basePlates = new List<Vector3>();
            var circle = new List<Vector3>();
            var seam = new List<Vector3>();
            var apron = new List<Vector3>();
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    top.Add(padT.TransformPoint(new Vector3(sx * tw, tp.Height, tp.StandOff + sz * tw)));
                    foreach (float dx in new[] { -PlateHalf, PlateHalf })
                        foreach (float dz in new[] { -PlateDepth, PlateDepth })
                            basePlates.Add(padT.TransformPoint(new Vector3(sx * tw + dx, PlateY, tp.StandOff + sz * tw + dz)));
                }
            top.Add(padT.TransformPoint(new Vector3(0f, topY, tp.StandOff)));
            if (tp.SpikeHeight > 0f) top.Add(padT.TransformPoint(new Vector3(0f, topY + tp.SpikeHeight, tp.StandOff)));
            foreach (var m in layout.Members)   // a pad built with the base-plate polish: frame the real plate ends too
                if (m.Id != null && m.Id.StartsWith("tower.base.plate"))
                { basePlates.Add(padT.TransformPoint(new Vector3(m.A.X, m.A.Y, m.A.Z))); basePlates.Add(padT.TransformPoint(new Vector3(m.B.X, m.B.Y, m.B.Z))); }
            const int ring = 24;
            for (int i = 0; i < ring; i++)
            {
                float a = 2f * Mathf.PI * i / ring, c = Mathf.Cos(a), s = Mathf.Sin(a);
                circle.Add(padT.TransformPoint(new Vector3(circleR * c, CircleY, circleR * s)));
                seam.Add(padT.TransformPoint(new Vector3(surface * c, ApronTopY, surface * s)));
                apron.Add(padT.TransformPoint(new Vector3((surface + ApronWidth) * c, ApronTopY, (surface + ApronWidth) * s)));
            }

            // Fixed poses. Bearings are pad-local (0 = toward the tower, clockwise toward +X): the design camera's 208 deg
            // for the wide and circle frames, and the tower's open west side (270) for the two close frames, so the
            // stack never stands between the lens and the tower. Elevations are fixed; only the distance is solved.
            float bearingDesign = PadGenerator.DefaultViewBearingDeg, bearingSide = 270f;

            var camGo = new GameObject("PadTowerCaptureCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false; cam.fieldOfView = 40f; cam.aspect = (float)w / h; cam.nearClipPlane = 0.3f; cam.farClipPlane = 4000f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.74f, 0.88f, 1f);
            // PadDetailRenderer draws for Camera.main only: park any other main camera for the length of the test.
            foreach (var other in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != cam && other.CompareTag("MainCamera") && other.enabled) { other.enabled = false; reEnable.Add(other); }

            pad.ViewCamera = cam;
            pad.QualityOverride = KitBudgets.Medium;
            Assert.That(pad.Quality, Is.EqualTo(KitBudgets.Medium));

            // The seam and apron are rings 50+ m out on a big pad: frame the arc on the lens side only (+/- 75 deg in
            // landscape, +/- 40 in portrait, where the width is the limit), and +/- 35 deg for the circle close-up.
            float wideArc = w >= h ? 75f : 40f, circleArc = 35f;
            var seamWide = Arc(seam, padT, bearingDesign, wideArc);
            var apronWide = Arc(apron, padT, bearingDesign, wideArc);
            var seamNear = Arc(seam, padT, bearingDesign, circleArc);
            var circleFrame = Join(circle, seamNear, Arc(apron, padT, bearingDesign, circleArc));
            var wide = Join(top, basePlates, circle, seamWide, apronWide);
            var frames = new (string name, float bearing, float elev, List<Vector3> pts, (string, List<Vector3>)[] must)[]
            {
                ("wide", bearingDesign, 30f, wide, new[] { ("tower top", top), ("base plates", basePlates), ("pad circle", circle), ("apron seam (lens-side arc)", seamWide), ("apron edge (lens-side arc)", apronWide) }),
                ("top", bearingSide, 8f, top, new[] { ("tower top", top) }),
                ("circle", bearingDesign, 35f, circleFrame, new[] { ("pad circle", circle), ("apron seam (near arc)", seamNear) }),
                ("base", bearingSide, 50f, Join(basePlates, TowerFootprint(padT, tp.StandOff, tw)), new[] { ("base plates", basePlates) }),
            };
            foreach (var f in frames)
            {
                Aim(cam, padT, f.bearing, f.elev, f.pts);
                foreach (var (label, pts) in f.must)
                    foreach (var p in pts)
                    {
                        var v = cam.WorldToViewportPoint(p);
                        Assert.That(v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f, Is.True,
                            size + " " + f.name + ": " + label + " point " + p + " is outside the frustum at " + v);
                    }
                Render(cam, pad, detailRenderer, w, h, "pad-tower-" + f.name + "-" + size);
            }
        }

        static List<Vector3> Join(params List<Vector3>[] lists)
        {
            var all = new List<Vector3>();
            foreach (var l in lists) all.AddRange(l);
            return all;
        }

        /// <summary>The tower footprint at deck level, so the base close-up shows the legs standing on the plates.</summary>
        static List<Vector3> TowerFootprint(Transform padT, float standOff, float half)
        {
            var l = new List<Vector3>();
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    l.Add(padT.TransformPoint(new Vector3(sx * (half + 1.6f), 0f, standOff + sz * (half + 1.6f))));
            return l;
        }

        /// <summary>The ring points on the lens side: within arcDeg of the camera bearing (pad-local, 0 = +Z, clockwise toward +X).</summary>
        static List<Vector3> Arc(List<Vector3> ring, Transform padT, float bearingDeg, float arcDeg)
        {
            float b = bearingDeg * Mathf.Deg2Rad;
            var toCam = new Vector2(Mathf.Sin(b), Mathf.Cos(b));
            var near = new List<Vector3>();
            foreach (var p in ring)
            {
                var l = padT.InverseTransformPoint(p);
                var d = new Vector2(l.x, l.z).normalized;
                if (Vector2.Dot(d, toCam) >= Mathf.Cos(arcDeg * Mathf.Deg2Rad)) near.Add(p);
            }
            return near;
        }

        /// <summary>Fixed bearing and elevation about the bounds centre of the points; the distance is the smallest (bisected,
        /// 40 steps) at which every point sits inside the viewport with FrameMargin to spare. Deterministic, no flyover.</summary>
        static void Aim(Camera cam, Transform padT, float bearingDeg, float elevDeg, List<Vector3> pts)
        {
            Assert.That(pts.Count, Is.GreaterThan(0), "no points to frame");
            var bmin = pts[0]; var bmax = pts[0];
            foreach (var p in pts) { bmin = Vector3.Min(bmin, p); bmax = Vector3.Max(bmax, p); }
            var centre = (bmin + bmax) * 0.5f;
            float b = bearingDeg * Mathf.Deg2Rad, e = elevDeg * Mathf.Deg2Rad;
            var horiz = padT.TransformDirection(new Vector3(Mathf.Sin(b), 0f, Mathf.Cos(b))).normalized;
            var dir = (horiz * Mathf.Cos(e) + Vector3.up * Mathf.Sin(e)).normalized;
            bool Fits(float d)
            {
                cam.transform.position = centre + dir * d;
                cam.transform.LookAt(centre, Vector3.up);
                foreach (var p in pts)
                {
                    var v = cam.WorldToViewportPoint(p);
                    if (v.z <= cam.nearClipPlane || v.x < FrameMargin || v.x > 1f - FrameMargin || v.y < FrameMargin || v.y > 1f - FrameMargin) return false;
                }
                return true;
            }
            float lo = 1f, hi = 900f;
            Assert.That(Fits(hi), Is.True, "the points cannot be framed within 900 m");
            for (int i = 0; i < 40; i++) { float mid = (lo + hi) * 0.5f; if (Fits(mid)) hi = mid; else lo = mid; }
            Fits(hi);
        }

        void Render(Camera cam, PadRenderer pad, PadDetailRenderer detail, int w, int h, string name)
        {
            // The pad draws in LateUpdate, which EditMode never runs: call it for this pose, then render.
            const BindingFlags any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            cam.enabled = true;   // PadDetailRenderer resolves Camera.main
            typeof(PadRenderer).GetMethod("LateUpdate", any)?.Invoke(pad, null);
            typeof(PadDetailRenderer).GetMethod("LateUpdate", any)?.Invoke(detail, null);
            cam.enabled = false;
            Assert.That(pad.Ledger.DrawCalls, Is.GreaterThan(0), name + ": the pad drew nothing for this camera");

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            made.Add(rt);
            var req = new RenderPipeline.StandardRequest { destination = rt };
            if (GraphicsSettings.currentRenderPipeline == null) { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
            else if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else { Debug.LogWarning("pad-tower: the pipeline takes no render request; no capture of " + name); return; }

            // Not blank: more than 1% of the pixels differ from the top-left pixel, and not all of them do.
            var prev = RenderTexture.active;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            made.Add(tex);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            var px = tex.GetPixels32();
            var c0 = px[(h - 1) * w];
            int diff = 0, n = 0;
            for (int i = 0; i < px.Length; i += 5)
            {
                n++;
                var c = px[i];
                if (Mathf.Abs(c.r - c0.r) + Mathf.Abs(c.g - c0.g) + Mathf.Abs(c.b - c0.b) > 12) diff++;
            }
            Assert.That(diff, Is.GreaterThan(n / 100), name + ": the frame is blank (" + diff + " of " + n + " sampled pixels differ from the corner)");
            var png = UiCapture.Save(rt, name);
            Assert.That(png, Is.Not.Null, name + ": no PNG written");
            Assert.That(System.IO.File.Exists(png), Is.True, name + " capture written");
        }
    }
}
