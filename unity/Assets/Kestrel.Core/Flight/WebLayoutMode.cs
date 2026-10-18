// Web player layout gate. iOS stays portrait-only. A portrait web viewport keeps the phone
// column. A landscape web viewport (wider than it is tall) uses the wide frames.
using System;

namespace Kestrel.Core.Flight
{
    public static class WebLayoutMode
    {
        /// <summary>True when the viewport is wider than it is tall. Square stays portrait, matching PortraitLayout.</summary>
        public static bool IsLandscape(float width, float height) => height > 0f && width > height;

        /// <summary>
        /// Runtime gate. False in the player builds that are not WebGL, and in the editor, so iOS
        /// and the EditMode tests keep the phone layout. Pass <paramref name="webBuild"/> from tests.
        /// </summary>
        public static bool UseWide(float width, float height, bool webBuild) => webBuild && IsLandscape(width, height);

        public static bool UseWide(float width, float height)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsLandscape(width, height);
#else
            return false;
#endif
        }

        /// <summary>The page FULL/EXIT control is drawn on top of the canvas in the web player.</summary>
        public static bool ReserveExit
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>EditMode seam. Null follows <see cref="ReserveExit"/>. Tests set true to reserve the page FULL button.</summary>
        public static bool? ForceReserveExit;

        public static bool WantExitReserve => ForceReserveExit ?? ReserveExit;

        /// <summary>Top inset so a full-width header starts under the page FULL button. The button is only the top-right corner.</summary>
        public static void ExitInset(bool reserve, out float top, out float right)
        {
            top = reserve ? HomeCornerLayout.ExitH + 4f : 0f;
            right = 0f;
        }
    }
}
