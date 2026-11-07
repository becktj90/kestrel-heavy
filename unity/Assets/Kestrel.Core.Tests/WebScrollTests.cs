// #416 item 8: web scroll chrome (no grey scroller, bottom fade) and the designer header plate contrast.
using Kestrel.Core.Designer;
using Kestrel.Core.Feel;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class WebScrollTests
    {
        [Test]
        public void TheWebKeepsNoScrollerLane()
        {
            Assert.AreEqual(0f, WebScroll.Lane(true, 16f));
            Assert.AreEqual(16f, WebScroll.Lane(false, 16f));
        }

        [TestCase(400f, 300f, 0f, true)]
        [TestCase(400f, 300f, 97f, true)]
        [TestCase(400f, 300f, 100f, false)]
        [TestCase(300f, 300f, 0f, false)]
        [TestCase(200f, 300f, 0f, false)]
        [TestCase(400f, 0f, 0f, false)]
        public void TheFadeShowsOnlyWhileMoreIsBelow(float content, float view, float offset, bool shows)
        {
            Assert.AreEqual(shows, WebScroll.FadeShows(content, view, offset));
        }

        [Test]
        public void TheFadeRampsFromClearToItsMax()
        {
            float prev = -1f;
            for (int i = 0; i < WebScroll.FadeBands; i++)
            {
                float a = WebScroll.BandAlpha(i);
                Assert.Greater(a, prev);
                prev = a;
            }
            Assert.Less(WebScroll.BandAlpha(0), 0.05f);
            Assert.AreEqual(WebScroll.FadeMaxAlpha, WebScroll.BandAlpha(WebScroll.FadeBands - 1), 1e-5);
        }

        [Test]
        public void TheDesignerSubtitleReadsAtLeast4_5To1OnAnySky()
        {
            var plate = CountdownStack.Rgba.Bytes(StarterScreen.HeaderPlateRgb[0], StarterScreen.HeaderPlateRgb[1], StarterScreen.HeaderPlateRgb[2], StarterScreen.HeaderPlateAlpha);
            var sub = CountdownStack.Rgba.Bytes(StarterScreen.SubtitleRgb[0], StarterScreen.SubtitleRgb[1], StarterScreen.SubtitleRgb[2]);
            double worst = double.MaxValue;
            foreach (var sky in CountdownStack.Backdrops)
                worst = System.Math.Min(worst, CountdownStack.Contrast(plate.Over(sky), sub));
            worst = System.Math.Min(worst, CountdownStack.Contrast(plate.Over(CountdownStack.Rgba.Bytes(255, 255, 255)), sub));
            Assert.GreaterOrEqual(worst, 4.5, "worst " + worst.ToString("0.00"));
            Assert.GreaterOrEqual(worst, 7.0, "measured 7.1:1 on white");
        }
    }
}
