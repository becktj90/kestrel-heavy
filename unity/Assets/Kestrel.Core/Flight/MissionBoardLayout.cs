// Mission board frames. Portrait is a constellation map over a pinned sheet.
// The sheet never covers a node: the path ends above it. A wide viewport
// (WebLayoutMode, the #252 landscape path) puts the map on the left and the
// sheet on the right. Points, origin top-left. Thrum keeps the top-right corner.
using System;

namespace Kestrel.Core.Flight
{
    public sealed class MissionBoardFrame
    {
        public bool Wide;
        public HudBox Header, Home, Title, Progress, Path, Card, Art, Name, Facts, Brief, Primary, Contracts, Sandbox, Thrum;
    }

    /// <summary>One patch center in the scroll content. Y grows downward. The star row sits under the disc.</summary>
    public readonly struct BoardNode
    {
        public readonly float X, Y;
        public BoardNode(float x, float y) { X = x; Y = y; }
    }

    public static class MissionBoardLayout
    {
        public const float Edge = 8f;
        public const float Gap = 8f;
        public const float HeaderH = 56f;
        public const float ButtonH = 44f;
        public const float PrimaryH = 52f;
        public const float ThrumPt = 64f;
        public const float HomeW = 76f;
        public const float MinPath = 120f;
        public const float NameH = 26f;
        public const float FactsH = 40f;
        public const float BriefH = 36f;
        public const float ArtMin = 56f;
        public const float Pad = 10f;
        public const float NodeR = 22f;
        public const float StarRow = 12f;
        public const float NodeStep = 78f;
        public const int ActSlots = 5;

        public static MissionBoardFrame Place(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, bool wide)
        {
            if (width < 1f) width = 1f;
            if (height < 1f) height = 1f;
            safeTop = Clamp0(safeTop);
            safeBottom = Clamp0(safeBottom);
            safeLeft = Clamp0(safeLeft);
            safeRight = Clamp0(safeRight);
            if (safeTop + safeBottom > height - 1f) { safeTop = 0f; safeBottom = 0f; }
            if (safeLeft + safeRight > width - 1f) { safeLeft = 0f; safeRight = 0f; }

            float left = safeLeft + Edge;
            float right = width - safeRight - Edge;
            float top = safeTop + Edge;
            float bottom = height - safeBottom - Edge;
            if (right < left + 80f) { left = Edge; right = width - Edge; }
            if (bottom < top + 80f) { top = Edge; bottom = height - Edge; }

            var f = new MissionBoardFrame { Wide = wide };
            f.Thrum = Box(width - safeRight - Edge - ThrumPt, safeTop + Edge, ThrumPt, ThrumPt);
            if (wide) PlaceWide(f, left, right, top, bottom);
            else PlacePortrait(f, left, right, top, bottom);
            return f;
        }

        /// <summary>Scroll-content centers for a zigzag constellation. Every disc and its stars stay inside the path width.</summary>
        public static BoardNode[] Nodes(float pathW, int count)
        {
            if (count < 0) count = 0;
            if (pathW < NodeR * 2f + 8f) pathW = NodeR * 2f + 8f;
            var nodes = new BoardNode[count];
            float left = pathW * 0.33f;
            float right = pathW * 0.67f;
            float min = NodeR + 4f;
            float max = pathW - NodeR - 4f;
            if (left < min) left = min;
            if (right > max) right = max;
            if (right < left) right = left;
            for (int i = 0; i < count; i++)
            {
                float x = (i % 2 == 0) ? left : right;
                float y = Edge + NodeR + i * NodeStep;
                nodes[i] = new BoardNode(x, y);
            }
            return nodes;
        }

        public static float ContentHeight(int count)
        {
            if (count <= 0) return Edge * 2f;
            return Edge + NodeR + (count - 1) * NodeStep + NodeR + StarRow + Edge;
        }

        /// <summary>One segment of the act bar. Slots match the flights in the act.</summary>
        public static HudBox Segment(HudBox bar, int index, int count)
        {
            if (count < 1) count = 1;
            if (index < 0) index = 0;
            if (index >= count) index = count - 1;
            float gap = count > 1 ? 3f : 0f;
            float w = (bar.W - gap * (count - 1)) / count;
            if (w < 1f) w = 1f;
            return Box(bar.X + index * (w + gap), bar.Y, w, bar.H);
        }

