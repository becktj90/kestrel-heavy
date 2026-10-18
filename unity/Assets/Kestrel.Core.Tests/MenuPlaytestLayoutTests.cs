// Play-test layout for Home, the mission board and the record line.
// Sizes are the web phone (~335), SE, Pro Max, iPad portrait and a 1280x800 landscape window.
using System.Linq;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MenuPlaytestLayoutTests
    {
        struct Size
        {
            public string Name;
            public float W, H, Top, Bottom;
            public Size(string name, float w, float h, float top, float bottom)
            { Name = name; W = w; H = h; Top = top; Bottom = bottom; }
        }

        static readonly Size[] Portrait =
        {
            new Size("web 335", 335f, 700f, 0f, 0f),
            new Size("SE", 375f, 667f, 20f, 0f),
            new Size("Pro Max", 440f, 956f, 62f, 34f),
            new Size("iPad", 1032f, 1376f, 24f, 20f),
        };

        [Test]
        public void HomeGearClearsTheFullscreenButton()
        {
            foreach (var d in Portrait)
            {
                var home = WebHomeLayout.Place(d.W, d.H, d.Top, d.Bottom, 0f, 0f);
                Assert.That(home.Gear.Overlaps(home.Exit), Is.False, d.Name);
                Assert.That(home.Gear.Right, Is.LessThanOrEqualTo(d.W + 0.6f), d.Name);
                Assert.That(home.Gear.Bottom, Is.LessThanOrEqualTo(d.H - d.Bottom + 0.6f), d.Name);
                if (d.W < WebHomeLayout.NarrowGear)
                    Assert.That(home.Gear.Y, Is.GreaterThanOrEqualTo(home.Exit.Bottom - 0.1f), d.Name + " gear stacks under FULL");
            }
        }

        [Test]
        public void Landscape1280KeepsCaptionsInsideTheirCards()
        {
            var home = WebHomeLayout.Place(1280f, 800f, 0f, 0f, 0f, 0f);
            Assert.That(home.Wide, Is.True);
            Assert.That(home.Scroll, Is.False);
            Assert.That(WebHomeLayout.TextFitError(home), Is.Null);
            Assert.That(home.Gear.Overlaps(home.Exit), Is.False);
            Assert.That(home.Stats.H, Is.GreaterThanOrEqualTo(home.Type.Stat * WebHomeLayout.Line + 16f));
            Assert.That(home.Hint.H, Is.GreaterThanOrEqualTo(home.Type.Hint * WebHomeLayout.Line));
        }

        [Test]
        public void BoardHeaderClearsFullscreenAndFactsStayOffTheButtons()
        {
            var sizes = new[]
            {
                new Size("web 335", 335f, 700f, 0f, 0f),
                new Size("SE", 375f, 667f, 0f, 0f),
                new Size("Pro Max", 440f, 956f, 0f, 0f),
                new Size("iPad", 1032f, 1376f, 0f, 0f),
                new Size("web landscape", 1280f, 800f, 0f, 0f),
            };
            foreach (var d in sizes)
            {
                WebLayoutMode.ExitInset(true, out float top, out float right);
                bool wide = WebLayoutMode.IsLandscape(d.W, d.H);
                var frame = MissionBoardLayout.Place(d.W, d.H, top, 0f, 0f, right, wide);
                Assert.That(frame.Home.Y, Is.GreaterThanOrEqualTo(HomeCornerLayout.ExitH - 0.1f), d.Name + " header under FULL");
                Assert.That(frame.Title.W, Is.GreaterThan(100f), d.Name + " act name");
                Assert.That(frame.Facts.Overlaps(frame.Contracts), Is.False, d.Name);
                Assert.That(frame.Facts.Overlaps(frame.Primary), Is.False, d.Name);
                Assert.That(frame.Brief.Overlaps(frame.Facts), Is.False, d.Name);
                Assert.That(frame.Facts.Bottom, Is.LessThanOrEqualTo(frame.Contracts.Y + 0.6f), d.Name);
            }
        }

        [Test]
        public void LockedMissionsSayWhatUnlocksThemAndContractsSayItOnce()
        {
            var campaign = Campaign.Load(Golden.Root);
            var second = campaign.Mission(2);
            var locked = MissionBoardFacts.Of(second, MissionBoardAccess.Locked, 0, null, 0, 0, false);
            Assert.That(locked.Primary, Is.EqualTo("Clear KH-1 to unlock"));
            var first = MissionBoardFacts.Of(campaign.Mission(1), MissionBoardAccess.Open, 0, null, 0, 0, true);
            Assert.That(first.Briefing, Does.Contain("50 kg"));

            var jobs = MissionBoardFacts.Contracts(false, "No contract is posted yet.", 20);
            Assert.That(jobs.Briefing, Is.EqualTo("Opens after KH-20."));
            Assert.That(jobs.Orbit, Is.Not.EqualTo(jobs.Briefing));
            Assert.That(jobs.Lock, Is.EqualTo(""));
            int drawn = new[] { jobs.Orbit, jobs.Briefing, jobs.Lock, jobs.Mass, jobs.Best }.Count(s => s != null && s.Contains("Opens after KH-20"));
            Assert.That(drawn, Is.EqualTo(1));
        }

        [Test]
        public void HomeRecordLineShowsClearedBankAndPatches()
        {
            Assert.That(HomeRecord.Line(0, 0, 100000, 0), Is.EqualTo("0 flights cleared · 0 Kc · 0 patches"));
            Assert.That(HomeRecord.Line(3, 12L * 100000L, 100000, 1), Is.EqualTo("3 flights cleared · 12 Kc · 1 patch"));
            Assert.That(HomeRecord.Line(1, 100000, 100000, 1), Is.EqualTo("1 flight cleared · 1 Kc · 1 patch"));
        }
    }
}
