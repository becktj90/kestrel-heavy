// Post-flight report fit (L4). Points, the card's own box. The live 1280x800 card was a scrolling column:
// huge type, a full-width button per exit with Home below the fold, an empty line under Apogee and text on
// the left edge. This sizes the sheet so the body and every exit fit with no scrolling at every size:
// a width-capped column with side margins, the primary exit full width over a 2- or 3-column grid, body type that steps down
// to the 14 pt floor and then drops the plots and the long narrator lines (they stay in the tooltip and the codex).
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Flight
{
    public static class ReportFit
    {
        /// <summary>The card never lays out shorter or narrower than this many points (short landscape web panels).</summary>
        public const float MinHeightPt = 440f;
        public const float MinWidthPt = 320f;
        /// <summary>Body and exits column cap, and the least side margin.</summary>
        public const float MaxContentPt = 720f;
        public const float SidePt = 16f;
        public const float HitPt = 48f;
        public const float GapPt = 8f;
        /// <summary>Three exits per row from this content width; two below.</summary>
        public const float ThreeColumnPt = 540f;
        /// <summary>Body type floor: 16 pt body to 14 pt (the report legibility floor), 22 pt titles to 19.25.</summary>
        public const float MinScale = 0.875f;
        public const float LineBox = 1.3f;
        public const float LineTopPt = 2f;
        public const float LineBottomPt = 6f;
        public const float PlotsPt = 96f;

        /// <summary>
        /// Panel pixels per point, capped so the card is at least <see cref="MinHeightPt"/> by
        /// <see cref="MinWidthPt"/> points (a 1280x800 web panel at the phone ratio drew 2.4x type).
        /// </summary>
        public static float Units(float upp, float widthPx, float heightPx)
        {
            if (!(upp > 0f)) upp = 1f;
            if (heightPx > 1f && heightPx / upp < MinHeightPt) upp = heightPx / MinHeightPt;
            if (widthPx > 1f && widthPx / upp < MinWidthPt) upp = Math.Min(upp, widthPx / MinWidthPt);
            return upp;
        }

        public static float ContentWidth(float widthPt) => Math.Max(120f, Math.Min(MaxContentPt, widthPt - 2f * SidePt));

        public static float SideMargin(float widthPt) => Math.Max(SidePt, (widthPt - ContentWidth(widthPt)) * 0.5f);

        public static int Columns(float contentWidthPt, int count)
        {
            if (count <= 1) return 1;
            return contentWidthPt >= ThreeColumnPt ? Math.Min(3, count) : 2;
        }

        public static int Rows(int count, int columns) => columns <= 0 ? count : (count + columns - 1) / columns;

        /// <summary>
        /// The exits footer: the first exit (the primary, "Fix it" on today's card) is one full-width row, and the
        /// rest share a 2-column grid (3 from <see cref="ThreeColumnPt"/>). A short last row splits the width
        /// between its own buttons. Six exits on an iPhone SE are 1 + 3 rows; on iPad and the web panels 1 + 2.
        /// </summary>
        public static int GridColumns(float contentWidthPt, int count) => count <= 1 ? 1 : Columns(contentWidthPt, count - 1);

        public static int FootRows(float contentWidthPt, int count)
        {
            if (count <= 0) return 0;
            if (count == 1) return 1;
            return 1 + Rows(count - 1, GridColumns(contentWidthPt, count));
        }

        public struct Cell
        {
            public int Row, Col, InRow;
            public float X, Y, W;
        }

        /// <summary>Exit <paramref name="index"/> of <paramref name="count"/>: its row, column and box in points from the grid's top-left.</summary>
        public static Cell GridCell(float contentWidthPt, int count, int index)
        {
            var c = new Cell();
            if (count <= 0 || index < 0 || index >= count) return c;
            if (index == 0)
            {
                c.InRow = 1; c.W = contentWidthPt;
                return c;
            }
            int cols = GridColumns(contentWidthPt, count);
            int rest = count - 1, i = index - 1;
            int gridRow = i / cols;
            c.Row = 1 + gridRow;
            c.Col = i % cols;
            c.InRow = Math.Min(cols, rest - gridRow * cols);
            c.W = ButtonWidth(contentWidthPt, c.InRow);
            c.X = c.Col * (c.W + GapPt);
            c.Y = c.Row * (HitPt + GapPt);
            return c;
        }

        public static float ButtonWidth(float contentWidthPt, int columns)
            => columns <= 1 ? contentWidthPt : (contentWidthPt - (columns - 1) * GapPt) / columns;

        /// <summary>Exit grid height: rows of hit targets with gaps, top pad, the safe bottom and a pad.</summary>
        public static float FootPt(int rows, float safeBottomPt)
            => rows * HitPt + Math.Max(0, rows - 1) * GapPt + 8f + Math.Max(12f, safeBottomPt) + 8f;

        /// <summary>One wrapped label: whole lines at <paramref name="fontPt"/> plus its margins.</summary>
        public static float LinePt(string text, float fontPt, float widthPt)
        {
            if (string.IsNullOrEmpty(text) || fontPt <= 0f) return 0f;
            float w = Math.Max(8f, widthPt);
            int lines = 0;
            foreach (var para in text.Split('\n'))
            {
                // Word wrap with Unity-sized widths (FlightHudLayout.MeasureFactor); a word wider than the column breaks by character.
                int wrapped = FlightHudLayout.WrapLines(para, w, fontPt);
                if (wrapped == int.MaxValue) wrapped = (int)Math.Ceiling(FlightHudLayout.BudgetWidth(para, fontPt) / w) + 1;
                lines += Math.Max(1, wrapped);
            }
            return lines * fontPt * LineBox + LineTopPt + LineBottomPt;
        }

        public struct Line
        {
            public string Name, Text;
            public float FontPt;
            /// <summary>Dropped, in this order (low first), when type at the floor still does not fit.</summary>
            public int DropOrder;
        }

        public struct Plan
        {
            public float Scale;
            public bool Plots;
            public HashSet<string> Dropped;
            public float BodyPt;
            public bool Fits;
        }

        /// <summary>
        /// Body type scale and what to drop so <paramref name="lines"/> (and the plots, when present) fit
        /// <paramref name="roomPt"/> at <paramref name="widthPt"/>.
        /// </summary>
        public static Plan Body(IList<Line> lines, bool plots, float widthPt, float roomPt)
        {
            var plan = new Plan { Scale = 1f, Plots = plots, Dropped = new HashSet<string>() };
            for (float s = 1f; s >= MinScale - 0.001f; s -= 0.05f)
            {
                plan.Scale = s;
                plan.BodyPt = Height(lines, plan, widthPt);
                if (plan.BodyPt <= roomPt) { plan.Fits = true; return plan; }
            }
            plan.Scale = MinScale;
            if (plan.Plots)
            {
                plan.Plots = false;
                plan.BodyPt = Height(lines, plan, widthPt);
                if (plan.BodyPt <= roomPt) { plan.Fits = true; return plan; }
            }
            var order = new List<Line>();
            foreach (var l in lines) if (l.DropOrder > 0) order.Add(l);
            order.Sort((a, b) => a.DropOrder.CompareTo(b.DropOrder));
            foreach (var l in order)
            {
                plan.Dropped.Add(l.Name);
                plan.BodyPt = Height(lines, plan, widthPt);
                if (plan.BodyPt <= roomPt) { plan.Fits = true; return plan; }
            }
            return plan;
        }

        static float Height(IList<Line> lines, Plan plan, float widthPt)
        {
            float h = 0f;
            foreach (var l in lines)
                if (!plan.Dropped.Contains(l.Name)) h += LinePt(l.Text, l.FontPt * plan.Scale, widthPt);
            if (plan.Plots) h += PlotsPt * plan.Scale + GapPt;
            return h;
        }
    }
}
