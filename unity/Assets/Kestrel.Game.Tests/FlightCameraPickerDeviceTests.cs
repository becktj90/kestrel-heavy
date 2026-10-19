// In-flight camera picker with real UI Toolkit rects (DeviceSurface, as WebDesignerOverlapDeviceTests).
// 335x720 portrait and landscape web at 1280x720, 1280x800, 1366x768 and 1920x1080 (a 1280x720 panel after
// WebPanelScale, laid out from the CSS viewport the way AscentHud reads it). The "Cam: Auto" button is a
// 44 pt target that covers no tools, band, health or dock element; the open picker has five hit-testable
// chips with no text overflow; a chip applies and saves the view; C (CycleWithToast) cycles with a toast;
// and the choice survives a new Bind. Each size is also captured as a real PNG, picker closed and open
// (UiCapture -> CaptureOut/ui, CI artifact "ui-captures").
using Kestrel.Core.Ui;
using System;
using System.Collections.Generic;
using System.Reflection;
using Kestrel.Core.Flight;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class FlightCameraPickerDeviceTests
    {
        struct Size { public string Name; public float CssW, CssH, PanelW, PanelH; public bool Wide; }

        static IEnumerable<Size> Sizes()
        {
            yield return new Size { Name = "335x720 portrait", CssW = 335f, CssH = 720f, PanelW = 335f, PanelH = 720f };
            foreach (var (w, h) in new[] { (1280, 720), (1280, 800), (1366, 768), (1920, 1080) })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                yield return new Size { Name = w + "x" + h, CssW = w, CssH = h, PanelW = Mathf.Round(w / s), PanelH = Mathf.Round(h / s), Wide = true };
            }
        }

        FlightViewMode before;
        readonly List<GameObject> made = new List<GameObject>();

        [SetUp]
        public void Remember() => before = FlightCameraStore.Load();

        [TearDown]
        public void Restore()
        {
            AscentHud.ForceWide = null;
            AscentHud.ForceCssW = AscentHud.ForceCssH = 0f;
            FlightCameraStore.Save(before);
            foreach (var go in made) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            made.Clear();
        }

        [Test]
        public void CameraButtonIsATargetAndCoversNothing()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.Open(size.PanelW, size.PanelH))
            {
                var (hud, chrome, ascent) = Flight(surface, size, FlightViewMode.Auto);
                var cam = hud.Q<Button>(UiIds.FlightCamera);
                Assert.That(cam, Is.Not.Null, size.Name);
                Assert.That(cam.text, Is.EqualTo("Cam: Auto"), size.Name);
                Assert.That(cam.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), size.Name);
                float unit = size.PanelW / size.CssW;
                var r = cam.worldBound;
                Assert.That(r.width, Is.GreaterThanOrEqualTo(44f * unit - 0.5f), size.Name + " width");
                Assert.That(r.height, Is.GreaterThanOrEqualTo(44f * unit - 0.5f), size.Name + " height");
                Assert.That(r.x > 1f || r.y > 1f, Is.True, size.Name + " not left at the HUD origin");
                foreach (var other in HudElements(hud))
                {
                    if (other == cam || !Shown(other)) continue;
                    Assert.That(Overlaps(r, other.worldBound), Is.False, size.Name + " Cam covers " + other.name + " " + other.worldBound + " vs " + r);
                }
                Assert.That(TextFit.Problems(cam), Is.Empty, size.Name + " caption fits");
                // Nav-harness names on the real elements.
                Assert.That(hud.name, Is.EqualTo(UiIds.ScreenFlight), size.Name);
                Assert.That(hud.Q(UiIds.ScreenPause), Is.Not.Null, size.Name);
                foreach (var id in UiIds.FlightControls)
                {
                    if (id == UiIds.FlightExit || id == UiIds.FlightSkipCount) continue;   // Slice.uxml, not built here
                    Assert.That(hud.Q<Button>(id), Is.Not.Null, size.Name + " has " + id);
                }
                ascent.Close();
            }
        }

        [Test]
        public void PickerOpensWithFiveHitTestableChipsAndAChipAppliesAndSaves()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.Open(size.PanelW, size.PanelH))
            {
                var (hud, chrome, ascent) = Flight(surface, size, FlightViewMode.Auto);
                Assert.That(chrome.PickerOpen, Is.False, size.Name + " closed by default");
                Click(hud.Q<Button>(UiIds.FlightCamera));
                surface.Pump(4);
                Assert.That(chrome.PickerOpen, Is.True, size.Name + " tap opens the picker");
                Assert.That(chrome.Paused, Is.False, size.Name + " flight is not paused");
                var picker = hud.Q(FlightChrome.PickerName);
                var chips = picker.Query<Button>(className: "flight-camera-chip").ToList();
                Assert.That(chips.Count, Is.EqualTo(5), size.Name);
                foreach (var chip in chips)
                {
                    var b = chip.worldBound;
                    var hit = hud.panel.Pick(b.center);
                    bool onChip = false;
                    for (var ve = hit; ve != null; ve = ve.parent) if (ve == chip) onChip = true;
                    Assert.That(onChip, Is.True, size.Name + " " + chip.name + " is hit-testable (got " + (hit != null ? hit.name : "null") + ")");
                }
                Assert.That(TextFit.Problems(picker), Is.Empty, size.Name + " chips fit");
                var amber = picker.Q<Button>(FlightChrome.ChipName(FlightViewMode.Auto)).resolvedStyle.backgroundColor;
                Assert.That(amber.r, Is.GreaterThan(0.9f), size.Name + " current chip is amber");
                foreach (var other in HudElements(hud))
                {
                    if (!Shown(other) || other.name == UiIds.FlightCamera) continue;
                    Assert.That(Overlaps(picker.worldBound, other.worldBound), Is.False, size.Name + " picker covers " + other.name);
                }

                Click(picker.Q<Button>(FlightChrome.ChipName(FlightViewMode.Above)));
                surface.Pump(2);
                Assert.That(chrome.View, Is.EqualTo(FlightViewMode.Above), size.Name);
                Assert.That(chrome.PickerOpen, Is.False, size.Name + " choosing closes");
                Assert.That(FlightCameraStore.Load(), Is.EqualTo(FlightViewMode.Above), size.Name + " saved");
                Assert.That(hud.Q<Button>(UiIds.FlightCamera).text, Is.EqualTo("Cam: Above"), size.Name);

                chrome.OpenPicker();
                chrome.ClosePicker();
                Assert.That(chrome.View, Is.EqualTo(FlightViewMode.Above), size.Name + " closing changes nothing");
                ascent.Close();
            }
        }

        [Test]
        public void CCyclesWithAToastAndTheChoicePersistsAcrossBind()
        {
            var size = new Size { Name = "1280x720", CssW = 1280f, CssH = 720f, PanelW = 1280f, PanelH = 720f, Wide = true };
            using (var surface = DeviceSurface.Open(size.PanelW, size.PanelH))
            {
                var (hud, chrome, ascent) = Flight(surface, size, FlightViewMode.Auto);
                chrome.CycleWithToast();
                Assert.That(chrome.View, Is.EqualTo(FlightView.Next(FlightViewMode.Auto)));
                Assert.That(chrome.PickerOpen, Is.False, "C does not open the picker");
                Assert.That(chrome.ToastText, Is.EqualTo("Camera: " + FlightView.Name(chrome.View)));
                chrome.TickToast(FlightView.ToastSec + 0.05f);
                Assert.That(chrome.ToastText, Is.Null, "the toast is gone after a second");
                var picked = chrome.View;
                Assert.That(FlightCameraStore.Load(), Is.EqualTo(picked));
                ascent.Close();

                var (_, again, ascent2) = Flight(surface, size, FlightCameraStore.Load());
                Assert.That(again.View, Is.EqualTo(picked), "a new Bind starts on the saved view");
                again.Show(true);
                again.CycleWithToast();
                Assert.That(again.View, Is.EqualTo(picked), "C is ignored while the pause menu is open");
                ascent2.Close();
            }
        }

        [Test]
        public void FollowVisiblyChangesTheFraming()
        {
            // Live build (F5): Follow was a silent recenter. Now an orbit moves the camera off the chase and
            // Follow brings it back; from the fixed Pad view Follow returns to the Auto chase with a toast;
            // and while already following the button reads "Following".
            var chase = new Vector3(-6f, 3f, -14f);
            var rig = FlightCameraRig.Begin(FlightCameraRig.Following(), new Vector2(400f, 300f));
            rig = FlightCameraRig.Drag(rig, new Vector2(520f, 260f));
            Assert.That(rig.Follow, Is.False);
            Assert.That(Vector3.Distance(FlightCameraRig.Orbit(chase, rig), chase), Is.GreaterThan(1f), "an orbit moves the camera");
            rig = FlightCameraRig.Recenter(rig);
            Assert.That(FlightCameraRig.Orbit(chase, rig), Is.EqualTo(chase), "Follow hands the view back");

            var size = new Size { Name = "1280x800", CssW = 1280f, CssH = 800f, PanelW = 1280f, PanelH = 800f, Wide = true };
            using (var surface = DeviceSurface.Open(size.PanelW, size.PanelH))
            {
                var (hud, chrome, ascent) = Flight(surface, size, FlightViewMode.Pad);
                var follow = hud.Q<Button>(UiIds.FlightFollow);
                Assert.That(follow, Is.Not.Null);
                Assert.That(chrome.Following, Is.False, "Pad is a fixed ground camera");
                Assert.That(follow.text, Is.EqualTo(FlightView.FollowLabel));
                chrome.FollowRocket();
                Assert.That(chrome.View, Is.EqualTo(FlightViewMode.Auto), "Follow leaves Pad for the chase");
                Assert.That(chrome.ToastText, Is.EqualTo("Camera: Auto"));
                Assert.That(chrome.Following, Is.True);
                Assert.That(follow.text, Is.EqualTo(FlightView.FollowingLabel));
                Assert.That(FlightCameraStore.Load(), Is.EqualTo(FlightViewMode.Auto), "the change is saved like a picker choice");
                chrome.Choose(FlightViewMode.Chase);
                chrome.FollowRocket();
                Assert.That(chrome.View, Is.EqualTo(FlightViewMode.Chase), "a chasing view is kept");
                Assert.That(follow.text, Is.EqualTo(FlightView.FollowingLabel));
                ascent.Close();
            }
        }

        [Test]
        public void CapturesEverySizeWithThePickerClosedAndOpen()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.OpenOffscreen(size.PanelW, size.PanelH, 2f))
            {
                // Dusk sky behind the HUD so the PNG reads like the flight, not a black void.
                surface.Stage.style.backgroundColor = new Color(0.16f, 0.24f, 0.38f, 1f);
                var (hud, chrome, ascent) = Flight(surface, size, FlightViewMode.Auto);
                string slug = "flight-picker-" + size.Name.Replace(" ", "-");
                surface.Pump(4);
                var closed = UiCapture.Save(surface.Texture, slug + "-closed");
                chrome.OpenPicker();
                surface.Pump(6);
                Assert.That(chrome.PickerOpen, Is.True, size.Name);
                var open = UiCapture.Save(surface.Texture, slug + "-open");
                Assert.That(System.IO.File.Exists(closed) && System.IO.File.Exists(open), Is.True, size.Name);
                ascent.Close();
            }
        }

        (VisualElement hud, FlightChrome chrome, AscentHud ascent) Flight(DeviceSurface surface, Size size, FlightViewMode initial)
        {
            surface.Stage.Clear();
            var hud = new VisualElement { name = UiIds.ScreenFlight };
            hud.style.position = Position.Absolute;
            hud.style.left = 0; hud.style.top = 0;
            hud.style.width = size.PanelW;
            hud.style.height = size.PanelH;
            hud.style.flexDirection = FlexDirection.Column;
            var band = new VisualElement { name = "hud-band" };
            var top = new VisualElement();
            top.AddToClassList("hud-top");
            top.style.flexDirection = FlexDirection.Row;
            top.Add(new Label { name = "h-t", text = "T+ 12.4" });
            top.Add(new Label { name = "h-stage", text = "S1" });
            band.Add(top);
            hud.Add(band);
            // The live HUD keeps the tools row low, just above the dock spacer: a growing gap stands in for
            // the open rocket column, and the webcast block is empty.
            var open = new VisualElement { name = "test-open-column", pickingMode = PickingMode.Ignore };
            open.style.flexGrow = 1;
            hud.Add(open);
            var cast = new VisualElement { name = "webcast" };
            hud.Add(cast);
            surface.Stage.Add(hud);

            var go = new GameObject("kh-camera-picker-device") { hideFlags = HideFlags.HideAndDontSave };
            made.Add(go);
            var chrome = go.AddComponent<FlightChrome>();
            chrome.Bind(hud, _ => { }, () => { }, () => { }, FlightCameraStore.Save, initial);
            chrome.Layout(1f);
            AscentHud.ForceWide = size.Wide;
            AscentHud.ForceCssW = size.CssW;
            AscentHud.ForceCssH = size.CssH;
            var ascent = new AscentHud();
            ascent.Open(hud, 1f);
            surface.Pump(6);
            chrome.Layout(1f);
            ascent.Layout(1f);
            surface.Pump(6);
            return (hud, chrome, ascent);
        }

        static IEnumerable<VisualElement> HudElements(VisualElement hud)
        {
            var named = new[] { UiIds.FlightFollow, UiIds.FlightPause, "h-t", "h-stage", "ascent-health", "ascent-telemetry", "ascent-actions", "ascent-plot", "ascent-stages" };
            foreach (var n in named)
            {
                var e = hud.Q(n);
                if (e != null) yield return e;
            }
            var health = hud.Q("ascent-health");
            if (health != null)
                foreach (var b in health.Query<Button>().ToList()) yield return b;
            var dock = hud.Q("ascent-actions");
            if (dock != null)
                foreach (var b in dock.Query<Button>().ToList()) yield return b;
        }

        static bool Shown(VisualElement e)
        {
            for (var v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None) return false;
            var r = e.worldBound;
            return r.width > 0.5f && r.height > 0.5f && !float.IsNaN(r.width);
        }

        static bool Overlaps(Rect a, Rect b) =>
            a.xMin < b.xMax - 0.5f && a.xMax > b.xMin + 0.5f && a.yMin < b.yMax - 0.5f && a.yMax > b.yMin + 0.5f;

        static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null);
            using (var ev = NavigationSubmitEvent.GetPooled())
            {
                ev.target = button;
                button.SendEvent(ev);
            }
        }

        sealed class DeviceSurface : IDisposable
        {
            EditorWindow window;
            GameObject go;
            PanelSettings settings;
            RenderTexture texture;
            public VisualElement Stage { get; private set; }
            /// <summary>The offscreen target (OpenOffscreen only), for UiCapture.</summary>
            public RenderTexture Texture => texture;

            /// <summary>An offscreen UIDocument drawing into a RenderTexture at scale x the panel size.</summary>
            public static DeviceSurface OpenOffscreen(float w, float h, float scale)
            {
                var surface = new DeviceSurface();
                surface.TryDocument(w, h, scale);
                surface.Pump(6);
                if (!surface.Sized(w, h))
                    Assert.Fail("offscreen panel did not take the device size " + w + "x" + h);
                return surface;
            }

            public static DeviceSurface Open(float w, float h)
            {
                var surface = new DeviceSurface();
                surface.TryWindow(w, h);
                surface.Pump(4);
                if (!surface.Sized(w, h))
                {
                    surface.CloseWindow();
                    surface.TryDocument(w, h);
                    surface.Pump(6);
                }
                if (!surface.Sized(w, h))
                    Assert.Fail("panel did not take the device size " + w + "x" + h);
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
                    if (window != null) window.Repaint();
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

            void TryWindow(float w, float h)
            {
                try
                {
                    window = ScriptableObject.CreateInstance<CameraPickerDeviceWindow>();
                    window.position = new Rect(0f, 0f, w, h);
                    window.minSize = new Vector2(w, h);
                    window.maxSize = new Vector2(w, h);
                    window.ShowUtility();
                    var root = window.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception) { }
            }

            void TryDocument(float w, float h, float scale = 1f)
            {
                try
                {
                    texture = new RenderTexture(Mathf.Max(8, (int)(w * scale)), Mathf.Max(8, (int)(h * scale)), 0, RenderTextureFormat.ARGB32);
                    texture.Create();
                    settings = ScriptableObject.CreateInstance<PanelSettings>();
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.scale = scale;
                    // The runtime theme gives the panel its default font, so the capture shows the words.
                    var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
                    if (theme != null) settings.themeStyleSheet = theme;
                    settings.targetTexture = texture;
                    go = new GameObject("kh-camera-picker-surface");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    var doc = go.AddComponent<UIDocument>();
                    doc.panelSettings = settings;
                    var root = doc.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception) { }
            }

            static VisualElement MakeStage(VisualElement panelRoot, float w, float h)
            {
                var tree = panelRoot.panel != null ? panelRoot.panel.visualTree : null;
                if (tree != null)
                {
                    tree.style.width = w;
                    tree.style.height = h;
                }
                panelRoot.style.width = w;
                panelRoot.style.height = h;
                var stage = new VisualElement { name = "stage", pickingMode = PickingMode.Ignore };
                stage.style.position = Position.Absolute;
                stage.style.left = 0f;
                stage.style.top = 0f;
                stage.style.width = w;
                stage.style.height = h;
                stage.style.overflow = Overflow.Hidden;
                panelRoot.Add(stage);
                return stage;
            }

            void CloseWindow()
            {
                if (window == null) return;
                window.Close();
                window = null;
                Stage = null;
            }

            public void Dispose()
            {
                CloseWindow();
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        sealed class CameraPickerDeviceWindow : EditorWindow { }
    }
}
