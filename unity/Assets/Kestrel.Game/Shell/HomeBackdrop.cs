// Home title backdrop: dusk sky gradient, ground band, and a generic heavy-lift stack on a pad with a
// lattice tower, drawn as vector shapes (no bitmap art exists in the repo yet). Full-bleed, scales with
// the element rect, so it fills SE, Pro Max and iPad portrait with no letterbox. Static: nothing
// animates or flashes, so it is Reduce Motion safe by construction. Generic shapes, no real vehicle.
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public sealed class HomeBackdrop : VisualElement
    {
        static readonly float[] SkyT = { 0.00f, 0.40f, 0.66f, 0.78f };
        static readonly Color32[] SkyC =
        {
            new Color32(0x0b, 0x10, 0x2e, 255), new Color32(0x1a, 0x3c, 0x6e, 255),
            new Color32(0x6b, 0x4a, 0x8a, 255), new Color32(0xf2, 0x9a, 0x5c, 255),
        };
        public const float Horizon = 0.78f;
        static readonly Color Ground = new Color32(0x14, 0x18, 0x24, 255);
        static readonly Color Stack = new Color32(0x06, 0x08, 0x12, 255);

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

        public HomeBackdrop()
        {
            name = "home-backdrop";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0; style.right = 0; style.top = 0; style.bottom = 0;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect; if (r.width <= 0 || r.height <= 0) return;
            float W = r.width, H = r.height;
            int bands = SkyT.Length - 1;
            var mesh = ctx.Allocate(bands * 4, bands * 6);
            for (int i = 0; i < bands; i++)
            {
                float y0 = SkyT[i] * H, y1 = SkyT[i + 1] * H;
                mesh.SetNextVertex(V(0, y0, SkyC[i])); mesh.SetNextVertex(V(W, y0, SkyC[i]));
                mesh.SetNextVertex(V(W, y1, SkyC[i + 1])); mesh.SetNextVertex(V(0, y1, SkyC[i + 1]));
                ushort b = (ushort)(i * 4);
                mesh.SetNextIndex(b); mesh.SetNextIndex((ushort)(b + 1)); mesh.SetNextIndex((ushort)(b + 2));
                mesh.SetNextIndex(b); mesh.SetNextIndex((ushort)(b + 2)); mesh.SetNextIndex((ushort)(b + 3));
            }

            var p = ctx.painter2D;
            float hy = Horizon * H;
            Box(p, 0, hy, W, H - hy, Ground);

            // Sized from the smaller of width/height so the stack keeps proportion on every device.
            float artTop = float.IsNaN(ArtTopY) ? FallbackArtTop * H : ArtTopY - r.y;
            float u = StackUnit(W, H, artTop);   // never overlaps the title block
            float cx = W * 0.5f, deck = hy;
            Box(p, cx - 3.2f * u, deck, 6.4f * u, 0.5f * u, Stack);
            float coreH = 9f * u, coreW = 1.1f * u, boosterW = 0.9f * u, boosterH = 6.4f * u;
            Box(p, cx - coreW / 2, deck - coreH, coreW, coreH, Stack);
            Nose(p, cx, deck - coreH, coreW * 0.62f, 1.6f * u);
            foreach (float side in new[] { -1f, 1f })
            {
                float bx = cx + side * (coreW / 2 + boosterW / 2 + 0.05f * u);
                Box(p, bx - boosterW / 2, deck - boosterH, boosterW, boosterH, Stack);
                Nose(p, bx, deck - boosterH, boosterW / 2, 0.9f * u);
            }
            float tx = cx - 3.0f * u, tw = 0.8f * u, th = 8.2f * u;
            p.strokeColor = Stack; p.lineWidth = Mathf.Max(1.5f, 0.08f * u);
            p.BeginPath();
            p.MoveTo(new Vector2(tx, deck)); p.LineTo(new Vector2(tx, deck - th));
            p.MoveTo(new Vector2(tx + tw, deck)); p.LineTo(new Vector2(tx + tw, deck - th));
            for (float y = deck; y > deck - th + 0.01f; y -= tw)
            {
                p.MoveTo(new Vector2(tx, y)); p.LineTo(new Vector2(tx + tw, y - tw));
                p.MoveTo(new Vector2(tx, y - tw)); p.LineTo(new Vector2(tx + tw, y - tw));
            }
            p.MoveTo(new Vector2(tx + tw, deck - th * 0.8f)); p.LineTo(new Vector2(cx - coreW / 2, deck - th * 0.8f));
            p.Stroke();
        }

        static Vertex V(float x, float y, Color32 c) => new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = c };

        static void Box(Painter2D p, float x, float y, float w, float h, Color c)
        {
            p.fillColor = c; p.BeginPath();
            p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(x + w, y)); p.LineTo(new Vector2(x + w, y + h)); p.LineTo(new Vector2(x, y + h));
            p.ClosePath(); p.Fill();
        }

        static void Nose(Painter2D p, float cx, float baseY, float halfW, float h)
        {
            p.fillColor = Stack; p.BeginPath();
            p.MoveTo(new Vector2(cx - halfW, baseY));
            p.BezierCurveTo(new Vector2(cx - halfW, baseY - h * 0.7f), new Vector2(cx - halfW * 0.3f, baseY - h), new Vector2(cx, baseY - h));
            p.BezierCurveTo(new Vector2(cx + halfW * 0.3f, baseY - h), new Vector2(cx + halfW, baseY - h * 0.7f), new Vector2(cx + halfW, baseY));
            p.ClosePath(); p.Fill();
        }
    }
}
