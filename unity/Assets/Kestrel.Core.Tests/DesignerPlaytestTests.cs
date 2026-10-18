// Play-test regressions: TWR never goes negative, warning copy is plain, the chip matches the list,
// and every sample string stays inside the glyphs the designer fonts actually contain.
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignerPlaytestTests
    {
        [Test]
        public void Twr_IsNeverNegative()
        {
            Assert.AreEqual(0, Readout.NonNegativeTwr(-1000, 1000), 0);
            Assert.AreEqual(0, Readout.NonNegativeTwr(1000, -1000), 0);
            Assert.AreEqual(0, Readout.NonNegativeTwr(double.NaN, 1000), 0);
            Assert.AreEqual(0, Readout.NonNegativeTwr(1000, 0), 0);
            Assert.AreEqual(0, Readout.ShownTwr(-4.52), 0);
            Assert.AreEqual(0, Readout.ShownTwr(double.PositiveInfinity), 0);
            Assert.Greater(Readout.NonNegativeTwr(9806.65, 1000), 0.9);
            Assert.Less(Readout.NonNegativeTwr(9806.65, 1000), 1.1);
        }

        [Test]
        public void DeltaChip_UsesDvAndAHyphen()
        {
            string chip = Readout.DeltaChip(1058, -4.52);
            Assert.AreEqual("+1058 m/s dv · -4.52 TWR", chip);
            Assert.AreEqual("", Readout.DeltaChip(0, 0));
            Assert.IsTrue(ShownText.Covered(chip), chip);
            Assert.IsFalse(chip.Contains("\u0394"));
            Assert.IsFalse(chip.Contains("\u2212"));
        }

        [Test]
        public void PlainWarnings_AreNotJargonOrAPadClock()
        {
            string tumble = ShownText.Plain("tumble", "", "tumble margin 0.07");
            string pressure = ShownText.Plain("engine-overpressure", "", "engine-overpressure margin 0.15");
            string fuel = ShownText.Plain("prop-depletion", "Your rocket ran out of fuel at T+0 s, short of the target.", "");
            string air = ShownText.Plain("structural-maxq", "Your rocket bent in half at T+0 s.", "");
            Assert.That(tumble, Does.Contain("flip"));
            Assert.That(pressure, Does.Contain("chamber pressure"));
            Assert.That(fuel, Does.Contain("fuel"));
            Assert.That(air, Does.Contain("air"));
            foreach (var line in new[] { tumble, pressure, fuel, air })
            {
                Assert.That(line, Does.Not.Contain("margin"));
                Assert.That(line, Does.Not.Contain("T+0"));
                Assert.IsTrue(ShownText.Covered(line), line);
            }
        }

        [Test]
        public void Notices_UseOneListForTheChipAndTheBars()
        {
            var advisories = new List<Advisory>
            {
                new Advisory { code = "twr-low", severity = AdvisorySeverity.Block, kid = "Too heavy to lift off.", engineer = "Liftoff TWR 0.62." },
                new Advisory { code = "risk-prop-depletion", severity = AdvisorySeverity.Warn, kid = "Your rocket ran out of fuel at T+0 s.", engineer = "prop-depletion margin -1.00" },
                new Advisory { code = "near-budget", severity = AdvisorySeverity.Note, kid = "Nearly at your budget." },
            };
            var risks = new List<FlightRisk>
            {
                new FlightRisk { kind = "prop-depletion", level = "red", kid = "Your rocket ran out of fuel at T+0 s.", engineer = "prop-depletion margin -1.00" },
                new FlightRisk { kind = "tumble", level = "amber", kid = "", engineer = "tumble margin 0.07" },
                new FlightRisk { kind = "engine-overpressure", level = "amber", kid = "", engineer = "engine-overpressure margin 0.15" },
            };
            var notices = DesignerNotices.Collect(advisories, risks);
            Assert.AreEqual(DesignerNotices.Chip(notices), ShownText.CountLabel(notices.Count));
            Assert.AreEqual("4 things to fix", DesignerNotices.Chip(notices));
            Assert.AreEqual(4, notices.Count);
            foreach (var notice in notices)
            {
                Assert.IsFalse(string.IsNullOrEmpty(notice.text), notice.code);
                Assert.That(notice.text, Does.Not.Contain("T+0"));
                Assert.That(notice.text, Does.Not.Contain("margin 0."));
            }
            Assert.AreEqual("", DesignerNotices.Chip(new List<DesignerNotice>()));
        }

        [Test]
        public void ShownSamples_StayInsideTheFontGlyphs()
        {
            foreach (var sample in ShownText.Samples())
                Assert.IsTrue(ShownText.Covered(sample), sample);
            Assert.IsTrue(ShownText.Covered(ShownText.Screen("Δv −4.00 × ✓ → ≤ ≥ ± °")));
            Assert.AreEqual("dv -4.00 x OK  to  <= >= +/-  deg", ShownText.Screen("Δv −4.00 × ✓ → ≤ ≥ ± °"));
        }
    }
}
