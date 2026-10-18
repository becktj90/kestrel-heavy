// Patch collection. Earned flights stay in color. A medal adds the gold edge.
// Locked flights are the same disc, grey, with an outline. Reachable from the mission list.
using System;
using System.Collections.Generic;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Game.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.MissionSelect
{
    public static class PatchCollectionScreen
    {
        public const string RootName = "patch-collection";
        public const string Title = "Patches";
        public const string BackText = "Back";
        public const string BackTip = "Back to the mission list.";
        public const string Note = "Earned flights stay in color. A gold edge means a medal.";
        public const string LockedLine = "Not flown yet.";
        public const string Footer = "A contract or a free flight keeps one patch. The same flight always matches.";

        public static VisualElement Build(Campaign campaign, Career career, IReadOnlyDictionary<string, MissionRecord> records, Action back)
        {
            int cleared = career == null || career.Cleared < 0 ? 0 : career.Cleared;
            var root = new VisualElement { name = RootName };
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.right = 0;
            root.style.top = 0;
            root.style.bottom = 0;
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = new Color(0.118f, 0.180f, 0.282f, 1f);
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            var title = new Label(Title) { name = "patches-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("kh-title");
            title.style.flexShrink = 0;
            title.style.whiteSpace = WhiteSpace.Normal;
            root.Add(title);

            var note = new Label(Note) { name = "patches-note", pickingMode = PickingMode.Ignore };
            note.AddToClassList("kh-caption");
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.flexShrink = 0;
            root.Add(note);

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "patches-scroll" };
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1;
            var grid = new VisualElement { name = "patch-grid" };
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.Center;
            grid.style.alignItems = Align.FlexStart;
            foreach (var spec in MissionPatchArt.CampaignPatches)
                grid.Add(Cell(spec, cleared, records));
            scroll.Add(grid);

            var footer = new Label(Footer) { name = "patches-footer", pickingMode = PickingMode.Ignore };
            footer.AddToClassList("kh-caption");
            footer.style.whiteSpace = WhiteSpace.Normal;
            footer.style.marginTop = 8;
            scroll.Add(footer);
            root.Add(scroll);

            var leave = new Button(back) { name = "patches-back", text = BackText, tooltip = BackTip };
            leave.AddToClassList("kh-button");
            leave.style.minHeight = 44;
            leave.style.fontSize = 14;
            leave.style.flexShrink = 0;
            leave.style.whiteSpace = WhiteSpace.Normal;
            leave.userData = back;
            root.Add(leave);

            root.RegisterCallback<GeometryChangedEvent>(_ => Reflow(root));
            return root;
        }

        public static PatchChrome ChromeFor(int index, int cleared, MissionRecord record)
        {
            var access = MissionMapBuilder.AccessFor(index, cleared);
            if (access == MissionAccess.Locked) return PatchChrome.Locked;
            if (access == MissionAccess.Cleared && record != null && record.BestMedals.Count > 0)
                return PatchChrome.Medal;
            return PatchChrome.Open;
        }

        static VisualElement Cell(MissionPatchSpec spec, int cleared, IReadOnlyDictionary<string, MissionRecord> records)
        {
            MissionRecord record = null;
            if (records != null && spec.MissionId != null) records.TryGetValue(spec.MissionId, out record);
            var chrome = ChromeFor(spec.Index, cleared, record);
            bool locked = chrome == PatchChrome.Locked;
            var cell = new VisualElement { name = "patch-cell-" + spec.Index.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            cell.style.flexDirection = FlexDirection.Column;
            cell.style.alignItems = Align.Center;
            cell.style.marginBottom = 10;
            cell.style.paddingLeft = 4;
            cell.style.paddingRight = 4;

            float size = 120f;
            var patch = MissionPatch.Render(spec.MissionId, size, chrome);
            patch.name = "patch-kh-" + spec.Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            patch.style.width = size;
            patch.style.height = size;
            patch.userData = new Slot { Id = spec.MissionId, Chrome = chrome, Size = size };
            if (locked) patch.tooltip = spec.Flight + ". " + LockedLine;
            cell.Add(patch);

            cell.Add(Caption("patch-name-" + spec.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), spec.Flight + " · " + spec.Name, bold: true));
            if (locked)
                cell.Add(Caption("patch-lock-" + spec.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), LockedLine, bold: false));
            else
            {
                cell.Add(Caption("patch-motto-" + spec.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), spec.Motto, bold: false));
                cell.Add(Caption("patch-anatomy-" + spec.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), spec.Anatomy, bold: false));
                cell.Add(Caption("patch-act-" + spec.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), spec.ActName, bold: false));
            }
            return cell;
        }

        static Label Caption(string name, string text, bool bold)
        {
            var line = new Label(text) { name = name, pickingMode = PickingMode.Ignore };
            line.AddToClassList(bold ? "kh-subtitle" : "kh-caption");
            line.style.whiteSpace = WhiteSpace.Normal;
            line.style.unityTextAlign = TextAnchor.MiddleCenter;
            line.style.fontSize = 14;
            line.style.marginTop = 2;
            return line;
        }

        static void Reflow(VisualElement root)
        {
            float width = root.contentRect.width;
            if (width < 32f) width = root.worldBound.width;
            if (width < 32f) return;
            int cols = PatchGrid.Columns(width);
            float size = PatchGrid.PatchSize(width);
            float basis = 100f / cols - 1.5f;
            var grid = root.Q("patch-grid");
            if (grid == null) return;
            for (int i = 0; i < grid.childCount; i++)
            {
                var cell = grid[i];
                cell.style.width = Length.Percent(basis);
                var art = cell.Q<MissionPatch>();
                if (art == null || !(art.userData is Slot slot)) continue;
                if (Mathf.Abs(slot.Size - size) < 0.5f) continue;
                slot.Size = size;
                art.style.width = size;
                art.style.height = size;
                art.Set(slot.Id, size, slot.Chrome);
                if (slot.Chrome == PatchChrome.Locked)
                {
                    var spec = MissionPatchArt.Resolve(slot.Id);
                    art.tooltip = spec.Flight + ". " + LockedLine;
                }
            }
        }

        sealed class Slot
        {
            public string Id;
            public PatchChrome Chrome;
            public float Size;
        }
    }
}
