// How a drag or a wheel on the guide box moves. A short page turns. A tall page scrolls,
// and a drag that starts already at the end turns the page.
using System;

namespace Kestrel.Game.Guide
{
    public enum GuideScrollAction { Tap, Next, Previous, Scroll }

    public static class GuideScroll
    {
        /// <summary>Movement below this is a tap. The typewriter still finishes on a tap.</summary>
        public const float TapSlop = 18f;

        /// <summary>A drag this long that starts at the end of a scrolling page turns the page.</summary>
        public const float PageSlop = 36f;

        /// <summary><paramref name="dy"/> is the finger movement in panel pixels. Negative means the finger moved up
        /// (the gesture that reveals text below). <paramref name="startScroll"/> is the offset when the finger went down.</summary>
        public static GuideScrollAction Release(float dy, float startScroll, float range)
        {
            float abs = dy < 0f ? -dy : dy;
            if (!(range > 1f))
            {
                if (abs < TapSlop) return GuideScrollAction.Tap;
                return dy < 0f ? GuideScrollAction.Next : GuideScrollAction.Previous;
            }
            if (abs < TapSlop) return GuideScrollAction.Tap;
            bool atBottom = startScroll >= range - 1f;
            bool atTop = startScroll <= 1f;
            if (dy <= -PageSlop && atBottom) return GuideScrollAction.Next;
            if (dy >= PageSlop && atTop) return GuideScrollAction.Previous;
            return GuideScrollAction.Scroll;
        }

        /// <summary>Positive <paramref name="deltaY"/> (wheel down, or a finger moving up) increases the offset.</summary>
        public static GuideScrollAction Wheel(float deltaY, float scrollY, float range)
        {
            if (deltaY < 0.01f && deltaY > -0.01f) return GuideScrollAction.Tap;
            if (!(range > 1f)) return deltaY > 0f ? GuideScrollAction.Next : GuideScrollAction.Previous;
            if (deltaY > 0f && scrollY >= range - 1f) return GuideScrollAction.Next;
            if (deltaY < 0f && scrollY <= 1f) return GuideScrollAction.Previous;
            return GuideScrollAction.Scroll;
        }

        /// <summary>Finger up (negative <paramref name="dy"/>) increases the offset, clamped to the page.</summary>
        public static float Offset(float start, float dy, float range)
        {
            if (range < 0f) range = 0f;
            float y = start - dy;
            if (y < 0f) y = 0f;
            if (y > range) y = range;
            return y;
        }
    }
}
