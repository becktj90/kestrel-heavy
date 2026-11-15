using System;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DailyChallengeTests
    {
        [Test]
        public void SameUtcDay_SameSeedAndMission()
        {
            var campaign = Campaign.Load(Golden.Root);
            var a = DailyChallenge.Pick(campaign, 5, new DateTime(2026, 11, 14, 8, 0, 0, DateTimeKind.Utc));
            var b = DailyChallenge.Pick(campaign, 5, new DateTime(2026, 11, 14, 23, 59, 0, DateTimeKind.Utc));
            Assert.AreEqual(a.DayKey, b.DayKey);
            Assert.AreEqual(a.Seed, b.Seed);
            Assert.AreEqual(a.MissionId, b.MissionId);
            Assert.IsFalse(string.IsNullOrEmpty(a.MissionId));
        }

        [Test]
        public void DifferentDays_CanDiffer()
        {
            var campaign = Campaign.Load(Golden.Root);
            var a = DailyChallenge.Pick(campaign, 10, new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc));
            var b = DailyChallenge.Pick(campaign, 10, new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc));
            Assert.AreNotEqual(a.DayKey, b.DayKey);
            Assert.AreNotEqual(a.Seed, b.Seed);
        }

        [Test]
        public void PicksOnlyUnlockedMissions()
        {
            var campaign = Campaign.Load(Golden.Root);
            var pick = DailyChallenge.Pick(campaign, 0, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.LessOrEqual(pick.MissionIndex, 1);
        }

        [Test]
        public void Arm_CarriesSeedUntilTaken()
        {
            DailyChallenge.ClearArm();
            var campaign = Campaign.Load(Golden.Root);
            var pick = DailyChallenge.Pick(campaign, 3, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
            DailyChallenge.Arm(pick);
            Assert.AreEqual(pick.Seed, DailyChallenge.PeekSeed(pick.MissionId));
            Assert.AreEqual(pick.Seed, DailyChallenge.PeekSeed(pick.MissionId), "peek does not clear");
            Assert.AreEqual(pick.Seed, DailyChallenge.TakeSeed(pick.MissionId));
            Assert.IsNull(DailyChallenge.PeekSeed(pick.MissionId));
            Assert.IsNull(DailyChallenge.TakeSeed(pick.MissionId));
            Assert.IsNull(DailyChallenge.TakeSeed("other"));
        }

        [Test]
        public void SeedFor_StaysInsideTheCareerStoreRange()
        {
            // 2026-11-30's raw FNV hash is 8681904728260016564, above CareerStore.SafeInteger.
            // RecordAttempt rejects that and the report card used to be thrown away with it.
            ulong named = DailyChallenge.SeedFor(new DateTime(2026, 11, 30, 12, 0, 0, DateTimeKind.Utc));
            Assert.Greater(named, 0UL);
            Assert.LessOrEqual(named, (ulong)CareerStore.SafeInteger);
            var day = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            while (day < end)
            {
                ulong seed = DailyChallenge.SeedFor(day);
                Assert.Greater(seed, 0UL, day.ToString("yyyy-MM-dd"));
                Assert.LessOrEqual(seed, (ulong)CareerStore.SafeInteger, day.ToString("yyyy-MM-dd"));
                Assert.AreEqual(seed, DailyChallenge.SeedFor(day.AddHours(18)));
                day = day.AddDays(1);
            }
        }
    }
}
