// Designer Launch used to add the hidden class to #design and stop there.
// On the web the readout column (web-readouts) is a sibling of #design, so hiding
// the sheet leaves that column on screen. On device an inline display on the
// designer wins over the stylesheet class, so the player stays on the designer.
// Show sets display:none on every designer surface and brings the flight HUD up.
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class FlightEntry
    {
        public const string WebHost = "web-readouts";

        public static void Show(VisualElement ui)
        {
            if (ui == null) return;
            Hide(ui.Q("design"));
            Hide(ui.Q("designer-v3"));
            Hide(ui.Q(WebHost));
            Hide(ui.Q("home"));
            Hide(ui.Q("issue-chip"));
            var hud = ui.Q("hud");
            if (hud == null) return;
            hud.RemoveFromClassList("hidden");
            hud.style.display = DisplayStyle.Flex;
            hud.style.position = Position.Absolute;
            hud.style.left = 0;
            hud.style.right = 0;
            hud.style.top = 0;
            hud.style.bottom = 0;
            hud.BringToFront();
        }

        /// <summary>
        /// Back on the pad. Clears the inline hides so the sheet can open again.
        /// The issue chip keeps its hidden class; validation shows it.
        /// The web readout host is left alone — WebDesignChrome owns that display.
        /// </summary>
        public static void Release(VisualElement ui)
        {
            if (ui == null) return;
            Unhide(ui.Q("design"));
            Unhide(ui.Q("designer-v3"));
            Unhide(ui.Q("home"));
            var chip = ui.Q("issue-chip");
            if (chip != null) chip.style.display = StyleKeyword.Null;
            var hud = ui.Q("hud");
            if (hud == null) return;
            hud.AddToClassList("hidden");
            hud.style.display = StyleKeyword.Null;
            hud.style.position = StyleKeyword.Null;
            hud.style.left = StyleKeyword.Null;
            hud.style.right = StyleKeyword.Null;
            hud.style.top = StyleKeyword.Null;
            hud.style.bottom = StyleKeyword.Null;
        }

        static void Hide(VisualElement e)
        {
            if (e == null) return;
            e.AddToClassList("hidden");
            e.style.display = DisplayStyle.None;
        }

        static void Unhide(VisualElement e)
        {
            if (e == null) return;
            e.RemoveFromClassList("hidden");
            e.style.display = StyleKeyword.Null;
        }
    }
}
