// Mission board frames. Portrait is a constellation map over a pinned sheet.
// The sheet never covers a node: the path ends above it. A wide viewport
// (WebLayoutMode, the #252 landscape path) puts the map on the left and the
// sheet on the right. Points, origin top-left. Thrum keeps the top-right corner.
//
// t461 (TestFlight 204, ~471x1024): the card clipped its brief and the Need / Difficulty rows (fixed 36 / 40 pt
// boxes, a 300 pt sheet cap, a clipping copy column, the theme's label padding), the act title's second line was cut
// by a 32 pt box and drawn under the progress bar, and Patches sat inside the map on top of the KH-2 node.
// Now: every text block is sized from its copy (word wrap budgeted with FlightHudLayout.BudgetWidth, whole-pixel line
// boxes of ceil(font x 1.3)); the sheet grows to the longest card it can show, and past the room it has the body
// scrolls (Body / MissionBoardBody.ContentH) while Contracts, Sandbox and the primary stay pinned; the header is its
// own rows (title block, then Patches when the title needs the width) and the map starts under them.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Flight
{
    public sealed class MissionBoardFrame
    {
        public bool Wide;
        public HudBox Header, Home, Title, Progress, Path, Card, Art, Name, Facts, Brief, Primary, Contracts, Sandbox, Thrum;
        /// <summary>The kicker line, the act title (one or two lines) and the Patches key: all in the header rows.</summary>
        public HudBox Kicker, Act, Patches;
        /// <summary>The scrolling viewport of the sheet body (art, copy, facts); the three keys sit under it.</summary>
        public HudBox Body;
        public float ActFontPt = MissionBoardLayout.ActPt;
        public int ActLines = 1;
        /// <summary>The body of the copy the sheet was sized for, laid out in <see cref="Body"/>.</summary>
        public MissionBoardBody Sized;
    }

    /// <summary>Card copy as drawn: what the layout measures.</summary>
    public sealed class MissionBoardCopy
    {
        public string Act = "", Name = "", Brief = "", Customer = "", Orbit = "", FactsTop = "", FactsBottom = "";

        public static MissionBoardCopy Of(MissionBoardFacts card, string act)
        {
            var c = new MissionBoardCopy { Act = act ?? "" };
            if (card == null) return c;
            c.Name = card.Title ?? "";
            c.Brief = card.Briefing ?? "";
            c.Customer = card.Customer ?? "";
            c.Orbit = card.Orbit ?? "";
            c.FactsTop = Join(card.Need, card.Mass);
            c.FactsBottom = Join(card.Difficulty, card.Best);
            return c;
        }

        public static string Join(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b ?? "";
            if (string.IsNullOrEmpty(b)) return a;
            return a + "  ·  " + b;
        }

        /// <summary>Both fact lines as one label ("Need · Mass" over "Difficulty · Best").</summary>
        public string Facts => FactsTop.Length == 0 ? FactsBottom : FactsBottom.Length == 0 ? FactsTop : FactsTop + "\n" + FactsBottom;

        /// <summary>A long card the default layout is sized and tested for.</summary>
        public static MissionBoardCopy Reserve => new MissionBoardCopy
        {
            Act = "Act I · First Light",
            Name = "KH-14  Brineglass Relay",
            Brief = "Park a 2,200 kg weather satellite in a sun-synchronous 550 km orbit.",
            Customer = "Customer · Brineglass Weather Bureau",
            Orbit = "Sun-sync 550 km at 97.6 degrees",
            FactsTop = "Need · 9,412 m/s  ·  Mass · 2,200 kg",
            FactsBottom = "Difficulty · Steady  ·  Best · no flight yet",
        };
    }

    /// <summary>
    /// The sheet body for one card. Boxes are relative to the scroll content (x from the body's left, y from the
    /// content top). <see cref="ContentH"/> is the full height; past <see cref="ViewH"/> the body scrolls.
    /// </summary>
    public struct MissionBoardBody
    {
        public HudBox Art, Name, Customer, Orbit, Brief, Facts;
        public int NameLines, CustomerLines, OrbitLines, BriefLines, FactsLines;
        public float ContentH, ViewH;
        public bool Scrolls => ContentH > ViewH + 0.5f;
        public float Overflow => Math.Max(0f, ContentH - ViewH);
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
        public const float HeaderH = 64f;
        public const float ButtonH = 44f;
        public const float PrimaryH = 52f;
        public const float ThrumPt = 64f;
        public const float HomeW = 76f;
        public const float MinPath = 120f;
        public const float NameH = 26f;
        public const float FactsH = 40f;
        /// <summary>Two fact lines at the 10 pt text-fit floor.</summary>
        public const float FactsTwoLineH = 28f;
        public const float BriefH = 36f;
        public const float ArtMin = 56f;
        public const float ArtMax = 104f;
        public const float Pad = 10f;
        public const float NodeR = 22f;
        public const float StarRow = 12f;
        public const float NodeStep = 78f;
        public const int ActSlots = 5;

        /// <summary>Faces the board draws, points.</summary>
        public const float KickerPt = 14f;
        public const float ActPt = 16f;
        public const float ActMinPt = 13f;
        public const float NamePt = 20f;
        public const float BodyPt = 14f;
        public const float PatchesW = 92f;
        public const float ProgressH = 8f;
        /// <summary>Line box: ceil(font x LineK), whole pixels (FlightHudLayout.LineHeight).</summary>
        public const float LineK = 1.3f;
        /// <summary>Under this width the text column beside the art holds only the name; customer and orbit run full width.</summary>
        public const float SideColumnMin = 200f;

        public static float LineBox(float fontPt) => (float)Math.Ceiling(fontPt * LineK - 0.001f);

        /// <summary>Wrapped line count for a label <paramref name="boxW"/> wide, widths budgeted the way Unity draws them.</summary>
        public static int Lines(string text, float boxW, float fontPt)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            float room = Math.Max(1f, boxW - 2f);
            int n = FlightHudLayout.WrapLines(text, room, fontPt);
            if (n != int.MaxValue) return n;
            // A word wider than the box: Unity breaks it by characters.
            int lines = 0;
            foreach (var para in text.Split('\n'))
                lines += Math.Max(1, (int)Math.Ceiling(FlightHudLayout.BudgetWidth(para, fontPt) / room));
            return lines;
        }

        public static MissionBoardFrame Place(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, bool wide)
            => Place(width, height, safeTop, safeBottom, safeLeft, safeRight, wide, null);

        /// <summary>
        /// Frame for a board that shows <paramref name="copies"/> (every mission card plus Contracts and Sandbox). The
        /// header fits the longest act title, the sheet the tallest card; null uses <see cref="MissionBoardCopy.Reserve"/>.
        /// </summary>
        public static MissionBoardFrame Place(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, bool wide, IReadOnlyList<MissionBoardCopy> copies)
        {
            if (width < 1f) width = 1f;
            if (height < 1f) height = 1f;
            safeTop = Clamp0(safeTop);
            safeBottom = Clamp0(safeBottom);
            safeLeft = Clamp0(safeLeft);
            safeRight = Clamp0(safeRight);
            if (safeTop + safeBottom > height - 1f) { safeTop = 0f; safeBottom = 0f; }
            if (safeLeft + safeRight > width - 1f) { safeLeft = 0f; safeRight = 0f; }
            if (copies == null || copies.Count == 0) copies = new[] { MissionBoardCopy.Reserve };

            float left = Snap(safeLeft + Edge);
            float right = Snap(width - safeRight - Edge);
            float top = Snap(safeTop + Edge);
            float bottom = Snap(height - safeBottom - Edge);
            if (right < left + 80f) { left = Edge; right = Snap(width - Edge); }
            if (bottom < top + 80f) { top = Edge; bottom = Snap(height - Edge); }

            var f = new MissionBoardFrame { Wide = wide };
            f.Thrum = Box(right - ThrumPt, top, ThrumPt, ThrumPt);
            string act = "";
            foreach (var c in copies) if (c != null && (c.Act ?? "").Length > act.Length) act = c.Act;
            if (wide) PlaceWide(f, left, right, top, bottom, act, copies);
            else PlacePortrait(f, left, right, top, bottom, act, copies);
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

        static void PlacePortrait(MissionBoardFrame f, float left, float right, float top, float bottom, string act, IReadOnlyList<MissionBoardCopy> copies)
        {
            float innerW = Math.Max(1f, right - left);
            float headBottom = PlaceHeader(f, left, top, innerW, right, act);
            float pathTop = Math.Max(headBottom + Gap, f.Thrum.Bottom + Gap);
            float bodyH = Math.Max(1f, bottom - pathTop);

            // The sheet is as tall as the tallest card it shows, up to what leaves the map its minimum.
            float need = 0f;
            foreach (var c in copies) need = Math.Max(need, SheetNeed(innerW, c));
            float maxSheet = bodyH - MinPath - Gap;
            float sheetH = Math.Min(need, maxSheet);
            float minSheet = Pad * 2f + ButtonH + Gap + PrimaryH + Gap + ArtMin;
            if (maxSheet < minSheet)
            {
                // A very short window: the map keeps 88 pt or more, the sheet the rest (its body scrolls).
                float pathH0 = Math.Min(MinPath, Math.Max(88f, bodyH * 0.36f));
                sheetH = Math.Max(180f, bodyH - pathH0 - Gap);
            }
            float pathH = Math.Max(1f, bodyH - sheetH - Gap);
            f.Path = Box(left, pathTop, innerW, Snap(pathH));
            f.Card = Box(left, f.Path.Bottom + Gap, innerW, Math.Max(1f, bottom - (f.Path.Bottom + Gap)));
            PlaceSheet(f, Tallest(innerW, copies));
        }

        static void PlaceWide(MissionBoardFrame f, float left, float right, float top, float bottom, string act, IReadOnlyList<MissionBoardCopy> copies)
        {
            float innerW = Math.Max(1f, right - left);
            float colW = Clamp(innerW * 0.40f, 220f, 460f);
            if (colW > innerW - 200f) colW = Math.Max(180f, innerW * 0.42f);
            colW = Snap(colW);
            float headBottom = PlaceHeader(f, left, top, colW, left + colW, act);
            float pathTop = headBottom + Gap;
            f.Path = Box(left, pathTop, colW, Math.Max(88f, bottom - pathTop));

            float cardX = left + colW + Gap;
            float cardTop = top;
            if (f.Thrum.Right > cardX && f.Thrum.X < right) cardTop = Math.Max(top, f.Thrum.Bottom + Gap);
            f.Card = Box(cardX, cardTop, Math.Max(140f, right - cardX), Math.Max(140f, bottom - cardTop));
            PlaceSheet(f, Tallest(f.Card.W, copies));
        }

        /// <summary>
        /// Home, then the title block (kicker, the act title on one or two whole-pixel lines, the act bar), Thrum at the
        /// right. Patches takes the right of that row when the title still fits beside it, else a row of its own under
        /// it. Returns the bottom of the header rows; the map starts under them, so no node is ever under a header key.
        /// </summary>
        static float PlaceHeader(MissionBoardFrame f, float left, float top, float width, float rightLimit, string act)
        {
            float homeW = Math.Min(HomeW, Math.Max(44f, width * 0.28f));
            f.Home = Box(left, top + Snap((HeaderH - ButtonH) * 0.5f), Snap(homeW), ButtonH);
            float titleX = f.Home.Right + Gap;
            bool beside = f.Thrum.Y < top + HeaderH && f.Thrum.X < rightLimit;
            float titleR = beside ? Math.Min(rightLimit, f.Thrum.X - Gap) : rightLimit;
            bool dropped = false;
            if (beside && titleR - titleX < 120f)
            {
                // The act name needs the row. The guide slot drops under the title block (placed below).
                dropped = true;
                beside = false;
                titleR = rightLimit;
            }
            if (titleR < titleX + 48f) titleR = Math.Min(rightLimit, titleX + 48f);

            // Patches beside the title when the act title still fits on one line next to it.
            float actOneLine = FlightHudLayout.BudgetWidth(act, ActPt) + 2f;
            bool patchesInRow = titleR - titleX - (PatchesW + Gap) >= Math.Max(120f, actOneLine);
            if (patchesInRow) titleR -= PatchesW + Gap;
            float titleW = Math.Max(48f, titleR - titleX);

            var fit = FlightHudLayout.WrapFit(act, titleW, float.PositiveInfinity, 2, ActPt, ActMinPt, allowCut: false);
            f.ActFontPt = fit.FontPt;
            f.ActLines = Math.Max(1, Math.Min(2, Lines(act, titleW, fit.FontPt)));
            float kickerH = LineBox(KickerPt);
            float actH = f.ActLines * LineBox(f.ActFontPt);
            f.Kicker = Box(titleX, top + 2f, titleW, kickerH);
            f.Act = Box(titleX, f.Kicker.Bottom, titleW, actH);
            f.Title = Box(titleX, f.Kicker.Y, titleW, kickerH + actH);
            f.Progress = Box(titleX, f.Title.Bottom + 4f, titleW, ProgressH);
            float rowBottom = Math.Max(top + HeaderH, f.Progress.Bottom + 2f);
            if (beside) rowBottom = Math.Max(rowBottom, f.Thrum.Bottom);
            if (dropped) f.Thrum = Box(f.Thrum.X, rowBottom + 4f, f.Thrum.W, f.Thrum.H);
            if (patchesInRow)
            {
                f.Patches = Box(titleR + Gap, top + Snap((HeaderH - ButtonH) * 0.5f), PatchesW, ButtonH);
                f.Header = Box(left, top, width, rowBottom - top);
                return rowBottom;
            }
            // Patches gets the row under the title, right-aligned (left of a dropped Thrum); the act bar runs from
            // under Home to Patches, centred on it, so the row costs one key height and no node sits under a key.
            float py = f.Act.Bottom + 2f;
            if (beside) py = Math.Max(py, f.Thrum.Bottom + 2f);
            float pr = rightLimit;
            if (dropped)
            {
                f.Thrum = Box(f.Thrum.X, py, f.Thrum.W, f.Thrum.H);
                pr = f.Thrum.X - Gap;
            }
            f.Patches = Box(Math.Max(left, pr - PatchesW), py, PatchesW, ButtonH);
            float barX = left;
            float barY = Math.Max(f.Home.Bottom + 2f, py + Snap((ButtonH - ProgressH) * 0.5f));
            float barW = f.Patches.X - Gap - barX;
            if (barW >= 48f)
            {
                f.Progress = Box(barX, barY, barW, ProgressH);
            }
            else
            {
                f.Patches = Box(f.Patches.X, Math.Max(py, f.Progress.Bottom + Gap), PatchesW, ButtonH);
                if (dropped) f.Thrum = Box(f.Thrum.X, f.Patches.Y, f.Thrum.W, f.Thrum.H);
            }
            float bottom = Math.Max(Math.Max(f.Patches.Bottom, f.Progress.Bottom + 2f), dropped ? f.Thrum.Bottom : 0f);
            f.Header = Box(left, top, width, bottom - top);
            return bottom;
        }

        /// <summary>The tallest card's copy for a sheet <paramref name="cardW"/> wide.</summary>
        static MissionBoardCopy Tallest(float cardW, IReadOnlyList<MissionBoardCopy> copies)
        {
            MissionBoardCopy best = null;
            float h = -1f;
            foreach (var c in copies)
            {
                if (c == null) continue;
                float n = SheetNeed(cardW, c);
                if (n > h) { h = n; best = c; }
            }
            return best ?? MissionBoardCopy.Reserve;
        }

        /// <summary>Sheet height that shows all of <paramref name="copy"/> without scrolling.</summary>
        public static float SheetNeed(float cardW, MissionBoardCopy copy)
        {
            float w = Math.Max(1f, cardW - 2f * Pad);
            var body = Body(w, float.PositiveInfinity, copy);
            return Pad + body.ContentH + Gap + KeysH(w) + Pad;
        }

        /// <summary>Height of the pinned keys: Contracts and Sandbox side by side over the primary, or stacked.</summary>
        static float KeysH(float w)
        {
            float half = (w - Gap) * 0.5f;
            return half >= 44f ? ButtonH + Gap + PrimaryH : PrimaryH + Gap + ButtonH + Gap + ButtonH;
        }

        static void PlaceSheet(MissionBoardFrame f, MissionBoardCopy sizing)
        {
            var c = f.Card;
            float x = c.X + Pad;
            float y = c.Y + Pad;
            float right = c.Right - Pad;
            float w = Math.Max(1f, right - x);
            float limit = c.Bottom - Pad;

            f.Primary = Box(x, limit - PrimaryH, w, PrimaryH);
            float footTop = f.Primary.Y - Gap - ButtonH;
            float half = Snap((w - Gap) * 0.5f);
            if (half >= 44f)
            {
                f.Contracts = Box(x, footTop, half, ButtonH);
                f.Sandbox = Box(x + half + Gap, footTop, Math.Max(44f, right - (x + half + Gap)), ButtonH);
            }
            else
            {
                // A side-by-side pair would be under 44 pt. Stack full-width keys instead.
                f.Sandbox = Box(x, limit - ButtonH, w, ButtonH);
                f.Contracts = Box(x, f.Sandbox.Y - Gap - ButtonH, w, ButtonH);
                f.Primary = Box(x, f.Contracts.Y - Gap - PrimaryH, w, PrimaryH);
            }

            // Keys own the bottom. The body (art, copy, facts) is the rest and scrolls when the copy is longer.
            float stackTop = Math.Min(f.Primary.Y, Math.Min(f.Contracts.Y, f.Sandbox.Y));
            f.Body = Box(x, y, w, Math.Max(1f, stackTop - Gap - y));
            var body = Body(w, f.Body.H, sizing);
            f.Sized = body;
            f.Art = Visible(f.Body, body.Art);
            f.Name = Visible(f.Body, body.Name);
            f.Brief = Visible(f.Body, body.Brief);
            f.Facts = Visible(f.Body, body.Facts);
        }

        /// <summary>The body of <paramref name="copy"/> in a viewport <paramref name="w"/> x <paramref name="viewH"/>.</summary>
        public static MissionBoardBody Body(MissionBoardFrame f, MissionBoardCopy copy) => Body(f.Body.W, f.Body.H, copy);

        public static MissionBoardBody Body(float w, float viewH, MissionBoardCopy copy)
        {
            if (copy == null) copy = new MissionBoardCopy();
            // Three arrangements; the shortest wins (ties keep the earlier), so a phone card stays whole where it can.
            var best = Arrange(w, viewH, copy, BodyMode.Side);
            foreach (var mode in new[] { BodyMode.Stacked, BodyMode.BriefBeside })
            {
                var b = Arrange(w, viewH, copy, mode);
                if (b.ContentH + 0.01f < best.ContentH) best = b;
            }
            return best;
        }

        enum BodyMode { Side, Stacked, BriefBeside }

        /// <summary>
        /// Side: name, customer and orbit beside the art (a phone trades a little art for a 200 pt column), brief and
        /// facts full width. Stacked: name beside the art, brief, customer, orbit and facts full width. BriefBeside:
        /// name and brief beside the art, customer, orbit and facts full width.
        /// </summary>
        static MissionBoardBody Arrange(float w, float viewH, MissionBoardCopy copy, BodyMode mode)
        {
            var b = new MissionBoardBody { ViewH = viewH };
            float art = Snap(Clamp(w * 0.3f, ArtMin, ArtMax));
            if (art > w - 80f) art = Snap(Math.Max(40f, w * 0.34f));
            if (mode != BodyMode.Stacked && w - art - Gap < SideColumnMin && w - Gap - SideColumnMin >= ArtMin)
                art = Snap(w - Gap - SideColumnMin);
            if (!float.IsInfinity(viewH) && art > viewH) art = Snap(Math.Max(40f, viewH));
            b.Art = Box(0f, 0f, art, art);

            float tx = art + Gap;
            float tw = Math.Max(40f, w - tx);
            float nameLb = LineBox(NamePt), bodyLb = LineBox(BodyPt);
            b.NameLines = Lines(copy.Name, tw, NamePt);
            b.Name = Box(tx, 0f, tw, b.NameLines * nameLb);
            float y = b.Name.Bottom;
            if (mode == BodyMode.Side && tw < SideColumnMin) mode = BodyMode.Stacked;

            float fullY;
            if (mode == BodyMode.Side)
            {
                b.CustomerLines = Lines(copy.Customer, tw, BodyPt);
                b.Customer = Box(tx, y + (b.CustomerLines > 0 ? 4f : 0f), tw, b.CustomerLines * bodyLb);
                y = b.Customer.Bottom;
                b.OrbitLines = Lines(copy.Orbit, tw, BodyPt);
                b.Orbit = Box(tx, y + (b.OrbitLines > 0 ? 2f : 0f), tw, b.OrbitLines * bodyLb);
                y = b.Orbit.Bottom;
                fullY = Math.Max(art, y) + Gap;
                b.BriefLines = Lines(copy.Brief, w, BodyPt);
                b.Brief = Box(0f, fullY, w, b.BriefLines * bodyLb);
                fullY = b.Brief.Bottom;
            }
            else
            {
                if (mode == BodyMode.BriefBeside)
                {
                    b.BriefLines = Lines(copy.Brief, tw, BodyPt);
                    b.Brief = Box(tx, y + 4f, tw, b.BriefLines * bodyLb);
                    fullY = Math.Max(art, b.Brief.Bottom);
                }
                else
                {
                    fullY = Math.Max(art, y) + Gap;
                    b.BriefLines = Lines(copy.Brief, w, BodyPt);
                    b.Brief = Box(0f, fullY, w, b.BriefLines * bodyLb);
                    fullY = b.Brief.Bottom;
                }
                b.CustomerLines = Lines(copy.Customer, w, BodyPt);
                b.Customer = Box(0f, fullY + (b.CustomerLines > 0 ? 4f : 0f), w, b.CustomerLines * bodyLb);
                fullY = b.Customer.Bottom;
                b.OrbitLines = Lines(copy.Orbit, w, BodyPt);
                b.Orbit = Box(0f, fullY + (b.OrbitLines > 0 ? 2f : 0f), w, b.OrbitLines * bodyLb);
                fullY = b.Orbit.Bottom;
            }
            b.FactsLines = Lines(copy.FactsTop, w, BodyPt) + Lines(copy.FactsBottom, w, BodyPt);
            b.Facts = Box(0f, fullY + (b.FactsLines > 0 ? Gap : 0f), w, b.FactsLines * bodyLb);
            b.ContentH = Snap(Math.Max(b.Art.Bottom, b.Facts.Bottom));
            return b;
        }

        /// <summary>The on-screen part of a body box at scroll 0, clipped to the viewport (empty when below it).</summary>
        static HudBox Visible(HudBox view, HudBox content)
        {
            float x0 = view.X + content.X, y0 = view.Y + content.Y;
            float x1 = Math.Min(view.Right, x0 + content.W), y1 = Math.Min(view.Bottom, y0 + content.H);
            if (y1 <= y0 || x1 <= x0) return Box(x0, Math.Min(y0, view.Bottom), 0f, 0f);
            return Box(x0, y0, x1 - x0, y1 - y0);
        }

        /// <summary>Whole points (the panel's pixel grid on a 1:1 panel; UI Toolkit snaps scaled panels from these).</summary>
        public static float Snap(float v) => (float)Math.Floor(v + 0.5f);

        static float Clamp0(float v) => v > 0f ? v : 0f;
        static float Clamp(float v, float lo, float hi) => Math.Max(lo, Math.Min(hi, v));
        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
