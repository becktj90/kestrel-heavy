// Resources fonts and tap-target containment for the portrait designer.
// Fonts come from Resources/KHFonts only. A dummy KESTREL_REPO_ROOT must not be walked for faces.
using System;
using Kestrel.Game.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice.DesignerV3
{
    public static class DesignerChrome
    {
        const string FrauncesBold = "KHFonts/Fraunces-Bold";
        const string Grotesk = "KHFonts/SpaceGrotesk-Medium";
        const string GroteskBold = "KHFonts/SpaceGrotesk-Bold";

        static Font frauncesBold;
        static Font grotesk;
        static Font groteskBold;
        static bool fontsTried;
        static bool containing;

        public static void Apply(VisualElement root)
        {
            if (root == null) return;
            LoadFonts();
            if (frauncesBold != null || grotesk != null || groteskBold != null)
                Paint(root);
            Contain(root);
        }

        public static void Contain(VisualElement root)
        {
            if (root == null || containing) return;
            float width = root.resolvedStyle.width;
            if (float.IsNaN(width) || width < 8f) width = root.worldBound.width;
            if (float.IsNaN(width) || width < 8f) return;
            containing = true;
            try
            {
                Cap(root, width);
                TextFit.Apply(root);
            }
            finally { containing = false; }
        }

        static void LoadFonts()
        {
            if (fontsTried) return;
            fontsTried = true;
            try
            {
                frauncesBold = Resources.Load<Font>(FrauncesBold);
                grotesk = Resources.Load<Font>(Grotesk);
                groteskBold = Resources.Load<Font>(GroteskBold);
            }
            catch (Exception)
            {
                frauncesBold = null;
                grotesk = null;
                groteskBold = null;
            }
        }

        static void Paint(VisualElement el)
        {
            if (el is Button)
            {
                var face = groteskBold != null ? groteskBold : grotesk;
                if (face != null) el.style.unityFontDefinition = FontDefinition.FromFont(face);
            }
            else if (el is Label label)
            {
                bool bold = label.style.unityFontStyleAndWeight == FontStyle.Bold;
                Font face = bold && frauncesBold != null ? frauncesBold : grotesk;
                if (face != null) label.style.unityFontDefinition = FontDefinition.FromFont(face);
            }
            else if (el is TextField && grotesk != null)
                el.style.unityFontDefinition = FontDefinition.FromFont(grotesk);

            foreach (var child in el.Children()) Paint(child);
        }

        static void Cap(VisualElement el, float width)
        {
            if (el is ScrollView scroll && scroll.mode == ScrollViewMode.Horizontal)
            {
                float inner = scroll.resolvedStyle.width;
                if (float.IsNaN(inner) || inner < 8f) inner = scroll.worldBound.width;
                if (float.IsNaN(inner) || inner < 8f) inner = width;
                var box = scroll.contentContainer;
                if (box != null)
                    foreach (var child in box.Children()) CapLeaf(child, inner);
                return;
            }
            if (el is Button || el is TextField) Fit(el, width);
            foreach (var child in el.Children()) Cap(child, width);
        }

        static void CapLeaf(VisualElement el, float width)
        {
            if (el is Button || el is Label || el is TextField) Fit(el, width);
            foreach (var child in el.Children()) CapLeaf(child, width);
        }

        static void Fit(VisualElement el, float width)
        {
            if (width < 8f) return;
            var current = el.style.maxWidth;
            if (current.keyword == StyleKeyword.Undefined || Math.Abs(current.value.value - width) > 0.5f)
                el.style.maxWidth = width;
        }
    }
}
