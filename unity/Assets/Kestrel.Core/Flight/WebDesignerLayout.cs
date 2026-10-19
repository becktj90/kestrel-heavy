// Designer composition. Portrait keeps a bottom sheet. Landscape is three panes in a centred band:
// the parts list left of the rocket, the readouts (with Launch) on the right.
using System;

namespace Kestrel.Core.Flight
{
    public sealed class WebDesignerFrame
    {
        public bool Wide;
        public HudBox Rocket, Parts, Readouts, Launch, Exit, Home, RightPanel;
        /// <summary>The "N things to fix" pill: its own band under Home, never on the panel or its title.</summary>
        public HudBox Issue;
    }

    public static class WebDesignerLayout
    {
        public const float Gap = 12f;
        public const float Edge = 8f;
        public const float LaunchH = 52f;
        public const float HomeW = 88f;
        public const float HomeH = 44f;
        /// <summary>Home sits 8 pt in from the safe corner (HomeButtonMetrics.InsetPt).</summary>
        public const float HomeInset = 8f;
        /// <summary>Pill band: 32 pt minimum target for a 15 pt label.</summary>
        public const float IssueH = 32f;
        public const float IssueMaxW = 260f;

        public static WebDesignerFrame Place(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight)
        {
            var f = new WebDesignerFrame { Wide = WebLayoutMode.IsLandscape(width, height) };
            safeTop = Math.Max(0f, safeTop);
            safeBottom = Math.Max(0f, safeBottom);
            safeLeft = Math.Max(0f, safeLeft);
            safeRight = Math.Max(0f, safeRight);
            float exitW = HomeCornerLayout.ExitW;
            float exitH = HomeCornerLayout.ExitH;
            f.Exit = Rect(width - safeRight - Edge - exitW, safeTop, exitW, exitH);
            f.Home = Rect(safeLeft + Edge, safeTop + HomeInset, HomeW, HomeH);
            f.Issue = Rect(safeLeft + Edge, f.Home.Bottom + HomeInset, IssueMaxW, IssueH);

            float left = safeLeft + Edge;
            float right = width - safeRight - Edge;
            float bottom = height - safeBottom - Edge;
            if (!f.Wide)
            {
                PlacePhone(f, left, right, bottom, height);
                return f;
            }

            float top = Math.Max(f.Exit.Bottom, f.Issue.Bottom) + Gap;
            // Three panes in a centred band at most 1280 wide (LANDSCAPE_WEB_BRIEF §3): the guided
            // stepper / parts column left (300-400), rocket centre (at least 22% of the band), readouts
            // and Launch right (200-300). Nothing is past the 400 pt side panel (DECISIONS #62).
            WebLandscapeType.Band(left, right, out float bandLeft, out float bandRight);
            float span = bandRight - bandLeft;
            float col = WebLandscapeType.PartsColumn(span);
            float side = WebLandscapeType.ReadoutColumn(span);

            float colH = Math.Max(80f, bottom - top);
            f.Parts = Rect(bandLeft, top, col, colH);
            float rightX = bandRight - side;
            col = side;
            f.Launch = Rect(rightX + Edge, top + colH - Edge - LaunchH, col - 2f * Edge, LaunchH);
            f.Readouts = Rect(rightX, top, col, Math.Max(44f, f.Launch.Y - Gap - top));
            f.RightPanel = Rect(rightX, top, col, colH);
            float rocketX = f.Parts.Right + Gap;
            f.Rocket = Rect(rocketX, top, Math.Max(1f, f.RightPanel.X - Gap - rocketX), colH);
            return f;
        }

        static void PlacePhone(WebDesignerFrame f, float left, float right, float bottom, float height)
        {
            float width = Math.Max(1f, right - left);
            float sheetTop = Math.Max(f.Home.Bottom + Gap, height * 0.46f);
            f.Rocket = Rect(left, f.Home.Bottom + Gap, width, Math.Max(1f, sheetTop - Gap - (f.Home.Bottom + Gap)));
            float sheetH = Math.Max(80f, bottom - sheetTop);
            f.Launch = Rect(left + Edge, bottom - LaunchH, width - 2f * Edge, LaunchH);
            float readH = Math.Max(44f, sheetH * 0.34f);
            f.Readouts = Rect(left, sheetTop, width, readH);
            float partsY = f.Readouts.Bottom + Gap;
            f.Parts = Rect(left, partsY, width, Math.Max(44f, f.Launch.Y - Gap - partsY));
            f.RightPanel = Rect(left, sheetTop, width, bottom - sheetTop);
        }

        static float Clamp(float v, float lo, float hi) => Math.Max(lo, Math.Min(hi, v));

        static HudBox Rect(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
