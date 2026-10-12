// Pads a UI Toolkit root to the VISUAL_SPEC content rect (Screen.safeArea + 8 pt sides) so HUD and
// controls never sit under the notch/Dynamic Island/home indicator, while the background (themed
// fill + world) still runs edge to edge.
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
            // Screen px (bottom-left origin) → panel coords (top-left origin).
            Vector2 tl = RuntimePanelUtils.ScreenToPanel(panel, new Vector2((float)f.Content.X, Screen.height - (float)f.Content.YMax));
            Vector2 br = RuntimePanelUtils.ScreenToPanel(panel, new Vector2((float)f.Content.XMax, Screen.height - (float)f.Content.Y));
            var root = panel.visualTree.layout;
            safe.style.position = Position.Absolute;
            safe.style.left = tl.x;
            safe.style.top = tl.y;
            safe.style.right = Mathf.Max(0, root.width - br.x);
            safe.style.bottom = Mathf.Max(0, root.height - br.y);
        }
    }
}
