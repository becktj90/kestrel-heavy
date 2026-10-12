using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MedalShelfTests
    {
        [Test]
        public void Awards_MatchGoldenVectors()
        {
            var tune = LandingTune.Load(Golden.Root);
            string path = Path.Combine(Golden.Root, "tests", "vectors", "medals", "shelf.json");
            var file = MiniJson.ParseObject(File.ReadAllText(path));
            Assert.AreEqual(MedalShelf.Rule, file.Str("rule"));
            foreach (JsonObject c in file.Arr("cases"))
            {
                var award = MedalShelf.Evaluate(tune, c.Str("kind"), c.Num("vy"), c.Num("dx"), c.Num("landingVy"), c.Num("landingTol"), c.Bool("tipped"));
                string id = c.Str("id");
                Assert.AreEqual(c.Str("grade"), award.Grade, id);
                Assert.AreEqual(c.Str("medal"), award.Medal, id);
                Assert.AreEqual(Partner(award.Grade), award.Medal, id + " partner");
            }
            Assert.AreEqual(file.Str("shelf"), MedalShelf.Format(new[] { "KH-1", "KH-2", "KH-3" }, new[] { "gold", "bronze", null }));
        }

        [Test]
        public void Tilt_DoesNotSplitTheGradeFromTheMedal()
        {
            var tune = LandingTune.Load(Golden.Root);
            var upright = MedalShelf.Evaluate(tune, "deck", 2, 18, 4, 36, false);
            Assert.AreEqual("Perfect", upright.Grade);
            Assert.AreEqual("gold", upright.Medal);
            Assert.AreEqual(upright.Medal, MedalShelf.From(tune.Grade("deck", 2, 18, 4, 36, false)).Medal);
        }

        [Test]
        public void Grid_KeepsOneLadder()
        {
            var tune = LandingTune.Load(Golden.Root);
            string[] kinds = { "deck", "water" };
            bool[] tips = { false, true };
            for (int iv = 0; iv <= 8; iv++)
            for (int id = 0; id <= 8; id++)
            foreach (var kind in kinds)
            foreach (var tip in tips)
            {
                double vy = iv * 0.5;
                double dx = id * 8;
                var grade = tune.Grade(kind, vy, dx, 4, 36, tip);
                var award = MedalShelf.From(grade);
                Assert.AreEqual(Partner(grade.Name), award.Medal);
                Assert.AreEqual(grade.Name, award.Grade);
            }
        }

        [Test]
        public void Better_KeepsTheHigherMedal()
        {
            Assert.AreEqual("gold", MedalShelf.Better("bronze", "gold"));
            Assert.AreEqual("silver", MedalShelf.Better("silver", "bronze"));
            Assert.AreEqual("bronze", MedalShelf.Better(null, "bronze"));
            Assert.IsNull(MedalShelf.Better(null, null));
        }

        static string Partner(string grade)
        {
            if (grade == "Perfect") return "gold";
            if (grade == "Good") return "silver";
            if (grade == "Hard" || grade == "Salvage") return "bronze";
            return null;
        }
    }
}
