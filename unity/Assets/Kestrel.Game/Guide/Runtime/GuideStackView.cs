using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide
{
    /// <summary>The retro subtitle stack. Ink panel, cream border, hard shadow.
    /// Ascent docks under the header and stays out of the thumb zone. Menu screens dock one box
    /// above the Home key and leave the mission list the space under it. Empty space does not pick,
    /// so buttons underneath still receive touches.</summary>
    public sealed class GuideStackView
    {
        public static readonly Color Ink = new Color(0x1B / 255f, 0x1F / 255f, 0x2A / 255f, 1f);
        public static readonly Color Cream = new Color(0xFA / 255f, 0xF0 / 255f, 0xD7 / 255f, 1f);
        public static readonly Color Amber = new Color(0xE8 / 255f, 0xB1 / 255f, 0x5A / 255f, 1f);
        public static readonly Color Shadow = new Color(0x0A / 255f, 0x0C / 255f, 0x12 / 255f, 1f);

        public readonly VisualElement Root;
        public GuideStage Stage;
        public GuideScreen Screen = new GuideScreen(375f, 667f, 20f, 0f, 0f, 0f, 0f, false);
        public float PanelPerPoint = 1f;
        public GlyphTable Glyphs;
        public Texture2D Atlas;
        public Texture2D Portrait;
        /// <summary>Panel pixels the menu scroll should give up so the box does not cover the cards. 0 off-menu.</summary>
        public float ClearancePx { get; private set; }

        readonly VisualElement stack;
        string signature = "";
        float downAt = -1f;
        int downIndex = -1;
        int suppressTap = -1;

        public GuideStackView(VisualElement parent, GuideStage stage)
        {
            Stage = stage;
            Root = parent ?? new VisualElement { name = "guide-host" };
            stack = new VisualElement { name = "guide-stack", pickingMode = PickingMode.Ignore };
            stack.AddToClassList("guide-stack");
            var sheet = Resources.Load<StyleSheet>("UI/GuideBox");
            if (sheet != null) stack.styleSheets.Add(sheet);
            Root.Add(stack);
        }

        public void Tick(float dt)
        {
            if (Stage == null) return;
            FinishSkipIfReleased();
            Stage.Tick(dt);
            Sync();
            if (Input.touchCount == 0 && !Input.GetMouseButton(0)) suppressTap = -1;
        }

        /// <summary>A press that ends outside the SKIP control still counts. Long-press fires at 0.5s
        /// even if the finger is still down. The following click on the box is swallowed.</summary>
        void FinishSkipIfReleased()
        {
            if (downIndex < 0 || Stage == null) return;
            bool held = Input.touchCount > 0 || Input.GetMouseButton(0);
            float elapsed = Time.unscaledTime - downAt;
            if (held && elapsed < GuidePacing.LongPressSeconds) return;
            int index = downIndex;
            downIndex = -1;
            downAt = -1f;
            suppressTap = index;
            if (elapsed >= GuidePacing.LongPressSeconds) Stage.SkipStepTips(index);
            else Stage.Skip(index);
        }

        public void Sync()
        {
            if (Stage == null) { stack.Clear(); signature = ""; return; }
            var models = Stage.Models();
            float scale = PanelPerPoint > 0f ? PanelPerPoint : 1f;
            float font = GuidePacing.FontPt(Stage.Engine.Settings.Size);
            bool[] portrait = new bool[models.Length];
            string[] measure = new string[models.Length];
            for (int i = 0; i < models.Length; i++)
            {
                portrait[i] = models[i].Portrait;
                measure[i] = models[i].Full;
            }
            var frames = GuideLayout.Place(Screen, portrait, measure, font, Stage.Place);
            int shown = models.Length < frames.Length ? models.Length : frames.Length;
            string next = "";
            for (int i = 0; i < shown; i++) next += models[i].Id + "|";
            next += Screen.Compact ? "c" : "f";
            next += Stage.Place == GuidePlace.Menu ? "m" : "d";
            if (next != signature || stack.childCount != shown)
            {
                signature = next;
                stack.Clear();
                for (int i = 0; i < shown; i++) stack.Add(Build(models[i], i));
            }
            if (shown == 0)
            {
                stack.style.display = DisplayStyle.None;
                ClearancePx = 0f;
                return;
            }
            stack.style.display = DisplayStyle.Flex;
            stack.style.position = Position.Absolute;
            stack.style.left = frames[0].X * scale;
            stack.style.top = frames[0].Y * scale;
            stack.style.width = frames[0].W * scale;
            float band = 0f;
            for (int i = 0; i < shown && i < stack.childCount; i++)
            {
                Fill(stack[i], models[i], frames[i], scale);
                band += frames[i].H;
            }
            if (shown > 1) band += GuidePacing.StackGapPt * (shown - 1);
            ClearancePx = Stage.Place == GuidePlace.Menu
                ? (band + GuidePacing.ShadowPt + GuidePacing.StackGapPt) * scale
                : 0f;
        }

        VisualElement Build(GuideBoxModel model, int index)
        {
            var box = new VisualElement { name = "guide-box", pickingMode = PickingMode.Position };
            box.AddToClassList("guide-box");
            int captured = index;
            box.RegisterCallback<ClickEvent>(_ =>
            {
                if (suppressTap == captured || downIndex >= 0)
                {
                    suppressTap = -1;
                    return;
                }
                Stage?.Tap(captured);
                Sync();
            });

            var shadow = new VisualElement { name = "guide-shadow", pickingMode = PickingMode.Ignore };
            shadow.AddToClassList("guide-shadow");
            box.Add(shadow);

            var panel = new VisualElement { name = "guide-panel", pickingMode = PickingMode.Ignore };
            panel.AddToClassList("guide-panel");
            box.Add(panel);

            var row = new VisualElement { name = "guide-row", pickingMode = PickingMode.Ignore };
            row.AddToClassList("guide-row");
            panel.Add(row);

            var portrait = new VisualElement { name = "guide-portrait", pickingMode = PickingMode.Ignore };
            portrait.AddToClassList("guide-portrait");
            row.Add(portrait);

            var column = new VisualElement { name = "guide-column", pickingMode = PickingMode.Ignore };
            column.AddToClassList("guide-column");
            row.Add(column);

            var name = new Label { name = "guide-name", pickingMode = PickingMode.Ignore };
            name.AddToClassList("guide-name");
            column.Add(name);

            var body = new BitmapTextElement { name = "guide-body" };
            body.AddToClassList("guide-body");
            body.style.flexShrink = 0;
            var clip = new ScrollView(ScrollViewMode.Vertical) { name = "guide-body-clip", pickingMode = PickingMode.Ignore };
            clip.AddToClassList("guide-body-clip");
            clip.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            clip.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            clip.Add(body);
            column.Add(clip);

            var foot = new VisualElement { name = "guide-foot", pickingMode = PickingMode.Ignore };
            foot.AddToClassList("guide-foot");
            column.Add(foot);
            var indexLabel = new Label { name = "guide-index", pickingMode = PickingMode.Ignore };
            indexLabel.AddToClassList("guide-index");
            var hint = new Label { name = "guide-hint", pickingMode = PickingMode.Ignore };
            hint.AddToClassList("guide-hint");
            foot.Add(indexLabel);
            foot.Add(hint);

            var skip = new Button { name = "guide-skip", text = "SKIP" };
            skip.tooltip = "Skip. Skips this beat. Press and hold to skip tips for this step.";
            skip.AddToClassList("guide-skip");
            skip.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            skip.RegisterCallback<PointerDownEvent>(e =>
            {
                downAt = Time.unscaledTime;
                downIndex = captured;
                e.StopPropagation();
            });
            skip.RegisterCallback<PointerUpEvent>(e =>
            {
                e.StopPropagation();
                if (downIndex != captured) return;
                float held = Time.unscaledTime - downAt;
                downIndex = -1;
                downAt = -1f;
                suppressTap = captured;
                if (held >= GuidePacing.LongPressSeconds) Stage?.SkipStepTips(captured);
                else Stage?.Skip(captured);
                Sync();
            });
            skip.RegisterCallback<PointerCancelEvent>(e =>
            {
                if (downIndex != captured) return;
                downIndex = -1;
                downAt = -1f;
                e.StopPropagation();
            });
            box.Add(skip);
            return box;
        }

        void Fill(VisualElement box, GuideBoxModel model, GuideFrame frame, float scale)
        {
            float font = frame.Font * scale;
            if (font < GuidePacing.MinFontPt * scale) font = GuidePacing.MinFontPt * scale;
            box.EnableInClassList("guide-box-compact", Screen.Compact);
            box.style.height = frame.H * scale;
            box.style.width = frame.W * scale;
            box.style.marginBottom = GuidePacing.StackGapPt * scale;
            box.style.position = Position.Relative;
            box.style.overflow = Overflow.Visible;
            box.style.flexShrink = 0;
            PaintChrome(box, scale);

            var portrait = box.Q("guide-portrait");
            if (portrait != null)
            {
                bool show = model.Portrait && frame.Portrait > 0f;
                portrait.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                portrait.style.width = frame.Portrait * scale;
                portrait.style.height = frame.Portrait * scale;
                portrait.style.minWidth = frame.Portrait * scale;
                var columnGap = box.Q("guide-column");
                if (columnGap != null) columnGap.style.marginLeft = (show ? GuidePacing.ColumnGapPt : 0f) * scale;
                if (show && Portrait != null)
                {
                    Portrait.filterMode = FilterMode.Point;
                    portrait.style.backgroundImage = Portrait;
                    float side = frame.Portrait * scale;
                    portrait.style.backgroundSize = new BackgroundSize(new Length(side), new Length(side));
                    portrait.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                }
            }

            var name = box.Q<Label>("guide-name");
            if (name != null)
            {
                name.text = string.IsNullOrEmpty(model.Role) ? model.Name : model.Name + " \u00B7 " + model.Role;
                name.style.fontSize = font;
                name.style.color = Amber;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                // The name sits above SKIP. It may use that width. The body and the hint stay in the column.
                bool nameBesidePortrait = model.Portrait && frame.Portrait > 0f && frame.H > frame.Skip + frame.Font * 2f;
                name.style.marginRight = nameBesidePortrait ? -(frame.Skip + GuidePacing.ColumnGapPt) * scale : 0f;
                name.style.overflow = nameBesidePortrait ? Overflow.Visible : Overflow.Hidden;
            }

            var body = box.Q<BitmapTextElement>("guide-body");
            float glyphPt = frame.Glyph >= 7f ? frame.Glyph : GuidePacing.GlyphPt(frame.Font);
            int texel = (int)Mathf.Floor(glyphPt * scale / 7f + 0.001f);
            if (texel < 1) texel = 1;
            if (body != null)
            {
                body.Glyphs = Glyphs;
                body.Atlas = Atlas;
                body.FontPx = texel * 7f;
                float textPx = frame.TextW * scale;
                if (frame.Scroll && textPx > 20f) textPx -= 12f * scale;
                body.LayoutWidth = textPx;
                body.style.width = textPx;
                body.style.maxWidth = textPx;
                body.SetLine(model.Shown, model.Caret);
                if (Atlas != null)
                {
                    Atlas.filterMode = FilterMode.Point;
                    Atlas.anisoLevel = 0;
                }
            }
            var clip = box.Q<ScrollView>("guide-body-clip");
            if (clip != null)
            {
                float bodyPx = frame.Body * scale;
                float linePx = texel * 8f;
                if (bodyPx < linePx) bodyPx = linePx;
                clip.style.height = bodyPx;
                clip.style.maxHeight = bodyPx;
                clip.style.flexGrow = 0;
                clip.style.flexShrink = 0;
                clip.verticalScrollerVisibility = frame.Scroll ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
                clip.pickingMode = frame.Scroll ? PickingMode.Position : PickingMode.Ignore;
            }

            var index = box.Q<Label>("guide-index");
            if (index != null) { index.text = model.Index; index.style.fontSize = font; index.style.color = Cream; }
            var hint = box.Q<Label>("guide-hint");
            if (hint != null) { hint.text = model.Hint; hint.style.fontSize = font; hint.style.color = Cream; }

            var skip = box.Q<Button>("guide-skip");
            if (skip != null)
            {
                skip.style.width = frame.Skip * scale;
                skip.style.height = frame.Skip * scale;
                skip.style.minWidth = frame.Skip * scale;
                skip.style.minHeight = frame.Skip * scale;
                skip.style.fontSize = font;
                skip.style.color = Cream;
                skip.style.backgroundColor = new Color(0x2A / 255f, 0x31 / 255f, 0x42 / 255f, 1f);
            }

            if (model.Announce && !string.IsNullOrEmpty(model.Spoken))
                box.tooltip = model.Spoken;
            else if (string.IsNullOrEmpty(box.tooltip) && !string.IsNullOrEmpty(model.Full))
                box.tooltip = model.Name + ". " + model.Full;
        }

        static void PaintChrome(VisualElement box, float scale)
        {
            var shadow = box.Q("guide-shadow");
            if (shadow != null)
            {
                float o = GuidePacing.ShadowPt * scale;
                shadow.style.position = Position.Absolute;
                shadow.style.left = o;
                shadow.style.top = o;
                shadow.style.right = -o;
                shadow.style.bottom = -o;
                shadow.style.backgroundColor = Shadow;
            }
            var panel = box.Q("guide-panel");
            if (panel == null) return;
            float border = GuidePacing.BorderPt * scale;
            float pad = GuidePacing.PadPt * scale;
            panel.style.backgroundColor = Ink;
            panel.style.borderLeftWidth = border;
            panel.style.borderRightWidth = border;
            panel.style.borderTopWidth = border;
            panel.style.borderBottomWidth = border;
            panel.style.borderLeftColor = Cream;
            panel.style.borderRightColor = Cream;
            panel.style.borderTopColor = Cream;
            panel.style.borderBottomColor = Cream;
            panel.style.borderTopLeftRadius = 0;
            panel.style.borderTopRightRadius = 0;
            panel.style.borderBottomLeftRadius = 0;
            panel.style.borderBottomRightRadius = 0;
            panel.style.paddingLeft = pad;
            panel.style.paddingRight = pad;
            panel.style.paddingTop = pad;
            panel.style.paddingBottom = pad;
            panel.style.flexGrow = 1;
            panel.style.overflow = Overflow.Hidden;
            var row = box.Q("guide-row");
            if (row != null)
            {
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.FlexStart;
            }
            var column = box.Q("guide-column");
            if (column != null)
            {
                column.style.flexGrow = 1;
                column.style.flexShrink = 1;
                column.style.flexDirection = FlexDirection.Column;
                column.style.marginLeft = 6f * scale;
                column.style.marginRight = (GuidePacing.SkipMinPt + GuidePacing.ColumnGapPt) * scale;
                column.style.overflow = Overflow.Visible;
            }
            var foot = box.Q("guide-foot");
            if (foot != null)
            {
                foot.style.flexDirection = FlexDirection.Row;
                foot.style.alignItems = Align.Center;
                foot.style.flexShrink = 0;
                foot.style.marginTop = 4f * scale;
            }
            var nameLabel = box.Q("guide-name");
            if (nameLabel != null)
            {
                nameLabel.style.flexShrink = 0;
                nameLabel.style.overflow = Overflow.Hidden;
                nameLabel.style.whiteSpace = WhiteSpace.NoWrap;
                nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            }
            var portrait = box.Q("guide-portrait");
            if (portrait != null)
            {
                portrait.style.marginRight = 0;
                portrait.style.marginLeft = 0;
                portrait.style.borderLeftWidth = 2f * scale;
                portrait.style.borderRightWidth = 2f * scale;
                portrait.style.borderTopWidth = 2f * scale;
                portrait.style.borderBottomWidth = 2f * scale;
                portrait.style.borderLeftColor = Amber;
                portrait.style.borderRightColor = Amber;
                portrait.style.borderTopColor = Amber;
                portrait.style.borderBottomColor = Amber;
                portrait.style.borderTopLeftRadius = 0;
                portrait.style.borderTopRightRadius = 0;
                portrait.style.borderBottomLeftRadius = 0;
                portrait.style.borderBottomRightRadius = 0;
            }
            var skip = box.Q("guide-skip");
            if (skip != null)
            {
                skip.style.position = Position.Absolute;
                skip.style.right = pad;
                skip.style.bottom = pad;
                skip.style.borderLeftWidth = 2f * scale;
                skip.style.borderRightWidth = 2f * scale;
                skip.style.borderTopWidth = 2f * scale;
                skip.style.borderBottomWidth = 2f * scale;
                skip.style.borderLeftColor = Amber;
                skip.style.borderRightColor = Amber;
                skip.style.borderTopColor = Amber;
                skip.style.borderBottomColor = Amber;
                skip.style.borderTopLeftRadius = 0;
                skip.style.borderTopRightRadius = 0;
                skip.style.borderBottomLeftRadius = 0;
                skip.style.borderBottomRightRadius = 0;
            }
        }
    }
}
