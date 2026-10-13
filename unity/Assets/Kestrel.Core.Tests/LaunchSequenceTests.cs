using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Json;
using Kestrel.Core.Timeline;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LaunchSequenceTests
    {
        [Test]
        public void RealSeconds_InvertsGameSeconds()
        {
            foreach (var mode in new[] { "kid", "standard", "engineer" })
            {
                foreach (var real in new[] { -7200d, -4000, -2280, -2100, -960, -420, -240, -180, -130, -60, -45, -16, -3, -0.2, 0, 12.5 })
                {
                    double game = TimelineClock.GameSeconds(real, mode);
                    double back = TimelineClock.RealSeconds(game, mode);
                    Assert.AreEqual(real, back, 1e-6, mode + " " + real);
                }
            }
        }

        [Test]
        public void KidCount_FitsTheBand_AndPlaysEveryCalloutInOrder()
        {
            var seq = Open("super-heavy", "kid");
            Assert.GreaterOrEqual(seq.StartTime, -40, "kid count stays inside the 20–40 s band");
            Assert.Less(seq.StartTime, -20, "the count is long enough to be a sequence");
            Assert.IsFalse(seq.Liftoff);
            Assert.IsNotNull(seq.Beats[0]);
            StringAssert.DoesNotContain(".hold.", seq.Beats[0].Id);

            double t = seq.Time;
            while (!seq.Liftoff)
            {
                seq.Step(0.25);
                Assert.Greater(seq.Time, t - 1e-9);
                t = seq.Time;
            }
            Assert.AreEqual(0, seq.Time, 1e-9);
            var seen = Ids(seq.Fired);
            Assert.Contains("term.liftoff", seen);
            Assert.Contains("term.count", seen);
            Assert.Contains("ign.light", seen);
            Assert.Contains("count.prop.go", seen);
            Assert.IsFalse(seen.Exists(id => id.Contains(".hold.")));

            seq.AdvanceSim(1e9);
            seen = Ids(seq.Fired);
            Assert.Contains("asc.maxq", seen);
            Assert.Contains("asc.meco", seen);
            Assert.Contains("stg.sep.1", seen);
            Assert.IsTrue(seen.Exists(id => id.StartsWith("stg.ses.") || id.StartsWith("stg.light.")),
                "upper stage announces a start");
            AssertOrdered(seq, seen);
            Assert.AreEqual(seq.Beats.Count, seen.Count, "every planned beat fires once");
        }

        [Test]
        public void Step_FiresTheWholeCountInOneJump_AndIgnoresSimUntilLiftoff()
        {
            var seq = Open("tpl-medium-kerolox-reusable", "kid");
            int pre = seq.Beats.Count(b => b.Nominal <= 0);
            int opened = seq.Fired.Count;
            var jumped = seq.Step(10_000);
            Assert.IsTrue(seq.Liftoff);
            Assert.AreEqual(pre, opened + jumped.Count, "the jump finishes the count Open already started");
            Assert.AreEqual("term.liftoff", jumped[jumped.Count - 1].Id);
            Assert.IsEmpty(seq.AdvanceSim(0));
            var rest = seq.AdvanceSim(seq.Beats[seq.Beats.Count - 1].Nominal);
            Assert.IsNotEmpty(rest);
            Assert.Greater(rest[0].Nominal, 0);
        }

        [Test]
        public void Hold_FreezesTheClock()
        {
            var seq = Open("tpl-hydrogen-upper", "standard");
            double t = seq.Time;
            seq.Hold();
            Assert.IsEmpty(seq.Step(5));
            Assert.AreEqual(t, seq.Time, 1e-12);
            seq.Release();
            seq.Step(0.5);
            Assert.Greater(seq.Time, t);
        }

        [Test]
        public void Solid_UsesBurnout_NotMeco()
        {
            var seq = Open("tpl-sounding-solid", "kid");
            seq.Step(10_000);
            var ids = Ids(seq.AdvanceSim(1e9));
            Assert.Contains("asc.burnout", ids);
            Assert.IsFalse(ids.Contains("asc.meco"));
            Assert.IsFalse(ids.Contains("stg.sep.1"));
        }

        [Test]
        public void PadTime_ReachesCrewArmAndUmbilicalDrop()
        {
            var seq = Open("super-heavy", "kid");
            bool sawArm = false, sawDrop = false;
            double prev = double.NegativeInfinity;
            while (!seq.Liftoff)
            {
                double real = TimelineClock.RealSeconds(seq.Time, seq.Mode);
                Assert.GreaterOrEqual(real, prev - 1e-6);
                prev = real;
                if (real >= -130) sawArm = true;
                if (real >= -0.6) sawDrop = true;
                seq.Step(0.02);
            }
            if (TimelineClock.RealSeconds(seq.Time, seq.Mode) >= -0.6) sawDrop = true;
            Assert.IsTrue(sawArm, "access arms get their real T−130 s during the compressed count");
            Assert.IsTrue(sawDrop, "umbilical drop is inside the count");
            Assert.AreEqual(0, TimelineClock.RealSeconds(0, "kid"), 1e-12);
        }

        static LaunchSequence Open(string id, string mode)
        {
            var cat = Golden.Catalog;
            var d = cat.Preset(id);
            if (d == null)
            {
                string dir = Path.Combine(Golden.Root, "web", "js", "data", "templates");
                foreach (var file in Directory.GetFiles(dir, "*.json"))
                {
                    var parsed = Design.FromJson(MiniJson.ParseObject(File.ReadAllText(file)));
                    if (parsed != null && parsed.id == id) { d = parsed; break; }
                }
            }
            Assert.IsNotNull(d, id);
            var seq = LaunchSequence.Open(TimelineGenerator.Generate(cat, d, Pad.For(cat, d), new Mission { mode = mode, id = id }, 1));
            Assert.IsNotEmpty(seq.Beats, id);
            return seq;
        }

        static List<string> Ids(IReadOnlyList<LaunchBeat> beats)
        {
            var ids = new List<string>(beats.Count);
            foreach (var b in beats) ids.Add(b.Id);
            return ids;
        }

        static void AssertOrdered(LaunchSequence seq, List<string> fired)
        {
            int at = 0;
            foreach (var b in seq.Beats)
            {
                if (at < fired.Count && fired[at] == b.Id) at++;
            }
            Assert.AreEqual(fired.Count, at, "fired " + string.Join(",", fired));
        }
    }
}
