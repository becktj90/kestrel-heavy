// UI Toolkit layer for the retro-cinematic main menu ("mix", Trevor's pick). It sits over the live 3D hangar
// scene (MainMenuController keeps the rocket, hangar and telemetry; the overlay replaces the 3D title, launch
// bar and glass rows with 2D controls that match the mockup). Static classes + USS, no bitmaps.
// Motion follows motion-spec.md where cheap: staggered intro by class toggle + USS transition, :active press,
// :focus ring. Reduce Motion: no intro, no transitions (root gets kh-mm-reduce).
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class MainMenuOverlay
    {
        public const string RootName = "mm-root";

        /// <summary>Builds the overlay. <paramref name="safe"/> holds the controls (safe-area inset);
        /// <paramref name="full"/> holds the full-bleed bars and grain. Returns the root parented in <paramref name="safe"/>.</summary>
        public static VisualElement Build(VisualElement full, VisualElement safe, Action launch, Action hangar, bool reduceMotion)
        {
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
            FitKestrel(kestrel);
            root.Add(title);

            var spacer = new VisualElement { pickingMode = PickingMode.Ignore }; spacer.style.flexGrow = 1; root.Add(spacer);

            // Controls: LAUNCH bar, then the 2x2 tiles.
            var launchKey = LaunchBar(launch);
            root.Add(launchKey);
            var grid = new VisualElement { name = "mm-grid", pickingMode = PickingMode.Ignore };
            grid.AddToClassList("kh-mm-grid");
            foreach (var it in MainMenuCopy.Tiles) grid.Add(Tile(it, hangar));
            root.Add(grid);

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
                root.EnableInClassList("kh-mm-narrow", MainMenuCopy.IsNarrow(full.resolvedStyle.width));
                float topInset = Mathf.Max(0f, safe.worldBound.yMin - full.worldBound.yMin);
                float botInset = Mathf.Max(0f, full.worldBound.yMax - safe.worldBound.yMax);
                top.style.height = MainMenuCopy.BarHeight(h, topInset);
                bottom.style.height = MainMenuCopy.BarHeight(h, botInset);
            });
            PlayIntro(root, reduceMotion);
            return root;
        }

        static Button LaunchBar(Action launch)
        {
            var b = new Button(launch) { name = "home-launch", tooltip = MainMenuCopy.Launch.Spoken };
            b.AddToClassList("kh-button"); b.AddToClassList("kh-mm-launch"); b.AddToClassList("kh-mm-anim");
            var lamp = new VisualElement { name = "lamp", pickingMode = PickingMode.Ignore }; lamp.AddToClassList("kh-mm-lamp"); lamp.AddToClassList("kh-mm-lamp--on");
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.AddToClassList("kh-mm-col");
            var label = new Label(MainMenuCopy.Launch.Label) { pickingMode = PickingMode.Ignore }; label.AddToClassList("kh-mm-launch-label");
            var cap = new Label(MainMenuCopy.Launch.Caption) { pickingMode = PickingMode.Ignore }; cap.AddToClassList("kh-mm-launch-caption");
            col.Add(label); col.Add(cap);
            b.Add(lamp); b.Add(col);
            b.text = string.Empty;
            return b;
        }

        static Button Tile(MainMenuCopy.Item it, Action hangar)
        {
            var b = new Button(it.Live && it.Id == "hangar" ? hangar : null) { name = "home-" + it.Id, tooltip = it.Spoken };
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

        static void FitKestrel(Label k)
        {
            float applied = -1f, at100 = -1f; bool busy = false;
            k.style.fontSize = 100f;
            k.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (busy) return;
                float avail = k.contentRect.width - 4f; if (avail <= 4f) return;
                if (at100 <= 0f)
                {
                    if (k.resolvedStyle.fontSize < 99f) return;   // wait until the 100px start size is resolved
                    at100 = k.MeasureTextSize(MainMenuCopy.TitleTop, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                    if (at100 <= 0f) return;
                }
                float size = MainMenuCopy.FitTitle(avail, at100);
                if (Mathf.Abs(size - applied) < 0.5f) return;
                busy = true; applied = size; k.style.fontSize = size; k.style.letterSpacing = size * 0.04f; busy = false;
            });
        }

        /// <summary>Intro per motion-spec.md: elements start hidden (kh-mm-anim) and get kh-mm-in on a staggered schedule.
        /// Reduce Motion: shown at once, transitions are zeroed by kh-mm-reduce.</summary>
        public static void PlayIntro(VisualElement root, bool reduceMotion)
        {
            Action<VisualElement, string> show = (el, id) =>
            {
                if (el == null) return;
                if (reduceMotion) el.AddToClassList("kh-mm-in");
                else el.schedule.Execute(() => el.AddToClassList("kh-mm-in")).StartingIn(MainMenuCopy.IntroDelayMs(id));
            };
            show(root.Q("mm-title"), "title");
            show(root.Q("home-launch"), "launch");
            foreach (var it in MainMenuCopy.Tiles) show(root.Q("home-" + it.Id), it.Id);
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
