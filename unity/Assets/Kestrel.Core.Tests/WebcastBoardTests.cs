using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Timeline;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class WebcastBoardTests
    {
        static LaunchBeat B(string id, string label, double t) => new LaunchBeat { Id = id, Label = label, Banner = label, Nominal = t };

        static List<LaunchBeat> Sample() => new List<LaunchBeat>
        {
            B("term.ignition", "Ignition", -3),
            B("term.liftoff", "Liftoff", 0),
            B("asc.maxq", "Max-Q", 68),
            B("stg.meco", "MECO", 150),
            B("asc.karman", "Karman", 160),
            B("stg.fairing", "Fairing", 170),
        };

        [Test]
        public void WindowMarksTheLiveEventAndKeepsNeighbours()
        {
            var chips = WebcastBoard.Window(Sample(), 68);
            Assert.AreEqual(5, chips.Count);
            Assert.AreEqual("Liftoff", chips[0].Label);
            Assert.AreEqual(ChipState.Past, chips[0].State);
            Assert.AreEqual("Max-Q", chips[1].Label);
            Assert.AreEqual(ChipState.Now, chips[1].State);
            Assert.AreEqual(ChipState.Next, chips[2].State);
            Assert.AreEqual("Fairing", chips[4].Label);
        }

        [Test]
        public void BeforeTheFirstBeatTheStripIsAllAhead()
        {
            var chips = WebcastBoard.Window(Sample(), -10);
            Assert.AreEqual(ChipState.Next, chips[0].State);
            Assert.AreEqual("Ignition", chips[0].Label);
            Assert.AreEqual(-1, WebcastBoard.CurrentIndex(Sample(), -10));
        }

        [Test]
        public void PulseReadsTheSheetBeforeLiftoffAndTheLiveSpeedAfter()
        {
            var wait = WebcastBoard.Pulse(false, 0, 0, 7600, 420, 0, 35, "LEO 400 km");
            Assert.AreEqual("On the count", wait.Verdict);
            StringAssert.Contains("420 m/s to spare", wait.Detail);
            StringAssert.Contains("LEO 400 km", wait.Detail);

            var climb = WebcastBoard.Pulse(true, 2000, 400, 7600, 420, 12000, 40, "LEO");
            Assert.AreEqual("Climbing", climb.Verdict);

            var hot = WebcastBoard.Pulse(true, 12000, 900, 7600, 420, 36000, 40, "LEO");
            Assert.AreEqual("Heating hard", hot.Verdict);
            Assert.Greater(hot.HeatFraction, WebcastBoard.HeatWarn);

            var done = WebcastBoard.Pulse(true, 80000, 7800, 7600, 420, 100, 40, "LEO");
            Assert.AreEqual("Speed is there", done.Verdict);

            var shortOn = WebcastBoard.Pulse(true, 40000, 2000, 7600, -300, 1000, 40, "LEO");
            Assert.AreEqual("Short on speed", shortOn.Verdict);
        }

        [Test]
        public void ATappedEventSaysWhatItMeansForThisFlight()
        {
            var pulse = WebcastBoard.Pulse(true, 42000, 1500, 7600, 0, 2000, 40, "LEO");
            var karman = WebcastBoard.Callout(B("asc.karman", "Karman", 160), pulse, 42000);
            StringAssert.Contains("100 km", karman);
            StringAssert.Contains("42.0 km", karman);
            var count = WebcastBoard.Callout(B("term.ignition", "Ignition", -3), pulse, 0);
            StringAssert.Contains("T-0:03", count);
        }
    }
}
