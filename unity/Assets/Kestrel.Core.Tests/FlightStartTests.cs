// Home LAUNCH -> briefing -> To the pad -> designer launch -> flight.
// The count used to reach T+0:00, clear itself, then drop the ascent if the first start threw.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Guide;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Timeline;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightStartTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            GameSession.Clear();
            PadHandoff.Clear();
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            HomeAutoBuild.InstallMissionOrbits(catalog, campaign);
            hangar = HangarData.LoadFromRepo(Golden.Root);
        }

        [TearDown]
        public void Clear()
        {
            GameSession.Clear();
            PadHandoff.Clear();
        }

        [Test]
        public void HomeLaunchBriefingThenPadReachesLiftoffAndClears100Metres()
        {
            var route = HomeRoutes.Launch(campaign, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Missions, route.Screen);
            Assert.AreEqual(SessionScreens.Briefing, route.Overlay, "LAUNCH opens the briefing, To the pad is next");
            var built = GameSession.Apply(RouteId.BriefingBuild);
            Assert.AreEqual(SessionScreens.Design, built.Screen);
            Assert.AreEqual(SessionScreens.None, built.Overlay);
            Assert.IsTrue(PadHandoff.FromHome);
            Assert.IsTrue(PadHandoff.TryTake(out var doc, out bool fly));
            Assert.IsTrue(fly, "Home LAUNCH enters flight from the pad");
            var design = Design.FromJson(doc);
            var session = new HangarSession(catalog, hangar, doc);
            Assert.IsTrue(session.LaunchEnabled, "First Hop is allowed to launch");
            Climb(design);
        }

        [Test]
        public void DesignerLaunchAfterDesignFirstAlsoClears100Metres()
        {
            var route = HomeRoutes.DesignFirst(campaign, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Design, route.Screen);
            Assert.AreEqual(SessionScreens.None, route.Overlay);
            Assert.IsTrue(PadHandoff.TryTake(out var doc, out bool fly));
            Assert.IsFalse(fly, "Design first waits on the designer launch control");
            var design = Design.FromJson(doc);
            var session = new HangarSession(catalog, hangar, doc);
            Assert.IsTrue(session.LaunchEnabled);
            Climb(design);
        }

        [Test]
        public void AFailedLiftoffIsRetriedOnTheNextTick()
        {
            var handoff = new LiftoffHandoff();
            handoff.Enter(null);
            Assert.IsFalse(handoff.Counting);
            int calls = 0;
            handoff.Tick(0, () =>
            {
                calls++;
                if (calls == 1) throw new InvalidOperationException("first liftoff frame");
                return true;
            });
            Assert.IsFalse(handoff.Ascending, "a throw must not freeze the handoff on COUNT");
            Assert.AreEqual(1, handoff.StartAttempts);
            Assert.IsInstanceOf<InvalidOperationException>(handoff.LastError);
            handoff.Tick(0, () =>
            {
                calls++;
                return true;
            });
            Assert.IsTrue(handoff.Ascending);
            Assert.AreEqual(2, handoff.StartAttempts);
            Assert.IsNull(handoff.LastError);
        }

        [Test]
        public void OneSlowFrameDoesNotSkipTheCount()
        {
            var design = HomeDesign();
            var handoff = new LiftoffHandoff();
            handoff.Enter(OpenSequence(design));
            Assert.IsTrue(handoff.Counting);
            Assert.AreEqual(LiftoffHandoff.OpenAt, handoff.Sequence.Time, 1e-6);
            bool started = false;
            handoff.Tick(30, () => { started = true; return true; });
            Assert.IsFalse(started);
            Assert.IsTrue(handoff.Counting);
            Assert.IsFalse(handoff.Sequence.Liftoff);
            Assert.AreEqual(LiftoffHandoff.OpenAt + LiftoffHandoff.MaxStep, handoff.Sequence.Time, 1e-6);
        }

        [Test]
        public void VoicePlaysOnBriefingAndMenuAndStaysSilentInFlight()
        {
            Assert.IsTrue(GuideClipPack.AllowsVoice(false), "menu and briefing");
            Assert.IsFalse(GuideClipPack.AllowsVoice(true), "ascent");

            Assert.IsTrue(GuideClipPack.TryStreamingUrl("http://localhost:8080/StreamingAssets", "a.mp3", out var web));
            Assert.AreEqual("http://localhost:8080/StreamingAssets/narration/guide/a.mp3", web);

            Assert.IsFalse(GuideClipPack.TryStreamingUrl(Path.Combine(Path.GetTempPath(), "kh-missing-stream"), "a.mp3", out _));

            string dir = Path.Combine(Path.GetTempPath(), "kh-stream-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "narration", "guide"));
            File.WriteAllBytes(Path.Combine(dir, "narration", "guide", "a.mp3"), new byte[] { 1, 2, 3 });
            try
            {
                Assert.IsTrue(GuideClipPack.TryStreamingUrl(dir, "a.mp3", out var disk));
                StringAssert.StartsWith("file:///", disk);
                StringAssert.Contains("a.mp3", disk);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }

            string ios = GuideClipPack.FileUrl("/var/mobile/Containers/Data/Application/x/a.mp3");
            StringAssert.StartsWith("file:///var/mobile/", ios);
        }

        [Test]
        public void SkipStartsAscentAfterTheCountAlreadyReachedZero()
        {
            var handoff = new LiftoffHandoff();
            var trace = new List<string>();
            handoff.Transitioned += trace.Add;
            handoff.Enter(OpenSequence(HomeDesign()));
            Assert.AreEqual("counting", handoff.Phase);
            Assert.IsTrue(trace.Exists(line => line.StartsWith("boot->counting", StringComparison.Ordinal)));
            int guard = 0;
            while (handoff.Counting && guard++ < 500)
                handoff.Tick(LiftoffHandoff.MaxStep, () => false);
            Assert.IsTrue(handoff.Sequence.Liftoff);
            Assert.IsFalse(handoff.Ascending, "a false start leaves the rocket on the pad at T+0");
            Assert.AreEqual("committed", handoff.Phase);
            bool started = false;
            handoff.Skip(() => { started = true; return true; });
            Assert.IsTrue(started, "Skip count still calls the ascent after the clock has already hit zero");
            Assert.IsTrue(handoff.Ascending);
            Assert.AreEqual("ascending", handoff.Phase);
        }

        [Test]
        public void LiftoffWithin15SecondsWhenAudioClipAndVoiceAreBlocked()
        {
            var design = HomeDesign();
            var blockers = new LiftoffBlockers
            {
                AudioDeviceReady = false,
                ClipLoadFinished = false,
                VoiceGateOpen = false,
                GuideLineHolding = true,
            };
            Assert.IsFalse(LiftoffHandoff.WaitsOn(blockers));
            string away = Path.Combine(Path.GetTempPath(), "kh-norepo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            string prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
            AeroModel.ClearTables();
            var handoff = new LiftoffHandoff();
            handoff.Enter(OpenSequence(design));
            AscentSim sim = null;
            try
            {
                Assert.DoesNotThrow(() => AeroModel.NoseHeatingFactor("ogive"));
                double elapsed = 0;
                while (!handoff.Ascending && elapsed < 15)
                {
                    double dt = LiftoffHandoff.FrameDt(0, 1.0 / 60.0);
                    elapsed += dt;
                    handoff.Tick(dt, () =>
                    {
                        if (LiftoffHandoff.WaitsOn(blockers)) return false;
                        var (stages, opts) = Stack.SimInputs(catalog, design, 1.0 / 120.0);
                        opts.tMax = 60;
                        sim = new AscentSim(stages, opts);
                        return sim != null;
                    }, blockers);
                }
                Assert.IsTrue(handoff.Ascending,
                    "phase=" + handoff.Phase
                    + " t=" + (handoff.Sequence != null ? handoff.Sequence.Time : double.NaN)
                    + " err=" + handoff.LastError);
                Assert.Less(elapsed, 15, "liftoff inside 15 seconds with the scaled clock stopped");
                Assert.IsNotNull(sim);
                Assert.IsNull(handoff.LastError);

                AeroModel.Install(
                    File.ReadAllText(Path.Combine(Golden.Root, "data", "fairings.json")),
                    File.ReadAllText(Path.Combine(Golden.Root, "data", "interstages.json")),
                    File.ReadAllText(Path.Combine(Golden.Root, "data", "fins.json")));
                Assert.AreEqual(0.33, AeroModel.NoseCd("biconic"), 1e-9, "player json still supplies a nose the built-in table does not have");
            }
            finally
            {
                Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prev);
                try { if (Directory.Exists(away)) Directory.Delete(away, true); }
                catch (Exception) { }
                AeroModel.ClearTables();
                AeroModel.NoseHeatingFactor("ogive");
            }
        }

        void Climb(Design design)
        {
            var handoff = new LiftoffHandoff();
            handoff.Enter(OpenSequence(design));
            Assert.IsTrue(handoff.Counting, "the webcast opens on the count, not already at liftoff");
            Assert.Less(handoff.Sequence.Time, 0);
            AscentSim sim = null;
            int frames = 0;
            const int limit = 2000;
            while (!handoff.Ascending && frames < limit)
            {
                handoff.Tick(LiftoffHandoff.FrameDt(1.0 / 60.0, 0), () =>
                {
                    var (stages, opts) = Stack.SimInputs(catalog, design, 1.0 / 120.0);
                    opts.tMax = 60;
                    sim = new AscentSim(stages, opts);
                    return true;
                });
                frames++;
            }
            Assert.IsTrue(handoff.Ascending, "liftoff frames=" + frames + " t=" + (handoff.Sequence != null ? handoff.Sequence.Time : double.NaN));
            Assert.Less(frames, 900, "twelve seconds of count at 60 Hz, plus the liftoff frame");
            Assert.IsNotNull(sim);
            double alt = 0;
            int guard = 0;
            while (!sim.Done && alt <= 100 && sim.T < 30 && guard++ < 20000)
            {
                sim.Tick();
                double r = Math.Sqrt(sim.Y[0] * sim.Y[0] + sim.Y[1] * sim.Y[1]);
                alt = r - Gravity.R_EARTH;
            }
            Assert.Greater(alt, 100, "alt=" + alt + " simT=" + sim.T);
            Assert.Less(sim.T, 30, "altitude within 30 seconds of liftoff");
        }

        Design HomeDesign()
        {
            HomeRoutes.Launch(campaign, 0, catalog, hangar.templates);
            Assert.IsTrue(PadHandoff.TryTake(out var doc, out _));
            return Design.FromJson(doc);
        }

        LaunchSequence OpenSequence(Design design)
        {
            var tl = TimelineGenerator.Generate(catalog, design, Pad.For(catalog, design), new Mission { mode = "standard" }, 1);
            return LaunchSequence.Open(tl);
        }
    }
}
