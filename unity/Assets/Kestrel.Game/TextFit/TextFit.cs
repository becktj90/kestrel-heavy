// Keeps every label and button string inside the box that clips it.
// Wrap first, then raise the min height when the box can grow, then shrink the face
// down to a floor. A seal remembers the face for the box it was fitted to, so a later
// layout pass that writes the unfitted size can ask Cap and keep the string inside.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class TextFit
    {
        public const float Slack = 3f;
        public const float Floor = 10f;
        const int Passes = 4;

        sealed class Seal
        {
            public float Size;
            public float Width;
            public float Height;
        }

        static readonly ConditionalWeakTable<VisualElement, Seal> Seals = new ConditionalWeakTable<VisualElement, Seal>();

        /// <summary>The face each label had when TextFit last looked at it, and a held grow: the face it grew to
        /// and the content height it had then. The hold lasts until layout moves that box (or <see cref="HoldApplies"/>).</summary>
        sealed class Seen
        {
            public float Face;
            public float HeldFace;      // > 0 while a grow is held
            public float HeldFrom;      // contentRect height when the grow was seen
            public int HeldApplies;     // Apply calls the hold has survived without the box moving
            public int LastApply = -1;
        }

        /// <summary>A box that has not moved through this many Apply calls after its face grew is not following its text
        /// (a fixed or crushed parent), so the normal height fit resumes.</summary>
        const int HoldApplies = 6;

        static readonly ConditionalWeakTable<VisualElement, Seen> Faces = new ConditionalWeakTable<VisualElement, Seen>();
        static int _apply;

        /// <summary>The fitted face when this element was sealed for the same box. Otherwise <paramref name="size"/>.</summary>
        public static float Cap(VisualElement element, float size)
        {
            if (element == null || !(size > 0f)) return size;
            if (!Seals.TryGetValue(element, out var seal) || !(seal.Size > 0f)) return size;
            float w, h;
            Box(element, out w, out h);
            if (w < 8f) return size;
            if (Math.Abs(w - seal.Width) > 6f) return size;
            if (h > seal.Height + 6f) return size;
            return size < seal.Size ? size : seal.Size;
        }

        /// <summary>Drop fitted faces under <paramref name="root"/> so the next layout can use the design size.</summary>
        public static void Release(VisualElement root)
        {
            if (root == null) return;
            if (root is TextElement self) Seals.Remove(self);
            var found = root.Query<TextElement>().ToList();
            if (found == null) return;
            for (int i = 0; i < found.Count; i++) Seals.Remove(found[i]);
        }

        /// <summary>Fit every text element under <paramref name="root"/>. Idempotent when the strings already fit.</summary>
        public static void Apply(VisualElement root)
        {
            if (root == null) return;
            _apply++;
            var list = new List<TextElement>(64);
            Collect(root, list);
            for (int pass = 0; pass < Passes; pass++)
            {
                bool changed = false;
                for (int i = 0; i < list.Count; i++)
                    if (Fit(list[i])) changed = true;
                if (!changed) return;
            }
        }

        /// <summary>Human-readable overflows after layout. Empty when every string sits inside its box.</summary>
        public static List<string> Problems(VisualElement root)
        {
            var problems = new List<string>();
            if (root == null) return problems;
            var list = new List<TextElement>(64);
            Collect(root, list);
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (!Shown(t)) continue;
                string text = t.text;
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (Shadow(t)) continue;
                float w, h;
                Box(t, out w, out h);
                if (w < 8f || h < 4f) continue;
                var need = Measure(t, text, w);
                float slack = Slack + Tracking(t);
                bool widthOver = need.x > w + slack;
                bool heightOver = need.y > h + Slack && !HeightScrolls(t);
                // A border box that sticks out of a visible parent is not cut-off text.
                // Only a clipping parent can hide the glyphs.
                bool spill = (widthOver || heightOver) && Spills(t);
                if (!widthOver && !heightOver) continue;
                string name = string.IsNullOrEmpty(t.name) ? t.GetType().Name : t.name;
                string sample = text.Length > 42 ? text.Substring(0, 42) : text;
                problems.Add(name + " \"" + sample + "\" need " + need.x.ToString("0") + "x" + need.y.ToString("0")
                    + " box " + w.ToString("0") + "x" + h.ToString("0")
                    + (spill ? " spills" : "")
                    + (widthOver ? " wide" : "")
                    + (heightOver ? " tall" : ""));
            }
            return problems;
        }

        static void Collect(VisualElement root, List<TextElement> into)
        {
            if (root is TextElement self) into.Add(self);
            var found = root.Query<TextElement>().ToList();
            if (found == null) return;
            for (int i = 0; i < found.Count; i++)
            {
                if (!ReferenceEquals(found[i], root)) into.Add(found[i]);
            }
        }

        static bool Fit(TextElement t)
        {
            if (!Shown(t) || Shadow(t)) return false;
            string text = t.text;
            if (string.IsNullOrWhiteSpace(text)) return false;
            float w, h;
            Box(t, out w, out h);
            if (w < 8f || h < 4f) return false;
            var one = Measure(t, text, 0f);
            // Before any early exit, so the face history sees every pass.
            bool grew = HoldGrow(t, h, one.y);
            var wrapped = Measure(t, text, w);
            // Trailing letter-spacing is in the measure and not always in the layout width.
            float slack = Slack + Tracking(t);
            bool widthOk = wrapped.x <= w + slack && (one.x <= w + slack || wrapped.y <= h + Slack || !HeightLocked(t));
            bool heightOk = wrapped.y <= h + Slack || HeightScrolls(t);
            bool spill = !(one.x <= w + slack && one.y <= h + Slack) && Spills(t);
            // A string that is within Slack of the box on one line still wraps when the layout gives it
            // the box width, and the second line is what the clip hides. Problems() measures wrapped, so
            // the early "it fits" exit must too, or a 129 pt string in a 128 pt, one-line box is never fitted.
            bool nowrapFace = t.resolvedStyle != null && t.resolvedStyle.whiteSpace == WhiteSpace.NoWrap;
            bool wrapsTall = !nowrapFace && wrapped.y > h + Slack && !HeightScrolls(t);
            if (one.x <= w + slack && one.y <= h + Slack && !spill && !wrapsTall)
            {
                ReleaseIfRoomy(t, w, h);
                return false;
            }
            if (widthOk && heightOk && !spill)
            {
                ReleaseIfRoomy(t, w, h);
                return false;
            }

            bool changed = false;
            bool nowrap = t.resolvedStyle != null && t.resolvedStyle.whiteSpace == WhiteSpace.NoWrap;
            if (!nowrap && t.style.whiteSpace.value != WhiteSpace.Normal)
            {
                t.style.whiteSpace = WhiteSpace.Normal;
                changed = true;
            }
            if (t.resolvedStyle != null && t.resolvedStyle.textOverflow == TextOverflow.Ellipsis)
            {
                t.style.textOverflow = TextOverflow.Clip;
                changed = true;
            }

            float parentW = ParentContentWidth(t);
            bool fitsParent = parentW > 8f && one.x <= parentW + slack;
            if (fitsParent && PixelLength(t.style.maxWidth) > 0.5f && PixelLength(t.style.maxWidth) < parentW - 1f)
            {
                t.style.maxWidth = StyleKeyword.Null;
                changed = true;
            }
            else if (!fitsParent && parentW > 8f && (t.worldBound.width > parentW + slack || one.x > w + slack))
            {
                var max = t.style.maxWidth;
                if (max.keyword == StyleKeyword.Undefined || max.value.value > parentW + 0.5f)
                {
                    t.style.maxWidth = parentW;
                    changed = true;
                }
            }

            wrapped = Measure(t, text, Math.Max(8f, parentW > 8f ? Math.Min(w, parentW) : w));
            bool stillWide = wrapped.x > w + slack;
            bool stillTall = wrapped.y > h + Slack && !HeightScrolls(t);
            if (stillTall && CanGrow(t))
            {
                float need = wrapped.y + 2f;
                bool asked = t.style.minHeight.value.value >= need - 0.5f;
                if (!asked)
                {
                    t.style.minHeight = need;
                    changed = true;
                }
                // The box is still the old height. A stylesheet height, or a pass that already
                // asked for more room, means growing will not uncover the glyphs.
                if (asked && h + 4f < need) stillTall = true;
                else stillTall = false;
            }

            // A line that already fits the parent should grow the box, not collapse to the floor
            // while the layout is still the previous face.
            if (!stillWide && fitsParent && CanGrow(t) && one.y > h + Slack)
            {
                float line = one.y + 2f;
                if (t.style.minHeight.value.value < line - 0.5f)
                {
                    t.style.minHeight = line;
                    changed = true;
                }
                stillTall = false;
            }

            // A larger face written since the last layout is measured against the box of the old face. A label
            // whose height follows its own text (no pixel height, no clip cutting it) will grow on the next
            // layout, so its height is not a reason to shrink. Without this, a wordmark restored from 12 pt to
            // 70 pt in the same geometry callback was measured against its 12 pt box and cut straight back to
            // h / lineHeight = 12.05 pt, whatever face the caller wrote (#291 ShortColumnReleasesCaps).
            if (grew && (stillTall || spill))
            {
                TraceFit(t, "grow held, no shrink", Face(t), Face(t), w, h);
                stillTall = false;
                spill = false;
            }

            // A pixel height or a larger face written this pass has not laid out yet.
            if ((stillWide || stillTall || spill) && !AwaitingGrow(t) && !FacePending(t))
            {
                float font = Face(t);
                float limit = stillWide ? w / Math.Max(1f, wrapped.x) : h / Math.Max(1f, wrapped.y);
                if (spill && !stillWide && !stillTall) limit = 0.92f;
                float next = Math.Max(Floor, font * Math.Min(0.98f, limit));
                if (next > font - 0.4f) next = Math.Max(Floor, font - 1f);
                if (next < font - 0.3f)
                {
                    TraceFit(t, stillWide ? "shrink wide" : stillTall ? "shrink tall" : "shrink spill", font, next, w, h);
                    t.style.fontSize = next;
                    Remember(t, next, w, h);
                    if (Faces.TryGetValue(t, out var seen)) seen.Face = next;
                    changed = true;
                }
            }
            return changed;
        }

        /// <summary>
        /// Sticky grow hold. When a label's face grows (a caller wrote a larger size), its box is still the content box
        /// of the old face until UI Toolkit lays it out again, and measuring the new face against it shrinks the label
        /// straight back to boxHeight / lineHeight. The hold starts when the face is seen to grow and the label's height
        /// is its own (no pixel height on it, no clipping ancestor cutting its box). It is held across every Apply
        /// (the root and the full-host Apply of one callback, and later callbacks) until one of:
        /// the content height moves off the height it had at the grow (layout ran), the content box is tall enough for
        /// one line of the face, a smaller face is written, or <see cref="HoldApplies"/> Apply calls pass with the box unmoved.
        /// </summary>
        static bool HoldGrow(TextElement t, float h, float oneLineH)
        {
            float face = Face(t);
            float own = OwnHeight(t);
            if (!Faces.TryGetValue(t, out var seen))
            {
                Faces.Add(t, new Seen { Face = face, LastApply = _apply });
                return false;
            }
            bool newApply = seen.LastApply != _apply;
            seen.LastApply = _apply;
            if (face > seen.Face + 0.5f && !HeightLocked(t) && !(own > h + 0.5f))
            {
                seen.HeldFace = face;
                seen.HeldFrom = own;
                seen.HeldApplies = 0;
            }
            seen.Face = face;
            if (!(seen.HeldFace > 0f)) return false;

            string release = null;
            if (face < seen.HeldFace - 0.5f) release = "smaller face written";
            else if (HeightLocked(t)) release = "pixel height";
            else if (Math.Abs(own - seen.HeldFrom) > 0.5f) release = "box moved " + seen.HeldFrom.ToString("0.#") + " -> " + own.ToString("0.#");
            else if (own + Slack >= oneLineH) release = "box tall enough";
            else if (own > h + 0.5f) release = "clip cuts the box";
            else
            {
                if (newApply) seen.HeldApplies++;
                if (seen.HeldApplies > HoldApplies) release = "box never moved";
            }
            if (release != null)
            {
                TraceFit(t, "grow hold released (" + release + ")", face, face, 0f, h);
                seen.HeldFace = 0f;
                return false;
            }
            return true;
        }

        static float OwnHeight(VisualElement t)
        {
            float own = t.contentRect.height;
            if (float.IsNaN(own) || own < 1f) own = t.layout.height;
            return own;
        }

        /// <summary>Hold state for traces: "held 2 from 15 content 15", or "-".</summary>
        public static string HoldInfo(VisualElement t)
        {
            if (t == null) return "-";
            string content = " content " + OwnHeight(t).ToString("0.#");
            if (!Faces.TryGetValue(t, out var seen) || !(seen.HeldFace > 0f)) return "no hold" + content;
            return "held " + seen.HeldApplies + " at " + seen.HeldFace.ToString("0.###") + " from " + seen.HeldFrom.ToString("0.#") + content;
        }

        /// <summary>Names whose face writes are logged in the editor and development builds when
        /// <see cref="TraceOn"/> holds. CI (batch mode) logs the Home wordmark so a failing run shows who wrote it.</summary>
        public static readonly HashSet<string> TraceNames = new HashSet<string> { "mm-kestrel" };
        public static bool Trace;
        public static bool TraceOn => Trace || Application.isBatchMode;

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        static void TraceFit(TextElement t, string why, float from, float to, float w, float h)
        {
            if (t == null || string.IsNullOrEmpty(t.name) || !TraceNames.Contains(t.name) || !TraceOn) return;
            Debug.Log("[kh-textfit] " + t.name + " " + why + " " + from.ToString("0.###") + " -> " + to.ToString("0.###")
                + " box " + w.ToString("0.#") + "x" + h.ToString("0.#")
                + " layout " + t.layout.height.ToString("0.#") + " resolved " + t.resolvedStyle.fontSize.ToString("0.###")
                + " apply " + _apply + " " + HoldInfo(t));
        }

        static void ReleaseIfRoomy(TextElement t, float w, float h)
        {
            if (!Seals.TryGetValue(t, out var seal)) return;
            if (h > seal.Height + 6f || Math.Abs(w - seal.Width) > 6f)
                Seals.Remove(t);
        }

        static void Remember(TextElement t, float size, float w, float h)
        {
            if (Seals.TryGetValue(t, out var seal))
            {
                seal.Size = size;
                seal.Width = w;
                seal.Height = h;
                return;
            }
            Seals.Add(t, new Seal { Size = size, Width = w, Height = h });
        }

        static bool Shown(VisualElement e)
        {
            for (var a = e; a != null; a = a.parent)
            {
                if (a.resolvedStyle == null) continue;
                if (a.resolvedStyle.display == DisplayStyle.None) return false;
                if (a.resolvedStyle.opacity <= 0.05f) return false;
            }
            return e != null && e.enabledInHierarchy;
        }

        static bool Shadow(VisualElement e)
        {
            return e != null && e.name != null && e.name.EndsWith("-shadow", StringComparison.Ordinal);
        }

        static void Box(VisualElement t, out float w, out float h)
        {
            w = t.contentRect.width;
            h = t.contentRect.height;
            if (float.IsNaN(w) || w < 1f) w = t.layout.width;
            if (float.IsNaN(h) || h < 1f) h = t.layout.height;
            float parentW = ParentContentWidth(t);
            if (parentW > 8f && parentW + Slack < w) w = parentW;
            var wb = t.worldBound;
            for (var a = t.parent; a != null; a = a.parent)
            {
                if (!Clips(a)) continue;
                if (a is ScrollView) continue;
                var ab = a.worldBound;
                float visW = Math.Min(wb.xMax, ab.xMax) - Math.Max(wb.xMin, ab.xMin);
                float visH = Math.Min(wb.yMax, ab.yMax) - Math.Max(wb.yMin, ab.yMin);
                if (visW > 1f && visW < w) w = visW;
                if (visH > 1f && visH < h) h = visH;
            }
        }

        static float ParentContentWidth(VisualElement t)
        {
            var p = t != null ? t.parent : null;
            if (p == null) return 0f;
            float w = p.contentRect.width;
            if (float.IsNaN(w) || w < 1f) w = p.layout.width;
            return w;
        }

        static Vector2 Measure(TextElement t, string text, float width)
        {
            if (width > 1f)
                return t.MeasureTextSize(text, width, VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined);
            return t.MeasureTextSize(text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
        }

        static float Face(VisualElement t)
        {
            if (t.resolvedStyle != null && t.resolvedStyle.fontSize > 0.5f && !float.IsNaN(t.resolvedStyle.fontSize))
                return t.resolvedStyle.fontSize;
            var set = t.style.fontSize;
            if (set.keyword == StyleKeyword.Undefined) return 14f;
            return set.value.value > 0.5f ? set.value.value : 14f;
        }

        static bool HeightLocked(VisualElement t)
        {
            if (t == null) return false;
            return HasLength(t.style.height) || HasLength(t.style.maxHeight);
        }

        /// <summary>An inline pixel length. Undefined with a positive value is how a pixel assignment is stored.
        /// A percent height fills its parent and is not a cap on the text.</summary>
        static bool HasLength(StyleLength length)
        {
            if (length.keyword == StyleKeyword.Auto || length.keyword == StyleKeyword.Null || length.keyword == StyleKeyword.None)
                return false;
            if (length.value.unit == LengthUnit.Percent) return false;
            return length.value.value > 0.5f;
        }

        static bool CanGrow(VisualElement t)
        {
            if (BlocksGrow(t)) return false;
            for (var a = t.parent; a != null; a = a.parent)
            {
                if (a is ScrollView sv && sv.mode != ScrollViewMode.Horizontal) return true;
                if (BlocksGrow(a)) return false;
            }
            return true;
        }

        /// <summary>A locked height that is already on screen. A taller pixel height that has not laid out yet does not block.</summary>
        static bool BlocksGrow(VisualElement e) => HeightLocked(e) && !PendingGrow(e);

        static bool PendingGrow(VisualElement e)
        {
            if (e == null) return false;
            float laid = e.layout.height;
            if (float.IsNaN(laid) || laid < 1f) return false;
            // Only an explicit pixel height that has not been applied yet. A min-height request
            // the layout ignored must not block the shrink that uncovers the glyphs.
            float h = PixelLength(e.style.height);
            if (h <= laid + 4f) return false;
            float max = PixelLength(e.style.maxHeight);
            if (max > 0.5f && max <= laid + 4f) return false;
            return true;
        }

        static bool FacePending(TextElement t)
        {
            float resolved = t.resolvedStyle != null ? t.resolvedStyle.fontSize : 0f;
            if (float.IsNaN(resolved) || resolved < 0.5f) return false;
            float set = PixelLength(t.style.fontSize);
            return set > resolved + 0.5f;
        }

        static float Tracking(VisualElement t)
        {
            if (t == null || t.style == null) return 0f;
            var ls = t.style.letterSpacing;
            if (ls.keyword == StyleKeyword.Auto || ls.keyword == StyleKeyword.Null || ls.keyword == StyleKeyword.None
                || ls.keyword == StyleKeyword.Initial)
                return 0f;
            float v = ls.value.value;
            return v > 0.5f ? v : 0f;
        }

        static bool AwaitingGrow(VisualElement t)
        {
            for (var a = t; a != null; a = a.parent)
                if (PendingGrow(a)) return true;
            return false;
        }

        static float PixelLength(StyleLength length)
        {
            if (length.keyword == StyleKeyword.Auto || length.keyword == StyleKeyword.Null || length.keyword == StyleKeyword.None)
                return 0f;
            if (length.value.unit == LengthUnit.Percent) return 0f;
            return length.value.value > 0.5f ? length.value.value : 0f;
        }

        static bool HeightScrolls(TextElement t)
        {
            for (var a = t.parent; a != null; a = a.parent)
            {
                if (a is ScrollView sv && sv.mode != ScrollViewMode.Horizontal) return true;
                if (Clips(a)) return false;
            }
            return false;
        }

        static bool Clips(VisualElement a)
        {
            if (a == null || a.style == null) return false;
            var overflow = a.style.overflow;
            if (overflow.keyword == StyleKeyword.Undefined || overflow.keyword == StyleKeyword.Null
                || overflow.keyword == StyleKeyword.None || overflow.keyword == StyleKeyword.Initial)
                return false;
            return overflow.value == Overflow.Hidden;
        }

        static bool Spills(VisualElement t)
        {
            var p = t.parent;
            if (p == null) return false;
            var a = t.worldBound;
            var b = p.worldBound;
            if (a.width < 2f || b.width < 2f) return false;
            return a.xMax > b.xMax + Slack || a.yMax > b.yMax + Slack || a.xMin < b.xMin - Slack || a.yMin < b.yMin - Slack;
        }
    }
}
