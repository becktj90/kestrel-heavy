using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightSeedTests
    {
        [Test]
        public void MissingDocument_UsesTheSessionSeed()
        {
            ulong daily = DailyChallenge.SeedFor(new System.DateTime(2026, 11, 30, 0, 0, 0, System.DateTimeKind.Utc));
            Assert.AreEqual(daily, FlightSeed.ForAscent(null, daily));
            Assert.AreEqual(daily, FlightSeed.ForAscent(new JsonObject(), daily));
            Assert.AreEqual(1UL, FlightSeed.ForAscent(new JsonObject(), null));
            Assert.AreEqual(1UL, FlightSeed.Read(null));
        }

        [Test]
        public void ExplicitDesignSeed_WinsOverTheSession()
        {
            var doc = new JsonObject();
            doc.Add("seed", "42");
            Assert.AreEqual(42UL, FlightSeed.ForAscent(doc, 99));
            Assert.AreEqual(42UL, FlightSeed.Read(doc));

            var hex = new JsonObject();
            hex.Add("missionSeed", "2A");
            Assert.AreEqual(42UL, FlightSeed.Read(hex));

            var numeric = new JsonObject();
            numeric.Add("missionSeed", 7d);
            Assert.AreEqual(7UL, FlightSeed.Read(numeric));
        }

        [Test]
        public void ZeroAndBlank_FallThroughToOne()
        {
            var zero = new JsonObject();
            zero.Add("seed", "0");
            Assert.AreEqual(1UL, FlightSeed.Read(zero));
            Assert.AreEqual(1UL, FlightSeed.ForAscent(null, 0));
        }
    }
}
