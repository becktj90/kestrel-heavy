// Pure portrait-scaling math from docs/VISUAL_SPEC.md §5 (no UnityEngine references, so it
// runs under dotnet and EditMode alike). All numbers come from data/visual-spec.json via
// SpecPortrait; nothing here is hand-copied except the defaults that mirror that file.
//
// Coordinates: pixels, origin bottom-left (Unity Screen / Screen.safeArea convention).
// "du" = design units of the 1280x720 world (VISUAL_SPEC §1). "pt" = iOS points (px / dpr).
using System;

namespace Kestrel.Game.Layout
{
    public readonly struct RectPx
    {
        public readonly double X, Y, W, H;
        public RectPx(double x, double y, double w, double h) { X = x; Y = y; W = w; H = h; }
        public double XMax => X + W;
        public double YMax => Y + H;
        public override string ToString() => $"({X:0.#},{Y:0.#} {W:0.#}x{H:0.#})";
    }

    /// <summary>Subset of VISUAL_SPEC §6 the layout needs ("world" + "portrait").</summary>
    public sealed class SpecPortrait
    {
        public double WorldW = 1280, WorldH = 720;
        public double StageKMax = 2, StageEdgeMax = 4096, KStep = 0.25;
        public bool Letterbox = false;
        public double SideMarginPt = 8, MinTouchPt = 44;
        public double VehicleAnchorY = 0.38, PlumeMaxWidthOfVisible = 0.8;
    }

    public readonly struct PortraitFrame
    {
        /// <summary>Envelop (cover) scale, device px per du. Height dominates in portrait.</summary>
        public readonly double PxPerDu;
        /// <summary>World extent visible on screen, in du (portrait: width &lt; 1280, height = 720).</summary>
        public readonly double VisibleWDu, VisibleHDu;
        /// <summary>Backing-store multiplier k from §5 (quarter steps, capped). Informational for Unity:
        /// render resolution is owned by the quality preset (G1), not by k.</summary>
        public readonly double StageK;
        /// <summary>Safe rect (Screen.safeArea) shrunk by the 8 pt side margin. HUD/vehicle/plume live here.</summary>
        public readonly RectPx Content;
        /// <summary>Screen-space y (px from bottom) where the vehicle sits during ascent (0.38 of content from top).</summary>
        public readonly double VehicleAnchorPxY;
        /// <summary>Max drawn plume width in du (0.8 x visible width).</summary>
        public readonly double PlumeMaxWidthDu;
        /// <summary>Minimum touch target in px (44 pt).</summary>
        public readonly double MinTouchPx;
        /// <summary>Areas outside the world (bars) are always 0 in envelop mode; anything not covered by
        /// world art (notch, rounding, overscroll) is painted with the themed fill, never black.</summary>
        public readonly bool Letterboxed;

        public PortraitFrame(double pxPerDu, double vw, double vh, double k, RectPx content, double anchorY, double plumeMax, double minTouch)
        { PxPerDu = pxPerDu; VisibleWDu = vw; VisibleHDu = vh; StageK = k; Content = content; VehicleAnchorPxY = anchorY; PlumeMaxWidthDu = plumeMax; MinTouchPx = minTouch; Letterboxed = false; }
    }

    public static class PortraitLayout
    {
        /// <summary>Portrait-only: true when the screen is taller than wide (square counts as portrait).</summary>
        public static bool IsPortrait(double widthPx, double heightPx) => heightPx >= widthPx;

        /// <summary>Envelop/cover scale: max(vw/W, vh/H). Never FIT, never RESIZE (VISUAL_SPEC §5 rule 1).</summary>
        public static double EnvelopScale(double widthPx, double heightPx, SpecPortrait s)
            => Math.Max(widthPx / s.WorldW, heightPx / s.WorldH);

        /// <summary>k = clamp(round4(max(vw/W, vh/H) * dpr), 1, min(kMax, edge/W, edge/H)).
        /// widthPx*heightPx are device pixels, which already include dpr.</summary>
        public static double StageK(double widthPx, double heightPx, SpecPortrait s)
        {
            double raw = EnvelopScale(widthPx, heightPx, s);
            double q = Math.Round(raw / s.KStep, MidpointRounding.AwayFromZero) * s.KStep;
            double cap = Math.Min(s.StageKMax, Math.Min(s.StageEdgeMax / s.WorldW, s.StageEdgeMax / s.WorldH));
            return Math.Max(1, Math.Min(cap, q));
        }

        public static PortraitFrame Compute(double widthPx, double heightPx, RectPx safeArea, double dpr, SpecPortrait s)
        {
            if (widthPx <= 0 || heightPx <= 0) throw new ArgumentOutOfRangeException(nameof(widthPx));
            if (dpr <= 0) dpr = 1;
            double scale = EnvelopScale(widthPx, heightPx, s);
            double visW = widthPx / scale, visH = heightPx / scale;

            // Clamp the reported safe area to the screen (simulators sometimes report slop).
            double sx = Math.Max(0, safeArea.X), sy = Math.Max(0, safeArea.Y);
            double sxm = Math.Min(widthPx, safeArea.XMax), sym = Math.Min(heightPx, safeArea.YMax);
            double margin = s.SideMarginPt * dpr;
            var content = new RectPx(sx + margin, sy, Math.Max(0, sxm - sx - 2 * margin), Math.Max(0, sym - sy));

            double anchorY = content.YMax - s.VehicleAnchorY * content.H;
            return new PortraitFrame(scale, visW, visH, StageK(widthPx, heightPx, s), content, anchorY,
                s.PlumeMaxWidthOfVisible * visW, s.MinTouchPt * dpr);
        }

