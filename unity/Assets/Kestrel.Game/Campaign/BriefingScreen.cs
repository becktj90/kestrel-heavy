// Portrait briefing overlay. Sits on the mission map. Build it and Back use the G1 routes.
// Numbers are the campaign row and the seeded S4 card. Offline. No timer and no price.
using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Game.Guide;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.MissionSelect
{
    public static class BriefingScreen
    {
        public const string RootName = "briefing-card";
        public const string SheetResource = "UI/Briefing";
        public const string BuildText = "Build it";
        public const string PadText = "To the pad";
        public const string BackText = "Back";
        public const string BuildTip = "Build it. Opens the designer with this mission.";
        public const string PadTip = "To the pad. A short rocket can still miss.";
        public const string BackTip = "Back to the mission list.";

        public static VisualElement Build(MissionContext ctx, Action build, Action back)
        {
            var card = TryCard(ctx);
            var board = BriefingBoard.Compose(ctx, card);
            GuideHooks.Briefing(GuideSession.Engine, ctx, card);
            var root = new VisualElement { name = RootName };
            root.AddToClassList("briefing");
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.right = 0;
            root.style.top = 0;
            root.style.bottom = 0;
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Column;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.FlexStart;
            root.style.backgroundColor = new Color(0.118f, 0.180f, 0.282f, 1f);
            var sheet = Resources.Load<StyleSheet>(SheetResource);
            if (sheet != null) root.styleSheets.Add(sheet);

            var column = new VisualElement { name = "briefing-column" };
            column.AddToClassList("briefing-column");
            column.style.flexGrow = 1;
            column.style.flexDirection = FlexDirection.Column;
            column.style.justifyContent = Justify.FlexStart;
            column.style.width = Length.Percent(100);
            column.style.maxWidth = 600;
            column.style.alignSelf = Align.Center;
            root.Add(column);

            column.Add(Line("briefing-title", board.Title, "briefing-title", shrink: true));

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "briefing-scroll" };
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            AddBlock(scroll, "goal", null, board.Goal);
            AddBlock(scroll, "payload", "Payload", board.Payload, board.Fit);
            AddBlock(scroll, "target", "Target", board.Target);
            if (board.Requirements.Count > 0)
            {
                scroll.Add(Head("briefing-requirements-title", "Requirements"));
                for (int i = 0; i < board.Requirements.Count; i++)
                    scroll.Add(Line("briefing-requirement-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), board.Requirements[i], "briefing-body", shrink: false));
            }
            if (board.Gentle.Length > 0) scroll.Add(Line("briefing-gentle", board.Gentle, "briefing-body", shrink: false));
            if (board.Bonuses.Count > 0)
            {
                scroll.Add(Head("briefing-bonus-title", "Bonus"));
                scroll.Add(Line("briefing-bonus-note", "Optional.", "briefing-note", shrink: false));
                for (int i = 0; i < board.Bonuses.Count; i++)
                    scroll.Add(Line("briefing-bonus-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), board.Bonuses[i], "briefing-body", shrink: false));
            }
            if (board.Budget.Length > 0 || board.Reward.Length > 0)
            {
                scroll.Add(Head("briefing-budget-title", "Budget"));
                if (board.Budget.Length > 0) scroll.Add(Line("briefing-budget", board.Budget, "briefing-body", shrink: false));
                if (board.Reward.Length > 0) scroll.Add(Line("briefing-reward", board.Reward, "briefing-body", shrink: false));
            }
            if (board.Wind.Length > 0 || board.Time.Length > 0 || board.Pad.Length > 0 || board.Seed.Length > 0)
            {
                scroll.Add(Head("briefing-launch-title", "Launch day"));
                if (board.Wind.Length > 0) scroll.Add(Line("briefing-wind", board.Wind, "briefing-body", shrink: false));
                if (board.Time.Length > 0) scroll.Add(Line("briefing-time", board.Time, "briefing-body", shrink: false));
                if (board.Pad.Length > 0) scroll.Add(Line("briefing-pad", board.Pad, "briefing-body", shrink: false));
                if (board.Seed.Length > 0) scroll.Add(Line("briefing-seed", board.Seed, "briefing-body", shrink: false));
            }
            if (board.Vehicle.Length > 0) scroll.Add(Line("briefing-vehicle", board.Vehicle, "briefing-body", shrink: false));
            if (board.Reach.Length > 0) scroll.Add(Line("briefing-reach", board.Reach, "briefing-body", shrink: false));
            if (board.Engineer.Length > 0)
            {
                scroll.Add(Head("briefing-engineer-title", "Engineer"));
                scroll.Add(Line("briefing-engineer", board.Engineer, "briefing-engineer", shrink: false));
            }
            column.Add(scroll);

            if (board.CanBuild)
            {
                var go = new Button(build) { name = "briefing-build", text = board.AutoPad ? PadText : BuildText, tooltip = board.AutoPad ? PadTip : BuildTip };
                StyleKey(go, "briefing-build");
                column.Add(go);
            }
            var leave = new Button(back) { name = "briefing-back", text = BackText, tooltip = BackTip };
            StyleKey(leave, "briefing-back");
            column.Add(leave);
            return root;
        }

        static void AddBlock(VisualElement parent, string id, string heading, params string[] lines)
        {
            bool any = false;
            for (int i = 0; i < lines.Length; i++) if (!string.IsNullOrEmpty(lines[i])) any = true;
            if (!any) return;
            if (!string.IsNullOrEmpty(heading)) parent.Add(Head("briefing-" + id + "-title", heading));
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                string name = lines.Length == 1 ? "briefing-" + id : "briefing-" + id + (i == 0 ? "" : "-fit");
                if (id == "payload" && i == 1) name = "briefing-fit";
                if (id == "goal") name = "briefing-goal";
                if (id == "target") name = "briefing-target";
                if (id == "payload" && i == 0) name = "briefing-payload";
                parent.Add(Line(name, lines[i], "briefing-body", shrink: false));
            }
        }

        static Label Head(string name, string text)
        {
            var line = new Label(text) { name = name, pickingMode = PickingMode.Ignore };
            line.AddToClassList("briefing-heading");
            line.style.whiteSpace = WhiteSpace.Normal;
            line.style.flexShrink = 0;
            return line;
        }

        static Label Line(string name, string text, string cls, bool shrink)
        {
            var line = new Label(text) { name = name, pickingMode = PickingMode.Ignore };
            line.AddToClassList(cls);
            line.style.whiteSpace = WhiteSpace.Normal;
            line.style.flexShrink = shrink ? 0 : 1;
            return line;
        }

        static void StyleKey(Button key, string cls)
        {
            key.AddToClassList("kh-button");
            key.AddToClassList(cls);
            key.style.minHeight = 44;
            key.style.flexShrink = 0;
            key.style.whiteSpace = WhiteSpace.Normal;
        }

        static MissionBriefing TryCard(MissionContext ctx)
        {
            if (ctx == null || ctx.IsFree || string.IsNullOrEmpty(ctx.MissionId)) return null;
            string root;
            try { root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory()); }
            catch (Exception) { return null; }
            try
            {
                var mission = Campaign.Load(root).ById(ctx.MissionId);
                if (mission == null) return null;
                return MissionBriefing.Load(root, mission.Index, 0, ctx.Seed);
            }
            catch (Exception) { return null; }
        }
    }
}
