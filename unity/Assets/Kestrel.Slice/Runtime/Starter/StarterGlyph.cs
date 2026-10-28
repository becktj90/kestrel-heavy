// Tiny Why? pictograms for the Starter rows (docs/DESIGNER_TIERS_SPEC.md: "a short Why? card with a tiny picture"),
// drawn with Painter2D like HomeGlyph: flame (engine fuel), stages (a stack splitting), fuel (a tank gauge) and
// boosters (a core with two side boosters). Static drawings, no animation (photosensitivity-safe).
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class StarterGlyph : VisualElement
    {
        string kind;
        public Color Ink = new Color(0.94f, 1f, 1f, 1f);

        public StarterGlyph(string kind)
        {
            this.kind = kind;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public string Kind
        {
            get => kind;
            set { if (kind == value) return; kind = value; MarkDirtyRepaint(); }
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 2f || r.height < 2f) return;
            var p = ctx.painter2D;
            p.strokeColor = Ink;
            p.fillColor = Ink;
            p.lineWidth = Mathf.Max(1.5f, r.width * 0.07f);
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            float w = r.width, h = r.height;
            switch (kind)
            {
                case "flame": Flame(p, w, h); break;
                case "stages": Stages(p, w, h); break;
                case "fuel": Fuel(p, w, h); break;
                case "boosters": Boosters(p, w, h); break;
                default: Dot(p, w, h); break;
            }
        }

        static void Rect(Painter2D p, float x0, float y0, float x1, float y1)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x0, y0));
            p.LineTo(new Vector2(x1, y0));
            p.LineTo(new Vector2(x1, y1));
            p.LineTo(new Vector2(x0, y1));
            p.ClosePath();
        }

        static void Flame(Painter2D p, float w, float h)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(w * 0.5f, h * 0.1f));
            p.BezierCurveTo(new Vector2(w * 0.78f, h * 0.38f), new Vector2(w * 0.82f, h * 0.62f), new Vector2(w * 0.5f, h * 0.9f));
            p.BezierCurveTo(new Vector2(w * 0.18f, h * 0.62f), new Vector2(w * 0.22f, h * 0.38f), new Vector2(w * 0.5f, h * 0.1f));
            p.ClosePath();
            p.Stroke();
            p.BeginPath();
            p.MoveTo(new Vector2(w * 0.5f, h * 0.48f));
            p.BezierCurveTo(new Vector2(w * 0.62f, h * 0.6f), new Vector2(w * 0.6f, h * 0.72f), new Vector2(w * 0.5f, h * 0.8f));
            p.BezierCurveTo(new Vector2(w * 0.4f, h * 0.72f), new Vector2(w * 0.38f, h * 0.6f), new Vector2(w * 0.5f, h * 0.48f));
            p.ClosePath();
            p.Fill();
        }

        static void Stages(Painter2D p, float w, float h)
        {
            // Upper stage flying on; the empty lower stage falling away, tilted.
            Rect(p, w * 0.38f, h * 0.08f, w * 0.62f, h * 0.44f);
            p.Stroke();
            p.BeginPath();
            p.MoveTo(new Vector2(w * 0.36f, h * 0.58f));
            p.LineTo(new Vector2(w * 0.6f, h * 0.54f));
            p.LineTo(new Vector2(w * 0.68f, h * 0.92f));
            p.LineTo(new Vector2(w * 0.44f, h * 0.96f));
            p.ClosePath();
            p.Stroke();
        }

        static void Fuel(Painter2D p, float w, float h)
        {
            Rect(p, w * 0.3f, h * 0.1f, w * 0.7f, h * 0.9f);
            p.Stroke();
            Rect(p, w * 0.36f, h * 0.48f, w * 0.64f, h * 0.84f);
            p.Fill();
        }

        static void Boosters(Painter2D p, float w, float h)
        {
            Rect(p, w * 0.4f, h * 0.08f, w * 0.6f, h * 0.92f);
            p.Stroke();
            Rect(p, w * 0.14f, h * 0.4f, w * 0.3f, h * 0.92f);
            p.Fill();
            Rect(p, w * 0.7f, h * 0.4f, w * 0.86f, h * 0.92f);
            p.Fill();
        }

        static void Dot(Painter2D p, float w, float h)
        {
            p.BeginPath();
            p.Arc(new Vector2(w * 0.5f, h * 0.5f), Mathf.Min(w, h) * 0.2f, 0f, 360f);
            p.Fill();
        }
    }
}
