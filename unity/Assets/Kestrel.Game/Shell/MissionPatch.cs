// Fictional KH-1 mission patch: navy disc, orange ring, cream chevron-and-star mark. Original shapes only,
// no agency insignia. Painter2D, static. Text "KH-1" is a child label so it uses the theme font.
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public sealed class MissionPatch : VisualElement
    {
        public MissionPatch()
        {
            generateVisualContent += Draw;
            var t = new Label("KH-1") { pickingMode = PickingMode.Ignore };
            t.AddToClassList("kh-home-patch-label");
            Add(t);
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect; float s = Mathf.Min(r.width, r.height); if (s <= 0) return;
            var c = new Vector2(r.width / 2, r.height / 2); float R = s / 2;
            var p = ctx.painter2D;
            p.fillColor = HomeArt.SunOuter; p.BeginPath(); p.Arc(c, R, 0, 360); p.Fill();
            p.fillColor = HomeArt.Navy; p.BeginPath(); p.Arc(c, R * 0.84f, 0, 360); p.Fill();
            // chevron (upward, rocket-ish) + small star
            p.fillColor = HomeArt.Cream; p.BeginPath();
            p.MoveTo(c + new Vector2(0, -R * 0.55f)); p.LineTo(c + new Vector2(R * 0.32f, R * 0.05f)); p.LineTo(c + new Vector2(0, -R * 0.12f));
            p.LineTo(c + new Vector2(-R * 0.32f, R * 0.05f)); p.ClosePath(); p.Fill();
            p.fillColor = HomeArt.SunOuter; p.BeginPath(); p.Arc(c + new Vector2(R * 0.4f, -R * 0.38f), R * 0.06f, 0, 360); p.Fill();
        }
    }
}
