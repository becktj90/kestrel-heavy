// UI Toolkit layer for the retro-cinematic main menu ("mix", Trevor's pick). It sits over the live 3D hangar
// scene (MainMenuController keeps the rocket, hangar and telemetry; the overlay replaces the 3D title, launch
// bar and glass rows with 2D controls that match the mockup). Static classes + USS, no bitmaps.
// Motion follows motion-spec.md where cheap: staggered intro by class toggle + USS transition, :active press,
// :focus ring. Reduce Motion: no intro, no transitions (root gets kh-mm-reduce).
using System;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class MainMenuOverlay
    {
        public const string RootName = "mm-root";

        /// <summary>Builds the overlay. <paramref name="safe"/> holds the controls (safe-area inset);
        /// <paramref name="full"/> holds the full-bleed bars and grain. Returns the root parented in <paramref name="safe"/>.</summary>
        public static VisualElement Build(VisualElement full, VisualElement safe, Action launch, Action design, Action missions, bool reduceMotion, string missionLine = null)
        {
            MainMenuLayout.HeroLift = 0f;
            var root = new VisualElement { name = RootName, pickingMode = PickingMode.Ignore };
            root.AddToClassList("kh-mm");
            if (reduceMotion) root.AddToClassList("kh-mm-reduce");

            // Full-bleed: grain + reel bars (decorative, never pick).
            var grain = new VisualElement { name = "mm-grain", pickingMode = PickingMode.Ignore };
            grain.AddToClassList("kh-mm-full"); grain.AddToClassList("kh-home-grain");
            grain.style.backgroundImage = new StyleBackground(HomeBackdrop.GrainTexture());
            grain.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            grain.style.backgroundSize = new BackgroundSize(new Length(HomeArt.GrainSize), new Length(HomeArt.GrainSize));
            var top = new Reel { name = "mm-bar-top" }; top.AddToClassList("kh-mm-bar"); top.AddToClassList("kh-mm-bar-top");
            var bottom = new Reel { name = "mm-bar-bottom" }; bottom.AddToClassList("kh-mm-bar"); bottom.AddToClassList("kh-mm-bar-bottom");
            var layer = new VisualElement { name = "mm-full", pickingMode = PickingMode.Ignore };
            layer.AddToClassList("kh-mm-full");
            layer.Add(grain); layer.Add(top); layer.Add(bottom);
            full.Add(layer);

            // Title card.
            var title = new VisualElement { name = "mm-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("kh-mm-title-card"); title.AddToClassList("kh-mm-anim");
            var kestrel = new Label(MainMenuCopy.TitleTop) { name = "mm-kestrel", pickingMode = PickingMode.Ignore };
            kestrel.AddToClassList("kh-mm-kestrel");
            var rule = new VisualElement { pickingMode = PickingMode.Ignore }; rule.AddToClassList("kh-mm-rule-row");
            var l = new VisualElement { pickingMode = PickingMode.Ignore }; l.AddToClassList("kh-mm-rule");
            var heavy = new Label(MainMenuCopy.TitleBottom) { name = "mm-heavy", pickingMode = PickingMode.Ignore };
            heavy.AddToClassList("kh-mm-heavy");
            var r = new VisualElement { pickingMode = PickingMode.Ignore }; r.AddToClassList("kh-mm-rule");
            rule.Add(l); rule.Add(heavy); rule.Add(r);
            title.Add(kestrel); title.Add(rule);
            root.Add(title);

            var spacer = new VisualElement { pickingMode = PickingMode.Ignore }; spacer.style.flexGrow = 1; root.Add(spacer);

            // Controls: tidy stats, the big LAUNCH, a smaller Design first, then Missions and Codex.
            var stats = StatsStrip();
            root.Add(stats);
            var launchKey = LaunchBar(launch, missionLine);
            root.Add(launchKey);
            var designKey = DesignBar(design);
            root.Add(designKey);
            var grid = new VisualElement { name = "mm-grid", pickingMode = PickingMode.Ignore };
            grid.AddToClassList("kh-mm-grid");
            foreach (var it in MainMenuCopy.Tiles) grid.Add(Tile(it, it.Id == "missions" ? missions : null));
            root.Add(grid);
            Gear(full);

            var hint = new Label(MainMenuCopy.TapHint) { name = "mm-hint", pickingMode = PickingMode.Ignore };
            hint.AddToClassList("kh-mm-hint");
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
                ApplyWebWide(root, WebLayoutMode.UseWide(Screen.width, Screen.height));
                top.style.height = MainMenuCopy.BarHeight(h, topInset);
                bottom.style.height = MainMenuCopy.BarHeight(h, botInset);
                MainMenuLayout.HeroLift = MeasureLift(root, safe, full, z, h, topInset);
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

        /// <summary>Landscape web: rocket stays in the centre, menu tiles sit in a column beside it.
        /// Portrait and iOS leave the phone column from ApplyScale.</summary>
        public static void ApplyWebWide(VisualElement root, bool wide)
        {
            if (root == null) return;
            root.EnableInClassList("kh-mm-wide", wide);
            if (!wide) { ClearWebWide(root); return; }
            float rw = root.resolvedStyle.width;
            float rh = root.resolvedStyle.height;
            if (rw < 8f || rh < 8f) return;
            WebViewport.Css(out float cssW, out float cssH);
            if (cssW < 8f || cssH < 8f) { cssW = rw; cssH = rh; }
            var f = WebHomeLayout.Place(cssW, cssH, 0f, 0f, 0f, 0f);
            float ux = rw / cssW;
            float uy = rh / cssH;
            HudBox Sc(HudBox b) => new HudBox { X = b.X * ux, Y = b.Y * uy, W = b.W * ux, H = b.H * uy };
            f.Title = Sc(f.Title); f.Stats = Sc(f.Stats); f.Launch = Sc(f.Launch); f.Design = Sc(f.Design);
            f.Missions = Sc(f.Missions); f.Codex = Sc(f.Codex); f.Menu = Sc(f.Menu); f.Hint = Sc(f.Hint);
            f.Rocket = Sc(f.Rocket);
            if (!f.Wide) { ClearWebWide(root); return; }
            root.style.paddingTop = 0;
            root.style.paddingBottom = 0;
            Pin(root.Q("mm-title"), f.Title);
            Pin(root.Q("mm-stats"), f.Stats);
            Pin(root.Q("home-launch"), f.Launch);
            Pin(root.Q("home-design"), f.Design);
            var grid = root.Q("mm-grid");
            if (grid != null)
            {
                float top = f.Missions.Y;
                float bottom = Mathf.Max(f.Missions.Bottom, f.Codex.Bottom);
                Pin(grid, new HudBox { X = f.Menu.X, Y = top, W = f.Menu.W, H = Mathf.Max(1f, bottom - top) });
                grid.style.flexDirection = FlexDirection.Column;
                grid.style.flexWrap = Wrap.NoWrap;
            }
            foreach (var it in MainMenuCopy.Tiles)
            {
                var tile = root.Q("home-" + it.Id);
                if (tile == null) continue;
                tile.style.width = Length.Percent(100);
                tile.style.position = Position.Relative;
            }
            Pin(root.Q("mm-hint"), f.Hint);
        }

        static void Pin(VisualElement e, HudBox b)
        {
            if (e == null || !b.Live) return;
            e.style.position = Position.Absolute;
            e.style.left = b.X;
            e.style.top = b.Y;
            e.style.width = b.W;
            e.style.height = b.H;
            e.style.right = StyleKeyword.Auto;
            e.style.bottom = StyleKeyword.Auto;
            e.style.marginTop = 0;
            e.style.marginBottom = 0;
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
            }
            Drop(root.Q("mm-title"));
            Drop(root.Q("mm-stats"));
            Drop(root.Q("home-launch"));
            Drop(root.Q("home-design"));
            Drop(root.Q("mm-hint"));
            var grid = root.Q("mm-grid");
            Drop(grid);
            if (grid != null)
            {
                grid.style.flexDirection = StyleKeyword.Null;
                grid.style.flexWrap = StyleKeyword.Null;
            }
            foreach (var it in MainMenuCopy.Tiles) Drop(root.Q("home-" + it.Id));
        }

        /// <summary>Shows the real next mission on the LAUNCH bar when Career knows it. The static caption stays the fallback.</summary>
        public static void SetMissionLine(VisualElement root, string line)
        {
            if (root == null || string.IsNullOrEmpty(line)) return;
            var cap = root.Q<Label>("mm-launch-caption");
            if (cap != null) cap.text = line;
        }

        static VisualElement StatsStrip()
        {
            var row = new VisualElement { name = "mm-stats", pickingMode = PickingMode.Ignore };
            row.AddToClassList("kh-mm-stats"); row.AddToClassList("kh-mm-anim");
            row.Add(Stat("mm-fuel", MainMenuCopy.FuelStat));
            row.Add(Stat("mm-thrust", MainMenuCopy.ThrustStat));
            return row;
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
            b.Add(lamp); b.Add(col);
            b.text = string.Empty;
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
            b.Add(lamp); b.Add(col);
            b.text = string.Empty;
            return b;
        }

        static void Gear(VisualElement full)
        {
            var sheet = new VisualElement { name = "home-settings-sheet" };
            sheet.AddToClassList("kh-mm-sheet");
            sheet.style.display = DisplayStyle.None;
            var title = new Label("Settings") { pickingMode = PickingMode.Ignore };
            title.AddToClassList("kh-mm-sheet-title");
            var motion = new Button { name = "home-reduce-motion", text = "Reduce motion" };
            motion.AddToClassList("kh-button");
            motion.tooltip = "Reduce motion. Holds the menu still.";
            motion.style.minHeight = 44;
            motion.clicked += () =>
            {
                int cur = PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0);
                PlayerPrefs.SetInt(MainMenuLayout.ReduceMotionPref, cur == 0 ? 1 : 0);
                PlayerPrefs.Save();
                motion.text = PlayerPrefs.GetInt(MainMenuLayout.ReduceMotionPref, 0) != 0 ? "Reduce motion on" : "Reduce motion";
            };
            var close = new Button { name = "home-settings-close", text = "Close" };
            close.AddToClassList("kh-button");
            close.tooltip = "Close settings.";
            close.style.minHeight = 44;
            close.clicked += () => sheet.style.display = DisplayStyle.None;
            sheet.Add(title); sheet.Add(motion); sheet.Add(close);

            var gear = new Button { name = "home-settings", text = "Set", tooltip = "Settings. Sound, motion, accessibility." };
            gear.AddToClassList("kh-button"); gear.AddToClassList("kh-mm-gear"); gear.AddToClassList("kh-mm-anim");
            gear.style.width = HomeCornerLayout.Gear;
            gear.style.height = HomeCornerLayout.Gear;
            gear.style.minWidth = HomeCornerLayout.Gear;
            gear.style.minHeight = HomeCornerLayout.Gear;
            gear.clicked += () => sheet.style.display = DisplayStyle.Flex;
            full.Add(gear);
            full.Add(sheet);
        }

        static Button Tile(MainMenuCopy.Item it, Action pressed)
        {
            var b = new Button(it.Live ? pressed : null) { name = "home-" + it.Id, tooltip = it.Spoken };
            b.AddToClassList("kh-button"); b.AddToClassList("kh-mm-tile"); b.AddToClassList("kh-mm-anim");
            if (it.Live) b.AddToClassList("kh-mm-tile--focus"); else b.AddToClassList("kh-mm-tile--soon");
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; head.AddToClassList("kh-mm-tile-head");
            var name = new Label(it.Label) { pickingMode = PickingMode.Ignore }; name.AddToClassList("kh-mm-tile-name");
            head.Add(name);
            if (it.Live)
            {
                var lamp = new VisualElement { name = "lamp", pickingMode = PickingMode.Ignore }; lamp.AddToClassList("kh-mm-lamp"); lamp.AddToClassList("kh-mm-lamp--on"); head.Add(lamp);
            }
            if (!it.Live)
            {
                // Own slot in the title row, so it can never overlap the caption below.
                var tag = new Label(MainMenuCopy.SoonTag) { name = "soon", pickingMode = PickingMode.Ignore }; tag.AddToClassList("kh-mm-soon"); head.Add(tag);
            }
            var cap = new Label(it.Caption) { pickingMode = PickingMode.Ignore }; cap.AddToClassList("kh-mm-tile-caption");
            b.Add(head); b.Add(cap);
            b.text = string.Empty;
            b.SetEnabled(it.Live);
            return b;
        }

        /// <summary>Applies MenuTypeScale to every piece of menu type and the heights derived from it. Inline styles,
        /// so they win over the USS defaults (which stay as the pre-layout fallback).</summary>
        public static void ApplyScale(VisualElement root, MenuTypeScale.Sizes z)
        {
            void Font(string cls, float size, float trackEm)
            {
                root.Query(className: cls).ForEach(e => { e.style.fontSize = size; e.style.letterSpacing = size * trackEm; });
            }
            Font("kh-mm-kestrel", z.Title, .04f);
            Font("kh-mm-heavy", z.Tagline, .5f);
            Font("kh-mm-launch-label", z.LaunchLabel, .10f);
            Font("kh-mm-launch-caption", z.LaunchCaption, 0f);
            Font("kh-mm-tile-caption", z.TileCaption, 0f);
            Font("kh-mm-soon", z.Soon, .15f);
            Font("kh-mm-hint", z.Hint, .18f);
            // Explicit heights (not min-height) so the block is exactly what MenuTypeScale computed; Unity cannot inflate it.
            root.style.paddingTop = MenuTypeScale.TopPad; root.style.paddingBottom = z.HintBand;
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
            if (full != null) show(full.Q("home-settings"), "gear");
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
