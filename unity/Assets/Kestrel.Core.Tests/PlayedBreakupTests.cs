// The played flight breaks up a design that cannot do the job. Stock still flies.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PlayedBreakupTests
    {
        [Test]
        public void AHeavyRocketTearsOnThePad()
        {
            var early = PlayedBreakup.At(new PlayedBreakup.Sample { T = 0.5, AltM = 0, LiftoffTwr = 0.4, Thrusting = true, QLimPa = 35000 });
            Assert.IsNull(early, "the hold gives the engines a moment");
            var hit = PlayedBreakup.At(new PlayedBreakup.Sample { T = 2.1, AltM = 0.2, LiftoffTwr = 0.4, Thrusting = true, QLimPa = 35000 });
            Assert.IsNotNull(hit);
            Assert.AreEqual("structural-maxq", hit.kind);
            StringAssert.Contains("too heavy", hit.kid);
        }

        [Test]
        public void ThickAirAndAHardPushBreakTheStack()
        {
            var q = PlayedBreakup.At(new PlayedBreakup.Sample { T = 50, AltM = 10000, QPa = 80000, QLimPa = 20000, LiftoffTwr = 1.4, Thrusting = true });
            Assert.AreEqual("structural-maxq", q.kind);
            Assert.AreEqual("q", q.cause);
            var g = PlayedBreakup.At(new PlayedBreakup.Sample
            {
                T = 80, AltM = 20000, AccelMps2 = 24 * FailureModel.G0, QLimPa = 80000, LiftoffTwr = 1.4, Thrusting = true,
            });
            Assert.AreEqual("overg", g.kind);
            var stockAir = PlayedBreakup.At(new PlayedBreakup.Sample
            {
                T = 60, AltM = 12000, QPa = 73000, AccelMps2 = 14 * FailureModel.G0, QLimPa = 35000, LiftoffTwr = 1.4, Thrusting = true,
            });
            Assert.IsNull(stockAir, "stock's 73 kPa and 14 g stay inside the played caps");
        }

        [Test]
        public void AShortDesignBreaksAtBurnoutAndASoundOneDoesNot()
        {
            Assert.IsNull(PlayedBreakup.Short(100, 0.05));
            var miss = PlayedBreakup.Short(100, -0.2);
            Assert.AreEqual("structural-maxq", miss.kind);
            StringAssert.Contains("Not enough speed", miss.kid);
            var fell = PlayedBreakup.Impact(40);
            StringAssert.Contains("broke apart", fell.kid);
        }

        [Test]
        public void StockFliesAndAWeakStructureComesApart()
        {
            var cat = Golden.Catalog;
            var stock = cat.Preset("stock");
            Assert.IsTrue(PlayedBreakup.Arcade(stock));
            var flown = Fly(cat, stock, 160);
            Assert.IsNull(flown.Breakup, flown.Note);
            Assert.GreaterOrEqual(flown.Margin, 0, "stock has the speed, so burnout is not a breakup");

            var weak = stock.Clone();
            weak.safetyFactor = 0.8;
            var broke = Fly(cat, weak, 160);
            Assert.IsNotNull(broke.Breakup, broke.Note);
            Assert.AreEqual("structural-maxq", broke.Breakup.kind);
            Assert.AreEqual("q", broke.Breakup.cause);
            Assert.Less(broke.T, 160);

            var starved = stock.Clone();
            starved.stages[0].propMass *= 0.15;
            if (starved.stages.Count > 1) starved.stages[1].propMass *= 0.15;
            var empty = Fly(cat, starved, 200);
            Assert.IsNotNull(empty.Breakup, empty.Note);
            Assert.Less(empty.Margin, 0);
        }

        [Test]
        public void AVersion3BreakupStopsThePlayedFlight()
        {
            var cat = Golden.Catalog;
            var d = cat.Preset("scout").Clone();
            d.version = 3;
            d.tier = "kid";
            d.safetyFactor = 1.1;
            for (int i = 0; i < d.stages.Count; i++) d.stages[i].material = "al-li";
            var input = Stack.SimInputs(cat, d, 0.2);
            input.opts.tMax = 180;
            Assert.IsNotNull(input.opts.monitor);
            var sim = new AscentSim(input.stages, input.opts);
            bool saw = false;
            while (!sim.Done && sim.T < 180)
            {
                sim.Tick();
                if (Has(sim, "failure")) { saw = true; break; }
            }
            Assert.IsTrue(saw, "the weak stack records a failure");
            Assert.IsFalse(sim.Done, "the headless coast has not finished");
            sim.BreakUp(null);
            Assert.IsTrue(sim.Done);
            var result = sim.Finish();
            Assert.IsNotNull(result.failure);
            Assert.AreEqual("structural-maxq", result.failure.kind);
            Assert.AreEqual("failure", result.outcome);
        }

        [Test]
        public void AnArcadeBreakupIsOnTheResult()
        {
            var cat = Golden.Catalog;
            var input = Stack.SimInputs(cat, cat.Preset("stock"), 0.2);
            Assert.IsNull(input.opts.monitor);
            var sim = new AscentSim(input.stages, input.opts);
            sim.Tick();
            var ev = PlayedBreakup.Impact(sim.T);
            sim.BreakUp(ev);
            var result = sim.Finish();
            Assert.AreSame(ev, result.failure);
            Assert.AreEqual("failure", result.outcome);
            Assert.IsTrue(sim.Done);
        }

        struct Run
        {
            public FailureEvent Breakup;
            public double T, Margin;
            public string Note;
        }

        static Run Fly(Catalog cat, Design d, double tMax)
        {
            var input = Stack.SimInputs(cat, d, 0.2);
            input.opts.tMax = tMax;
            double ideal = Headless.StackDeltaV(input.stages, input.opts.payload).total;
            double required = Stack.RequiredDeltaV(cat, d.payloadOrbit).required;
            double margin = PlayedBreakup.Margin(ideal, required);
            double qLim = FailureMonitor.StructuralQLimPa(cat, d);
            var sim = new AscentSim(input.stages, input.opts);
            double liftoff = input.stages.Count > 0 && sim.Y[4] > 1 ? input.stages[0].thrustSL / (sim.Y[4] * Atmosphere.G0) : 0;
            var serial = Stack.SerialStages(cat, d).stages;
            int mecoStage = serial.FindIndex(s => s.role == "core" && s.index == 0) + 1;
            bool meco = false;
            FailureEvent hit = null;
            while (!sim.Done && sim.T < tMax && hit == null)
            {
                int before = sim.Events.Count;
                sim.Tick();
                for (int i = before; i < sim.Events.Count; i++)
                    if (sim.Events[i].type == "burnout" && sim.Events[i].stage == mecoStage) meco = true;
                if (Has(sim, "impact")) hit = PlayedBreakup.Impact(sim.T);
                else if (meco && margin < 0) hit = PlayedBreakup.Short(sim.T, margin);
                else if (PlayedBreakup.Arcade(d))
                {
                    double mass = sim.Y[4];
                    double accel = mass > 1 ? sim.LastThrust / mass : 0;
                    hit = PlayedBreakup.At(new PlayedBreakup.Sample
                    {
                        T = sim.T,
                        AltM = Math.Sqrt(sim.Y[0] * sim.Y[0] + sim.Y[1] * sim.Y[1]) - Gravity.R_EARTH,
                        QPa = sim.LastQ,
                        AccelMps2 = accel,
                        QLimPa = qLim,
                        LiftoffTwr = liftoff,
                        Thrusting = sim.Thrusting,
                    });
                }
            }
            double g = sim.Y[4] > 1 ? sim.LastThrust / sim.Y[4] / FailureModel.G0 : 0;
            return new Run
            {
                Breakup = hit,
                T = sim.T,
                Margin = margin,
                Note = "t=" + sim.T.ToString("0.0") + " q=" + sim.LastQ.ToString("0") + " qLim=" + qLim.ToString("0")
                    + " g=" + g.ToString("0.00") + " margin=" + margin.ToString("0.00") + " twr=" + liftoff.ToString("0.00"),
            };
        }

        static bool Has(AscentSim sim, string type)
        {
            for (int i = sim.Events.Count - 1; i >= 0; i--)
                if (sim.Events[i].type == type) return true;
            return false;
        }
    }
}
