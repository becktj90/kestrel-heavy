using System;
using System.Globalization;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class GapToGoalTests
    {
        [Test]
        public void Sentences_MatchGoldenVectors()
        {
            string path = Path.Combine(Golden.Root, "tests", "vectors", "debrief", "landing.json");
            var file = MiniJson.ParseObject(File.ReadAllText(path));
            foreach (JsonObject c in file.Arr("cases"))
            {
                var d = GapToGoal.Landing(c.Str("kind"), c.Num("vy"), c.Num("dx"), c.Num("landingVy"), c.Num("landingTol"), c.Bool("tipped"));
                string id = c.Str("id");
                Assert.AreEqual(c.Str("text"), d.Text, id);
                Assert.AreEqual(c.Str("fix"), d.Fix, id + " fix");
                Assert.AreEqual(c.Num("sinkShortfallMs"), d.SinkShortfallMs, 1e-9, id + " sink");
                Assert.AreEqual(c.Num("offsetShortfallM"), d.OffsetShortfallM, 1e-9, id + " offset");
                Assert.IsFalse(d.Text.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0, id);
                Assert.IsFalse(d.Text.IndexOf("almost", StringComparison.OrdinalIgnoreCase) >= 0, id);
            }
        }

        [Test]
        public void Shortfall_IsNotRoundedDown_SpareMargin_IsNotRoundedUp()
        {
            var d = GapToGoal.Landing("deck", 4.11, 36.2, 4, 36, false);
            StringAssert.Contains("Short by 0.2 m/s sink", d.Text);
            Assert.Less(4.11 - 4, 0.2 + 1e-9);
            var inside = GapToGoal.Landing("deck", 2.19, 10.9, 4, 36, false);
            StringAssert.Contains("Sink margin 1.8 m/s", inside.Text);
            StringAssert.Contains("offset margin 25 m", inside.Text);
            Assert.IsNull(inside.Fix);
        }

        [Test]
        public void SameInput_IsTheSameSentence()
        {
            var a = GapToGoal.Landing("deck", 6, 50, 4, 36, true);
            var b = GapToGoal.Landing("deck", 6, 50, 4, 36, true);
            Assert.AreEqual(a.Text, b.Text);
            Assert.AreEqual(GapToGoal.EaseBurn, a.Fix);
        }
    }
}
