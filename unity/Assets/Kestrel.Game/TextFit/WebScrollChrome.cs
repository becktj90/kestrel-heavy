// Web-only scroll chrome (#416 item 8). Hides the grey desktop scroller on every vertical ScrollView
// under a root and adds a bottom fade while more content sits below. Wheel, drag and touch scrolling
// are untouched. Sweeps periodically so lists built later (screens, sheets, help) are covered too.
using Kestrel.Core.Feel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class WebScrollChrome
    {
        public const string DressedClass = "kh-web-scroll";
        public const string FadeName = "kh-scroll-fade";

        /// <summary>Test / capture seam: true dresses scrollers outside the WebGL player.</summary>
        public static bool? Force;

        public static bool Active => Force ?? Application.platform == RuntimePlatform.WebGLPlayer;

        static readonly Color Ink = new Color(8f / 255f, 12f / 255f, 20f / 255f, 1f);

        /// <summary>Keeps every ScrollView under <paramref name="root"/> dressed. No-op off the web.</summary>
        public static void Watch(VisualElement root)
        {
            if (root == null || !Active) return;
            Sweep(root);
            root.schedule.Execute(() => Sweep(root)).Every(300);
        }

        public static void Sweep(VisualElement root)
        {
            if (root == null) return;
            root.Query<ScrollView>().ForEach(Dress);
        }

        public static void Dress(ScrollView sv)
        {
            if (sv == null) return;
            var bar = sv.verticalScroller;
            if (bar != null)
            {
                bar.style.width = 0f;
                bar.style.minWidth = 0f;
                bar.style.maxWidth = 0f;
                // The default theme gives the scroller a 1 px side border, which keeps a 1 px grey line.
                bar.style.borderLeftWidth = 0f;
                bar.style.borderRightWidth = 0f;
                bar.style.paddingLeft = 0f;
                bar.style.paddingRight = 0f;
                bar.style.marginLeft = 0f;
                bar.style.marginRight = 0f;
                bar.style.opacity = 0f;
            }
            if (sv.ClassListContains(DressedClass)) { Refresh(sv); return; }
            sv.AddToClassList(DressedClass);
            // A click-through clip (the guide's body) never fades: it is not a list the player scrolls.
            if (sv.pickingMode == PickingMode.Ignore) return;
            var fade = new VisualElement { name = FadeName, pickingMode = PickingMode.Ignore };
            fade.style.position = Position.Absolute;
            fade.style.left = 0f; fade.style.right = 0f; fade.style.bottom = 0f;
            fade.style.height = WebScroll.FadePt;
            fade.generateVisualContent += ctx => Paint(ctx, fade);
            sv.contentViewport.Add(fade);
            if (bar != null) bar.valueChanged += _ => Refresh(sv);
            sv.RegisterCallback<GeometryChangedEvent>(_ => Refresh(sv));
            Refresh(sv);
        }

        static void Refresh(ScrollView sv)
        {
            var fade = sv.contentViewport.Q(FadeName);
            if (fade == null) return;
            bool on = WebScroll.FadeShows(sv.contentContainer.layout.height, sv.contentViewport.layout.height, sv.scrollOffset.y);
            fade.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            fade.userData = Under(sv);
            fade.MarkDirtyRepaint();
        }

        /// <summary>The nearest opaque-ish background behind the list, else the dusk Ink.</summary>
        static Color Under(VisualElement el)
        {
            for (var p = el; p != null; p = p.parent)
            {
                var c = p.resolvedStyle.backgroundColor;
                if (c.a >= 0.5f) return new Color(c.r, c.g, c.b, 1f);
            }
            return Ink;
        }

        static void Paint(MeshGenerationContext ctx, VisualElement fade)
        {
            var r = fade.contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            var c = fade.userData is Color col ? col : Ink;
            var p = ctx.painter2D;
            float band = r.height / WebScroll.FadeBands;
            for (int i = 0; i < WebScroll.FadeBands; i++)
            {
                p.fillColor = new Color(c.r, c.g, c.b, WebScroll.BandAlpha(i));
                p.BeginPath();
                float y = i * band;
                p.MoveTo(new Vector2(0f, y));
                p.LineTo(new Vector2(r.width, y));
                p.LineTo(new Vector2(r.width, y + band + 0.5f));
                p.LineTo(new Vector2(0f, y + band + 0.5f));
                p.ClosePath();
                p.Fill();
            }
        }
    }
}
