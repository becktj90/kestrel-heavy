// CSS pixel size of the WebGL stage. Layout frames and the page FULL/EXIT control share that space.
// Screen.width is the canvas buffer (CSS times the pixel ratio), so it is not the button's pixel.
using System.Runtime.InteropServices;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class WebViewport
    {
        public static void Css(out float width, out float height)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            width = KhCssWidth();
            height = KhCssHeight();
            if (width > 1f && height > 1f) return;
#endif
            width = Screen.width;
            height = Screen.height;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int KhCssWidth();
        [DllImport("__Internal")] static extern int KhCssHeight();
#endif
    }
}
