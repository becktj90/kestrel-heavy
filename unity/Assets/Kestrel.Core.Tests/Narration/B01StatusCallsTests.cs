// B01 mission-status narration (PR #495 takes): each call is raised only when its fact is true for the rocket flying,
// a gate that cannot be read keeps it silent, held clips never reach the game manifest, and the takes that did ship
// are only eligible for the rockets they are true for.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Flight;
using Kestrel.Core.Flight.Narration;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests.Narration
{
    public class B01StatusCallsTests
    {
        const string Climbing = WebcastBoard.VerdictClimbing, OnTrack = WebcastBoard.VerdictOnTrack,
            Short = WebcastBoard.VerdictShort, Speed = WebcastBoard.VerdictSpeed, Heating = WebcastBoard.VerdictHeating;

        static List<string> Ids(StatusCalls s)
        {
            var list = new List<StatusCue>();
            s.Drain(list);
            return list.Select(c => c.LitStage > 0 ? c.EventId + "." + c.LitStage : c.EventId).ToList();
        }

        // ---- board verdict calls ----

        [Test]
        public void OnTrack_FirstTimeAfterClimbing_WithTheLiveSpareKnownAndNotNegative_Once()
        {
            var s = new StatusCalls();
            s.OnVerdict(OnTrack, 1000, true, true, 1);
            Assert.AreEqual(0, Ids(s).Count(), "never before Climbing");
            s.OnVerdict(Climbing, 1000, true, true, 2);
            s.OnVerdict(OnTrack, 1000, true, true, 3);
            CollectionAssert.AreEqual(new[] { StatusCalls.OnTrack }, Ids(s));
            s.OnVerdict(Climbing, 1000, true, true, 4);
            s.OnVerdict(OnTrack, 1000, true, true, 5);
            Assert.AreEqual(0, Ids(s).Count(), "once per flight");
        }

        [Test]
        public void OnTrack_FailsClosed_WhenTheLiveSpareCannotBeReadOrIsNegative()
        {
            foreach (bool? spare in new bool?[] { null, false })
            {
                var s = new StatusCalls();
                s.OnVerdict(Climbing, 1000, true, spare, 1);
                s.OnVerdict(OnTrack, 1000, true, spare, 2);
                Assert.AreEqual(0, Ids(s).Count(), "spare " + spare);
            }
        }

        [Test]
        public void Short_OnlyWithTheSpareNegative_ThenAgainOnlyAfterAClear()
        {
            var s = new StatusCalls();
            s.OnVerdict(Climbing, 0, true, true, 1);
            s.OnVerdict(Short, 0, true, null, 2);
            Assert.AreEqual(0, Ids(s).Count(), "unknown spare: silent");
            s.OnVerdict(OnTrack, 0, true, true, 3);
            CollectionAssert.AreEqual(new[] { StatusCalls.OnTrack }, Ids(s));
            s.OnVerdict(Short, 0, true, false, 4);
            CollectionAssert.AreEqual(new[] { StatusCalls.Short }, Ids(s));
            s.OnVerdict(OnTrack, 0, true, false, 5);
            Assert.AreEqual(0, Ids(s).Count(), "no clear while the spare is still negative");
            s.OnVerdict(Short, 0, true, false, 6);
            Assert.AreEqual(0, Ids(s).Count(), "no second short without a clear");
            s.OnVerdict(OnTrack, 0, true, true, 7);
            CollectionAssert.AreEqual(new[] { StatusCalls.ShortClear }, Ids(s));
            s.OnVerdict(Short, 0, true, false, 8);
            CollectionAssert.AreEqual(new[] { StatusCalls.Short }, Ids(s), "re-armed by the clear");
        }

        [Test]
        public void Speed_OnlyWhileAnEngineBurns_Once()
        {
            var s = new StatusCalls();
            s.OnVerdict(Speed, 0, false, true, 1);
            Assert.AreEqual(0, Ids(s).Count(), "after cutoff the takes' 'stand by for cutoff' is false");
            s.OnVerdict(OnTrack, 0, true, true, 2);
            s.OnVerdict(Speed, 0, true, true, 3);
            CollectionAssert.AreEqual(new[] { StatusCalls.Speed }, Ids(s));
            s.OnVerdict(OnTrack, 0, true, true, 4);
            s.OnVerdict(Speed, 0, true, true, 5);
            Assert.AreEqual(0, Ids(s).Count());
        }

        [Test]
        public void Heating_OnlyWhileQIsStillRising_BeforeThePeak()
        {
            var s = new StatusCalls();
            s.OnVerdict(Climbing, 20000, true, true, 1);
            s.OnVerdict(Heating, 19000, true, true, 2);
            Assert.AreEqual(0, Ids(s).Count(), "q falling: past the peak, 'coming up on max-Q' would be false");
            s.OnVerdict(Heating, 18000, true, true, 3);
            Assert.AreEqual(0, Ids(s).Count());

            var r = new StatusCalls();
            r.OnVerdict(Heating, 20000, true, true, 1);
            Assert.AreEqual(0, Ids(r).Count(), "no earlier q sample: cannot tell rising, silent");
            r.OnVerdict(Heating, 21000, true, true, 2);
            CollectionAssert.AreEqual(new[] { StatusCalls.Heating }, Ids(r));
            r.OnVerdict(Climbing, 22000, true, true, 3);
            r.OnVerdict(Heating, 23000, true, true, 4);
            Assert.AreEqual(0, Ids(r).Count(), "once per flight");
        }

        [Test]
        public void Heating_QuietForFiveSecondsAfterAMaxQThrottleBeat()
        {
            var s = new StatusCalls();
            s.OnVerdict(Climbing, 20000, true, true, 10);
            s.OnSimEvent(AscentDirector.MaxQThrottleEvent, 0, 10);
            s.OnVerdict(Heating, 21000, true, true, 14.9);
            Assert.AreEqual(0, Ids(s).Count());
            s.OnVerdict(Heating, 22000, true, true, 15.1);
            CollectionAssert.AreEqual(new[] { StatusCalls.Heating }, Ids(s));
        }

        [Test]
        public void ALostVehicle_HearsNoMoreStatusCalls()
        {
            var s = new StatusCalls();
            s.OnVerdict(Climbing, 0, true, true, 1);
            s.OnSimEvent("failure", 0, 2);
            s.OnVerdict(OnTrack, 0, true, true, 3);
            s.OnVerdict(Speed, 0, true, true, 4);
            Assert.AreEqual(0, Ids(s).Count());
        }

        // ---- engine-out.ok ----

        [Test]
        public void EngineOutOk_ThreeSecondsLater_StillClimbingAndIntact_KeepsTheLitStage()
        {
            var s = new StatusCalls();
            s.OnFlight(39, 9000);
            s.OnSimEvent(StatusCalls.EngineOut, 1, 40);
            s.OnFlight(41, 9500);
            s.OnFlight(42.9, 9900);
            Assert.AreEqual(0, Ids(s).Count());
            s.OnFlight(43.0, 10000);
            CollectionAssert.AreEqual(new[] { StatusCalls.EngineOutOk + ".1" }, Ids(s));
            s.OnFlight(44, 10200);
            Assert.AreEqual(0, Ids(s).Count(), "once");
        }

        [Test]
        public void EngineOutOk_SilentWhenFallingLostOrLate()
        {
            var falling = new StatusCalls();
            falling.OnSimEvent(StatusCalls.EngineOut, 1, 40);
            falling.OnFlight(42.5, 9000);
            falling.OnFlight(43.1, 8900);
            Assert.AreEqual(0, Ids(falling).Count(), "not climbing");

            var lost = new StatusCalls();
            lost.OnSimEvent(StatusCalls.EngineOut, 1, 40);
            lost.OnFlight(42.5, 9000);
            lost.OnSimEvent("failure", 1, 42.8);
            lost.OnFlight(43.1, 9100);
            Assert.AreEqual(0, Ids(lost).Count(), "vehicle lost");

            var late = new StatusCalls();
            late.OnSimEvent(StatusCalls.EngineOut, 1, 40);
            late.OnFlight(40.5, 9000);
            late.OnFlight(48, 12000);
            Assert.AreEqual(0, Ids(late).Count(), "a warp step far past the moment is too late to say it");

            var none = new StatusCalls();
            none.OnFlight(42.5, 9000);
            none.OnFlight(43.1, 9100);
            Assert.AreEqual(0, Ids(none).Count(), "no engine-out, no call");
        }

        // ---- booster landing callouts ----

        [Test]
        public void LandingAltitude_EachMarkOnALiveStepOfTheLandingBurn_DroppedIfBusy()
        {
            var s = new StatusCalls();
            double t = 300, alt = 1300;
            var got = new List<StatusCue>();
            while (alt > 0)
            {
                s.OnBooster(true, true, alt, 0, t);
                s.Drain(got);
                t += 0.1; alt -= 12;
            }
            CollectionAssert.AreEqual(StatusCalls.LandingAltEvents, got.Select(c => c.EventId).ToArray());
            Assert.IsTrue(got.All(c => c.DropIfBusy && c.LitStage == 0));
        }

        [Test]
        public void LandingAltitude_SilentOffTheBurn_OnAnUnplannedLanding_AndOnAJump()
        {
            var coast = new StatusCalls();
            coast.OnBooster(true, false, 1100, 0, 1);
            coast.OnBooster(true, false, 900, 0, 1.1);
            Assert.AreEqual(0, Ids(coast).Count(), "not the landing burn");
            coast.OnBooster(true, true, 600, 0, 1.2);
            coast.OnBooster(true, true, 499, 0, 1.3);
            CollectionAssert.AreEqual(new[] { "landing.alt.500" }, Ids(coast), "1k passed before the burn is not news");

            var lost = new StatusCalls();
            lost.OnBooster(false, true, 1100, 0, 1);
            lost.OnBooster(false, true, 900, 0, 1.1);
            Assert.AreEqual(0, Ids(lost).Count(), "no planned landing");

            var jump = new StatusCalls();
            jump.OnBooster(true, true, 1100, 0, 1);
            jump.OnBooster(true, true, 50, 0, 1.2);
            Assert.AreEqual(0, Ids(jump).Count(), "one step over two marks says neither");
            var settle = new StatusCalls();
            settle.OnBooster(true, true, 1100, 0, 1);
            settle.OnBooster(true, true, 990, 0, 9);
            Assert.AreEqual(0, Ids(settle).Count(), "a long step is a skip, not a live callout");
            settle.OnBooster(true, true, 400, 0, 9.1);
            CollectionAssert.AreEqual(new[] { "landing.alt.500" }, Ids(settle), "the next live step calls the one mark it passes; 1k is gone");
        }

        [Test]
        public void LandingLegs_WhenTheyFinishDeploying_OnALiveStep_Once()
        {
            var s = new StatusCalls();
            s.OnBooster(true, true, 200, 0.0, 1);
            s.OnBooster(true, true, 190, 0.5, 1.1);
            Assert.AreEqual(0, Ids(s).Count());
            s.OnBooster(true, true, 180, 1.0, 1.2);
            CollectionAssert.AreEqual(new[] { StatusCalls.LandingLegs }, Ids(s));
            s.OnBooster(true, true, 170, 0.5, 1.3);
            s.OnBooster(true, true, 160, 1.0, 1.4);
            Assert.AreEqual(0, Ids(s).Count());

            var none = new StatusCalls();
            none.OnBooster(false, true, 200, 0.5, 1);
            none.OnBooster(false, true, 190, 1.0, 1.1);
            Assert.AreEqual(0, Ids(none).Count(), "no planned landing");
        }

        // ---- routes: sep-clear and engine-out ----

        [Test]
        public void Separation_IsSepClear_OnlyForTheFirstCoreStageDrop_OnAMultiStageRocket()
        {
            Assert.AreEqual("sep-clear", NarrationRoute.Route("separation", 1, false, 2).EventId);
            Assert.IsTrue(NarrationRoute.Route("separation", 2, false, 3).Silent, "a later core sep is not booster/upper stage");
            Assert.IsTrue(NarrationRoute.Route("separation", 1, true, 2).Silent, "strap-on drop");
            Assert.AreEqual("sep-clear", NarrationRoute.Route("separation", 2, true, 2).EventId);
            Assert.IsTrue(NarrationRoute.Route("separation", 1, false, 1).Silent, "single stage");
            Assert.IsTrue(NarrationRoute.Route("separation", 1, false, 0).Silent, "facts unknown: fail closed");
            Assert.IsTrue(NarrationRoute.Route("separation", 0, false, 2).Silent);
        }

        [Test]
        public void EngineOut_LightsCoreStageOne_ButNoStageWhenStrapOnsShareTheEntry()
        {
            var plain = NarrationRoute.Route("engine-out", 1, false, 2);
            Assert.AreEqual("engine-out", plain.EventId);
            Assert.AreEqual(1, plain.LitStage);
            var straps = NarrationRoute.Route("engine-out", 1, true, 2);
            Assert.AreEqual("engine-out", straps.EventId);
            Assert.AreEqual(0, straps.LitStage);
            Assert.AreEqual(0, NarrationRoute.DefaultLit("engine-out.ok", 2), "no default lit stage: lit.engines unknown, gated takes silent");
        }

        // ---- hold_start ----

        [Test]
        public void HoldStart_FiresWhenTheCountEntersAHold_OncePerHold_AndDropsWhenTheHoldEnds()
        {
            var t = new CallTriggers(CallSchedule.Empty, 2, false);
            t.CountTick(-20, null, 0, false);
            Drain(t, 0);
            t.CountTick(-15, null, 1, true);
            CollectionAssert.AreEqual(new[] { CallTriggers.HoldStart }, Drain(t, 1));
            t.CountTick(-15, null, 2, true);
            Assert.AreEqual(0, Drain(t, 2).Count(), "the same hold");
            t.CountTick(-15, null, 3, false);
            t.CountTick(-14, null, 4, true);
            Assert.IsTrue(t.DueIds.Contains(CallTriggers.HoldStart), "a new hold");
            t.CountTick(-14, null, 5, false);
            Assert.IsFalse(t.DueIds.Contains(CallTriggers.HoldStart), "not said once the hold is over");
            t.CountTick(-13, null, 6, true);
            t.Liftoff();
            Assert.IsFalse(t.DueIds.Contains(CallTriggers.HoldStart));
        }

        [Test]
        public void RaisedFlightCalls_WaitForLiftoff_AndDropIfBusyCuesNeverQueue()
        {
            var t = new CallTriggers(CallSchedule.Empty, 2, false);
            t.Raise(StatusCalls.OnTrack, 0, 0, false);
            Assert.AreEqual(0, t.DueIds.Count(), "before liftoff");
            t.Liftoff();
            t.Raise("landing.alt.500", 0, 1, true);
            Assert.IsFalse(t.Next(1, true, false, out _));
            Assert.AreEqual(0, t.DueIds.Count(), "dropped while a line plays");
            t.Raise(StatusCalls.OnTrack, 0, 2, false);
            Assert.IsFalse(t.Next(2, true, true, out _));
            CollectionAssert.AreEqual(new[] { StatusCalls.OnTrack }, t.DueIds.ToArray(), "a normal call waits");
            Assert.AreEqual(0, Drain(t, 7).Count(), "and still expires after FreshSec");
        }

        static List<string> Drain(CallTriggers t, double now)
        {
            var got = new List<string>();
            for (int i = 0; i < 20 && t.Next(now, false, false, out var cue); i++) { got.Add(cue.EventId); t.Offered(cue); }
            return got;
        }

        // ---- the shipped manifest ----

        static string ManifestPath => Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Game", "Audio", "Runtime", "Resources", "Narration", "manifest.json");
        static List<JsonObject> clips;
        static List<JsonObject> Clips()
        {
            if (clips != null) return clips;
            clips = new List<JsonObject>();
            foreach (JsonObject c in MiniJson.ParseObject(File.ReadAllText(ManifestPath)).Arr("clips")) clips.Add(c);
            return clips;
        }
        static List<NarrationTake> Takes(string ev) => Clips().Where(c => c.Str("event") == ev).Select(NarrationTake.FromJson).ToList();

        [Test]
        public void Manifest_HasNoHeldClip_NoOutcomeLine_NoBannedRoar()
        {
            foreach (var c in Clips())
            {
                string id = c.Str("id"), ev = c.Str("event") ?? "", file = c.Str("file") ?? "";
                Assert.IsFalse(c.Bool("hold"), id + ": hold:true clips are never wired");
                Assert.IsFalse(ev.StartsWith("outcome."), id + ": no outcome.* line until an emitter exists");
                StringAssert.DoesNotContain("engine.roar.far.v3", ev + " " + id + " " + file);
                if (c.Str("batch") == "B01") Assert.AreNotEqual("fuel-low", ev, id + ": B01 fuel-low lines wait for a 12% propellant trigger");
                Assert.AreNotEqual("landing.fuel-low", ev, id);
            }
        }

        [Test]
        public void Manifest_B01Takes_AreCallsUnderTheWordCap_AndOnlyForWiredEvents()
        {
            var wired = new HashSet<string>(StatusCalls.Events) { "engine-out", "sep-clear" };
            var b01 = Clips().Where(c => c.Str("batch") == "B01").ToList();
            Assert.That(b01.Count, Is.EqualTo(91));
            foreach (var c in b01)
            {
                string id = c.Str("id");
                Assert.IsTrue(wired.Contains(c.Str("event")), id + " has no trigger");
                Assert.AreEqual("call", c.Str("layer"), id);
                Assert.IsFalse(c.Bool("interruptible"), id);
                Assert.That(c.Str("text").Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length, Is.LessThanOrEqualTo(14), id);
                Assert.That(c.Num("durationSec"), Is.GreaterThan(0).And.LessThanOrEqualTo(6), id);
                Assert.IsTrue(NarrationTake.FromJson(c).Tagged, id + " carries a valid fact tag");
            }
            foreach (var ev in StatusCalls.Events) Assert.IsNotEmpty(Takes(ev), ev + " has takes");
        }

        static NarrationFacts Rocket(int stages, int s1Engines, string recovery, bool legs)
        {
            var f = new NarrationFacts().With("stages", stages).With("stage1.engines", s1Engines).With("recovery", recovery).With("legs", legs);
            if (stages >= 2) f = f.With("stage2.engines", 1);
            return f;
        }

        static int Eligible(string ev, NarrationFacts f) => Takes(ev).Count(t => NarrationRequires.Eligible(t, f));

        [Test]
        public void Gates_EachB01LineIsEligibleOnlyWhereItIsTrue()
        {
            var single = Rocket(1, 1, "none", false);
            var expendable = Rocket(2, 9, "none", false);
            var barge = Rocket(2, 9, "barge", true);
            var bargeNoLegs = Rocket(2, 9, "barge", false);

            Assert.AreEqual(0, Eligible("sep-clear", single), "sep-clear needs stages >= 2");
            Assert.Greater(Eligible("sep-clear", expendable), 0);
            foreach (var ev in StatusCalls.LandingAltEvents)
            {
                Assert.AreEqual(0, Eligible(ev, expendable), ev + " needs barge|rtls");
                Assert.AreEqual(0, Eligible(ev, single), ev);
                Assert.Greater(Eligible(ev, barge), 0, ev);
                Assert.Greater(Eligible(ev, Rocket(2, 9, "rtls", false)), 0, ev);
            }
            Assert.AreEqual(0, Eligible("landing.legs", bargeNoLegs), "no legs, no legs line");
            Assert.AreEqual(0, Eligible("landing.legs", Rocket(2, 9, "none", true)), "legs but no landing picked");
            Assert.Greater(Eligible("landing.legs", barge), 0);

            Assert.AreEqual(0, Eligible("engine-out.ok", barge), "no lit stage: lit.engines unknown, silent");
            Assert.AreEqual(0, Eligible("engine-out.ok", single.WithLit(1)), "one engine: no 'remaining engines'");
            Assert.Greater(Eligible("engine-out.ok", barge.WithLit(1)), 0);

            foreach (var ev in new[] { "status.ontrack", "status.short", "status.short.clear", "status.speed", "status.heating", "engine-out" })
            {
                Assert.AreEqual(Takes(ev).Count, Eligible(ev, single), ev + ": neutral, true on every rocket");
                Assert.AreEqual(Takes(ev).Count, Takes(ev).Count(t => NarrationRequires.Eligible(t, null)), ev + ": neutral even with no facts");
            }
            foreach (var ev in new[] { "sep-clear", "landing.legs", "engine-out.ok", "landing.alt.1k" })
                Assert.AreEqual(0, Takes(ev).Count(t => NarrationRequires.Eligible(t, null) && !t.Neutral), ev + ": gated takes fail closed with no facts");
        }

        [Test]
        public void Gates_EvaluativeLandingTakesAndHeldLinesStayOut()
        {
            var ids = new HashSet<string>(Clips().Select(c => c.Str("id")));
            foreach (var held in new[]
            {
                "mission-control/landing.alt.1k.g5-6", "mission-control/landing.alt.500.g5-5", "mission-control/landing.alt.500.g5-6",
                "mission-control/landing.alt.100.g5-3", "mission-control/landing.alt.100.g5-6", "mission-control/landing.alt.10.g5-3",
                "mission-control/landing.legs.g5-1", "mission-control/status.offcourse.g5-1", "host/sep-wide.g5-1",
                "mission-control/landing.sink.high.g5-1", "mission-control/landing.drift.g5-1", "host/fuel-low.g5-1",
            })
                Assert.IsFalse(ids.Contains(held), held);
        }
    }
}
