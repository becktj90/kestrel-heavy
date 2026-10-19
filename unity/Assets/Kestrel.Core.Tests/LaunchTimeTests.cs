// Launch time of day. Literals below were computed outside this assembly:
// FNV-1a 64, offset 14695981039346656037, prime 1099511628211,
// UTF-16 code units of the id, then the seed as 8 little-endian bytes.
// slot = hash % (day+sunset+night), buckets Day, Sunset, Night in that order.
// A fixed row skips the hash. Unknown ids use default 60/25/15.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LaunchTimeTests
    {
        [Test]
        public void Pick_IsDeterministic()
        {
            var ids = ShippedIds();
            foreach (var id in ids)
            {
                for (ulong seed = 0; seed < 8; seed++)
                {
                    var once = LaunchTime.PickTimeOfDay(id, seed);
                    var twice = LaunchTime.PickTimeOfDay(id, seed);
                    Assert.AreEqual(once, twice, id + " seed " + seed);
                }
            }
            Assert.AreEqual(LaunchTime.PickTimeOfDay("KH-99", 11), LaunchTime.PickTimeOfDay("KH-99", 11));
        }

        [Test]
        public void DifferentSeeds_VaryInsideTheWeights()
        {
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-7", 42));
            Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay("KH-7", 43));

            var seen = new HashSet<LaunchTimeOfDay>();
            for (ulong seed = 0; seed < 500; seed++) seen.Add(LaunchTime.PickTimeOfDay("KH-9", seed));
            Assert.AreEqual(3, seen.Count, "KH-9 weights include Day, Sunset, and Night");

            seen.Clear();
            for (ulong seed = 0; seed < 200; seed++) seen.Add(LaunchTime.PickTimeOfDay("KH-3", seed));
            Assert.AreEqual(3, seen.Count, "an early night mission still changes with the seed");
        }

        [Test]
        public void FixedOverride_PinsKh1ToDay()
        {
            var table = LaunchTimeTable.Load(Golden.Root);
            Assert.IsTrue(table.TryFixed("KH-1", out var pin));
            Assert.AreEqual(LaunchTimeOfDay.Day, pin);
            for (ulong seed = 0; seed < 64; seed++)
                Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-1", seed));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-1", 999983));

            const string json = "{\"default\":{\"day\":0,\"sunset\":0,\"night\":1},\"missions\":{\"STORY\":{\"fixed\":\"Sunset\",\"day\":100,\"sunset\":0,\"night\":0}}}";
            var synthetic = LaunchTimeTable.Parse(json);
            for (ulong seed = 0; seed < 16; seed++)
                Assert.AreEqual(LaunchTimeOfDay.Sunset, synthetic.Pick("STORY", seed));
        }

        [Test]
        public void DefaultWeights_AreRoughlyRespected()
        {
            var table = LaunchTimeTable.Load(Golden.Root);
            table.Chances("not-a-mission", out int day, out int sunset, out int night);
            Assert.AreEqual(60, day);
            Assert.AreEqual(25, sunset);
            Assert.AreEqual(15, night);

            const int seeds = 2000;
            Count("not-a-mission", seeds, out int d, out int s, out int n);
            Assert.AreEqual(0.60, d / (double)seeds, 0.04);
            Assert.AreEqual(0.25, s / (double)seeds, 0.04);
            Assert.AreEqual(0.15, n / (double)seeds, 0.04);
        }

        [Test]
        public void FixedPairs_MatchIndependentLiterals()
        {
            Assert.AreEqual(0xC39D71C85DA55BA0UL, LaunchTime.Hash("KH-1", 0));
            Assert.AreEqual(0x5EE1AAD81086F3B8UL, LaunchTime.Hash("KH-2", 1));
            Assert.AreEqual(0x4BD7A317074C5B62UL, LaunchTime.Hash("", 7));
            Assert.AreEqual(LaunchTime.Hash("", 7), LaunchTime.Hash(null, 7));

            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-1", 0));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-1", 999983));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-2", 1));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-2", 42));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-3", 0));
            Assert.AreEqual(LaunchTimeOfDay.Night, LaunchTime.PickTimeOfDay("KH-3", 2));
            Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay("KH-3", 14));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-7", 42));
            Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay("KH-7", 43));
            Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay("KH-15", 100));
            Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay("KH-18", 7));
            Assert.AreEqual(LaunchTimeOfDay.Night, LaunchTime.PickTimeOfDay("KH-20", 123456789UL));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-99", 1));
            Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay("KH-99", 2));
            Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay("", 7));
            Assert.AreEqual(LaunchTimeOfDay.Sunset, LaunchTime.PickTimeOfDay(null, 7));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("no-such-mission", 11));
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("m1", 3));
        }

        [Test]
        public void UnknownMission_FallsBackToTheDefault()
        {
            var table = LaunchTimeTable.Load(Golden.Root);
            Assert.IsFalse(table.Contains("KH-99"));
            Assert.IsFalse(table.Contains("m1"));
            Assert.IsFalse(table.Contains(""));
            Assert.IsFalse(table.Contains(null));
            Assert.IsFalse(table.TryFixed("KH-99", out _));

            table.Chances("KH-99", out int a, out int b, out int c);
            table.Chances("m1", out int d, out int e, out int f);
            table.Chances(null, out int g, out int h, out int i);
            Assert.AreEqual(60, a); Assert.AreEqual(25, b); Assert.AreEqual(15, c);
            Assert.AreEqual(a, d); Assert.AreEqual(b, e); Assert.AreEqual(c, f);
            Assert.AreEqual(a, g); Assert.AreEqual(b, h); Assert.AreEqual(c, i);

            Assert.Throws<FormatException>(() => LaunchTimeTable.Parse(
                "{\"default\":{\"day\":1,\"sunset\":0,\"night\":0},\"missions\":{\"X\":{\"fixed\":\"Noon\"}}}"));
        }

        [Test]
        public void ShippedMix_LandsNearHalfDayAndAQuarterEachOfTheOthers()
        {
            var table = LaunchTimeTable.Load(Golden.Root);
            var campaign = Campaign.Load(Golden.Root);
            var ids = ShippedIds();
            Assert.AreEqual(20, ids.Count);
            Assert.AreEqual(20, campaign.Missions.Count);

            long day = 0, sunset = 0, night = 0;
            foreach (var id in ids)
            {
                Assert.IsTrue(table.Contains(id), id);
                table.Chances(id, out int d, out int s, out int n);
                Assert.AreEqual(100, d + s + n, id);
                day += d; sunset += s; night += n;
            }
            Assert.AreEqual(1000, day);
            Assert.AreEqual(500, sunset);
            Assert.AreEqual(500, night);

            Assert.IsTrue(table.TryFixed("KH-1", out var kh1));
            Assert.AreEqual(LaunchTimeOfDay.Day, kh1);
            Assert.AreEqual(LaunchTimeOfDay.Day, LaunchTime.PickTimeOfDay("KH-1", 1));

            const int seeds = 1000;
            int gotDay = 0, gotSunset = 0, gotNight = 0;
            foreach (var id in ids)
            {
                Count(id, seeds, out int d, out int s, out int n);
                gotDay += d; gotSunset += s; gotNight += n;
            }
            int total = gotDay + gotSunset + gotNight;
            Assert.AreEqual(0.50, gotDay / (double)total, 0.03, "day");
            Assert.AreEqual(0.25, gotSunset / (double)total, 0.03, "sunset");
            Assert.AreEqual(0.25, gotNight / (double)total, 0.03, "night");
        }

        [Test]
        public void EarlyMissions_IncludeNightByKh5()
        {
            var table = LaunchTimeTable.Load(Golden.Root);
            int nightMissions = 0;
            int nightMass = 0;
            foreach (var id in new[] { "KH-3", "KH-4", "KH-5" })
            {
                table.Chances(id, out int day, out int sunset, out int night);
                Assert.IsFalse(table.TryFixed(id, out _), id + " still varies with the seed");
                if (night > day && night > sunset) nightMissions++;
                nightMass += night;
                Count(id, 500, out int d, out int s, out int n);
                Assert.Greater(n, d, id);
                Assert.Greater(n, s, id);
            }
            Assert.GreaterOrEqual(nightMissions, 2, "a few night missions in KH-3..KH-5");
            Assert.GreaterOrEqual(nightMass, 200, "expected night count across the three is at least 2");
        }

        [Test]
        public void GeoAndLate_SkewNightOrSunset()
        {
            var table = LaunchTimeTable.Load(Golden.Root);
            var campaign = Campaign.Load(Golden.Root);
            int skewed = 0;
            foreach (var mission in campaign.Missions)
            {
                bool geo = mission.PayloadId == "gto-comsat" || mission.PayloadId == "geo-relay";
                bool late = mission.Index >= 16;
                if (!geo && !late) continue;
                string id = MissionContext.LabelFor(mission.Index);
                table.Chances(id, out int day, out int sunset, out int night);
                Assert.IsFalse(table.TryFixed(id, out _), id);
                Assert.Greater(Math.Max(sunset, night), day, id);
                Assert.Less(day, sunset + night, id);
                skewed++;
            }
            Assert.GreaterOrEqual(skewed, 8);

            table.Chances("KH-8", out int crewDay, out int crewSunset, out int crewNight);
            Assert.Greater(crewDay, crewSunset);
            Assert.Greater(crewDay, crewNight);
            table.Chances("KH-18", out int jarDay, out int jarSunset, out int jarNight);
            Assert.Greater(jarNight, jarDay);
            Assert.Greater(jarNight, jarSunset);
        }

        static List<string> ShippedIds()
        {
            var campaign = Campaign.Load(Golden.Root);
            var ids = new List<string>();
            var seen = new HashSet<int>();
            foreach (var mission in campaign.Missions)
            {
                Assert.IsTrue(seen.Add(mission.Index), "duplicate campaign index");
                ids.Add(MissionContext.LabelFor(mission.Index));
            }
            ids.Sort(StringComparer.Ordinal);
            for (int i = 1; i <= 20; i++)
                Assert.IsTrue(ids.Contains("KH-" + i), "KH-" + i);
            return ids;
        }

        static void Count(string id, int seeds, out int day, out int sunset, out int night)
        {
            day = sunset = night = 0;
            for (int i = 0; i < seeds; i++)
            {
                switch (LaunchTime.PickTimeOfDay(id, (ulong)i))
                {
                    case LaunchTimeOfDay.Day: day++; break;
                    case LaunchTimeOfDay.Sunset: sunset++; break;
                    default: night++; break;
                }
            }
        }
    }
}
