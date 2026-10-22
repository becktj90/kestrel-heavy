// t461 (TestFlight 204 campaign map, ~471x1024, Trevor: "Improve rendering of payload. I can't scroll down to read
// about it either."). The live mission board in a real offscreen UI Toolkit panel (the FlightCameraPickerDeviceTests
// DeviceSurface pattern) at 335x720, 375x667, 430x932, 471x1024 portrait and a 1280x800 web window (its 1280x720-class
// panel after WebPanelScale), plus a 335x560 window short enough that the card body has to scroll:
//  - every card (each mission, Contracts, Sandbox): its text is not clipped, or the body ScrollView reaches all of
//    it (scroll range >= the overflow); UiTreeChecks finds no clipped text, overlap or element off screen;
//  - Contracts, Sandbox and the primary are visible, 44 pt, inside the safe area, under the body (pinned);
//  - the act title is not clipped and sits on whole-pixel line boxes;
//  - the header (kicker, act title, act bar, Home, Patches) is clear of every node's drawn bounds.
// Each size is captured (UiCapture -> CaptureOut/ui, CI artifact "ui-captures") as campaign-<size>.png, and every
// payload type's lit render as payload-<type>.png.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Game.MissionSelect;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class CampaignCardDeviceTests
    {
        struct Size { public string Name; public float W, H; public bool Wide; }

        static IEnumerable<Size> Sizes()
        {
            yield return new Size { Name = "335x720", W = 335f, H = 720f };
            yield return new Size { Name = "375x667", W = 375f, H = 667f };
            yield return new Size { Name = "430x932", W = 430f, H = 932f };
            yield return new Size { Name = "471x1024", W = 471f, H = 1024f };
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float s = WebLandscapeType.PanelScale(1280f, 800f, rw, rh, m);
            yield return new Size { Name = "1280x800", W = Mathf.Round(1280f / s), H = Mathf.Round(800f / s), Wide = true };
        }

        static readonly Size Short = new Size { Name = "335x560", W = 335f, H = 560f };

        Action _patchesBefore;

        [SetUp]
        public void Remember() => _patchesBefore = MissionBoardScreen.OpenPatches;

        [TearDown]
        public void Restore() => MissionBoardScreen.OpenPatches = _patchesBefore;

        static string Repo() => Catalog.FindRepoRoot(Directory.GetCurrentDirectory());

        static MissionBoardView Board(Size size, out List<string> missionIds)
        {
            string repo = Repo();
            var campaign = Campaign.Load(repo);
            string json = File.ReadAllText(Path.Combine(repo, "data", "campaign.json"));
            var career = Career.Start(campaign, MissionMapText.MinorPerKc(json));
            career.Cleared = 2;   // a played board: cleared, open and locked nodes, like t461 (KH-3 next)
            missionIds = new List<string>();
            foreach (var mission in campaign.Missions) missionIds.Add(mission.Id);
            var board = MissionBoardScreen.Build(
                campaign, career, new Dictionary<string, MissionRecord>(), MissionMapText.ReadPayloadLabels(json),
                _ => { }, _ => { }, () => { }, () => { },
                true, _ => true, size.W, size.H, size.Wide);
            board.style.position = Position.Absolute;
            board.style.left = 0f;
            board.style.top = 0f;
            board.style.width = size.W;
            board.style.height = size.H;
            return board;
        }

        [Test]
        public void CardTextFitsOrScrollsWithTheKeysPinnedAtEverySize()
        {
            var all = new List<Size>(Sizes()) { Short };
            bool sawScroll = false;
            foreach (var size in all)
            using (var surface = DeviceSurface.OpenOffscreen(size.W, size.H, 1f))
            {
                var board = Board(size, out var ids);
                surface.Stage.Add(board);
                surface.Pump(6);
                var screen = surface.Stage.worldBound;
                var cards = new List<(string name, Action select)>();
                foreach (var id in ids) { string m = id; cards.Add((m, () => board.SelectMission(m, false))); }
                cards.Add(("contracts", board.SelectContracts));
                cards.Add(("sandbox", board.SelectSandbox));
                foreach (var (cardName, select) in cards)
                {
                    select();
                    surface.Pump(3);
                    string where = size.Name + " " + cardName;
                    var scroll = board.Q<ScrollView>("mission-card-scroll");
                    Assert.That(scroll, Is.Not.Null, where);
                    float viewH = scroll.contentViewport.layout.height;
                    float contentH = scroll.contentContainer.layout.height;
                    float overflow = 0f;
                    foreach (var n in new[] { "mission-name", "mission-customer", "mission-orbit", "mission-brief", "mission-facts", "mission-art" })
                    {
                        var e = board.Q(n);
                        if (e == null || e.resolvedStyle.display == DisplayStyle.None) continue;
                        Assert.That(e.layout.yMax, Is.LessThanOrEqualTo(contentH + 0.6f), where + " " + n + " is past the scroll content");
                        overflow = Mathf.Max(overflow, e.layout.yMax - viewH);
                    }
                    if (overflow > 0.6f)
                    {
                        sawScroll = true;
                        float range = contentH - viewH;
                        Assert.That(range, Is.GreaterThanOrEqualTo(overflow - 0.6f), where + " scroll range " + range + " < overflow " + overflow);
                        // Scrolled to the end, the facts are inside the viewport.
                        scroll.scrollOffset = new Vector2(0f, range);
                        surface.Pump(2);
                        var facts = board.Q<Label>("mission-facts");
                        var vp = scroll.contentViewport.worldBound;
                        Assert.That(facts.worldBound.yMax, Is.LessThanOrEqualTo(vp.yMax + 1f), where + " facts reachable");
                        scroll.scrollOffset = Vector2.zero;
                        surface.Pump(2);
                    }
                    else
                    {
                        foreach (var n in new[] { "mission-name", "mission-customer", "mission-orbit", "mission-brief", "mission-facts" })
                        {
                            var t = board.Q<Label>(n);
                            if (t == null || !UiTreeChecks.Visible(t)) continue;
                            var ink = UiTreeChecks.TextRect(t);
                            var shown = UiTreeChecks.Clipped(t, ink);
                            Assert.That(shown.height, Is.GreaterThanOrEqualTo(ink.height - 1f), where + " " + n + " clipped " + shown + " of " + ink);
                        }
                    }
                    Assert.That(UiTreeChecks.TextBounds(board.Q("mission-card")), Is.Empty, where);

                    foreach (var key in new[] { "mission-contracts", "mission-sandbox", "mission-primary" })
                    {
                        var b = board.Q<Button>(key);
                        Assert.That(UiTreeChecks.Visible(b), Is.True, where + " " + key + " visible");
                        var r = b.worldBound;
                        Assert.That(r.height, Is.GreaterThanOrEqualTo(44f - 0.5f), where + " " + key);
                        Assert.That(r.yMin, Is.GreaterThanOrEqualTo(scroll.worldBound.yMax - 0.5f), where + " " + key + " under the body");
                        Assert.That(screen.Contains(new Vector2(r.xMin + 0.5f, r.yMin + 0.5f)) && screen.Contains(new Vector2(r.xMax - 0.5f, r.yMax - 0.5f)), Is.True,
                            where + " " + key + " " + r + " inside the safe area " + screen);
                    }
                }
                var problems = UiTreeChecks.Problems(board, screen, screen, size.Name);
                Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems));
            }
            Assert.That(sawScroll, Is.True, "the short window exercises the scrolling body");
        }

        [Test]
        public void ActTitleIsWholeAndTheHeaderClearsEveryNode()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.OpenOffscreen(size.W, size.H, 1f))
            {
                var board = Board(size, out var ids);
                surface.Stage.Add(board);
                surface.Pump(6);
                var frame = board.userData as MissionBoardFrame;
                Assert.That(frame, Is.Not.Null, size.Name);
                var act = board.Q<Label>("mission-act");
                Assert.That(UiTreeChecks.Visible(act), Is.True, size.Name);
                float lb = MissionBoardLayout.LineBox(frame.ActFontPt);
                Assert.That(act.layout.height, Is.EqualTo(frame.ActLines * lb).Within(0.6f), size.Name + " act line boxes");
                Assert.That(act.resolvedStyle.paddingTop + act.resolvedStyle.paddingBottom, Is.EqualTo(0f).Within(0.01f), size.Name + " act padding");
                var ink = UiTreeChecks.TextRect(act);
                var shown = UiTreeChecks.Clipped(act, ink);
                Assert.That(shown.height, Is.GreaterThanOrEqualTo(ink.height - 1f), size.Name + " act clipped " + shown + " of " + ink);
                Assert.That(UiTreeChecks.TextBounds(act), Is.Empty, size.Name);
                Assert.That(Kestrel.Game.Shell.TextFit.Problems(act), Is.Empty, size.Name);

                var header = new List<VisualElement> { board.Q("mission-home"), board.Q("mission-patches"), board.Q("mission-kicker"), act };
                for (int i = 0; i < MissionBoardLayout.ActSlots; i++) header.Add(board.Q("mission-progress-" + i));
                var patches = board.Q<Button>("mission-patches");
                Assert.That(UiTreeChecks.Visible(patches), Is.True, size.Name);
                Assert.That(patches.worldBound.yMax, Is.LessThanOrEqualTo(board.Q("mission-path").worldBound.yMin + 0.5f), size.Name + " Patches above the map");
                foreach (var id in ids)
                {
                    var node = board.Q<Button>("mission-node-" + id);
                    Assert.That(node, Is.Not.Null, id);
                    var drawn = UiTreeChecks.Clipped(node, node.worldBound);
                    if (drawn.width < 0.5f || drawn.height < 0.5f) continue;   // scrolled out of the map
                    foreach (var h in header)
                    {
                        if (h == null || !UiTreeChecks.Visible(h)) continue;
                        Assert.That(h.worldBound.Overlaps(drawn), Is.False, size.Name + " " + h.name + " " + h.worldBound + " on " + node.name + " " + drawn);
                    }
                }
                // Patches still opens the collection.
                int opened = 0;
                MissionBoardScreen.OpenPatches = () => opened++;
                MenuPlaytestPress.Press(patches);
                Assert.That(opened, Is.EqualTo(1), size.Name);
            }
        }

        [Test]
        public void CapturesEverySize()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.OpenOffscreen(size.W, size.H, 2f))
            {
                surface.Stage.style.backgroundColor = new Color(0.03f, 0.05f, 0.1f, 1f);
                var board = Board(size, out _);
                surface.Stage.Add(board);
                surface.Pump(6);
                board.SelectMission("m3", false);
                surface.Pump(2);
                PayloadThumbnails.Flush();
                surface.Pump(4);
                var art = board.Q<PayloadThumb>("mission-art-render");
                Assert.That(art, Is.Not.Null);
                Assert.That(art.Ready, Is.True, size.Name + " the lit render is on the card");
                float dpr = art.panel.scaledPixelsPerPoint;
                Assert.That(dpr, Is.GreaterThan(1.5f), size.Name + " a 2x panel");
                Assert.That(art.Px, Is.EqualTo(PayloadThumbnails.PixelsFor(art.layout.width, dpr)), size.Name + " rendered at physical pixels");
                Assert.That(art.Shown, Is.Not.Null);
                Assert.That(art.Shown.width, Is.EqualTo(art.Px), size.Name + " texture is the physical size");
                var path = UiCapture.Save(surface.Texture, "campaign-" + (int)(size.Wide ? 1280 : size.W) + "x" + (int)(size.Wide ? 800 : size.H));
                if (path != null) Assert.That(File.Exists(path), Is.True, size.Name);
            }
        }

        [Test]
        public void EveryPayloadTypeRendersALitRenderTextureAtItsPhysicalSize()
        {
            bool device = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            foreach (var form in PayloadRender.Types)
            {
                int px = PayloadThumbnails.PixelsFor(MissionBoardLayout.ArtMax, 3f);
                var tex = PayloadThumbnails.RenderNow(form, px, PayloadBackdrop.Dusk);
                Assert.That(tex, Is.Not.Null, form);
                Assert.That(tex.width, Is.EqualTo(px), form);
                Assert.That(tex.height, Is.EqualTo(px), form);
                if (!device) continue;
                var rt = tex as RenderTexture;
                Assert.That(rt, Is.Not.Null, form + " is a RenderTexture with a graphics device");
                Assert.That(rt.IsCreated(), Is.True, form);
                var rgba = Read(rt);
                double v = PayloadRender.LumaVariance(rgba);
                Assert.That(v, Is.GreaterThan(0.004), form + " luma variance " + v + " (flat or empty)");
                var path = UiCapture.Save(rt, "payload-" + form);
                if (path != null) Assert.That(File.Exists(path), Is.True, form);
            }
        }

        [Test]
        public void AThumbRendersOnceForItsFormAndSizeAndAgainOnlyWhenOneChanges()
        {
            using (var surface = DeviceSurface.OpenOffscreen(200f, 200f, 2f))
            {
                string a = PayloadRender.Types[0], b = PayloadRender.Types[1];
                var thumb = PayloadArt.Element("thumb-test", a, 61, PayloadBackdrop.Dusk, urgent: true);
                surface.Stage.Add(thumb);
                surface.Pump(4);
                PayloadThumbnails.Flush();
                surface.Pump(2);
                Assert.That(thumb.Ready, Is.True);
                float dpr = thumb.panel.scaledPixelsPerPoint;
                Assert.That(dpr, Is.GreaterThan(1.5f), "a 2x panel");
                Assert.That(thumb.Px, Is.EqualTo(PayloadThumbnails.PixelsFor(61f, dpr)), "61 pt x DPR, whole pixels");
                int renders = PayloadThumbnails.Renders;
                thumb.Refresh();
                surface.Pump(4);
                PayloadThumbnails.Flush();
                Assert.That(PayloadThumbnails.Renders, Is.EqualTo(renders), "same form and size: cached");
                thumb.SetForm(b, PayloadBackdrop.Dusk);
                PayloadThumbnails.Flush();
                surface.Pump(2);
                Assert.That(thumb.Ready, Is.True);
                Assert.That(thumb.Form, Is.EqualTo(b));
                Assert.That(PayloadThumbnails.Renders, Is.EqualTo(renders + 1), "a new form renders once");
                thumb.style.width = 80f;
                thumb.style.height = 80f;
                surface.Pump(4);
                PayloadThumbnails.Flush();
                surface.Pump(2);
                Assert.That(thumb.Px, Is.EqualTo(PayloadThumbnails.PixelsFor(80f, dpr)), "a new size renders at its physical pixels");
                Assert.That(PayloadThumbnails.Renders, Is.EqualTo(renders + 2));
                // Pump keeps to its budget: one call never runs a whole board's worth of renders.
                PayloadThumbnails.Get(PayloadRender.Types[2], 300, PayloadBackdrop.Dusk);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                PayloadThumbnails.Pump(2.0);
                Assert.That(clock.Elapsed.TotalMilliseconds, Is.LessThan(60.0), "time-sliced");
                PayloadThumbnails.Flush();
            }
        }

        static byte[] Read(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                var px = tex.GetPixels32();
                var rgba = new byte[px.Length * 4];
                for (int i = 0; i < px.Length; i++)
                {
                    rgba[i * 4] = px[i].r;
                    rgba[i * 4 + 1] = px[i].g;
                    rgba[i * 4 + 2] = px[i].b;
                    rgba[i * 4 + 3] = px[i].a;
                }
                return rgba;
            }
            finally
            {
                RenderTexture.active = prev;
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        /// <summary>An offscreen UIDocument drawing into a RenderTexture (FlightCameraPickerDeviceTests' surface).</summary>
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
                if (!surface.Sized(w, h))
                    Assert.Fail("offscreen panel did not take the device size " + w + "x" + h);
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
                    go = new GameObject("kh-campaign-card-surface");
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

            public void Dispose()
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }
    }
}
