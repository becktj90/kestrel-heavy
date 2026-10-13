// Home title backdrop, retro "mix-v2" (Trevor's pick): navy poster sky with stars, striped sun, hills, a generic
// heavy-lift stack in cream blueprint linework with a few dimension/part callouts, and the console panel colour
// below ConsoleTopY. Drawn at runtime with Painter2D (0 IPA bytes); paper grain is a 128px generated tile. Full-bleed, scales with
// the element rect, so it fills SE, Pro Max and iPad portrait with no letterbox. Static: nothing
// animates or flashes, so it is Reduce Motion safe by construction. Generic shapes, no real vehicle.
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public sealed class HomeBackdrop : VisualElement
    {
        // Legacy pad-silhouette sizing, kept for DesignFramingTests (Design reuses the framing rule).
        public const float Horizon = 0.78f;

        /// <summary>Panel-y the stack must stay below (bottom of the title block + gap). Set by the Home
        /// screen from the subtitle's live rect; NaN = fall back to 42% of the height.</summary>
        public float ArtTopY = float.NaN;
        public const float FallbackArtTop = 0.42f;

        /// <summary>Stack unit so the tallest point (core + fairing = 10.6 u above the deck) stays below artTop,
        /// and the tower/deck (6.4 u wide) fits the width. Pure, for tests.</summary>
        public static float StackUnit(float W, float H, float artTop)
        {
            float deck = Horizon * H;
            float room = Mathf.Max(0f, deck - artTop);
            return Mathf.Max(0f, Mathf.Min(W / 10f, room / 10.6f));
        }

        /// <summary>Panel-y of the console panel top (set by the Home screen). NaN = 74% of the height.</summary>
        public float ConsoleTopY = float.NaN;
        public const float FallbackConsoleTop = 0.74f;
        readonly Label _hLabel, _aLabel, _bLabel;
        readonly VisualElement _grain;
        static Texture2D _grainTex;

        public HomeBackdrop()
        {
            name = "home-backdrop";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0; style.right = 0; style.top = 0; style.bottom = 0;
            generateVisualContent += Draw;
            _hLabel = Callout("H 71.4 m"); _aLabel = Callout("A  FAIRING"); _bLabel = Callout("B  INTERSTAGE");
            _grain = new VisualElement { name = "home-grain", pickingMode = PickingMode.Ignore };
            _grain.AddToClassList("kh-home-grain");
            _grain.style.position = Position.Absolute; _grain.style.left = 0; _grain.style.right = 0; _grain.style.top = 0; _grain.style.bottom = 0;
            _grain.style.backgroundImage = new StyleBackground(GrainTexture());
            _grain.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            _grain.style.backgroundSize = new BackgroundSize(new Length(HomeArt.GrainSize), new Length(HomeArt.GrainSize));
            Add(_grain);
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }

        Label Callout(string t)
        {
            var l = new Label(t) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("kh-home-callout");
            l.style.position = Position.Absolute;
            Add(l); return l;
        }

        public static Texture2D GrainTexture()
        {
            if (_grainTex != null) return _grainTex;
            _grainTex = new Texture2D(HomeArt.GrainSize, HomeArt.GrainSize, TextureFormat.RGBA32, false) { name = "KH_HomeGrain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            _grainTex.SetPixelData(HomeArt.GrainRgba(), 0);
            _grainTex.Apply(false, true);
            return _grainTex;
        }

        public HomeArt.Layout CurrentLayout()
        {
            var r = contentRect; float W = r.width, H = r.height;
            float title = float.IsNaN(ArtTopY) ? FallbackArtTop * H : ArtTopY - r.y;
            float console = float.IsNaN(ConsoleTopY) ? FallbackConsoleTop * H : ConsoleTopY - r.y;
            return HomeArt.Compute(W, H, title, console);
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect; if (r.width <= 0 || r.height <= 0) return;
            float W = r.width, H = r.height;
            var L = CurrentLayout();
            var p = ctx.painter2D;
            // Sky: two-band gradient (top darker), via a mesh so it is one draw.
            var mesh = ctx.Allocate(4, 6);
            mesh.SetNextVertex(V(0, 0, HomeArt.NavyTop)); mesh.SetNextVertex(V(W, 0, HomeArt.NavyTop));
            mesh.SetNextVertex(V(W, H, HomeArt.Navy)); mesh.SetNextVertex(V(0, H, HomeArt.Navy));
            mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2); mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);

            foreach (var s in HomeArt.Stars())
            {
                float sy = s.y * Mathf.Max(20f, L.SunC.y - L.SunR); float sx = s.x * W;
                p.fillColor = HomeArt.Cream; p.BeginPath(); p.Arc(new Vector2(sx, sy), s.z * 0.6f, 0f, 360f); p.Fill();
            }
            Disc(p, L.SunC, L.SunR, HomeArt.SunOuter);
            Disc(p, L.SunC, L.SunInnerR, HomeArt.SunInner);
            foreach (var (y0, y1) in HomeArt.StripeBands(L)) Poly(p, HomeArt.CircleBand(L.SunC, L.SunR + 1f, y0, y1), HomeArt.Navy);

            // Hills + console fill (full-bleed, under the bottom safe area too).
            float ct = L.ConsoleTop;
            p.fillColor = HomeArt.Hills; p.BeginPath();
            p.MoveTo(new Vector2(0, ct)); p.LineTo(new Vector2(0, ct - 0.05f * H)); p.LineTo(new Vector2(0.2f * W, ct - 0.1f * H));
            p.LineTo(new Vector2(0.4f * W, ct - 0.06f * H)); p.LineTo(new Vector2(0.6f * W, ct - 0.12f * H)); p.LineTo(new Vector2(0.82f * W, ct - 0.07f * H));
            p.LineTo(new Vector2(W, ct - 0.1f * H)); p.LineTo(new Vector2(W, ct)); p.ClosePath(); p.Fill();
            Box(p, 0, ct, W, H - ct, HomeArt.Console);

            DrawRocket(p, L);
            PlaceCallouts(L);
        }

        void DrawRocket(Painter2D p, HomeArt.Layout L)
        {
            float u = L.RocketUnit; if (u <= 0.5f) return;
            float cx = L.RocketCx, b = L.RocketBase, cw = 0.9f * u, lw = Mathf.Max(1.5f, 0.05f * u);
            void Rect(float x0, float y0, float x1, float y1) { Box(p, x0, y0, x1 - x0, y1 - y0, HomeArt.Navy); Outline(p, x0, y0, x1, y1, lw); }
            // tower (hills colour, behind)
            float tx = L.TowerX, tw = 0.6f * u, th = 9f * u;
            p.strokeColor = HomeArt.Console; p.lineWidth = lw * 1.4f; p.BeginPath();
            p.MoveTo(new Vector2(tx, b)); p.LineTo(new Vector2(tx, b - th)); p.MoveTo(new Vector2(tx + tw, b)); p.LineTo(new Vector2(tx + tw, b - th));
            for (float y = b; y > b - th + 0.01f; y -= tw) { p.MoveTo(new Vector2(tx, y)); p.LineTo(new Vector2(tx + tw, y - tw)); p.MoveTo(new Vector2(tx + tw, y)); p.LineTo(new Vector2(tx, y - tw)); }
            p.Stroke();
            foreach (float sg in new[] { -1f, 1f })
            {
                float bx = cx + sg * (cw / 2 + 0.36f * u);
                Rect(bx - 0.3f * u, b - 6.2f * u, bx + 0.3f * u, b - 0.6f * u);
                Tri(p, bx - 0.3f * u, b - 6.2f * u, bx, b - 7.1f * u, bx + 0.3f * u, b - 6.2f * u, lw);
                for (int k = 1; k <= 3; k++) Line(p, bx - 0.3f * u, b - 0.6f * u - k * 1.4f * u, bx + 0.3f * u, b - 0.6f * u - k * 1.4f * u, lw * 0.5f);
            }
            Rect(cx - cw / 2, b - 8.2f * u, cx + cw / 2, b - 0.6f * u);
            Rect(cx - cw * 0.42f, b - 9.4f * u, cx + cw * 0.42f, b - 8.2f * u);
            Rect(cx - cw / 2, b - 10f * u, cx + cw / 2, b - 9.4f * u);
            // ogive nose
            p.fillColor = HomeArt.Navy; p.strokeColor = HomeArt.Cream; p.lineWidth = lw; p.BeginPath();
            p.MoveTo(new Vector2(cx - cw / 2, b - 10f * u));
            p.BezierCurveTo(new Vector2(cx - cw / 2, b - 10.8f * u), new Vector2(cx - cw * 0.2f, b - 11.3f * u), new Vector2(cx, b - 11.3f * u));
            p.BezierCurveTo(new Vector2(cx + cw * 0.2f, b - 11.3f * u), new Vector2(cx + cw / 2, b - 10.8f * u), new Vector2(cx + cw / 2, b - 10f * u));
            p.ClosePath(); p.Fill(); p.Stroke();
            for (int k = 1; k <= 5; k++) Line(p, cx - cw / 2, b - 0.6f * u - k * 1.27f * u, cx + cw / 2, b - 0.6f * u - k * 1.27f * u, lw * 0.5f);
            for (int k = -1; k <= 1; k++) { float ex = cx + k * 0.28f * u; Tri(p, ex - 0.1f * u, b - 0.6f * u, ex, b - 0.6f * u, ex + 0.1f * u, b - 0.6f * u, lw); Outline(p, ex - 0.15f * u, b - 0.6f * u, ex + 0.15f * u, b, lw * 0.8f); }
            // centre line (dashed) + height dimension
            for (float y = b - 11.6f * u; y < b; y += 0.3f * u) Line(p, cx, y, cx, Mathf.Min(b, y + 0.14f * u), lw * 0.4f);
            float dx = cx + cw / 2 + 1.55f * u;
            Line(p, dx, b - 11.3f * u, dx, b, lw * 0.6f); Line(p, dx - 0.18f * u, b - 11.3f * u, dx + 0.18f * u, b - 11.3f * u, lw * 0.6f); Line(p, dx - 0.18f * u, b, dx + 0.18f * u, b, lw * 0.6f);
            Line(p, cx + cw / 2, b - 10.3f * u, dx - 0.1f * u, b - 10.3f * u, lw * 0.4f);
            Line(p, cx + cw / 2, b - 8.8f * u, dx - 0.1f * u, b - 8.8f * u, lw * 0.4f);
        }

        void PlaceCallouts(HomeArt.Layout L)
        {
            float u = L.RocketUnit; bool show = u > 14f;   // hide labels when the art is too small to read
            float x = L.RocketCx + 0.45f * u + 1.55f * u + 0.25f * u;
            Set(_hLabel, show, x, L.RocketBase - 6f * u);
            Set(_aLabel, show, x, L.RocketBase - 10.3f * u - 8f);
            Set(_bLabel, show, x, L.RocketBase - 8.8f * u - 8f);
        }
        static void Set(Label l, bool show, float x, float y) { l.style.display = show ? DisplayStyle.Flex : DisplayStyle.None; l.style.left = x; l.style.top = y; }

        static Vertex V(float x, float y, Color32 c) => new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = c };
        static void Disc(Painter2D p, Vector2 c, float r, Color col) { p.fillColor = col; p.BeginPath(); p.Arc(c, r, 0f, 360f); p.ClosePath(); p.Fill(); }
        static void Poly(Painter2D p, System.Collections.Generic.List<Vector2> pts, Color col)
        { if (pts.Count < 3) return; p.fillColor = col; p.BeginPath(); p.MoveTo(pts[0]); for (int i = 1; i < pts.Count; i++) p.LineTo(pts[i]); p.ClosePath(); p.Fill(); }
        static void Box(Painter2D p, float x, float y, float w, float h, Color c)
        {
            p.fillColor = c; p.BeginPath();
            p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(x + w, y)); p.LineTo(new Vector2(x + w, y + h)); p.LineTo(new Vector2(x, y + h));
            p.ClosePath(); p.Fill();
        }
        static void Outline(Painter2D p, float x0, float y0, float x1, float y1, float lw)
        {
            p.strokeColor = HomeArt.Cream; p.lineWidth = lw; p.BeginPath();
            p.MoveTo(new Vector2(x0, y0)); p.LineTo(new Vector2(x1, y0)); p.LineTo(new Vector2(x1, y1)); p.LineTo(new Vector2(x0, y1)); p.ClosePath(); p.Stroke();
        }
        static void Line(Painter2D p, float x0, float y0, float x1, float y1, float lw)
        { p.strokeColor = HomeArt.Cream; p.lineWidth = lw; p.BeginPath(); p.MoveTo(new Vector2(x0, y0)); p.LineTo(new Vector2(x1, y1)); p.Stroke(); }
        static void Tri(Painter2D p, float ax, float ay, float bx, float by, float cx, float cy, float lw)
        {
            p.fillColor = HomeArt.Navy; p.strokeColor = HomeArt.Cream; p.lineWidth = lw; p.BeginPath();
            p.MoveTo(new Vector2(ax, ay)); p.LineTo(new Vector2(bx, by)); p.LineTo(new Vector2(cx, cy)); p.ClosePath(); p.Fill(); p.Stroke();
        }
    }
}
