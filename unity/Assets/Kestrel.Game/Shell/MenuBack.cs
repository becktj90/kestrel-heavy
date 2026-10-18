// Escape and the hardware back key. A settings sheet that is open closes first.
// Otherwise the visible screen's back control runs. Overlay leaves (briefing, report)
// come before the screen they cover. Buttons keep that Action in userData:
// Clickable's callback list is not readable from outside the button.
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class MenuBack
    {
        /// <summary>Set when <see cref="Try"/> handles Escape, so a later Update the same frame does not handle it again.</summary>
        public static bool Claimed;

        /// <summary>
        /// Flight pause lives with the flight HUD. The shell is gone once Slice loads, so the web
        /// Escape bridge calls this when no menu is up. The flight agent sets it. This does not draw pause UI.
        /// </summary>
        public static Action FlightEscape;

        static readonly string[] Leaves =
        {
            "briefing-back",
            "report-home",
            "patches-back",
            "codex-back",
            "missions-home",
            "mission-home",
            "stepper-back",
            "home",
        };

        /// <summary>True when something closed or a screen left.</summary>
        public static bool Try(VisualElement root)
        {
            if (root == null) return false;
            if (CloseIfOpen(root, "home-thrum-note")) return true;
            if (CloseIfOpen(root, "home-codex-soon")) return true;
            if (CloseIfOpen(root, "home-patches-note")) return true;
            if (CloseIfOpen(root, "mission-payload-note")) return true;
            var sheet = root.Q("home-settings-sheet");
            if (SheetOpen(sheet))
            {
                sheet.style.display = DisplayStyle.None;
                Claimed = true;
                return true;
            }
            for (int i = 0; i < Leaves.Length; i++)
            {
                List<Button> buttons = root.Query<Button>(Leaves[i]).ToList();
                for (int b = 0; b < buttons.Count; b++)
                {
                    var button = buttons[b];
                    if (button == null || Hidden(button) || !button.enabledSelf) continue;
                    if (button.userData is Action leave)
                    {
                        leave();
                        Claimed = true;
                        return true;
                    }
                }
            }
            return false;
        }

        static bool CloseIfOpen(VisualElement root, string name)
        {
            var note = root.Q(name);
            if (!SheetOpen(note)) return false;
            note.style.display = DisplayStyle.None;
            Claimed = true;
            return true;
        }

        static bool SheetOpen(VisualElement sheet) => sheet != null && !Hidden(sheet);

        /// <summary>
        /// Inline display wins, including an ancestor the shell hides while Design is up.
        /// An unset inline value is not "none": resolved style stays None until a panel lays
        /// the element out, and that would skip a back control built off-panel.
        /// </summary>
        static bool Hidden(VisualElement el)
        {
            for (var node = el; node != null; node = node.parent)
            {
                var inline = node.style.display;
                if (inline.keyword != StyleKeyword.Undefined && inline.keyword != StyleKeyword.Null && inline.keyword != StyleKeyword.Auto)
                {
                    if (inline.value == DisplayStyle.None) return true;
                    continue;
                }
                if (node.panel != null && node.resolvedStyle.display == DisplayStyle.None) return true;
            }
            return false;
        }
    }
}
