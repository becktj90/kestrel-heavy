// Portrait layout for Designer v3 (§6): sticky strip + tab bar + scrolling content.
// Fits iPhone SE (375×667) through Pro Max and iPad portrait. 44 pt targets; safe areas.
using System;
using Kestrel.Slice;
using UnityEngine.UIElements;

namespace Kestrel.Slice.DesignerV3
{
    public static class DesignerV3Layout
    {
        public const float TargetPt = 44f;
        public const float StripPt = 72f;
        public const float TabBarPt = 52f;
        public const float ConfirmPt = 64f;
        public const float SideInsetPt = 16f;
        public const float SeSideInsetPt = 8f;
        public const float SheetMaxFraction = 0.55f;
        public const float MinContentPt = 120f;
        /// <summary>Rocket band is the top ~45% on phones (spec §6).</summary>
        public const float RocketFraction = 0.45f;

        public static bool SidePanel(float screenWpx, float screenHpx) => HangarPanelLayout.SidePanel(screenWpx, screenHpx);

        public static float SideInset(float screenWidthPt) => screenWidthPt <= 375.5f ? SeSideInsetPt : SideInsetPt;

        /// <summary>Scrolling content height on a phone sheet after strip + tabs (+ optional launch-confirm row).</summary>
        public static float ContentMaxPt(float screenHeightPt, float safeBottomPt, bool showLaunchConfirm)
        {
            if (!(screenHeightPt > 0)) return MinContentPt;
            float fixedPt = StripPt + TabBarPt + (showLaunchConfirm ? ConfirmPt : 0f);
            return Math.Max(MinContentPt, screenHeightPt * SheetMaxFraction - fixedPt - Math.Max(0, safeBottomPt));
        }

        /// <summary>Rocket viewport height in points: top 45% on phones; on iPad the side panel leaves more height.</summary>
        public static float RocketBandPt(float screenHeightPt, bool sidePanel)
        {
            if (!(screenHeightPt > 0)) return 0;
            if (sidePanel) return screenHeightPt;
            return screenHeightPt * RocketFraction;
        }

        public static bool FitsSeContent(float contentPt) => contentPt >= MinContentPt;

        /// <summary>Thumb row at phone width and in the landscape parts column. Words stay on screen.</summary>
        public static void FitThumbRow(VisualElement row, float widthPx, float upp)
        {
            if (row == null) return;
            if (!(upp > 0f)) upp = 1f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center;
            row.style.overflow = Overflow.Visible;
            bool narrow = !(widthPx > 420f);
            foreach (var child in row.Children())
            {
                child.style.flexShrink = 1;
                child.style.overflow = Overflow.Visible;
                if (child is Button button)
                {
                    button.style.whiteSpace = WhiteSpace.Normal;
                    button.style.overflow = Overflow.Visible;
                    if (narrow)
                    {
                        button.style.minWidth = 44f * upp;
                        button.style.fontSize = 13f * upp;
                        button.style.minHeight = 44f * upp;
                        button.style.height = StyleKeyword.Auto;
                    }
                }
            }
        }
    }
}
