// Flight HUD placement. The telemetry grid used to be the 2nd of 4 children in a space-between column,
// so it landed in the vertical middle of the screen, right over the rocket (every v2 flight shot).
// It now sits in a top band directly under the clock, padded by Screen.safeArea, and this helper is the
// testable contract: the band must end well above the vehicle anchor (0.38 of the content height from
// the top, VISUAL_SPEC §5) on iPhone, Pro Max and iPad.
using UnityEngine;

namespace Kestrel.Slice
{
    public static class HudLayout
    {
        public const float VehicleAnchorY = 0.38f;   // VISUAL_SPEC §5, from top of content (safe) rect
        public const float MinPadPanel = 24f;        // panel units, matches .hud padding
        public const float SideMarginPt = 8f;

        /// <summary>Panel-space padding (left, top, right, bottom) that keeps the HUD inside the safe area.
        /// screen/safe in device px (safe: bottom-left origin, like Screen.safeArea); panel = root panel size.</summary>
        public static Vector4 SafePadding(Vector2 screen, Rect safe, Vector2 panel, float dpr)
        {
            float sx = panel.x / Mathf.Max(1, screen.x), sy = panel.y / Mathf.Max(1, screen.y);
            float side = SideMarginPt * dpr;
            float l = (safe.xMin + side) * sx, r = (screen.x - safe.xMax + side) * sx;
            float t = (screen.y - safe.yMax) * sy, b = safe.yMin * sy;
            return new Vector4(Mathf.Max(MinPadPanel, l), Mathf.Max(MinPadPanel, t + 12f), Mathf.Max(MinPadPanel, r), Mathf.Max(MinPadPanel, b));
        }

        /// <summary>Panel-y where the vehicle anchor sits (0.38 of the safe rect from its top).</summary>
        public static float AnchorPanelY(Vector2 screen, Rect safe, Vector2 panel)
        {
            float sy = panel.y / Mathf.Max(1, screen.y);
            return ((screen.y - safe.yMax) + VehicleAnchorY * safe.height) * sy;
        }

        /// <summary>Band must end at or above this panel-y: half-way between safe top and the anchor,
        /// which leaves the upper half of the vehicle (nose/fairing) clear as well as its middle.</summary>
        public static float MaxBandBottom(Vector2 screen, Rect safe, Vector2 panel)
        {
            float sy = panel.y / Mathf.Max(1, screen.y);
            float top = (screen.y - safe.yMax) * sy;
            return top + 0.5f * (AnchorPanelY(screen, safe, panel) - top);
        }
    }
}
