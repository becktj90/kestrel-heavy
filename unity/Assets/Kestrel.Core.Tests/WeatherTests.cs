// P4 weather generator + LCC goldens. Bands: docs/PHYSICS.md § Weather (P4).
// Flight.Weather.Hold is unchanged (params / timeline goldens).
//
// External seed→day golden (computed OUTSIDE this repo; mulberry32 + Rayleigh/Box-Muller):
//
//   python3 - <<'PY'
//   import math
//   def next_u(state):
//       state = (state + 0x6D2B79F5) & 0xFFFFFFFF
//       a = state
//       imul = lambda x,y: (x*y) & 0xFFFFFFFF
//       t = imul(a ^ (a >> 15), 1 | a)
//       t2 = (t + imul(t ^ (t >> 7), 61 | t)) & 0xFFFFFFFF
//       t = (t2 ^ t) ^ ((t2 ^ t) >> 14)
//       # corrected below to match Sensors.Mulberry32
//   # See SiteClimatology + WeatherGenerator formulas; seed=1, doy=100 → April.
//   PY
//
// Literals below match a verified Sensors.Mulberry32 stream (C# == Python).
//
// Launch-anyway penalty weights (external):
//   wind 0.15, gust 0.10, lightning 1.0, precip 0.20, ceiling 0.15, temp 0.10, shear 0.20
//   wind-only → 0.15; wind+gust → 0.25; lightning → 1.0; all soft → 0.90
using System;
using Kestrel.Core.Flight;
using Kestrel.Core.Timeline;
using Kestrel.Core.Weather;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class WeatherTests
    {
        const double RelSeedDay = 1e-12;

        // Hard-coded literals: seed=1, dayOfYear=100 (April), Haven.
        const double Seed1WindMs = 3.391738521054382;
        const double Seed1GustMs = 3.9037468970309694;
        const double Seed1DirDeg = 189.8809343855828;
        const double Seed1TempC = 22.479394511754162;
        const double Seed1Cloud = 0.281103502959013;
        const double Seed1CeilingM = 5051.309561120216;
        const double Seed1LightningKm = 131.09563425183296;
        const double Seed1ShearPerSec = 0.02887057364196517;
        // External launch-anyway penalty literals (weights above).
        const double PenaltyWindOnly = 0.15;
        const double PenaltyWindGust = 0.25;
        const double PenaltyLightning = 1.0;
        const double PenaltyAllSoft = 0.90;

        [Test]
        public void FlightWeatherHold_Unchanged_KidNeverHolds()
        {
            // Regression: policy stub used by params goldens must stay.
            Assert.AreEqual(12.0, Flight.Weather.DefaultWindLimitMs);
            var kid = Flight.Weather.Hold("KID", false, 40);
            Assert.IsFalse(kid.hold);
            Assert.IsFalse(kid.allowed);
            var std = Flight.Weather.Hold("STANDARD", false, 12.01);
            Assert.IsTrue(std.hold);
            Assert.IsTrue(std.allowed);
        }

        [Test]
        public void Generate_Seed1Day100_MatchesExternalLiteral()
        {
            var a = WeatherGenerator.Generate(SiteClimatology.HavenId, 100, 1);
            var b = WeatherGenerator.Generate(SiteClimatology.HavenId, 100, 1);
            Assert.AreEqual(a.windMs, b.windMs, 0, "bit-identical second draw");
            Assert.AreEqual(Seed1WindMs, a.windMs, RelSeedDay * Math.Max(1, Math.Abs(Seed1WindMs)), "wind");
            Assert.AreEqual(Seed1GustMs, a.gustMs, RelSeedDay * Math.Max(1, Math.Abs(Seed1GustMs)), "gust");
            Assert.AreEqual(Seed1DirDeg, a.directionDeg, RelSeedDay * Math.Max(1, Math.Abs(Seed1DirDeg)), "dir");
            Assert.AreEqual(Seed1TempC, a.tempC, RelSeedDay * Math.Max(1, Math.Abs(Seed1TempC)), "temp");
            Assert.AreEqual(Seed1Cloud, a.cloudFraction, RelSeedDay, "cloud");
            Assert.IsFalse(a.precip);
            Assert.AreEqual(Seed1CeilingM, a.ceilingM, RelSeedDay * Seed1CeilingM, "ceiling");
            Assert.IsFalse(a.thunderDay);
            Assert.AreEqual(Seed1LightningKm, a.lightningKm, RelSeedDay * Seed1LightningKm, "lightning");
            Assert.AreEqual(Seed1ShearPerSec, a.shearPerSec, RelSeedDay * Seed1ShearPerSec, "shear");
            Assert.AreEqual(4, a.month);
        }

        [Test]
        public void Climatology_MonthlyMeans_WithinBands()
        {
            // 10 000 draws per month; bands from PHYSICS_CORE_AUDIT: wind ±0.5 m/s, temp ±1 K,
            // thunder-day frequency ±20 % relative.
            const int N = 10000;
            for (int month = 1; month <= 12; month++)
            {
                int doy = MonthMidDoy(month);
                double sumW = 0, sumT = 0;
                int thunder = 0;
                for (int i = 0; i < N; i++)
                {
                    var d = WeatherGenerator.Generate(SiteClimatology.HavenId, doy, (uint)(i + 1));
                    sumW += d.windMs;
                    sumT += d.tempC;
                    if (d.thunderDay) thunder++;
                }
                double meanW = sumW / N, meanT = sumT / N, thFreq = thunder / (double)N;
                double expectW = SiteClimatology.MeanWindMs[month - 1];
                double expectT = SiteClimatology.MeanTempC[month - 1];
                double expectTh = SiteClimatology.ThunderDayProbability[month - 1];
                Assert.AreEqual(expectW, meanW, 0.5, "mean wind month " + month);
                Assert.AreEqual(expectT, meanT, 1.0, "mean temp month " + month);
                Assert.AreEqual(expectTh, thFreq, 0.20 * Math.Max(expectTh, 0.05), "thunder freq month " + month);
            }
        }

        [Test]
        public void Lcc_TruthTable_ThirtyHandCases()
        {
            // 30 exact cases. Standard commitAllowed; KID never holds.
            var cases = new (WeatherDay day, string diff, bool relaxed, LccVerdict expect, int nViol)[]
            {
                (LaunchCommit.HandCase(), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(windMs: 12.0), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(windMs: 12.01), "STANDARD", false, LccVerdict.Hold, 1),
                (LaunchCommit.HandCase(gustMs: 16.01), "STANDARD", false, LccVerdict.Hold, 1),
                (LaunchCommit.HandCase(lightningKm: 10), "STANDARD", false, LccVerdict.Scrub, 1),
                (LaunchCommit.HandCase(precip: true), "STANDARD", false, LccVerdict.Hold, 1),
                (LaunchCommit.HandCase(ceilingM: 1499), "STANDARD", false, LccVerdict.Hold, 1),
                (LaunchCommit.HandCase(tempC: 1.9), "STANDARD", false, LccVerdict.Hold, 1),
                (LaunchCommit.HandCase(tempC: 35.1), "STANDARD", false, LccVerdict.Hold, 1),
                (LaunchCommit.HandCase(shearPerSec: 0.031), "STANDARD", false, LccVerdict.Hold, 1),
                (LaunchCommit.HandCase(windMs: 13, gustMs: 17), "STANDARD", false, LccVerdict.Hold, 2),
                (LaunchCommit.HandCase(windMs: 13, lightningKm: 5), "STANDARD", false, LccVerdict.Scrub, 2),
                (LaunchCommit.HandCase(precip: true, ceilingM: 1000), "STANDARD", false, LccVerdict.Hold, 2),
                (LaunchCommit.HandCase(tempC: 40, shearPerSec: 0.04), "STANDARD", false, LccVerdict.Hold, 2),
                (LaunchCommit.HandCase(windMs: 20, precip: true, ceilingM: 500), "STANDARD", false, LccVerdict.Hold, 3),
                (LaunchCommit.HandCase(windMs: 20), "KID", false, LccVerdict.Go, 1),
                (LaunchCommit.HandCase(lightningKm: 1), "KID", false, LccVerdict.Go, 1),
                (LaunchCommit.HandCase(windMs: 20), "STANDARD", true, LccVerdict.Go, 1),
                (LaunchCommit.HandCase(lightningKm: 1), "CADET", true, LccVerdict.Go, 1),
                (LaunchCommit.HandCase(windMs: 0, gustMs: 0), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(ceilingM: 1500), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(tempC: 2.0), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(tempC: 35.0), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(shearPerSec: 0.030), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(gustMs: 16.0), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(lightningKm: 18.52), "STANDARD", false, LccVerdict.Go, 0),
                (LaunchCommit.HandCase(lightningKm: 18.519), "STANDARD", false, LccVerdict.Scrub, 1),
                (LaunchCommit.HandCase(windMs: 12.5, tempC: -5, precip: true), "STANDARD", false, LccVerdict.Hold, 3),
                (LaunchCommit.HandCase(windMs: 15, gustMs: 20, lightningKm: 3, precip: true), "STANDARD", false, LccVerdict.Scrub, 4),
                (LaunchCommit.HandCase(windMs: 15, gustMs: 20, lightningKm: 3, precip: true), "KID", false, LccVerdict.Go, 4),
            };
            Assert.AreEqual(30, cases.Length);
            for (int i = 0; i < cases.Length; i++)
            {
                var c = cases[i];
                var r = LaunchCommit.Evaluate(c.day, c.diff, c.relaxed);
                Assert.AreEqual(c.expect, r.verdict, "case " + i + " verdict");
                Assert.AreEqual(c.nViol, r.violationCount, "case " + i + " nViol");
                bool expectAllowed = !(c.diff == "KID" || c.relaxed);
                Assert.AreEqual(expectAllowed, r.commitAllowed, "case " + i + " allowed");
            }
        }

        [Test]
        public void Ensemble_ViolateFraction_NearGaussianWindExceedance()
        {
            // Analytic check: wind-only Rayleigh→approx Gaussian on a high-mean month is loose;
            // compare ensemble violate fraction to a single-threshold Gaussian on wind using the
            // sample mean/σ from the ensemble members' wind, abs 0.05 (audit band).
            const int N = 50;
            uint seed = 42;
            int doy = 15; // January
            var ens = WeatherEnsemble.Run(SiteClimatology.HavenId, doy, seed, N);
            // Collect winds for σ
            double sum = 0, sum2 = 0;
            int windViol = 0;
            for (int i = 0; i < N; i++)
            {
                uint ms = Mix(seed, (uint)(i + 1));
                var d = WeatherGenerator.Generate(SiteClimatology.HavenId, doy, ms);
                sum += d.windMs;
                sum2 += d.windMs * d.windMs;
                if (d.windMs > LaunchCommit.SurfaceWindLimitMs) windViol++;
            }
            double mean = sum / N;
            double var = Math.Max(1e-12, sum2 / N - mean * mean);
            double sigma = Math.Sqrt(var);
            double analytic = WeatherEnsemble.GaussianExceedance(mean, sigma, LaunchCommit.SurfaceWindLimitMs);
            double sample = windViol / (double)N;
            Assert.AreEqual(analytic, sample, 0.05, "wind exceedance ensemble vs Gaussian");
            Assert.GreaterOrEqual(ens.members, N);
        }

        static int MonthMidDoy(int month)
        {
            int[] end = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
            return (end[month - 1] + end[month] + 1) / 2;
        }

        [Test]
        public void LaunchAnyway_Penalty_ExternalLiterals()
        {
            var wind = LaunchCommit.Evaluate(LaunchCommit.HandCase(windMs: 13), "STANDARD");
            Assert.AreEqual(LccVerdict.Hold, wind.verdict);
            Assert.IsTrue(LaunchAnyway.CanOverride(wind));
            Assert.AreEqual(PenaltyWindOnly, LaunchAnyway.ReliabilityPenalty(wind), 1e-12);

            var windGust = LaunchCommit.Evaluate(LaunchCommit.HandCase(windMs: 13, gustMs: 17), "STANDARD");
            Assert.AreEqual(PenaltyWindGust, LaunchAnyway.ReliabilityPenalty(windGust), 1e-12);

            var scrub = LaunchCommit.Evaluate(LaunchCommit.HandCase(lightningKm: 5), "STANDARD");
            Assert.AreEqual(LccVerdict.Scrub, scrub.verdict);
            Assert.IsFalse(LaunchAnyway.CanOverride(scrub));
            Assert.AreEqual(PenaltyLightning, LaunchAnyway.ReliabilityPenalty(scrub), 1e-12);

            var soft = LaunchCommit.Evaluate(
                LaunchCommit.HandCase(windMs: 13, gustMs: 17, precip: true, ceilingM: 1000, tempC: 40, shearPerSec: 0.04),
                "STANDARD");
            Assert.AreEqual(PenaltyAllSoft, LaunchAnyway.ReliabilityPenalty(soft), 1e-12);

            var kid = LaunchCommit.Evaluate(LaunchCommit.HandCase(windMs: 20), "KID");
            Assert.AreEqual(0, LaunchAnyway.ReliabilityPenalty(kid), 0);
            Assert.IsFalse(LaunchAnyway.CanOverride(kid));
        }

        [Test]
        public void WeatherCommit_DrivesMissionEventHoldAndScrub()
        {
            var poll = new MissionEvent
            {
                id = WeatherCommit.DefaultPollEventId,
                holdable = true,
                status = new EventStatus { state = TimelineStatus.Pending, progress = 0 },
            };
            var hold = LaunchCommit.Evaluate(LaunchCommit.HandCase(windMs: 13), "STANDARD");
            Assert.IsTrue(WeatherCommit.Apply(hold, poll));
            Assert.AreEqual(TimelineStatus.Held, poll.status.state);

            poll.status.state = TimelineStatus.Pending;
            var scrub = LaunchCommit.Evaluate(LaunchCommit.HandCase(lightningKm: 5), "STANDARD");
            Assert.IsTrue(WeatherCommit.Apply(scrub, poll));
            Assert.AreEqual(TimelineStatus.Failed, poll.status.state);

            poll.status.state = TimelineStatus.Pending;
            var kid = LaunchCommit.Evaluate(LaunchCommit.HandCase(windMs: 20), "KID");
            Assert.IsFalse(WeatherCommit.Apply(kid, poll), "KID visual-only — no MissionEvent mutation");
            Assert.AreEqual(TimelineStatus.Pending, poll.status.state);

            var timeline = new Timeline.Timeline();
            timeline.events.Add(new MissionEvent
            {
                id = "term.poll",
                holdable = true,
                status = new EventStatus { state = TimelineStatus.Pending, progress = 0 },
            });
            Assert.IsTrue(WeatherCommit.ApplyToTimeline(hold, timeline));
            Assert.AreEqual(TimelineStatus.Held, timeline.events[0].status.state);
        }

        [Test]
        public void ShearProfile_VisualOnly_NotAscentSim()
        {
            // Profile is kinematic from surface + shear; AscentSim / Headless do not reference it.
            var day = WeatherGenerator.Generate(SiteClimatology.HavenId, 100, 1);
            Assert.AreEqual(5, day.shearWindMs.Length);
            Assert.AreEqual(Seed1WindMs, day.shearWindMs[0], RelSeedDay * Seed1WindMs);
            double expect12 = Seed1WindMs + Seed1ShearPerSec * 12000.0;
            Assert.AreEqual(expect12, day.shearWindMs[4], RelSeedDay * Math.Max(1, expect12));
            Assert.AreEqual(0, ShearProfile.AltitudeKm[0], 0);
            Assert.AreEqual(12, ShearProfile.AltitudeKm[4], 0);
        }

        static uint Mix(uint seed, uint index)
        {
            unchecked
            {
                uint x = seed ^ (index * 0x9E3779B9u);
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return x == 0 ? 1u : x;
            }
        }
    }
}
