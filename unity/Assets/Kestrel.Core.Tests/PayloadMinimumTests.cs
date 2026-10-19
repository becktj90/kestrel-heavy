// B-02: KH-3 flies a 20 kg cubesat (data/campaign.json). The designer minimum used to be 50 kg,
// so Validate.Shape blocked every KH-3 design and the campaign could not pass KH-3.
// The minimum is now 10 kg (data/designer-limits.json). Expected masses come from campaign.json.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PayloadMinimumTests
    {
        Catalog catalog;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            HomeAutoBuild.InstallMissionOrbits(catalog, campaign);
        }

        [Test]
        public void DesignerMinimum_IsTenKilograms_AndAdmitsEveryMissionPayload()
        {
            var lim = DesignerLimits.Load(Golden.Root);
            Assert.AreEqual(10, lim.payloadMassMin);
            Assert.AreEqual(10, new DesignerLimits().payloadMassMin, "built-in default matches the data file");
            Assert.AreEqual(20, campaign.ById("m3").MassKg, "KH-3 keeps its 20 kg cubesat");
            foreach (var m in campaign.Missions)
                Assert.That(m.MassKg, Is.InRange(lim.payloadMassMin, lim.payloadMassMax), m.Id);
        }

        [Test]
        public void EveryCampaignWinner_HasNoSchemaBlock() => AssertNoSchemaBlock(Path.Combine("data", "golden", "campaign-winners.json"), 20);

        [Test]
        public void EveryBoardStarter_HasNoSchemaBlock() => AssertNoSchemaBlock(Path.Combine("data", "board-starters.json"), 18);

        void AssertNoSchemaBlock(string file, int missions)
        {
            var root = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, file)));
            var rows = root.Arr("missions");
            Assert.AreEqual(missions, rows.Count, file);
            var bad = new List<string>();
            int checkedDesigns = 0;
            foreach (JsonObject row in rows)
            {
                var mission = campaign.ById(row.Str("id"));
                Assert.IsNotNull(mission, row.Str("id"));
                foreach (JsonObject stored in row.Arr("designs"))
                {
                    // As stored, and as Home stamps it for the mission (HomeAutoBuild.ApplyMission).
                    var asStored = JsonMut.CloneObject(stored);
                    var stamped = JsonMut.CloneObject(stored);
                    HomeAutoBuild.ApplyMission(catalog, stamped, mission);
                    Assert.AreEqual(mission.MassKg, stamped.Obj("payload").Num("mass"), mission.Id + " exact mission mass");
                    foreach (var (label, design) in new[] { ("stored", asStored), ("stamped", stamped) })
                    {
                        string where = mission.Id + " " + stored.Str("name") + " " + label;
                        var shape = Validate.Shape(catalog, design);
                        if (!shape.ok) bad.Add(where + ": " + string.Join("; ", shape.errors));
                        var report = Validate.Engineering(catalog, design);
                        foreach (var b in report.blocks)
                            if (b.code == "schema" || b.code == "nozzle-unknown") bad.Add(where + ": " + b.code + " " + b.msg);
                    }
                    checkedDesigns++;
                }
            }
            Assert.That(checkedDesigns, Is.GreaterThanOrEqualTo(missions));
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }
    }
}
