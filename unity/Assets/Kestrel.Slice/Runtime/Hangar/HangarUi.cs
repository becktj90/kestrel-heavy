// Small UI Toolkit builders for the Hangar panel. Sizes are in iOS points and converted with the live
// units-per-point (the panel scales with screen width, so a fixed USS px size is 3x bigger on SE than on iPad).
// Every tap target is at least 44 pt. Steppers repeat while held and fold the repeats into one undo step.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class HangarUi
    {
        public const float TargetPt = 44f, BodyPt = 16f, SmallPt = 13f, TitlePt = 18f, RadiusPt = 10f, GapPt = 6f;
        public const long RepeatDelayMs = 400, RepeatEveryMs = 90;

        public float Upp = 3f;
        public float U(float pt) => pt * Upp;

        public static readonly Color Panel = new Color(0.03f, 0.05f, 0.08f, 0.92f);
        public static readonly Color Card = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color ChipOff = new Color(1f, 1f, 1f, 0.10f);
        public static readonly Color ChipOn = new Color(0.31f, 0.86f, 1f, 1f);
        public static readonly Color TextColor = new Color(0.94f, 0.96f, 1f);
        public static readonly Color Dim = new Color(0.70f, 0.76f, 0.86f);
        public static readonly Color Amber = new Color(1f, 0.69f, 0f);
        public static readonly Color Red = new Color(0.92f, 0.27f, 0.27f);
        public static readonly Color Green = new Color(0.45f, 0.86f, 0.55f);
        public static readonly Color Ink = new Color(0.06f, 0.08f, 0.12f);

        public void Radius(IStyle s, float pt)
        {
            float r = U(pt);
            s.borderTopLeftRadius = r; s.borderTopRightRadius = r; s.borderBottomLeftRadius = r; s.borderBottomRightRadius = r;
        }

        public void Pad(IStyle s, float v, float h)
        {
            s.paddingTop = U(v); s.paddingBottom = U(v); s.paddingLeft = U(h); s.paddingRight = U(h);
        }

        public void Margin(IStyle s, float v, float h)
        {
            s.marginTop = U(v); s.marginBottom = U(v); s.marginLeft = U(h); s.marginRight = U(h);
        }

        public Label Text(string text, float pt = BodyPt, Color? color = null, bool bold = false)
        {
            var l = new Label(text);
            l.style.fontSize = U(pt);
            l.style.color = color ?? TextColor;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = 0; l.style.marginBottom = 0;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        public VisualElement Row(float gapPt = GapPt)
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row;
            r.style.alignItems = Align.Center;
            r.style.marginTop = U(gapPt * 0.5f); r.style.marginBottom = U(gapPt * 0.5f);
            return r;
        }

        public VisualElement Column()
        {
            var c = new VisualElement();
            c.style.flexDirection = FlexDirection.Column;
            return c;
        }

        public VisualElement CardBox()
        {
            var c = Column();
            c.style.backgroundColor = Card;
            Radius(c.style, 12f);
            Pad(c.style, 8f, 10f);
            c.style.marginTop = U(4f); c.style.marginBottom = U(4f);
            return c;
        }

        public Label Section(string text)
        {
            var l = Text(text.ToUpperInvariant(), SmallPt, Dim, bold: true);
            l.style.marginTop = U(10f); l.style.marginBottom = U(2f);
            return l;
        }

        /// <summary>A 44 pt button. The tooltip doubles as the accessibility label (project convention).</summary>
        public Button Btn(string text, Action onClick, string tooltip = null, Color? bg = null, Color? fg = null, float minWidthPt = TargetPt)
        {
            var b = new Button(onClick) { text = text };
            b.tooltip = tooltip ?? text;
            b.style.minHeight = U(TargetPt); b.style.height = U(TargetPt);
            b.style.minWidth = U(minWidthPt);
            b.style.fontSize = U(BodyPt);
            b.style.color = fg ?? TextColor;
            b.style.backgroundColor = bg ?? ChipOff;
            b.style.borderLeftWidth = 0; b.style.borderRightWidth = 0; b.style.borderTopWidth = 0; b.style.borderBottomWidth = 0;
            Radius(b.style, RadiusPt);
            Pad(b.style, 0f, 12f);
            b.style.marginLeft = U(3f); b.style.marginRight = U(3f); b.style.marginTop = U(2f); b.style.marginBottom = U(2f);
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            return b;
        }

        public void SetChip(Button b, bool on, bool locked = false)
        {
            b.style.backgroundColor = on ? ChipOn : ChipOff;
            b.style.color = on ? Ink : locked ? Dim : TextColor;
            b.style.opacity = locked ? 0.55f : 1f;
        }

        public struct Choice
        {
            public string id, text, tooltip;
            public bool locked;
            public Choice(string id, string text, string tooltip = null, bool locked = false) { this.id = id; this.text = text; this.tooltip = tooltip; this.locked = locked; }
        }

        /// <summary>Label over a horizontal strip of chips. Locked chips stay visible (the rule is public) but do nothing.</summary>
        public VisualElement Chips(string label, IList<Choice> choices, Func<string> selected, Action<string> pick, List<Action> refreshers)
        {
            var col = Column();
            col.style.marginTop = U(4f);
            if (!string.IsNullOrEmpty(label)) col.Add(Text(label, SmallPt, Dim));
            var strip = new ScrollView(ScrollViewMode.Horizontal);
            strip.style.flexShrink = 0;
            var buttons = new List<(Button b, Choice c)>();
            foreach (var ch in choices)
            {
                var c = ch;
                Button b = null;
                b = Btn(c.locked ? c.text + " (locked)" : c.text, () => { if (!c.locked) pick(c.id); }, c.tooltip ?? c.text);
                buttons.Add((b, c));
                strip.Add(b);
            }
            col.Add(strip);
            void Refresh()
            {
                string sel = selected();
                foreach (var (b, c) in buttons) SetChip(b, c.id == sel, c.locked);
            }
            Refresh();
            refreshers?.Add(Refresh);
            return col;
        }

        /// <summary>
        /// "Label   value   [−] [+]" with a hint line. Holding a button repeats; all repeats share
        /// <paramref name="gestureKey"/> so they fold into one undo step, sealed on release by <paramref name="release"/>.
        /// </summary>
        public VisualElement Stepper(string label, Func<string> value, Func<string> hint, Action<int, string> step, Action release, string gestureKey, List<Action> refreshers, string tooltip = null)
        {
            var row = Row(2f);
            var left = Column(); left.style.flexGrow = 1; left.style.flexShrink = 1;
            var name = Text(label, BodyPt);
            var val = Text("", TitlePt, TextColor, bold: true);
            var h = Text("", SmallPt, Dim);
            left.Add(name); left.Add(val); left.Add(h);
            row.Add(left);
            row.Add(Hold("−", -1, step, release, gestureKey, (tooltip ?? label) + ", less"));
            row.Add(Hold("+", +1, step, release, gestureKey, (tooltip ?? label) + ", more"));
            void Refresh()
            {
                val.text = value();
                string ht = hint?.Invoke();
                h.text = ht ?? "";
                h.style.display = string.IsNullOrEmpty(ht) ? DisplayStyle.None : DisplayStyle.Flex;
            }
            Refresh();
            refreshers?.Add(Refresh);
            return row;
        }

        VisualElement Hold(string glyph, int dir, Action<int, string> step, Action release, string key, string tooltip)
        {
            var b = new Label(glyph) { tooltip = tooltip };
            b.style.width = U(TargetPt); b.style.height = U(TargetPt); b.style.minWidth = U(TargetPt);
            b.style.fontSize = U(24f);
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.backgroundColor = ChipOff;
            b.style.color = TextColor;
            b.style.marginLeft = U(4f);
            Radius(b.style, TargetPt / 2f);
            IVisualElementScheduledItem repeat = null;
            bool down = false;
            void Stop()
            {
                if (!down) return;
                down = false;
                repeat?.Pause(); repeat = null;
                b.style.backgroundColor = ChipOff;
                release?.Invoke();
            }
            b.RegisterCallback<PointerDownEvent>(e =>
            {
                if (down) return;
                down = true;
                b.style.backgroundColor = ChipOn;
                step(dir, key);
                repeat = b.schedule.Execute(() => { if (down) step(dir, key); }).StartingIn(RepeatDelayMs).Every(RepeatEveryMs);
                e.StopPropagation();
            });
            b.RegisterCallback<PointerUpEvent>(e => Stop());
            b.RegisterCallback<PointerLeaveEvent>(e => Stop());
            b.RegisterCallback<PointerCancelEvent>(e => Stop());
            b.RegisterCallback<DetachFromPanelEvent>(e => Stop());
            return b;
        }

        /// <summary>"Label  [On|Off]". One tap flips it.</summary>
        public VisualElement Switch(string label, string hint, Func<bool> get, Action<bool> set, List<Action> refreshers)
        {
            var row = Row(2f);
            var left = Column(); left.style.flexGrow = 1; left.style.flexShrink = 1;
            left.Add(Text(label, BodyPt));
            if (!string.IsNullOrEmpty(hint)) left.Add(Text(hint, SmallPt, Dim));
            row.Add(left);
            Button b = null;
            b = Btn("", () => set(!get()), label, minWidthPt: 72f);
            row.Add(b);
            void Refresh() { bool on = get(); b.text = on ? "On" : "Off"; SetChip(b, on); b.tooltip = label + (on ? ", on" : ", off"); }
            Refresh();
            refreshers?.Add(Refresh);
            return row;
        }

        /// <summary>A colour swatch row (the 16-colour palette). The selected swatch gets a ring.</summary>
        public VisualElement Swatches(string label, IList<string> palette, Func<string> selected, Action<string> pick, List<Action> refreshers)
        {
            var col = Column();
            col.Add(Text(label, SmallPt, Dim));
            var wrap = new VisualElement();
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.flexWrap = Wrap.Wrap;
            var cells = new List<(VisualElement v, string hex)>();
            foreach (var hex in palette)
            {
                var h = hex;
                var sw = new Button(() => pick(h)) { text = "", tooltip = "Colour " + h };
                sw.style.width = U(TargetPt); sw.style.height = U(TargetPt); sw.style.minWidth = U(TargetPt);
                sw.style.backgroundColor = HangarRocketView.Hex(h, Color.white);
                Radius(sw.style, TargetPt / 2f);
                sw.style.marginLeft = U(3f); sw.style.marginRight = U(3f); sw.style.marginTop = U(3f); sw.style.marginBottom = U(3f);
                cells.Add((sw, h));
                wrap.Add(sw);
            }
            col.Add(wrap);
            void Refresh()
            {
                string sel = selected() ?? "";
                foreach (var (v, hex) in cells)
                {
                    float w = string.Equals(hex, sel, StringComparison.OrdinalIgnoreCase) ? U(3f) : 0f;
                    v.style.borderLeftWidth = w; v.style.borderRightWidth = w; v.style.borderTopWidth = w; v.style.borderBottomWidth = w;
                    v.style.borderLeftColor = ChipOn; v.style.borderRightColor = ChipOn; v.style.borderTopColor = ChipOn; v.style.borderBottomColor = ChipOn;
                }
            }
            Refresh();
            refreshers?.Add(Refresh);
            return col;
        }
    }
}
