// VISUAL_SPEC §5 device table as EditMode tests (iPhone, Pro Max, iPad 11", iPad 13").
using Kestrel.Game.Layout;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    public class PortraitLayoutTests
    {
        static readonly SpecPortrait S = new SpecPortrait();

        // class, pt w, pt h, dpr, safe top, safe bottom, envelop css px/du, visible du
        [TestCase("iphone", 393, 852, 3, 59, 34, 1.183, 332)]
        [TestCase("iphone-promax", 440, 956, 3, 62, 34, 1.328, 331)]
        [TestCase("ipad", 820, 1180, 2, 24, 20, 1.639, 500)]
        [TestCase("ipad-large", 1032, 1376, 2, 24, 20, 1.911, 540)]
        public void DeviceTable(string cls, double w, double h, double dpr, double top, double bot, double envCss, double visDu)
        {
            double wp = w * dpr, hp = h * dpr;
            var f = PortraitLayout.Compute(wp, hp, PortraitLayout.SafeFromInsets(w, h, top, bot, dpr), dpr, S);
            Assert.That(PortraitLayout.IsPortrait(wp, hp), cls);
            Assert.That(f.Letterboxed, Is.False);
            Assert.That(f.PxPerDu / dpr, Is.EqualTo(envCss).Within(0.001), "envelop");
            Assert.That(f.VisibleHDu, Is.EqualTo(720).Within(1e-9), "height dominates in portrait");
            Assert.That(f.VisibleWDu, Is.EqualTo(visDu).Within(1.5), "visible width");
            Assert.That(f.StageK, Is.EqualTo(2), "k capped");
            // Cover: the world fills the whole screen in both axes (no bars).
            Assert.That(f.VisibleWDu * f.PxPerDu, Is.EqualTo(wp).Within(1e-6));
            Assert.That(f.VisibleHDu * f.PxPerDu, Is.EqualTo(hp).Within(1e-6));
            // Content rect = safe area minus 8 pt each side.
            Assert.That(f.Content.W, Is.EqualTo((w - 16) * dpr).Within(1e-9));
            Assert.That(f.Content.Y, Is.EqualTo(bot * dpr).Within(1e-9));
            Assert.That(f.Content.YMax, Is.EqualTo((h - top) * dpr).Within(1e-9));
            Assert.That(f.VehicleAnchorPxY, Is.EqualTo(f.Content.YMax - 0.38 * f.Content.H).Within(1e-9));
            // Narrowest screen must still fit the widest vehicle (92 du) and the plume clamp.
            Assert.That(f.VisibleWDu, Is.GreaterThan(92));
            Assert.That(f.PlumeMaxWidthDu, Is.EqualTo(0.8 * f.VisibleWDu).Within(1e-9));
            Assert.That(f.MinTouchPx, Is.EqualTo(44 * dpr));
        }

        [Test]
        public void SafeAreaIsClampedToScreen()
        {
            var f = PortraitLayout.Compute(1000, 2000, new RectPx(-10, -10, 1100, 2100), 2, S);
            Assert.That(f.Content.X, Is.EqualTo(16));
            Assert.That(f.Content.XMax, Is.EqualTo(1000 - 16));
            Assert.That(f.Content.Y, Is.EqualTo(0));
            Assert.That(f.Content.YMax, Is.EqualTo(2000));
        }

        [Test]
        public void SkyBlendMatchesTodJs()
        {
            Assert.That(SkyBlend.SpaceBlend(0), Is.EqualTo(0));
            Assert.That(SkyBlend.SpaceBlend(6), Is.EqualTo(0));
            Assert.That(SkyBlend.SpaceBlend(31), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(SkyBlend.SpaceBlend(56), Is.EqualTo(1));
            Assert.That(SkyBlend.SpaceBlend(0, "SEP"), Is.EqualTo(1));
            Assert.That(SkyBlend.SpaceBlend(0, "JACKLYN"), Is.EqualTo(1));
            // noon wash: horizon 0x6fb4e8 -> space 0x010105
            Assert.That(SkyBlend.ThemedFill(0x6fb4e8, 0x010105, 0), Is.EqualTo(0x6fb4e8));
            Assert.That(SkyBlend.ThemedFill(0x6fb4e8, 0x010105, 1), Is.EqualTo(0x010105));
            Assert.That(SkyBlend.ThemedFill(0x6fb4e8, 0x010105, 0.5), Is.EqualTo(0x385b77));
            Assert.That(SkyBlend.ParseHex("#6fb4e8"), Is.EqualTo(0x6fb4e8));
        }
    }
}
