// Pads a UI Toolkit root to the VISUAL_SPEC content rect (Screen.safeArea + 8 pt sides) so HUD and
// controls never sit under the notch/Dynamic Island/home indicator, while the background (themed
// fill + world) still runs edge to edge.
//
// Insets are fractions of the screen mapped onto the panel's layout rect. That matches
// Scale-With-Screen-Size when the panel's display is the screen, and stays correct when the panel
// is rendered into a texture of a different size (PlayMode captures). Dividing raw screen pixels by
// the panel scale pins the safe rect to a corner of that texture.
using Kestrel.Game.Layout;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class SafeAreaRoot
    {
        public const string SafeClass = "kh-safe";

        public static void Apply(VisualElement safe, PortraitFrame f)
        {
            var panel = safe.panel;
            if (panel == null) return;
            var root = panel.visualTree.layout;
            if (root.width <= 0 || root.height <= 0 || Screen.width <= 0 || Screen.height <= 0) return;
            var insets = PortraitLayout.MapContentToPanel(Screen.width, Screen.height, f.Content, root.width, root.height);
            safe.style.position = Position.Absolute;
            safe.style.left = (float)insets.Left;
            safe.style.top = (float)insets.Top;
            safe.style.right = (float)insets.Right;
            safe.style.bottom = (float)insets.Bottom;
        }
    }
}
