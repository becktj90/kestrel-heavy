// Review follow-ups to #278 (Kestrel Physics): event-order contract, first-peak max-Q,
// vacuum emits no max-Q, the mission setup wires the fairing gate, and a throwing sink
// cannot change the run.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public partial class FlightSimSinkTests
    {
        /// <summary>
        /// Small first stage peaks low, a weak sustainer lets q fall more than 2%, then a hot third stage
        /// drives a higher second peak. The hook keeps the first peak; SimResult.maxQ is the higher one.
        /// </summary>
        static SimStage[] TwoPeakStack() => new[]
        {
            new SimStage { thrustSL = 1.6e6, thrustVac = 1.7e6, ispSL = 280, ispVac = 300, propMass = 20000, dryMass = 2000, diameter = 3, cd = 0.3 },
            new SimStage { thrustSL = 30000, thrustVac = 40000, ispSL = 280, ispVac = 300, propMass = 250, dryMass = 300, diameter = 3, cd = 0.3 },
            new SimStage { thrustSL = 9.0e6, thrustVac = 9.5e6, ispSL = 280, ispVac = 300, propMass = 50000, dryMass = 3000, diameter = 3, cd = 0.3 },
        };

        [Test]
        public void MaxQ_FiresAtTheFirstPeak_AndAHigherSecondPeakOnlyMovesTheResult()
        {
            var opts = new SimOptions { dt = Dt, tMax = 600, cutoff = false };
            var log = new HookLog();
            var r = Headless.Simulate(TwoPeakStack(), opts, log);
            var bare = Headless.Simulate(TwoPeakStack(), new SimOptions { dt = Dt, tMax = 600, cutoff = false });
            Same(bare, r);

            Assert.AreEqual(1, log.MaxQCount, string.Join(" ", log.Names()));
            Assert.Less(log.MaxQQ, r.maxQ, "a later, higher peak raises SimResult.maxQ: first " + log.MaxQQ.ToString("0", CultureInfo.InvariantCulture) + " Pa, run max " + r.maxQ.ToString("0", CultureInfo.InvariantCulture) + " Pa");
            Assert.Less(log.MaxQT, r.maxQTime);
            // The hook fired during the flight, before the stage that makes the second peak lit.
            var names = log.Names();
            int maxqAt = names.IndexOf("maxq");
            int thirdLit = -1;
            for (int i = 0; i < log.IgnitionStages.Count; i++) if (log.IgnitionStages[i] == 3) thirdLit = i;
            Assert.GreaterOrEqual(thirdLit, 0, "stage 3 lit");
            Assert.Less(log.MaxQT, log.IgnitionTimes[thirdLit]);
            Assert.Less(log.MaxQT, r.maxQTime);
            AssertContract(names);
        }

        [Test]
        public void Vacuum_EmitsNoMaxQ_AndSoNoFairing()
        {
            var opts = new SimOptions { dt = Dt, tMax = 800, vacuum = true, fairingJettisonAltM = FairingM };
            var log = new HookLog();
            var r = Headless.Simulate(Headless.MediumLifter(), opts, log);
            Assert.AreEqual(0, r.maxQ, 0);
            Assert.AreEqual(0, log.MaxQCount);
            Assert.AreEqual(0, log.FairingCount, "the fairing gate waits for max-Q");
            Assert.AreEqual(0, Count(r, "fairing"));
            Assert.Greater(log.SepCount, 0);
            AssertContract(log.Names());
        }

        [Test]
        public void EventOrder_HoldsAcrossPresetsAndFailures()
        {
            var cat = Golden.Catalog;
            int flights = 0;
            foreach (var id in new[] { "stock", "scout", "kid", "super-heavy" })
            {
                var d = cat.Preset(id);
                Assert.IsNotNull(d, id);
                var setup = MissionFlightSetup.Build(cat, null, d, 0.2);
                var log = new HookLog();
                Headless.Simulate(setup.Stages, setup.Options, log);
                AssertContract(log.Names());
                flights++;
            }
            var weak = FlyWeakScout();
            AssertContract(weak.Log.Names());
            var log2 = new HookLog();
            Headless.Simulate(Headless.MediumLifter(), Opt(true), log2);
            AssertContract(log2.Names());
            CollectionAssert.AreEqual(new[] { "ignition", "liftoff", "maxq", "sep", "ignition", "fairing" }, log2.Names());
            Assert.AreEqual(4, flights);
        }

        [Test]
        public void MissionSetup_WiresTheDesignJettisonAltitude_AndTheFairingHookFires()
        {
            var cat = Golden.Catalog;
            var campaign = Campaign.Load(Golden.Root);
            int fired = 0, flights = 0;
            foreach (var w in CampaignWinners())
            {
                var mission = campaign.ById(w.missionId);
                Assert.IsNotNull(mission, w.missionId);
                var setup = MissionFlightSetup.Build(cat, mission, w.design, MissionFlightSetup.BatchDt);
                double want = MissionFlightSetup.FairingJettisonAltM(w.design);
                Assert.AreEqual(want, setup.Options.fairingJettisonAltM, 0, w.missionId);
                if (w.design.fairingLength > 0.2 && w.design.fairingDiameter > 0.1)
                    Assert.AreEqual((w.design.fairingJettisonAltitudeKm > 0 ? w.design.fairingJettisonAltitudeKm : 110) * 1000, want, 0, w.missionId);

                var log = new HookLog();
                var flown = Headless.Simulate(setup.Stages, setup.Options, log);
                setup.Options.fairingJettisonAltM = 0;
                var without = Headless.Simulate(setup.Stages, setup.Options);
                SameExceptFairing(without, flown);
                AssertContract(log.Names());
                Assert.AreEqual(Count(flown, "fairing"), log.FairingCount, w.missionId);
                if (log.FairingCount == 1)
                {
                    fired++;
                    var fair = Find(flown, "fairing", 0);
                    Assert.GreaterOrEqual(fair.alt.Value, want, w.missionId);
                    Assert.GreaterOrEqual(fair.t, flown.maxQTime >= log.MaxQT ? log.MaxQT : flown.maxQTime, w.missionId);
                }
                flights++;
            }
            Assert.GreaterOrEqual(flights, 10);
            Assert.GreaterOrEqual(fired, flights / 2, "most campaign winners climb past their jettison altitude");

            var bare = cat.Preset("stock").Clone();
            Assert.AreEqual(110000, MissionFlightSetup.FairingJettisonAltM(WithJettison(bare, 0)), 0, "missing altitude is 110 km");
            Assert.AreEqual(95000, MissionFlightSetup.FairingJettisonAltM(WithJettison(bare, 95)), 0);
            var none = bare.Clone();
            none.fairingLength = 0;
            Assert.AreEqual(0, MissionFlightSetup.FairingJettisonAltM(none), 0, "no fairing, no gate");
            Assert.AreEqual(0, MissionFlightSetup.FairingJettisonAltM(null), 0);
        }

        [Test]
        public void ThrowingSink_LeavesEveryRunBitIdentical_AndLogsOncePerHook()
        {
            var messages = new List<string>();
            var keep = GuardedFlightSink.Log;
            GuardedFlightSink.Log = messages.Add;
            try
            {
                // Ascent with staging and fairing.
                var opts = Opt(true);
                var none = Headless.Simulate(Headless.MediumLifter(), opts);
                var thrower = new ThrowingSink();
                var loud = Headless.Simulate(Headless.MediumLifter(), opts, thrower);
                Same(none, loud);
                Assert.Greater(thrower.Calls, 4);

                // Live ticking with a throwing sink across a failure and its impact.
                var weak = FlyWeakScout();
                var cat = Golden.Catalog;
                var design = cat.Preset("scout").Clone();
                design.version = 3; design.tier = "kid"; design.safetyFactor = 1.1;
                for (int i = 0; i < design.stages.Count; i++) design.stages[i].material = "al-li";
                var input = Stack.SimInputs(cat, design, 0.2);
                input.opts.tMax = 2000;
                var broken = new ThrowingSink();
                var sim = new AscentSim(input.stages, input.opts) { Sink = broken };
                Assert.AreSame(broken, sim.Sink, "Sink returns the caller's sink, not the guard");
                int guard = 0;
                while (!sim.Done && guard++ < 20000) sim.Tick();
                var brokenResult = sim.Finish();
                Same(weak.Result, brokenResult);
                Assert.AreEqual("failure", brokenResult.outcome);

                // Played break-up with an engine failure.
                var a = new AscentSim(Headless.MediumLifter(), new SimOptions { dt = Dt, tMax = 30 });
                var b = new AscentSim(Headless.MediumLifter(), new SimOptions { dt = Dt, tMax = 30 }) { Sink = new ThrowingSink() };
                foreach (var s in new[] { a, b })
                {
                    while (!s.Done && s.T < 2) s.Tick();
                    s.BreakUp(new FailureEvent { t = s.T, kind = "engine-out", cause = "engine-out", engineIndex = 3, stage = 1 });
                }
                Same(a.Finish(), b.Finish());

                // Recovery.
                var rbare = RecoverySim.Fly(Craft(9000, "barge"), 4500, 20000, 150, 25, RecoveryStick.Assisted);
                var rloud = RecoverySim.Fly(Craft(9000, "barge"), 4500, 20000, 150, 25, RecoveryStick.Assisted, new ThrowingSink());
                Assert.AreEqual(rbare.End.Sink, rloud.End.Sink, 0);
                Assert.AreEqual(rbare.End.T, rloud.End.T, 0);
                Assert.AreEqual(rbare.MissM, rloud.MissM, 0);
                Assert.AreEqual(rbare.Cause, rloud.Cause);
                Assert.AreEqual(rbare.Marks.Length, rloud.Marks.Length);
                var rcrash = RecoverySim.Fly(Craft(400, "barge"), 4500, 20000, 150, 25, RecoveryStick.Assisted);
                var rcrashLoud = RecoverySim.Fly(Craft(400, "barge"), 4500, 20000, 150, 25, RecoveryStick.Assisted, new ThrowingSink());
                Assert.AreEqual(rcrash.End.Sink, rcrashLoud.End.Sink, 0);
                Assert.AreEqual(rcrash.Cause, rcrashLoud.Cause);
            }
            finally
            {
                GuardedFlightSink.Log = keep;
            }

            // One message per hook type per guarded sink, never one per call.
            var perSinkHook = new HashSet<string>();
            foreach (var m in messages) Assert.IsTrue(m.Contains("ThrowingSink.On"), m);
            var hooks = new HashSet<string>();
            foreach (var m in messages)
            {
                int at = m.IndexOf(".On", StringComparison.Ordinal);
                hooks.Add(m.Substring(at + 1, m.IndexOf(' ', at) - at - 1));
            }
            foreach (var h in new[] { "OnIgnition", "OnLiftoff", "OnMaxQ", "OnStageSeparation", "OnFairingSeparation", "OnFailure", "OnImpact", "OnEngineFailure", "OnRecoveryPhase", "OnTouchdown" })
                Assert.IsTrue(hooks.Contains(h), h + " threw and was logged: " + string.Join(", ", hooks));

            var single = (GuardedFlightSink)GuardedFlightSink.Wrap(new ThrowingSink());
            var once = new List<string>();
            var keep2 = GuardedFlightSink.Log;
            GuardedFlightSink.Log = once.Add;
            try
            {
                for (int i = 0; i < 5; i++) single.OnIgnition(1, i);
                single.OnLiftoff(0, new FlightVec(0, 0));
            }
            finally { GuardedFlightSink.Log = keep2; }
            Assert.AreEqual(2, once.Count, "first throw per hook type only");
            Assert.AreEqual(6, single.Faults);
            Assert.AreSame(single, GuardedFlightSink.Wrap(single));
            Assert.IsNull(GuardedFlightSink.Wrap(null));
        }

        /// <summary>The contract on IFlightSimSink, checked on a hook-name log.</summary>
        static void AssertContract(List<string> n)
        {
            string all = string.Join(" ", n);
            Assert.Greater(n.Count, 0);
            Assert.AreEqual("ignition", n[0], all);
            int lift = n.IndexOf("liftoff");
            Assert.AreEqual(lift, n.LastIndexOf("liftoff"), all);
            Assert.LessOrEqual(n.FindAll(x => x == "maxq").Count, 1, all);
            Assert.LessOrEqual(n.FindAll(x => x == "fairing").Count, 1, all);
            Assert.LessOrEqual(n.FindAll(x => x == "failure").Count, 1, all);
            Assert.LessOrEqual(n.FindAll(x => x == "impact").Count, 1, all);
            for (int i = 0; i < n.Count; i++)
            {
                if (n[i] == "sep")
                {
                    Assert.Greater(i, lift, all);
                    if (i + 1 < n.Count) Assert.AreEqual("ignition", n[i + 1], "separation is followed by the next ignition: " + all);
                }
                if (n[i] == "maxq" && lift >= 0) Assert.Greater(i, lift, all);
                if (n[i] == "fairing") Assert.Greater(i, n.IndexOf("maxq"), all);
                if (n[i] == "engine") Assert.AreEqual("failure", n[i - 1], all);
                if (n[i] == "ignition" && i > 0) Assert.AreEqual("sep", n[i - 1], all);
            }
            int impact = n.IndexOf("impact");
            if (impact >= 0) Assert.AreEqual(n.Count - 1, impact, "impact is the last ascent call: " + all);
        }

        static void SameExceptFairing(SimResult a, SimResult b)
        {
            Assert.AreEqual(a.outcome, b.outcome);
            Assert.AreEqual(a.time, b.time, 0);
            Assert.AreEqual(a.maxQ, b.maxQ, 0);
            Assert.AreEqual(a.x, b.x, 0);
            Assert.AreEqual(a.y, b.y, 0);
            Assert.AreEqual(a.vx, b.vx, 0);
            Assert.AreEqual(a.vy, b.vy, 0);
            Assert.AreEqual(a.m, b.m, 0);
            Assert.AreEqual(a.orbit.periAlt, b.orbit.periAlt, 0);
            Assert.AreEqual(a.orbit.apoAlt, b.orbit.apoAlt, 0);
            var rest = b.events.FindAll(e => e.type != "fairing");
            Assert.AreEqual(a.events.Count, rest.Count);
            for (int i = 0; i < rest.Count; i++)
            {
                Assert.AreEqual(a.events[i].type, rest[i].type);
                Assert.AreEqual(a.events[i].t, rest[i].t, 0);
            }
        }

        static Design WithJettison(Design d, double km)
        {
            var c = d.Clone();
            if (!(c.fairingLength > 0.2)) c.fairingLength = 5;
            if (!(c.fairingDiameter > 0.1)) c.fairingDiameter = 3;
            c.fairingJettisonAltitudeKm = km;
            return c;
        }

        struct Winner { public string missionId; public Design design; }

        static IEnumerable<Winner> CampaignWinners()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            foreach (JsonObject row in file.Arr("missions"))
                yield return new Winner { missionId = row.Str("id"), design = Design.FromJson((JsonObject)row.Arr("designs")[0]) };
        }

        sealed class ThrowingSink : IFlightSimSink
        {
            public int Calls;
            Exception Boom(string hook) { Calls++; return new InvalidOperationException("vfx broke in " + hook); }
            public void OnIgnition(int stage, double t) => throw Boom("ignition");
            public void OnLiftoff(double t, FlightVec padPos) => throw Boom("liftoff");
            public void OnMaxQ(double q, double t) => throw Boom("maxq");
            public void OnStageSeparation(int stage, FlightVec pos, FlightVec vel) => throw Boom("sep");
            public void OnFairingSeparation(double t, FlightVec pos, FlightVec vel) => throw Boom("fairing");
            public void OnRecoveryPhase(RecoveryPhase phase, double t) => throw Boom("phase");
            public void OnTouchdown(double vz, double tilt, FlightSurface surface) => throw Boom("touchdown");
            public void OnCrash(string kind, FlightVec pos, FlightVec vel, double energy) => throw Boom("crash");
            public void OnFailure(string kind, string cause, int engineIndex, FlightVec pos, FlightVec vel, double t) => throw new ArgumentException("failure hook");
            public void OnEngineFailure(int engineIndex, string mode) => throw Boom("engine");
            public void OnImpact(double speed, FlightVec pos) => throw Boom("impact");
        }
    }
}
