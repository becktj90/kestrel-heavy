// Dusk post-flight sheet. Fonts come from Resources (USS font urls fall back to the
// default sans on device), sizes are in points times the panel's units-per-point,
// and the sheet replaces the flight HUD instead of sitting on top of a live count.
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
            float footH = ((HitPt + 8f) * 5f + Mathf.Max(12f, safeBottomPt) + 24f) * upp;
            if (scroll != null)
            {
                scroll.style.backgroundColor = Ink;
                scroll.style.position = Position.Absolute;
                scroll.style.left = 0;
                scroll.style.right = 0;
                scroll.style.top = 0;
                scroll.style.bottom = foot != null ? footH : 0;
                scroll.style.height = StyleKeyword.Auto;
                scroll.style.paddingTop = Mathf.Max(8f, safeTopPt) * upp;
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
                foot.style.backgroundColor = Ink;
            }
            var title = TitleFace();
            var body = BodyFace();
            var titleDef = title != null ? new StyleFontDefinition(FontDefinition.FromFont(title)) : default;
            var bodyDef = body != null ? new StyleFontDefinition(FontDefinition.FromFont(body)) : default;
            card.Query<Label>().ForEach(label =>
            {
                if (label == null || InGuide(label)) return;
                bool heading = IsTitle(label.name);
                float pt = heading ? TitlePt : BodyPt;
                if (label.name == "plot-label") pt = 14f;
                label.style.fontSize = pt * upp;
                label.style.color = heading ? Cream : new Color(Cream.r, Cream.g, Cream.b, 0.94f);
                if (heading && title != null) label.style.unityFontDefinition = titleDef;
                else if (body != null) label.style.unityFontDefinition = bodyDef;
            });
            var buttonFace = ButtonFace();
            var buttonDef = buttonFace != null ? new StyleFontDefinition(FontDefinition.FromFont(buttonFace)) : default;
            card.Query<Button>().ForEach(button =>
            {
                if (button == null) return;
                PaintButton(button, HitPt, Role(button.name));
                button.style.minHeight = HitPt * upp;
                button.style.fontSize = ButtonPt * upp;
                button.style.marginBottom = 8f * upp;
                button.style.marginLeft = 12f * upp;
                button.style.marginRight = 12f * upp;
                button.style.borderBottomWidth = 4f * upp;
                if (buttonFace != null) button.style.unityFontDefinition = buttonDef;
            });
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

        static readonly string[] FlightChrome = { "hud", "ascent-hud", "ascent-nudge", "skip-count", "guide-overlay", "guide-pop", "guide-box" };
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
                if (name == "skip-count" || name.IndexOf("nudge", StringComparison.OrdinalIgnoreCase) >= 0)
                    Restore(button);
            }
            for (int i = 0; i < Readouts.Length; i++) Clear(tree, Readouts[i]);
        }

        /// <summary>Hide the live flight chrome and clear a countdown that outlived the flight.</summary>
        public static void RetireFlight(VisualElement tree)
        {
            if (tree == null) return;
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
                if (name == "skip-count" || name.IndexOf("nudge", StringComparison.OrdinalIgnoreCase) >= 0)
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
                default: return new Color(246f / 255f, 235f / 255f, 210f / 255f, 0.45f);
            }
        }
    }
}
