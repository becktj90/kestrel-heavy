// Escape and the hardware back key. A settings sheet that is open closes first.
// Otherwise the visible screen's back control runs. Buttons keep that Action in
// userData: Clickable's callback list is not readable from outside the button.
using System;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class MenuBack
    {
        static readonly string[] Leaves =
        {
            "patches-back",
            "codex-back",
            "mission-home",
            "briefing-back",
            "report-home",
            "missions-home",
        };

        /// <summary>True when something closed or a screen left.</summary>
        public static bool Try(VisualElement root)
        {
            if (root == null) return false;
            var sheet = root.Q("home-settings-sheet");
            if (SheetOpen(sheet))
            {
                sheet.style.display = DisplayStyle.None;
                return true;
            }
            for (int i = 0; i < Leaves.Length; i++)
            {
                var button = root.Q<Button>(Leaves[i]);
                if (button == null || Hidden(button)) continue;
                if (button.userData is Action leave)
                {
                    leave();
                    return true;
                }
            }
            return false;
        }

        static bool SheetOpen(VisualElement sheet) => sheet != null && !Hidden(sheet);

        /// <summary>
        /// Inline display wins. An unset inline value is not "none": resolved style stays None
        /// until a panel lays the element out, and that would skip a back control built off-panel.
        /// </summary>
        static bool Hidden(VisualElement el)
        {
            var inline = el.style.display;
            if (inline.keyword != StyleKeyword.Undefined && inline.keyword != StyleKeyword.Null && inline.keyword != StyleKeyword.Auto)
                return inline.value == DisplayStyle.None;
            return el.panel != null && el.resolvedStyle.display == DisplayStyle.None;
        }
    }
}
