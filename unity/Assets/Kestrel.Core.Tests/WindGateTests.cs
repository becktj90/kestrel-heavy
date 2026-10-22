// #338 (Kestrel Physics): the briefing-wind gate. With the wind-shear ease gone (no 12 s, 85% throttle, -0.5° pitch
// call that switched on only in wind), every board starter, after its row's one move (fly, Fix it, or the strength
// restore), must win the live pad in the seed-1 briefing wind: seed 1, FlightDriver's 1/60 s frames at LiveDt, the
// shared director on default settings, played checks, judged by the live report card's gates.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class WindGateTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        [Test]
        public void BoardStarter_FixItOrOneStrengthEdit_WinsTheLivePadInSeedOneWind()
        {
            var misses = new List<string>();
            int seen = 0;
            foreach (var (mission, design, mode) in BoardStarters())
            {
                var live = LivePadLoop.Fly(catalog, mission, design, true);
                if (!LivePadLoop.Passes(catalog, mission, design, live))
                    misses.Add(mission.Id + " (" + mode + ") live seed-1 wind " + LivePadLoop.Text(live));
                seen++;
            }
            Assert.AreEqual(18, seen, "KH-3 through KH-20");
            Assert.That(misses, Is.Empty, string.Join(" | ", misses));
        }

        /// <summary>No wind-shear call is offered any more, in wind or calm (the ease is gone, #338).</summary>
        [Test]
        public void NoWindShearCall_InTheSeedOneWind()
        {
            int windy = 0;
            foreach (var (mission, design, _) in BoardStarters())
            {
                ulong hash = FlightBatch.DesignHash(design);
                var card = MissionBriefing.Load(Golden.Root, mission.Index, hash, 1);
                if (card?.Wind == null || card.Wind.Calm) continue;
                double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
                if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
                var monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, 1) : null;
                var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, card.Wind, FailureMonitor.StructuralQLimPa(catalog, design),
                    FailureMonitor.GentleTier(design), target, hash, 1, MissionFlightSetup.DefaultAutoAssist, false, false, card.Bonuses);
                var site = new MissionFlightSite { Wind = card.Wind };
                var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
                if (monitor != null) setup.Options.monitor = monitor;
                var sim = new AscentSim(setup.Stages, setup.Options);
                var directed = new DirectedFlight(sim, director, setup.Options);
                int guard = 0;
                while (!sim.Done && guard++ < 2000000)
                {
                    directed.Step();
                    Assert.IsFalse(director.IsOpen(AscentCall.WindShear), mission.Id + " offers no wind-shear call");
                }
                Assert.IsTrue(sim.Done, mission.Id + " flight ends");
                Assert.AreEqual(AscentGrade.None, director.GradeOf(AscentCall.WindShear), mission.Id + " never resolves a wind-shear call");
                windy++;
            }
            Assert.That(windy, Is.GreaterThan(0), "some starters fly a windy seed-1 briefing");
        }

        List<(CampaignMission, Design, string)> BoardStarters()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json")));
            var list = new List<(CampaignMission, Design, string)>();
            foreach (JsonObject row in file.Arr("missions"))
            {
                var mission = campaign.ById(row.Str("id"));
                Assert.IsNotNull(mission, row.Str("id"));
                var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
                Assert.IsNotNull(choice, mission.Id);
                var design = Design.FromJson(choice.Design);
                string mode = row.Str("mode");
                if (mode == "fix")
                {
                    var json = JsonMut.CloneObject(choice.Design);
                    Readout.Reference(catalog, json);
                    var report = StepperModel.Evaluate(catalog, json);
                    StepCheck check = null;
                    for (int i = 0; i < report.Checks.Length && check == null; i++)
                        if (report.Checks[i] != null && !string.IsNullOrEmpty(report.Checks[i].Code)) check = report.Checks[i];
                    Assert.IsNotNull(check, mission.Id + " has a warning for Fix it");
                    StepperFix.Apply(catalog, json, check);
                    design = Design.FromJson(json);
                }
                else if (mode == "strength") design.safetyFactor = row.Num("restoreSafetyFactor");
                else Assert.AreEqual("fly", mode, mission.Id);
                list.Add((mission, design, mode));
            }
            return list;
        }
    }
}
