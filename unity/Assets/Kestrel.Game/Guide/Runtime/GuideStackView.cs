using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide
{
    /// <summary>Legacy subtitle stack, still built by view tests. The opening introduction uses it too.
    /// Ink panel, cream border, hard shadow.
    /// Ascent docks under the header and stays out of the thumb zone. Menu screens dock one box
    /// above the Home key and leave the mission list the space under it. Empty space does not pick,
    /// so buttons underneath still receive touches.
    /// A drag scrolls a tall page and turns a short one. SKIP on the introduction closes it.</summary>
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
        /// <summary>The host parks this stack. Sync places it when the boxes change, then leaves the edges alone
        /// so a press is not cancelled by rewriting the same position every frame.</summary>
        public bool HostParksPosition;

        readonly VisualElement stack;
        string signature = "";

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
            Stage.Tick(dt);
            Sync();
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
            bool place = !HostParksPosition;
            for (int i = 0; i < shown; i++) next += models[i].Id + "|";
            next += Screen.Compact ? "c" : "f";
            next += Stage.Place == GuidePlace.Menu ? "m" : "d";
            if (next != signature || stack.childCount != shown)
            {
                place = true;
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
            float left = frames[0].X * scale;
            float top = frames[0].Y * scale;
            float width = frames[0].W * scale;
            if (left < 0f) left = 0f;
            if (top < 0f) top = 0f;
            var host = stack.parent;
            float hostW = host != null ? host.resolvedStyle.width : 0f;
            if (hostW > 1f && left + width > hostW)
            {
                width = Mathf.Max(80f, hostW - left);
                if (left + width > hostW) { left = 0f; width = hostW; }
            }
            if (place)
            {
                stack.style.left = left;
                stack.style.top = top;
                stack.style.width = width;
            }
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
            var gesture = new BoxGesture();
            box.userData = gesture;
            box.RegisterCallback<ClickEvent>(_ =>
            {
                // A drag already chose scroll, next, or previous. A following click must not turn the page.
                if (gesture.Dragged || gesture.Handled) return;
                Apply(gesture, captured, GuideScrollAction.Tap);
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
            body.style.flexGrow = 0;
            body.style.position = Position.Relative;
            // A plain clip, not a ScrollView. The ScrollView's viewport sat on top of the text and the
            // SKIP button and ate the pointer, so a drag did not move and a tap did not turn the page.
            var clip = new VisualElement { name = "guide-body-clip", pickingMode = PickingMode.Ignore };
            clip.AddToClassList("guide-body-clip");
            clip.style.overflow = Overflow.Hidden;
            clip.Add(body);
            column.Add(clip);
            gesture.Clip = clip;
            gesture.Body = body;
            BindGesture(box, gesture, captured);

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
            skip.pickingMode = PickingMode.Position;
            skip.AddToClassList("guide-skip");
            BindSkip(skip, captured);
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
            box.style.overflow = Overflow.Hidden;
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
                // Wrap inside the column. A nowrap name ("MABEL THRUM · HANGAR KEEPER") draws past the
                // box and onto the Home key. Extending it over SKIP made the glyphs wider than the column.
                name.style.whiteSpace = WhiteSpace.Normal;
                name.style.flexShrink = 1;
                name.style.width = Length.Percent(100);
                name.style.marginRight = 0f;
                name.style.overflow = Overflow.Hidden;
                name.style.letterSpacing = 0f;
                float nameW = frame.TextW * scale;
                if (nameW > 20f)
                {
                    // The check measures the whole string, not the ellipsis. MeasureTextSize reports the
                    // already-constrained box, so size the face from the character count instead.
                    float fit = Mathf.Max(48f, nameW - 18f);
                    float natural = name.text.Length * font * 0.85f;
                    if (natural > fit && natural > 1f) name.style.fontSize = font * fit / natural;
                    name.style.width = fit;
                    name.style.maxWidth = fit;
                }
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
            var clip = box.Q("guide-body-clip");
            var gesture = box.userData as BoxGesture;
            if (clip != null)
            {
                float bodyPx = frame.Body * scale;
                float linePx = texel * 8f;
                if (bodyPx < linePx) bodyPx = linePx;
                float textH = body != null ? body.TextHeight : linePx;
                clip.style.height = bodyPx;
                clip.style.maxHeight = bodyPx;
                clip.style.minHeight = linePx;
                clip.style.flexGrow = 0;
                clip.style.flexShrink = 0;
                clip.style.overflow = Overflow.Hidden;
                clip.pickingMode = PickingMode.Ignore;
                if (gesture != null)
                {
                    string key = model.Id + ":" + model.Page;
                    if (gesture.Key != key)
                    {
                        gesture.Key = key;
                        gesture.Offset = 0f;
                    }
                    float range = textH - bodyPx;
                    if (range < 0f) range = 0f;
                    if (gesture.Offset > range) gesture.Offset = range;
                    if (body != null) body.style.top = -gesture.Offset;
                }
            }

            var index = box.Q<Label>("guide-index");
            if (index != null) { index.text = model.Index; index.style.fontSize = font; index.style.color = Cream; }
            var hint = box.Q<Label>("guide-hint");
            if (hint != null) { hint.text = model.Hint; hint.style.fontSize = font; hint.style.color = Cream; }

            var skip = box.Q<Button>("guide-skip");
            if (skip != null)
            {
                skip.tooltip = OpeningIntroSequence.IsWelcome(model.Id)
                    ? "Skip. Closes the introduction."
                    : "Skip. Skips this beat. Press and hold to skip tips for this step.";
                skip.BringToFront();
                float skipPx = frame.Skip * scale;
                float boxH = frame.H * scale;
                if (boxH > 4f && skipPx > boxH - 4f) skipPx = boxH - 4f;
                if (skipPx < 8f) skipPx = 8f;
                float skipFont = frame.Skip * scale > 1f ? font * skipPx / (frame.Skip * scale) : font;
                skip.style.width = skipPx;
                skip.style.height = skipPx;
                skip.style.minWidth = skipPx;
                skip.style.minHeight = skipPx;
                skip.style.fontSize = skipFont;
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
                nameLabel.style.flexShrink = 1;
                nameLabel.style.overflow = Overflow.Hidden;
                nameLabel.style.whiteSpace = WhiteSpace.Normal;
                nameLabel.style.width = Length.Percent(100);
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
                skip.pickingMode = PickingMode.Position;
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

        void BindGesture(VisualElement box, BoxGesture gesture, int index)
        {
            box.RegisterCallback<PointerDownEvent>(e =>
            {
                if (IsSkip(e.target as VisualElement)) return;
                gesture.Pointer = e.pointerId;
                gesture.Start = e.position;
                gesture.StartScroll = gesture.Offset;
                gesture.Dragged = false;
                gesture.Handled = false;
                box.CapturePointer(e.pointerId);
            });
            box.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pointerId != gesture.Pointer) return;
                float dy = e.position.y - gesture.Start.y;
                float dx = e.position.x - gesture.Start.x;
                if (!gesture.Dragged && Mathf.Abs(dy) < GuideScroll.TapSlop && Mathf.Abs(dx) < GuideScroll.TapSlop) return;
                gesture.Dragged = true;
                gesture.Offset = GuideScroll.Offset(gesture.StartScroll, dy, Range(gesture));
                PlaceBody(gesture);
            });
            box.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerId != gesture.Pointer) return;
                box.ReleasePointer(e.pointerId);
                float dy = e.position.y - gesture.Start.y;
                var action = GuideScroll.Release(dy, gesture.StartScroll, Range(gesture));
                gesture.Pointer = -1;
                Apply(gesture, index, action);
            });
            box.RegisterCallback<PointerCancelEvent>(e =>
            {
                if (e.pointerId != gesture.Pointer) return;
                gesture.Pointer = -1;
                box.ReleasePointer(e.pointerId);
            });
            box.RegisterCallback<WheelEvent>(e =>
            {
                float range = Range(gesture);
                var action = GuideScroll.Wheel(e.delta.y, gesture.Offset, range);
                if (action == GuideScrollAction.Scroll)
                {
                    gesture.Offset = GuideScroll.Offset(gesture.Offset, -e.delta.y, range);
                    PlaceBody(gesture);
                }
                else Apply(gesture, index, action);
                e.StopPropagation();
            });
        }

        void Apply(BoxGesture gesture, int index, GuideScrollAction action)
        {
            if (gesture.Handled || Stage == null) return;
            gesture.Handled = true;
            if (action == GuideScrollAction.Next) Stage.Next(index);
            else if (action == GuideScrollAction.Previous) Stage.Back(index);
            else if (action == GuideScrollAction.Tap) Stage.Tap(index);
            else return;
            Sync();
            // The page turn rebuilds the box. A click still in this gesture must not turn the new one.
            if (index >= 0 && index < stack.childCount && stack[index].userData is BoxGesture fresh)
                fresh.Handled = true;
        }

        void BindSkip(Button skip, int index)
        {
            var press = new SkipPress();
            skip.userData = press;
            void Commit(bool tips)
            {
                if (press.Done || Stage == null) return;
                press.Done = true;
                press.Down = false;
                var models = Stage.Models();
                bool welcome = index >= 0 && index < models.Length && models[index] != null
                    && OpeningIntroSequence.IsWelcome(models[index].Id);
                if (welcome || !tips) Stage.Skip(index);
                else Stage.SkipStepTips(index);
                Sync();
                // Skip rebuilds the box under the finger. The new button must ignore the rest of this press.
                SwallowFreshSkip();
            }
            skip.RegisterCallback<PointerDownEvent>(e =>
            {
                press.Down = true;
                press.Done = false;
                press.DownAt = Time.unscaledTime;
                int token = ++press.Token;
                if (skip.panel != null)
                {
                    skip.schedule.Execute(() =>
                    {
                        if (!press.Down || press.Done || token != press.Token) return;
                        Commit(true);
                    }).StartingIn((long)(GuidePacing.LongPressSeconds * 1000f));
                }
                e.StopPropagation();
            });
            skip.RegisterCallback<PointerUpEvent>(e =>
            {
                e.StopPropagation();
                if (!press.Down) return;
                Commit(Time.unscaledTime - press.DownAt >= GuidePacing.LongPressSeconds);
            });
            // The press used to be thrown away when the pointer was cancelled, so SKIP did nothing.
            skip.RegisterCallback<PointerCancelEvent>(e =>
            {
                e.StopPropagation();
                if (!press.Down) return;
                Commit(false);
            });
            skip.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                if (!press.Down) return;
                Commit(false);
            });
        }

        void SwallowFreshSkip()
        {
            SwallowFreshSkip(stack);
        }

        static void SwallowFreshSkip(VisualElement ve)
        {
            if (ve == null) return;
            if (ve.name == "guide-skip" && ve.userData is SkipPress press) press.Done = true;
            for (int i = 0; i < ve.childCount; i++) SwallowFreshSkip(ve[i]);
        }

        static float Range(BoxGesture gesture)
        {
            if (gesture == null || gesture.Body == null || gesture.Clip == null) return 0f;
            float view = gesture.Clip.resolvedStyle.height;
            if (float.IsNaN(view) || view < 1f)
            {
                var styled = gesture.Clip.style.height;
                view = styled.keyword == StyleKeyword.Undefined ? styled.value.value : 0f;
            }
            float range = gesture.Body.TextHeight - view;
            return range > 0f ? range : 0f;
        }

        static void PlaceBody(BoxGesture gesture)
        {
            if (gesture == null || gesture.Body == null) return;
            gesture.Body.style.top = -gesture.Offset;
        }

        static bool IsSkip(VisualElement ve)
        {
            for (var p = ve; p != null; p = p.parent)
                if (p.name == "guide-skip") return true;
            return false;
        }

        sealed class BoxGesture
        {
            public VisualElement Clip;
            public BitmapTextElement Body;
            public string Key = "";
            public float Offset;
            public float StartScroll;
            public Vector2 Start;
            public int Pointer = -1;
            public bool Dragged;
            public bool Handled;
        }

        sealed class SkipPress
        {
            public bool Down;
            public bool Done;
            public float DownAt;
            public int Token;
        }
    }
}
