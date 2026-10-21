// Countdown in Cam: Below, drawn for real (TestFlight t455, T-0:06.7): the count card on an offscreen UIDocument
// over a Below render of a generated pad, at 335x720, 375x667, 390x844, 430x932, 471x1024 and 1280x800 (wide web).
// Trevor's shot showed the spare line ("2,080 m/s to spare for Tiny Orbit") between the status banner and the
// skip bar, and hoses beside the rocket with nothing holding them. Each size asserts, on the live tree:
//   - the spare line is shown, inside the card, on its own whole-pixel row, its text fits that row (Unity's own
//     text measure), and it intersects no button and not the status pill;
//   - every shown card child sits inside the card (the card grows to fit; nothing is clipped);
// and on the pad, for a rail pad and a tower pad:
//   - every umbilical's pad end is on structure (UmbilicalRig.StructureGap <= 0.6 m, PadGenerator.RailSkinGap);
//   - the pad renderer and the umbilical visuals are enabled and drew for the Below camera (PadRenderer.Ledger,
//     LodPolicy cull distance), and each anchor and its structure member are inside the Below camera's frustum.
// PNGs: countdown-below-<size>.png (rail pad) via UiCapture -> CaptureOut/ui, CI artifact "ui-captures".
using System;
using System.Collections.Generic;
using System.Reflection;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using Kestrel.Pad;
using Kestrel.Slice;
using Kestrel.Vehicle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class CountdownBelowDeviceTests
    {
        const string Spare = "2,080 m/s to spare for Tiny Orbit";
        const string Title = "On the count";
        const string Status = "Deluge";
        const float MaxStructureGapM = 0.6f;

        struct Size { public string Name; public float CssW, CssH, PanelW, PanelH, Top, Bottom, Side; public bool Wide; }

        static IEnumerable<Size> Sizes()
        {
            yield return new Size { Name = "335x720", CssW = 335f, CssH = 720f, PanelW = 335f, PanelH = 720f, Top = 24f, Bottom = 24f, Side = 24f };
            yield return new Size { Name = "375x667", CssW = 375f, CssH = 667f, PanelW = 375f, PanelH = 667f, Top = 20f };
            yield return new Size { Name = "390x844", CssW = 390f, CssH = 844f, PanelW = 390f, PanelH = 844f, Top = 47f, Bottom = 34f };
            yield return new Size { Name = "430x932", CssW = 430f, CssH = 932f, PanelW = 430f, PanelH = 932f, Top = 59f, Bottom = 34f };
            yield return new Size { Name = "471x1024", CssW = 471f, CssH = 1024f, PanelW = 471f, PanelH = 1024f };
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float s = WebLandscapeType.PanelScale(1280f, 800f, rw, rh, m);
            yield return new Size { Name = "1280x800", CssW = 1280f, CssH = 800f, PanelW = Mathf.Round(1280f / s), PanelH = Mathf.Round(800f / s), Wide = true };
        }

        readonly List<UnityEngine.Object> made = new List<UnityEngine.Object>();

        [TearDown]
        public void Clean()
        {
            for (int i = made.Count - 1; i >= 0; i--)
            {
                var o = made[i];
                if (o is RenderTexture rt) rt.Release();
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }
            made.Clear();
        }

        [Test]
        public void SpareLineHasItsOwnRowAndUmbilicalsHangFromStructureInBelow()
        {
            var problems = new List<string>();
            foreach (var size in Sizes())
            {
                var rail = Scene(22f, 0.6f, PadArchetype.Rail, size, problems);
                var tower = Scene(48f, 1.6f, PadArchetype.Tower, size, problems);
                Umbilicals(rail, size.Name + " rail", problems);
                Umbilicals(tower, size.Name + " tower", problems);
                using (var surface = DeviceSurface.OpenOffscreen(size.PanelW, size.PanelH, 2f))
                {
                    surface.Stage.style.backgroundColor = new Color(0.62f, 0.74f, 0.88f, 1f);
                    if (rail.Rt != null) surface.Stage.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rail.Rt));
                    var hud = Count(surface, size, out var cast, out var pulse, out var fit);
                    surface.Pump(4);
                    var png = UiCapture.Save(surface.Texture, "countdown-below-" + size.Name);
                    if (png != null) Assert.That(System.IO.File.Exists(png), Is.True, size.Name + " capture written");
                    SpareRow(hud, cast, pulse, fit, size.Name, problems);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // ---- the count card ------------------------------------------------------------------------------------

        /// <summary>The flight HUD as SliceApp has it on the count: hud-band, the open column, the tools row
        /// (FlightChrome inserts it before the webcast card) and the webcast card from Slice.uxml, dressed by
        /// CountdownChrome with the card width SliceApp.CountCardWidthPt reads (the card host's content box).</summary>
        VisualElement Count(DeviceSurface surface, Size size, out VisualElement cast, out Label pulse, out CountdownStackFit fit)
        {
            surface.Stage.Clear();
            var hud = new VisualElement { name = UiIds.ScreenFlight };
            hud.style.position = Position.Absolute;
            hud.style.left = 0; hud.style.top = 0;
            hud.style.width = size.PanelW; hud.style.height = size.PanelH;
            hud.style.flexDirection = FlexDirection.Column;
            hud.style.paddingTop = size.Top; hud.style.paddingBottom = size.Bottom;
            hud.style.paddingLeft = size.Side; hud.style.paddingRight = size.Side;
            var band = new VisualElement { name = "hud-band" };
            var top = new VisualElement();
            top.AddToClassList("hud-top");
            top.style.flexDirection = FlexDirection.Row;
            top.Add(new Label { name = "h-t", text = "T-0:06.7" });
            var tag = new Label { name = "h-stage", text = CountdownStack.TagText };
            top.Add(tag);
            band.Add(top);
            hud.Add(band);
            var open = new VisualElement { name = "test-open-column", pickingMode = PickingMode.Ignore };
            open.style.flexGrow = 1;
            hud.Add(open);
            cast = new VisualElement { name = "webcast" };
            cast.AddToClassList("webcast");
            var verdict = new Label { name = "h-verdict", text = Title };
            pulse = new Label { name = "h-pulse", text = Spare };
            var callout = new Label { name = "h-callout", text = Status };
            var seq = new VisualElement { name = "h-seq" };
            seq.style.display = DisplayStyle.None;
            var skip = new Button { name = "flight-skip-count", text = "Skip count" };
            cast.Add(verdict); cast.Add(pulse); cast.Add(callout); cast.Add(seq); cast.Add(skip);
            hud.Add(cast);
            surface.Stage.Add(hud);

            var go = new GameObject("kh-countdown-below-chrome") { hideFlags = HideFlags.HideAndDontSave };
            made.Add(go);
            var chrome = go.AddComponent<FlightChrome>();
            chrome.Bind(hud, _ => { }, () => { }, () => { }, _ => { }, FlightViewMode.Below);
            chrome.Layout(1f);
            surface.Pump(6);
            CountdownChrome.DressTag(tag, 1f);
            fit = CountdownChrome.Dress(cast, verdict, pulse, callout, skip, 1f, CardWidthPt(cast, size), true);
            surface.Pump(6);
            chrome.Layout(1f);
            // SliceApp re-dresses every count tick with the laid-out width.
            fit = CountdownChrome.Dress(cast, verdict, pulse, callout, skip, 1f, CardWidthPt(cast, size), true);
            surface.Pump(6);
            return hud;
        }

        static float CardWidthPt(VisualElement cast, Size size)
        {
            var host = cast.parent;
            float w = host != null ? host.contentRect.width : float.NaN;
            if (float.IsNaN(w) || w < 8f) w = size.PanelW - 2f * size.Side - 2f * FlightHudLayout.SideMarginPt;
            return w;
        }

        static void SpareRow(VisualElement hud, VisualElement cast, Label pulse, CountdownStackFit fit, string name, List<string> problems)
        {
            if (!Shown(cast)) { problems.Add(name + ": the count card is not shown"); return; }
            if (!Shown(pulse)) { problems.Add(name + ": the spare line is not shown"); return; }
            var card = cast.worldBound;
            var line = pulse.worldBound;
            if (!Inside(line, card)) problems.Add(name + ": spare line " + line + " leaves the card " + card);
            // Its own row: whole pixels, and Unity's text measure fits in it (no wrap into the row below).
            if (Mathf.Abs(line.height - Mathf.Round(line.height)) > 0.01f) problems.Add(name + ": spare row height " + line.height + " is not whole pixels");
            float want = fit.SpareLines * CountdownStack.LinePt(fit.SparePt);
            if (Mathf.Abs(line.height - Mathf.Round(want)) > 0.51f) problems.Add(name + ": spare row is " + line.height + " px, the fit reserved " + want);
            var text = pulse.MeasureTextSize(Spare, pulse.contentRect.width, VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined);
            if (text.y > pulse.contentRect.height + 0.5f)
                problems.Add(name + ": spare text needs " + text.y.ToString("0.0") + " px, its row is " + pulse.contentRect.height.ToString("0.0") + " (" + fit.SpareLines + " line(s) at " + fit.SparePt + " pt)");
            hud.Query<Button>().ForEach(b =>
            {
                if (Shown(b) && Overlaps(line, b.worldBound))
                    problems.Add(name + ": spare line " + line + " intersects button " + b.name + " \"" + b.text + "\" " + b.worldBound);
            });
            var pill = cast.Q(CountdownChrome.StatusName);
            if (Shown(pill) && Overlaps(line, pill.worldBound)) problems.Add(name + ": spare line " + line + " intersects the status pill " + pill.worldBound);
            // The card grows to fit: every shown row (and the row's pill and button) is inside it.
            foreach (var child in cast.Children())
                if (Shown(child) && !Inside(child.worldBound, card))
                    problems.Add(name + ": card child " + child.name + " " + child.worldBound + " is clipped by the card " + card);
            var row = cast.Q(CountdownChrome.RowName);
            if (row != null)
                foreach (var child in row.Children())
                    if (Shown(child) && !Inside(child.worldBound, card))
                        problems.Add(name + ": " + child.name + " " + child.worldBound + " is clipped by the card " + card);
            if (Shown(row) && line.yMax > row.worldBound.yMin + 0.5f) problems.Add(name + ": spare line " + line + " runs into the status row " + row.worldBound);
            var stage = hud.parent != null ? hud.parent.worldBound : hud.worldBound;
            if (!Inside(card, stage)) problems.Add(name + ": the card " + card + " leaves the panel " + stage);
        }

        // ---- the pad in Below ----------------------------------------------------------------------------------

        sealed class PadScene
        {
            public PadRenderer Pad; public Camera Cam; public RenderTexture Rt; public float Height;
        }

        /// <summary>A generated pad (PadTower.Apply, as SliceApp.BuildRocket), a stand-in vehicle and the deck
        /// plinth, with the Below camera placed from FlightViewPose exactly as SliceApp.DesiredFlightPose feeds it.</summary>
        PadScene Scene(float h, float r, PadArchetype type, Size size, List<string> problems)
        {
            var root = new GameObject("kh-countdown-below-" + type) { hideFlags = HideFlags.HideAndDontSave };
            made.Add(root);
            var vehicle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vehicle.name = "Vehicle";
            UnityEngine.Object.DestroyImmediate(vehicle.GetComponent<Collider>());
            vehicle.transform.SetParent(root.transform, false);
            vehicle.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            vehicle.transform.localScale = new Vector3(2f * r, h * 0.5f, 2f * r);
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deck.name = "Deck";
            UnityEngine.Object.DestroyImmediate(deck.GetComponent<Collider>());
            deck.transform.SetParent(root.transform, false);
            var tower = new GameObject("PadTower").transform;
            tower.SetParent(root.transform, false);

            // The plates SliceApp publishes for a two-stage liquid stack (TankShellUmbilicals per tank, vehicle frame),
            // as Pad/Tests/Core/PadAttachmentTests; PadTower.Apply turns them into the pad frame.
            var plates = new List<AttachPoint>();
            float lower = h * 0.55f, upper = h * 0.25f;
            var s0 = TankShellSpec.Default(r * 1.005f, lower, 11UL);
            var s1 = TankShellSpec.Default(r * 1.005f, upper, 29UL);
            plates.AddRange(TankShellUmbilicals.ForStage(s0, 0, 0, new Vec3(0, 1f, 0), true, true, true, ReleaseStyle.TailServiceMastDrop));
            plates.AddRange(TankShellUmbilicals.ForStage(s1, 1, 0, new Vec3(0, lower + 3f, 0), true, true, true, ReleaseStyle.SwingArmRetract));
            var pad = PadTower.Apply(tower, h, r, type, PadTower.DefaultSeed, plates);
            float deckR = PadGround.DeckRadius(pad != null ? pad.Layout : null);
            deck.transform.localScale = new Vector3(2f * deckR, -PadGround.GroundY * 0.5f, 2f * deckR);
            deck.transform.localPosition = new Vector3(0f, PadGround.GroundY * 0.5f, 0f);

            var camGo = new GameObject("BelowCamera");
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.nearClipPlane = 0.5f; cam.farClipPlane = 4000f;
            cam.aspect = size.PanelW / size.PanelH;
            var trench = pad != null && pad.Layout != null ? pad.Layout.Trench : null;
            var pose = FlightViewPose.Pose(new FlightPoseInput
            {
                Mode = FlightViewMode.Below,
                Vehicle = new PoseVec(0, 0, 0), Up = PoseVec.Up, Height = h, KeptBase = 0, Altitude = 0,
                GroundY = PadGround.GroundY, DeckY = 0, HasDeck = true, Pad = new PoseVec(0, PadGround.GroundY, 0),
                TrenchBearingDeg = trench != null ? trench.BearingDeg : double.NaN,
                BaseFovDeg = FlightJuice.BaseFov,
                PadYawDeg = pad != null ? pad.transform.eulerAngles.y : 0f,
                DeckRadius = deckR,
            });
            cam.fieldOfView = (float)pose.Fov;
            cam.transform.position = PadGround.ClampAboveGround(U(pose.Position), PadGround.GroundY, (float)pose.Clearance);
            cam.transform.LookAt(U(pose.LookAt), Vector3.up);

            var scene = new PadScene { Pad = pad, Cam = cam, Height = h };
            if (pad == null || pad.Layout == null) { problems.Add(size.Name + " " + type + ": PadTower.Apply built no pad"); return scene; }
            pad.ViewCamera = cam;
            // EditMode has no player loop: run the renderer's frame (matrices, umbilical sync, cull + draw, Ledger).
            var late = typeof(PadRenderer).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            late?.Invoke(pad, null);
            if (type == PadArchetype.Rail && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                var rt = new RenderTexture(Mathf.Max(8, (int)(size.PanelW * 2f)), Mathf.Max(8, (int)(size.PanelH * 2f)), 24, RenderTextureFormat.ARGB32);
                rt.Create();
                made.Add(rt);
                scene.Rt = rt;
                var req = new RenderPipeline.StandardRequest { destination = rt };
                if (GraphicsSettings.currentRenderPipeline == null) { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
                else if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
                else Debug.LogWarning("countdown-below: the pipeline takes no render request; the capture has the sky only");
            }
            return scene;
        }

        static void Umbilicals(PadScene s, string name, List<string> problems)
        {
            var pad = s.Pad;
            if (pad == null || pad.Layout == null) return;
            var L = pad.Layout;
            if (L.Links.Count == 0) { problems.Add(name + ": no umbilical links"); return; }
            if (!pad.isActiveAndEnabled) problems.Add(name + ": the pad renderer is disabled");
            if (pad.Ledger.DrawCalls <= 0) problems.Add(name + ": the pad drew nothing for the Below camera (" + pad.Ledger.Report(pad.Quality) + ")");
            if (pad.Umbilicals.Count != L.Links.Count) problems.Add(name + ": " + pad.Umbilicals.Count + " umbilical visuals for " + L.Links.Count + " links");
            foreach (var u in pad.Umbilicals)
                if (u == null || !u.isActiveAndEnabled) problems.Add(name + ": umbilical visual " + (u != null ? u.AttachId : "null") + " is disabled");
            var cam = s.Cam;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            float cull = LodPolicy.CullDistance(pad.Quality, UmbilicalVisual.PadCull);
            var t = pad.transform;
            foreach (var link in L.Links)
            {
                var end = UmbilicalRig.PadEnd(link);
                float gap = UmbilicalRig.StructureGap(L, end, out string on);
                if (!(gap <= MaxStructureGapM))
                    problems.Add(name + ": umbilical " + link.AttachId + " hangs " + gap.ToString("0.00") + " m from " + (on ?? "nothing") + " (max " + MaxStructureGapM + ")");
                var anchor = t.TransformPoint(U(end));
                if (!GeometryUtility.TestPlanesAABB(planes, new Bounds(anchor, Vector3.one * 0.5f)))
                    problems.Add(name + ": umbilical " + link.AttachId + " anchor " + anchor + " is outside the Below camera's frustum (cam " + cam.transform.position + ")");
                if (Vector3.Distance(anchor, cam.transform.position) > cull)
                    problems.Add(name + ": umbilical " + link.AttachId + " anchor is past the pad cull distance " + cull + " m");
                if (Find(L, on, out var a, out var b))
                {
                    var box = new Bounds(t.TransformPoint(U(a)), Vector3.one * 0.5f);
                    box.Encapsulate(t.TransformPoint(U(b)));
                    if (!GeometryUtility.TestPlanesAABB(planes, box))
                        problems.Add(name + ": umbilical " + link.AttachId + " structure " + on + " " + box + " is outside the Below camera's frustum");
                }
                else problems.Add(name + ": umbilical " + link.AttachId + " structure \"" + on + "\" is not a pad member");
            }
        }

        static bool Find(PadLayout L, string id, out Vec3 a, out Vec3 b)
        {
            a = b = default;
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var m in L.Members) if (m.Id == id) { a = m.A; b = m.B; return true; }
            foreach (var link in L.Links)
                if (link.Mast != null)
                    foreach (var m in link.Mast) if (m.Id == id) { a = m.A; b = m.B; return true; }
            return false;
        }

        static Vector3 U(PoseVec v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        static Vector3 U(Vec3 v) => new Vector3(v.X, v.Y, v.Z);

        // ---- helpers (as FlightCameraPickerDeviceTests) --------------------------------------------------------

        static bool Shown(VisualElement e)
        {
            if (e == null) return false;
            for (var v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None) return false;
            var r = e.worldBound;
            return r.width > 0.5f && r.height > 0.5f && !float.IsNaN(r.width);
        }

        static bool Overlaps(Rect a, Rect b) =>
            a.xMin < b.xMax - 0.5f && a.xMax > b.xMin + 0.5f && a.yMin < b.yMax - 0.5f && a.yMax > b.yMin + 0.5f;

        static bool Inside(Rect inner, Rect outer) =>
            inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

        sealed class DeviceSurface : IDisposable
        {
            GameObject go;
            PanelSettings settings;
            RenderTexture texture;
            public VisualElement Stage { get; private set; }
            public RenderTexture Texture => texture;

            public static DeviceSurface OpenOffscreen(float w, float h, float scale)
            {
                var surface = new DeviceSurface();
                surface.TryDocument(w, h, scale);
                surface.Pump(6);
                if (!surface.Sized(w, h)) Assert.Fail("offscreen panel did not take the device size " + w + "x" + h);
                return surface;
            }

            public void Pump(int passes)
            {
                for (int i = 0; i < passes; i++)
                {
                    if (Stage != null && Stage.panel != null)
                    {
                        var tree = Stage.panel.visualTree;
                        if (tree != null && Stage.resolvedStyle.width > 2f)
                        {
                            tree.style.width = Stage.resolvedStyle.width;
                            tree.style.height = Stage.resolvedStyle.height;
                        }
                    }
                    if (settings != null)
                    {
                        var apply = typeof(PanelSettings).GetMethod("ApplyPanelSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (apply != null) apply.Invoke(settings, null);
                    }
                    var utility = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                    var update = utility != null ? utility.GetMethod("UpdatePanels", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                    if (update != null && update.GetParameters().Length == 0) update.Invoke(null, null);
                    var panel = Stage != null ? Stage.panel : null;
                    if (panel != null)
                    {
                        var validate = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (validate != null && validate.GetParameters().Length == 0) validate.Invoke(panel, null);
                    }
                }
            }

            bool Sized(float w, float h)
            {
                if (Stage == null || Stage.panel == null) return false;
                var bound = Stage.worldBound;
                if (bound.width < 8f || bound.height < 8f) return false;
                float sx = bound.width / w, sy = bound.height / h;
                return sx >= 0.8f && sy >= 0.8f && sx <= 3.5f && sy <= 3.5f && Math.Abs(sx - sy) <= 0.15f;
            }

            void TryDocument(float w, float h, float scale)
            {
                try
                {
                    texture = new RenderTexture(Mathf.Max(8, (int)(w * scale)), Mathf.Max(8, (int)(h * scale)), 0, RenderTextureFormat.ARGB32);
                    texture.Create();
                    settings = ScriptableObject.CreateInstance<PanelSettings>();
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.scale = scale;
                    var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
                    if (theme != null) settings.themeStyleSheet = theme;
                    settings.targetTexture = texture;
                    go = new GameObject("kh-countdown-below-surface") { hideFlags = HideFlags.HideAndDontSave };
                    var doc = go.AddComponent<UIDocument>();
                    doc.panelSettings = settings;
                    var root = doc.rootVisualElement;
                    if (root == null) return;
                    var tree = root.panel != null ? root.panel.visualTree : null;
                    if (tree != null) { tree.style.width = w; tree.style.height = h; }
                    root.style.width = w; root.style.height = h;
                    Stage = new VisualElement { name = "stage", pickingMode = PickingMode.Ignore };
                    Stage.style.position = Position.Absolute;
                    Stage.style.left = 0f; Stage.style.top = 0f;
                    Stage.style.width = w; Stage.style.height = h;
                    Stage.style.overflow = Overflow.Hidden;
                    root.Add(Stage);
                }
                catch (Exception) { }
            }

            public void Dispose()
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
                if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
            }
        }
    }
}
