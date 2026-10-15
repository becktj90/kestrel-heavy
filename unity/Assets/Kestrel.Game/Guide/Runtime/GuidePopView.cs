using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide
{
    /// <summary>A 16×16 portrait that slides in at the top-right corner. One caption line sits under it
    /// only when her voice will not play. The portrait is the only hit target.</summary>
    public sealed class GuidePopView
    {
        public readonly VisualElement Pop;
        public Texture2D Portrait;
        public GlyphTable Glyphs;
        public Texture2D Atlas;
        public event System.Action Tapped;

        public GuidePopView(VisualElement parent)
        {
            var host = parent ?? new VisualElement { name = "guide-host" };
            Pop = new VisualElement { name = "guide-pop", pickingMode = PickingMode.Position };
            Pop.tooltip = "Thrum, hangar keeper. Tap to replay or dismiss.";
            Pop.style.position = Position.Absolute;
            Pop.style.display = DisplayStyle.None;
            Pop.style.flexDirection = FlexDirection.Column;
            Pop.style.alignItems = Align.FlexEnd;

            var portrait = new VisualElement { name = "guide-portrait", pickingMode = PickingMode.Ignore };
            portrait.style.flexShrink = 0;
            Pop.Add(portrait);

            var caption = new BitmapTextElement { name = "guide-caption" };
            caption.style.display = DisplayStyle.None;
            caption.style.marginTop = 2f;
            Pop.Add(caption);

            Pop.RegisterCallback<ClickEvent>(_ => Tapped?.Invoke());
            host.Add(Pop);
        }

        public void Hide()
        {
            Pop.style.display = DisplayStyle.None;
        }

        public void Show(GuidePopFrame frame, float panelPerPoint, bool caption, string text, float glyphPt)
        {
            float scale = panelPerPoint > 0f ? panelPerPoint : 1f;
            float side = frame.Size * scale;
            float block = frame.Block * scale;
            Pop.style.display = DisplayStyle.Flex;
            Pop.style.left = frame.X * scale;
            Pop.style.top = frame.Y * scale;
            Pop.style.width = block;
            Pop.style.height = StyleKeyword.Auto;

            var portrait = Pop.Q("guide-portrait");
            if (portrait != null)
            {
                portrait.style.width = side;
                portrait.style.height = side;
                portrait.style.minWidth = side;
                portrait.style.minHeight = side;
                if (Portrait != null)
                {
                    Portrait.filterMode = FilterMode.Point;
                    Portrait.anisoLevel = 0;
                    portrait.style.backgroundImage = new StyleBackground(Portrait);
                    portrait.style.backgroundSize = new BackgroundSize(new Length(side), new Length(side));
                    portrait.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                }
            }

            var line = Pop.Q<BitmapTextElement>("guide-caption");
            if (line == null) return;
            line.style.display = caption ? DisplayStyle.Flex : DisplayStyle.None;
            if (!caption) return;
            float glyph = glyphPt >= 7f ? glyphPt : 7f;
            int texel = (int)Mathf.Floor(glyph * scale / 7f + 0.001f);
            if (texel < 1) texel = 1;
            line.Glyphs = Glyphs;
            line.Atlas = Atlas;
            if (Atlas != null)
            {
                Atlas.filterMode = FilterMode.Point;
                Atlas.anisoLevel = 0;
            }
            float width = frame.CaptionW * scale;
            line.FontPx = texel * 7f;
            line.LayoutWidth = width;
            line.style.width = width;
            line.style.maxWidth = width;
            line.SetLine(text ?? "", false);
        }
    }
}
