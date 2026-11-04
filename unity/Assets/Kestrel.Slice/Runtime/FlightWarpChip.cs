// PR5 coast warp chip (UX review on #436): its own small fixed-width label right after the mission clock reading. It
// shows and hides by visibility, instantly and without a pulse, so nothing reflows.
//
// The chip is the clock's sibling, absolutely placed, never the clock's child. A UI Toolkit Label that gets a child
// loses its text measure (Yoga measures only leaf nodes), so the content-sized portrait clock laid out at 0x0 and its
// face ran past its box and over the stage chip (CI PlayModeDevice on cecc301c/6c1d9f17: "h-t text past its content
// 0x0", "overlap h-t on h-stage" at every phone size).
//   Wide (clock pinned to its FlightHudLayout box): that box already holds FlightHudLayout.WarpChipReserve at its
//   right end and the face is fitted to the rest (ClockFitWithChip), so the chip sits flush with the box's right edge.
//   Portrait (clock content-sized in the header row): the chip sits one gap past the clock's right edge, out of flow.
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class FlightWarpChip
    {
        public const string Name = "h-warp";

        public static Label Build(Label clock)
        {
            var row = clock != null ? clock.parent : null;
            if (row == null) return null;
            var chip = row.Q<Label>(Name);
            if (chip != null) return chip;
            chip = new Label { name = Name, pickingMode = PickingMode.Ignore };
            chip.style.position = Position.Absolute;
            chip.style.unityTextAlign = TextAnchor.MiddleCenter;
            chip.style.whiteSpace = WhiteSpace.NoWrap;
            chip.style.marginLeft = 0; chip.style.marginRight = 0; chip.style.marginTop = 0; chip.style.marginBottom = 0;
            chip.style.visibility = Visibility.Hidden;
            row.Insert(row.IndexOf(clock) + 1, chip);
            clock.RegisterCallback<GeometryChangedEvent>(_ => Place(chip, clock, chip.resolvedStyle.width));
            return chip;
        }

        /// <summary>Next to the clock (see the header): flush right inside a pinned wide box, one gap past a portrait one.</summary>
        static void Place(Label chip, Label clock, float chipW)
        {
            if (chip == null || clock == null || chip.parent != clock.parent) return;
            var c = clock.layout;
            if (float.IsNaN(c.width) || float.IsNaN(chipW)) return;
            // AscentHud.Pin sets the wide clock absolute inline; Unpin resets it to StyleKeyword.Null (portrait, in flow).
            var pos = clock.style.position;
            bool pinned = pos.keyword == StyleKeyword.Undefined && pos.value == Position.Absolute;
            float u = chipW / Mathf.Max(1f, FlightHudLayout.WarpChipWidth());
            chip.style.left = pinned ? c.xMax - chipW : c.xMax + FlightHudLayout.GapPt * u;
            chip.style.top = c.y;
            chip.style.height = c.height;
        }

        public static void Show(Label chip, Label clock, double factor, float unitsPerPoint)
        {
            if (chip == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            float w = FlightHudLayout.WarpChipWidth() * u;
            chip.style.width = w;
            chip.style.fontSize = FlightHudLayout.WarpChipFontPt * u;
            Place(chip, clock, w);
            bool on = factor > CoastWarp.Off;
            chip.text = on ? CoastWarp.ChipText(factor) : CoastWarp.ChipText(CoastWarp.Fast);
            // Accessible text: Unity UI Toolkit has no screen-reader label; the tooltip carries it (as the HUD buttons do).
            chip.tooltip = on ? CoastWarp.AccessibleText(factor) : "";
            chip.style.visibility = on ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
