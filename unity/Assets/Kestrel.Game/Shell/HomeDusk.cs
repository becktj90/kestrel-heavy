// Dusk launch-complex backdrop for Home. Layered meshes and strokes, no realtime blur:
// WebGL gets the same baked translucent fills as iOS. Stars drift by transform only.
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public sealed class HomeDuskBackdrop : VisualElement
    {
        public static readonly Color32 Night = new Color32(0x05, 0x08, 0x1A, 255);
        public static readonly Color32 Deep = new Color32(0x0F, 0x17, 0x40, 255);
        public static readonly Color32 Violet = new Color32(0x3A, 0x2A, 0x5E, 255);
        public static readonly Color32 Rose = new Color32(0xC7, 0x51, 0x4A, 255);
        public static readonly Color32 Amber = new Color32(0xFF, 0xB0, 0x5A, 255);
        public static readonly Color32 Cream = new Color32(0xFF, 0xD8, 0x9A, 255);
        public static readonly Color32 RidgeFar = new Color32(0x2A, 0x1C, 0x48, 255);
        public static readonly Color32 RidgeNear = new Color32(0x16, 0x10, 0x2C, 255);
        public static readonly Color32 Ground = new Color32(0x10, 0x0C, 0x20, 255);
        public static readonly Color32 Hull = new Color32(0xF4, 0xF7, 0xFB, 255);
        public static readonly Color32 HullShade = new Color32(0xC5, 0xCC, 0xD8, 255);
        public static readonly Color32 HullLit = new Color32(0xFF, 0xFF, 0xFF, 255);
        public static readonly Color32 Ember = new Color32(0xF2, 0x54, 0x3D, 255);
        public static readonly Color32 Teal = new Color32(0x46, 0xD9, 0xCC, 255);

        readonly VisualElement _stars;
        readonly VisualElement _land;
        Rect _hero;
        bool _reduce;
        IVisualElementScheduledItem _tick;
        static Texture2D _launch;

        public HomeDuskBackdrop()
        {
            name = "mm-dusk";
            pickingMode = PickingMode.Ignore;
            AddToClassList("kh-mm-full");
            generateVisualContent += DrawSky;
            _stars = new VisualElement { name = "mm-stars", pickingMode = PickingMode.Ignore };
            _stars.generateVisualContent += DrawStars;
            _stars.style.position = Position.Absolute;
            _stars.style.left = Length.Percent(-6);
            _stars.style.top = 0;
            _stars.style.bottom = 0;
            _stars.style.width = Length.Percent(112);
            _land = new VisualElement { name = "mm-land", pickingMode = PickingMode.Ignore };
            _land.AddToClassList("kh-mm-full");
            _land.generateVisualContent += DrawLand;
            Add(_stars);
            Add(_land);
            RegisterCallback<AttachToPanelEvent>(_ => Arm());
            RegisterCallback<GeometryChangedEvent>(_ => RepaintAll());
        }

        public void SetHero(Rect hero, bool reduce)
        {
            _hero = hero;
            if (_reduce != reduce)
            {
                _reduce = reduce;
                Arm();
            }
            _land.MarkDirtyRepaint();
        }

        /// <summary>Baked ember fill for the LAUNCH control. No shader blur.</summary>
        public static Texture2D LaunchFill()
        {
            if (_launch != null) return _launch;
            _launch = new Texture2D(4, 64, TextureFormat.RGBA32, false) { name = "KH_LaunchFill", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f;
                var c = t < 0.45f
                    ? Color.Lerp(new Color32(0xFF, 0x7A, 0x52, 255), new Color32(0xF2, 0x54, 0x3D, 255), t / 0.45f)
                    : Color.Lerp(new Color32(0xF2, 0x54, 0x3D, 255), new Color32(0xC4, 0x2E, 0x4E, 255), (t - 0.45f) / 0.55f);
                for (int x = 0; x < 4; x++) _launch.SetPixel(x, 63 - y, c);
            }
            _launch.Apply(false, true);
            return _launch;
        }

        void Arm()
        {
            _tick?.Pause();
            _tick = null;
            _stars.style.translate = new Translate(0, 0);
            if (_reduce || panel == null) return;
            _tick = schedule.Execute(Drift).Every(32);
        }

        void Drift()
        {
            if (_reduce) return;
            float w = resolvedStyle.width;
            if (w < 8f) return;
            float phase = (Time.realtimeSinceStartup % 40f) / 40f;
            float dx = Mathf.Sin(phase * Mathf.PI * 2f) * w * 0.025f;
            _stars.style.translate = new Translate(dx, 0f);
        }

        void RepaintAll()
        {
            MarkDirtyRepaint();
            _stars.MarkDirtyRepaint();
            _land.MarkDirtyRepaint();
        }

        void DrawSky(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 2f || r.height < 2f) return;
            float W = r.width, H = r.height;
            Band(ctx, W, 0f, H * 0.28f, Night, Deep);
            Band(ctx, W, H * 0.28f, H * 0.52f, Deep, Violet);
            Band(ctx, W, H * 0.52f, H * 0.72f, Violet, Rose);
            Band(ctx, W, H * 0.72f, H * 0.88f, Rose, Amber);
            Band(ctx, W, H * 0.88f, H, Amber, Cream);
            var p = ctx.painter2D;
            Ellipse(p, W * 0.5f, H * 0.78f, W * 0.62f, H * 0.16f, new Color(1f, 0.62f, 0.32f, 0.28f));
            Ellipse(p, W * 0.5f, H * 0.82f, W * 0.38f, H * 0.08f, new Color(1f, 0.82f, 0.55f, 0.22f));
            Band(ctx, W, 0f, H * 0.22f, new Color(0.02f, 0.03f, 0.08f, 0.45f), new Color(0.02f, 0.03f, 0.08f, 0f));
        }

        void DrawStars(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 2f || r.height < 2f) return;
            var p = ctx.painter2D;
            float W = r.width, H = r.height;
            uint x = unchecked(11u * 2654435761u);
            for (int i = 0; i < 90; i++)
            {
                float u = Next(ref x), v = Next(ref x), s = Next(ref x);
                if (v > 0.62f) continue;
                float a = (1f - v / 0.62f) * (0.35f + 0.65f * s);
                p.fillColor = new Color(0.96f, 0.94f, 0.88f, a);
                float rad = 0.6f + s * 1.3f;
                p.BeginPath();
                p.Arc(new Vector2(u * W, v * H), rad, 0f, 360f);
                p.Fill();
            }
        }

        void DrawLand(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 2f || r.height < 2f) return;
            float W = r.width, H = r.height;
            var p = ctx.painter2D;
            float horizon = H * 0.78f;
            Ridge(p, W, horizon + H * 0.02f, H * 0.045f, RidgeFar, 0.15f);
            Ridge(p, W, horizon + H * 0.055f, H * 0.06f, RidgeNear, 0.55f);
            p.fillColor = Ground;
            p.BeginPath();
            p.MoveTo(new Vector2(0, horizon + H * 0.08f));
            p.LineTo(new Vector2(W, horizon + H * 0.08f));
            p.LineTo(new Vector2(W, H));
            p.LineTo(new Vector2(0, H));
            p.ClosePath();
            p.Fill();

            Rect hero = _hero.width > 8f ? _hero : new Rect(W * 0.28f, H * 0.22f, W * 0.44f, H * 0.5f);
            float bodyH = hero.height * 0.92f;
            float bodyW = Mathf.Min(hero.width * 0.62f, bodyH * 0.40f);
            float bx = hero.x + (hero.width - bodyW) * 0.5f;
            float baseY = hero.yMax - hero.height * 0.02f;
            float top = baseY - bodyH;

            Ellipse(p, bx + bodyW * 0.5f, baseY + bodyH * 0.02f, bodyW * 1.15f, bodyH * 0.045f, new Color(0.95f, 0.45f, 0.22f, 0.45f));
            Ellipse(p, bx + bodyW * 0.5f, baseY + bodyH * 0.03f, bodyW * 0.85f, bodyH * 0.02f, new Color(0.45f, 0.28f, 0.16f, 0.9f));

            float towerW = bodyW * 0.22f;
            Tower(p, bx - bodyW * 0.42f, baseY, towerW, bodyH * 0.72f);
            Tower(p, bx + bodyW + bodyW * 0.20f, baseY, towerW, bodyH * 0.72f);
            DrawStack(ctx, p, bx, top, bodyW, bodyH);
        }

        static void DrawStack(MeshGenerationContext ctx, Painter2D p, float x, float top, float w, float h)
        {
            float coreW = w * 0.46f;
            float boostW = w * 0.24f;
            float cx = x + (w - coreW) * 0.5f;
            float coreTop = top + h * 0.02f;
            float coreH = h * 0.9f;
            float boostH = h * 0.62f;
            float boostTop = top + h - boostH;
            Column(ctx, x, boostTop, boostW, boostH, HullShade, Hull, HullShade);
            Column(ctx, x + w - boostW, boostTop, boostW, boostH, HullShade, HullLit, HullShade);
            Column(ctx, cx, coreTop + h * 0.08f, coreW, coreH - h * 0.08f, HullShade, HullLit, Hull);
            Nose(p, x, boostTop, boostW, h * 0.08f);
            Nose(p, x + w - boostW, boostTop, boostW, h * 0.08f);
            Nose(p, cx, coreTop, coreW, h * 0.12f);
            Band(p, x + boostW * 0.12f, boostTop + boostH * 0.18f, boostW * 0.76f, Mathf.Max(2f, h * 0.012f), Ember);
            Band(p, x + w - boostW + boostW * 0.12f, boostTop + boostH * 0.18f, boostW * 0.76f, Mathf.Max(2f, h * 0.012f), Ember);
            Band(p, cx + coreW * 0.08f, coreTop + coreH * 0.42f, coreW * 0.84f, Mathf.Max(3f, h * 0.045f), new Color32(0x14, 0x16, 0x1C, 255));
            Band(p, cx + coreW * 0.08f, coreTop + coreH * 0.56f, coreW * 0.84f, Mathf.Max(2f, h * 0.018f), Ember);
            float led = Mathf.Max(3f, coreW * 0.1f);
            p.fillColor = Teal;
            p.BeginPath();
            p.Arc(new Vector2(cx + coreW * 0.5f, coreTop + coreH * 0.3f), led, 0f, 360f);
            p.Fill();
            p.strokeColor = new Color(1f, 1f, 1f, 0.55f);
            p.lineWidth = Mathf.Max(1f, w * 0.012f);
            p.BeginPath();
            p.MoveTo(new Vector2(cx + coreW * 0.28f, coreTop + h * 0.16f));
            p.LineTo(new Vector2(cx + coreW * 0.28f, coreTop + coreH * 0.92f));
            p.Stroke();
        }

        static void Nose(Painter2D p, float x, float y, float w, float h)
        {
            p.fillColor = HullLit;
            p.BeginPath();
            p.MoveTo(new Vector2(x, y + h));
            p.BezierCurveTo(new Vector2(x, y + h * 0.2f), new Vector2(x + w * 0.2f, y), new Vector2(x + w * 0.5f, y));
            p.BezierCurveTo(new Vector2(x + w * 0.8f, y), new Vector2(x + w, y + h * 0.2f), new Vector2(x + w, y + h));
            p.ClosePath();
            p.Fill();
        }

        static void Column(MeshGenerationContext ctx, float x, float y, float w, float h, Color left, Color mid, Color right)
        {
            var mesh = ctx.Allocate(6, 12);
            mesh.SetNextVertex(V(x, y, left));
            mesh.SetNextVertex(V(x + w * 0.42f, y, mid));
            mesh.SetNextVertex(V(x + w, y, right));
            mesh.SetNextVertex(V(x, y + h, left));
            mesh.SetNextVertex(V(x + w * 0.42f, y + h, mid));
            mesh.SetNextVertex(V(x + w, y + h, right));
            int[] t = { 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4 };
            for (int i = 0; i < t.Length; i++) mesh.SetNextIndex((ushort)t[i]);
        }

        static void Band(Painter2D p, float x, float y, float w, float h, Color c)
        {
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(new Vector2(x, y));
            p.LineTo(new Vector2(x + w, y));
            p.LineTo(new Vector2(x + w, y + h));
            p.LineTo(new Vector2(x, y + h));
            p.ClosePath();
            p.Fill();
        }

        static void Tower(Painter2D p, float x, float baseY, float w, float h)
        {
            p.strokeColor = new Color(0.08f, 0.06f, 0.12f, 0.92f);
            p.lineWidth = Mathf.Max(1.25f, w * 0.08f);
            p.lineCap = LineCap.Round;
            float top = baseY - h;
            p.BeginPath();
            p.MoveTo(new Vector2(x, baseY));
            p.LineTo(new Vector2(x, top));
            p.MoveTo(new Vector2(x + w, baseY));
            p.LineTo(new Vector2(x + w, top));
            float step = h / 7f;
            for (int i = 0; i <= 7; i++)
            {
                float y = baseY - i * step;
                p.MoveTo(new Vector2(x, y));
                p.LineTo(new Vector2(x + w, y));
                if (i < 7)
                {
                    p.MoveTo(new Vector2(x, y));
                    p.LineTo(new Vector2(x + w, y - step));
                    p.MoveTo(new Vector2(x + w, y));
                    p.LineTo(new Vector2(x, y - step));
                }
            }
            p.Stroke();
            p.strokeColor = new Color(0.85f, 0.45f, 0.32f, 0.35f);
            p.lineWidth = Mathf.Max(1f, w * 0.04f);
            p.BeginPath();
            p.MoveTo(new Vector2(x + w * 0.15f, baseY));
            p.LineTo(new Vector2(x + w * 0.15f, top));
            p.Stroke();
        }

        static void Ridge(Painter2D p, float W, float baseY, float amp, Color c, float phase)
        {
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(new Vector2(0, baseY + amp));
            for (int i = 0; i <= 8; i++)
            {
                float u = i / 8f;
                float y = baseY - amp * Mathf.Sin((u + phase) * Mathf.PI * 2.2f) * (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(u * 3.1f)));
                p.LineTo(new Vector2(u * W, y));
            }
            p.LineTo(new Vector2(W, baseY + amp * 3f));
            p.LineTo(new Vector2(0, baseY + amp * 3f));
            p.ClosePath();
            p.Fill();
        }

        static void Ellipse(Painter2D p, float cx, float cy, float rx, float ry, Color c)
        {
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(new Vector2(cx, cy - ry));
            p.BezierCurveTo(new Vector2(cx + rx, cy - ry), new Vector2(cx + rx, cy + ry), new Vector2(cx, cy + ry));
            p.BezierCurveTo(new Vector2(cx - rx, cy + ry), new Vector2(cx - rx, cy - ry), new Vector2(cx, cy - ry));
            p.ClosePath();
            p.Fill();
        }

        static void Band(MeshGenerationContext ctx, float w, float y0, float y1, Color c0, Color c1)
        {
            var mesh = ctx.Allocate(4, 6);
            mesh.SetNextVertex(V(0, y0, c0));
            mesh.SetNextVertex(V(w, y0, c0));
            mesh.SetNextVertex(V(w, y1, c1));
            mesh.SetNextVertex(V(0, y1, c1));
            mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
            mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
        }

        static float Next(ref uint x)
        {
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            return (x & 0xFFFFFF) / (float)0x1000000;
        }

        static Vertex V(float x, float y, Color c) => new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = c };
    }

    /// <summary>Original 24 px line icons. Two-pixel stroke, round caps, one accent.</summary>
    public sealed class HomeGlyph : VisualElement
    {
        public string Kind;
        public Color Ink = new Color(0.96f, 0.92f, 0.84f, 1f);
        public HomeGlyph(string kind) { Kind = kind; pickingMode = PickingMode.Ignore; generateVisualContent += Draw; }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 2f || r.height < 2f) return;
            var p = ctx.painter2D;
            p.strokeColor = Ink;
            p.fillColor = Ink;
            p.lineWidth = Mathf.Max(1.5f, r.width * 0.08f);
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            float w = r.width, h = r.height;
            switch (Kind)
            {
                case "rocket": Rocket(p, w, h); break;
                case "blueprint": Blueprint(p, w, h); break;
                case "orbit": Orbit(p, w, h); break;
                case "book": Book(p, w, h); break;
                case "gear": Gear(p, w, h); break;
                default: Disc(p, w, h); break;
            }
        }

        static void Rocket(Painter2D p, float w, float h)
        {
            float cx = w * 0.5f;
            p.BeginPath();
            p.MoveTo(new Vector2(cx, h * 0.08f));
            p.LineTo(new Vector2(w * 0.72f, h * 0.42f));
            p.LineTo(new Vector2(w * 0.62f, h * 0.42f));
            p.LineTo(new Vector2(w * 0.62f, h * 0.72f));
            p.LineTo(new Vector2(w * 0.78f, h * 0.9f));
            p.LineTo(new Vector2(cx, h * 0.78f));
            p.LineTo(new Vector2(w * 0.22f, h * 0.9f));
            p.LineTo(new Vector2(w * 0.38f, h * 0.72f));
            p.LineTo(new Vector2(w * 0.38f, h * 0.42f));
            p.LineTo(new Vector2(w * 0.28f, h * 0.42f));
            p.ClosePath();
            p.Stroke();
        }

        static void Blueprint(Painter2D p, float w, float h)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(w * 0.16f, h * 0.2f));
            p.LineTo(new Vector2(w * 0.68f, h * 0.2f));
            p.LineTo(new Vector2(w * 0.84f, h * 0.36f));
            p.LineTo(new Vector2(w * 0.84f, h * 0.82f));
            p.LineTo(new Vector2(w * 0.16f, h * 0.82f));
            p.ClosePath();
            p.MoveTo(new Vector2(w * 0.68f, h * 0.2f));
            p.LineTo(new Vector2(w * 0.68f, h * 0.36f));
            p.LineTo(new Vector2(w * 0.84f, h * 0.36f));
            p.Stroke();
        }

        static void Orbit(Painter2D p, float w, float h)
        {
            p.BeginPath();
            p.Arc(new Vector2(w * 0.5f, h * 0.5f), w * 0.28f, 0f, 360f);
            p.Stroke();
            p.BeginPath();
            p.Arc(new Vector2(w * 0.74f, h * 0.32f), w * 0.07f, 0f, 360f);
            p.Fill();
        }

        static void Book(Painter2D p, float w, float h)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(w * 0.5f, h * 0.22f));
            p.LineTo(new Vector2(w * 0.18f, h * 0.32f));
            p.LineTo(new Vector2(w * 0.18f, h * 0.8f));
            p.LineTo(new Vector2(w * 0.5f, h * 0.68f));
            p.LineTo(new Vector2(w * 0.82f, h * 0.8f));
            p.LineTo(new Vector2(w * 0.82f, h * 0.32f));
            p.ClosePath();
            p.MoveTo(new Vector2(w * 0.5f, h * 0.22f));
            p.LineTo(new Vector2(w * 0.5f, h * 0.68f));
            p.Stroke();
        }

        static void Gear(Painter2D p, float w, float h)
        {
            p.BeginPath();
            p.Arc(new Vector2(w * 0.5f, h * 0.5f), w * 0.16f, 0f, 360f);
            p.Stroke();
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                var c = new Vector2(w * 0.5f, h * 0.5f);
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                p.BeginPath();
                p.MoveTo(c + d * w * 0.22f);
                p.LineTo(c + d * w * 0.38f);
                p.Stroke();
            }
        }

        static void Disc(Painter2D p, float w, float h)
        {
            p.BeginPath();
            p.Arc(new Vector2(w * 0.5f, h * 0.5f), w * 0.28f, 0f, 360f);
            p.Stroke();
        }
    }
}