        static void PlacePortrait(MissionBoardFrame f, float left, float right, float top, float bottom)
        {
            float innerW = Math.Max(1f, right - left);
            PlaceHeader(f, left, top, innerW, right);
            float pathTop = Math.Max(top + HeaderH + Gap, f.Thrum.Bottom + Gap);
            float bodyH = Math.Max(1f, bottom - pathTop);
            float sheetH = Clamp(bodyH * 0.46f, 220f, 300f);
            float pathH = bodyH - sheetH - Gap;
            if (pathH < MinPath)
            {
                pathH = Math.Min(MinPath, Math.Max(88f, bodyH * 0.36f));
                sheetH = Math.Max(180f, bodyH - pathH - Gap);
            }
            f.Path = Box(left, pathTop, innerW, pathH);
            f.Card = Box(left, f.Path.Bottom + Gap, innerW, Math.Max(1f, bottom - (f.Path.Bottom + Gap)));
            PlaceSheet(f);
        }

        static void PlaceWide(MissionBoardFrame f, float left, float right, float top, float bottom)
        {
            float innerW = Math.Max(1f, right - left);
            float colW = Clamp(innerW * 0.40f, 220f, 460f);
            if (colW > innerW - 200f) colW = Math.Max(180f, innerW * 0.42f);
            PlaceHeader(f, left, top, colW, left + colW);
            float pathTop = top + HeaderH + Gap;
            f.Path = Box(left, pathTop, colW, Math.Max(88f, bottom - pathTop));

            float cardX = left + colW + Gap;
            float cardTop = top;
            if (f.Thrum.Right > cardX && f.Thrum.X < right) cardTop = Math.Max(top, f.Thrum.Bottom + Gap);
            f.Card = Box(cardX, cardTop, Math.Max(140f, right - cardX), Math.Max(140f, bottom - cardTop));
            PlaceSheet(f);
        }

        static void PlaceHeader(MissionBoardFrame f, float left, float top, float width, float rightLimit)
        {
            float homeW = Math.Min(HomeW, Math.Max(44f, width * 0.28f));
            f.Home = Box(left, top + (HeaderH - ButtonH) * 0.5f, homeW, ButtonH);
            float titleX = f.Home.Right + Gap;
            float titleR = Math.Min(rightLimit, f.Thrum.X - Gap);
            if (titleR < titleX + 48f) titleR = Math.Min(rightLimit, titleX + 48f);
            float titleW = Math.Max(48f, titleR - titleX);
            f.Title = Box(titleX, top + 2f, titleW, 30f);
            f.Progress = Box(titleX, f.Title.Bottom + 4f, titleW, 8f);
            f.Header = Box(left, top, width, HeaderH);
        }

        static void PlaceSheet(MissionBoardFrame f)
        {
            var c = f.Card;
            float x = c.X + Pad;
            float y = c.Y + Pad;
            float right = c.Right - Pad;
            float w = Math.Max(1f, right - x);
            float limit = c.Bottom - Pad;

            f.Primary = Box(x, limit - PrimaryH, w, PrimaryH);
            float footTop = f.Primary.Y - Gap - ButtonH;
            float half = (w - Gap) * 0.5f;
            if (half < 44f) half = Math.Max(36f, (w - Gap) * 0.5f);
            f.Contracts = Box(x, footTop, half, ButtonH);
            f.Sandbox = Box(x + half + Gap, footTop, Math.Max(36f, right - (x + half + Gap)), ButtonH);

            float factsTop = footTop - Gap - FactsH;
            float heroBottom = factsTop - Gap;
            float heroH = Math.Max(48f, heroBottom - y);
            float art = Math.Min(104f, Math.Min(heroH - 2f, w * 0.38f));
            if (art < ArtMin) art = Math.Min(ArtMin, Math.Max(40f, heroH - 2f));
            if (art > w - 80f) art = Math.Max(40f, w * 0.34f);
            f.Art = Box(x, y, art, art);

            float tx = f.Art.Right + Gap;
            float tw = Math.Max(40f, right - tx);
            float nameH = Math.Min(NameH, Math.Max(18f, art * 0.42f));
            f.Name = Box(tx, y, tw, nameH);
            float briefTop = f.Name.Bottom + 4f;
            float briefRoom = f.Art.Bottom - briefTop;
            if (briefRoom < 16f)
            {
                briefTop = f.Art.Bottom + 4f;
                briefRoom = Math.Max(16f, heroBottom - briefTop);
            }
            f.Brief = Box(tx, briefTop, tw, Math.Min(BriefH, Math.Max(16f, briefRoom)));
            float under = Math.Max(f.Art.Bottom, f.Brief.Bottom) + 4f;
            if (under > factsTop) factsTop = under;
            float factsH = Math.Max(14f, Math.Min(FactsH, footTop - Gap - factsTop));
            f.Facts = Box(x, factsTop, w, factsH);
        }

        static float Clamp0(float v) => v > 0f ? v : 0f;
        static float Clamp(float v, float lo, float hi) => Math.Max(lo, Math.Min(hi, v));
        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
