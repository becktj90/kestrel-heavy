// Launch time of day, review follow-ups to #272:
// - the id is hashed as explicit UTF-8 bytes (published FNV-1a 64 vectors, non-ASCII ids);
// - data/golden/launch-time.json is shared with the JS port (web/js/flight/launch-time.js);
// - per-mission draws stay inside 3*sqrt(p(1-p)/N) at large N;
// - a player build installs the bundled table instead of walking up to a checkout.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LaunchTimeGoldenTests
    {
        [Test]
        public void Fnv1a64_PublishedVectors()
        {
            Assert.AreEqual(0xcbf29ce484222325UL, LaunchTime.Fnv1a64(""));
            Assert.AreEqual(0xaf63dc4c8601ec8cUL, LaunchTime.Fnv1a64("a"));
            Assert.AreEqual(0x85944171f73967e8UL, LaunchTime.Fnv1a64("foobar"));
            Assert.AreEqual(LaunchTime.Fnv1a64(""), LaunchTime.Fnv1a64(null));
            Assert.AreEqual(LaunchTime.FnvOffset, LaunchTime.Fnv1a64(""));
        }

        [Test]
        public void NonAsciiId_HashesUtf8Bytes()
        {
            // Literals from Python over s.encode('utf-8'). The UTF-16 code-unit hash used before
            // gave 05d56c5ed7f3df28 for ("KH-é", 0) and 814778a85af1dfcc for ("KH-🚀", 0).
            Assert.AreEqual(0x0ac21707b7181e01UL, LaunchTime.Fnv1a64("é"));
            Assert.AreEqual(0x1be063d5f4d579adUL, LaunchTime.Hash("KH-é", 0));
            Assert.AreEqual(0x8b98646cb4673f6eUL, LaunchTime.Hash("KH-🚀", 0));
            Assert.AreNotEqual(0x05d56c5ed7f3df28UL, LaunchTime.Hash("KH-é", 0));
            // Lone surrogate is U+FFFD, the same as Encoding.UTF8 and the JS TextEncoder.
            Assert.AreEqual(0x6f6d661b9658624aUL, LaunchTime.Fnv1a64("\uD800"));

            foreach (var s in new[] { "", "a", "foobar", "é", "KH-é", "KH-🚀", "ケストレル", "\uD800x", "x\uDC00", "\uDBFF\uDFFF" })
                Assert.AreEqual(BytesFnv(Encoding.UTF8.GetBytes(s)), LaunchTime.Fnv1a64(s), s);
        }

        [Test]
        public void AsciiIds_UnchangedFromUtf16CodeUnits()
        {
            Assert.AreEqual(0xC39D71C85DA55BA0UL, LaunchTime.Hash("KH-1", 0));
            for (int i = 1; i <= 20; i++)
            {
                string id = "KH-" + i;
                for (ulong seed = 0; seed < 256; seed++)
                    Assert.AreEqual(Utf16Hash(id, seed), LaunchTime.Hash(id, seed), id + " seed " + seed);
            }
        }

        [Test]
        public void SharedGolden_MatchesCSharp()
        {
            var table = LaunchTimeTable.Load(Golden.Root);
            int picks = 0, plain = 0;
            foreach (var c in Golden.Cases("launch-time"))
            {
                var input = c.Obj("in");
                var output = c.Obj("out");
                ulong want = ulong.Parse(output.Str("hash"), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (c.Str("fn") == "fnv1a64")
                {
                    Assert.AreEqual(want, LaunchTime.Fnv1a64(input.Str("text")), input.Str("text"));
                    plain++;
                    continue;
                }
                Assert.AreEqual("pick", c.Str("fn"));
                string id = input.Str("id");
                ulong seed = ulong.Parse(input.Str("seed"), NumberStyles.None, CultureInfo.InvariantCulture);
                Assert.AreEqual(want, LaunchTime.Hash(id, seed), id + " " + seed);
                Assert.IsTrue(LaunchTimeTable.TryParse(output.Str("pick"), out var pick), output.Str("pick"));
                Assert.AreEqual(pick, table.Pick(id, seed), id + " " + seed);
                Assert.AreEqual(pick, LaunchTime.PickTimeOfDay(id, seed), id + " " + seed);
                picks++;
            }
            Assert.GreaterOrEqual(plain, 3);
            Assert.GreaterOrEqual(picks, 150);
        }

        [Test]
        public void PerMission_LargeN_StaysInsideThreeSigma()
        {
            // 20000 seeds per weighted row (19 missions plus the default): about 400k hashes, well under a second.
            const int N = 20000;
            var table = LaunchTimeTable.Load(Golden.Root);
            var o = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "time-of-day.json")));
            var missions = o.Obj("missions");
            int cells = 0;
            foreach (var id in missions.Order)
            {
                if (table.TryFixed(id, out _)) continue;
                cells += CheckBands(table, id, N);
            }
            cells += CheckBands(table, "not-a-mission", N);
            Assert.AreEqual(60, cells, "19 weighted missions + the default, three buckets each");
        }

        [Test]
        public void InstalledTable_IsUsedWithoutACheckout()
        {
            // A device build has no data/ folder. LaunchTimeBoot (Kestrel.Game.Shell) installs the bundled
            // Resources/KestrelData/time-of-day text through UseJson; PickTimeOfDay must then use only that.
            Assert.AreEqual("KestrelData/time-of-day", LaunchTime.ResourcePath);
            Assert.IsTrue(File.Exists(Path.Combine(Golden.Root, "data", "time-of-day.json")), "CI copies data/*.json into Resources/KestrelData");
            try
            {
                LaunchTime.UseJson("{\"version\":1,\"default\":{\"day\":0,\"sunset\":0,\"night\":1},\"missions\":{\"KH-1\":{\"fixed\":\"Sunset\"}}}");
                Assert.IsTrue(LaunchTime.HasTable);
                for (ulong seed = 0; seed < 32; seed++)
                {
                    Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay("KH-1", seed));
                    Assert.AreEqual(LaunchTimeOfDay.Night, LaunchTime.PickTimeOfDay("KH-7", seed));
                }

                var bundled = File.ReadAllText(Path.Combine(Golden.Root, "data", "time-of-day.json"));
                var installed = LaunchTime.UseJson(bundled);
                var fromDisk = LaunchTimeTable.Load(Golden.Root);
                foreach (var id in new[] { "KH-1", "KH-3", "KH-7", "KH-20", "KH-99" })
                    for (ulong seed = 0; seed < 64; seed++)
                    {
                        Assert.AreEqual(fromDisk.Pick(id, seed), installed.Pick(id, seed));
                        Assert.AreEqual(fromDisk.Pick(id, seed), LaunchTime.PickTimeOfDay(id, seed));
                    }

                Assert.Throws<FormatException>(() => LaunchTime.UseJson("{\"missions\":{}}"));
            }
            finally
            {
                LaunchTime.UseTable(null);
            }
            Assert.IsFalse(LaunchTime.HasTable);
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-1", 3), "reloads the checkout table after reset");
        }

        static int CheckBands(LaunchTimeTable table, string id, int n)
        {
            table.Chances(id, out int wd, out int ws, out int wn);
            double total = wd + ws + wn;
            int day = 0, sunset = 0, night = 0;
            for (int i = 0; i < n; i++)
            {
                switch (table.Pick(id, (ulong)i))
                {
                    case LaunchTimeOfDay.Day: day++; break;
                    case LaunchTimeOfDay.Sunset: sunset++; break;
                    default: night++; break;
                }
            }
            Band(id, "day", day, wd / total, n);
            Band(id, "sunset", sunset, ws / total, n);
            Band(id, "night", night, wn / total, n);
            return 3;
        }

        static void Band(string id, string what, int got, double p, int n)
        {
            double band = 3 * Math.Sqrt(p * (1 - p) / n);
            double f = got / (double)n;
            Assert.LessOrEqual(Math.Abs(f - p), band, id + " " + what + ": " + f.ToString("F5", CultureInfo.InvariantCulture) + " vs " + p.ToString("F5", CultureInfo.InvariantCulture));
        }

        static ulong BytesFnv(byte[] bytes)
        {
            ulong h = 14695981039346656037UL;
            foreach (var b in bytes) { h ^= b; h *= 1099511628211UL; }
            return h;
        }

        // The #272 implementation: UTF-16 code units, then 8 LE seed bytes.
        static ulong Utf16Hash(string s, ulong seed)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 1099511628211UL; }
            for (int i = 0; i < 8; i++) { h ^= (seed >> (8 * i)) & 0xFFUL; h *= 1099511628211UL; }
            return h;
        }
    }
}
