using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class CinematicFlightSinkTests
    {
        CinematicFlightSink sink;
        readonly List<CinematicEvent> blasts = new List<CinematicEvent>();
        readonly List<StagingFlashEvent> flashes = new List<StagingFlashEvent>();
        readonly List<CameraBeatEvent> beats = new List<CameraBeatEvent>();
        readonly List<ShockwaveEvent> rings = new List<ShockwaveEvent>();
        System.Action<CinematicEvent> hb; System.Action<StagingFlashEvent> hf; System.Action<CameraBeatEvent> hc; System.Action<ShockwaveEvent> hs;
        static readonly FlightVec V = new FlightVec(1, 2);

        [SetUp] public void SetUp()
        {
            // NUnit reuses one fixture instance for every test, so the capture lists must be emptied here.
            blasts.Clear(); flashes.Clear(); beats.Clear(); rings.Clear();
            CinematicEvents.ResetSinkState();
            sink = new CinematicFlightSink { Anchor = () => new Vector3(1, 2, 3), Reduce = () => false };
            hb = e => blasts.Add(e); hf = e => flashes.Add(e); hc = e => beats.Add(e); hs = e => rings.Add(e);
            CinematicEvents.Raised += hb; CinematicEvents.StagingFlashRaised += hf; CinematicEvents.CameraBeatRaised += hc; CinematicEvents.ShockwaveRaised += hs;
        }

        [TearDown] public void TearDown()
        {
            CinematicEvents.Raised -= hb; CinematicEvents.StagingFlashRaised -= hf; CinematicEvents.CameraBeatRaised -= hc; CinematicEvents.ShockwaveRaised -= hs;
            CinematicEvents.ResetSinkState();
        }

        [Test] public void EveryHookRunsWithoutThrowingAndIsCounted()
        {
            sink.OnIgnition(1, 0); sink.OnLiftoff(0, V); sink.OnMaxQ(30000, 60); sink.OnStageSeparation(1, V, V); sink.OnFairingSeparation(120, V, V);
            sink.OnRecoveryPhase(RecoveryPhase.Descent, 1); sink.OnTouchdown(1, 2, FlightSurface.Deck);
            sink.OnCrash("hard", V, V, 1e7); sink.OnFailure("breakup", "q", -1, V, V, 5); sink.OnEngineFailure(2, "sputter"); sink.OnImpact(50, V);
            Assert.That(sink.Hooks, Is.EqualTo(11));
            Assert.That(sink.LastPhase, Is.EqualTo((int)RecoveryPhase.Descent));
        }

        [Test] public void SeparationsRaiseStagingFlashAndBeatButNoDebris()
        {
            sink.OnStageSeparation(1, V, V); sink.OnFairingSeparation(120, V, V);
            Assert.That(flashes.Count, Is.EqualTo(2)); Assert.That(flashes[0].Kind, Is.EqualTo(StagingFlashKind.Separation));
            Assert.That(beats.TrueForAll(b => b.Beat == CameraBeat.Staging), Is.True);
            Assert.That(blasts.Count, Is.EqualTo(0));   // FlightStageShow owns separation debris
            Assert.That(flashes[0].X, Is.EqualTo(1f)); Assert.That(flashes[0].Z, Is.EqualTo(3f));   // scene anchor, not the Earth-frame FlightVec
        }

        [Test] public void IgnitionOfStageOneAddsPadRingLaterStagesOnlyGlow()
        {
            sink.OnIgnition(1, 0);
            Assert.That(rings.Count, Is.EqualTo(1)); Assert.That(rings[0].Grounded, Is.True);
            rings.Clear(); flashes.Clear();
            sink.OnIgnition(2, 100);
            Assert.That(rings.Count, Is.EqualTo(0)); Assert.That(flashes.Count, Is.EqualTo(1)); Assert.That(flashes[0].Kind, Is.EqualTo(StagingFlashKind.Ignition));
        }

        [Test] public void CrashFailureAndEngineFailureMapToTheirEventKinds()
        {
            sink.OnCrash("hard", V, V, 2e7); sink.OnFailure("breakup", "q", -1, V, V, 5); sink.OnEngineFailure(3, FlightEngineMode.Overpressure); sink.OnEngineFailure(0, FlightEngineMode.Sputter);
            Assert.That(blasts.Count, Is.EqualTo(4));
            Assert.That(blasts[0].Kind, Is.EqualTo(CinematicKind.Crash)); Assert.That(blasts[0].Energy01, Is.EqualTo(1f));
            Assert.That(blasts[1].Kind, Is.EqualTo(CinematicKind.Failure));
            Assert.That(blasts[2].Kind, Is.EqualTo(CinematicKind.EngineFailure)); Assert.That(blasts[2].Overpressure, Is.True); Assert.That(blasts[2].EngineIndex, Is.EqualTo(3));
            Assert.That(blasts[3].Overpressure, Is.False);
        }

        [Test] public void EngineKindFailureWaitsForTheEngineHookSoItNeverDoublesUp()
        {
            sink.OnFailure("engine-overpressure", "x", 1, V, V, 5);
            Assert.That(blasts.Count, Is.EqualTo(0));
            sink.OnEngineFailure(1, FlightEngineMode.Overpressure);
            Assert.That(blasts.Count, Is.EqualTo(1));
        }

        [Test] public void ImpactAfterACrashOrBreakupAddsNothing()
        {
            sink.OnFailure("breakup", "q", -1, V, V, 5); sink.OnImpact(80, V);
            Assert.That(blasts.Count, Is.EqualTo(1));
            sink.Attach(); sink.Detach(); sink.Attach();   // a new flight re-arms it
            sink.OnImpact(80, V);
            Assert.That(blasts.Count, Is.EqualTo(2));
            sink.Detach();
        }

        [Test] public void ReduceMotionIsPassedOnEverywhere()
        {
            sink.Reduce = () => true;
            sink.OnIgnition(1, 0); sink.OnStageSeparation(1, V, V); sink.OnTouchdown(6, 0, FlightSurface.Ground); sink.OnCrash("x", V, V, 1e7);
            Assert.That(flashes.TrueForAll(f => f.Reduce), Is.True);
            Assert.That(beats.TrueForAll(b => b.Reduce), Is.True);
            Assert.That(rings.TrueForAll(r => r.Reduce), Is.True);
            Assert.That(blasts.TrueForAll(b => b.Reduce), Is.True);
        }

        [Test] public void TouchdownRingOnlyWhenHardAndNotOnSea()
        {
            sink.OnTouchdown(1, 0, FlightSurface.Deck); sink.OnTouchdown(6, 0, FlightSurface.Sea);
            Assert.That(rings.Count, Is.EqualTo(0));
            sink.OnTouchdown(6, 0, FlightSurface.Ground);
            Assert.That(rings.Count, Is.EqualTo(1));
            Assert.That(beats.FindAll(b => b.Beat == CameraBeat.Touchdown).Count, Is.EqualTo(3));
        }

        [Test] public void AttachDetachDrivesTheDedupFlagAndIsIdempotent()
        {
            Assert.That(CinematicEvents.FlightSinkActive, Is.False);
            sink.Attach(); sink.Attach();
            Assert.That(CinematicEvents.FlightSinkActive, Is.True);
            sink.Detach();
            Assert.That(CinematicEvents.FlightSinkActive, Is.False);
            sink.Detach();
            Assert.That(CinematicEvents.FlightSinkActive, Is.False);
        }

        [Test] public void NullAnchorFallsBackToTheOriginAndEngineAnchorWins()
        {
            var s = new CinematicFlightSink { Reduce = () => false, EngineAnchor = i => new Vector3(9, i, 0) };
            s.OnStageSeparation(1, V, V); s.OnEngineFailure(4, FlightEngineMode.Overpressure);
            Assert.That(flashes[0].X, Is.EqualTo(0f));
            Assert.That(blasts[0].X, Is.EqualTo(9f)); Assert.That(blasts[0].Y, Is.EqualTo(4f));
        }
    }
}
