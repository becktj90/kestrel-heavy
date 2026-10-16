// Home composition. Portrait is the phone column (controls under the rocket). Landscape puts the
// rocket on the centre of the screen and the menu tiles in a column beside it.
using System;

namespace Kestrel.Core.Flight
{
    public sealed class WebHomeFrame
    {
        public bool Wide;
        public HudBox Rocket, Menu, Title, Stats, Launch, Design, Missions, Codex, Gear, Exit, Audio, Hint;
    }

    public static class WebHomeLayout
    {
        public const float Gap = 8f;
        public const float Edge = 12f;

        public static WebHomeFrame Place(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight)
        {
            var f = new WebHomeFrame { Wide = WebLayoutMode.IsLandscape(width, height) };
            safeTop = Math.Max(0f, safeTop);
            safeBottom = Math.Max(0f, safeBottom);
            safeLeft = Math.Max(0f, safeLeft);
            safeRight = Math.Max(0f, safeRight);
            // Same corner contract as HomeCornerLayout (reserveExit), shifted inside the safe area
            // so a landscape notch does not push EXIT off the screen.
            float exitX = width - safeRight - HomeCornerLayout.Edge - HomeCornerLayout.ExitW;
            float gearX = exitX - HomeCornerLayout.Edge - HomeCornerLayout.Gear;
            f.Exit = Rect(exitX, safeTop, HomeCornerLayout.ExitW, HomeCornerLayout.ExitH);
            f.Gear = Rect(gearX, safeTop, HomeCornerLayout.Gear, HomeCornerLayout.Gear);
            f.Audio = Rect(safeLeft + HomeCornerLayout.Edge, safeTop, HomeCornerLayout.AudioW, HomeCornerLayout.AudioH);

            float topBar = Math.Max(f.Audio.Bottom, Math.Max(f.Gear.Bottom, f.Exit.Bottom));
            float bottom = height - safeBottom;
            float left = safeLeft;
            float right = width - safeRight;
            if (!f.Wide)
            {
                PlacePhone(f, left, right, topBar, bottom);
                return f;
            }
            PlaceWide(f, width, left, right, topBar, bottom);
            return f;
        }

        static void PlacePhone(WebHomeFrame f, float left, float right, float topBar, float bottom)
        {
            float innerL = left + Edge;
            float innerW = Math.Max(1f, right - left - 2f * Edge);
            float tileH = 76f, launchH = 64f, designH = 48f, statsH = 36f, hintH = 18f;
            float stack = statsH + Gap + launchH + Gap + designH + Gap + tileH + Gap + hintH;
            float y = bottom - Edge - stack;
            f.Stats = Rect(innerL, y, innerW, statsH); y += statsH + Gap;
            f.Launch = Rect(innerL, y, innerW, launchH); y += launchH + Gap;
            f.Design = Rect(innerL, y, innerW, designH); y += designH + Gap;
            float tileW = (innerW - Gap) * 0.5f;
            f.Missions = Rect(innerL, y, tileW, tileH);
            f.Codex = Rect(innerL + tileW + Gap, y, tileW, tileH);
            y += tileH + Gap;
            f.Hint = Rect(innerL, y, innerW, hintH);
            f.Menu = Rect(innerL, f.Stats.Y, innerW, f.Hint.Bottom - f.Stats.Y);
            float titleH = 64f;
            f.Title = Rect(innerL, topBar + Gap, innerW, titleH);
            float rocketY = f.Title.Bottom + Gap;
            f.Rocket = Rect(innerL, rocketY, innerW, Math.Max(1f, f.Menu.Y - Gap - rocketY));
        }

        static void PlaceWide(WebHomeFrame f, float width, float left, float right, float topBar, float bottom)
        {
            float cx = width * 0.5f;
            float rocketW = Clamp(width * 0.30f, 180f, 560f);
            float menuW = Clamp(width * 0.26f, 200f, 400f);
            float statsW = Clamp(width * 0.18f, 120f, 280f);
            float avail = right - left;
            float need = statsW + rocketW + menuW + Edge * 4f;
            if (need > avail)
            {
                float over = need - avail;
                rocketW = Math.Max(150f, rocketW - over * 0.45f);
                menuW = Math.Max(180f, menuW - over * 0.35f);
                statsW = Math.Max(88f, avail - rocketW - menuW - Edge * 4f);
            }

            float rocketX = cx - rocketW * 0.5f;
            float menuX = rocketX + rocketW + Edge;
            float menuLimit = right - Edge;
            if (menuX + menuW > menuLimit)
            {
                menuX = menuLimit - menuW;
                float rocketRight = menuX - Edge;
                rocketW = Math.Max(140f, Math.Min(rocketW, (rocketRight - rocketX)));
                rocketX = cx - rocketW * 0.5f;
                if (rocketX + rocketW + Edge > menuX)
                    rocketX = menuX - Edge - rocketW;
            }
            float statsX = left + Edge;
            if (statsX + statsW > rocketX - Edge)
                statsW = Math.Max(72f, rocketX - Edge - statsX);

            float y0 = topBar + Edge;
            float y1 = bottom - Edge;
            float titleH = Clamp((y1 - y0) * 0.16f, 44f, 72f);
            f.Title = Rect(rocketX, y0, rocketW, titleH);
            float rocketY = f.Title.Bottom + Gap;
            f.Rocket = Rect(rocketX, rocketY, rocketW, Math.Max(1f, y1 - rocketY));

            float statsH = Clamp((y1 - y0) * 0.22f, 44f, 88f);
            float statsY = y0 + Math.Max(0f, (y1 - y0 - statsH) * 0.45f);
            f.Stats = Rect(statsX, statsY, statsW, statsH);

            float hintH = 20f;
            float colTop = y0;
            float colBot = y1;
            float hintY = colBot - hintH;
            float bodyH = Math.Max(44f, hintY - Gap - colTop);
            float launchH = Clamp(bodyH * 0.22f, 44f, 72f);
            float designH = Clamp(bodyH * 0.16f, 44f, 60f);
            float tileH = (bodyH - launchH - designH - Gap * 3f) * 0.5f;
            if (tileH < 44f)
            {
                float room = bodyH - Gap * 3f;
                float scale = room / (launchH + designH + 88f);
                if (scale < 1f) { launchH *= scale; designH *= scale; }
                launchH = Math.Max(36f, launchH);
                designH = Math.Max(36f, designH);
                tileH = Math.Max(36f, (bodyH - launchH - designH - Gap * 3f) * 0.5f);
            }
            float y = colTop;
            f.Launch = Rect(menuX, y, menuW, launchH); y += launchH + Gap;
            f.Design = Rect(menuX, y, menuW, designH); y += designH + Gap;
            f.Missions = Rect(menuX, y, menuW, tileH); y += tileH + Gap;
            f.Codex = Rect(menuX, y, menuW, tileH);
            f.Hint = Rect(menuX, hintY, menuW, hintH);
            f.Menu = Rect(menuX, colTop, menuW, colBot - colTop);
        }

        static float Clamp(float v, float lo, float hi) => Math.Max(lo, Math.Min(hi, v));

        static HudBox Rect(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
