// #316: a starter must never break its own limits. KH-2's starter flew 6.89 g (batch) / 6.92 g (live dt)
// against its own 6 g guidance limit: one gg-kerolox-small cannot throttle below 60%, so near burnout the
// g limiter is on the floor and cannot hold 6 g. The payload allows 12 g and the failure monitor's cap is
// 8 g, so Arcade raised the starter's own limit to 9 g (HomeAutoBuild.HundredKmGLimit).
// Every board starter, KH-1 to KH-20, flies seeds 1-5 at the batch and live steps and stays inside its own
// g limit (guidance gLimit, 2% transient allowance), the monitor's g cap, and its structural q limit,
// with no failure event.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterLimitsTests
    {
        const double Transient = 1.02;

        [Test]
        public void Kh2StarterOwnGLimitIsNineG()
        {
            Assert.AreEqual(9.0, HomeAutoBuild.HundredKmGLimit);
        }

        [Test]
        public void EveryStarterFliesInsideItsOwnLimitsOnSeedsOneToFive()
        {
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            var hangar = HangarData.LoadFromRepo(Golden.Root);
            var campaign = Campaign.Load(Golden.Root);
            Assert.AreEqual(20, campaign.Missions.Count);
            Assert.That(campaign.Seeds.Count, Is.GreaterThanOrEqualTo(5));
            var broken = new List<string>();
            for (int index = 1; index <= 20; index++)
            {
                var mission = campaign.Mission(index);
                var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
                Assert.IsNotNull(choice?.Design, "KH-" + index);
                var design = Design.FromJson(choice.Design);
                Assert.IsTrue(FailureMonitor.Engages(design), "KH-" + index + " starter is judged by the failure monitor");
                double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
                bool ownG = design.guidance != null && design.guidance.gLimitSet && design.guidance.gLimit > 0;
                foreach (double dt in new[] { MissionFlightSetup.BatchDt, MissionFlightSetup.LiveDt })
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var setup = MissionFlightSetup.Build(catalog, mission, design, dt);
                        var monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(design), campaign.Seeds[i]);
                        monitor.Gentle = false;
                        setup.Options.monitor = monitor;
                        var sim = Headless.Simulate(setup.Stages, setup.Options);
                        string tag = "KH-" + index + " dt " + dt.ToString("0.000") + " seed " + campaign.Seeds[i];
                        if (sim.failure != null)
                        {
                            broken.Add(tag + ": " + sim.failure.kind + " at T+" + sim.failure.t.ToString("0.0") + " s");
                            continue;
                        }
                        if (ownG && sim.maxG > design.guidance.gLimit * Transient)
                            broken.Add(tag + ": " + sim.maxG.ToString("0.00") + " g over its own " + design.guidance.gLimit.ToString("0.0") + " g");
                        var gRisk = sim.risks.Find(r => r.kind == "overg");
                        if (gRisk != null && gRisk.limit > 0 && sim.maxG > gRisk.limit)
                            broken.Add(tag + ": " + sim.maxG.ToString("0.00") + " g over the monitor's " + gRisk.limit.ToString("0.0") + " g");
                        var q = sim.risks.Find(r => r.kind == "structural-maxq");
                        if (q != null && q.value >= qLim)
                            broken.Add(tag + ": q " + (q.value / 1000).ToString("0.0") + " kPa at or over q_lim " + (qLim / 1000).ToString("0.0") + " kPa");
                    }
                }
            }
            Assert.IsEmpty(broken, string.Join("; ", broken));
        }
    }
}
