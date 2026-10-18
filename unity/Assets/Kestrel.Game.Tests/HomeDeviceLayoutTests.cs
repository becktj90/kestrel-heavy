// Device layout for dusk Home, the mission board and the patch collection.
// The iPhone shot (build 151) showed only the rocket, the audio toggles and the gear:
// the opaque dusk plate was added after the safe column, so it painted over the controls.
// Picking still hit the buttons (the plate ignores pointers), so these tests also require
// paint order and a non-zero rect inside the viewport.
// KESTREL_REPO_ROOT points at an empty directory, the same device stand-in as #254, and
// fonts are read with Resources.Load only.
using System;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class HomeDeviceLayoutTests
    {
        const string Tokens = "Assets/Kestrel.Game/Shell/UI/KH_Tokens.uss";

        struct Device
        {
            public string Name;
            public float W, H, Top, Bottom;
            public bool Wide;
            public Device(string name, float w, float h, float top, float bottom, bool wide)
            { Name = name; W = w; H = h; Top = top; Bottom = bottom; Wide = wide; }
        }

        // Points, safe insets from the mix tests. Landscape SE has no notch. Web is the wide frames.
        static readonly Device[] Devices =
        {
            new Device("SE", 375f, 667f, 20f, 0f, false),
            new Device("ProMax", 440f, 956f, 62f, 34f, false),
            new Device("iPad", 1032f, 1376f, 24f, 20f, false),
            new Device("SE-landscape", 667f, 375f, 0f, 0f, false),
            new Device("web-landscape", 1280f, 720f, 0f, 0f, true),
        };

        string _prev;
        string _away;

        [SetUp]
        public void UnavailableCheckout()
        {
            _away = Path.Combine(Path.GetTempPath(), "kh-norepo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_away);
            _prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _away);
            MainMenuOverlay.ForceWide = null;
        }

        [TearDown]
        public void RestoreCheckout()
        {
            MainMenuOverlay.ForceWide = null;
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _prev);
            try { if (!string.IsNullOrEmpty(_away) && Directory.Exists(_away)) Directory.Delete(_away, true); }
            catch (Exception) { }
        }

        [Test]
        public void UnavailableCheckoutDoesNotWalkUpToTheRepo()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null, "a dummy KESTREL_REPO_ROOT must not walk to the CI checkout");
            Assert.That(ScreenStub.RepoRoot(), Is.Null);
            Assert.DoesNotThrow(() => ScreenStub.LoadCampaign());
        }

        [Test]
        public void HomeChromeIsOnScreenAboveTheDuskPlate()
        {
            var black = Resources.Load<Font>("KHFonts/Fraunces-Black");
            var grotesk = Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
            Assert.That(black, Is.Not.Null, "Fraunces-Black from Resources");
            Assert.That(grotesk, Is.Not.Null, "SpaceGrotesk-Medium from Resources");

            foreach (var d in Devices)
            {
                MainMenuOverlay.ForceWide = d.Wide;
                using (var surface = DeviceSurface.Open(d.W, d.H))
                {
                    var shell = MountHome(surface.Stage, d.Top, d.Bottom);
                    surface.Pump(8);
                    AssertHome(surface, shell, d, black, grotesk);
                }
            }
        }

        [Test]
        public void ShortColumnReleasesCapsWhenTheViewportGrows()
        {
            // A short phone column pins max-height (and may clip the title). Growing into a
            // reference portrait used to leave those caps in place, so LAUNCH stayed compressed
            // and the wordmark stayed clipped. Same elements, no rebuild.
            MainMenuOverlay.ForceWide = false;
            using (var surface = DeviceSurface.Open(640f, 260f))
            {
                var shell = MountHome(surface.Stage, 0f, 0f);
                surface.Pump(8);
                var root = shell.Q(MainMenuOverlay.RootName);
                var launch = root.Q<Button>("home-launch");
                var title = root.Q("mm-title");
                Assert.That(launch, Is.Not.Null);
                Assert.That(title, Is.Not.Null);
                Assert.That(ColumnCapped(root), Is.True,
                    "short column did not contain " + CapDescribe(root));

                surface.Resize(390f, 844f);
                float w = shell.resolvedStyle.width;
                float h = shell.resolvedStyle.height;
                Assert.That(h, Is.GreaterThan(700f), "portrait column did not grow " + w + "x" + h);
                var z = MenuTypeScale.Compute(w, h, 0f, 0f);
                var word = root.Q<Label>("mm-kestrel");
                var cap = launch.resolvedStyle.maxHeight;
                float capPx = cap.keyword == StyleKeyword.Undefined ? cap.value : float.NaN;
                Assert.That(float.IsNaN(capPx) || capPx >= z.LaunchH - 1f,
                    "LAUNCH max-height " + cap.keyword + " " + cap.value + " after grow");
                Assert.That(launch.resolvedStyle.height, Is.GreaterThan(MenuTypeScale.HitMin + 8f),
                    "LAUNCH stayed at the short-column size");
                Assert.That(title.style.height.keyword, Is.Not.EqualTo(StyleKeyword.Undefined), CapDescribe(root));
                Assert.That(title.style.maxHeight.keyword, Is.Not.EqualTo(StyleKeyword.Undefined), CapDescribe(root));
                Assert.That(word.resolvedStyle.fontSize, Is.EqualTo(z.Title).Within(1.5f),
                    "title font stayed contained");
                Inside(surface.Stage, launch, "grown LAUNCH");
                Inside(surface.Stage, word, "grown title");
                Receives(shell.panel, launch, "grown LAUNCH");
            }
        }

        static bool ColumnCapped(VisualElement root)
        {
            if (InlineMax(root.Q("mm-stats")) || InlineMax(root.Q("home-launch")) || InlineMax(root.Q("home-design")))
                return true;
            if (InlineMax(root.Q("mm-title"))) return true;
            var title = root.Q("mm-title");
            if (title != null && title.style.height.keyword == StyleKeyword.Undefined) return true;
            foreach (var it in MainMenuCopy.Tiles)
                if (InlineMax(root.Q("home-" + it.Id))) return true;
            return false;
        }

        static bool InlineMax(VisualElement el)
        {
            return el != null && el.style.maxHeight.keyword == StyleKeyword.Undefined;
        }

        static string CapDescribe(VisualElement root)
        {
            string One(string name)
            {
                var el = root.Q(name);
                if (el == null) return name + " missing";
                return name + " max " + el.style.maxHeight.keyword + " h " + el.resolvedStyle.height;
            }
            return One("home-launch") + "; " + One("mm-title") + "; " + One("mm-stats");
        }

        [Test]
        public void MissionBoardAndPatchesStayOnScreenWithoutTheCheckout()
        {
            foreach (var d in Devices)
            {
                if (d.Wide) continue;
                using (var surface = DeviceSurface.Open(d.W, d.H))
                {
                    var shell = MountBoard(surface.Stage, d.Top, d.Bottom);
                    surface.Pump(8);
                    AssertBoard(surface, shell, d.Name);
                }
                using (var surface = DeviceSurface.Open(d.W, d.H))
                {
                    var shell = MountPatches(surface.Stage, d.Top, d.Bottom);
                    surface.Pump(6);
                    AssertPatches(surface, shell, d.Name);
                }
            }
        }

        static VisualElement MountHome(VisualElement stage, float top, float bottom)
        {
            var shell = Column(stage, top, bottom, out var host);
            Assert.DoesNotThrow(() =>
                MainMenuOverlay.Build(shell, host, () => { }, () => { }, () => { }, true));
            return shell;
        }

        static VisualElement MountBoard(VisualElement stage, float top, float bottom)
        {
            var shell = Column(stage, top, bottom, out var host);
            MissionBoardView board = null;
            Assert.DoesNotThrow(() =>
            {
                board = MissionBoardScreen.Build(
                    ScreenStub.LoadCampaign(),
                    _ => { }, _ => { }, () => { }, () => { },
                    true, _ => false);
            });
            Assert.That(board, Is.Not.Null);
            ScreenStub.FillHost(board);
            host.Add(board);
            return shell;
        }

        static VisualElement MountPatches(VisualElement stage, float top, float bottom)
        {
            var shell = Column(stage, top, bottom, out var host);
            VisualElement patches = null;
            Assert.DoesNotThrow(() => patches = PatchCollectionScreen.Build(null, null, null, () => { }));
            Assert.That(patches, Is.Not.Null);
            host.Add(patches);
            return shell;
        }

        static VisualElement Column(VisualElement stage, float top, float bottom, out VisualElement host)
        {
            var shell = new VisualElement { name = "shell", pickingMode = PickingMode.Ignore };
            shell.AddToClassList("kh-root");
            shell.style.position = Position.Absolute;
            shell.style.left = 0;
            shell.style.right = 0;
            shell.style.top = 0;
            shell.style.bottom = 0;
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Tokens);
            Assert.That(sheet, Is.Not.Null, Tokens);
            shell.styleSheets.Add(sheet);
            stage.Add(shell);

            var safe = new VisualElement { name = "safe", pickingMode = PickingMode.Ignore };
            safe.AddToClassList("kh-safe");
            safe.style.position = Position.Absolute;
            safe.style.left = 0;
            safe.style.right = 0;
            safe.style.top = top;
            safe.style.bottom = bottom;
            shell.Add(safe);

            host = new VisualElement { name = "home-host", pickingMode = PickingMode.Ignore };
            host.AddToClassList("kh-screen");
            host.AddToClassList("kh-home");
            ScreenStub.FillHost(host);
            safe.Add(host);
            return shell;
        }

        static void AssertHome(DeviceSurface surface, VisualElement shell, Device d, Font black, Font grotesk)
        {
            var stage = surface.Stage;
            var root = shell.Q(MainMenuOverlay.RootName);
            Assert.That(root, Is.Not.Null, d.Name);
            var title = root.Q<Label>("mm-kestrel");
            var launch = root.Q<Button>("home-launch");
            var design = root.Q<Button>("home-design");
            var missions = root.Q<Button>("home-missions");
            var stats = root.Q("mm-stats");
            var thrum = root.Q("mm-hint");
            var sky = shell.Q("mm-dusk");
            var plate = shell.Q("mm-full");
            var gear = shell.Q<Button>("home-settings");
            Assert.That(title, Is.Not.Null, d.Name + " title");
            Assert.That(launch, Is.Not.Null, d.Name + " LAUNCH");
            Assert.That(design, Is.Not.Null, d.Name + " Design first");
            Assert.That(missions, Is.Not.Null, d.Name + " Missions");
            Assert.That(sky, Is.Not.Null, d.Name + " dusk");
            Assert.That(plate, Is.Not.Null, d.Name);
            Assert.That(title.text, Is.EqualTo(MainMenuCopy.TitleTop), d.Name);
            Assert.That(launch.Q<Label>(className: "kh-mm-launch-label").text, Is.EqualTo(MainMenuCopy.Launch.Label), d.Name);
            Assert.That(design.Q<Label>(className: "kh-mm-design-label").text, Is.EqualTo(MainMenuCopy.Design.Label), d.Name);
            Assert.That(missions.Q<Label>(className: "kh-mm-tile-name").text, Does.Contain("Mission").IgnoreCase, d.Name);
            Assert.That(title.style.unityFontDefinition.value.font, Is.SameAs(black), d.Name + " title font is the Resources face");
            Assert.That(root.Q<Label>("mm-eyebrow").style.unityFontDefinition.value.font, Is.SameAs(grotesk), d.Name);

            // The plate really is full-bleed. A zero-size sky would make "above the sky" vacuous.
            var sb = sky.worldBound;
            Assert.That(sb.width, Is.GreaterThan(stage.worldBound.width * 0.9f), d.Name + " sky width " + sb);
            Assert.That(sb.height, Is.GreaterThan(stage.worldBound.height * 0.9f), d.Name + " sky height " + sb);

            foreach (var el in new VisualElement[] { title, launch, design, missions, stats, thrum, gear })
            {
                Assert.That(el, Is.Not.Null, d.Name);
                Inside(stage, el, d.Name + " " + el.name);
                Assert.That(Shown(el), Is.GreaterThan(0.5f), d.Name + " " + el.name + " opacity");
                Assert.That(PaintsAbove(el, sky), Is.True, d.Name + " " + el.name + " is under the rocket");
                Assert.That(PaintsAbove(el, plate), Is.True, d.Name + " " + el.name + " is under mm-full");
            }
            Receives(shell.panel, launch, d.Name + " LAUNCH");
            Receives(shell.panel, design, d.Name + " Design first");
            Receives(shell.panel, missions, d.Name + " Missions");
            Receives(shell.panel, gear, d.Name + " gear");
        }

        static void AssertBoard(DeviceSurface surface, VisualElement shell, string name)
        {
            var board = shell.Q(MissionBoardView.RootName);
            var sky = shell.Q("mission-sky");
            var home = shell.Q<Button>("mission-home");
            var primary = shell.Q<Button>("mission-primary");
            Assert.That(board, Is.Not.Null, name);
            Assert.That(sky, Is.Not.Null, name);
            Assert.That(home, Is.Not.Null, name);
            Assert.That(primary, Is.Not.Null, name);
            Inside(surface.Stage, home, name + " home");
            Inside(surface.Stage, primary, name + " primary");
            Assert.That(Shown(home), Is.GreaterThan(0.5f), name);
            Assert.That(Shown(primary), Is.GreaterThan(0.5f), name);
            Assert.That(PaintsAbove(home, sky), Is.True, name + " home under the sky");
            Assert.That(PaintsAbove(primary, sky), Is.True, name + " primary under the sky");
            Receives(shell.panel, home, name + " home");
            Assert.That(primary.pickingMode, Is.Not.EqualTo(PickingMode.Ignore), name);
            var hit = shell.panel.Pick(primary.worldBound.center);
            Assert.That(Reaches(hit, primary), Is.True, name + " primary pick " + HitName(hit));

            var node = shell.Query<Button>().ToList().Find(b => b.name != null && b.name.StartsWith("mission-node-"));
            if (node == null) return;
            Assert.That(node.worldBound.width, Is.GreaterThan(1f), name + " node");
            Assert.That(node.worldBound.height, Is.GreaterThan(1f), name + " node");
            Assert.That(PaintsAbove(node, sky), Is.True, name + " node under the sky");
            Assert.That(Overlaps(surface.Stage, node), Is.True, name + " first node off screen " + node.worldBound);
        }

        static void AssertPatches(DeviceSurface surface, VisualElement shell, string name)
        {
            var title = shell.Q<Label>("patches-title");
            var back = shell.Q<Button>("patches-back");
            var patch = shell.Q("patch-kh-1");
            Assert.That(title, Is.Not.Null, name);
            Assert.That(back, Is.Not.Null, name);
            Assert.That(patch, Is.Not.Null, name);
            Inside(surface.Stage, title, name + " patches title");
            Inside(surface.Stage, back, name + " patches back");
            Assert.That(patch.worldBound.width, Is.GreaterThan(1f), name + " patch");
            Assert.That(patch.worldBound.height, Is.GreaterThan(1f), name + " patch");
            Assert.That(Overlaps(surface.Stage, patch), Is.True, name + " patch off screen");
            Assert.That(Shown(title), Is.GreaterThan(0.5f), name);
            Assert.That(Shown(back), Is.GreaterThan(0.5f), name);
            Receives(shell.panel, back, name + " patches back");
        }

        static void Inside(VisualElement viewport, VisualElement el, string what)
        {
            var v = viewport.worldBound;
            var b = el.worldBound;
            Assert.That(b.width, Is.GreaterThan(1f), what + " width " + b + " viewport " + v);
            Assert.That(b.height, Is.GreaterThan(1f), what + " height " + b);
            Assert.That(b.xMin, Is.GreaterThanOrEqualTo(v.xMin - 1f), what + " left " + b + " vs " + v);
            Assert.That(b.yMin, Is.GreaterThanOrEqualTo(v.yMin - 1f), what + " top " + b + " vs " + v);
            Assert.That(b.xMax, Is.LessThanOrEqualTo(v.xMax + 1f), what + " right " + b + " vs " + v);
            Assert.That(b.yMax, Is.LessThanOrEqualTo(v.yMax + 1f), what + " bottom " + b + " vs " + v);
        }

        static bool Overlaps(VisualElement viewport, VisualElement el)
        {
            var v = viewport.worldBound;
            var b = el.worldBound;
            return b.xMax > v.xMin + 1f && b.xMin < v.xMax - 1f && b.yMax > v.yMin + 1f && b.yMin < v.yMax - 1f;
        }

        static float Shown(VisualElement el)
        {
            float o = 1f;
            for (var e = el; e != null; e = e.parent)
            {
                float p = e.resolvedStyle.opacity;
                if (!float.IsNaN(p)) o *= p;
            }
            return o;
        }

        /// <summary>Later siblings paint on top. A descendant paints above its ancestor's background.</summary>
        static bool PaintsAbove(VisualElement front, VisualElement back)
        {
            if (front == null || back == null) return false;
            var a = Chain(front);
            var b = Chain(back);
            int n = Math.Min(a.Count, b.Count);
            for (int i = 0; i < n; i++)
            {
                if (a[i] == b[i]) continue;
                var parent = a[i].parent;
                if (parent == null || b[i].parent != parent) return false;
                return parent.IndexOf(a[i]) > parent.IndexOf(b[i]);
            }
            return a.Count > b.Count;
        }

        static System.Collections.Generic.List<VisualElement> Chain(VisualElement el)
        {
            var list = new System.Collections.Generic.List<VisualElement>();
            for (var e = el; e != null; e = e.parent) list.Add(e);
            list.Reverse();
            return list;
        }

        static void Receives(IPanel panel, VisualElement target, string what)
        {
            Assert.That(panel, Is.Not.Null, what);
            Assert.That(target.pickingMode, Is.Not.EqualTo(PickingMode.Ignore), what);
            Assert.That(target.enabledInHierarchy, Is.True, what);
            var hit = panel.Pick(target.worldBound.center);
            Assert.That(Reaches(hit, target), Is.True, what + " pick hit " + HitName(hit) + " at " + target.worldBound);
        }

        static bool Reaches(VisualElement hit, VisualElement target)
        {
            for (var e = hit; e != null; e = e.parent)
                if (e == target) return true;
            return false;
        }

        static string HitName(VisualElement hit) => hit == null ? "nothing" : (string.IsNullOrEmpty(hit.name) ? hit.GetType().Name : hit.name);

        /// <summary>A sized UI Toolkit panel. EditorWindow first, then a render-texture document.</summary>
        sealed class DeviceSurface : IDisposable
        {
            EditorWindow _window;
            GameObject _go;
            PanelSettings _settings;
            RenderTexture _rt;
            public VisualElement Stage { get; private set; }
            public string How = "none";

            public static DeviceSurface Open(float w, float h)
            {
                var s = new DeviceSurface();
                s.TryWindow(w, h);
                s.Pump(4);
                if (!s.Sized(w, h))
                {
                    s.CloseWindow();
                    s.TryDocument(w, h);
                    s.Pump(6);
                }
                if (!s.Sized(w, h))
                    Assert.Fail("panel did not take the device size " + w + "x" + h + " (" + s.How + ") " + s.Diagnostic());
                return s;
            }

            public void Resize(float w, float h)
            {
                if (_window != null)
                {
                    _window.minSize = new Vector2(8f, 8f);
                    _window.maxSize = new Vector2(w, h);
                    _window.minSize = new Vector2(w, h);
                    _window.position = new Rect(0f, 0f, w, h);
                }
                if (_rt != null)
                {
                    _rt.Release();
                    UnityEngine.Object.DestroyImmediate(_rt);
                    _rt = new RenderTexture(Mathf.Max(8, (int)w), Mathf.Max(8, (int)h), 0, RenderTextureFormat.ARGB32);
                    _rt.Create();
                    if (_settings != null) _settings.targetTexture = _rt;
                    ApplySettings();
                }
                if (Stage != null)
                {
                    var tree = Stage.panel != null ? Stage.panel.visualTree : null;
                    if (tree != null)
                    {
                        tree.style.width = w;
                        tree.style.height = h;
                    }
                    var panelRoot = Stage.parent;
                    if (panelRoot != null)
                    {
                        panelRoot.style.width = w;
                        panelRoot.style.height = h;
                    }
                    Stage.style.width = w;
                    Stage.style.height = h;
                }
                Pump(8);
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
                    ApplySettings();
                    UpdatePanels();
                    if (_window != null) _window.Repaint();
                    Validate(Stage != null ? Stage.panel : null);
                }
            }

            bool Sized(float w, float h)
            {
                if (Stage == null || Stage.panel == null) return false;
                var b = Stage.worldBound;
                if (b.width < 8f || b.height < 8f) return false;
                float sx = b.width / w, sy = b.height / h;
                return sx >= 0.8f && sy >= 0.8f && sx <= 3.5f && sy <= 3.5f && Math.Abs(sx - sy) <= 0.15f;
            }

            void TryWindow(float w, float h)
            {
                try
                {
                    _window = ScriptableObject.CreateInstance<DeviceLayoutWindow>();
                    _window.position = new Rect(0f, 0f, w, h);
                    _window.minSize = new Vector2(w, h);
                    _window.maxSize = new Vector2(w, h);
                    _window.ShowUtility();
                    var root = _window.rootVisualElement;
                    if (root == null) { How = "window-no-root"; return; }
                    Stage = MakeStage(root, w, h);
                    How = "editor-window";
                }
                catch (Exception e) { How = "window-failed: " + e.GetType().Name; }
            }

            void TryDocument(float w, float h)
            {
                try
                {
                    _rt = new RenderTexture(Mathf.Max(8, (int)w), Mathf.Max(8, (int)h), 0, RenderTextureFormat.ARGB32);
                    _rt.Create();
                    _settings = ScriptableObject.CreateInstance<PanelSettings>();
                    _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    _settings.scale = 1f;
                    _settings.targetTexture = _rt;
                    var theme = Resources.Load<PanelSettings>("UI/KH_ShellPanelSettings");
                    if (theme != null && theme.themeStyleSheet != null) _settings.themeStyleSheet = theme.themeStyleSheet;
                    _go = new GameObject("kh-device-layout");
                    _go.hideFlags = HideFlags.HideAndDontSave;
                    var doc = _go.AddComponent<UIDocument>();
                    doc.panelSettings = _settings;
                    ApplySettings();
                    var root = doc.rootVisualElement;
                    if (root == null) { How += "+doc-no-root"; return; }
                    Stage = MakeStage(root, w, h);
                    How += "+uidocument";
                }
                catch (Exception e) { How += "+doc-failed: " + e.GetType().Name; }
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

            void ApplySettings()
            {
                if (_settings == null) return;
                var m = typeof(PanelSettings).GetMethod("ApplyPanelSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null) m.Invoke(_settings, null);
            }

            static void UpdatePanels()
            {
                var t = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                var m = t != null ? t.GetMethod("UpdatePanels", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                if (m != null && m.GetParameters().Length == 0) m.Invoke(null, null);
            }

            static void Validate(IPanel panel)
            {
                if (panel == null) return;
                var m = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null && m.GetParameters().Length == 0) m.Invoke(panel, null);
            }

            string Diagnostic()
            {
                var b = Stage != null ? Stage.worldBound : new Rect(0f, 0f, 0f, 0f);
                var panel = Stage != null ? Stage.panel : null;
                string names = "";
                if (panel != null)
                {
                    int n = 0;
                    foreach (var m in panel.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (m.Name.IndexOf("ayout", StringComparison.Ordinal) < 0 && m.Name.IndexOf("epaint", StringComparison.Ordinal) < 0) continue;
                        if (n++ > 12) break;
                        names += " " + m.Name;
                    }
                }
                return "bounds " + b + " panel " + (panel == null ? "null" : panel.GetType().FullName) + names;
            }

            void CloseWindow()
            {
                if (_window == null) return;
                _window.Close();
                _window = null;
                Stage = null;
            }

            public void Dispose()
            {
                CloseWindow();
                if (_go != null) { UnityEngine.Object.DestroyImmediate(_go); _go = null; }
                if (_settings != null) { UnityEngine.Object.DestroyImmediate(_settings); _settings = null; }
                if (_rt != null)
                {
                    _rt.Release();
                    UnityEngine.Object.DestroyImmediate(_rt);
                    _rt = null;
                }
            }
        }

        sealed class DeviceLayoutWindow : EditorWindow { }
    }
}
