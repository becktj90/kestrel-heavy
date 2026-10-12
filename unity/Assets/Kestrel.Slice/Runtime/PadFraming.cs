// Pre-launch camera framing (P0 fix 4: "pre-launch framing cuts off the tower base and pad").
//
// Root cause (main b514d53): SliceApp.FrameCamera (SliceApp.cs:133) used a fixed offset (0.9h, 0.45h, -1.4h)
// aimed at mid-vehicle with a fixed 50 deg vertical FOV. It only knew the vehicle height: not the tower
// (which sits ~r+8 m to the side and up to ~20 m behind, 1.08 h tall), not the deck, not the portrait aspect,
// and not the DESIGN panel, which covers the bottom ~27 % of the screen on iPhone. The visible window above
// the panel bottomed out around +0.1 h, so the deck and tower base were always hidden behind the UI.
//
// Fix: fit the bounding points of vehicle + tower + deck into the unobstructed viewport (PortraitLayout safe
// content rect, minus the design panel) with an exact solve: for a fixed view rotation, each point gives a
// linear interval constraint on the camera's lateral offset; binary-search the smallest distance where the
// intervals intersect, then centre in them.
using System.Collections.Generic;
using Kestrel.Game.Layout;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class PadFraming
    {
        /// <summary>View direction (camera forward): the old framing's horizontal heading, pitched down 6 deg so the deck reads.</summary>
        public static readonly Quaternion View = Quaternion.LookRotation(new Vector3(-0.9f, 0f, 1.4f).normalized) * Quaternion.Euler(6f, 0f, 0f);
        public const float Padding = 0.04f;   // fraction of the viewport kept clear on each side

        /// <summary>Unobstructed viewport (0..1, bottom-left origin) from the portrait safe content rect,
        /// with the bottom raised to the top of the design panel (panelFraction = panel height / screen height).</summary>
        public static Rect Viewport(double wPx, double hPx, RectPx safe, double dpr, float panelFraction)
        {
            var f = PortraitLayout.Compute(wPx, hPx, safe, dpr, new SpecPortrait());
            float x0 = (float)(f.Content.X / wPx), x1 = (float)(f.Content.XMax / wPx);
            float y0 = Mathf.Max((float)(f.Content.Y / hPx), Mathf.Clamp01(panelFraction)), y1 = (float)(f.Content.YMax / hPx);
            if (y1 - y0 < 0.2f) y0 = y1 - 0.2f;   // never collapse
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>Camera position (rotation = rot) such that every point projects inside viewport (with Padding).</summary>
        public static Vector3 Fit(IReadOnlyList<Vector3> pts, Quaternion rot, float vFovDeg, float aspect, Rect viewport)
        {
            float ty = Mathf.Tan(vFovDeg * 0.5f * Mathf.Deg2Rad), tx = ty * aspect;
            float u0 = viewport.xMin + Padding * viewport.width, u1 = viewport.xMax - Padding * viewport.width;
            float v0 = viewport.yMin + Padding * viewport.height, v1 = viewport.yMax - Padding * viewport.height;
            float xMin = tx * (2 * u0 - 1), xMax = tx * (2 * u1 - 1), yMin = ty * (2 * v0 - 1), yMax = ty * (2 * v1 - 1);
            var inv = Quaternion.Inverse(rot);
            var local = new Vector3[pts.Count];
            Vector3 c = Vector3.zero; foreach (var p in pts) c += p; c /= Mathf.Max(1, pts.Count);
            float zMin = float.MaxValue, span = 1f;
            for (int i = 0; i < pts.Count; i++) { local[i] = inv * (pts[i] - c); zMin = Mathf.Min(zMin, local[i].z); span = Mathf.Max(span, local[i].magnitude); }

            // Camera at c + rot * (a, b, -D). Point i: (x - a) / (z + D) in [xMin, xMax] -> a in [x - xMax (z+D), x - xMin (z+D)].
            bool Feasible(float D, out float a, out float b)
            {
                float aLo = float.MinValue, aHi = float.MaxValue, bLo = float.MinValue, bHi = float.MaxValue;
                foreach (var q in local)
                {
                    float z = q.z + D; if (z <= 0.5f) { a = b = 0; return false; }
                    aLo = Mathf.Max(aLo, q.x - xMax * z); aHi = Mathf.Min(aHi, q.x - xMin * z);
                    bLo = Mathf.Max(bLo, q.y - yMax * z); bHi = Mathf.Min(bHi, q.y - yMin * z);
                }
                a = (aLo + aHi) * 0.5f; b = (bLo + bHi) * 0.5f;
                return aLo <= aHi && bLo <= bHi;
            }

            float lo = -zMin + 0.5f, hi = Mathf.Max(lo, 1f) + span * 100f;
            for (int it = 0; it < 60; it++) { float mid = (lo + hi) * 0.5f; if (Feasible(mid, out _, out _)) hi = mid; else lo = mid; }
            Feasible(hi, out float ax, out float by);
            return c + rot * new Vector3(ax, by, -hi);
        }

        /// <summary>Bounding points: vehicle cylinder box (radius r, height h) on the deck, the deck rim, and extra points (tower).</summary>
        public static List<Vector3> Points(float h, float r, float deckRadius, IEnumerable<Vector3> extra)
        {
            var pts = new List<Vector3>();
            foreach (var y in new[] { 0f, h }) for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2; pts.Add(new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a))); }
            for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; pts.Add(new Vector3(deckRadius * Mathf.Cos(a), -0.5f, deckRadius * Mathf.Sin(a))); }
            if (extra != null) pts.AddRange(extra);
            return pts;
        }

        /// <summary>World-space corners of the generated pad members' bounds (tower, arms) under padTransform.</summary>
        public static IEnumerable<Vector3> PadBounds(Kestrel.Pad.PadLayout L, Transform padTransform)
        {
            if (L == null || L.Members.Count == 0 || padTransform == null) yield break;
            Vector3 mn = Vector3.positiveInfinity, mx = Vector3.negativeInfinity;
            foreach (var m in L.Members)
            {
                var a = new Vector3(m.A.X, m.A.Y, m.A.Z); var b = new Vector3(m.B.X, m.B.Y, m.B.Z);
                mn = Vector3.Min(mn, Vector3.Min(a, b)); mx = Vector3.Max(mx, Vector3.Max(a, b));
            }
            for (int i = 0; i < 8; i++)
                yield return padTransform.TransformPoint(new Vector3((i & 1) == 0 ? mn.x : mx.x, (i & 2) == 0 ? mn.y : mx.y, (i & 4) == 0 ? mn.z : mx.z));
        }
    }
}
