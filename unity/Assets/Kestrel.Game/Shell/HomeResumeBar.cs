// Credits chip and CONTINUE on Home. The chip is always there. CONTINUE appears only when
// a draft, a saved rocket, or a cleared mission exists. Text shrinks until it fits 335 px.
using System;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class HomeResumeBar
    {
        public const string BarName = "home-resume";
        public const string ChipName = "home-credits";
        public const string ContinueName = "home-continue";

        public static VisualElement Build(long credits, bool showContinue, Action onContinue)
        {
            var face = Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
            var bar = new VisualElement { name = BarName, pickingMode = PickingMode.Ignore };
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.style.position = Position.Absolute;
            bar.style.left = 8;
            bar.style.right = 8;
            bar.style.height = 48;
            bar.style.minHeight = 44;

            var chip = new Label(CreditsText.Chip(credits)) { name = ChipName, pickingMode = PickingMode.Ignore };
            Face(chip, face, 18);
            chip.style.flexGrow = 1;
            chip.style.flexShrink = 1;
            chip.style.color = new Color(0.96f, 0.94f, 0.90f, 1f);
            chip.style.whiteSpace = WhiteSpace.NoWrap;
            chip.style.overflow = Overflow.Hidden;
            chip.style.textOverflow = TextOverflow.Clip;
            bar.Add(chip);

            if (showContinue)
            {
                var button = new Button(onContinue) { name = ContinueName, text = ResumeOffer.ContinueLabel };
                button.tooltip = ResumeOffer.ContinueSpoken;
                Dusk(button);
                bar.Add(button);
            }

            bar.RegisterCallback<GeometryChangedEvent>(_ => Fit(bar));
            return bar;
        }

        /// <summary>Add the bar and pin it in the top-left corner band, clear of the title, stats, gear and EXIT.</summary>
        public static void Mount(VisualElement root, long credits, bool showContinue, Action onContinue)
        {
            if (root == null) return;
            var old = root.Q(BarName);
            if (old != null) old.RemoveFromHierarchy();
            var bar = Build(credits, showContinue, onContinue);
            root.Add(bar);
            void Place(GeometryChangedEvent _)
            {
                PlaceBar(root, bar);
                Fit(bar);
            }
            root.RegisterCallback<GeometryChangedEvent>(Place);
            PlaceBar(root, bar);
        }

        /// <summary>Shrink the chip and CONTINUE from 18 px down to 11 px until both fit the bar.</summary>
        public static void Fit(VisualElement bar)
        {
            if (bar == null) return;
            var chip = bar.Q<Label>(ChipName);
            var button = bar.Q<Button>(ContinueName);
            float width = bar.resolvedStyle.width;
            if (width < 8f) width = bar.layout.width;
            if (width < 8f) return;
            int chosen = 11;
            for (int size = 18; size >= 11; size--)
            {
                chosen = size;
                float need = Measure(chip, size) + Measure(button, size);
                if (button != null) need += 24f + 2f + 8f + 2f;   // padding, border, the 8 px gap, slack
                if (need <= width) break;
            }
            if (chip != null)
            {
                chip.style.fontSize = chosen;
                float text = Measure(chip, chosen);
                if (text > 0f) chip.style.minWidth = text;
            }
            if (button != null)
            {
                button.style.fontSize = chosen;
                float text = Measure(button, chosen);
                if (text > 0f) button.style.minWidth = text + 24f;
            }
        }

        static void PlaceBar(VisualElement root, VisualElement bar)
        {
            // The free top-left band (HomeCornerLayout.Credits), never on the title, the stats or LAUNCH.
            bar.style.position = Position.Absolute;
            bar.style.bottom = StyleKeyword.Auto;
            bar.style.right = StyleKeyword.Auto;
            var parent = bar.parent ?? root;
            float width = parent.worldBound.width;
            if (float.IsNaN(width) || width < 1f) width = parent.layout.width;
            // Only the web page has an EXIT beside the gear. Reserving it on iOS left a 131 px bar on a 308-wide
            // phone panel, and the chip and CONTINUE shrank to 11 px.
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;
            var box = HomeCornerLayout.Credits(width > 1f ? width : 375f, 0f, web);
            bar.style.left = box.X;
            bar.style.top = box.Y;
            bar.style.width = box.W;
            bar.style.height = box.H;
            bar.style.minHeight = box.H;
        }

        public const string ContinueClass = "kh-mm-continue";

        /// <summary>CONTINUE in the Home look: the dark glass of DESIGN FIRST, cream Fraunces, a 44 pt pill. Inline, so the
        /// runtime theme's grey system button never shows (TestFlight drew a grey default button over the eyebrow).</summary>
        public static void Dusk(Button button)
        {
            if (button == null) return;
            button.AddToClassList("kh-button");
            button.AddToClassList(ContinueClass);
            var serif = Resources.Load<Font>("KHFonts/Fraunces-Bold");
            Face(button, serif != null ? serif : Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium"), 18);
            button.style.minHeight = 44;
            button.style.height = 44;
            button.style.marginTop = 0; button.style.marginBottom = 0; button.style.marginLeft = 8; button.style.marginRight = 0;
            button.style.paddingLeft = 12; button.style.paddingRight = 12; button.style.paddingTop = 0; button.style.paddingBottom = 0;
            button.style.flexShrink = 0;
            button.style.whiteSpace = WhiteSpace.NoWrap;
            button.style.overflow = Overflow.Hidden;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.color = new Color(0.957f, 0.918f, 0.824f, 1f);
            button.style.backgroundColor = new Color(8f / 255f, 12f / 255f, 28f / 255f, 0.88f);
            var edge = new Color(0.957f, 0.918f, 0.824f, 0.55f);
            button.style.borderTopColor = edge; button.style.borderBottomColor = edge; button.style.borderLeftColor = edge; button.style.borderRightColor = edge;
            button.style.borderTopWidth = 1; button.style.borderBottomWidth = 1; button.style.borderLeftWidth = 1; button.style.borderRightWidth = 1;
            button.style.borderTopLeftRadius = 22; button.style.borderTopRightRadius = 22; button.style.borderBottomLeftRadius = 22; button.style.borderBottomRightRadius = 22;
            button.style.letterSpacing = 0.5f;
            button.style.unityFontStyleAndWeight = FontStyle.Normal;
        }

        static float Measure(TextElement el, int size)
        {
            if (el == null || string.IsNullOrEmpty(el.text)) return 0f;
            el.style.fontSize = size;
            return el.MeasureTextSize(el.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
        }

        static void Face(VisualElement el, Font face, int size)
        {
            el.style.fontSize = size;
            if (face != null) el.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(face));
        }
    }
}
