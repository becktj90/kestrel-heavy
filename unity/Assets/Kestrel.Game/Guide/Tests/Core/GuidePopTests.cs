// Corner portrait: where she stands, when the caption shows, and what a tap does.
using System.Collections.Generic;
using NUnit.Framework;

namespace Kestrel.Game.Guide.Tests
{
    public class GuidePopTests
    {
        [Test]
        public void PortraitShowsOnCampaignScreensAndNeverOnAscent()
        {
            Assert.IsTrue(GuidePopLayout.Shows(GuidePlace.Menu));
            Assert.IsTrue(GuidePopLayout.Shows(GuidePlace.Designer));
            Assert.IsFalse(GuidePopLayout.Shows(GuidePlace.Ascent));
        }

        [Test]
        public void SpriteSlidesInFromTheRightCornerAtSixteenPixels()
        {
            var screen = new GuideScreen(375f, 667f, 20f, 34f, 0f, 0f, 0f, false);
            Assert.AreEqual(64f, GuidePopLayout.DisplayPt);
            Assert.AreEqual(16f, GuidePopLayout.ArtPx);
            var off = GuidePopLayout.Place(screen, 0f, false, false);
            var rest = GuidePopLayout.Place(screen, 1f, false, false);
            Assert.AreEqual(screen.Width, off.X, 0.01f);
            Assert.Less(rest.X + rest.Block, screen.Width - screen.SafeRight);
            Assert.Greater(rest.X, screen.SafeLeft);
            Assert.AreEqual(screen.SafeTop + GuidePopLayout.MarginPt, rest.Y, 0.01f);
            Assert.AreEqual(64f, rest.Size);
            var still = GuidePopLayout.Place(screen, 0f, true, false);
            Assert.AreEqual(rest.X, still.X, 0.01f);
            Assert.AreEqual(0f, GuidePopLayout.Slide(0f, false));
            Assert.AreEqual(1f, GuidePopLayout.Slide(0f, true));
        }

        [Test]
        public void NarrowPortraitAndLandscapeLeaveTheLeftHalfClear()
        {
            var portrait = new GuideScreen(335f, 720f, 47f, 34f, 0f, 0f, 0f, false);
            var landscape = new GuideScreen(335f, 210f, 0f, 0f, 47f, 47f, 0f, true);
            foreach (var screen in new[] { portrait, landscape })
            {
                var rest = GuidePopLayout.Place(screen, 1f, true, true);
                Assert.GreaterOrEqual(rest.X, screen.SafeLeft, "inside the left safe edge");
                Assert.LessOrEqual(rest.X + rest.Block, screen.Width - screen.SafeRight + 0.05f, "inside the right safe edge");
                Assert.GreaterOrEqual(rest.X, screen.Width * 0.5f - 0.05f, "left half stays free at 335");
                Assert.AreEqual(64f, rest.Size);
                Assert.GreaterOrEqual(rest.Y, screen.SafeTop);
            }
        }

        [Test]
        public void CaptionIsOneLineAndOnlyWhenVoiceWillNotPlay()
        {
            Assert.IsFalse(GuideCaption.Visible(true, false, false));
            Assert.IsTrue(GuideCaption.Visible(false, false, false));
            Assert.IsTrue(GuideCaption.Visible(true, true, false));
            Assert.IsTrue(GuideCaption.Visible(true, false, true));

            float normal = GuideCaption.Glyph(SubtitleSize.Normal);
            float large = GuideCaption.Glyph(SubtitleSize.ExtraLarge);
            Assert.AreEqual(7f, normal);
            Assert.Greater(large, normal);
            string text = "Wind 14 knots from the W. The rocket will lean. That is fine. We plan for it.";
            string small = GuideCaption.Line(text, 180f, normal);
            string big = GuideCaption.Line(text, 180f, large);
            Assert.That(small, Does.Not.Contain("\n"));
            Assert.Greater(small.Length, big.Length);
            Assert.That(big, Does.EndWith("..."));
            Assert.AreEqual("Hold still now", GuideCaption.Line("Hold\nstill   now", 200f, normal));
        }

        [Test]
        public void TapFinishesReplaysOrDismisses()
        {
            Assert.AreEqual(GuidePopAction.Finish, GuidePopLayout.OnTap(true, false, false, false));
            Assert.AreEqual(GuidePopAction.Dismiss, GuidePopLayout.OnTap(true, true, false, false));
            Assert.AreEqual(GuidePopAction.Dismiss, GuidePopLayout.OnTap(false, true, true, true));
            Assert.AreEqual(GuidePopAction.Replay, GuidePopLayout.OnTap(false, true, true, false));
        }

        [Test]
        public void EngineerPageKeepsTheCardIdAndDoesNotSpeak()
        {
            var beat = new GuideBeat
            {
                Id = "step.2.pass",
                Cards = new[]
                {
                    new GuideCard { Id = "step.2.pass.c1", Kid = "Tidy! {twr} bananas of push.", Eng = "TWR {twr}." },
                },
                Tokens = new Dictionary<string, string> { { "twr", "1.42" } },
            };
            var kid = GuidePages.Pages(beat, false);
            Assert.AreEqual(1, kid.Length);
            Assert.IsTrue(kid[0].Speaks);
            Assert.AreEqual("step.2.pass.c1", kid[0].CardId);
            Assert.That(kid[0].Text, Does.Contain("1.42"));
            var both = GuidePages.Pages(beat, true);
            Assert.AreEqual(2, both.Length);
            Assert.IsFalse(both[1].Speaks);
            Assert.AreEqual("step.2.pass.c1", both[1].CardId);
            Assert.That(both[1].Text, Does.Contain("1.42"));
        }
    }
}
