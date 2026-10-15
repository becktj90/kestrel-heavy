// Portrait layout for the guided designer. Points, not pixels.
// iPhone SE (375×667) through iPad portrait. The Hangar sheet already pads Screen.safeArea;
// ApplySafeArea is for a host that is not inside that sheet, and for the fit check below.
using System;

namespace Kestrel.Slice.DesignerV3.Stepper
{
    public static class StepperLayout
    {
        public const float RocketBandPt = 112f;
        public const float NavButtonPt = 52f;
        public const float MinTargetPt = 44f;
        public const float HeaderPt = 64f;
        public const float ProgressPt = 48f;
        public const float NavPadPt = 12f;
        public const float MinInnerWidthPt = 300f;
        public const float MinScrollPt = 220f;
        public const float SeWidthPt = 375f;
        public const float SeSideInsetPt = 8f;
        public const float SideInsetPt = 16f;

        public readonly struct Frame
        {
            public readonly float WidthPt, HeightPt;
            public readonly float SafeTop, SafeBottom, SafeLeft, SafeRight;
            public readonly bool Portrait;
            public readonly float InnerWidthPt;
            public readonly float ScrollPt;
            public readonly bool Fits;

            public Frame(float widthPt, float heightPt, float safeTop, float safeBottom, float safeLeft, float safeRight,
                bool portrait, float innerWidthPt, float scrollPt, bool fits)
            {
                WidthPt = widthPt; HeightPt = heightPt;
                SafeTop = safeTop; SafeBottom = safeBottom; SafeLeft = safeLeft; SafeRight = safeRight;
                Portrait = portrait; InnerWidthPt = innerWidthPt; ScrollPt = scrollPt; Fits = fits;
            }
        }

        public static float SideInset(float widthPt) => widthPt <= SeWidthPt + 0.5f ? SeSideInsetPt : SideInsetPt;

        /// <summary>
        /// One column, thumb at the bottom. Landscape does not fit: this screen has no side-by-side layout.
        /// Scroll room is what remains after the header, the progress bar, the nav, and the safe insets.
        /// </summary>
        public static Frame Solve(float widthPt, float heightPt, float safeTop, float safeBottom, float safeLeft, float safeRight)
        {
            float top = Math.Max(0f, safeTop);
            float bottom = Math.Max(0f, safeBottom);
            float left = Math.Max(0f, safeLeft);
            float right = Math.Max(0f, safeRight);
            bool portrait = widthPt > 0f && heightPt >= widthPt;
            float inner = widthPt - left - right;
            float scroll = heightPt - top - bottom - HeaderPt - ProgressPt - NavButtonPt - NavPadPt;
            bool fits = portrait && inner >= MinInnerWidthPt && scroll >= MinScrollPt;
            return new Frame(widthPt, heightPt, top, bottom, left, right, portrait, inner, scroll, fits);
        }
    }
}
