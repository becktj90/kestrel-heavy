// Credits chip and CONTINUE on Home. The chip is always there. CONTINUE appears only when
// a draft, a saved rocket, or a cleared mission exists. Text shrinks until it fits 335 px.
using System;
using Kestrel.Core.Economy;
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
                Face(button, face, 18);
                button.style.minHeight = 44;
                button.style.paddingLeft = 12;
                button.style.paddingRight = 12;
                button.style.whiteSpace = WhiteSpace.NoWrap;
                button.style.overflow = Overflow.Hidden;
                button.style.unityTextAlign = TextAnchor.MiddleCenter;
                bar.Add(button);
            }

            bar.RegisterCallback<GeometryChangedEvent>(_ => Fit(bar));
            return bar;
        }

        /// <summary>Insert the bar just before LAUNCH and pin it above that button. A wide frame clamps the top at 0.</summary>
        public static void Mount(VisualElement root, long credits, bool showContinue, Action onContinue)
        {
            if (root == null) return;
            var old = root.Q(BarName);
            if (old != null) old.RemoveFromHierarchy();
            var bar = Build(credits, showContinue, onContinue);
            var launch = root.Q("home-launch");
            if (launch != null && launch.parent != null)
            {
                int at = launch.parent.IndexOf(launch);
                if (at >= 0) launch.parent.Insert(at, bar);
                else root.Add(bar);
            }
            else root.Add(bar);
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
                if (button != null) need += 28f;
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
            var launch = root.Q("home-launch");
            bar.style.position = Position.Absolute;
            bar.style.left = 8;
            bar.style.right = 8;
            bar.style.bottom = StyleKeyword.Auto;
            bar.style.height = 48;
            float top = 8f;
            if (launch != null && launch.layout.height > 1f)
            {
                top = launch.layout.y - 52f;
                if (top < 0f) top = 0f;
            }
            if (root.ClassListContains("kh-mm-wide") && top < 0f) top = 0f;
            bar.style.top = top;
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
