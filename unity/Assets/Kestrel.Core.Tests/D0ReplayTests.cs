using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;
using Kestrel.Core.Replay;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class D0ReplayTests
    {
        [Test]
        public void Contract_MatchesRecordedTraces()
        {
            foreach (JsonObject c in Golden.Cases("d0-replay"))
            {
                if (c.Str("kind") == "grade")
                {
                    var t = c.Obj("touchdown");
                    string medal = D0Replay.GradeLanding(t.Str("kind"), t.Bool("recovered"), t.Num("vy"), t.Num("dx"), t.Num("tilt"), t.Num("landingVy"), t.Num("landingTol"));
                    Assert.AreEqual(c.ContainsKey("medal") ? c["medal"] as string : null, medal);
                    continue;
                }
                Assert.AreEqual(c.Num("seed"), D0Replay.SeedFor(c.Str("mission"), c.Str("difficulty")), c.Str("id"));
                Assert.AreEqual(D0Replay.FrameMs, c.Num("frameMs"), c.Str("id") + " frame");
                var trace = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "tests", "replay", "traces", c.Str("id") + ".json")));
                var expect = trace.Obj("expect");
                JsonSame.Equal(expect["result"], c["result"], c.Str("id") + " result");
                JsonSame.Equal(expect["phases"], c["phases"], c.Str("id") + " phases");
                JsonSame.Equal(expect["events"], c["events"], c.Str("id") + " events");
                var cps = expect.Arr("checkpoints");
                Assert.AreEqual((int)c.Num("checkpointCount"), cps.Count, c.Str("id"));
                foreach (List<object> cp in cps)
                {
                    var snap = (List<object>)cp[2];
                    Assert.AreEqual(D0Replay.PortraitSnap, (string)snap[snap.Count - 1], c.Str("id") + " portrait");
                }
                Assert.IsTrue(c.Bool("portrait"), c.Str("id"));
                var input = new List<(int, string, int)>();
                foreach (List<object> ev in trace.Arr("input")) input.Add(((int)(double)ev[0], (string)ev[1], (int)(double)ev[2]));
                var masks = D0Replay.InputMasks(input, (int)expect.Num("frames"));
                var want = c.Arr("masks");
                Assert.AreEqual(want.Count, masks.Length, c.Str("id") + " masks");
                for (int i = 0; i < masks.Length; i++) Assert.AreEqual((int)(double)want[i], masks[i], c.Str("id") + " mask " + i);
                foreach (JsonObject sample in c.Arr("fnv"))
                {
                    Assert.AreEqual(sample.Str("hash"), D0Replay.Fnv1a(sample.Str("json")), c.Str("id") + " fnv " + sample.Num("frame"));
                }
            }
        }
    }
}
