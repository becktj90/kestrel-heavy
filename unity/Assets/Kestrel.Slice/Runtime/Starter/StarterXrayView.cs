// The X-ray view on the Starter screen (docs/DESIGNER_TIERS_SPEC.md, "X-ray (all tiers)"): a full-screen layer drawn
// from Kestrel.Core.Designer.StarterXray (the model) and StarterXrayLayout (the boxes). A 2D cutaway rather than the
// Hangar's 3D exploded view (HangarExplodedView, DECISIONS #60): that one needs the Hangar camera and orbit, which the
// portrait Starter screen does not have, and a flat cutaway can show the tank fill and the landing band on any phone.
// Each part is a 44 pt button; tapping one fills the card (its plain line; numbers only in Engineer). No animation.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class StarterXrayView
    {
        static readonly Color Bg = new Color(8f / 255f, 12f / 255f, 20f / 255f, 1f);
        static readonly Color CardBg = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color Ink = new Color(0.95f, 0.95f, 0.95f);
        static readonly Color Muted = new Color(0.78f, 0.82f, 0.90f);
        static readonly Color Shell = new Color(0.72f, 0.76f, 0.82f, 0.30f);
        static readonly Color Ox = new Color(120f / 255f, 170f / 255f, 1f, 0.85f);
        static readonly Color FuelC = new Color(1f, 168f / 255f, 76f / 255f, 0.85f);
        static readonly Color Solid = new Color(196f / 255f, 150f / 255f, 110f / 255f, 0.9f);
        static readonly Color Reserve = new Color(44f / 255f, 196f / 255f, 190f / 255f, 1f);
        static readonly Color Metal = new Color(0.55f, 0.58f, 0.64f, 0.9f);
        static readonly Color Cargo = new Color(0.92f, 0.92f, 0.96f, 0.9f);
        static readonly Color Amber = new Color(245f / 255f, 165f / 255f, 36f / 255f);

        public event Action Closed;

        public VisualElement Root { get; }
        public XrayModel Model { get; private set; }
        public StarterXrayFrame Frame { get; private set; }
        public StarterXrayInput Input { get; private set; }
        public string Selected { get; private set; }
        public bool Engineer { get; set; }

        readonly DesignerTierText copy;
        readonly Button close;
        readonly Label title, cardTitle, cardLine, cardNumbers, plan;
        readonly VisualElement cutaway, card, strip, mirror;
        readonly Dictionary<string, Button> parts = new Dictionary<string, Button>();
        readonly List<(VisualElement cell, Label name, Label clock)> cells = new List<(VisualElement, Label, Label)>();
        float upp = 1f, wPt, hPt, safeT, safeB, safeL, safeR;
        bool reserveExit, laidOut;

        public StarterXrayView(DesignerTierText copy, VisualElement parent)
        {
            this.copy = copy ?? new DesignerTierText();
            Root = new VisualElement { name = UiIds.StarterXray };
            Root.style.position = Position.Absolute;
            Root.style.left = 0f; Root.style.top = 0f; Root.style.right = 0f; Root.style.bottom = 0f;
            Root.style.backgroundColor = Bg;

            close = new Button(() => Closed?.Invoke()) { name = UiIds.StarterXrayClose, text = this.copy.XrayClose, tooltip = this.copy.XrayCloseHint };
            ButtonStyle(close, new Color(1f, 1f, 1f, 0.10f), Color.white);
            title = Text(UiIds.StarterXrayTitle, Ink, true, false);
            title.text = this.copy.XrayTitle;
            cutaway = new VisualElement { name = "starter-xray-cutaway", pickingMode = PickingMode.Ignore };
            mirror = new VisualElement { name = "starter-xray-booster-mirror", pickingMode = PickingMode.Ignore };
            card = new VisualElement { name = UiIds.StarterXrayCard, pickingMode = PickingMode.Ignore };
            card.style.backgroundColor = CardBg;
            cardTitle = Text("starter-xray-card-title", Ink, true, true);
            cardLine = Text("starter-xray-card-line", Ink, false, true);
            cardNumbers = Text("starter-xray-card-numbers", Muted, false, true);
            strip = new VisualElement { name = UiIds.StarterXrayPlan, pickingMode = PickingMode.Ignore };
            plan = Text("starter-xray-plan-title", Muted, true, false);
            plan.text = this.copy.XrayPlan;
            Root.Add(close); Root.Add(title); Root.Add(cutaway); Root.Add(mirror); Root.Add(card); Root.Add(strip);
            card.Add(cardTitle); card.Add(cardLine); card.Add(cardNumbers);
            Root.Add(plan);
            parent?.Add(Root);
        }

        /// <summary>Rebuild from the rocket as it is now (the controls may have changed since the last open).</summary>
        public void Show(XrayModel model)
        {
            Model = model ?? new XrayModel();
            foreach (var b in parts.Values) b.RemoveFromHierarchy();
            parts.Clear();
            foreach (var c in cells) c.cell.RemoveFromHierarchy();
            cells.Clear();
            foreach (var p in Model.Parts) parts[p.Id] = MakePart(p);
            foreach (var e in Model.Events)
            {
                var cell = new VisualElement { name = UiIds.StarterXrayEvent(e.Id), pickingMode = PickingMode.Ignore };
                var n = Text("starter-xray-event-name-" + e.Id, Ink, true, false); n.text = e.Label;
                var k = Text("starter-xray-event-time-" + e.Id, Muted, false, false); k.text = e.Clock;
                cell.Add(n); cell.Add(k);
                cell.tooltip = e.Label + " " + e.Clock.Replace("T+", "T plus ");
                strip.Add(cell);
                cells.Add((cell, n, k));
            }
            Selected = null;
            Relayout();
        }

        public void Select(string partId)
        {
            Selected = partId != null && parts.ContainsKey(partId) ? partId : null;
            foreach (var kv in parts)
            {
                bool on = kv.Key == Selected;
                float bw = on ? 3f : 0f;
                kv.Value.style.borderLeftWidth = bw; kv.Value.style.borderRightWidth = bw; kv.Value.style.borderTopWidth = bw; kv.Value.style.borderBottomWidth = bw;
                kv.Value.EnableInClassList("starter-on", on);
            }
            Relayout();
        }

        public void Layout(float widthPt, float heightPt, float safeTop, float safeBottom, float safeLeft, float safeRight, float unitsPerPoint, bool reserveWebExit)
        {
            wPt = widthPt; hPt = heightPt; safeT = safeTop; safeB = safeBottom; safeL = safeLeft; safeR = safeRight;
            upp = unitsPerPoint > 0f ? unitsPerPoint : 1f;
            reserveExit = reserveWebExit;
            laidOut = true;
            Relayout();
        }

        void Relayout()
        {
            if (!laidOut || Model == null) return;
            var sel = Selected != null ? Model.Part(Selected) : null;
            Input = new StarterXrayInput
            {
                Width = wPt, Height = hPt, SafeTop = safeT, SafeBottom = safeB, SafeLeft = safeL, SafeRight = safeR, ReserveExit = reserveExit,
                Close = copy.XrayClose, Title = copy.XrayTitle, Plan = copy.XrayPlan, Model = Model,
                CardTitle = sel?.Title ?? "", CardLine = sel?.Line ?? Model.Pick, CardNumbers = Engineer && sel != null ? sel.Numbers : "",
            };
            var f = StarterXrayLayout.Place(Input);
            Frame = f;
            var zero = new HudBox();
            Pin(close, f.Close, zero); Font(close, StarterXrayLayout.ClosePt); Radius(close, 12f);
            close.style.paddingLeft = StarterLayout.ButtonPadX * upp; close.style.paddingRight = StarterLayout.ButtonPadX * upp;
            Pin(title, f.Title, zero); Font(title, f.TitlePt);
            Pin(cutaway, f.Cutaway, zero);
            foreach (var kv in f.Parts)
            {
                if (!parts.TryGetValue(kv.Key, out var b)) continue;
                Pin(b, kv.Value, zero);
                Bands(b, Model.Part(kv.Key), kv.Value);
            }
            mirror.style.display = f.BoosterMirror.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.BoosterMirror.Live) { Pin(mirror, f.BoosterMirror, zero); mirror.style.backgroundColor = Solid; Radius(mirror, 8f); }

            Pin(card, f.Card, zero); Radius(card, 14f);
            Show(cardTitle, Input.CardTitle, f.CardTitle, f.Card, StarterXrayLayout.CardTitlePt);
            Show(cardLine, Input.CardLine, f.CardLine, f.Card, StarterXrayLayout.CardPt);
            Show(cardNumbers, Input.CardNumbers, f.CardNumbers, f.Card, StarterXrayLayout.NumbersPt);

            strip.style.display = f.Strip.Live ? DisplayStyle.Flex : DisplayStyle.None;
            plan.style.display = f.PlanTitle.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Strip.Live)
            {
                Pin(strip, f.Strip, zero);
                Pin(plan, f.PlanTitle, zero); Font(plan, StarterXrayLayout.CellPt);
                for (int k = 0; k < cells.Count && k < f.Cells.Count; k++)
                {
                    Pin(cells[k].cell, f.Cells[k], f.Strip);
                    Pin(cells[k].name, f.CellLabels[k], f.Cells[k]); Font(cells[k].name, StarterXrayLayout.CellPt);
                    Pin(cells[k].clock, f.CellClocks[k], f.Cells[k]); Font(cells[k].clock, StarterXrayLayout.CellPt);
                }
            }
        }

        void Show(Label l, string text, HudBox box, HudBox parentBox, float pt)
        {
            bool on = box.Live && !string.IsNullOrEmpty(text);
            l.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (!on) return;
            l.text = text;
            Pin(l, box, parentBox);
            Font(l, pt);
        }

        Button MakePart(XrayPart p)
        {
            var b = new Button(() => Select(p.Id)) { name = UiIds.StarterXrayPart(p.Id), text = "", tooltip = p.Title + ". " + p.Line };
            ButtonStyle(b, p.Kind == XrayPartKind.Tank ? Shell : p.Kind == XrayPartKind.Boosters ? Solid : Metal, Ink);
            b.style.borderLeftColor = Amber; b.style.borderRightColor = Amber; b.style.borderTopColor = Amber; b.style.borderBottomColor = Amber;
            b.style.overflow = Overflow.Hidden;
            if (p.Kind == XrayPartKind.Tank)
            {
                foreach (var n in new[] { "ox", "fuel", "reserve" })
                {
                    var band = new VisualElement { name = "starter-xray-band-" + n, pickingMode = PickingMode.Ignore };
                    band.style.position = Position.Absolute;
                    band.style.backgroundColor = n == "ox" ? Ox : n == "reserve" ? Reserve : p.Solid ? Solid : FuelC;
                    b.Add(band);
                }
            }
            else if (p.Kind == XrayPartKind.Fairing)
            {
                var cargo = new VisualElement { name = "starter-xray-cargo", pickingMode = PickingMode.Ignore };
                cargo.style.position = Position.Absolute;
                cargo.style.backgroundColor = Cargo;
                b.Add(cargo);
            }
            Root.Add(b);
            return b;
        }

        /// <summary>Tank: oxidizer above fuel by volume, with the teal landing band at the bottom when a landing is planned.
        /// Fairing: the cargo box inside the nose cone.</summary>
        void Bands(Button b, XrayPart p, HudBox box)
        {
            if (p == null) return;
            float inset = 4f, w = Mathf.Max(0f, box.W - 2f * inset), h = Mathf.Max(0f, box.H - 2f * inset);
            if (p.Kind == XrayPartKind.Tank)
            {
                float oxH = h * (float)p.OxShare, fuelH = h - oxH, resH = h * (float)p.ReserveShare;
                Place(b.Q("starter-xray-band-ox"), inset, inset, w, oxH);
                Place(b.Q("starter-xray-band-fuel"), inset, inset + oxH, w, fuelH);
                var res = b.Q("starter-xray-band-reserve");
                res.style.display = resH > 0.5f ? DisplayStyle.Flex : DisplayStyle.None;
                if (resH > 0.5f) Place(res, inset, inset + h - Mathf.Max(3f, resH), w, Mathf.Max(3f, resH));
            }
            else if (p.Kind == XrayPartKind.Fairing)
                Place(b.Q("starter-xray-cargo"), box.W * 0.25f, box.H * 0.45f, box.W * 0.5f, Mathf.Max(4f, box.H * 0.4f));
        }

        void Place(VisualElement e, float x, float y, float w, float h)
        {
            if (e == null) return;
            e.style.left = x * upp; e.style.top = y * upp; e.style.width = Mathf.Max(0f, w) * upp; e.style.height = Mathf.Max(0f, h) * upp;
        }

        static Label Text(string name, Color color, bool bold, bool wraps)
        {
            var l = new Label { name = name, pickingMode = PickingMode.Ignore };
            l.style.color = color;
            l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            l.style.whiteSpace = wraps ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            l.style.marginLeft = 0f; l.style.marginRight = 0f; l.style.marginTop = 0f; l.style.marginBottom = 0f;
            l.style.paddingLeft = 0f; l.style.paddingRight = 0f; l.style.paddingTop = 0f; l.style.paddingBottom = 0f;
            l.style.unityTextAlign = wraps ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            return l;
        }

        static void ButtonStyle(Button b, Color bg, Color ink)
        {
            b.style.backgroundColor = bg;
            b.style.color = ink;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.whiteSpace = WhiteSpace.NoWrap;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.borderLeftWidth = 0f; b.style.borderRightWidth = 0f; b.style.borderTopWidth = 0f; b.style.borderBottomWidth = 0f;
            b.style.marginLeft = 0f; b.style.marginRight = 0f; b.style.marginTop = 0f; b.style.marginBottom = 0f;
            b.style.paddingTop = 0f; b.style.paddingBottom = 0f; b.style.paddingLeft = 0f; b.style.paddingRight = 0f;
        }

        void Radius(VisualElement e, float pt)
        {
            float r = pt * upp;
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        void Font(VisualElement e, float pt) => e.style.fontSize = pt * upp;

        void Pin(VisualElement e, HudBox b, HudBox parentBox)
        {
            if (e == null) return;
            e.style.position = Position.Absolute;
            e.style.left = (b.X - parentBox.X) * upp;
            e.style.top = (b.Y - parentBox.Y) * upp;
            e.style.width = Mathf.Max(0f, b.W) * upp;
            e.style.height = Mathf.Max(0f, b.H) * upp;
            e.style.right = StyleKeyword.Auto;
            e.style.bottom = StyleKeyword.Auto;
            e.style.flexShrink = 0f;
        }
    }
}
