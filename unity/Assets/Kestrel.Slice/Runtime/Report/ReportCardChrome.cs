// Dusk post-flight sheet. Fonts come from Resources (USS font urls fall back to the
// default sans on device), sizes are in points times the panel's units-per-point,
// and the sheet replaces the flight HUD instead of sitting on top of a live count.
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class ReportCardChrome
    {
        public const float BodyPt = 16f;
        public const float TitlePt = 22f;
        public const float ButtonPt = 17f;
        public const float HitPt = 48f;
        public const string SheetResource = "UI/ReportCard";

        public static readonly Color Ink = new Color(13f / 255f, 20f / 255f, 40f / 255f, 1f);
        public static readonly Color Cream = new Color(246f / 255f, 235f / 255f, 210f / 255f, 1f);
        public static readonly Color Fly = new Color(242f / 255f, 84f / 255f, 61f / 255f, 1f);
        public static readonly Color FlyEdge = new Color(158f / 255f, 36f / 255f, 56f / 255f, 1f);
        public static readonly Color Teal = new Color(70f / 255f, 217f / 255f, 204f / 255f, 1f);
        public static readonly Color Sun = new Color(232f / 255f, 140f / 255f, 60f / 255f, 1f);

        /// <summary>Home, when the card is up. Back and Escape use this.</summary>
        public static Action Escape;

        public static Font TitleFace() => Resources.Load<Font>("KHFonts/Fraunces-Bold");
        public static Font BodyFace() => Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
        public static Font ButtonFace() => Resources.Load<Font>("KHFonts/SpaceGrotesk-Bold");

        /// <summary>
        /// Panel pixels per point from the screen width. Landscape used to divide that
        /// width by the screen height, which grew the card until only the buttons fit.
        /// </summary>
        public static float UnitsPerPoint(float screenWidthPx, float screenHeightPx, float dpi, float panelWidthPx)
        {
            float width = screenWidthPx > 8f ? screenWidthPx : 750f;
            float height = screenHeightPx > 8f ? screenHeightPx : 1334f;
            float ppp = DesignFraming.PixelsPerPoint(width, height, dpi);
            float wPt = screenWidthPx > 8f ? screenWidthPx / ppp : 375f;
            if (panelWidthPx > 1f && wPt > 1f) return panelWidthPx / wPt;
            return ppp > 1f ? ppp : 1f;
        }

        public static void PaintButton(Button button, float tapPt, string role)
        {
            if (button == null) return;
            float tap = tapPt < HitPt ? HitPt : tapPt;
            button.style.minHeight = tap;
            button.style.marginLeft = 12;
            button.style.marginRight = 12;
            button.style.marginBottom = 8;
            button.style.flexShrink = 0;
            button.style.fontSize = ButtonPt;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            button.style.backgroundColor = Fill(role);
            button.style.color = role == "home" || role == "missions" ? Cream : Color.white;
            button.style.borderTopWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderBottomWidth = 4;
            button.style.borderBottomColor = Edge(role);
            button.style.borderTopLeftRadius = 14;
            button.style.borderTopRightRadius = 14;
            button.style.borderBottomLeftRadius = 14;
            button.style.borderBottomRightRadius = 14;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.pickingMode = PickingMode.Position;
            button.focusable = true;
            var face = ButtonFace();
            if (face != null) button.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(face));
        }

        /// <summary>
        /// Full-bleed dusk sheet. <paramref name="unitsPerPoint"/> is panel pixels per iOS point
        /// (1 on the device-layout harness). Safe insets are in points.
        /// </summary>
        public static void Present(VisualElement tree, VisualElement card, float unitsPerPoint, float safeTopPt, float safeBottomPt)
        {
            if (tree == null || card == null) return;
            float upp = unitsPerPoint > 0.25f && unitsPerPoint < 12f ? unitsPerPoint : 1f;
            RetireFlight(tree);
            Dress(card, upp, safeTopPt, safeBottomPt);
            Cover(card);
            if (card.parent != tree) tree.Add(card);
            RetireFlight(tree);
            if (card.panel != null) RetireFlight(card.panel.visualTree);
            Dress(card, upp, safeTopPt, safeBottomPt);
            EventCallback<GeometryChangedEvent> fit = null;
            fit = _ =>
            {
                float known = card.layout.height;
                if (known <= 1f && card.parent != null) known = card.parent.layout.height;
                if (known <= 1f) return;
                card.UnregisterCallback(fit);
                Dress(card, upp, safeTopPt, safeBottomPt);
                Cover(card);
                card.BringToFront();
            };
            card.RegisterCallback(fit);
            card.BringToFront();
            card.pickingMode = PickingMode.Position;
            card.focusable = true;
        }

        public static void Dress(VisualElement card, float unitsPerPoint, float safeTopPt, float safeBottomPt)
        {
            if (card == null) return;
            float upp = unitsPerPoint > 0.25f && unitsPerPoint < 12f ? unitsPerPoint : 1f;
            var sheet = Resources.Load<StyleSheet>(SheetResource);
            if (sheet != null && !card.styleSheets.Contains(sheet)) card.styleSheets.Add(sheet);
            card.AddToClassList("kh-report");
            card.style.backgroundColor = Ink;
            card.style.opacity = 1f;
            card.style.borderTopWidth = 0;
            card.style.borderBottomWidth = 0;
            card.style.borderLeftWidth = 0;
            card.style.borderRightWidth = 0;
            var plate = card.Q("report-plate");
            if (plate != null)
            {
                plate.style.backgroundColor = Ink;
                plate.pickingMode = PickingMode.Ignore;
            }
            var scroll = card.Q("report-scroll");
            var foot = card.Q("report-actions");
            float roomH = card.layout.height;
            if (roomH <= 1f && card.parent != null) roomH = card.parent.layout.height;
            if (roomH <= 1f) roomH = card.resolvedStyle.height;
            float roomW = card.layout.width;
            if (roomW <= 1f && card.parent != null) roomW = card.parent.layout.width;
            if (roomW <= 1f) roomW = card.resolvedStyle.width;
            // L4: cap the scale on short landscape panels, then lay the sheet out in points of that scale.
            upp = ReportFit.Units(upp, roomW, roomH);
            float wPt = roomW > 1f && !float.IsNaN(roomW) ? roomW / upp : 375f;
            float hPt = roomH > 1f && !float.IsNaN(roomH) ? roomH / upp : 667f;
            float contentW = ReportFit.ContentWidth(wPt);
            float side = ReportFit.SideMargin(wPt);

            // Every exit is a direct child of the footer grid, whatever row wrappers the exits code adds
            // (#311 puts Missions and Home in one "report-exit-row"); the grid below owns their placement.
            if (foot != null) FlattenExits(foot);
            var exits = new System.Collections.Generic.List<Button>();
            if (foot != null)
                foreach (var child in foot.Children())
                    if (child is Button eb && eb.style.display != DisplayStyle.None) exits.Add(eb);
            int rows = ReportFit.FootRows(contentW, exits.Count);
            float footPt = foot != null ? ReportFit.FootPt(rows, safeBottomPt) : 0f;
            float footH = footPt * upp;
            float topPt = Mathf.Max(8f, safeTopPt);
            if (scroll != null)
            {
                scroll.style.backgroundColor = Ink;
                scroll.style.position = Position.Absolute;
                scroll.style.left = 0;
                scroll.style.right = 0;
                scroll.style.top = 0;
                scroll.style.bottom = foot != null ? footH : 0;
                scroll.style.height = StyleKeyword.Auto;
                scroll.style.paddingTop = topPt * upp;
                scroll.style.paddingLeft = side * upp;
                scroll.style.paddingRight = side * upp;
                if (scroll is ScrollView sv)
                {
                    // The body is sized to fit (below); no scroll bars, no scrolling sheet.
                    sv.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                    sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                }
            }
            if (foot != null)
            {
                foot.style.position = Position.Absolute;
                foot.style.left = 0;
                foot.style.right = 0;
                foot.style.bottom = 0;
                foot.style.height = footH;
                foot.style.paddingTop = 8f * upp;
                foot.style.paddingBottom = (Mathf.Max(12f, safeBottomPt) + 8f) * upp;
                foot.style.paddingLeft = side * upp;
                foot.style.paddingRight = side * upp;
                foot.style.flexDirection = FlexDirection.Row;
                foot.style.flexWrap = Wrap.Wrap;
                foot.style.alignContent = Align.FlexStart;
                foot.style.justifyContent = Justify.FlexStart;
                foot.style.backgroundColor = Ink;
                foot.BringToFront();
            }
            scroll?.BringToFront();
            foot?.BringToFront();
            var title = TitleFace();
            var body = BodyFace();
            var titleDef = title != null ? new StyleFontDefinition(FontDefinition.FromFont(title)) : default;
            var bodyDef = body != null ? new StyleFontDefinition(FontDefinition.FromFont(body)) : default;

            // Body lines: empty ones go (the gap under Apogee was an empty narrator line with a min height).
            var lines = new System.Collections.Generic.List<ReportFit.Line>();
            var shown = new System.Collections.Generic.List<Label>();
            var plots = scroll != null ? scroll.Q("failure-plots") : null;
            card.Query<Label>().ForEach(label =>
            {
                if (label == null || InGuide(label) || label.name == "plot-label") return;
                bool empty = string.IsNullOrEmpty(label.text);
                label.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
                if (empty) return;
                bool heading = IsTitle(label.name);
                lines.Add(new ReportFit.Line { Name = label.name, Text = label.text, FontPt = heading ? TitlePt : BodyPt, DropOrder = DropOrder(label.name) });
                shown.Add(label);
            });
            float bodyRoom = hPt - footPt - topPt - 4f;
            var plan = ReportFit.Body(lines, plots != null, contentW, bodyRoom);
            for (int i = 0; i < shown.Count; i++)
            {
                var label = shown[i];
                bool heading = IsTitle(label.name);
                bool drop = plan.Dropped.Contains(label.name);
                label.style.display = drop ? DisplayStyle.None : DisplayStyle.Flex;
                float pt = (heading ? TitlePt : BodyPt) * plan.Scale;
                label.style.fontSize = pt * upp;
                label.style.minHeight = 0;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginLeft = 0;
                label.style.marginRight = 0;
                label.style.marginTop = ReportFit.LineTopPt * upp;
                label.style.marginBottom = ReportFit.LineBottomPt * upp;
                label.style.paddingLeft = 0;
                label.style.paddingRight = 0;
                label.style.paddingTop = 0;
                label.style.paddingBottom = 0;
                label.style.maxWidth = Length.Percent(100);
                label.style.color = heading ? Cream : new Color(Cream.r, Cream.g, Cream.b, 0.94f);
                if (heading && title != null) label.style.unityFontDefinition = titleDef;
                else if (body != null) label.style.unityFontDefinition = bodyDef;
            }
            if (plots != null)
            {
                plots.style.display = plan.Plots ? DisplayStyle.Flex : DisplayStyle.None;
                plots.style.height = ReportFit.PlotsPt * plan.Scale * upp;
                plots.style.marginLeft = 0;
                plots.style.marginRight = 0;
                plots.style.marginBottom = ReportFit.GapPt * upp;
                plots.Query<Label>("plot-label").ForEach(l =>
                {
                    l.style.fontSize = 14f * upp;
                    l.style.color = new Color(Cream.r, Cream.g, Cream.b, 0.94f);
                    if (body != null) l.style.unityFontDefinition = bodyDef;
                });
            }

            var buttonFace = ButtonFace();
            var buttonDef = buttonFace != null ? new StyleFontDefinition(FontDefinition.FromFont(buttonFace)) : default;
            card.Query<Button>().ForEach(button =>
            {
                if (button == null) return;
                PaintButton(button, HitPt, Role(button.name));
                button.style.minHeight = ReportFit.HitPt * upp;
                button.style.height = ReportFit.HitPt * upp;
                button.style.fontSize = ButtonPt * upp;
                button.style.borderBottomWidth = 4f * upp;
                button.style.whiteSpace = WhiteSpace.Normal;
                button.style.paddingLeft = 6f * upp;
                button.style.paddingRight = 6f * upp;
                button.style.overflow = Overflow.Hidden;
                if (buttonFace != null) button.style.unityFontDefinition = buttonDef;
                int index = exits.IndexOf(button);
                if (index < 0)
                {
                    button.style.marginBottom = 8f * upp;
                    button.style.marginLeft = 0;
                    button.style.marginRight = 0;
                    return;
                }
                // Absolute cells in the footer's content box: no flex-wrap rounding can push a cell to a new row.
                var cell = ReportFit.GridCell(contentW, exits.Count, index);
                button.style.position = Position.Absolute;
                button.style.left = (side + cell.X) * upp;
                button.style.top = (8f + cell.Y) * upp;
                button.style.flexGrow = 0;
                button.style.flexShrink = 0;
                button.style.flexBasis = StyleKeyword.Auto;
                button.style.minWidth = 0;
                button.style.maxWidth = StyleKeyword.None;
                button.style.width = cell.W * upp;
                button.style.marginLeft = 0;
                button.style.marginTop = 0;
                button.style.marginRight = 0;
                button.style.marginBottom = 0;
                // A label too long for one line at 17 pt ("Back to designer" in a 2-column phone grid) steps
                // down to the 14 pt floor and wraps to two lines inside the 48 pt target.
                float fitPt = ButtonPt;
                float need = FlightHudLayout.TextWidth(button.text ?? "", fitPt);
                float room = cell.W - 12f;
                if (need > room && need > 0f) fitPt = Mathf.Max(14f, fitPt * room / need);
                button.style.fontSize = Mathf.Min(ButtonPt, fitPt) * upp;
            });
        }

        /// <summary>
        /// Lifts every exit button nested in a row wrapper up to be a direct footer child, in reading order, and
        /// hides the emptied wrapper. Idempotent (a second Dress finds nothing nested).
        /// </summary>
        static void FlattenExits(VisualElement foot)
        {
            var kids = new System.Collections.Generic.List<VisualElement>(foot.Children());
            foreach (var k in kids)
            {
                if (k is Button) continue;
                var inner = k.Query<Button>().ToList();
                if (inner.Count == 0) continue;
                int at = foot.IndexOf(k);
                foreach (var b in inner)
                {
                    b.RemoveFromHierarchy();
                    foot.Insert(at++, b);
                }
                k.style.display = DisplayStyle.None;
            }
        }

        /// <summary>Lines dropped (low first) when 70% type still does not fit: narration, then the engineer note.</summary>
        static int DropOrder(string name)
        {
            switch (name)
            {
                case "narrator-eng": return 1;
                case "narrator-kid": return 2;
                case "report-cosmetics": return 3;
                case "report-unlocks": return 4;
                case "failure-engineer": return 5;
                case "report-kc": return 6;
                default: return 0;
            }
        }

        public static void Cover(VisualElement card)
        {
            card.style.position = Position.Absolute;
            card.style.left = 0;
            card.style.top = 0;
            card.style.right = StyleKeyword.Auto;
            card.style.bottom = StyleKeyword.Auto;
            card.style.width = Length.Percent(100);
            card.style.height = Length.Percent(100);
            card.style.marginLeft = 0;
            card.style.marginRight = 0;
            card.style.marginTop = 0;
            card.style.marginBottom = 0;
        }

        static readonly string[] FlightChrome = { UiIds.ScreenFlight, "ascent-hud", "ascent-nudge", UiIds.FlightSkipCount, "guide-overlay", "guide-pop", "guide-box" };
        static readonly string[] Readouts = { "h-t", "h-stage", "h-alt", "h-vel", "h-mach", "h-q", "h-thr", "h-dr", "h-msg", "h-verdict", "h-pulse", "h-callout" };

        /// <summary>Take the result card out of the tree so a retry can count down again.</summary>
        public static void Dismiss(VisualElement tree)
        {
            if (tree == null) return;
            var cards = tree.Query(ReportView.RootName).ToList();
            for (int i = cards.Count - 1; i >= 0; i--)
                cards[i]?.RemoveFromHierarchy();
        }

        /// <summary>
        /// Undo <see cref="RetireFlight"/> on the flight chrome and blank the readouts.
        /// EnterFlight used to only drop the hidden class, so display:none and a disabled HUD
        /// survived into the next count.
        /// </summary>
        public static void RestoreFlight(VisualElement tree)
        {
            if (tree == null) return;
            for (int i = 0; i < FlightChrome.Length; i++)
            {
                var found = tree.Query(FlightChrome[i]).ToList();
                for (int n = 0; n < found.Count; n++) Restore(found[n]);
            }
            var buttons = tree.Query<Button>().ToList();
            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || InReport(button)) continue;
                string name = button.name ?? "";
                if (name == UiIds.FlightSkipCount || name.IndexOf("nudge", StringComparison.OrdinalIgnoreCase) >= 0)
                    Restore(button);
            }
            for (int i = 0; i < Readouts.Length; i++) Clear(tree, Readouts[i]);
        }

        /// <summary>Hide the live flight chrome and clear a countdown that outlived the flight.</summary>
        public static void RetireFlight(VisualElement tree)
        {
            if (tree == null) return;
            WebDesignChrome.Cover(tree);
            for (int i = 0; i < FlightChrome.Length; i++)
            {
                var found = tree.Query(FlightChrome[i]).ToList();
                for (int n = 0; n < found.Count; n++) Retire(found[n]);
            }
            var buttons = tree.Query<Button>().ToList();
            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || InReport(button)) continue;
                string name = button.name ?? "";
                if (name == UiIds.FlightSkipCount || name.IndexOf("nudge", StringComparison.OrdinalIgnoreCase) >= 0)
                    Retire(button);
            }
            for (int i = 0; i < Readouts.Length; i++) Clear(tree, Readouts[i]);
        }

        public static bool ConsumeKey(VisualElement tree)
        {
            bool down;
            try { down = Input.GetKeyDown(KeyCode.Escape); }
            catch (Exception) { down = false; }
            if (!down || tree == null || tree.Q(ReportView.RootName) == null) return false;
            InvokeEscape(tree);
            return true;
        }

        public static void InvokeEscape(VisualElement tree)
        {
            if (Escape != null)
            {
                Escape();
                return;
            }
            var home = tree != null ? tree.Q<Button>("report-home") : null;
            var missions = tree != null ? tree.Q<Button>("report-missions") : null;
            Click(home ?? missions);
        }

        static void Click(Button button)
        {
            if (button == null) return;
            // Clickable listens for a navigation submit. A pooled ClickEvent does not run the button action.
            using (var ev = NavigationSubmitEvent.GetPooled())
            {
                ev.target = button;
                button.SendEvent(ev);
            }
        }

        const string PickSaved = "kh-report-pick-saved";
        const string PickIgnore = "kh-report-pick-ignore";

        static void Retire(VisualElement el)
        {
            if (el == null || InReport(el)) return;
            if (!el.ClassListContains(PickSaved))
            {
                el.AddToClassList(PickSaved);
                // guide-overlay, hud and ascent-hud are full-screen and Ignore.
                // Remembering that here keeps a retry from turning them into the hit target.
                if (el.pickingMode == PickingMode.Ignore) el.AddToClassList(PickIgnore);
            }
            el.AddToClassList("hidden");
            el.style.display = DisplayStyle.None;
            el.SetEnabled(false);
            el.pickingMode = PickingMode.Ignore;
        }

        static void Restore(VisualElement el)
        {
            if (el == null || InReport(el)) return;
            el.RemoveFromClassList("hidden");
            el.style.display = StyleKeyword.Null;
            el.SetEnabled(true);
            if (!el.ClassListContains(PickSaved)) return;
            bool ignore = el.ClassListContains(PickIgnore);
            el.RemoveFromClassList(PickIgnore);
            el.RemoveFromClassList(PickSaved);
            el.pickingMode = ignore ? PickingMode.Ignore : PickingMode.Position;
        }

        static void Clear(VisualElement tree, string name)
        {
            var labels = tree.Query<Label>(name).ToList();
            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i] == null || InReport(labels[i])) continue;
                labels[i].text = "";
            }
        }

        static bool InReport(VisualElement el)
        {
            for (var p = el; p != null; p = p.parent)
                if (p.name == ReportView.RootName) return true;
            return false;
        }

        static bool InGuide(VisualElement el)
        {
            for (var p = el; p != null; p = p.parent)
                if (p.name == "guide-pop" || p.name == "guide-overlay") return true;
            return false;
        }

        public static bool IsTitle(string name)
            => name == "failure-cause" || name == "report-stars";

        public static string Role(string name)
        {
            switch (name)
            {
                case "failure-fix": return "fix";
                case "report-next": return "next";
                case "report-retry": return "retry";
                case UiIds.ReportDesigner: return "designer";
                case "report-back-to-designer": return "designer"; // #311's id for the same exit
                case "report-missions": return "missions";
                case "report-home": return "home";
                default: return "home";
            }
        }

        static Color Fill(string role)
        {
            switch (role)
            {
                case "fix": return Fly;
                case "next": return new Color(20f / 255f, 92f / 255f, 96f / 255f, 1f);
                case "retry": return Sun;
                case "designer": return new Color(48f / 255f, 62f / 255f, 112f / 255f, 1f);
                default: return new Color(30f / 255f, 46f / 255f, 72f / 255f, 1f);
            }
        }

        static Color Edge(string role)
        {
            switch (role)
            {
                case "fix": return FlyEdge;
                case "next": return new Color(10f / 255f, 48f / 255f, 52f / 255f, 1f);
                case "retry": return new Color(140f / 255f, 72f / 255f, 20f / 255f, 1f);
                case "designer": return new Color(24f / 255f, 32f / 255f, 66f / 255f, 1f);
                default: return new Color(246f / 255f, 235f / 255f, 210f / 255f, 0.45f);
            }
        }
    }
}
