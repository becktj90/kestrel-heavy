// Places the music row and the settings gear so they do not share a corner with the web EXIT.
using Kestrel.Core.Flight;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class HomeChrome
    {
        public static void Place(VisualElement gear, VisualElement audio, float width, float topInset, bool reserveExit)
        {
            if (width <= 0f) return;
            HomeCornerLayout.Place(width, topInset, reserveExit, out var audioBox, out var gearBox, out _);
            AudioBar.Place(audio, audioBox.Y, audioBox.X);
            if (gear == null) return;
            gear.style.position = Position.Absolute;
            gear.style.left = gearBox.X;
            gear.style.top = gearBox.Y;
            gear.style.width = gearBox.W;
            gear.style.height = gearBox.H;
            gear.style.right = StyleKeyword.Auto;
        }
    }
}
