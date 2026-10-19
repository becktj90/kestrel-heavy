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

        /// <summary>Credits chip (and CONTINUE) on Home: the free top-left corner band, left of the gear and EXIT.
        /// It used to sit 52 pt above LAUNCH, which put "0 credits" on the KESTREL title (portrait) and the
        /// stats card (landscape). The wide home already keeps this band (WebHomeLayout's top bar).</summary>
        public const float CreditsMaxW = 240f;
        public const float CreditsH = 44f;

        public static HomeRect Credits(float width, float topInset, bool reserveExit)
        {
            float top = Math.Max(0f, topInset);
            float corner = Edge + Gear + Edge + (reserveExit ? ExitW + Edge : 0f);
            float w = Math.Max(44f, Math.Min(CreditsMaxW, width - Edge - corner));
            return new HomeRect(Edge, top, w, CreditsH);
        }

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
