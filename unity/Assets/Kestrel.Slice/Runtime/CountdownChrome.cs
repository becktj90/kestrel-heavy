// The countdown's bottom stack and COUNT tag, dressed inline while the count runs (TestFlight 202, #333).
// Layout and colours come from Kestrel.Core.Flight.CountdownStack (pure, tested in Kestrel.Core.Tests): one Ink
// card with a sun edge holding the title, the spare line and one row with the status pill and Skip count.
// Before: "On the count" banner, a loose cream spare line on the sky, a "Deluge" banner that read as a button and
// a 22 pt skip bar. Steady styles only (photosensitivity): no blink, no pulse. Whole pixels on the scaled panel.
// AscentHud adopts the same webcast card at liftoff, so Undress puts the tree and inline styles back first.
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class CountdownChrome
    {
        public const string RowName = "count-row", StatusName = "count-status", DotName = "count-status-dot";

        static Color C(CountdownStack.Rgba c) => new Color(c.R, c.G, c.B, c.A);
        static float Px(float pt, float u) => Mathf.Round(pt * u);

        public static bool Dressed(VisualElement cast) => cast != null && cast.Q(RowName) != null;

        /// <summary>Dress the webcast card for the count. <paramref name="cardWPt"/>: the card's outer width (pt).</summary>
        public static CountdownStackFit Dress(VisualElement cast, Label verdict, Label pulse, Label callout, Button skip, float u, float cardWPt, bool skipShown)
        {
            u = u > 0.5f ? u : 1f;
            string status = callout != null && callout.style.display != DisplayStyle.None ? callout.text : null;
            string spare = pulse != null ? pulse.text : null;
            var fit = CountdownStack.Fit(cardWPt, verdict != null ? verdict.text : null, spare, status, skipShown && skip != null);
            if (cast == null) return fit;
            var row = EnsureRow(cast, callout, skip, out var pill, out var dot);

            // The card: ink plate, sun edge, whole-pixel padding and radius.
            cast.style.backgroundColor = FlightDusk.Ink;
            cast.style.flexDirection = FlexDirection.Column;
            cast.style.alignItems = Align.Stretch;
            cast.style.paddingLeft = Px(CountdownStack.CardPadXPt, u);
            cast.style.paddingRight = Px(CountdownStack.CardPadXPt, u);
            cast.style.paddingTop = Px(CountdownStack.CardPadYPt, u);
            cast.style.paddingBottom = Px(CountdownStack.CardPadYPt, u);
            float r = Px(CountdownStack.CardRadiusPt, u);
            cast.style.borderTopLeftRadius = r; cast.style.borderTopRightRadius = r;
            cast.style.borderBottomLeftRadius = r; cast.style.borderBottomRightRadius = r;
            cast.style.borderTopWidth = 0; cast.style.borderLeftWidth = 0; cast.style.borderRightWidth = 0;
            cast.style.borderBottomWidth = Px(CountdownStack.EdgePt, u);
            cast.style.borderBottomColor = FlightDusk.Sun;
            cast.style.overflow = Overflow.Hidden;

            float room = Mathf.Max(1f, cardWPt - 2f * CountdownStack.CardPadXPt);
            if (verdict != null)
            {
                Plain(verdict);
                float titlePt = CountdownStack.OneLine(verdict.text, room, CountdownStack.TitlePt, CountdownStack.TitleMinPt);
                int lines = FlightHudLayout.WrapLines(verdict.text ?? "", room - 2f, titlePt);
                lines = lines == int.MaxValue ? 2 : Mathf.Max(1, lines);
                verdict.style.fontSize = Px(titlePt, u);
                verdict.style.height = Px(lines * CountdownStack.LinePt(titlePt), u);
                verdict.style.color = FlightDusk.Cream;
                verdict.style.unityFontStyleAndWeight = FontStyle.Bold;
                verdict.style.whiteSpace = WhiteSpace.Normal;
                verdict.style.unityTextAlign = TextAnchor.MiddleCenter;
                verdict.style.display = string.IsNullOrEmpty(verdict.text) ? DisplayStyle.None : DisplayStyle.Flex;
                FlightDusk.Face(verdict, FlightDusk.TitleFace());
            }
            if (pulse != null)
            {
                Plain(pulse);
                bool show = !string.IsNullOrEmpty(spare);
                pulse.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                pulse.style.fontSize = Px(fit.SparePt > 0f ? fit.SparePt : CountdownStack.SparePt, u);
                pulse.style.height = Px(Mathf.Max(1, fit.SpareLines) * CountdownStack.LinePt(fit.SparePt > 0f ? fit.SparePt : CountdownStack.SparePt), u);
                pulse.style.marginTop = Px(CountdownStack.RowGapPt, u);
                pulse.style.color = FlightDusk.Cream;
                pulse.style.whiteSpace = WhiteSpace.Normal;
                pulse.style.unityTextAlign = TextAnchor.MiddleCenter;
                pulse.style.overflow = Overflow.Hidden;
                FlightDusk.Face(pulse, FlightDusk.BodyFace());
            }

            bool hasStatus = !string.IsNullOrEmpty(status);
            bool hasSkip = skipShown && skip != null;
            row.style.display = hasStatus || hasSkip ? DisplayStyle.Flex : DisplayStyle.None;
            row.style.flexDirection = fit.OneRow ? FlexDirection.Row : FlexDirection.Column;
            row.style.justifyContent = hasStatus && hasSkip && fit.OneRow ? Justify.SpaceBetween : Justify.Center;
            row.style.alignItems = Align.Center;
            row.style.marginTop = Px(CountdownStack.RowGapPt, u);
            row.style.flexShrink = 0;

            // Status: an indicator, not a control. Teal outline and dot, cream text, never picked, never blinking.
            pill.style.display = hasStatus ? DisplayStyle.Flex : DisplayStyle.None;
            pill.pickingMode = PickingMode.Ignore;
            pill.style.flexDirection = FlexDirection.Row;
            pill.style.alignItems = Align.Center;
            pill.style.flexShrink = 0;
            pill.style.width = Px(fit.StatusW, u);
            pill.style.height = Px(CountdownStack.StatusH, u);
            pill.style.paddingLeft = Px(CountdownStack.StatusPadXPt, u);
            pill.style.paddingRight = Px(CountdownStack.StatusPadXPt, u);
            float pr = Mathf.Floor(Px(CountdownStack.StatusH, u) * 0.5f);
            pill.style.borderTopLeftRadius = pr; pill.style.borderTopRightRadius = pr;
            pill.style.borderBottomLeftRadius = pr; pill.style.borderBottomRightRadius = pr;
            float ow = Mathf.Max(1f, Px(CountdownStack.StatusOutlinePt, u));
            pill.style.borderTopWidth = ow; pill.style.borderBottomWidth = ow; pill.style.borderLeftWidth = ow; pill.style.borderRightWidth = ow;
            pill.style.borderTopColor = FlightDusk.Teal; pill.style.borderBottomColor = FlightDusk.Teal;
            pill.style.borderLeftColor = FlightDusk.Teal; pill.style.borderRightColor = FlightDusk.Teal;
            pill.style.backgroundColor = new Color(FlightDusk.Teal.r, FlightDusk.Teal.g, FlightDusk.Teal.b, 0.14f);
            pill.style.marginBottom = fit.OneRow || !hasSkip ? 0f : Px(CountdownStack.RowGapPt, u);
            float d = Px(CountdownStack.StatusDotPt, u);
            dot.pickingMode = PickingMode.Ignore;
            dot.style.width = d; dot.style.height = d; dot.style.flexShrink = 0;
            dot.style.marginRight = Px(CountdownStack.StatusDotGapPt, u);
            float dr = Mathf.Floor(d * 0.5f);
            dot.style.borderTopLeftRadius = dr; dot.style.borderTopRightRadius = dr;
            dot.style.borderBottomLeftRadius = dr; dot.style.borderBottomRightRadius = dr;
            dot.style.backgroundColor = FlightDusk.Teal;
            if (callout != null)
            {
                Plain(callout);
                callout.pickingMode = PickingMode.Ignore;
                callout.style.fontSize = Px(CountdownStack.StatusPt, u);
                callout.style.color = FlightDusk.Cream;
                callout.style.whiteSpace = WhiteSpace.NoWrap;
                callout.style.unityTextAlign = TextAnchor.MiddleLeft;
                callout.style.unityFontStyleAndWeight = FontStyle.Bold;
                callout.style.flexGrow = 1; callout.style.flexShrink = 1;
                callout.style.overflow = Overflow.Hidden;
                FlightDusk.Face(callout, FlightDusk.BodyFace());
            }

            // Skip count: secondary. Outline, transparent plate, 44 pt hit area.
            if (skip != null)
            {
                skip.style.width = Px(fit.SkipW > 0f ? fit.SkipW : CountdownStack.SkipWidth(), u);
                skip.style.height = Px(CountdownStack.SkipPt, u);
                skip.style.minHeight = Px(CountdownStack.SkipPt, u);
                skip.style.flexShrink = 0;
                skip.style.marginLeft = 0; skip.style.marginRight = 0; skip.style.marginTop = 0; skip.style.marginBottom = 0;
                skip.style.paddingLeft = 0; skip.style.paddingRight = 0; skip.style.paddingTop = 0; skip.style.paddingBottom = 0;
                skip.style.fontSize = Px(CountdownStack.SkipFontPt, u);
                skip.style.whiteSpace = WhiteSpace.NoWrap;
                skip.style.unityTextAlign = TextAnchor.MiddleCenter;
                skip.style.unityFontStyleAndWeight = FontStyle.Bold;
                skip.style.color = FlightDusk.Cream;
                skip.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                float sw = Mathf.Max(1f, Px(CountdownStack.SkipOutlinePt, u));
                var edge = new Color(FlightDusk.Cream.r, FlightDusk.Cream.g, FlightDusk.Cream.b, 0.72f);
                skip.style.borderTopWidth = sw; skip.style.borderBottomWidth = sw; skip.style.borderLeftWidth = sw; skip.style.borderRightWidth = sw;
                skip.style.borderTopColor = edge; skip.style.borderBottomColor = edge; skip.style.borderLeftColor = edge; skip.style.borderRightColor = edge;
                float sr = Px(10f, u);
                skip.style.borderTopLeftRadius = sr; skip.style.borderTopRightRadius = sr;
                skip.style.borderBottomLeftRadius = sr; skip.style.borderBottomRightRadius = sr;
                FlightDusk.Face(skip, FlightDusk.BodyFace());
            }
            return fit;
        }

        /// <summary>Strip a label of the dusk frame (Banner/Callout) so it sits flat on the card.</summary>
        static void Plain(Label l)
        {
            l.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            l.style.borderTopWidth = 0; l.style.borderBottomWidth = 0; l.style.borderLeftWidth = 0; l.style.borderRightWidth = 0;
            l.style.paddingLeft = 0; l.style.paddingRight = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0;
            l.style.marginLeft = 0; l.style.marginRight = 0; l.style.marginTop = 0; l.style.marginBottom = 0;
            l.style.minHeight = 0;
            l.style.maxWidth = Length.Percent(100);
        }

        static VisualElement EnsureRow(VisualElement cast, Label callout, Button skip, out VisualElement pill, out VisualElement dot)
        {
            var row = cast.Q(RowName);
            if (row == null)
            {
                row = new VisualElement { name = RowName, pickingMode = PickingMode.Ignore };
                // Where the callout was: after the spare line, before the (hidden) event chips.
                int at = callout != null && callout.parent == cast ? cast.IndexOf(callout) : cast.childCount;
                cast.Insert(at, row);
                pill = new VisualElement { name = StatusName, pickingMode = PickingMode.Ignore };
                dot = new VisualElement { name = DotName, pickingMode = PickingMode.Ignore };
                pill.Add(dot);
                if (callout != null) { callout.RemoveFromHierarchy(); pill.Add(callout); }
                row.Add(pill);
                if (skip != null) { skip.RemoveFromHierarchy(); row.Add(skip); }
            }
            pill = row.Q(StatusName);
            dot = row.Q(DotName);
            return row;
        }

        /// <summary>Put the tree and inline styles back as Slice.uxml has them (liftoff: AscentHud adopts the card).</summary>
        public static void Undress(VisualElement cast, Label verdict, Label pulse, Label callout, Button skip)
        {
            if (cast == null) return;
            var row = cast.Q(RowName);
            if (row != null)
            {
                int at = cast.IndexOf(row);
                if (callout != null) { callout.RemoveFromHierarchy(); cast.Insert(at++, callout); }
                row.RemoveFromHierarchy();
                if (skip != null) { skip.RemoveFromHierarchy(); cast.Add(skip); }
            }
            var s = cast.style;
            s.backgroundColor = StyleKeyword.Null; s.flexDirection = StyleKeyword.Null; s.alignItems = StyleKeyword.Null;
            s.paddingLeft = StyleKeyword.Null; s.paddingRight = StyleKeyword.Null; s.paddingTop = StyleKeyword.Null; s.paddingBottom = StyleKeyword.Null;
            s.borderTopLeftRadius = StyleKeyword.Null; s.borderTopRightRadius = StyleKeyword.Null;
            s.borderBottomLeftRadius = StyleKeyword.Null; s.borderBottomRightRadius = StyleKeyword.Null;
            s.borderTopWidth = StyleKeyword.Null; s.borderBottomWidth = StyleKeyword.Null; s.borderLeftWidth = StyleKeyword.Null; s.borderRightWidth = StyleKeyword.Null;
            s.borderBottomColor = StyleKeyword.Null; s.overflow = StyleKeyword.Null;
            foreach (var l in new TextElement[] { verdict, pulse, callout, skip })
            {
                if (l == null) continue;
                var t = l.style;
                t.height = StyleKeyword.Null; t.width = StyleKeyword.Null; t.minHeight = StyleKeyword.Null;
                t.flexGrow = StyleKeyword.Null; t.flexShrink = StyleKeyword.Null;
                t.marginTop = StyleKeyword.Null; t.marginBottom = StyleKeyword.Null; t.marginLeft = StyleKeyword.Null; t.marginRight = StyleKeyword.Null;
                t.borderTopWidth = StyleKeyword.Null; t.borderBottomWidth = StyleKeyword.Null; t.borderLeftWidth = StyleKeyword.Null; t.borderRightWidth = StyleKeyword.Null;
                t.borderTopColor = StyleKeyword.Null; t.borderBottomColor = StyleKeyword.Null; t.borderLeftColor = StyleKeyword.Null; t.borderRightColor = StyleKeyword.Null;
                t.backgroundColor = StyleKeyword.Null; t.color = StyleKeyword.Null;
                t.whiteSpace = StyleKeyword.Null; t.unityTextAlign = StyleKeyword.Null;
            }
            if (callout != null) callout.pickingMode = PickingMode.Position;
        }

        // ---- COUNT tag ---------------------------------------------------------------------------

        /// <summary>The h-stage chip while counting: beside the clock (not at the far right under the narrator
        /// portrait), sized for its text, inside hud-top.</summary>
        public static void DressTag(Label tag, float u)
        {
            if (tag == null) return;
            u = u > 0.5f ? u : 1f;
            var top = tag.parent;
            if (top != null)
            {
                top.style.justifyContent = Justify.FlexStart;
                top.style.alignItems = Align.Center;
                top.style.overflow = Overflow.Hidden;
            }
            tag.style.marginLeft = Px(CountdownStack.TagGapPt, u);
            tag.style.width = Px(CountdownStack.TagWidth(tag.text), u);
            tag.style.height = Px(CountdownStack.TagH, u);
            tag.style.flexShrink = 0; tag.style.flexGrow = 0;
            tag.style.paddingLeft = 0; tag.style.paddingRight = 0; tag.style.paddingTop = 0; tag.style.paddingBottom = 0;
            tag.style.fontSize = Px(CountdownStack.TagPt, u);
            tag.style.unityFontStyleAndWeight = FontStyle.Bold;
            tag.style.whiteSpace = WhiteSpace.NoWrap;
            tag.style.unityTextAlign = TextAnchor.MiddleCenter;
            tag.style.overflow = Overflow.Hidden;
            float r = Px(CountdownStack.TagRadiusPt, u);
            tag.style.borderTopLeftRadius = r; tag.style.borderTopRightRadius = r;
            tag.style.borderBottomLeftRadius = r; tag.style.borderBottomRightRadius = r;
            FlightDusk.Face(tag, FlightDusk.BodyFace());
        }

        public static void UndressTag(Label tag)
        {
            if (tag == null) return;
            var top = tag.parent;
            if (top != null) { top.style.justifyContent = StyleKeyword.Null; top.style.alignItems = StyleKeyword.Null; top.style.overflow = StyleKeyword.Null; }
            var t = tag.style;
            t.marginLeft = StyleKeyword.Null; t.width = StyleKeyword.Null; t.height = StyleKeyword.Null;
            t.flexShrink = StyleKeyword.Null; t.flexGrow = StyleKeyword.Null;
            t.paddingLeft = StyleKeyword.Null; t.paddingRight = StyleKeyword.Null; t.paddingTop = StyleKeyword.Null; t.paddingBottom = StyleKeyword.Null;
            t.fontSize = StyleKeyword.Null; t.unityFontStyleAndWeight = StyleKeyword.Null; t.whiteSpace = StyleKeyword.Null;
            t.unityTextAlign = StyleKeyword.Null; t.overflow = StyleKeyword.Null; t.unityFontDefinition = StyleKeyword.Null;
            t.borderTopLeftRadius = StyleKeyword.Null; t.borderTopRightRadius = StyleKeyword.Null;
            t.borderBottomLeftRadius = StyleKeyword.Null; t.borderBottomRightRadius = StyleKeyword.Null;
        }
    }
}
