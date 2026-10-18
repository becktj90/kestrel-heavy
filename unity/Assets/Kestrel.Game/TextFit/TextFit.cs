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

        /// <summary>Fit every text element under <paramref name="root"/>. Idempotent when the strings already fit.</summary>
        public static void Apply(VisualElement root)
        {
            if (root == null) return;
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
                bool widthOver = need.x > w + Slack;
                bool heightOver = need.y > h + Slack && !HeightScrolls(t);
                bool spill = Spills(t);
                if (!widthOver && !heightOver && !spill) continue;
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
            var wrapped = Measure(t, text, w);
            bool widthOk = wrapped.x <= w + Slack && (one.x <= w + Slack || wrapped.y <= h + Slack || !HeightLocked(t));
            bool heightOk = wrapped.y <= h + Slack || HeightScrolls(t);
            bool spill = Spills(t);
            if (one.x <= w + Slack && one.y <= h + Slack && !spill)
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
            if (t.resolvedStyle != null && t.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
            {
                t.style.whiteSpace = WhiteSpace.Normal;
                changed = true;
            }
            else if (t.style.whiteSpace.value != WhiteSpace.Normal)
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
            if (parentW > 8f && (t.worldBound.width > parentW + Slack || one.x > w + Slack))
            {
                var max = t.style.maxWidth;
                if (max.keyword == StyleKeyword.Undefined || max.value.value > parentW + 0.5f)
                {
                    t.style.maxWidth = parentW;
                    changed = true;
                }
            }

            wrapped = Measure(t, text, Math.Max(8f, parentW > 8f ? Math.Min(w, parentW) : w));
            bool stillWide = wrapped.x > w + Slack;
            bool stillTall = wrapped.y > h + Slack && !HeightScrolls(t);
            if (stillTall && !HeightLocked(t))
            {
                float need = wrapped.y + 2f;
                if (t.style.minHeight.keyword == StyleKeyword.Undefined || t.style.minHeight.value.value < need - 0.5f)
                {
                    t.style.minHeight = need;
                    changed = true;
                }
                if (t.parent != null && t.parent.style.minHeight.keyword != StyleKeyword.Undefined
                    && t.parent.style.minHeight.value.value < need - 0.5f
                    && !HeightLocked(t.parent))
                {
                    t.parent.style.minHeight = need;
                    changed = true;
                }
                stillTall = false;
            }

            if (stillWide || stillTall || spill)
            {
                float font = Face(t);
                float limit = stillWide ? w / Math.Max(1f, wrapped.x) : h / Math.Max(1f, wrapped.y);
                if (spill && !stillWide && !stillTall) limit = 0.92f;
                float next = Math.Max(Floor, font * Math.Min(0.98f, limit));
                if (next > font - 0.4f) next = Math.Max(Floor, font - 1f);
                if (next < font - 0.3f)
                {
                    t.style.fontSize = next;
                    Remember(t, next, w, h);
                    changed = true;
                }
            }
            return changed;
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
            var h = t.style.height;
            if (h.keyword == StyleKeyword.Auto || h.keyword == StyleKeyword.Null || h.keyword == StyleKeyword.None)
                return false;
            if (h.keyword == StyleKeyword.Undefined) return false;
            var max = t.style.maxHeight;
            if (max.keyword != StyleKeyword.Undefined && max.keyword != StyleKeyword.Null && max.keyword != StyleKeyword.Auto
                && max.keyword != StyleKeyword.None && max.value.value > 0f)
                return true;
            return h.value.value > 0f;
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
