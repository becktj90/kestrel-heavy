// Skip to next stage, recovery side (DECISIONS #78): the booster advances by the ascent's skipped seconds at
// the same fixed recovery steps, and the skip stops at its next burn phase (entry burn, landing burn) or
// touchdown. An expendable stage with no landing fuel has no burn to skip to.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StageSkipRecoveryTests
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

        [SetUp]
        public void Clear() => RecoveryLog.Clear();

        [TearDown]
        public void Drop() => RecoveryLog.Clear();

        sealed class Started
        {
            public BoosterLanding Landing;
            public double SepT;
        }

        /// <summary>Fly the live pad loop (seed-1 briefing wind, auto-assist on) until the first stage separates and its landing starts.</summary>
        Started BeginBooster(int index)
        {
            const double frame = 1.0 / 60.0;
            var mission = campaign.Mission(index);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            ulong hash = FlightBatch.DesignHash(design);
            ulong seed = 1;
            var card = MissionBriefing.Load(Golden.Root, index, hash, seed);
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            FailureMonitor monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, true, false, false, card?.Bonuses);
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            var checks = new PlayedChecks(catalog, design, setup.Stages, opts, sim);
            var directed = new DirectedFlight(sim, director, opts);
            double omegaAir = opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;
            var landing = BoosterLanding.Arm(catalog, design);
            double clock = 0, prevDown = 0, prevAlt = 0, attitude = 90;
            bool hasAttitude = false;
            double down = 0, alt = 0, speed = 0;
            int frames = 0;
            while (frames++ < 200000)
            {
                clock += frame;
                int guard = 0;
                while (sim.T < clock && !sim.Done && guard++ < 64)
                {
                    int before = sim.Events.Count;
                    directed.Step();
                    checks.NoteEvents(sim, before);
                    if (checks.StopForBreakup(sim)) break;
                }
                double x = sim.Y[0], y = sim.Y[1], r = Math.Sqrt(x * x + y * y);
                double padAngle = Math.PI / 2 + omegaAir * sim.T;
                down = Gravity.R_EARTH * (Math.Atan2(y, x) - padAngle);
                alt = r - Gravity.R_EARTH;
                double vx = sim.Y[2] + omegaAir * y, vy = sim.Y[3] - omegaAir * x;
                speed = Math.Sqrt(vx * vx + vy * vy);
                double dx = down - prevDown, dy = alt - prevAlt;
                if (dx * dx + dy * dy > 1e-6 && alt > 20) { attitude = Math.Atan2(dy, dx) * 180 / Math.PI; hasAttitude = true; }
                prevDown = down; prevAlt = alt;
                if (landing.Armed && !landing.Started)
                {
                    if (landing.Watch(sim.T, sim.StageIdx, down, alt, speed, attitude, hasAttitude, sim.Done))
                        return new Started { Landing = landing, SepT = sim.T };
                }
                if (sim.Done) break;
            }
            Assert.Fail("KH-" + index + " never separates (ascent ends first)");
            return null;
        }

        static void AssertSameSample(RecoverySample a, RecoverySample b, string what)
        {
            Assert.That(b.Phase, Is.EqualTo(a.Phase), what + " phase");
            Assert.That(BitConverter.DoubleToInt64Bits(b.T), Is.EqualTo(BitConverter.DoubleToInt64Bits(a.T)), what + " T");
            Assert.That(BitConverter.DoubleToInt64Bits(b.Alt), Is.EqualTo(BitConverter.DoubleToInt64Bits(a.Alt)), what + " alt");
            Assert.That(BitConverter.DoubleToInt64Bits(b.Speed), Is.EqualTo(BitConverter.DoubleToInt64Bits(a.Speed)), what + " speed");
            Assert.That(BitConverter.DoubleToInt64Bits(b.Prop), Is.EqualTo(BitConverter.DoubleToInt64Bits(a.Prop)), what + " prop");
        }

        [Test]
        public void Kh7PlannedBooster_SkipToEntryBurnMatchesLiveStepping()
        {
            var live = BeginBooster(7);
            Assert.AreEqual(BoosterFate.Planned, live.Landing.Fate, "KH-7's starter picks Haven");
            Assert.That(StageSkip.NextRecoveryTarget(live.Landing), Is.EqualTo(StageSkipTarget.EntryBurn), "a planned landing has an entry burn to skip to");
            int n = 0;
            while (live.Landing.Sample().Phase != RecoveryPhase.Entry && n++ < 100000)
                live.Landing.Step(1.0 / 60.0, RecoveryStick.Assisted);
            Assert.That(live.Landing.Sample().Phase, Is.EqualTo(RecoveryPhase.Entry), "the live booster enters its entry burn");
            var liveSample = live.Landing.Sample();

            var skipped = BeginBooster(7);
            Assert.That(skipped.SepT, Is.EqualTo(live.SepT), "the two runs separate together");
            var entered = skipped.Landing.AdvanceSkip(RecoveryStick.Assisted, 100000, 900);
            Assert.That(entered, Is.EqualTo(RecoveryPhase.Entry), "the skip lands on the entry burn");
            AssertSameSample(liveSample, skipped.Landing.Sample(), "KH-7 entry burn");
        }

        [Test]
        public void Kh7PlannedBooster_SkipToLandingBurnReachesTheBurn()
        {
            var live = BeginBooster(7);
            int n = 0;
            while (live.Landing.Sample().Phase != RecoveryPhase.Landing && n++ < 200000)
                live.Landing.Step(1.0 / 60.0, RecoveryStick.Assisted);
            Assert.That(live.Landing.Sample().Phase, Is.EqualTo(RecoveryPhase.Landing), "the live booster enters its landing burn");
            var liveSample = live.Landing.Sample();

            var skipped = BeginBooster(7);
            var first = skipped.Landing.AdvanceSkip(RecoveryStick.Assisted, 100000, 900);
            Assert.That(first, Is.EqualTo(RecoveryPhase.Entry), "first skip lands on the entry burn");
            var second = skipped.Landing.AdvanceSkip(RecoveryStick.Assisted, 200000, 900);
            Assert.That(second, Is.EqualTo(RecoveryPhase.Landing), "second skip lands on the landing burn");
            var skipSample = skipped.Landing.Sample();
            // The landing burn starts low, where the near-ground step (0.0125 s) slices presented time
            // differently live than in a skip, so the two agree to a step, not to the bit.
            Assert.That(skipSample.Phase, Is.EqualTo(liveSample.Phase), "phase");
            Assert.That(Math.Abs(skipSample.T - liveSample.T), Is.LessThan(0.1), "T within a step");
            Assert.That(Math.Abs(skipSample.Alt - liveSample.Alt), Is.LessThan(10), "alt within a step");
            Assert.That(Math.Abs(skipSample.Speed - liveSample.Speed), Is.LessThan(2), "speed within a step");
        }

        [Test]
        public void Kh4ExpendableBooster_HasNoBurnToSkipTo()
        {
            var run = BeginBooster(4);
            Assert.AreEqual(BoosterFate.Expendable, run.Landing.Fate, "KH-4's starter picked no landing");
            Assert.That(StageSkip.NextRecoveryTarget(run.Landing), Is.EqualTo(StageSkipTarget.None),
                "no landing fuel means no entry burn to skip to");
            bool enteredEntry = false;
            int n = 0;
            while (!run.Landing.Touched && n++ < 200000)
            {
                run.Landing.Step(1.0 / 60.0, RecoveryStick.Assisted);
                if (run.Landing.Sample().Phase == RecoveryPhase.Entry) enteredEntry = true;
            }
            Assert.IsTrue(run.Landing.Touched, "the attempt still reaches the surface");
            Assert.IsFalse(enteredEntry, "the expendable attempt never lights an entry burn");
        }
    }
}
