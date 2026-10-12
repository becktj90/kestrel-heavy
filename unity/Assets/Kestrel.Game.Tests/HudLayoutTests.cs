using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // Regression: the telemetry grid sat mid-screen over the rocket in every flight shot.
    public class HudLayoutTests
    {
        // Upper bound of the top band's height in panel units: T+ row (56 px font) + 10 margin
        // + grid (2 rows of 28 px text + 8 px margins, 12 px padding each side) with slack.
        const float BandHeight = 220f;

        // device px, safe area in px (bottom-left origin), dpr; panel matches width 1179 (KH_PanelSettings).
        static readonly object[] Devices =
        {
            new object[] { "iPhone 16 Pro",     1179f, 2556f, new Rect(0, 102, 1179, 2556 - 102 - 177), 3f },
            new object[] { "iPhone 16 Pro Max", 1290f, 2796f, new Rect(0, 102, 1290, 2796 - 102 - 177), 3f },
            new object[] { "iPhone SE",          750f, 1334f, new Rect(0, 0, 750, 1334 - 40), 2f },
            new object[] { "iPad Pro 13",       2064f, 2752f, new Rect(0, 40, 2064, 2752 - 40 - 48), 2f },
        };

        static Vector2 Panel(float w, float h) => new Vector2(1179f, h * 1179f / w);

        [TestCaseSource(nameof(Devices))]
        public void TopBandIsInsideSafeAreaAndAboveVehicle(string name, float w, float h, Rect safe, float dpr)
        {
            var screen = new Vector2(w, h); var panel = Panel(w, h);
            var pad = HudLayout.SafePadding(screen, safe, panel, dpr);
            float safeTopPanel = (h - safe.yMax) * panel.y / h;
            Assert.That(pad.y, Is.GreaterThanOrEqualTo(safeTopPanel), name + ": band must start below the notch / status area");
            Assert.That(pad.x, Is.GreaterThanOrEqualTo(safe.xMin * panel.x / w), name);
            Assert.That(pad.w, Is.GreaterThanOrEqualTo(safe.yMin * panel.y / h), name + ": bottom must clear the home indicator");
            float bandBottom = pad.y + BandHeight;
            Assert.That(bandBottom, Is.LessThan(HudLayout.MaxBandBottom(screen, safe, panel)), name + ": band overlaps the vehicle");
            Assert.That(bandBottom, Is.LessThan(HudLayout.AnchorPanelY(screen, safe, panel)), name);
        }

        [Test]
        public void AnchorIs38PercentOfSafeRectFromTop()
        {
            var screen = new Vector2(1000, 2000); var safe = new Rect(0, 100, 1000, 1700); // top inset 200
            Assert.That(HudLayout.AnchorPanelY(screen, safe, screen), Is.EqualTo(200 + 0.38f * 1700).Within(0.01f));
        }
    }
}
