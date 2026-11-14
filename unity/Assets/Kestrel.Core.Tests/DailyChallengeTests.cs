using System;
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
    }
}
