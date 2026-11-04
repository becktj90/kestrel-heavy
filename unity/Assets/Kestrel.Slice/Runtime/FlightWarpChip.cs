// PR5 coast warp chip (UX review on #436): its own small fixed-width label inside the mission clock, right after the
// reading. The clock face always reserves its width (FlightHudLayout.WarpChipReserve), and the chip shows and hides by
// visibility, instantly and without a pulse, so nothing reflows.
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
            if (clock == null) return null;
            var chip = clock.Q<Label>(Name);
            if (chip != null) return chip;
            chip = new Label { name = Name, pickingMode = PickingMode.Ignore };
            chip.style.position = Position.Absolute;
            chip.style.right = 0;
            chip.style.top = 0;
            chip.style.bottom = 0;
            chip.style.unityTextAlign = TextAnchor.MiddleCenter;
            chip.style.whiteSpace = WhiteSpace.NoWrap;
            chip.style.visibility = Visibility.Hidden;
            clock.Add(chip);
            return chip;
        }

        public static void Show(Label chip, double factor, float unitsPerPoint)
        {
            if (chip == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            chip.style.width = FlightHudLayout.WarpChipWidth() * u;
            chip.style.fontSize = FlightHudLayout.WarpChipFontPt * u;
            bool on = factor > CoastWarp.Off;
            chip.text = on ? CoastWarp.ChipText(factor) : CoastWarp.ChipText(CoastWarp.Fast);
            // Accessible text: Unity UI Toolkit has no screen-reader label; the tooltip carries it (as the HUD buttons do).
            chip.tooltip = on ? CoastWarp.AccessibleText(factor) : "";
            chip.style.visibility = on ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
