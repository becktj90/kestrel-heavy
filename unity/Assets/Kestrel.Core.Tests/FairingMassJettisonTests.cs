// #335: the fairing shell leaves the stack at the "fairing" event (JS parity: web/js/physics/headless.js
// drops fairingMass at the fairing-jettison event). Before, the C# event was a marker only and the fairing's
// mass rode with the payload for the whole burn, so the live pad, FlightBatch, LaunchPredictor and designer
// predict all flew heavy after jettison. The drop follows the (possibly heat-gated) event; a design with no
// fairing has fairingMassKg 0 and is unchanged.

using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FairingMassJettisonTests
    {
        Catalog catalog;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        static JsonObject Winner(string id)
        {
            foreach (JsonObject row in Golden.File("campaign-winners").Arr("missions"))
                if (row.Str("id") == id) return (JsonObject)row.Arr("designs")[0];
            Assert.Fail("no winner " + id);
            return null;
        }

        SimResult Fly(CampaignMission m, Design d, double dt, double fairingMassKgOverride = double.NaN)
        {
            var setup = MissionFlightSetup.Build(catalog, m, d, dt);
            if (!double.IsNaN(fairingMassKgOverride)) setup.Options.fairingMassKg = fairingMassKgOverride;
            return Headless.Simulate(setup.Stages, setup.Options);
        }

        [Test]
        public void FairingMass_DropsAtTheFairingEvent()
        {
            var m = campaign.ById("m5");
            var design = Design.FromJson(Winner("m5"));
            double fairing = Mass.FairingMass(design);
            Assert.Greater(fairing, 0, "m5 carries a fairing");
            var setup = MissionFlightSetup.Build(catalog, m, design, MissionFlightSetup.BatchDt);
            Assert.AreEqual(fairing, setup.Options.fairingMassKg, 1e-9, "SimInputs wires the fairing mass");
            var flown = Headless.Simulate(setup.Stages, setup.Options);
            SimEvent ev = null;
            foreach (var e in flown.events) if (e.type == "fairing") ev = e;
            Assert.IsNotNull(ev, "a fairing event fires");
            Assert.IsTrue(ev.mass.HasValue, "the event carries the jettisoned mass");
            Assert.AreEqual(fairing, ev.mass.Value, 1e-9, "the event drops the whole shell");
            Assert.AreEqual("orbit", flown.outcome, "m5 still reaches orbit");
            // Old behavior: the same flight with the mass wired to 0 (marker-only event).
            var old = Fly(m, design, MissionFlightSetup.BatchDt, 0);
            Assert.Greater(flown.departureDv, old.departureDv + 20, "dropping the shell buys departure Δv");
            // finalMass itself falls by the shell minus the propellant saved; count the shell back in.
            Assert.Greater(flown.finalMass + fairing, old.finalMass + 100, "the upper stage keeps more propellant net of the shell");
        }

        [Test]
        public void NoFairing_DesignIsUnchanged()
        {
            var m = campaign.ById("m5");
            var design = Design.FromJson(Winner("m5"));
            design.fairingLength = 0; design.fairingDiameter = 0;
            Assert.AreEqual(0, Mass.FairingMass(design), 1e-9);
            var setup = MissionFlightSetup.Build(catalog, m, design, MissionFlightSetup.BatchDt);
            Assert.AreEqual(0, setup.Options.fairingMassKg, 1e-9, "no fairing, no mass to drop");
            Assert.AreEqual(0, setup.Options.fairingJettisonAltM, 1e-9, "no fairing, no event");
            var flown = Headless.Simulate(setup.Stages, setup.Options);
            foreach (var e in flown.events) Assert.AreNotEqual("fairing", e.type, "a bare stack never emits a fairing event");
        }
    }
}
