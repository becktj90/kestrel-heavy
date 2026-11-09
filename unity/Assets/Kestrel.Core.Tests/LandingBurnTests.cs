// #79: the player flies the final landing burn. Autopilot still flies boostback, entry and the
// descent; only the Landing phase takes a player stick, through the same sim (same guidance, same
// 10 deg/s pitch rate, same fin/leg/phase gates), and RecoveryScore still grades the touchdown.
// The driver is BoosterLanding stepped live at 60 fps, the way RecoveryShow steps it, with the
// director's stick only while the Landing phase flies (Auto-assist everywhere else).
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LandingBurnTests
    {
        static readonly string[] LandingMissions = { "m6", "m7", "m10", "m13", "m15", "m19" };

        // The MECO hand-overs, captured once per run (one AscentSim each, as HavenLandingGoldenTests
        // captures them per test). The cache is built lazily in SetUp so a load failure fails tests
        // individually instead of the whole class at type init.
        static Dictionary<string, Handover> handovers;
        static Catalog catalog;

        [SetUp]
        public void Load()
        {
            RecoveryLog.Clear();
            if (handovers == null) handovers = LoadHandovers();
        }

        [TearDown]
        public void Drop() => RecoveryLog.Clear();

        [Test, Timeout(120000)]
        public void AssistedWithNoInput_LandsExactlyLikeAuto_ForEveryLandingMission()
        {
            foreach (string id in LandingMissions)
            {
                var h = handovers[id];
                var auto = Drive(h, body => RecoveryStick.Assisted);
                var assisted = Drive(h, body => BurnStick(body, h.Craft, LandingBurnInput.None, LandingBurnAssist.Assisted));
                Assert.AreEqual(auto.Verdict.Sink, assisted.Verdict.Sink, 0, id + " sink");
                Assert.AreEqual(auto.Verdict.MissM, assisted.Verdict.MissM, 0, id + " miss");
                Assert.AreEqual(auto.Verdict.T, assisted.Verdict.T, 0, id + " time");
                Assert.AreEqual(auto.Verdict.GradeName, assisted.Verdict.GradeName, id + " grade");
                Assert.AreEqual(auto.Verdict.Medal, assisted.Verdict.Medal, id + " medal");
                Assert.IsNull(auto.Verdict.Cause, id + " auto lands clean: " + Dump(auto.Verdict));
                Assert.IsFalse(assisted.Verdict.PlayerFlown, id + " no input is not player-flown");
            }
        }

        [Test, Timeout(120000)]
        public void AssistedFullThrottle_StillGoesThroughTheFailureFlow()
        {
            // Holding full burn from the moment the burn opens wastes the reserve climbing, then the
            // stage comes down dry and fast. The failure is the existing named flow, not a crash of it.
            // #335: m7's handover moved with the fairing drop + the starter retune (kick 6 -> 2, shape
            // 0.5 -> 0), so holding full burn now hits fast (sink 178.3 m/s, Salvage) rather than
            // running dry - the cause is recovery-fast, not recovery-fuel. m13 is still recovery-fast.
            var m7 = Drive(handovers["m7"], body => BurnStick(body, handovers["m7"].Craft,
                LandingBurnInput.Held(1, 0), LandingBurnAssist.Assisted));
            Assert.IsTrue(m7.Verdict.PlayerFlown, "holding burn is player input");
            Assert.AreEqual(RecoveryScore.Fast, m7.Verdict.Cause, "m7 " + Dump(m7.Verdict));
            Assert.IsNotNull(m7.Verdict.Failure, "the failure event is there for the card");
            Assert.AreEqual("Salvage", m7.Verdict.GradeName, "m7 " + Dump(m7.Verdict));

            var m13 = Drive(handovers["m13"], body => BurnStick(body, handovers["m13"].Craft,
                LandingBurnInput.Held(1, 0), LandingBurnAssist.Assisted));
            Assert.IsTrue(m13.Verdict.PlayerFlown, "holding burn is player input");
            Assert.AreEqual(RecoveryScore.Fast, m13.Verdict.Cause, "m13 " + Dump(m13.Verdict));
            Assert.IsNotNull(m13.Verdict.Failure, "the failure event is there for the card");
        }

        [Test, Timeout(120000)]
        public void TiltSteersTheTouchdown_EastAndWest_AndStaysSoft()
        {
            foreach (string id in new[] { "m7", "m13" })
            {
                var h = handovers[id];
                var auto = Drive(h, body => RecoveryStick.Assisted);
                var east = Drive(h, body => BurnStick(body, h.Craft, LandingBurnInput.Held(0, 1), LandingBurnAssist.Assisted));
                var west = Drive(h, body => BurnStick(body, h.Craft, LandingBurnInput.Held(0, -1), LandingBurnAssist.Assisted));
                Assert.Greater(east.Verdict.MissM, auto.Verdict.MissM + 2, id + " tilt right moves the touchdown east");
                Assert.Less(west.Verdict.MissM, auto.Verdict.MissM - 2, id + " tilt left moves the touchdown west");
                Assert.That(east.Verdict.Sink, Is.EqualTo(0.8).Within(0.05), id + " steering does not hurry the sink");
                Assert.That(west.Verdict.Sink, Is.EqualTo(0.8).Within(0.05), id + " steering does not hurry the sink");
                Assert.IsTrue(east.Verdict.PlayerFlown && west.Verdict.PlayerFlown, id + " steering is player input");
            }
        }

        [Test, Timeout(120000)]
        public void ManualWithNoInput_DoesNotLand()
        {
            // Engineer with no input is the idle stick: the stage falls unpowered and the existing
            // failure flow says so. The burn really is the player's in Manual.
            var run = Drive(handovers["m7"], body => BurnStick(body, handovers["m7"].Craft,
                LandingBurnInput.None, LandingBurnAssist.Manual));
            Assert.IsNotNull(run.Verdict.Cause, "an idle manual burn does not land: " + Dump(run.Verdict));
            Assert.IsFalse(MissionScore.IsGood(run.Verdict.GradeName), "no medal for an idle burn");
            Assert.IsFalse(run.Verdict.PlayerFlown, "no input, no player-flown flag");
        }

        [Test, Timeout(120000)]
        public void SwellDeck_MovesTheTouchdownMiss_ButKeepsTheGrade()
        {
            foreach (string id in new[] { "m7", "m13" })
            {
                var h = handovers[id];
                var calm = Drive(h, body => RecoveryStick.Assisted, 0);
                var swell = Drive(h, body => RecoveryStick.Assisted, LandingBurnDirector.DefaultSwellAmpM);
                double offset = LandingBurnDirector.DeckOffsetM(swell.Verdict.T, LandingBurnDirector.DefaultSwellAmpM);
                Assert.AreEqual(calm.Verdict.MissM - offset, swell.Verdict.MissM, 0.1,
                    id + " the miss is measured against the moved deck");
                Assert.That(Math.Abs(swell.Verdict.MissM - calm.Verdict.MissM), Is.LessThanOrEqualTo(LandingBurnDirector.DefaultSwellAmpM + 0.1),
                    id + " the deck only moves by its swell");
                Assert.AreEqual(calm.Verdict.GradeName, swell.Verdict.GradeName, id + " the swell does not move the grade");
                Assert.AreEqual("Perfect", swell.Verdict.GradeName, id + " still a perfect catch on the swell");
            }
        }

        [Test]
        public void GradingStillComesFromLandingJson()
        {
            var tune = LandingTune.Load(Golden.Root);
            var haven = RecoveryScore.HavenTune();
            Assert.AreEqual(tune.PadRatLandingVy, haven.PadRatLandingVy, 0, "padRatLandingVy");
            Assert.AreEqual(tune.PerfectMin, haven.PerfectMin, 0, "perfectMin");
            Assert.AreEqual(tune.GoodMin, haven.GoodMin, 0, "goodMin");

            var craft = new RecoveryCraft { Mode = "barge", Legs = true, LegsKind = "light", ReserveKg = 10000, DryKg = 25000 };
            var end = new RecoverySample { Sink = 0.8, MissM = 0, TiltDeg = 0, Legs01 = 1, Prop = 1000, T = 1 };
            Assert.AreEqual("Perfect", RecoveryScore.Judge(end, craft, null).GradeName, "dead centre, soft");
            end.MissM = 20;
            Assert.AreEqual("Good", RecoveryScore.Judge(end, craft, null).GradeName, "20 m off is Good, not Perfect");
            end.MissM = 30;
            Assert.AreEqual("Hard", RecoveryScore.Judge(end, craft, null).GradeName, "30 m off is Hard");
            end.MissM = 0;
            end.TiltDeg = 20;
            Assert.AreEqual("Salvage", RecoveryScore.Judge(end, craft, null).GradeName, "tipped is Salvage");
        }

        [Test]
        public void BurnReadout_LinesUpSinkOffsetAndTiltAgainstTheirLimits()
        {
            var craft = new RecoveryCraft { Mode = "barge", Legs = true, LegsKind = "light", ReserveKg = 10000, DryKg = 25000 };
            var sample = new RecoverySample { Sink = 0.8, MissM = 9, TiltDeg = 2.1, Phase = RecoveryPhase.Landing };
            var readout = LandingBurnDirector.Readout(sample, craft, 6);
            Assert.AreEqual(0.8, readout.Sink, 0, "sink");
            Assert.AreEqual(2, readout.SinkLimit, 0, "the light-leg limit");
            Assert.AreEqual(3, readout.Offset, 0, "offset against the moved deck (9 - 6)");
            Assert.AreEqual(RecoverySim.DeckRadiusM, readout.OffsetLimit, 0, "the deck radius");
            Assert.AreEqual(2.1, readout.TiltDeg, 0, "tilt");
            Assert.AreEqual(RecoverySim.TiltLimitDeg, readout.TiltLimitDeg, 0, "the tip line");
            Assert.IsTrue(readout.OnDeck, "3 m off is on the deck");
            Assert.AreEqual("SINK 0.8/2.0 m/s  OFF 3/36 m  TILT 2.1/8°", readout.Line(), "one line for the burn panel");
        }

        [Test]
        public void Score_MultipliesOnlyTheLandingBonus_WhenPlayerFlown()
        {
            Assert.AreEqual(200, MissionScore.LandingBonus("perfect", false), 0, "plain perfect");
            Assert.AreEqual(400, MissionScore.LandingBonus("perfect", true), 0, "a hand-flown perfect pays double");
            Assert.AreEqual(100, MissionScore.LandingBonus("good", false), 0, "plain good");
            Assert.AreEqual(150, MissionScore.LandingBonus("good", true), 0, "a hand-flown good pays half as much again");
            Assert.AreEqual(0, MissionScore.LandingBonus("hard", true), 0, "hard pays nothing either way");
            Assert.AreEqual(0, MissionScore.LandingBonus("short", true), 0, "no landing, no bonus");
            // The pinned CampaignTests score (1500 base + bonus): only the bonus moves.
            Assert.AreEqual(1700, MissionScore.Points(true, 0.05, 0, 100, "perfect", false), 1e-6, "auto perfect");
            Assert.AreEqual(1900, MissionScore.Points(true, 0.05, 0, 100, "perfect", true), 1e-6, "hand-flown perfect");
            Assert.AreEqual(1600, MissionScore.Points(true, 0.05, 0, 100, "good", false), 1e-6, "auto good");
            Assert.AreEqual(1650, MissionScore.Points(true, 0.05, 0, 100, "good", true), 1e-6, "hand-flown good");
            Assert.AreEqual(0, MissionScore.Points(false, 0.05, 0, 100, "perfect", true), 0, "a failed flight still scores 0");
        }

        [Test, Timeout(120000)]
        public void Conclude_DoesNotSettle_AnOpenPlayerBurn()
        {
            var h = handovers["m7"];
            var landing = Arm(h, 0);
            Begin(landing, h);
            int guard = 0;
            while (landing.Body.Phase != RecoveryPhase.Landing && guard++ < 300000)
                landing.Step(1.0 / 60.0, RecoveryStick.Assisted);
            Assert.AreEqual(RecoveryPhase.Landing, landing.Body.Phase, "the burn has opened");
            Assert.IsFalse(landing.Conclude(true, false), "the open burn is not settled away");
            Assert.IsFalse(landing.Touched, "the player still has the burn");
            while (!landing.Touched && guard++ < 300000)
                landing.Step(1.0 / 60.0, RecoveryStick.Assisted);
            Assert.IsTrue(landing.Touched, "the burn still flies to touchdown");
            Assert.AreEqual("Perfect", landing.Verdict.GradeName, "and lands as before");
        }

        [Test, Timeout(120000)]
        public void Conclude_SettlesBeforeTheBurnOpens_AndSettlesAnExpendableAttempt()
        {
            var h = handovers["m7"];
            var landing = Arm(h, 0);
            Begin(landing, h);
            // Conclude is budgeted per call (KH-3 freeze: no main-thread Settle hitch). Present loops it.
            int guard = 0;
            while (!landing.Touched && guard++ < 10000)
                landing.Conclude(true, false);
            Assert.IsTrue(landing.Touched, "still in boostback/entry/descent, t466 settles across Present frames");
            Assert.That(guard, Is.GreaterThan(1), "budgeted Conclude takes more than one call");

            // The same rocket with no landing picked: an expendable attempt settles as before, ungraded.
            var expendable = Design.FromJson(WinnerDoc("m7"));
            expendable.recoveryMode = null;
            var lost = BoosterLanding.Arm(catalog, expendable, 0);
            Assert.AreEqual(BoosterFate.Expendable, lost.Fate, "no landing picked");
            Begin(lost, h);
            guard = 0;
            while (!lost.Touched && guard++ < 10000)
                lost.Conclude(true, false);
            Assert.IsTrue(lost.Touched, "an expendable attempt settles to the surface");
            Assert.IsTrue(lost.Verdict.Expendable, "never graded");
        }

        [Test, Timeout(120000)]
        public void Step_AnnouncesTouchdown_OnTheSink_Once()
        {
            var sink = new RecordingSink();
            var good = Drive(handovers["m7"], body => RecoveryStick.Assisted, 0, sink);
            Assert.AreEqual(1, sink.Touchdowns.Count, "one touchdown announcement");
            Assert.AreEqual(good.Verdict.Sink, sink.Touchdowns[0].Vz, 1e-9, "the announced sink is the verdict's");
            Assert.AreEqual(good.Verdict.TiltDeg, sink.Touchdowns[0].Tilt, 1e-9, "the announced tilt is the verdict's");
            Assert.That(sink.Phases, Does.Contain(RecoveryPhase.Landing), "the burn phase is announced");
            Assert.AreEqual(0, sink.Crashes.Count, "a good landing crashes nothing");

            var crashy = new RecordingSink();
            var bad = Drive(handovers["m7"], body => BurnStick(body, handovers["m7"].Craft,
                LandingBurnInput.None, LandingBurnAssist.Manual), 0, crashy);
            Assert.IsNotNull(bad.Verdict.Cause, "the idle manual burn fails: " + Dump(bad.Verdict));
            Assert.AreEqual(1, crashy.Touchdowns.Count, "even a failed touchdown announces once");
            Assert.AreEqual(1, crashy.Crashes.Count, "and the hard hit announces its crash");
            Assert.AreEqual("structural-maxq", crashy.Crashes[0], "the fast hit's boom kind");
        }

        // ---- the live loop, headless ----

        static RecoveryStick BurnStick(RecoveryBody body, RecoveryCraft craft, LandingBurnInput input, LandingBurnAssist assist)
        {
            if (body.Phase != RecoveryPhase.Landing) return RecoveryStick.Assisted;
            return LandingBurnDirector.Stick(body, craft, input, assist);
        }

        static BoosterLanding Drive(Handover h, Func<RecoveryBody, RecoveryStick> stickFor, double swellAmpM = 0, IFlightSimSink sink = null)
        {
            var landing = Arm(h, swellAmpM);
            Begin(landing, h);
            int guard = 0;
            while (!landing.Touched && guard++ < 300000)
                landing.Step(1.0 / 60.0, stickFor(landing.Body), sink);
            Assert.IsTrue(landing.Touched, "the booster reaches the surface");
            Assert.IsNotNull(landing.Verdict, "a verdict is published");
            return landing;
        }

        static BoosterLanding Arm(Handover h, double swellAmpM)
        {
            var landing = BoosterLanding.Arm(catalog, h.Design, swellAmpM);
            Assert.IsTrue(landing.Armed, "the landing arms");
            return landing;
        }

        static void Begin(BoosterLanding landing, Handover h)
        {
            double speed = Math.Sqrt(h.East * h.East + h.Up * h.Up);
            double pitch = Math.Atan2(h.Up, h.East) * 180.0 / Math.PI;
            landing.Watch(0, landing.CoreSerial, h.Down, h.Alt, speed, pitch, true, false);
            landing.Watch(0.1, landing.CoreSerial + 1, h.Down, h.Alt, speed, pitch, true, false);
            Assert.IsTrue(landing.Started, "the landing starts after separation");
        }

        sealed class RecordingSink : IFlightSimSink
        {
            public readonly List<RecoveryPhase> Phases = new List<RecoveryPhase>();
            public readonly List<(double Vz, double Tilt)> Touchdowns = new List<(double, double)>();
            public readonly List<string> Crashes = new List<string>();

            public void OnIgnition(int stage, double t) { }
            public void OnLiftoff(double t, FlightVec padPos) { }
            public void OnMaxQ(double q, double t) { }
            public void OnStageSeparation(int stage, FlightVec pos, FlightVec vel) { }
            public void OnFairingSeparation(double t, FlightVec pos, FlightVec vel) { }
            public void OnRecoveryPhase(RecoveryPhase phase, double t) => Phases.Add(phase);
            public void OnTouchdown(double vz, double tilt, FlightSurface surface) => Touchdowns.Add((vz, tilt));
            public void OnCrash(string kind, FlightVec pos, FlightVec vel, double energy) => Crashes.Add(kind);
            public void OnFailure(string kind, string cause, int engineIndex, FlightVec pos, FlightVec vel, double t) { }
            public void OnEngineFailure(int engineIndex, string mode) { }
            public void OnImpact(double speed, FlightVec pos) { }
        }

        static string Dump(RecoveryVerdict v)
            => "sink " + v.Sink.ToString("0.00") + " miss " + v.MissM.ToString("0.0") + " T " + v.T.ToString("0.0")
            + " grade " + v.GradeName + " cause " + (v.Cause ?? "none") + " flown " + v.PlayerFlown;

        // ---- hand-overs (the HavenLandingGoldenTests capture) ----

        sealed class Handover
        {
            public Design Design;
            public RecoveryCraft Craft;
            public double Down, Alt, East, Up;
        }

        static Dictionary<string, Handover> LoadHandovers()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            var campaign = Campaign.Load(Golden.Root);
            var map = new Dictionary<string, Handover>();
            foreach (string id in LandingMissions) map[id] = Capture(catalog, campaign, id);
            return map;
        }

        static JsonObject WinnerDoc(string id)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            foreach (JsonObject row in file.Arr("missions"))
                if (row.Str("id") == id) return (JsonObject)row.Arr("designs")[0];
            Assert.Fail(id + " has no winner");
            return null;
        }

        static Handover Capture(Catalog catalog, Campaign campaign, string id)
        {
            var doc = WinnerDoc(id);
            int index = int.Parse(id.Substring(1));
            var mission = campaign.Mission(index);
            var design = Design.FromJson(doc);
            Assert.IsTrue(RecoveryCrafts.TryFrom(catalog, design, out var craft), id);
            var input = Stack.SimInputs(catalog, design, 0.05, 0);
            input.opts.targetAlt = mission.GuidanceAltM;
            if (input.opts.guidance != null) input.opts.guidance.targetAlt = mission.GuidanceAltM;
            int core = design.StrapOnCount > 0 ? 2 : 1;
            var sim = new AscentSim(input.stages, input.opts);
            bool caught = false;
            double down = 0, alt = 0, east = 0, up = 0;
            int guard = 0;
            while (!sim.Done && guard++ < 200000)
            {
                sim.Tick();
                for (int i = 0; i < sim.Events.Count; i++)
                {
                    var e = sim.Events[i];
                    if (e.type == "burnout" && e.stage == core && !caught)
                    {
                        caught = true;
                        Mecco(sim.Y, out down, out alt, out east, out up);
                    }
                }
                if (caught) break;
            }
            Assert.IsTrue(caught, id + " core burnout");
            return new Handover { Design = design, Craft = craft, Down = down, Alt = alt, East = east, Up = up };
        }

        static void Mecco(double[] y, out double down, out double alt, out double east, out double up)
        {
            double x = y[0], yy = y[1];
            double r = Math.Sqrt(x * x + yy * yy);
            double padAngle = Math.PI / 2;
            down = Gravity.R_EARTH * (Math.Atan2(yy, x) - padAngle);
            alt = r - Gravity.R_EARTH;
            double vx = y[2], vy = y[3];
            double upx = x / r, upy = yy / r;
            double ex = -upy, ey = upx;
            east = vx * ex + vy * ey;
            up = vx * upx + vy * upy;
        }
    }
}
