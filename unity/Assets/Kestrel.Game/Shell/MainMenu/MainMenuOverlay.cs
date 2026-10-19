// UI Toolkit layer for the dusk Home screen. It sits over the live 3D hangar scene. The overlay paints the
// sky, ridges and hero rocket, and replaces the 3D title, launch bar and glass rows. Glass is a baked
// translucent fill (no realtime blur). Static classes + USS.
// Motion follows motion-spec.md where cheap: staggered intro by class toggle + USS transition, :active press,
// :focus ring. Reduce Motion: no intro, no transitions (root gets kh-mm-reduce).
using System;
using Kestrel.Core.Flight;
using Kestrel.Game.Audio;
using Kestrel.Game.Guide;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class MainMenuOverlay
    {
        public const string RootName = "mm-root";

        /// <summary>
        /// EditMode seam. Null follows <see cref="WebLayoutMode.UseWide(float, float)"/> (phone column
        /// in the editor and on iOS). True lays the landscape web frames out using the panel size, so a
        /// device test can exercise wide Home without a WebGL player.
        /// </summary>
        public static bool? ForceWide;

        /// <summary>Home record opens the patch collection. Null shows a short note instead.</summary>
        public static Action OpenPatches;

        /// <summary>Codex tile opens the reader. Null shows "Coming soon."</summary>
        public static Action OpenCodex;

        /// <summary>Builds the overlay. <paramref name="safe"/> holds the controls (safe-area inset);
        /// <paramref name="full"/> holds the full-bleed bars and grain. Returns the root parented in <paramref name="safe"/>.</summary>
        public static VisualElement Build(VisualElement full, VisualElement safe, Action launch, Action design, Action missions, bool reduceMotion, string missionLine = null)
        {
            MainMenuLayout.HeroLift = 0f;
            var root = new VisualElement { name = RootName, pickingMode = PickingMode.Ignore };
            root.AddToClassList("kh-mm");
            // Fill the host even when the stylesheet has not applied yet (device, or a panel test).
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.right = 0;
            root.style.top = 0;
            root.style.bottom = 0;
            root.style.flexDirection = FlexDirection.Column;
            if (reduceMotion)
            {
                root.AddToClassList("kh-mm-reduce");
                // The gear lives on the shell, outside mm-root, so the zero-duration rule has to see the class there too.
                if (full != null) full.AddToClassList("kh-mm-reduce");
            }

            // Full-bleed dusk, then a light grain. Reel bars stay in the tree for the mix tests; USS hides them.
            var dusk = new HomeDuskBackdrop();
            var grain = new VisualElement { name = "mm-grain", pickingMode = PickingMode.Ignore };
            grain.AddToClassList("kh-mm-full"); grain.AddToClassList("kh-home-grain");
            grain.style.backgroundImage = new StyleBackground(HomeBackdrop.GrainTexture());
            grain.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            grain.style.backgroundSize = new BackgroundSize(new Length(HomeArt.GrainSize), new Length(HomeArt.GrainSize));
            var top = new Reel { name = "mm-bar-top" }; top.AddToClassList("kh-mm-bar"); top.AddToClassList("kh-mm-bar-top");
            var bottom = new Reel { name = "mm-bar-bottom" }; bottom.AddToClassList("kh-mm-bar"); bottom.AddToClassList("kh-mm-bar-bottom");
            var layer = new VisualElement { name = "mm-full", pickingMode = PickingMode.Ignore };
            layer.AddToClassList("kh-mm-full");
            layer.Add(dusk); layer.Add(grain); layer.Add(top); layer.Add(bottom);
            // Behind the safe column. full.Add paints this opaque plate over LAUNCH and the title.
            PlaceSky(full, layer, safe);

            // Title card: eyebrow, gold wordmark with a hard under-shadow, italic HEAVY between brass rules.
            var title = new VisualElement { name = "mm-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("kh-mm-title-card"); title.AddToClassList("kh-mm-anim");
            var eye = new Label(MainMenuCopy.Eyebrow) { name = "mm-eyebrow", pickingMode = PickingMode.Ignore };
            eye.AddToClassList("kh-mm-eyebrow");
            var word = new VisualElement { pickingMode = PickingMode.Ignore }; word.AddToClassList("kh-mm-word");
            var shadow = new Label(MainMenuCopy.TitleTop) { name = "mm-kestrel-shadow", pickingMode = PickingMode.Ignore };
            shadow.AddToClassList("kh-mm-kestrel"); shadow.AddToClassList("kh-mm-kestrel-shadow");
            var kestrel = new Label(MainMenuCopy.TitleTop) { name = "mm-kestrel", pickingMode = PickingMode.Ignore };
            kestrel.AddToClassList("kh-mm-kestrel");
            word.Add(shadow); word.Add(kestrel);
            var rule = new VisualElement { pickingMode = PickingMode.Ignore }; rule.AddToClassList("kh-mm-rule-row");
            var l = new VisualElement { pickingMode = PickingMode.Ignore }; l.AddToClassList("kh-mm-rule");
            var heavy = new Label(MainMenuCopy.TitleBottom) { name = "mm-heavy", pickingMode = PickingMode.Ignore };
            heavy.AddToClassList("kh-mm-heavy");
            var r = new VisualElement { pickingMode = PickingMode.Ignore }; r.AddToClassList("kh-mm-rule");
            rule.Add(l); rule.Add(heavy); rule.Add(r);
            title.Add(eye); title.Add(word); title.Add(rule);
            root.Add(title);

            var spacer = new VisualElement { pickingMode = PickingMode.Ignore }; spacer.style.flexGrow = 1; root.Add(spacer);

            // Controls: tidy stats, the big LAUNCH, a smaller Design first, then Missions and Codex.
            var stats = StatsStrip(full);
            root.Add(stats);
            var launchKey = LaunchBar(launch, missionLine);
            root.Add(launchKey);
            var designKey = DesignBar(design);
            root.Add(designKey);
            var grid = new VisualElement { name = "mm-grid", pickingMode = PickingMode.Ignore };
            grid.AddToClassList("kh-mm-grid");
            grid.style.flexShrink = 0;
            foreach (var it in MainMenuCopy.Tiles)
            {
                Action act = null;
                if (it.Id == "missions") act = missions;
                else if (it.Id == "codex") act = () => OpenCodexOrSoon(full);
                grid.Add(Tile(it, act));
            }
            root.Add(grid);
            Gear(full);

            var hint = ThrumStrip(full);
            root.Add(hint);
            safe.Add(root);
            MainMenuFonts.Apply(root);

            // Bars follow the safe insets so the hint always sits inside the bottom bar.
            root.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float h = full.resolvedStyle.height;
                if (h <= 0f) return;
                float w = full.resolvedStyle.width;
                root.EnableInClassList("kh-mm-narrow", MainMenuCopy.IsNarrow(w));
                float topInset = Mathf.Max(0f, safe.worldBound.yMin - full.worldBound.yMin);
                float botInset = Mathf.Max(0f, full.worldBound.yMax - safe.worldBound.yMax);
                var z = MenuTypeScale.Compute(w, h, topInset, botInset);
                ApplyScale(root, z);
                bool wide = ForceWide.HasValue
                    ? ForceWide.Value && WebLayoutMode.IsLandscape(w, h)
                    : WebLayoutMode.UseWide(Screen.width, Screen.height);
                bool wasWide = root.ClassListContains("kh-mm-wide");
                ApplyWebWide(root, wide);
                if (!wide && wasWide)
                {
                    // Wide frames pin their own max-heights. Drop them and measure the phone
                    // column on the next geometry pass, once this layout has caught up.
                    ClearContainment(root);
                    ForgetColumnFit(root);
                }
                else if (!root.ClassListContains("kh-mm-wide")) ContainColumn(root, z);
                PlaceHero(root, full, dusk);
                top.style.height = MainMenuCopy.BarHeight(h, topInset);
                bottom.style.height = MainMenuCopy.BarHeight(h, botInset);
                MainMenuLayout.HeroLift = MeasureLift(root, safe, full, z, h, topInset);
                TextFit.Apply(root);
                if (full != null) TextFit.Apply(full);
            });
            launchKey.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float h = full.resolvedStyle.height; if (h <= 0f) return;
                float topInset = Mathf.Max(0f, safe.worldBound.yMin - full.worldBound.yMin);
                float botInset = Mathf.Max(0f, full.worldBound.yMax - safe.worldBound.yMax);
                MainMenuLayout.HeroLift = MeasureLift(root, safe, full, MenuTypeScale.Compute(full.resolvedStyle.width, h, topInset, botInset), h, topInset);
            });
            PlayIntro(root, full, reduceMotion);
            return root;
        }

        /// <summary>Landscape web: the wordmark sits on the hero, and the action cards sit in a column
        /// beside it, sized to the free space. Portrait and iOS leave the phone column from ApplyScale.</summary>
        public static void ApplyWebWide(VisualElement root, bool wide)
        {
            if (root == null) return;
            // .kh-screen caps the phone column at 600. Wide web needs the whole safe width.
            var host = root.parent;
            if (host != null)
            {
                if (wide) host.style.maxWidth = Length.Percent(100);
                else host.style.maxWidth = StyleKeyword.Null;
                if (wide) host.style.alignSelf = Align.Stretch;
                else host.style.alignSelf = StyleKeyword.Null;
            }
            root.EnableInClassList("kh-mm-wide", wide);
            if (!wide) { ClearWebWide(root); return; }
            float rw = root.resolvedStyle.width;
            float rh = root.resolvedStyle.height;
            if (rw < 8f || rh < 8f) return;
            float cssW, cssH, safeTop, safeBottom, safeLeft, safeRight;
            if (ForceWide == true)
            {
                // The editor's Screen is not the device under test. Lay the wide frames out in the panel.
                cssW = rw;
                cssH = rh;
                safeTop = safeBottom = safeLeft = safeRight = 0f;
            }
            else
            {
                WebViewport.Css(out cssW, out cssH);
                if (cssW < 8f || cssH < 8f) { cssW = rw; cssH = rh; }
                CssSafe(cssW, cssH, out safeTop, out safeBottom, out safeLeft, out safeRight);
            }
            var frame = WebHomeLayout.Place(cssW, cssH, safeTop, safeBottom, safeLeft, safeRight);
            // The host was just released from the 600px cap, so this pass can still be the phone
            // column. Keep the wide class and place on the next geometry.
            if (!frame.Wide || frame.Type == null) return;
            // mm-root already fills the safe rect, so CSS boxes are shifted back to that origin.
            float innerW = Mathf.Max(1f, cssW - safeLeft - safeRight);
            float innerH = Mathf.Max(1f, cssH - safeTop - safeBottom);
            float sx = rw / innerW;
            float sy = rh / innerH;
            float fs = Mathf.Min(sx, sy);
            HudBox Sc(HudBox b) => new HudBox
            {
                X = (b.X - safeLeft) * sx,
                Y = (b.Y - safeTop) * sy,
                W = b.W * sx,
                H = b.H * sy,
            };
            frame.Title = Sc(frame.Title); frame.Stats = Sc(frame.Stats);
            frame.Launch = Sc(frame.Launch); frame.Design = Sc(frame.Design);
            frame.Missions = Sc(frame.Missions); frame.Codex = Sc(frame.Codex);
            frame.Menu = Sc(frame.Menu); frame.Hint = Sc(frame.Hint);
            root.style.paddingTop = 0;
            root.style.paddingBottom = 0;
            root.style.paddingLeft = 0;
            root.style.paddingRight = 0;
            Reparent(root, "mm-title");
            Reparent(root, "mm-stats");
            Reparent(root, "home-launch");
            Reparent(root, "home-design");
            Reparent(root, "mm-grid");
            Reparent(root, "mm-hint");
            Pin(root.Q("mm-title"), frame.Title);
            Pin(root.Q("mm-stats"), frame.Stats);
            Clip(root.Q("mm-title"));
            Clip(root.Q("mm-stats"));
            var type = frame.Type;
            Font(root.Q("mm-kestrel"), WidePt(type.Title * fs), 0.02f);
            Font(root.Q("mm-kestrel-shadow"), WidePt(type.Title * fs), 0.02f);
            Font(root.Q("mm-eyebrow"), Mathf.Clamp(type.Title * fs * 0.18f, 10f, 18f), 0.12f);
            Font(root.Q("mm-heavy"), WidePt(type.Tagline * fs), 0.42f);
            var kestrel = root.Q("mm-kestrel");
            if (kestrel != null) { kestrel.style.whiteSpace = WhiteSpace.NoWrap; kestrel.style.overflow = Overflow.Hidden; }
            var heavy = root.Q("mm-heavy");
            if (heavy != null) { heavy.style.whiteSpace = WhiteSpace.NoWrap; heavy.style.overflow = Overflow.Hidden; }
            root.Query(className: "kh-mm-stat").ForEach(e => e.style.fontSize = type.Stat * fs);
            var record = root.Q("mm-record");
            if (record != null) record.style.fontSize = Mathf.Max(11f, type.Stat * fs);

            bool scroll = frame.Scroll;
            var scroller = WideScroll(root, scroll);
            // `host` above is the safe column (.kh-screen). This is where the wide actions go.
            VisualElement slot = scroll ? scroller : root;
            PlaceAction(root, slot, "home-launch", frame.Launch, scroll, sx, sy);
            PlaceAction(root, slot, "home-design", frame.Design, scroll, sx, sy);
            var grid = root.Q("mm-grid");
            if (grid != null)
            {
                if (scroll)
                {
                    if (grid.parent != scroller) scroller.Add(grid);
                    grid.style.position = Position.Relative;
                    grid.style.left = StyleKeyword.Auto;
                    grid.style.top = StyleKeyword.Auto;
                    grid.style.width = Length.Percent(100);
                    grid.style.height = StyleKeyword.Auto;
                    grid.style.marginTop = 0;
                    grid.style.marginBottom = WebHomeLayout.Gap * sy;
                }
                else
                {
                    Reparent(root, "mm-grid");
                    float top = frame.Missions.Y;
                    float bottom = Mathf.Max(frame.Missions.Bottom, frame.Codex.Bottom);
                    Pin(grid, new HudBox { X = frame.Menu.X, Y = top, W = frame.Menu.W, H = Mathf.Max(1f, bottom - top) });
                }
                grid.style.flexDirection = FlexDirection.Column;
                grid.style.flexWrap = Wrap.NoWrap;
                grid.style.overflow = Overflow.Hidden;
            }
            float tileGap = scroll ? WebHomeLayout.Gap * sy : Mathf.Max(0f, frame.Codex.Y - frame.Missions.Bottom);
            foreach (var it in MainMenuCopy.Tiles)
            {
                var tile = root.Q("home-" + it.Id);
                if (tile == null) continue;
                float h = it.Id == "codex" ? frame.Codex.H : frame.Missions.H;
                if (scroll) h = Mathf.Max(h, 44f * sy);
                tile.style.position = Position.Relative;
                tile.style.left = StyleKeyword.Auto;
                tile.style.top = StyleKeyword.Auto;
                tile.style.width = Length.Percent(100);
                tile.style.height = h;
                tile.style.minHeight = h;
                tile.style.maxHeight = h;
                tile.style.marginTop = 0;
                tile.style.marginBottom = it.Id == "missions" ? tileGap : 0f;
                tile.style.flexShrink = 0;
                tile.style.overflow = Overflow.Hidden;
                tile.style.paddingLeft = 10f * sx;
                tile.style.paddingRight = 10f * sx;
                tile.style.paddingTop = 10f * sy;
                tile.style.paddingBottom = 10f * sy;
            }
            PlaceAction(root, slot, "mm-hint", frame.Hint, scroll, sx, sy);
            // Layout already sized these to the box. A 14px floor here is what clipped
            // "Next flight…" and "Thrust 850 kN" on a landscape web window.
            Font(root.Q(className: "kh-mm-launch-label"), type.Launch * fs, 0.10f);
            Font(root.Q(className: "kh-mm-launch-caption"), type.LaunchCaption * fs, 0f);
            Font(root.Q(className: "kh-mm-design-label"), type.Design * fs, 0.03f);
            Font(root.Q(className: "kh-mm-design-caption"), type.DesignCaption * fs, 0f);
            root.Query(className: "kh-mm-tile-name").ForEach(e =>
            {
                float name = type.TileName * fs;
                e.style.fontSize = name;
                e.style.letterSpacing = name * 0.03f;
                e.style.whiteSpace = WhiteSpace.NoWrap;
                e.style.overflow = Overflow.Hidden;
            });
            root.Query(className: "kh-mm-tile-caption").ForEach(e =>
            {
                e.style.fontSize = type.TileCaption * fs;
                e.style.whiteSpace = WhiteSpace.Normal;
                e.style.overflow = Overflow.Hidden;
            });
            root.Query(className: "kh-mm-soon").ForEach(e =>
            {
                float soon = type.Soon * fs;
                e.style.fontSize = soon;
                e.style.letterSpacing = soon * 0.15f;
            });
            Font(root.Q("mm-hint-line"), type.Hint * fs, 0f);
            var hint = root.Q("mm-hint-line");
            if (hint != null) { hint.style.whiteSpace = WhiteSpace.Normal; hint.style.overflow = Overflow.Hidden; }
            FitCardText(root.Q("home-launch"), true);
            FitCardText(root.Q("home-design"), false);
            if (scroll) Pin(scroller, frame.Menu);
        }

        static void CssSafe(float cssW, float cssH, out float top, out float bottom, out float left, out float right)
        {
            top = bottom = left = right = 0f;
            if (Screen.width <= 1f || Screen.height <= 1f || cssW <= 1f || cssH <= 1f) return;
            var safe = Screen.safeArea;
            float perX = Screen.width / cssW;
            float perY = Screen.height / cssH;
            if (perX <= 0f || perY <= 0f) return;
            top = Mathf.Max(0f, (Screen.height - safe.yMax) / perY);
            bottom = Mathf.Max(0f, safe.yMin / perY);
            left = Mathf.Max(0f, safe.xMin / perX);
            right = Mathf.Max(0f, (Screen.width - safe.xMax) / perX);
        }

        static void PlaceAction(VisualElement root, VisualElement host, string name, HudBox box, bool scroll, float sx, float sy)
        {
            var e = root.Q(name);
            if (e == null) return;
            if (scroll)
            {
                if (e.parent != host) host.Add(e);
                e.style.position = Position.Relative;
                e.style.left = StyleKeyword.Auto;
                e.style.top = StyleKeyword.Auto;
                e.style.right = StyleKeyword.Auto;
                e.style.bottom = StyleKeyword.Auto;
                e.style.width = Length.Percent(100);
                float h = name == "mm-hint" ? box.H : Mathf.Max(box.H, 44f * sy);
                e.style.height = h;
                e.style.minHeight = h;
                e.style.maxHeight = h;
                e.style.marginTop = 0;
                e.style.marginBottom = WebHomeLayout.Gap * sy;
                e.style.flexShrink = 0;
            }
            else
            {
                Reparent(root, name);
                Pin(e, box);
            }
            e.style.overflow = Overflow.Hidden;
            if (name == "home-launch" || name == "home-design")
            {
                float padX = 12f * sx;
                float padY = (name == "home-launch" ? 8f : 6f) * sy;
                e.style.paddingLeft = padX;
                e.style.paddingRight = padX;
                e.style.paddingTop = padY;
                e.style.paddingBottom = padY;
            }
        }

        static void FitCardText(VisualElement card, bool launch)
        {
            if (card == null) return;
            card.style.overflow = Overflow.Hidden;
            var label = card.Q(className: launch ? "kh-mm-launch-label" : "kh-mm-design-label");
            var cap = card.Q(className: launch ? "kh-mm-launch-caption" : "kh-mm-design-caption");
            if (label != null) { label.style.whiteSpace = WhiteSpace.Normal; label.style.textOverflow = TextOverflow.Clip; label.style.overflow = Overflow.Hidden; }
            if (cap != null) { cap.style.whiteSpace = WhiteSpace.Normal; cap.style.textOverflow = TextOverflow.Clip; cap.style.overflow = Overflow.Hidden; }
        }

        /// <summary>
        /// Wide web type is already fitted to the card. A 14 pt floor here is what pushed
        /// "DESIGN FIRST" and the captions out of a narrow landscape column.
        /// </summary>
        static float WidePt(float size) => size;

        static void Font(VisualElement e, float size, float trackEm)
        {
            if (e == null) return;
            size = TextFit.Cap(e, size);
            e.style.fontSize = size;
            e.style.letterSpacing = trackEm > 0f ? size * trackEm : 0f;
        }

        static void Clip(VisualElement e)
        {
            if (e != null) e.style.overflow = Overflow.Hidden;
        }

        static void Reparent(VisualElement root, string name)
        {
            var e = root.Q(name);
            if (e != null && e.parent != root) root.Add(e);
        }

        static ScrollView WideScroll(VisualElement root, bool on)
        {
            var scroller = root.Q<ScrollView>("mm-wide-scroll");
            if (!on)
            {
                if (scroller != null) scroller.RemoveFromHierarchy();
                return null;
            }
            if (scroller == null)
            {
                scroller = new ScrollView(ScrollViewMode.Vertical) { name = "mm-wide-scroll" };
                scroller.mode = ScrollViewMode.Vertical;
                scroller.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                scroller.verticalScrollerVisibility = ScrollerVisibility.Auto;
                root.Add(scroller);
            }
            return scroller;
        }

        static void Pin(VisualElement e, HudBox b)
        {
            if (e == null || !b.Live) return;
            e.style.position = Position.Absolute;
            e.style.left = b.X;
            e.style.top = b.Y;
            e.style.width = b.W;
            e.style.height = b.H;
            e.style.minHeight = b.H;
            e.style.maxHeight = b.H;
            e.style.right = StyleKeyword.Auto;
            e.style.bottom = StyleKeyword.Auto;
            e.style.marginTop = 0;
            e.style.marginBottom = 0;
            e.style.flexGrow = 0;
            e.style.flexShrink = 0;
        }

        static void ClearWebWide(VisualElement root)
        {
            void Drop(VisualElement e)
            {
                if (e == null) return;
                e.style.position = StyleKeyword.Null;
                e.style.left = StyleKeyword.Null;
                e.style.top = StyleKeyword.Null;
                e.style.right = StyleKeyword.Null;
                e.style.bottom = StyleKeyword.Null;
                e.style.width = StyleKeyword.Null;
                e.style.maxHeight = StyleKeyword.Null;
                e.style.overflow = StyleKeyword.Null;
            }
            Reparent(root, "mm-title");
            Reparent(root, "mm-stats");
            Reparent(root, "home-launch");
            Reparent(root, "home-design");
            Reparent(root, "mm-grid");
            Reparent(root, "mm-hint");
            var scroller = root.Q("mm-wide-scroll");
            if (scroller != null) scroller.RemoveFromHierarchy();
            var title = root.Q("mm-title");
            Drop(title);
            if (title != null)
            {
                title.style.height = StyleKeyword.Null;
                title.style.minHeight = StyleKeyword.Null;
            }
            Drop(root.Q("mm-stats"));
            Drop(root.Q("home-launch"));
            var design = root.Q("home-design");
            Drop(design);
            if (design != null)
            {
                design.style.paddingTop = StyleKeyword.Null;
                design.style.paddingBottom = StyleKeyword.Null;
                design.style.paddingLeft = StyleKeyword.Null;
                design.style.paddingRight = StyleKeyword.Null;
            }
            var hint = root.Q("mm-hint");
            Drop(hint);
            if (hint != null)
            {
                hint.style.height = StyleKeyword.Null;
                hint.style.minHeight = StyleKeyword.Null;
            }
            var grid = root.Q("mm-grid");
            Drop(grid);
            if (grid != null)
            {
                grid.style.height = StyleKeyword.Null;
                grid.style.minHeight = StyleKeyword.Null;
                grid.style.flexDirection = StyleKeyword.Null;
                grid.style.flexWrap = StyleKeyword.Null;
            }
            foreach (var it in MainMenuCopy.Tiles) Drop(root.Q("home-" + it.Id));
        }

        /// <summary>
        /// Full-bleed dusk goes behind the safe column. The plate is opaque; a later sibling covers
        /// every control that lives under Safe. Gear and the audio row are added after this, so they
        /// stay on top. A host that is not under <paramref name="full"/> (the mix tests) still receives the plate.
        /// </summary>
        static void PlaceSky(VisualElement full, VisualElement layer, VisualElement safe)
        {
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            if (full == null) return;
            var branch = safe;
            while (branch != null && branch.parent != null && branch.parent != full)
                branch = branch.parent;
            if (branch != null && branch.parent == full)
            {
                if (layer.parent != null) layer.RemoveFromHierarchy();
                full.Insert(full.IndexOf(branch), layer);
                return;
            }
            full.Add(layer);
        }

        /// <summary>Natural title height and the column height it was measured in.
        /// A later pass cannot read the natural height off <c>layout</c> once the title is pinned.</summary>
        sealed class ColumnFit
        {
            public float Host;
            public float TitleH;
        }

        /// <summary>Height of the Thrum strip for a two-to-four line caption at the hint face.
        /// The portrait and the button padding eat width the column total does not.</summary>
        static float ThrumBlock(VisualElement root, MenuTypeScale.Sizes z)
        {
            float col = root.resolvedStyle.width;
            if (float.IsNaN(col) || col < 8f) col = root.layout.width;
            float inner = Mathf.Max(48f, col - 24f - 20f - 30f);
            float size = Mathf.Max(14f, z.Hint);
            float textW = MainMenuCopy.ThrumWidth100 * size / 100f;
            int lines = (int)Mathf.Ceil(textW * 1.08f / inner);
            if (lines < 1) lines = 1;
            if (lines > 4) lines = 4;
            float name = Mathf.Max(14f, z.Hint) * MenuTypeScale.LineH;
            return 8f + name + lines * size * MenuTypeScale.LineH + 6f;
        }

        static void PinThrum(VisualElement root, float h)
        {
            var hint = root != null ? root.Q("mm-hint") : null;
            if (hint == null || h < 8f) return;
            hint.style.height = h;
            hint.style.minHeight = h;
            hint.style.maxHeight = h;
            hint.style.flexShrink = 0;
            hint.style.overflow = Overflow.Hidden;
        }

        /// <summary>
        /// Phone column only. Portrait already fits, so this returns without touching styles.
        /// A short landscape column (SE 667×375) overflows once the spacer hits zero; shrink padding
        /// and the blocks, never below a 44pt hit target, and keep the Thrum strip's band.
        /// Caps are cleared when the column grows or the natural stack fits again, so a rotation
        /// back to portrait does not leave max-height and a clipped title behind.
        /// </summary>
        static void ContainColumn(VisualElement root, MenuTypeScale.Sizes z)
        {
            if (root == null || root.ClassListContains("kh-mm-wide")) return;
            float host = root.layout.height;
            if (host < 8f) return;
            var title = root.Q("mm-title");
            if (title == null) return;
            // A title at 0 while LAUNCH has a height has already been flex-crushed, not "not laid out".
            // Skipping here left the short column uncapped. Use the type-scale block until a real height exists.
            bool titleLaidOut = title.layout.height >= 1f;
            if (!titleLaidOut)
            {
                var launchProbe = root.Q("home-launch");
                if (launchProbe == null || launchProbe.layout.height < 1f) return;
            }
            float naturalTitle = titleLaidOut ? title.layout.height : TitleBlock(z);

            var fit = root.userData as ColumnFit;
            // The column height changed (landscape to portrait, or the reverse). The saved
            // title belongs to the previous scale. Drop the caps and let the next pass
            // measure the title ApplyScale just restored.
            if (fit != null && Mathf.Abs(host - fit.Host) > 1f)
            {
                ClearContainment(root);
                ForgetColumnFit(root);
                return;
            }

            float titleH = fit != null && fit.TitleH > 1f ? fit.TitleH : naturalTitle;
            float padTop = MenuTypeScale.TopPad;
            float padBot = z.HintBand;
            // The Thrum strip sits under the tiles and is not part of ControlsH. Leaving it out
            // let a wrapped caption grow over the Missions tile on a short landscape column.
            float hint = root.Q("mm-hint") != null ? ThrumBlock(root, z) : 0f;
            float need = padTop + padBot + titleH + z.ControlsH + hint;
            if (need <= host + 1f)
            {
                ClearContainment(root);
                ForgetColumnFit(root);
                PinThrum(root, hint);
                // A short pass sealed a small face. The column fits again, so put the type scale back
                // before the fit pass below this callback decides whether it still has to shrink.
                TextFit.Release(title);
                var word = root.Q("mm-kestrel");
                var shadow = root.Q("mm-kestrel-shadow");
                var heavy = root.Q("mm-heavy");
                var eye = root.Q("mm-eyebrow");
                if (word != null) word.style.fontSize = z.Title;
                if (shadow != null) shadow.style.fontSize = z.Title;
                if (heavy != null) heavy.style.fontSize = z.Tagline;
                if (eye != null) eye.style.fontSize = Mathf.Max(14f, z.Hint);
                return;
            }

            // Recompute from the natural title. Clearing first drops a cap this pass no longer needs.
            ClearContainment(root);
            float overflow = need - host;
            const float topFloor = 8f;
            const float botFloor = 32f;
            float eatTop = Mathf.Min(overflow, Mathf.Max(0f, padTop - topFloor));
            padTop -= eatTop;
            overflow -= eatTop;
            float eatBot = Mathf.Min(overflow, Mathf.Max(0f, padBot - botFloor));
            padBot -= eatBot;
            overflow -= eatBot;
            root.style.paddingTop = padTop;
            root.style.paddingBottom = Mathf.Max(botFloor, padBot);

            void Cut(VisualElement el, ref float box, float floor)
            {
                if (overflow <= 1f || el == null) return;
                float cut = Mathf.Min(overflow, Mathf.Max(0f, box - floor));
                if (cut <= 0f) return;
                box -= cut;
                overflow -= cut;
                el.style.height = box;
                el.style.minHeight = box;
                el.style.maxHeight = box;
                el.style.flexShrink = 1;
            }

            float stats = z.StatsH, launch = z.LaunchH, design = z.DesignH, tile = z.TileH;
            Cut(root.Q("mm-hint"), ref hint, MenuTypeScale.HitMin);
            Cut(root.Q("mm-stats"), ref stats, 28f);
            Cut(root.Q("home-design"), ref design, MenuTypeScale.HitMin);
            Cut(root.Q("home-launch"), ref launch, MenuTypeScale.HitMin);
            if (overflow > 1f)
            {
                float cut = Mathf.Min(overflow, Mathf.Max(0f, tile - MenuTypeScale.HitMin));
                if (cut > 0f)
                {
                    tile -= cut;
                    overflow -= cut;
                    foreach (var it in MainMenuCopy.Tiles)
                    {
                        var b = root.Q("home-" + it.Id);
                        if (b == null) continue;
                        b.style.height = tile;
                        b.style.minHeight = tile;
                        b.style.maxHeight = tile;
                        b.style.flexShrink = 1;
                    }
                }
            }
            if (overflow > 1f)
            {
                const float floor = 36f;
                float cut = Mathf.Min(overflow, Mathf.Max(0f, titleH - floor));
                if (cut > 0f)
                {
                    float next = titleH - cut;
                    title.style.height = next;
                    title.style.minHeight = next;
                    title.style.maxHeight = next;
                    title.style.overflow = Overflow.Hidden;
                    title.style.flexShrink = 1;
                    float size = Mathf.Max(18f, next * 0.42f);
                    var kestrel = root.Q("mm-kestrel");
                    var shadow = root.Q("mm-kestrel-shadow");
                    var heavy = root.Q("mm-heavy");
                    var eye = root.Q("mm-eyebrow");
                    if (kestrel != null) kestrel.style.fontSize = size;
                    if (shadow != null) shadow.style.fontSize = size;
                    if (heavy != null) heavy.style.fontSize = Mathf.Max(11f, size * 0.45f);
                    if (eye != null) eye.style.fontSize = Mathf.Max(9f, size * 0.32f);
                }
            }

            PinThrum(root, hint);
            var saved = root.userData as ColumnFit;
            if (saved == null)
            {
                saved = new ColumnFit { TitleH = naturalTitle };
                root.userData = saved;
            }
            saved.Host = host;
        }

        /// <summary>Eyebrow, wordmark and tagline when the title's layout height is not the content size.</summary>
        static float TitleBlock(MenuTypeScale.Sizes z)
        {
            float eye = Mathf.Max(11f, z.Title * 0.22f);
            return eye * MenuTypeScale.LineH + z.Title * 1.3f + z.Tagline * MenuTypeScale.LineH;
        }

        /// <summary>Removes the inline max-heights, the pinned title, and the clipped title fonts' box.
        /// Control heights and fonts belong to <see cref="ApplyScale"/>, which runs before this.</summary>
        static void ClearContainment(VisualElement root)
        {
            if (root == null) return;
            void Free(VisualElement el)
            {
                if (el == null) return;
                el.style.maxHeight = StyleKeyword.Null;
                el.style.flexShrink = 0;
            }
            Free(root.Q("mm-stats"));
            Free(root.Q("home-launch"));
            Free(root.Q("home-design"));
            foreach (var it in MainMenuCopy.Tiles) Free(root.Q("home-" + it.Id));
            var title = root.Q("mm-title");
            if (title == null) return;
            title.style.height = StyleKeyword.Null;
            title.style.minHeight = StyleKeyword.Null;
            title.style.maxHeight = StyleKeyword.Null;
            title.style.overflow = StyleKeyword.Null;
            title.style.flexShrink = StyleKeyword.Null;
        }

        static void ForgetColumnFit(VisualElement root)
        {
            if (root != null && root.userData is ColumnFit) root.userData = null;
        }

        /// <summary>Shows the real next mission on the LAUNCH bar when Career knows it. The static caption stays the fallback.</summary>
        public static void SetMissionLine(VisualElement root, string line)
        {
            if (root == null || string.IsNullOrEmpty(line)) return;
            var cap = root.Q<Label>("mm-launch-caption");
            if (cap != null) cap.text = line;
        }

        /// <summary>Paints the hero rocket in the open pad between the title and the controls.</summary>
        static void PlaceHero(VisualElement root, VisualElement full, HomeDuskBackdrop dusk)
        {
            if (root == null || full == null || dusk == null) return;
            float w = full.resolvedStyle.width;
            float h = full.resolvedStyle.height;
            if (w < 8f || h < 8f) return;
            float originY = full.worldBound.yMin;
            float originX = full.worldBound.xMin;
            var title = root.Q("mm-title");
            var stats = root.Q("mm-stats");
            var launch = root.Q("home-launch");
            float top = h * 0.16f;
            if (title != null && title.worldBound.height > 1f)
                top = title.worldBound.yMax - originY + 4f;
            bool wide = root.ClassListContains("kh-mm-wide");
            float left = w * 0.22f;
            float right = w * 0.78f;
            float bottom = h * 0.72f;
            if (wide)
            {
                if (stats != null && stats.worldBound.width > 1f)
                    left = stats.worldBound.xMax - originX + 8f;
                if (launch != null && launch.worldBound.width > 1f)
                    right = launch.worldBound.xMin - originX - 8f;
                bottom = h - MainMenuCopy.BarHeight(h, 0f);
            }
            else if (stats != null && stats.worldBound.height > 1f)
            {
                bottom = stats.worldBound.yMin - originY - 6f;
                left = w * 0.18f;
                right = w * 0.82f;
            }
            float width = right - left;
            if (width < 24f || bottom - top < 24f) return;
            dusk.SetHero(new Rect(left, top, width, bottom - top), root.ClassListContains("kh-mm-reduce"));
        }

        static VisualElement ThrumStrip(VisualElement full)
        {
            var row = new Button { name = "mm-hint", text = string.Empty, tooltip = MainMenuCopy.ThrumLine };
            row.AddToClassList("kh-mm-thrum");
            row.AddToClassList("kh-mm-glass");
            row.AddToClassList("kh-mm-anim");
            row.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            row.style.borderTopWidth = 0f;
            row.style.borderRightWidth = 0f;
            row.style.borderBottomWidth = 0f;
            row.style.borderLeftWidth = 0f;
            var face = new VisualElement { name = "mm-thrum-portrait", pickingMode = PickingMode.Ignore };
            face.AddToClassList("kh-mm-thrum-portrait");
            var tex = Resources.Load<Texture2D>("Guide/thrum");
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                face.style.backgroundImage = new StyleBackground(tex);
            }
            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.AddToClassList("kh-mm-col");
            var who = new Label("Thrum") { name = "mm-thrum-name", pickingMode = PickingMode.Ignore };
            who.AddToClassList("kh-mm-thrum-name");
            var line = new Label(MainMenuCopy.ThrumLine) { name = "mm-hint-line", pickingMode = PickingMode.Ignore };
            line.AddToClassList("kh-mm-hint");
            line.style.whiteSpace = WhiteSpace.Normal;
            col.Add(who); col.Add(line);
            row.Add(face); row.Add(col);
            row.clicked += () => ShowNote(full, "home-thrum-note", "Thrum", MainMenuCopy.ThrumLine);
            return row;
        }

        static VisualElement StatsStrip(VisualElement full)
        {
            var row = new VisualElement { name = "mm-stats", pickingMode = PickingMode.Ignore };
            row.AddToClassList("kh-mm-stats"); row.AddToClassList("kh-mm-glass"); row.AddToClassList("kh-mm-anim");
            row.style.flexDirection = FlexDirection.Column;
            row.style.justifyContent = Justify.Center;
            // The USS padding would shrink the hit target inside the 46px box. Inset the words instead.
            row.style.paddingTop = 0f;
            row.style.paddingBottom = 0f;
            row.style.paddingLeft = 0f;
            row.style.paddingRight = 0f;
            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.style.flexDirection = FlexDirection.Column;
            text.style.justifyContent = Justify.Center;
            text.style.flexGrow = 1;
            text.style.paddingTop = 2f;
            text.style.paddingBottom = 2f;
            text.style.paddingLeft = 10f;
            text.style.paddingRight = 10f;
            var pills = new VisualElement { pickingMode = PickingMode.Ignore };
            pills.style.flexDirection = FlexDirection.Row;
            pills.style.flexShrink = 0;
            pills.Add(Stat("mm-fuel", MainMenuCopy.FuelStat));
            pills.Add(Stat("mm-thrust", MainMenuCopy.ThrustStat));
            text.Add(pills);
            var line = new Label(HomeRecord.Line(0, 0, 1, 0)) { name = "mm-record", pickingMode = PickingMode.Ignore };
            line.AddToClassList("kh-mm-stat");
            line.style.whiteSpace = WhiteSpace.Normal;
            line.style.flexShrink = 0;
            text.Add(line);
            row.Add(text);
            // Covers the whole stats box, so the hit stays 44pt on the phone column (the box is 46).
            var hit = new Button { name = "home-patches", text = string.Empty, tooltip = "Patches. Flights you have cleared." };
            hit.style.position = Position.Absolute;
            hit.style.left = 0f;
            hit.style.right = 0f;
            hit.style.top = 0f;
            hit.style.bottom = 0f;
            hit.style.marginTop = 0f;
            hit.style.marginBottom = 0f;
            hit.style.marginLeft = 0f;
            hit.style.marginRight = 0f;
            hit.style.paddingTop = 0f;
            hit.style.paddingBottom = 0f;
            hit.style.paddingLeft = 0f;
            hit.style.paddingRight = 0f;
            hit.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            hit.style.borderTopWidth = 0f;
            hit.style.borderRightWidth = 0f;
            hit.style.borderBottomWidth = 0f;
            hit.style.borderLeftWidth = 0f;
            hit.clicked += () =>
            {
                if (OpenPatches != null) OpenPatches();
                else ShowNote(full, "home-patches-note", "Patches", "Your patches are on this device once a flight is cleared.");
            };
            row.Add(hit);
            return row;
        }

        /// <summary>Rewrite the home record after the save is read. A missing save stays at zero.</summary>
        public static void SetRecord(VisualElement root, string line)
        {
            var label = root != null ? root.Q<Label>("mm-record") : null;
            if (label == null || string.IsNullOrEmpty(line)) return;
            label.text = line;
        }

        static Label Stat(string name, string text)
        {
            var line = new Label(text) { name = name, pickingMode = PickingMode.Ignore };
            line.AddToClassList("kh-mm-stat");
            return line;
        }

        static Button LaunchBar(Action launch, string missionLine)
        {
            var b = new Button(launch) { name = "home-launch", tooltip = MainMenuCopy.Launch.Spoken };
            b.AddToClassList("kh-button"); b.AddToClassList("kh-mm-launch"); b.AddToClassList("kh-mm-anim");
            var lamp = new VisualElement { name = "lamp", pickingMode = PickingMode.Ignore }; lamp.AddToClassList("kh-mm-lamp"); lamp.AddToClassList("kh-mm-lamp--on");
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.AddToClassList("kh-mm-col");
            var label = new Label(MainMenuCopy.Launch.Label) { pickingMode = PickingMode.Ignore }; label.AddToClassList("kh-mm-launch-label");
            var cap = new Label(string.IsNullOrEmpty(missionLine) ? MainMenuCopy.Launch.Caption : missionLine) { name = "mm-launch-caption", pickingMode = PickingMode.Ignore };
            cap.AddToClassList("kh-mm-launch-caption");
            col.Add(label); col.Add(cap);
            var icon = new HomeGlyph("rocket") { name = "mm-launch-icon" };
            icon.AddToClassList("kh-mm-launch-icon");
            icon.Ink = Color.white;
            b.Add(lamp); b.Add(col); b.Add(icon);
            b.text = string.Empty;
            b.style.backgroundImage = new StyleBackground(HomeDuskBackdrop.LaunchFill());
            return b;
        }

        static Button DesignBar(Action design)
        {
            var b = new Button(design) { name = "home-design", tooltip = MainMenuCopy.Design.Spoken };
            b.AddToClassList("kh-button"); b.AddToClassList("kh-mm-design"); b.AddToClassList("kh-mm-anim");
            var lamp = new VisualElement { name = "lamp", pickingMode = PickingMode.Ignore }; lamp.AddToClassList("kh-mm-lamp"); lamp.AddToClassList("kh-mm-lamp--on");
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.AddToClassList("kh-mm-col");
            var label = new Label(MainMenuCopy.Design.Label) { pickingMode = PickingMode.Ignore }; label.AddToClassList("kh-mm-design-label");
            var cap = new Label(MainMenuCopy.Design.Caption) { pickingMode = PickingMode.Ignore }; cap.AddToClassList("kh-mm-design-caption");
            col.Add(label); col.Add(cap);
            var icon = new HomeGlyph("blueprint") { name = "mm-design-icon" };
            icon.style.width = 22; icon.style.height = 22; icon.style.marginLeft = 8;
            b.Add(lamp); b.Add(col); b.Add(icon);
            b.text = string.Empty;
            return b;
        }

        static void Gear(VisualElement full)
        {
            var sheet = new VisualElement { name = "home-settings-sheet" };
            sheet.AddToClassList("kh-mm-sheet");
            sheet.style.display = DisplayStyle.None;
            sheet.style.flexDirection = FlexDirection.Column;
            sheet.style.overflow = Overflow.Hidden;
            var title = new Label("Settings") { name = "home-settings-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("kh-mm-sheet-title");
            title.style.fontSize = 16;
            title.style.flexShrink = 0;
            SettingFont(title, "SpaceGrotesk-Bold");

            var list = new ScrollView(ScrollViewMode.Vertical) { name = "home-settings-list" };
            list.style.flexDirection = FlexDirection.Column;
            list.style.flexGrow = 1f;
            list.style.flexShrink = 1f;
            list.style.minHeight = 0f;
            list.style.alignItems = Align.Stretch;

            var music = SettingButton("home-setting-music", "Music");
            var sound = SettingButton("home-setting-sound", "Sound");
            var voice = SettingButton("home-setting-voice", "Voice");
            var subtitles = SettingButton("home-setting-subtitles", "Subtitles");
            var sandbox = SettingButton("home-setting-sandbox", "Sandbox");
            var motion = SettingButton("home-reduce-motion", "Reduce motion");
            motion.tooltip = "Reduce motion. Holds the menu still.";

            void PaintMix()
            {
                PaintToggle(music, "Music", AudioPrefs.MusicMuted);
                PaintToggle(sound, "Sound", AudioPrefs.SfxMuted);
                PaintToggle(voice, "Voice", AudioPrefs.NarrationMuted);
                AudioBar.Sync(full);
            }
            void PaintSubs()
            {
                bool on = AudioPrefs.SubtitlesOn;
                PaintCaption(subtitles, AudioPrefs.Caption("Subtitles", !on), on);
                subtitles.tooltip = on ? "Subtitles are on." : "Subtitles are off.";
            }
            void PaintSandbox()
            {
                bool on = AudioPrefs.SandboxUnlocked;
                PaintCaption(sandbox, AudioPrefs.Caption("Sandbox", !on), on);
                sandbox.tooltip = on ? "Sandbox is available on the mission board." : "Sandbox is off. The mission board keeps it locked.";
            }
            void PaintMotion()
            {
                bool on = PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0) != 0;
                PaintCaption(motion, on ? "Reduce motion on" : "Reduce motion", on);
                motion.tooltip = on ? "Reduce motion is on." : "Reduce motion is off.";
            }

            music.clicked += () => { AudioPrefs.SetMusicMuted(!AudioPrefs.MusicMuted); PaintMix(); };
            sound.clicked += () => { AudioPrefs.SetSfxMuted(!AudioPrefs.SfxMuted); PaintMix(); };
            voice.clicked += () => { AudioPrefs.SetNarrationMuted(!AudioPrefs.NarrationMuted); PaintMix(); };
            subtitles.clicked += () =>
            {
                AudioPrefs.SetSubtitlesOn(!AudioPrefs.SubtitlesOn);
                PaintSubs();
                GuideSession.Engine?.PullSettings();
            };
            sandbox.clicked += () => { AudioPrefs.SetSandboxUnlocked(!AudioPrefs.SandboxUnlocked); PaintSandbox(); };
            motion.clicked += () =>
            {
                int cur = PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0);
                PlayerPrefs.SetInt(MainMenuLayout.ReduceMotionPref, cur == 0 ? 1 : 0);
                PlayerPrefs.Save();
                PaintMotion();
            };
            PaintMix();
            PaintSubs();
            PaintSandbox();
            PaintMotion();

            list.Add(music);
            list.Add(sound);
            list.Add(voice);
            list.Add(subtitles);
            list.Add(sandbox);
            list.Add(motion);

            var close = new Button { name = "home-settings-close", text = "Close" };
            close.AddToClassList("kh-button");
            close.tooltip = "Close settings.";
            close.style.minHeight = 44;
            close.style.height = 44;
            close.style.fontSize = 14;
            close.style.flexShrink = 0;
            close.style.whiteSpace = WhiteSpace.Normal;
            close.style.position = Position.Absolute;
            close.style.left = 8;
            close.style.right = 8;
            close.style.bottom = 8;
            SettingFont(close, "SpaceGrotesk-Bold");
            close.clicked += () => sheet.style.display = DisplayStyle.None;

            sheet.Add(title);
            sheet.Add(list);
            sheet.Add(close);

            float fitW = -1f, fitH = -1f;
            void FitSheet()
            {
                if (full == null || sheet.style.display.value == DisplayStyle.None) return;
                float hostW = full.resolvedStyle.width;
                float hostH = full.resolvedStyle.height;
                if (hostW < 8f) hostW = full.worldBound.width;
                if (hostH < 8f) hostH = full.worldBound.height;
                if (hostW < 8f || hostH < 8f) return;
                if (Mathf.Abs(hostW - fitW) < 0.5f && Mathf.Abs(hostH - fitH) < 0.5f) return;
                fitW = hostW;
                fitH = hostH;
                // Under the gear when the rows fit; otherwise the top of the screen, so a 390-tall
                // landscape still keeps Close inside the window. The list scrolls; Close stays pinned.
                const float edge = 8f;
                const float chrome = 16f + 24f + 44f;
                int rows = 0;
                list.Query<Button>().ForEach(b =>
                {
                    if (b.name != null && b.name.StartsWith("home-")) rows++;
                });
                float content = chrome + rows * 46f;
                float roomBelowGear = hostH - 52f - edge;
                float top = content <= roomBelowGear ? 52f : edge;
                float maxH = Mathf.Max(120f, hostH - top - edge);
                float height = Mathf.Min(content, maxH);
                float width = Mathf.Min(320f, Mathf.Max(200f, hostW - 16f));
                float left = Mathf.Max(edge, hostW - width - edge);
                sheet.style.position = Position.Absolute;
                sheet.style.top = top;
                sheet.style.left = left;
                sheet.style.right = StyleKeyword.Auto;
                sheet.style.width = width;
                sheet.style.height = height;
                sheet.style.maxHeight = height;
                sheet.style.paddingBottom = 60f;
                float listH = Mathf.Max(46f, height - chrome);
                list.style.height = listH;
                list.style.maxHeight = listH;
                list.style.minHeight = 0f;
                float inner = Mathf.Max(120f, width - 24f);
                list.Query<Button>().ForEach(b =>
                {
                    if (b.name != null && b.name.StartsWith("home-")) b.style.width = inner;
                });
            }

            var gear = new Button { name = "home-settings", text = string.Empty, tooltip = "Settings. Sound, motion, accessibility." };
            gear.AddToClassList("kh-button"); gear.AddToClassList("kh-mm-gear"); gear.AddToClassList("kh-mm-anim");
            var mark = new HomeGlyph("gear");
            mark.style.width = 22; mark.style.height = 22;
            gear.Add(mark);
            gear.style.width = HomeCornerLayout.Gear;
            gear.style.height = HomeCornerLayout.Gear;
            gear.style.minWidth = HomeCornerLayout.Gear;
            gear.style.minHeight = HomeCornerLayout.Gear;
            gear.clicked += () =>
            {
                PaintMix();
                PaintSubs();
                PaintSandbox();
                PaintMotion();
                sheet.style.display = DisplayStyle.Flex;
                fitW = -1f;
                FitSheet();
                sheet.BringToFront();
            };
            if (full != null)
            {
                full.Add(gear);
                full.Add(sheet);
            }
        }

        static void PaintToggle(Button b, string label, bool muted)
        {
            PaintCaption(b, AudioPrefs.Caption(label, muted), !muted);
            b.tooltip = label + ". " + b.text + ".";
        }

        static void PaintCaption(Button b, string caption, bool on)
        {
            if (b == null) return;
            b.text = caption;
            var state = b.Q<Label>(b.name + "-state");
            if (state != null) state.text = on ? "On" : "Off";
            b.style.backgroundColor = on
                ? new Color(0.15f, 0.40f, 0.32f, 1f)
                : new Color(0.10f, 0.14f, 0.22f, 1f);
            b.style.color = new Color(0.96f, 0.93f, 0.86f, 1f);
        }

        static Button SettingButton(string name, string label)
        {
            var b = new Button { name = name, text = label };
            b.AddToClassList("kh-button");
            b.style.minHeight = 44;
            b.style.height = StyleKeyword.Auto;
            b.style.fontSize = 14;
            b.style.whiteSpace = WhiteSpace.NoWrap;
            b.style.overflow = Overflow.Visible;
            b.style.unityTextAlign = TextAnchor.MiddleLeft;
            b.style.alignSelf = Align.Stretch;
            b.style.flexShrink = 0;
            b.style.marginTop = 0;
            b.style.marginBottom = 2;
            b.style.paddingLeft = 10;
            b.style.paddingRight = 56;
            var state = new Label("On") { name = name + "-state", pickingMode = PickingMode.Ignore };
            state.style.position = Position.Absolute;
            state.style.right = 8;
            state.style.top = 8;
            state.style.width = 40;
            state.style.height = 28;
            state.style.fontSize = 14;
            state.style.whiteSpace = WhiteSpace.NoWrap;
            state.style.unityTextAlign = TextAnchor.MiddleCenter;
            state.style.color = Color.white;
            state.style.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 0.55f);
            SettingFont(state, "SpaceGrotesk-Bold");
            b.Add(state);
            SettingFont(b, "SpaceGrotesk-Medium");
            return b;
        }

        static void SettingFont(VisualElement el, string fontName)
        {
            if (el == null) return;
            var font = Resources.Load<Font>(MainMenuFonts.Dir + fontName);
            if (font == null) return;
            el.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
        }

        static void OpenCodexOrSoon(VisualElement full)
        {
            if (OpenCodex != null) OpenCodex();
            else ShowNote(full, "home-codex-soon", "Codex", "Coming soon.");
        }

        static void ShowNote(VisualElement host, string name, string title, string body)
        {
            if (host == null) return;
            var note = host.Q(name);
            if (note != null)
            {
                bool open = note.style.display.value != DisplayStyle.None;
                note.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
                if (!open) note.BringToFront();
                return;
            }
            note = new VisualElement { name = name };
            note.style.position = Position.Absolute;
            note.style.left = 12;
            note.style.right = 12;
            note.style.top = 64;
            note.style.paddingLeft = 12;
            note.style.paddingRight = 12;
            note.style.paddingTop = 12;
            note.style.paddingBottom = 12;
            note.style.backgroundColor = new Color(0.06f, 0.09f, 0.16f, 0.96f);
            note.style.flexDirection = FlexDirection.Column;
            var head = new Label(title) { pickingMode = PickingMode.Ignore };
            head.style.fontSize = 16;
            head.style.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            head.style.whiteSpace = WhiteSpace.Normal;
            var copy = new Label(body) { name = name + "-body", pickingMode = PickingMode.Ignore };
            copy.style.fontSize = 14;
            copy.style.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            copy.style.whiteSpace = WhiteSpace.Normal;
            copy.style.marginTop = 6;
            var close = new Button { name = name + "-close", text = "Close" };
            close.style.minHeight = 44;
            close.style.fontSize = 14;
            close.style.marginTop = 8;
            close.clicked += () => note.style.display = DisplayStyle.None;
            note.Add(head);
            note.Add(copy);
            note.Add(close);
            host.Add(note);
            note.BringToFront();
        }

        static Button Tile(MainMenuCopy.Item it, Action pressed)
        {
            bool open = it.Live || pressed != null;
            var b = new Button(open ? pressed : null) { name = "home-" + it.Id, tooltip = it.Spoken };
            b.AddToClassList("kh-button"); b.AddToClassList("kh-mm-tile"); b.AddToClassList("kh-mm-anim");
            if (open) b.AddToClassList("kh-mm-tile--focus"); else b.AddToClassList("kh-mm-tile--soon");
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; head.AddToClassList("kh-mm-tile-head");
            var icon = new HomeGlyph(it.Id == "codex" ? "book" : "orbit");
            icon.style.width = 18; icon.style.height = 18; icon.style.marginRight = 6;
            var name = new Label(it.Id == "codex" ? "Codex" : "Missions") { pickingMode = PickingMode.Ignore }; name.AddToClassList("kh-mm-tile-name");
            head.Add(icon); head.Add(name);
            if (it.Live)
            {
                var lamp = new VisualElement { name = "lamp", pickingMode = PickingMode.Ignore }; lamp.AddToClassList("kh-mm-lamp"); lamp.AddToClassList("kh-mm-lamp--on"); head.Add(lamp);
            }
            var cap = new Label(it.Caption) { pickingMode = PickingMode.Ignore }; cap.AddToClassList("kh-mm-tile-caption");
            b.Add(head); b.Add(cap);
            b.text = string.Empty;
            b.SetEnabled(open);
            return b;
        }

        /// <summary>Applies MenuTypeScale to every piece of menu type and the heights derived from it. Inline styles,
        /// so they win over the USS defaults (which stay as the pre-layout fallback).</summary>
        public static void ApplyScale(VisualElement root, MenuTypeScale.Sizes z)
        {
            void Font(string cls, float size, float trackEm)
            {
                root.Query(className: cls).ForEach(e =>
                {
                    float use = TextFit.Cap(e, size);
                    e.style.fontSize = use;
                    e.style.letterSpacing = use * trackEm;
                });
            }
            Font("kh-mm-kestrel", z.Title, .04f);
            Font("kh-mm-heavy", z.Tagline, .5f);
            Font("kh-mm-launch-label", z.LaunchLabel, .10f);
            Font("kh-mm-launch-caption", z.LaunchCaption, 0f);
            Font("kh-mm-tile-caption", z.TileCaption, 0f);
            Font("kh-mm-soon", z.Soon, .15f);
            Font("kh-mm-hint", z.Hint, 0f);
            Font("kh-mm-eyebrow", Mathf.Max(14f, z.Hint), .12f);
            Font("kh-mm-design-label", z.DesignLabel > 1f ? z.DesignLabel : Mathf.Max(14f, z.LaunchLabel * 0.72f), .03f);
            Font("kh-mm-design-caption", Mathf.Max(14f, z.LaunchCaption), 0f);
            var designLabel = root.Q(className: "kh-mm-design-label");
            if (designLabel != null)
            {
                designLabel.style.whiteSpace = WhiteSpace.Normal;
                designLabel.style.textOverflow = TextOverflow.Clip;
            }
            var designCaption = root.Q(className: "kh-mm-design-caption");
            if (designCaption != null)
            {
                designCaption.style.whiteSpace = WhiteSpace.Normal;
                designCaption.style.textOverflow = TextOverflow.Clip;
            }
            Font("kh-mm-thrum-name", Mathf.Max(14f, z.Hint), 0f);
            // Explicit heights (not min-height) so the block is exactly what MenuTypeScale computed; Unity cannot inflate it.
            // The title must not flex-shrink before ContainColumn measures it, or a short column reads height 0 and never caps.
            root.style.paddingTop = MenuTypeScale.TopPad; root.style.paddingBottom = z.HintBand;
            var title = root.Q("mm-title");
            if (title != null) title.style.flexShrink = 0;
            var stats = root.Q("mm-stats");
            if (stats != null)
            {
                stats.style.height = z.StatsH; stats.style.minHeight = z.StatsH; stats.style.flexShrink = 0;
                stats.style.marginBottom = MenuTypeScale.Gap;
            }
            root.Query(className: "kh-mm-stat").ForEach(e => e.style.fontSize = z.StatFont);
            var launch = root.Q("home-launch");
            if (launch != null)
            {
                launch.style.height = z.LaunchH; launch.style.minHeight = z.LaunchH; launch.style.flexShrink = 0;
                launch.style.marginTop = 0; launch.style.marginBottom = MenuTypeScale.Gap;
                float padH = MenuTypeScale.Clamp(14f * z.Scale, 10f, 18f), padV = MenuTypeScale.Clamp(8f * z.Scale, 6f, 10f);
                launch.style.paddingLeft = padH; launch.style.paddingRight = padH; launch.style.paddingTop = padV; launch.style.paddingBottom = padV;
            }
            var design = root.Q("home-design");
            if (design != null)
            {
                design.style.height = z.DesignH; design.style.minHeight = z.DesignH; design.style.flexShrink = 0;
                design.style.marginTop = 0; design.style.marginBottom = MenuTypeScale.Gap;
                design.style.paddingLeft = 12f; design.style.paddingRight = 12f;
                design.style.paddingTop = 8f; design.style.paddingBottom = 8f;
            }
            var hintLine = root.Q("mm-hint-line");
            if (hintLine != null)
            {
                hintLine.style.whiteSpace = WhiteSpace.Normal;
                hintLine.style.textOverflow = TextOverflow.Clip;
            }
            foreach (var it in MainMenuCopy.Tiles)
            {
                var b = root.Q("home-" + it.Id); if (b == null) continue;
                b.style.height = z.TileH; b.style.minHeight = z.TileH; b.style.flexShrink = 0;
                b.style.marginTop = 0; b.style.marginBottom = MenuTypeScale.Gap;
                b.style.paddingLeft = z.Pad; b.style.paddingRight = z.Pad; b.style.paddingTop = z.Pad; b.style.paddingBottom = z.Pad;
                var head = b.Q(className: "kh-mm-tile-head");
                if (head != null) head.style.minHeight = Mathf.Max(22f, z.TileName * MenuTypeScale.LineH);
                var cap = b.Q(className: "kh-mm-tile-caption");
                if (cap != null) { cap.style.marginTop = 3f; cap.style.marginLeft = 0; cap.style.marginRight = 0; cap.style.paddingLeft = 0; cap.style.paddingRight = 0; }
                var name = b.Q(className: "kh-mm-tile-name");
                if (name != null)
                {
                    float n = MenuTypeScale.FitName(z, it.Label, !it.Live);
                    name.style.fontSize = n; name.style.letterSpacing = n * .03f;
                    // No Label chrome and no shrink: the width was budgeted from the font metrics (with NameSlack).
                    name.style.marginLeft = 0; name.style.marginRight = 0; name.style.paddingLeft = 0; name.style.paddingRight = 0;
                    name.style.flexShrink = 0; name.style.textOverflow = TextOverflow.Clip;
                }
                var soon = b.Q(className: "kh-mm-soon");
                if (soon != null) { soon.style.marginLeft = 4f; soon.style.marginTop = 0; soon.style.marginBottom = 0; soon.style.paddingLeft = 4f; soon.style.paddingRight = 4f; soon.style.paddingTop = 0; soon.style.paddingBottom = 0; }
            }
        }

        /// <summary>The hero lift from where the title block and the LAUNCH bar really ended up (layout rects, no intro translate);
        /// the estimate from MenuTypeScale is used until the first layout and as a floor.</summary>
        static float MeasureLift(VisualElement root, VisualElement safe, VisualElement full, MenuTypeScale.Sizes z, float h, float topInset)
        {
            float controlsTop = z.ControlsTop, titleBottom = z.TitleBottom;
            var stats = root.Q("mm-stats"); var launch = root.Q("home-launch"); var title = root.Q("mm-title");
            float baseY = topInset + root.layout.y;
            var topControl = stats != null && stats.layout.height > 1f ? stats : launch;
            if (topControl != null && topControl.layout.height > 1f) controlsTop = Mathf.Min(controlsTop, baseY + topControl.layout.yMin);
            if (title != null && title.layout.height > 1f) titleBottom = Mathf.Max(titleBottom, baseY + title.layout.yMax);
            return MenuTypeScale.HeroLift(h, titleBottom, controlsTop);
        }

        /// <summary>Intro per motion-spec.md: elements start hidden (kh-mm-anim) and get kh-mm-in on a staggered schedule.
        /// Reduce Motion: shown at once, transitions are zeroed by kh-mm-reduce.</summary>
        public static void PlayIntro(VisualElement root, VisualElement full, bool reduceMotion)
        {
            Action<VisualElement, string> show = (el, id) =>
            {
                if (el == null) return;
                if (reduceMotion) el.AddToClassList("kh-mm-in");
                else el.schedule.Execute(() => el.AddToClassList("kh-mm-in")).StartingIn(MainMenuCopy.IntroDelayMs(id));
            };
            show(root.Q("mm-title"), "title");
            show(root.Q("mm-stats"), "stats");
            show(root.Q("home-launch"), "launch");
            show(root.Q("home-design"), "design");
            foreach (var it in MainMenuCopy.Tiles) show(root.Q("home-" + it.Id), it.Id);
            show(root.Q("mm-hint"), "codex");
            if (full != null) show(full.Q("home-settings"), "gear");
            if (!reduceMotion) Breathe(root.Q("mm-launch-icon"));
        }

        /// <summary>LAUNCH glyph pulses on opacity over 2.4s. Transform-free, and skipped when motion is reduced.</summary>
        static void Breathe(VisualElement icon)
        {
            if (icon == null) return;
            float t0 = Time.realtimeSinceStartup;
            icon.schedule.Execute(() =>
            {
                float phase = ((Time.realtimeSinceStartup - t0) % 2.4f) / 2.4f;
                float s = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
                icon.style.opacity = 0.62f + 0.38f * s;
            }).Every(48);
        }

        /// <summary>Film-reel bar: dark band with sprocket holes. Painter2D, static.</summary>
        public sealed class Reel : VisualElement
        {
            public Reel() { pickingMode = PickingMode.Ignore; generateVisualContent += Draw; }
            void Draw(MeshGenerationContext ctx)
            {
                var r = contentRect; if (r.width <= 0 || r.height <= 0) return;
                var p = ctx.painter2D; p.fillColor = new Color32(0x28, 0x24, 0x28, 255);
                float hole = Mathf.Clamp(r.height * 0.16f, 6f, 16f), pitch = hole * 2.6f, y = (r.height - hole) * 0.5f;
                if (r.height < hole * 2f) return;
                y = ClassListContains("kh-mm-bar-top") ? Mathf.Min(y, r.height - hole - 4f) : Mathf.Max(y, 4f);
                for (float x = pitch * 0.5f; x < r.width; x += pitch)
                {
                    p.BeginPath(); p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(x + hole, y)); p.LineTo(new Vector2(x + hole, y + hole * 0.8f)); p.LineTo(new Vector2(x, y + hole * 0.8f)); p.ClosePath(); p.Fill();
                }
            }
        }
    }
}
