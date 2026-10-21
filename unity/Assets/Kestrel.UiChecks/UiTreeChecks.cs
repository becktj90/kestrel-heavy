// Real-tree UI checks for device and capture tests (EditMode and PlayMode). Point them at a laid-out UI Toolkit tree
// (the real USS, a panel at the device size, after layout) and they return human-readable problems; the caller asserts
// the list is empty, so one run reports every fault at every size. NUnit-free, so both test assemblies can share it.
//
//   TextBounds: every visible label's glyph block, measured with MeasureTextSize at its resolved font and width and placed
//               by its text alignment, must sit inside its own content rect and inside its parent's padding box.
//   Overlaps:   no two visible leaves (text with words, buttons) draw on each other unless one contains the other.
//   Outside:    no visible leaf leaves an area (the screen, the safe area).
// Skipped on purpose: "-shadow" twins (they draw under their label), Unity's own scroller parts, elements with the
// kh-hit-overlay class (a transparent hit target that covers its card), and anything a scroll view has scrolled away.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.UiChecks
{
    public static class UiTreeChecks
    {
        public const float Slack = 1.5f;
        public const string HitOverlayClass = "kh-hit-overlay";

        /// <summary>Shown on screen: displayed, visible and not transparent all the way up, with a real size.</summary>
        public static bool Visible(VisualElement e)
        {
            if (e == null || e.panel == null) return false;
            var b = e.worldBound;
            if (float.IsNaN(b.width) || b.width < 0.5f || b.height < 0.5f) return false;
            for (var p = e; p != null; p = p.parent)
            {
                var r = p.resolvedStyle;
                if (r.display == DisplayStyle.None) return false;
                if (r.visibility == Visibility.Hidden) return false;
                if (r.opacity < 0.02f) return false;
            }
            return true;
        }

        static float Scale(VisualElement e)
        {
            float lw = e.layout.width;
            float ww = e.worldBound.width;
            return lw > 0.5f && ww > 0.5f ? ww / lw : 1f;
        }

        /// <summary>The content rect in panel (world) coordinates.</summary>
        public static Rect ContentWorld(VisualElement e)
        {
            var wb = e.worldBound;
            var cr = e.contentRect;
            float k = Scale(e);
            return new Rect(wb.x + cr.x * k, wb.y + cr.y * k, cr.width * k, cr.height * k);
        }

        /// <summary>Inside the border: what the element paints its children into.</summary>
        public static Rect PaddingWorld(VisualElement e)
        {
            var wb = e.worldBound;
            var r = e.resolvedStyle;
            float k = Scale(e);
            float l = Num(r.borderLeftWidth) * k, t = Num(r.borderTopWidth) * k;
            float rr = Num(r.borderRightWidth) * k, bb = Num(r.borderBottomWidth) * k;
            return new Rect(wb.x + l, wb.y + t, Mathf.Max(0f, wb.width - l - rr), Mathf.Max(0f, wb.height - t - bb));
        }

        static float Num(float v) => float.IsNaN(v) ? 0f : v;

        static bool Shadow(VisualElement e) => e.name != null && e.name.EndsWith("-shadow", StringComparison.Ordinal);

        static bool UnderScroller(VisualElement e)
        {
            for (var p = e; p != null; p = p.parent)
                if (p is Scroller) return true;
            return false;
        }

        static bool HitOverlay(VisualElement e) => e.ClassListContains(HitOverlayClass);

        /// <summary>The glyph block of <paramref name="t"/>'s text in world coordinates: the measured size at the resolved
        /// face (one line for nowrap, wrapped at the content width otherwise), placed by -unity-text-align.</summary>
        public static Rect TextRect(TextElement t)
        {
            var cr = t.contentRect;
            bool nowrap = t.resolvedStyle.whiteSpace == WhiteSpace.NoWrap;
            var need = nowrap
                ? t.MeasureTextSize(t.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined)
                : t.MeasureTextSize(t.text, Mathf.Max(1f, cr.width), VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined);
            float w = Num(need.x), h = Num(need.y);
            Fractions(t.resolvedStyle.unityTextAlign, out float fx, out float fy);
            float x = cr.x + (cr.width - w) * fx;
            float y = cr.y + (cr.height - h) * fy;
            var wb = t.worldBound;
            float k = Scale(t);
            return new Rect(wb.x + x * k, wb.y + y * k, w * k, h * k);
        }

        static void Fractions(TextAnchor a, out float fx, out float fy)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: fx = 0f; fy = 0f; break;
                case TextAnchor.UpperCenter: fx = 0.5f; fy = 0f; break;
                case TextAnchor.UpperRight: fx = 1f; fy = 0f; break;
                case TextAnchor.MiddleLeft: fx = 0f; fy = 0.5f; break;
                case TextAnchor.MiddleCenter: fx = 0.5f; fy = 0.5f; break;
                case TextAnchor.MiddleRight: fx = 1f; fy = 0.5f; break;
                case TextAnchor.LowerLeft: fx = 0f; fy = 1f; break;
                case TextAnchor.LowerCenter: fx = 0.5f; fy = 1f; break;
                default: fx = 1f; fy = 1f; break;
            }
        }

        /// <summary>What is really drawn of <paramref name="r"/>: clipped by every overflow-hidden ancestor of <paramref name="e"/>.</summary>
        public static Rect Clipped(VisualElement e, Rect r)
        {
            for (var p = e.parent; p != null; p = p.parent)
            {
                if (p.resolvedStyle.overflow != Overflow.Hidden) continue;
                var c = PaddingWorld(p);
                float x0 = Mathf.Max(r.xMin, c.xMin), y0 = Mathf.Max(r.yMin, c.yMin);
                float x1 = Mathf.Min(r.xMax, c.xMax), y1 = Mathf.Min(r.yMax, c.yMax);
                if (x1 <= x0 || y1 <= y0) return Rect.zero;
                r = Rect.MinMaxRect(x0, y0, x1, y1);
            }
            return r;
        }

        public static List<TextElement> VisibleText(VisualElement root)
        {
            var list = new List<TextElement>();
            if (root == null) return list;
            Walk(root, e =>
            {
                if (e is TextElement t && !string.IsNullOrWhiteSpace(t.text) && !Shadow(t) && !UnderScroller(t) && Visible(t))
                    list.Add(t);
            });
            return list;
        }

        /// <summary>Visible leaves: words, and buttons (hit-only overlays excluded).</summary>
        public static List<VisualElement> Leaves(VisualElement root)
        {
            var list = new List<VisualElement>();
            if (root == null) return list;
            Walk(root, e =>
            {
                if (UnderScroller(e) || Shadow(e) || HitOverlay(e) || !Visible(e)) return;
                bool words = e is TextElement t && !string.IsNullOrWhiteSpace(t.text);
                if (!words && !(e is Button)) return;
                if (Drawn(e).width < 0.5f) return;   // scrolled away
                list.Add(e);
            });
            return list;
        }

        /// <summary>The rect a leaf draws: a label's glyph block (a stretched label box is not ink), a button's box.
        /// Clipped by its overflow-hidden ancestors.</summary>
        public static Rect Drawn(VisualElement e)
        {
            Rect r = e is TextElement t && !(e is Button) && !string.IsNullOrWhiteSpace(t.text) ? TextRect(t) : e.worldBound;
            return Clipped(e, r);
        }

        static void Walk(VisualElement e, Action<VisualElement> visit)
        {
            visit(e);
            int n = e.hierarchy.childCount;
            for (int i = 0; i < n; i++) Walk(e.hierarchy[i], visit);
        }

        /// <summary>Text past its own content rect, or past its parent's padding box.</summary>
        public static List<string> TextBounds(VisualElement root)
        {
            var problems = new List<string>();
            foreach (var t in VisibleText(root))
            {
                if (Clipped(t, t.worldBound).width < 0.5f) continue;   // scrolled out of its viewport
                var text = TextRect(t);
                float slack = Slack + Mathf.Max(0f, Num(t.resolvedStyle.letterSpacing));
                var own = ContentWorld(t);
                string over = Over(text, own, slack);
                if (over != null) problems.Add(Describe(t) + " text " + Fmt(text) + " past its content " + Fmt(own) + " (" + over + ")");
                var parent = t.hierarchy.parent;
                if (parent == null || parent.worldBound.width < 0.5f) continue;
                var box = PaddingWorld(parent);
                over = Over(text, box, slack);
                if (over != null) problems.Add(Describe(t) + " text " + Fmt(text) + " past its parent " + Describe(parent) + " " + Fmt(box) + " (" + over + ")");
            }
            return problems;
        }

        /// <summary>Two visible leaves drawing on each other (neither contains the other).</summary>
        public static List<string> Overlaps(VisualElement root)
        {
            var problems = new List<string>();
            var leaves = Leaves(root);
            var drawn = new Rect[leaves.Count];
            for (int i = 0; i < leaves.Count; i++) drawn[i] = Drawn(leaves[i]);
            for (int a = 0; a < leaves.Count; a++)
                for (int b = a + 1; b < leaves.Count; b++)
                {
                    if (Contains(leaves[a], leaves[b]) || Contains(leaves[b], leaves[a])) continue;
                    if (!Hit(drawn[a], drawn[b], 0.5f)) continue;
                    problems.Add(Describe(leaves[a]) + " " + Fmt(drawn[a]) + " on " + Describe(leaves[b]) + " " + Fmt(drawn[b]));
                }
            return problems;
        }

        /// <summary>Visible leaves that leave <paramref name="area"/> (the screen, or the safe area).</summary>
        public static List<string> Outside(VisualElement root, Rect area, string areaName, Func<VisualElement, bool> exempt = null)
        {
            var problems = new List<string>();
            foreach (var e in Leaves(root))
            {
                if (exempt != null && exempt(e)) continue;
                var r = Drawn(e);
                string over = Over(r, area, Slack);
                if (over != null) problems.Add(Describe(e) + " " + Fmt(r) + " leaves the " + areaName + " " + Fmt(area) + " (" + over + ")");
            }
            return problems;
        }

        /// <summary>All three checks; <paramref name="safe"/> may be the screen when there is no inset.</summary>
        public static List<string> Problems(VisualElement root, Rect screen, Rect safe, string where, Func<VisualElement, bool> safeExempt = null)
        {
            var all = new List<string>();
            foreach (var p in TextBounds(root)) all.Add(where + ": " + p);
            foreach (var p in Overlaps(root)) all.Add(where + ": overlap " + p);
            foreach (var p in Outside(root, screen, "screen")) all.Add(where + ": " + p);
            foreach (var p in Outside(root, safe, "safe area", safeExempt)) all.Add(where + ": " + p);
            return all;
        }

        public static string Report(List<string> problems, int max = 80)
        {
            var sb = new StringBuilder();
            sb.Append(problems.Count).Append(" UI problem(s):");
            for (int i = 0; i < problems.Count && i < max; i++) sb.Append('\n').Append("  ").Append(problems[i]);
            if (problems.Count > max) sb.Append("\n  ...");
            return sb.ToString();
        }

        static bool Contains(VisualElement outer, VisualElement inner)
        {
            for (var p = inner.hierarchy.parent; p != null; p = p.hierarchy.parent)
                if (ReferenceEquals(p, outer)) return true;
            return false;
        }

        static bool Hit(Rect a, Rect b, float slack) =>
            a.xMax > b.xMin + slack && b.xMax > a.xMin + slack && a.yMax > b.yMin + slack && b.yMax > a.yMin + slack;

        static string Over(Rect inner, Rect outer, float slack)
        {
            var sb = new StringBuilder();
            if (inner.xMin < outer.xMin - slack) sb.Append("left ").Append((outer.xMin - inner.xMin).ToString("0.#")).Append(' ');
            if (inner.xMax > outer.xMax + slack) sb.Append("right ").Append((inner.xMax - outer.xMax).ToString("0.#")).Append(' ');
            if (inner.yMin < outer.yMin - slack) sb.Append("top ").Append((outer.yMin - inner.yMin).ToString("0.#")).Append(' ');
            if (inner.yMax > outer.yMax + slack) sb.Append("bottom ").Append((inner.yMax - outer.yMax).ToString("0.#")).Append(' ');
            return sb.Length == 0 ? null : sb.ToString().TrimEnd();
        }

        public static string Describe(VisualElement e)
        {
            if (e == null) return "null";
            string id = string.IsNullOrEmpty(e.name) ? e.GetType().Name : e.name;
            if (e is TextElement t && !string.IsNullOrEmpty(t.text))
                id += " \u201C" + (t.text.Length > 32 ? t.text.Substring(0, 32) + "\u2026" : t.text) + "\u201D";
            return id;
        }

        static string Fmt(Rect r) => "(" + r.x.ToString("0.#") + "," + r.y.ToString("0.#") + " " + r.width.ToString("0.#") + "x" + r.height.ToString("0.#") + ")";
    }
}
