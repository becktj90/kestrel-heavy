// KcFormat: the report card's compact money (#416 item 3).
using Kestrel.Core.Economy;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class KcFormatTests
    {
        [TestCase(0d, "0")]
        [TestCase(0.4d, "0")]
        [TestCase(-0.4d, "0")]
        [TestCase(4d, "4")]
        [TestCase(10d, "10")]
        [TestCase(999d, "999")]
        [TestCase(999.4d, "999")]
        [TestCase(999.5d, "1.0K")]
        [TestCase(1000d, "1.0K")]
        [TestCase(1049d, "1.0K")]
        [TestCase(1050d, "1.1K")]
        [TestCase(4839.04d, "4.8K")]
        [TestCase(12500d, "12.5K")]
        [TestCase(999949d, "999.9K")]
        [TestCase(999950d, "1.0M")]
        [TestCase(2000000d, "2.0M")]
        [TestCase(3995160.96d, "4.0M")]
        [TestCase(5000000d, "5.0M")]
        [TestCase(8990236.09d, "9.0M")]
        [TestCase(40000000d, "40.0M")]
        [TestCase(1250000000d, "1250.0M")]
        [TestCase(-4839.04d, "-4.8K")]
        [TestCase(-2000000d, "-2.0M")]
        public void CompactUsesWholeKcThenOneDecimalKOrM(double kc, string expected)
        {
            Assert.That(KcFormat.Compact(kc), Is.EqualTo(expected));
        }

        [TestCase(2000000d, "+2.0M")]
        [TestCase(1995160.96d, "+2.0M")]
        [TestCase(10d, "+10")]
        [TestCase(0.2d, "0")]
        [TestCase(0d, "0")]
        [TestCase(-4839.04d, "-4.8K")]
        public void SignedMarksGainsWithAPlus(double kc, string expected)
        {
            Assert.That(KcFormat.Signed(kc), Is.EqualTo(expected));
        }

        [Test]
        public void NotANumberReadsZero()
        {
            Assert.That(KcFormat.Compact(double.NaN), Is.EqualTo("0"));
            Assert.That(KcFormat.Compact(double.PositiveInfinity), Is.EqualTo("0"));
            Assert.That(KcFormat.Signed(double.NegativeInfinity), Is.EqualTo("0"));
        }

        [Test]
        public void CompactNeverShowsMoreThanOneDecimalOrAThousandK()
        {
            for (double kc = 0; kc < 3e6; kc += 777.7)
            {
                string s = KcFormat.Compact(kc);
                Assert.That(s, Does.Not.Contain("1000.0K"), s);
                int dot = s.IndexOf('.');
                if (dot >= 0) Assert.That(s.Length - dot - 1, Is.EqualTo(2), s + " one decimal then the unit");
            }
        }
    }
}
