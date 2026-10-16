// Designer composition. Portrait keeps a bottom sheet. Landscape puts the parts list on the left
// of the rocket and the readouts (with Launch) on the right.
using System;

namespace Kestrel.Core.Flight
{
    public sealed class WebDesignerFrame
    {
        public bool Wide;
        public HudBox Rocket, Parts, Readouts, Launch, Exit, Home, RightPanel;
    }

    public static class WebDesignerLayout
    {
        public const float Gap = 12f;
        public const float Edge = 8f;
        public const float LaunchH = 52f;
        public const float HomeW = 88f;
        public const float HomeH = 44f;

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
            f.Home = Rect(safeLeft + Edge, safeTop, HomeW, HomeH);

            float left = safeLeft + Edge;
            float right = width - safeRight - Edge;
            float bottom = height - safeBottom - Edge;
            if (!f.Wide)
            {
                PlacePhone(f, left, right, bottom, height);
                return f;
            }

            float top = Math.Max(f.Exit.Bottom, f.Home.Bottom + 36f) + Gap;
            float col = Clamp(width * 0.24f, 200f, 400f);
            float span = right - left;
            float center = span - 2f * col - 2f * Gap;
            if (center < width * 0.22f)
                col = Math.Max(150f, (span - width * 0.22f - 2f * Gap) * 0.5f);

            float colH = Math.Max(80f, bottom - top);
            f.Parts = Rect(left, top, col, colH);
            float rightX = right - col;
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
