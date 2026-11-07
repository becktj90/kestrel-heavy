// The line under the Hangar LAUNCH row ("Not enough speed for this orbit…"). It used to appear and vanish,
// so the bottom-anchored sheet grew and shrank 23–57 px under the player's finger as a tweak crossed a limit
// (fun review item 13, engineer/119 and 252). The slot keeps its room: two lines always, and once a longer
// reason has shown, that height until the font size changes.
using System;

namespace Kestrel.Core.Designer
{
    public sealed class LaunchReasonSlot
    {
        public const int ReservedLines = 2;
        /// <summary>Space Grotesk ascent plus descent is ~1.28 em.</summary>
        public const float LineH = 1.3f;

        float font = -1f, highWater;

        /// <summary>Minimum height for <paramref name="fontPx"/>. Never smaller than two lines.</summary>
        public static float Base(float fontPx) => ReservedLines * Math.Max(0f, fontPx) * LineH;

        /// <summary>The reserved height now. A font change drops the remembered height.</summary>
        public float Reserve(float fontPx)
        {
            if (Math.Abs(fontPx - font) > 0.01f) { font = fontPx; highWater = 0f; }
            return Math.Max(Base(fontPx), highWater);
        }

        /// <summary>Record a laid-out height while a reason is showing. Returns true when the reserve grew.</summary>
        public bool Saw(float fontPx, float height)
        {
            if (float.IsNaN(height) || height <= 0f) return false;
            float before = Reserve(fontPx);
            if (height <= before + 0.5f) return false;
            highWater = height;
            return true;
        }
    }
}
