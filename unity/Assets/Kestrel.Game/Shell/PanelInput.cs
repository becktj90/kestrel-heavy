using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    /// <summary>
    /// Legacy Input positions (Input.mousePosition, Touch.position) have their origin at the bottom-left of the screen;
    /// UI Toolkit's RuntimePanelUtils.ScreenToPanel and panel.Pick work from the top-left. Without the flip a tap is
    /// hit-tested at its mirror image: #348 (TestFlight 204, t463) the open camera picker read a tap on its Chase
    /// chip as a tap outside (the mirrored point was on the HUD above), closed itself before the chip's click, and
    /// the camera stayed on Pad. Quick taps that landed down and up before Update still worked, so it looked random.
    /// </summary>
    public static class PanelInput
    {
        /// <summary>Bottom-left screen pixels to top-left screen pixels.</summary>
        public static Vector2 TopLeft(Vector2 screenBottomLeft, float screenHeight) =>
            new Vector2(screenBottomLeft.x, screenHeight - screenBottomLeft.y);

        /// <summary>Inverse of <see cref="TopLeft"/> (the same mirror).</summary>
        public static Vector2 BottomLeft(Vector2 screenTopLeft, float screenHeight) => TopLeft(screenTopLeft, screenHeight);

        /// <summary>A legacy Input position as a panel point for <c>panel.Pick</c>.</summary>
        public static Vector2 ToPanel(IPanel panel, Vector2 screenBottomLeft) =>
            RuntimePanelUtils.ScreenToPanel(panel, TopLeft(screenBottomLeft, Screen.height));

        /// <summary>The element under a legacy Input position, or null.</summary>
        public static VisualElement Pick(IPanel panel, Vector2 screenBottomLeft) =>
            panel != null ? panel.Pick(ToPanel(panel, screenBottomLeft)) : null;
    }
}
