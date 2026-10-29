// Trevor: "I want to see it blow up if it fails." Every way a rocket can be lost in the sim must put a visible explosion on screen
// (fire + smoke particles, debris, a ring) through the real CinematicFlightSink -> CinematicEvents -> ExplosionFx / DebrisFx / ShockwaveFx path.
// The list of outcomes is read from the sim itself: every FailureKind constant (flight breakups and engine losses, Headless.Fail), every
// ground impact (OnImpact after any loss, and OnCrash for a hard touchdown) and every booster-recovery crash cause (RecoveryViews.Boom).
// If a new FailureKind is added the loop picks it up, so it cannot silently ship without an explosion.
using System.Collections.Generic;
using System.Reflection;
using Kestrel.Core.Flight;
using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class ExplosionFailurePathTests
    {
        GameObject go; ExplosionFx fx; ShockwaveFx sw; DebrisFx debris; CinematicFlightSink sink;
        static readonly FlightVec V = new FlightVec(1, 2);

        [SetUp]
        public void SetUp()
        {
            ExplosionFx.QualityOverride = 2;
            CinematicEvents.ClearSubscribers(); CinematicEvents.ResetSinkState(); FlashGovernor.Reset();
            go = new GameObject("failure-paths");
            fx = go.AddComponent<ExplosionFx>(); fx.Subscribe();
            sw = go.AddComponent<ShockwaveFx>(); sw.Subscribe();
            debris = go.AddComponent<DebrisFx>(); debris.Subscribe();
            NewSink();
        }

        void NewSink() => sink = new CinematicFlightSink { Anchor = () => new Vector3(0f, 30f, 0f), Reduce = () => false, VehicleRadiusM = FlightSinkMath.VehicleRadiusM(0.8) };

        [TearDown]
        public void TearDown()
        {
            ExplosionFx.QualityOverride = -1;
            fx.Unsubscribe(); sw.Unsubscribe(); debris.Unsubscribe();
            Object.DestroyImmediate(go);
            CinematicEvents.ClearSubscribers(); CinematicEvents.ResetSinkState();
        }

        int Live(ExplosionLayer l)
        {
            var ps = fx.LayerRenderer(l) != null ? fx.LayerRenderer(l).GetComponent<ParticleSystem>() : null;
            if (ps == null) return 0;
            ps.Simulate(0.02f, false, false, true);
            return ps.particleCount;
        }

        void AssertWholeVehicleExplosion(string what)
        {
            Assert.That(Live(ExplosionLayer.Fire), Is.GreaterThan(0), what + ": no fire");
            Assert.That(Live(ExplosionLayer.Smoke), Is.GreaterThan(0), what + ": no smoke");
            Assert.That(debris.World.Live, Is.GreaterThan(0), what + ": no debris");
            Assert.That(sw.LiveRings + sw.LiveHaze, Is.GreaterThan(0), what + ": no blast ring or haze");
            Assert.That(fx.LastPlan.Fire, Is.GreaterThan(0), what + ": plan emits no fire");
            // And the picture is the rocket's size, not a runaway: a 0.8 m stack gives a small blast.
            Assert.That(fx.LastPlan.Radius, Is.LessThanOrEqualTo(16f));
        }

        static List<int> AllFailureKinds()
        {
            var kinds = new List<int>();
            foreach (var f in typeof(FailureKind).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.IsLiteral && f.FieldType == typeof(int) && (int)f.GetRawConstantValue() != FailureKind.None) kinds.Add((int)f.GetRawConstantValue());
            return kinds;
        }

        [Test]
        public void EveryFailureKindIsKnownAndEnumerated()
        {
            var kinds = AllFailureKinds();
            Assert.That(kinds.Count, Is.EqualTo(11), "FailureKind gained or lost a kind: update the failure paths below");
            foreach (int k in kinds) Assert.That(FailureKind.Name(k), Is.Not.Empty, "kind " + k);
        }

        [Test]
        public void EveryFlightFailureKindEndsInAVisibleExplosion()
        {
            foreach (int kind in AllFailureKinds())
            {
                TearDown(); SetUp();   // a clean pool and limiter per kind
                string name = FailureKind.Name(kind);
                bool engine = FlightEngineMode.IsEngine(name);
                // The sim's order (IFlightSimSink contract): OnFailure, then OnEngineFailure for an engine kind, then the vehicle falls and OnImpact.
                sink.OnFailure(name, "cause", engine ? 0 : -1, V, V, 42);
                if (engine) sink.OnEngineFailure(0, FlightEngineMode.Of(name));
                if (!engine)
                    AssertWholeVehicleExplosion(name + " (breakup)");   // a breakup blows up on the spot, not at impact
                else
                    Assert.That(Live(ExplosionLayer.Fire) + Live(ExplosionLayer.Flash) + Live(ExplosionLayer.Sparks) + Live(ExplosionLayer.Embers), Is.GreaterThan(0), name + ": no engine-bell effect");
                sink.OnImpact(120, V);
                AssertWholeVehicleExplosion(name + " (after impact)");
            }
        }

        [Test]
        public void ARocketThatHitsTheGroundAtSpeedBlowsUpWithoutAnyFailureEvent()
        {
            // Headless: a vehicle that never fails but reaches the ground fast (OnImpact with nothing raised before).
            sink.OnImpact(150, V);
            AssertWholeVehicleExplosion("plain ground impact");
        }

        [Test]
        public void AHardTouchdownCrashBlowsUp()
        {
            sink.OnCrash("hard", V, V, 5e7);
            AssertWholeVehicleExplosion("hard touchdown crash");
        }

        [Test]
        public void EveryBoosterRecoveryCrashCauseBlowsUpAndOnlyASoftMissDoesNot()
        {
            const double vLeg = 3.0;
            var causes = new[] { RecoveryScore.Fast, RecoveryScore.Fuel, RecoveryScore.Miss, RecoveryScore.Tilt };
            foreach (string cause in causes)
                foreach (double sink0 in new[] { 1.0, 6.0, 25.0, 80.0 })
                {
                    TearDown(); SetUp();
                    string boom = RecoveryViews.Boom(cause, sink0, vLeg);
                    if (boom == null)
                    {
                        // The only outcome with no explosion: a miss/fuel/fast landing that is still slower than the legs' rating.
                        Assert.That(cause, Is.Not.EqualTo(RecoveryScore.Tilt), "a tilted landing always collapses");
                        Assert.That(sink0, Is.LessThanOrEqualTo(vLeg), $"{cause} at {sink0} m/s sink must show a crash");
                        continue;
                    }
                    sink.OnCrash(boom, V, V, 0.5 * 20000 * sink0 * sink0);
                    AssertWholeVehicleExplosion($"{cause} sink {sink0}");
                }
        }

        [Test]
        public void ReducedMotionStillShowsTheFireAndDebrisJustWithoutTheFlash()
        {
            sink.Reduce = () => true;
            sink.OnFailure("structural-maxq", "q", -1, V, V, 1);
            Assert.That(Live(ExplosionLayer.Flash), Is.EqualTo(0), "reduce-flashing drops the flash");
            Assert.That(Live(ExplosionLayer.Fire), Is.GreaterThan(0));
            Assert.That(debris.World.Live, Is.GreaterThan(0));
        }
    }
}
