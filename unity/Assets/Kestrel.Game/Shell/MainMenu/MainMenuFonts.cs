// Loads the subset Fraunces / Space Grotesk fonts from Resources and applies them inline, so the overlay does not
// depend on USS font URLs resolving (they fell back to the default sans in the #223 device shots). If a font is
// missing the USS rule or the default stays.
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class MainMenuFonts
    {
        public const string Dir = "KHFonts/";
        static Font Load(string n) => Resources.Load<Font>(Dir + n);

        public static void Apply(VisualElement root)
        {
            Set(root, "kh-mm-kestrel", "Fraunces-Black"); Set(root, "kh-mm-launch-label", "Fraunces-Black");
            Set(root, "kh-mm-tile-name", "Fraunces-Bold"); Set(root, "kh-mm-heavy", "Fraunces-Bold"); Set(root, "kh-mm-design-label", "Fraunces-Bold");
            Set(root, "kh-mm-soon", "SpaceGrotesk-Bold"); Set(root, "kh-mm-thrum-name", "SpaceGrotesk-Bold");
            Set(root, "kh-mm-hint", "SpaceGrotesk-Medium"); Set(root, "kh-mm-eyebrow", "SpaceGrotesk-Medium");
            Set(root, "kh-mm-launch-caption", "SpaceGrotesk-Medium"); Set(root, "kh-mm-tile-caption", "SpaceGrotesk-Medium");
            Set(root, "kh-mm-design-caption", "SpaceGrotesk-Medium");
        }

        static void Set(VisualElement root, string cls, string font)
        {
            var f = Load(font); if (f == null) return;
            var def = new StyleFontDefinition(FontDefinition.FromFont(f));
            root.Query(className: cls).ForEach(e => e.style.unityFontDefinition = def);
        }
    }
}
