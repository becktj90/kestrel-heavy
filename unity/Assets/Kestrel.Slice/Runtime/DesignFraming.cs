// Design-screen camera framing. The Design panel covers the bottom of the screen, so the rocket and the
// pad deck must be framed in the band ABOVE the panel's top edge, not in the full viewport (v4 captures:
// the panel hid the bottom quarter of the stack and the whole pad deck on iPhone / Pro Max, more on iPad).
// The band is measured from the live panel rect each layout pass, never a hardcoded pixel value.
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class DesignFraming
    {
        /// <summary>The subject is never squeezed into less than this share of the screen height.</summary>
        public const float MinVisibleFraction = 0.35f;
        /// <summary>Extra room so the pad deck (y ≈ -0.5 m) and nose clear the band edges.</summary>
        public const float Margin = 0.80f;   // stack + deck fill ~80% of the band (was 1.12 → ~58%)
        /// <summary>Points on the rocket the camera aims at, as a fraction of its height (slightly low so the deck is in).</summary>
        public const float AimHeight = 0.47f;

        /// <summary>Device pixels per iOS point. Screen.dpi is 0 or a desktop value in the editor / simulators, which
        /// made the 64 pt handle ~3x too tall on SE; fall back to the iOS scale by device shape.</summary>
        public static float PixelsPerPoint(float widthPx, float heightPx, float dpi)
        {
            if (dpi >= 200f) return Mathf.Max(1f, Mathf.Round(dpi / 160f));
            bool tablet = heightPx > 0 && widthPx / heightPx > 0.6f;   // iPads are ~0.75, phones ≤ 0.56
            if (tablet) return 2f;
            return widthPx >= 1080f ? 3f : 2f;                          // SE/8 = 2x (750 px), Pro/Max = 3x
        }

        /// <summary>Share of the root height, from the top, that is not covered by the panel.
        /// rootHeight and panelTop in the same (panel) units; panelTop measured from the top of the root.</summary>
        public static float VisibleFraction(float rootHeight, float panelTop)
        {
            if (rootHeight <= 0f || float.IsNaN(panelTop)) return 1f;
            return Mathf.Clamp(panelTop / rootHeight, MinVisibleFraction, 1f);
        }

        /// <summary>How far to pull the camera back so the stack that filled the full view fits the band.</summary>
        public static float DistanceScale(float visibleFraction) => Margin / Mathf.Clamp(visibleFraction, MinVisibleFraction, 1f);

        /// <summary>Rocket height used for framing: non-finite or tiny values (layout pass before the rocket is
        /// built, a zero-height mesh) fall back so the camera offset can never collapse onto the aim point.</summary>
        public static float SafeHeight(float h) => float.IsNaN(h) || float.IsInfinity(h) || h < 1f ? 70f : h;

        /// <summary>Band fraction with NaN / infinity mapped to the full screen.</summary>
        public static float SafeVisible(float f) => float.IsNaN(f) || float.IsInfinity(f) ? 1f : Mathf.Clamp(f, MinVisibleFraction, 1f);

        /// <summary>True when a camera of this pixel size has a usable aspect (size changes can report 0 x N).</summary>
        public static bool ValidViewport(int pixelWidth, int pixelHeight) => pixelWidth > 0 && pixelHeight > 0;

        /// <summary>Every element finite (Unity logs IsMatrixValid / NaN camera errors otherwise).</summary>
        public static bool IsFinite(Matrix4x4 m)
        {
            for (int i = 0; i < 16; i++) { float v = m[i]; if (float.IsNaN(v) || float.IsInfinity(v)) return false; }
            return true;
        }

        public static bool IsFinite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

        /// <summary>Vertical NDC offset that moves the image centre to the middle of the top band.
        /// Band spans NDC y in [1 - 2f, 1]; its centre is 1 - f.</summary>
        public static float NdcShiftY(float visibleFraction) => 1f - Mathf.Clamp(visibleFraction, MinVisibleFraction, 1f);

        /// <summary>Camera projection with the principal point moved up by NdcShiftY. Full-screen render
        /// (no viewport rect, so nothing is letterboxed); only the framing moves.</summary>
        public static Matrix4x4 ShiftedProjection(Matrix4x4 baseProjection, float visibleFraction)
        {
            var p = baseProjection;
            // ndc_y = (m11*y + m12*z) / -z  →  adding -s to m12 adds +s to ndc_y for every point.
            p.m12 -= NdcShiftY(visibleFraction);
            return p;
        }

        // ---- Pre-launch fit (P0-4, supersedes #120's PadFraming). Same camera rig as SliceApp.FrameCamera
        // (offset direction (0.9, 0.45-AimHeight, -1.4) from the aim point, ShiftedProjection band), but the pull-back
        // is solved so EVERY subject point (vehicle box, tower/arm bounds, deck rim at PadGround.DeckRadius) lands
        // inside the band above the panel, on any portrait aspect, instead of only the vehicle's height.

        /// <summary>Fraction of the band kept clear on each edge by FitDistanceScale.</summary>
        public const float FitPadding = 0.04f;

        /// <summary>Camera position for height h and distance scale k (identical to SliceApp.FrameCamera).</summary>
        public static Vector3 CameraPosition(float h, float k, out Vector3 aim)
        {
            aim = new Vector3(0, h * AimHeight, 0);
            return aim + new Vector3(h * 0.9f, h * (0.45f - AimHeight), -h * 1.4f) * k;
        }

        /// <summary>NDC of a world point for the rig (h, k), aspect, vertical FOV, band f.</summary>
        public static Vector2 Project(Vector3 p, float h, float k, float aspect, float vFovDeg, float visibleFraction)
        {
            var pos = CameraPosition(h, k, out var aim);
            var view = Matrix4x4.TRS(pos, Quaternion.LookRotation(aim - pos), new Vector3(1, 1, -1)).inverse;
            var proj = ShiftedProjection(Matrix4x4.Perspective(vFovDeg, aspect, 0.5f, 200000f), visibleFraction);
            var c = proj * (view * new Vector4(p.x, p.y, p.z, 1));
            if (c.w <= 1e-4f) return new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            return new Vector2(c.x / c.w, c.y / c.w);
        }

        static bool Fits(IReadOnlyList<Vector3> pts, float h, float k, float aspect, float fov, float f)
        {
            f = Mathf.Clamp(f, MinVisibleFraction, 1f);
            float padY = FitPadding * 2f * f, padX = FitPadding * 2f;
            float yLo = 1f - 2f * f + padY, yHi = 1f - padY, xLo = -1f + padX, xHi = 1f - padX;
            foreach (var p in pts)
            {
                var n = Project(p, h, k, aspect, fov, f);
                if (!(n.x > xLo && n.x < xHi && n.y > yLo && n.y < yHi)) return false;
            }
            return true;
        }

        /// <summary>Smallest distance scale (never tighter than DistanceScale(f)) at which all points fit the band.</summary>
        public static float FitDistanceScale(IReadOnlyList<Vector3> pts, float h, float aspect, float vFovDeg, float visibleFraction)
        {
            float k0 = DistanceScale(visibleFraction);
            if (pts == null || pts.Count == 0 || h <= 0f || aspect <= 0f) return k0;
            if (Fits(pts, h, k0, aspect, vFovDeg, visibleFraction)) return k0;
            float lo = k0, hi = k0 * 2f;
            for (int i = 0; i < 12 && !Fits(pts, h, hi, aspect, vFovDeg, visibleFraction); i++) { lo = hi; hi *= 2f; }
            for (int i = 0; i < 40; i++) { float mid = 0.5f * (lo + hi); if (Fits(pts, h, mid, aspect, vFovDeg, visibleFraction)) hi = mid; else lo = mid; }
            return hi;
        }

        /// <summary>Subject points: vehicle box (radius r, height h) on the deck, deck rim at deckRadius (y = 0 and
        /// GroundY plinth foot), and any extra points (tower / arm bounds).</summary>
        public static List<Vector3> SubjectPoints(float h, float r, float deckRadius, IEnumerable<Vector3> extra = null)
        {
            var pts = new List<Vector3>();
            foreach (var y in new[] { 0f, h }) for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2; pts.Add(new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a))); }
            foreach (var y in new[] { 0f, PadGround.GroundY })
                for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; pts.Add(new Vector3(deckRadius * Mathf.Cos(a), y, deckRadius * Mathf.Sin(a))); }
            if (extra != null) pts.AddRange(extra);
            return pts;
        }

        /// <summary>Corners of the generated pad members' bounds (tower, arms) through padToWorld.</summary>
        public static IEnumerable<Vector3> PadBounds(Kestrel.Pad.PadLayout L, Matrix4x4 padToWorld)
        {
            if (L == null || L.Members == null || L.Members.Count == 0) yield break;
            Vector3 mn = Vector3.positiveInfinity, mx = Vector3.negativeInfinity;
            foreach (var m in L.Members)
            {
                var a = new Vector3(m.A.X, m.A.Y, m.A.Z); var b = new Vector3(m.B.X, m.B.Y, m.B.Z);
                mn = Vector3.Min(mn, Vector3.Min(a, b)); mx = Vector3.Max(mx, Vector3.Max(a, b));
            }
            for (int i = 0; i < 8; i++)
                yield return padToWorld.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? mn.x : mx.x, (i & 2) == 0 ? mn.y : mx.y, (i & 4) == 0 ? mn.z : mx.z));
        }
    }
}
