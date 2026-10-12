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

        // Shell panel: Scale With Screen Size, 375×667 reference, match height. "KESTREL" at 52px bold
        // with 4px tracking measured ~254 panel px in the CI font; 320 leaves slack for a wider face.
        const double ShellRefH = 667;
        const double HomePad = 32;       // .kh-home padding 16 + 16
        const double Outline = 2;        // HomeTitleFit keeps the 1px outline inside the column
        const double WordmarkAt52 = 320;

        static (double panelW, double panelH) ShellPanel(double screenW, double screenH)
            => (screenW * ShellRefH / screenH, ShellRefH);

        static double TitleAvailable(double screenW, double screenH, RectPx safe, double dpr, double panelW, double panelH)
        {
            var frame = PortraitLayout.Compute(screenW, screenH, safe, dpr, S);
            var insets = PortraitLayout.MapContentToPanel(screenW, screenH, frame.Content, panelW, panelH);
            double column = System.Math.Min(insets.Width(panelW), 600); // .kh-screen max-width
            return column - HomePad - Outline;
        }

        [TestCase("iPhone SE", 750, 1334, 2, 20, 0)]
        [TestCase("iPhone 16 Pro", 1179, 2556, 3, 59, 34)]
        [TestCase("iPhone 16 Pro Max", 1320, 2868, 3, 62, 34)]
        [TestCase("iPad Pro 13", 2064, 2752, 2, 24, 20)]
        public void WordmarkFitsInsideTheSafeColumn(string name, double w, double h, double dpr, double topPt, double botPt)
        {
            var safe = PortraitLayout.SafeFromInsets(w / dpr, h / dpr, topPt, botPt, dpr);
            var panel = ShellPanel(w, h);
            double avail = TitleAvailable(w, h, safe, dpr, panel.panelW, panel.panelH);
            double size = PortraitLayout.FitFontSize(avail, HomeTitleFitMax, 18, WordmarkAt52);
            Assert.That(WordmarkAt52 * size / HomeTitleFitMax, Is.LessThanOrEqualTo(avail + 1e-6), name);
            Assert.That(size, Is.GreaterThanOrEqualTo(40), name + ": wordmark stays a title");
        }

        const double HomeTitleFitMax = 52;

        [Test]
        public void InsetsMatchScaleWhenThePanelFillsTheScreen()
        {
            const double w = 1179, h = 2556, dpr = 3;
            var safe = PortraitLayout.SafeFromInsets(w / dpr, h / dpr, 59, 34, dpr);
            var frame = PortraitLayout.Compute(w, h, safe, dpr, S);
            var panel = ShellPanel(w, h);
            double scale = h / ShellRefH;
            var insets = PortraitLayout.MapContentToPanel(w, h, frame.Content, panel.panelW, panel.panelH);
            Assert.That(insets.Left, Is.EqualTo(frame.Content.X / scale).Within(1e-6));
            Assert.That(insets.Right, Is.EqualTo((w - frame.Content.XMax) / scale).Within(1e-6));
            Assert.That(insets.Top, Is.EqualTo((h - frame.Content.YMax) / scale).Within(1e-6));
            Assert.That(insets.Bottom, Is.EqualTo(frame.Content.Y / scale).Within(1e-6));
        }

        [Test]
        public void CaptureTargetKeepsTheSafeColumnOnTheWholePanel()
        {
            // PlayMode Home.png renders the shell panel into 1179×2556 while the batchmode game view
            // stays 640×480. Dividing those screen pixels by the render-target scale pinned the safe
            // column to ~163 panel px and clipped KESTREL on the left.
            const double screenW = 640, screenH = 480, rtW = 1179, rtH = 2556;
            var panel = ShellPanel(rtW, rtH);
            var safe = new RectPx(0, 0, screenW, screenH);
            var frame = PortraitLayout.Compute(screenW, screenH, safe, 1, S);
            var insets = PortraitLayout.MapContentToPanel(screenW, screenH, frame.Content, panel.panelW, panel.panelH);
            Assert.That(insets.Width(panel.panelW), Is.GreaterThan(panel.panelW * 0.9));
            double avail = TitleAvailable(screenW, screenH, safe, 1, panel.panelW, panel.panelH);
            double size = PortraitLayout.FitFontSize(avail, HomeTitleFitMax, 18, WordmarkAt52);
            Assert.That(WordmarkAt52 * size / HomeTitleFitMax, Is.LessThanOrEqualTo(avail + 1e-6));
            Assert.That(size, Is.GreaterThanOrEqualTo(40));
        }

        [Test]
        public void FitFontSizeScalesDownOnlyWhenTheLineIsWiderThanTheColumn()
        {
            Assert.That(PortraitLayout.FitFontSize(400, 52, 18, 320), Is.EqualTo(52));
            Assert.That(PortraitLayout.FitFontSize(160, 52, 18, 320), Is.EqualTo(26).Within(1e-9));
            Assert.That(PortraitLayout.FitFontSize(10, 52, 18, 320), Is.EqualTo(18));
            Assert.That(PortraitLayout.FitFontSize(0, 52, 18, 320), Is.EqualTo(18));
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
