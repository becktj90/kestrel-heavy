// DECISIONS #89 (fun review PR7, Physics option b for release): no max-Q call. The press never changed the throttle: a design
// with a max-Q setting flies its own law (GuidanceProfile) whether the player presses or not. The law now runs on its own, like a
// flight computer's throttle bucket, and the HUD marks it with two beats taken from the sim's real throttle-law transitions.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Flight.Narration;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class Pr7MaxQAutoTests
    {
        struct Run { public List<(double t, bool on, bool thrusting)> Law; public List<(double t, string text, int serial, string ev)> Beats; public bool CallSeen; }

        static Run Fly(Catalog catalog, CampaignMission mission, Design design, bool auto)
        {
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, new MissionFlightSite());
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            var director = MissionFlightSetup.Director(catalog, design, null, null, qLim, FailureMonitor.GentleTier(design),
                target, FlightBatch.DesignHash(design), 1, auto, false, false, null);
            var sim = new AscentSim(setup.Stages, setup.Options);
            var flight = new DirectedFlight(sim, director, setup.Options);
            var run = new Run { Law = new List<(double, bool, bool)>(), Beats = new List<(double, string, int, string)>() };
            bool was = false; int serial = 0; int guard = 0;
            while (!sim.Done && guard++ < 2000000)
            {
                flight.Step();
                if (sim.MaxQThrottling != was) { was = sim.MaxQThrottling; run.Law.Add((sim.T, was, sim.Thrusting)); }
                if (director.MaxQBeatSerial != serial) { serial = director.MaxQBeatSerial; run.Beats.Add((sim.T, director.MaxQBeatText, serial, director.MaxQBeatEvent)); }
                if (director.IsOpen(AscentCall.MaxQ) || director.IsDone(AscentCall.MaxQ)) run.CallSeen = true;
            }
            return run;
        }

        [Test]
        public void MaxQDesign_BeatsFollowTheSimsThrottleLaw_NoCall()
        {
            // Every Home LAUNCH pick with a max-Q cap: the throttle-down beat lands on the tick the sim's law engages, the
            // through beat on the first tick it releases with the engines still burning (a burnout inside the bucket is
            // not "through max-Q", so it stays silent). Once each, with or without auto-assist, and never a call.
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            var hangar = HangarData.LoadFromRepo(Golden.Root);
            int flown = 0, throughs = 0;
            foreach (var mission in Campaign.Load(Golden.Root).Missions)
            {
                var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
                bool fact = NarrationFacts.From(design, null).TryGet(NarrationFacts.MaxQThrottle, out var fv) && fv is bool fb && fb;
                Assert.AreEqual(design.guidance != null && design.guidance.maxQThrottleSet, fact, mission.Id + ": throttle.maxq fact");
                if (design.guidance == null || !design.guidance.maxQThrottleSet || design.guidance.maxQThrottle >= 100) continue;
                foreach (bool auto in new[] { true, false })
                {
                    var run = Fly(catalog, mission, design, auto);
                    string at = mission.Id + " auto " + auto;
                    Assert.IsFalse(run.CallSeen, at + ": no max-Q call");
                    var want = new List<(double, string)>();
                    int down = run.Law.FindIndex(x => x.on);
                    if (down >= 0)
                    {
                        want.Add((run.Law[down].t, AscentNotes.MaxQThrottleDown));
                        int up = run.Law.FindIndex(down + 1, x => !x.on && x.thrusting);
                        if (up >= 0) want.Add((run.Law[up].t, AscentNotes.MaxQThrough));
                    }
                    Assert.AreEqual(want.Count, run.Beats.Count, at + " beats");
                    for (int i = 0; i < want.Count; i++)
                    {
                        Assert.AreEqual(want[i].Item2, run.Beats[i].text, at);
                        Assert.AreEqual(want[i].Item1, run.Beats[i].t, 1e-9, at + ": on the transition tick");
                        Assert.AreEqual(i + 1, run.Beats[i].serial, at);
                        Assert.AreEqual(i == 0 ? "maxq-throttle" : "maxq-through", run.Beats[i].ev, at + ": Narrator Voice key");
                    }
                    flown++;
                    if (want.Count == 2) throughs++;
                }
            }
            Assert.Greater(flown, 0, "Home picks with a max-Q cap");
            Assert.Greater(throughs, 0, "at least one design throttles back up under power");
        }

        [Test]
        public void NoMaxQSetting_NoBeat_NoCall_OnEveryStarterDefault()
        {
            int plain = 0;
            foreach (var mission in StarterFixture.Campaign.Missions)
            {
                var design = Design.FromJson(StarterFixture.Rocket(mission.Id).BuildDesign());
                bool set = design.guidance != null && design.guidance.maxQThrottleSet && design.guidance.maxQThrottle < 100;
                var run = Fly(StarterFixture.Catalog, mission, design, true);
                Assert.IsFalse(run.CallSeen, mission.Id);
                Assert.IsTrue(NarrationFacts.From(design, null).TryGet("throttle.maxq", out var fv) && fv is bool, mission.Id);
                if (design.guidance == null || !design.guidance.maxQThrottleSet) Assert.AreEqual(false, fv, mission.Id + ": no setting, no throttle.maxq");
                if (!set)
                {
                    plain++;
                    Assert.IsEmpty(run.Law, mission.Id + ": the sim never reports a max-Q throttle-down");
                    Assert.IsEmpty(run.Beats, mission.Id + ": narration only says what is true for this rocket");
                }
                else Assert.LessOrEqual(run.Beats.Count, 2, mission.Id);
            }
            Assert.Greater(plain, 0, "at least one Starter default has no max-Q setting");
        }

        [Test]
        public void TheLawHelper_MatchesTheThrottleLaw()
        {
            var g = new GuidanceProfile { maxQThrottleSet = true, maxQThrottle = 60 };
            double bucket = GuidanceProfile.MaxQReferencePa * GuidanceProfile.MaxQBucketFraction;
            Assert.IsFalse(g.MaxQThrottleActive(new GuidanceState { q = bucket - 1 }));
            Assert.IsTrue(g.MaxQThrottleActive(new GuidanceState { q = bucket }));
            Assert.IsFalse(g.MaxQThrottleActive(new GuidanceState { q = bucket, throttleMin = 1 }), "an engine that cannot throttle never throttles down");
            Assert.IsFalse(new GuidanceProfile { maxQThrottleSet = true, maxQThrottle = 100 }.MaxQThrottleActive(new GuidanceState { q = 1e5 }), "a 100 % cap is no throttle-down");
            Assert.IsFalse(new GuidanceProfile { gLimitSet = true, gLimit = 1 }.MaxQThrottleActive(new GuidanceState { q = 1e5, thrustN = 1e7, mass = 1e5 }), "a g-limit cap is not max-Q");
        }

        [Test]
        public void TheBeats_FitAt335AndInWebLandscape()
        {
            foreach (var text in AscentNotes.Beats)
            {
                Assert.LessOrEqual(text.Length, AscentNotes.MaxChars, text);
                // Portrait 335 px (and every width 320-1032, ReportFitTests.StageBannerIsNeverCutAtAnyPortraitWidth): the banner
                // takes the card or the full-width row, whole, at 11 pt or more.
                float row = FlightHudLayout.BannerRowPt(text, 335f);
                var p = FlightHudLayout.Place(335f, 720f, 20f, 0f, 0f, 0f, FlightHudMode.Cruise, row);
                Assert.IsTrue(FlightHudLayout.BannerFitsRow(text, p), "335: " + text);
                Assert.IsFalse(FlightHudLayout.BannerFit(text, p.Banner.W + 2f * FlightHudLayout.BannerCardPadPt).Cut, "335 uncut: " + text);
                // Web landscape: the stages panel of the wide HUD at the web sizes the layout tests use.
                foreach (var wh in new[] { new[] { 844f, 390f }, new[] { 1280f, 720f }, new[] { 1280f, 800f }, new[] { 1920f, 1080f } })
                {
                    var f = FlightHudLayout.PlaceWide(wh[0], wh[1], 0f, 0f, 0f, 0f, FlightHudMode.Cruise);
                    Assert.IsTrue(FlightHudLayout.BannerFitsRow(text, f), wh[0] + "x" + wh[1] + ": " + text);
                }
            }
        }
    }
}
