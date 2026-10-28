// The X-ray view's composition, in points (docs/DESIGNER_TIERS_SPEC.md, "X-ray (all tiers)"). A full-screen layer over
// the Starter screen: "◀ Rocket" top-left with the X-ray title, the cutaway column on the left (nose cone, then per stage
// its tanks, engines and the interstage below it, each a 44 pt tap target whose height follows the part's real length,
// boosters as a tappable side column), the tapped part's card on the right (title, plain line, numbers only in
// Engineer) and the flight plan strip along the bottom. Pure rectangles, checked like StarterLayout (overlaps, safe area,
// 44 pt targets, text fit with FlightHudLayout's conservative advance); the Unity view pins its elements to these boxes.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Designer
{
    public sealed class StarterXrayInput
    {
        public float Width, Height, SafeTop, SafeBottom, SafeLeft, SafeRight;
        public bool ReserveExit;
        public string Close = "", Title = "", Plan = "";
        /// <summary>The card: the tapped part's title, line and (Engineer only) numbers; with nothing tapped, the pick line.</summary>
        public string CardTitle = "", CardLine = "", CardNumbers = "";
        public XrayModel Model = new XrayModel();
    }

    public sealed class StarterXrayFrame
    {
        public HudBox Close, Title, Exit, Cutaway, Card, CardTitle, CardLine, CardNumbers, Strip, PlanTitle;
        /// <summary>Per model part (same order as XrayModel.Parts): its tap target. Boosters sit beside stage 1.</summary>
        public readonly List<KeyValuePair<string, HudBox>> Parts = new List<KeyValuePair<string, HudBox>>();
        /// <summary>The right-hand booster drawing (decoration; the left column is the tap target).</summary>
        public HudBox BoosterMirror;
        /// <summary>Per flight plan event: the cell, its name and its planned time.</summary>
        public readonly List<HudBox> Cells = new List<HudBox>(), CellLabels = new List<HudBox>(), CellClocks = new List<HudBox>();
        public float TitlePt = StarterXrayLayout.TitlePt, CardTitlePt = StarterXrayLayout.CardTitlePt, CardPt = StarterXrayLayout.CardPt, CellPt = StarterXrayLayout.CellPt;
        public int CardLines;
        public readonly List<StarterText> Texts = new List<StarterText>();

        public HudBox PartBox(string id)
        {
            foreach (var kv in Parts) if (kv.Key == id) return kv.Value;
            return default;
        }
    }

    public static class StarterXrayLayout
    {
        public const float Edge = StarterLayout.Edge, Gap = StarterLayout.Gap, Touch = StarterLayout.Touch, ColumnMax = 640f;
        public const float ClosePt = StarterLayout.CornerPt, TitlePt = 20f, CardTitlePt = 16f, CardPt = 14f, NumbersPt = 13f, CellPt = 13f;
        public const float CardPad = 10f, CutMax = 180f, CutFraction = 0.45f, BoosterW = 44f;

        public static float Line(float pt) => StarterLayout.Line(pt);

        public static StarterXrayFrame Place(StarterXrayInput i)
        {
            if (i == null) throw new ArgumentNullException(nameof(i));
            var f = new StarterXrayFrame();
            float st = Math.Max(0f, i.SafeTop), sb = Math.Max(0f, i.SafeBottom), sl = Math.Max(0f, i.SafeLeft), sr = Math.Max(0f, i.SafeRight);
            float left = sl + Edge, right = i.Width - sr - Edge, top = st + Edge, bottom = i.Height - sb - Edge;
            float avail = Math.Max(1f, right - left);
            float colW = Math.Min(avail, ColumnMax), colL = left + (avail - colW) * 0.5f, colR = colL + colW;

            // Top row: close, title (and the web EXIT reserve on the right).
            float closeW = Math.Max(StarterLayout.CornerW, (float)Math.Ceiling(FlightHudLayout.TextWidth(i.Close, ClosePt)) + 2f * StarterLayout.ButtonPadX + 2f);
            f.Close = Box(left, top, closeW, Touch);
            float titleRight = right;
            if (i.ReserveExit)
            {
                f.Exit = Box(i.Width - sr - Edge - HomeCornerLayout.ExitW, st, HomeCornerLayout.ExitW, HomeCornerLayout.ExitH);
                titleRight = f.Exit.X - Gap;
            }
            f.Title = Box(f.Close.Right + Gap, top, Math.Max(8f, titleRight - f.Close.Right - Gap), Touch);
            float pt = TitlePt;
            while (pt > 15f && FlightHudLayout.TextWidth(i.Title, pt) > f.Title.W) pt -= 1f;
            f.TitlePt = pt;
            float contentTop = Math.Max(f.Close.Bottom, f.Exit.Live ? f.Exit.Bottom : 0f) + Gap;

            // Flight plan strip at the bottom: the "Flight plan" line, then one cell per event (name over time).
            var events = i.Model?.Events ?? new List<XrayEvent>();
            float cellH = 2f * Line(CellPt);
            float stripH = events.Count > 0 ? Line(CellPt) + 4f + cellH : 0f;
            if (stripH > 0f)
            {
                f.Strip = Box(colL, bottom - stripH, colW, stripH);
                f.PlanTitle = Box(colL, f.Strip.Y, colW, Line(CellPt));
                float cy = f.PlanTitle.Bottom + 4f;
                float each = (colW - 4f * (events.Count - 1)) / events.Count;
                for (int k = 0; k < events.Count; k++)
                {
                    var c = Box(colL + k * (each + 4f), cy, each, cellH);
                    f.Cells.Add(c);
                    f.CellLabels.Add(Box(c.X, c.Y, c.W, Line(CellPt)));
                    f.CellClocks.Add(Box(c.X, c.Y + Line(CellPt), c.W, Line(CellPt)));
                }
            }
            float contentBottom = stripH > 0f ? f.Strip.Y - Gap : bottom;

            // Cutaway column, left.
            var parts = i.Model?.Parts ?? new List<XrayPart>();
            bool boosters = false;
            foreach (var p in parts) if (p.Kind == XrayPartKind.Boosters) boosters = true;
            float cutW = Math.Min(CutMax, Math.Max(boosters ? 2f * BoosterW + 2f * 4f + Touch : Touch, colW * CutFraction));
            f.Cutaway = Box(colL, contentTop, cutW, Math.Max(0f, contentBottom - contentTop));
            float coreW = boosters ? cutW - 2f * (BoosterW + 4f) : Math.Min(cutW, 110f);
            float coreX = colL + (cutW - coreW) * 0.5f;
            var stack = new List<XrayPart>();
            foreach (var p in parts) if (p.Kind != XrayPartKind.Boosters) stack.Add(p);
            double sumL = 0;
            foreach (var p in stack) sumL += Math.Max(0.1, p.LengthM);
            float extra = Math.Max(0f, f.Cutaway.H - stack.Count * Touch);
            float y = f.Cutaway.Y;
            HudBox tank1 = default, eng1 = default;
            foreach (var p in stack)
            {
                float h = Touch + (sumL > 0 ? (float)(extra * Math.Max(0.1, p.LengthM) / sumL) : 0f);
                var b = Box(coreX, y, coreW, h);
                f.Parts.Add(new KeyValuePair<string, HudBox>(p.Id, b));
                if (p.Kind == XrayPartKind.Tank && p.Stage == 1) tank1 = b;
                if (p.Kind == XrayPartKind.Engines && p.Stage == 1) eng1 = b;
                y += h;
            }
            foreach (var p in parts)
            {
                if (p.Kind != XrayPartKind.Boosters || !tank1.Live) continue;
                float by = tank1.Y, bh = (eng1.Live ? eng1.Bottom : tank1.Bottom) - by;
                f.Parts.Add(new KeyValuePair<string, HudBox>(p.Id, Box(colL, by, BoosterW, bh)));
                f.BoosterMirror = Box(colL + cutW - BoosterW, by, BoosterW, bh);
            }

            // The part card, right of the cutaway.
            float cardX = f.Cutaway.Right + Gap + 4f, cardW = Math.Max(Touch, colR - cardX);
            float innerX = cardX + CardPad, innerW = Math.Max(8f, cardW - 2f * CardPad);
            float cy2 = contentTop + CardPad;
            int titleLines = string.IsNullOrEmpty(i.CardTitle) ? 0 : Math.Max(1, DesignerSheetLayout.LineCount(i.CardTitle, innerW, CardTitlePt));
            if (titleLines > 0) { f.CardTitle = Box(innerX, cy2, innerW, titleLines * Line(CardTitlePt)); cy2 = f.CardTitle.Bottom + 4f; }
            f.CardLines = string.IsNullOrEmpty(i.CardLine) ? 0 : Math.Max(1, DesignerSheetLayout.LineCount(i.CardLine, innerW, CardPt));
            if (f.CardLines > 0) { f.CardLine = Box(innerX, cy2, innerW, f.CardLines * Line(CardPt)); cy2 = f.CardLine.Bottom + 4f; }
            if (!string.IsNullOrEmpty(i.CardNumbers))
            {
                int n = Math.Max(1, DesignerSheetLayout.LineCount(i.CardNumbers, innerW, NumbersPt));
                f.CardNumbers = Box(innerX, cy2, innerW, n * Line(NumbersPt));
                cy2 = f.CardNumbers.Bottom + 4f;
            }
            f.Card = Box(cardX, contentTop, cardW, cy2 - 4f + CardPad - contentTop);
            Collect(f, i);
            return f;
        }

        static void Collect(StarterXrayFrame f, StarterXrayInput i)
        {
            void T(string name, string text, HudBox b, float pt, float pad, bool wraps)
            {
                if (!b.Live || string.IsNullOrEmpty(text)) return;
                f.Texts.Add(new StarterText { Name = name, Text = text, Box = b, Pt = pt, PadX = pad, Wraps = wraps });
            }
            T("close", i.Close, f.Close, ClosePt, StarterLayout.ButtonPadX, false);
            T("title", i.Title, f.Title, f.TitlePt, 0f, false);
            T("card-title", i.CardTitle, f.CardTitle, CardTitlePt, 0f, true);
            T("card-line", i.CardLine, f.CardLine, CardPt, 0f, true);
            T("card-numbers", i.CardNumbers, f.CardNumbers, NumbersPt, 0f, true);
            T("plan", i.Plan, f.PlanTitle, CellPt, 0f, false);
            var ev = i.Model?.Events;
            if (ev != null)
                for (int k = 0; k < ev.Count && k < f.Cells.Count; k++)
                {
                    T("event-" + ev[k].Id, ev[k].Label, f.CellLabels[k], CellPt, 0f, false);
                    T("clock-" + ev[k].Id, ev[k].Clock, f.CellClocks[k], CellPt, 0f, false);
                }
        }

        /// <summary>Every leaf that must not intersect another (the cutaway and card containers excluded).</summary>
        public static List<KeyValuePair<string, HudBox>> Leaves(StarterXrayFrame f)
        {
            var o = new List<KeyValuePair<string, HudBox>>();
            void Add(string n, HudBox b) { if (b.Live) o.Add(new KeyValuePair<string, HudBox>(n, b)); }
            Add("close", f.Close); Add("title", f.Title); Add("exit", f.Exit);
            foreach (var kv in f.Parts) Add("part-" + kv.Key, kv.Value);
            Add("booster-mirror", f.BoosterMirror);
            Add("card-title", f.CardTitle); Add("card-line", f.CardLine); Add("card-numbers", f.CardNumbers);
            Add("plan", f.PlanTitle);
            for (int k = 0; k < f.Cells.Count; k++) { Add("cell-label-" + k, f.CellLabels[k]); Add("cell-clock-" + k, f.CellClocks[k]); }
            return o;
        }

        public static List<string> Problems(StarterXrayFrame f, StarterXrayInput i)
        {
            var p = new List<string>();
            var leaves = Leaves(f);
            for (int a = 0; a < leaves.Count; a++)
                for (int b = a + 1; b < leaves.Count; b++)
                    if (leaves[a].Value.Overlaps(leaves[b].Value)) p.Add(leaves[a].Key + " overlaps " + leaves[b].Key);
            float l = Math.Max(0f, i.SafeLeft), t = Math.Max(0f, i.SafeTop);
            float r = i.Width - Math.Max(0f, i.SafeRight), btm = i.Height - Math.Max(0f, i.SafeBottom);
            foreach (var kv in leaves)
            {
                var b = kv.Value;
                if (b.X < l - 0.5f || b.Y < t - 0.5f || b.Right > r + 0.5f || b.Bottom > btm + 0.5f) p.Add(kv.Key + " outside the safe area");
            }
            if (f.Card.Bottom > (f.Strip.Live ? f.Strip.Y : btm) + 0.5f) p.Add("card runs into the flight plan");
            if (f.Card.Overlaps(f.Cutaway)) p.Add("card overlaps the cutaway");
            foreach (var kv in f.Parts)
            {
                var b = kv.Value;
                if (b.W < Touch - 0.01f || b.H < Touch - 0.01f) p.Add("part " + kv.Key + " target " + b.W.ToString("0.#") + "x" + b.H.ToString("0.#") + " under 44");
                if (b.Bottom > f.Cutaway.Bottom + 0.5f) p.Add("part " + kv.Key + " below the cutaway");
            }
            if (f.Close.W < Touch || f.Close.H < Touch) p.Add("close under 44");
            foreach (var tx in f.Texts)
            {
                float w = tx.Box.W - 2f * tx.PadX;
                if (tx.Wraps)
                {
                    int lines = DesignerSheetLayout.LineCount(tx.Text, w, tx.Pt);
                    if (lines * Line(tx.Pt) > tx.Box.H + 0.5f) p.Add(tx.Name + " needs " + lines + " lines in " + tx.Box.H.ToString("0") + " pt");
                    foreach (var word in tx.Text.Split(' '))
                        if (FlightHudLayout.TextWidth(word, tx.Pt) > w + 0.5f) p.Add(tx.Name + " word \"" + word + "\" wider than its box");
                }
                else if (FlightHudLayout.TextWidth(tx.Text, tx.Pt) > w + 0.5f)
                    p.Add(tx.Name + " \"" + tx.Text + "\" needs " + FlightHudLayout.TextWidth(tx.Text, tx.Pt).ToString("0") + " in " + w.ToString("0"));
                if (Line(tx.Pt) > tx.Box.H + 0.5f) p.Add(tx.Name + " line taller than its box");
            }
            return p;
        }

        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
