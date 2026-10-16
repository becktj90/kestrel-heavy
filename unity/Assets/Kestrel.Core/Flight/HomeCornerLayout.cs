// Home corner controls. Pure rectangles so SE, Pro Max and iPad can be checked without a canvas.
// Music sits on the left. The gear sits on the right, and web builds keep a gap for the page EXIT.
using System;

namespace Kestrel.Core.Flight
{
    public readonly struct HomeRect
    {
        public readonly float X, Y, W, H;
        public HomeRect(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
        public float Right => X + W;
        public float Bottom => Y + H;
        public bool Overlaps(HomeRect o) => Right > o.X && o.Right > X && Bottom > o.Y && o.Bottom > Y;
    }

    public static class HomeCornerLayout
    {
        public const float Edge = 8f;
        public const float Gear = 44f;
        public const float AudioW = 200f;
        public const float AudioH = 44f;
        public const float ExitW = 88f;
        public const float ExitH = 44f;

        /// <summary>reserveExit is the web page EXIT in the top-right of the canvas.</summary>
        public static void Place(float width, float topInset, bool reserveExit, out HomeRect audio, out HomeRect gear, out HomeRect exit)
        {
            float top = Math.Max(0f, topInset);
            audio = new HomeRect(Edge, top, AudioW, AudioH);
            float gearInset = Edge + (reserveExit ? ExitW + Edge : 0f);
            gear = new HomeRect(width - gearInset - Gear, top, Gear, Gear);
            exit = new HomeRect(width - Edge - ExitW, top, ExitW, ExitH);
        }
    }
}
