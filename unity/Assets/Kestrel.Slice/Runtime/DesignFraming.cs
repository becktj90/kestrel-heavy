// Design-screen camera framing. The Design panel covers the bottom of the screen, so the rocket and the
// pad deck must be framed in the band ABOVE the panel's top edge, not in the full viewport (v4 captures:
// the panel hid the bottom quarter of the stack and the whole pad deck on iPhone / Pro Max, more on iPad).
// The band is measured from the live panel rect each layout pass, never a hardcoded pixel value.
using UnityEngine;

namespace Kestrel.Slice
{
    public static class DesignFraming
    {
        /// <summary>The subject is never squeezed into less than this share of the screen height.</summary>
        public const float MinVisibleFraction = 0.35f;
        /// <summary>Extra room so the pad deck (y ≈ -0.5 m) and nose clear the band edges.</summary>
        public const float Margin = 1.12f;
        /// <summary>Points on the rocket the camera aims at, as a fraction of its height (slightly low so the deck is in).</summary>
        public const float AimHeight = 0.46f;

        /// <summary>Share of the root height, from the top, that is not covered by the panel.
        /// rootHeight and panelTop in the same (panel) units; panelTop measured from the top of the root.</summary>
        public static float VisibleFraction(float rootHeight, float panelTop)
        {
            if (rootHeight <= 0f || float.IsNaN(panelTop)) return 1f;
            return Mathf.Clamp(panelTop / rootHeight, MinVisibleFraction, 1f);
        }

        /// <summary>How far to pull the camera back so the stack that filled the full view fits the band.</summary>
        public static float DistanceScale(float visibleFraction) => Margin / Mathf.Clamp(visibleFraction, MinVisibleFraction, 1f);

        /// <summary>Vertical NDC offset that moves the image centre to the middle of the top band.
        /// Band spans NDC y in [1 - 2f, 1]; its centre is 1 - f.</summary>
        public static float NdcShiftY(float visibleFraction) => 1f - Mathf.Clamp(visibleFraction, MinVisibleFraction, 1f);

        /// <summary>Camera projection with the principal point moved up by NdcShiftY. Full-screen render
        /// (no viewport rect, so nothing is letterboxed); only the framing moves.</summary>
        public static Matrix4x4 ShiftedProjection(Matrix4x4 baseProjection, float visibleFraction)
        {
            var p = baseProjection;
            // ndc_y = (m11*y + m12*z) / -z  →  adding -s to m12 adds +s to ndc_y for every point.
            p.m12 -= NdcShiftY(visibleFraction);
            return p;
        }
    }
}