        /// <summary>Safe area in px (bottom-left origin) from top/bottom insets in pt, as listed in the
        /// VISUAL_SPEC device table. Used by tests and the editor preview harness.</summary>
        public static RectPx SafeFromInsets(double widthPt, double heightPt, double topPt, double bottomPt, double dpr)
            => new RectPx(0, bottomPt * dpr, widthPt * dpr, (heightPt - topPt - bottomPt) * dpr);

        /// <summary>Insets of a screen-space content rect (bottom-left origin, px) expressed in panel units.
        /// Fractions of the screen, so a panel laid out for a render target that is not <paramref name="screenW"/>
        /// × <paramref name="screenH"/> still covers that screen instead of a corner of it. When the panel's
        /// display rect is the screen, this matches dividing by the Scale-With-Screen-Size scale.</summary>
        public static PanelEdgeInsets MapContentToPanel(double screenW, double screenH, RectPx content, double panelW, double panelH)
        {
            if (screenW <= 0 || screenH <= 0 || panelW <= 0 || panelH <= 0)
                return new PanelEdgeInsets(0, 0, 0, 0);
            double left = content.X / screenW * panelW;
            double right = (screenW - content.XMax) / screenW * panelW;
            double bottom = content.Y / screenH * panelH;
            double top = (screenH - content.YMax) / screenH * panelH;
            return new PanelEdgeInsets(Math.Max(0, left), Math.Max(0, top), Math.Max(0, right), Math.Max(0, bottom));
        }

        /// <summary>Largest font size in [<paramref name="minFont"/>, <paramref name="maxFont"/>] whose text,
        /// measured at <paramref name="textWidthAtMax"/> when drawn at <paramref name="maxFont"/>, fits
        /// <paramref name="available"/>. Linear in font size (dynamic UI Toolkit fonts).</summary>
        public static double FitFontSize(double available, double maxFont, double minFont, double textWidthAtMax)
        {
            if (available <= 0 || textWidthAtMax <= 0 || maxFont <= 0 || double.IsNaN(available) || double.IsNaN(textWidthAtMax))
                return minFont;
            if (textWidthAtMax <= available) return maxFont;
            double fit = maxFont * available / textWidthAtMax;
            return fit < minFont ? minFont : fit;
        }
    }

    /// <summary>Panel-space insets (left, top, right, bottom), same edge order as a UI Toolkit absolute element.</summary>
    public readonly struct PanelEdgeInsets
    {
        public readonly double Left, Top, Right, Bottom;
        public PanelEdgeInsets(double left, double top, double right, double bottom)
        { Left = left; Top = top; Right = right; Bottom = bottom; }
        public double Width(double panelW) => Math.Max(0, panelW - Left - Right);
        public double Height(double panelH) => Math.Max(0, panelH - Top - Bottom);
    }

    /// <summary>VISUAL_SPEC §4: atmo→vacuum blend and the themed fill colour behind everything.</summary>
    public static class SkyBlend
    {
        public static double Clamp01(double x) => double.IsNaN(x) ? 0 : Math.Max(0, Math.Min(1, x));

        public static double Smoothstep(double e0, double e1, double x)
        {
            double span = e1 - e0;
            if (span == 0) return x >= e1 ? 1 : 0;
            double t = Clamp01((x - e0) / span);
            return t * t * (3 - 2 * t);
        }

        /// <summary>spaceBlend(altKm, status): smoothstep(6, 56, altKm); 1 in SEP / JACKLYN.</summary>
        public static double SpaceBlend(double altKm, string status = null, double lo = 6, double hi = 56)
            => status == "SEP" || status == "JACKLYN" ? 1 : Smoothstep(lo, hi, altKm);

        /// <summary>lerpRgb from tod.js (0xRRGGBB ints, per-channel round).</summary>
        public static int LerpRgb(int a, int b, double t)
        {
            double k = Clamp01(t);
            int L(int sh) { int ac = (a >> sh) & 255, bc = (b >> sh) & 255; return (int)Math.Round(ac + (bc - ac) * k, MidpointRounding.AwayFromZero) & 255; }
            return (L(16) << 16) | (L(8) << 8) | L(0);
        }

        /// <summary>Themed fill = lerpRgb(wash.horizon, wash.space, spaceBlend).</summary>
        public static int ThemedFill(int washHorizon, int washSpace, double spaceT) => LerpRgb(washHorizon, washSpace, spaceT);

        public static int ParseHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("empty colour");
            return Convert.ToInt32(hex.TrimStart('#'), 16);
        }
    }
}
