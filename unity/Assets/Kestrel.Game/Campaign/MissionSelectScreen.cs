// Portrait Mission Select. The campaign map replaces the flat KH-1..KH-10 list.
// Reads the local career save. Does not open a network socket and does not start a locked flight.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Game.Guide;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.MissionSelect
{
    public static class MissionSelectScreen
    {
        public const string RootName = "mission-map";
        public const string SheetResource = "UI/Campaign";

        public static VisualElement Build(Campaign campaign, Action<string> startMission, Action home)
        {
            string json = ReadCampaignText();
            var labels = MissionMapText.ReadPayloadLabels(json);
            long minor = MissionMapText.MinorPerKc(json);
            MissionSnapshot snap;
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "saves");
                snap = MissionProgress.ReadSave(campaign, dir, minor);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] career unreadable, showing a fresh board: " + e.Message);
                snap = campaign == null ? new MissionSnapshot() : MissionProgress.Fresh(campaign, minor);
            }
            return Build(campaign, snap.Career, snap.Records, labels, startMission, home);
        }

        public static VisualElement Build(Campaign campaign, Career career, IReadOnlyDictionary<string, MissionRecord> records, IReadOnlyDictionary<string, PayloadLabel> labels, Action<string> startMission, Action home)
        {
            var map = MissionMapBuilder.From(campaign, career, records, labels);
            var root = new VisualElement { name = RootName };
            root.AddToClassList("kh-screen");
            root.AddToClassList("mission-map");
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Column;
            root.style.justifyContent = Justify.FlexStart;
            var sheet = Resources.Load<StyleSheet>(SheetResource);
            if (sheet != null) root.styleSheets.Add(sheet);

            var title = new Label(MissionMapCopy.Title) { name = "missions-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("kh-title");
            title.style.flexShrink = 0;
            root.Add(title);

            if (map.Empty)
            {
                var empty = new Label(map.Note) { name = "missions-empty", pickingMode = PickingMode.Ignore };
                empty.AddToClassList("kh-caption");
                empty.style.whiteSpace = WhiteSpace.Normal;
                root.Add(empty);
            }
            else
            {
                var note = new Label(map.Note) { name = "missions-note", pickingMode = PickingMode.Ignore };
                note.AddToClassList("kh-caption");
                note.style.whiteSpace = WhiteSpace.Normal;
                note.style.flexShrink = 0;
                root.Add(note);
            }

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "mission-scroll" };
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1;
            for (int a = 0; a < map.Acts.Count; a++) scroll.Add(Act(map.Acts[a], startMission));
            if (map.ShowOpenContracts) scroll.Add(Contracts());
            root.Add(scroll);
            GuideHooks.MissionSelect(GuideSession.Engine, map.Next != null ? map.Next.MissionId : null, map.ShowOpenContracts);

            var homeKey = new Button(home) { name = "missions-home", text = MissionMapCopy.Home, tooltip = MissionMapCopy.HomeTip };
            homeKey.AddToClassList("kh-button");
            homeKey.AddToClassList("mission-home");
            homeKey.style.minHeight = 44;
            homeKey.style.flexShrink = 0;
            root.Add(homeKey);
            return root;
        }

        static VisualElement Act(MissionAct act, Action<string> startMission)
        {
            var block = new VisualElement { name = act.Id };
            block.AddToClassList("mission-act");
            var heading = new Label(act.Title) { name = act.Id + "-title", pickingMode = PickingMode.Ignore };
            heading.AddToClassList("mission-act-title");
            heading.style.whiteSpace = WhiteSpace.Normal;
            block.Add(heading);
            for (int i = 0; i < act.Entries.Count; i++) block.Add(Row(act.Entries[i], startMission));
            return block;
        }

        static Button Row(MissionMapEntry entry, Action<string> startMission)
        {
            bool playable = entry.Access != MissionAccess.Locked && !string.IsNullOrEmpty(entry.MissionId);
            string id = entry.MissionId;
            Action click = null;
            if (playable) click = () => startMission?.Invoke(id);
            var row = new Button(click) { name = entry.ElementName, tooltip = entry.Tooltip };
            row.text = string.Empty;
            row.AddToClassList("kh-button");
            row.AddToClassList("mission-row");
            if (entry.Access == MissionAccess.Locked) row.AddToClassList("mission-row--locked");
            else if (entry.Access == MissionAccess.Cleared) row.AddToClassList("mission-row--cleared");
            else row.AddToClassList("mission-row--next");
            row.style.minHeight = 44;
            row.style.flexShrink = 0;
            row.style.flexDirection = FlexDirection.Column;
            row.style.alignItems = Align.Stretch;
            row.style.whiteSpace = WhiteSpace.Normal;
            if (!playable) row.SetEnabled(false);

            var head = new VisualElement { name = entry.ElementName + "-head", pickingMode = PickingMode.Ignore };
            head.AddToClassList("mission-row-head");
            head.style.flexDirection = FlexDirection.Row;
            head.style.flexWrap = Wrap.Wrap;
            head.style.alignItems = Align.Center;
            head.style.alignSelf = Align.Stretch;
            head.Add(Line(entry.ElementName + "-name", entry.Title, "mission-row-name", grow: true));
            if (entry.IsNext) head.Add(Line(entry.ElementName + "-next", MissionMapCopy.NextChip, "mission-chip", grow: false));
            if (entry.StarsText != null) head.Add(Line(entry.ElementName + "-stars", entry.StarsText, "mission-stars", grow: false));
            row.Add(head);

            if (!string.IsNullOrEmpty(entry.PayloadLine)) row.Add(Line(entry.ElementName + "-payload", entry.PayloadLine, "mission-meta", grow: false));
            if (!string.IsNullOrEmpty(entry.OrbitLine)) row.Add(Line(entry.ElementName + "-orbit", entry.OrbitLine, "mission-meta", grow: false));
            if (entry.MedalsText != null) row.Add(Line(entry.ElementName + "-medals", entry.MedalsText, "mission-meta", grow: false));
            if (entry.PartsLine != null) row.Add(Line(entry.ElementName + "-parts", entry.PartsLine, "mission-meta", grow: false));
            if (entry.LockLine != null) row.Add(Line(entry.ElementName + "-lock", entry.LockLine, "mission-lock", grow: false));
            return row;
        }

        static Label Line(string name, string text, string cls, bool grow)
        {
            var line = new Label(text) { name = name, pickingMode = PickingMode.Ignore };
            line.AddToClassList(cls);
            line.style.whiteSpace = WhiteSpace.Normal;
            if (grow) line.style.flexGrow = 1;
            else line.style.flexShrink = 0;
            return line;
        }

        static VisualElement Contracts()
        {
            var panel = new VisualElement { name = "open-contracts", tooltip = MissionMapCopy.ContractsBody };
            panel.AddToClassList("open-contracts");
            panel.style.minHeight = 44;
            panel.style.flexShrink = 0;
            var title = new Label(MissionMapCopy.ContractsTitle) { name = "open-contracts-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("open-contracts-title");
            title.style.whiteSpace = WhiteSpace.Normal;
            var body = new Label(MissionMapCopy.ContractsBody) { name = "open-contracts-body", pickingMode = PickingMode.Ignore };
            body.AddToClassList("open-contracts-body");
            body.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(title);
            panel.Add(body);
            return panel;
        }

        static string ReadCampaignText()
        {
            try
            {
                string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
                string path = Path.Combine(root, "data", "campaign.json");
                if (File.Exists(path)) return File.ReadAllText(path);
            }
            catch (Exception) { }
            var asset = Resources.Load<TextAsset>("KestrelData/campaign");
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text)) return asset.text;
            return null;
        }
    }
}
