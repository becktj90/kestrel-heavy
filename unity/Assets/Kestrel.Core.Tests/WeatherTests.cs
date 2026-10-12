// P4 weather generator + LCC goldens. Bands: docs/PHYSICS.md § Weather (P4).
// Flight.Weather.Hold is unchanged (params / timeline goldens).
//
// External seed→day golden (mulberry32 + Rayleigh/Box-Muller; verified C# == Python):
//   Haven calibrated to NOAA USW00012838; April mean wind 4.46 m/s, temp 22.11 °C;
//   shear = 0.002 + 0.016*u9. Seed=1, doy=100.
//
// Launch-anyway penalty weights (fictional game abstraction; lightning has no weight):
//   wind 0.15, gust 0.10, precip 0.20, ceiling 0.15, temp 0.10, shear 0.20
//   wind-only → 0.15; wind+gust → 0.25; all soft → 0.90
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

        // Hard-coded literals: seed=1, dayOfYear=100 (April), Haven @ NOAA Melbourne means.
        const double Seed1WindMs = 3.4379895008869417;
        const double Seed1GustMs = 3.95697980926321;
        const double Seed1DirDeg = 189.8809343855828;
        const double Seed1TempC = 22.58939451175416;
        const double Seed1Cloud = 0.281103502959013;
        const double Seed1CeilingM = 5051.309561120216;
        const double Seed1LightningKm = 131.09563425183296;
        const double Seed1ShearPerSec = 0.017917167130857704;
        // External launch-anyway penalty literals (fictional weights above).
        const double PenaltyWindOnly = 0.15;
        const double PenaltyWindGust = 0.25;
        const double PenaltyAllSoft = 0.90;

        // NOAA NCEI U.S. Climate Normals 1991–2020 — Melbourne Intl AP, FL (USW00012838).
        // Temp: MLY-TAVG-NORMAL °F→°C. Wind: monthly mean HLY-WIND-AVGSPD mph→m/s.
        // Interannual temp σ: MLY-TAVG-STDDEV °F→°C. Wind interannual: fictional 10% of mean.
        // Thunder p: fictional Florida-coastal (~80 days/yr); interannual spread 0.05 abs.
        static readonly double[] NoaaMeanWindMs =
        {
            3.91, 4.05, 4.35, 4.46, 4.31, 3.60, 3.24, 3.30, 3.71, 4.17, 4.13, 3.75,
        };
        static readonly double[] NoaaMeanTempC =
        {
            16.50, 17.78, 19.67, 22.11, 24.89, 27.00, 27.61, 27.78, 27.00, 24.61, 20.56, 18.17,
        };
        static readonly double[] NoaaInterannualTempSigmaC =
        {
            1.611, 1.889, 1.722, 1.278, 0.778, 0.611, 0.611, 0.722, 0.556, 1.056, 1.500, 2.167,
        };
        static readonly double[] NoaaInterannualWindSigmaMs =
        {
            0.391, 0.405, 0.435, 0.446, 0.431, 0.360, 0.324, 0.330, 0.371, 0.417, 0.413, 0.375,
        };
        static readonly double[] ThunderDayProbability =
        {
            0.05, 0.06, 0.10, 0.15, 0.25, 0.40, 0.50, 0.48, 0.35, 0.15, 0.08, 0.05,
        };
        const double ThunderInterannualSpread = 0.05;
        const double TempDrawSigmaC = 2.5;
        const string NoaaStationId = "USW00012838";

        [Test]
        public void FlightWeatherHold_Unchanged_KidNeverHolds()
        {
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
        public void Shear_GeneratedDay_CanViolateLimit()
        {
            // Seed 1 / doy 100 draws shear ≈ 0.0179 /s > ShearLimitPerSec 0.012.
            var day = WeatherGenerator.Generate(SiteClimatology.HavenId, 100, 1);
            Assert.Greater(day.shearPerSec, LaunchCommit.ShearLimitPerSec,
                "generator tail must be able to exceed the shear LCC");
            var r = LaunchCommit.Evaluate(day, "STANDARD");
            Assert.IsTrue(r.shear, "shear violation flag");
            Assert.AreEqual(LccVerdict.Hold, r.verdict);
        }

        [Test]
        public void Climatology_MonthlyMeans_VsNoaaMelbourne()
        {
            // Non-circular: compare generator sample means to hard-coded NOAA USW00012838 literals
            // (not to SiteClimatology). Bands = 3σ/√N + interannual spread.
            Assert.AreEqual(NoaaStationId, SiteClimatology.NoaaStationId);
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
                double expectW = NoaaMeanWindMs[month - 1];
                double expectT = NoaaMeanTempC[month - 1];
                double expectTh = ThunderDayProbability[month - 1];
                // Rayleigh day-to-day σ ≈ 0.5227 μ
                double windSigma = expectW * Math.Sqrt(4.0 / Math.PI - 1.0);
                double windBand = 3.0 * windSigma / Math.Sqrt(N) + NoaaInterannualWindSigmaMs[month - 1];
                double tempBand = 3.0 * TempDrawSigmaC / Math.Sqrt(N) + NoaaInterannualTempSigmaC[month - 1];
                double thBand = 3.0 * Math.Sqrt(expectTh * (1.0 - expectTh) / N) + ThunderInterannualSpread;
                Assert.AreEqual(expectW, meanW, windBand, "mean wind month " + month);
                Assert.AreEqual(expectT, meanT, tempBand, "mean temp month " + month);
                Assert.AreEqual(expectTh, thFreq, thBand, "thunder freq month " + month);
            }
        }

        [Test]
        public void Lcc_TruthTable_ThirtyHandCases()
        {
            // 30 exact cases. Lightning → Hold + 30 min (not Scrub). Standard commitAllowed; KID never holds.
            var cases = new (WeatherDay day, string diff, bool relaxed, LccVerdict expect, int nViol, int holdMin)[]
            {
                (LaunchCommit.HandCase(), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(windMs: 12.0), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(windMs: 12.01), "STANDARD", false, LccVerdict.Hold, 1, 0),
                (LaunchCommit.HandCase(gustMs: 16.01), "STANDARD", false, LccVerdict.Hold, 1, 0),
                (LaunchCommit.HandCase(lightningKm: 10), "STANDARD", false, LccVerdict.Hold, 1, 30),
                (LaunchCommit.HandCase(precip: true), "STANDARD", false, LccVerdict.Hold, 1, 0),
                (LaunchCommit.HandCase(ceilingM: 1499), "STANDARD", false, LccVerdict.Hold, 1, 0),
                (LaunchCommit.HandCase(tempC: 1.9), "STANDARD", false, LccVerdict.Hold, 1, 0),
                (LaunchCommit.HandCase(tempC: 35.1), "STANDARD", false, LccVerdict.Hold, 1, 0),
                (LaunchCommit.HandCase(shearPerSec: 0.013), "STANDARD", false, LccVerdict.Hold, 1, 0),
                (LaunchCommit.HandCase(windMs: 13, gustMs: 17), "STANDARD", false, LccVerdict.Hold, 2, 0),
                (LaunchCommit.HandCase(windMs: 13, lightningKm: 5), "STANDARD", false, LccVerdict.Hold, 2, 30),
                (LaunchCommit.HandCase(precip: true, ceilingM: 1000), "STANDARD", false, LccVerdict.Hold, 2, 0),
                (LaunchCommit.HandCase(tempC: 40, shearPerSec: 0.04), "STANDARD", false, LccVerdict.Hold, 2, 0),
                (LaunchCommit.HandCase(windMs: 20, precip: true, ceilingM: 500), "STANDARD", false, LccVerdict.Hold, 3, 0),
                (LaunchCommit.HandCase(windMs: 20), "KID", false, LccVerdict.Go, 1, 0),
                (LaunchCommit.HandCase(lightningKm: 1), "KID", false, LccVerdict.Go, 1, 0),
                (LaunchCommit.HandCase(windMs: 20), "STANDARD", true, LccVerdict.Go, 1, 0),
                (LaunchCommit.HandCase(lightningKm: 1), "CADET", true, LccVerdict.Go, 1, 0),
                (LaunchCommit.HandCase(windMs: 0, gustMs: 0), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(ceilingM: 1500), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(tempC: 2.0), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(tempC: 35.0), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(shearPerSec: 0.012), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(gustMs: 16.0), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(lightningKm: 18.52), "STANDARD", false, LccVerdict.Go, 0, 0),
                (LaunchCommit.HandCase(lightningKm: 18.519), "STANDARD", false, LccVerdict.Hold, 1, 30),
                (LaunchCommit.HandCase(windMs: 12.5, tempC: -5, precip: true), "STANDARD", false, LccVerdict.Hold, 3, 0),
                (LaunchCommit.HandCase(windMs: 15, gustMs: 20, lightningKm: 3, precip: true), "STANDARD", false, LccVerdict.Hold, 4, 30),
                (LaunchCommit.HandCase(windMs: 15, gustMs: 20, lightningKm: 3, precip: true), "KID", false, LccVerdict.Go, 4, 0),
            };
            Assert.AreEqual(30, cases.Length);
            for (int i = 0; i < cases.Length; i++)
            {
                var c = cases[i];
                var r = LaunchCommit.Evaluate(c.day, c.diff, c.relaxed);
                Assert.AreEqual(c.expect, r.verdict, "case " + i + " verdict");
                Assert.AreEqual(c.nViol, r.violationCount, "case " + i + " nViol");
                Assert.AreEqual(c.holdMin, r.holdMinutes, "case " + i + " holdMinutes");
                bool expectAllowed = !(c.diff == "KID" || c.relaxed);
                Assert.AreEqual(expectAllowed, r.commitAllowed, "case " + i + " allowed");
            }
        }

        [Test]
        public void Ensemble_WindExceedance_MatchesRayleigh_LargeN()
        {
            // Exact Rayleigh: P(W > L) = exp(−L²/(2s²)), s = μ/√(π/2). Band = 3√(p(1−p)/N).
            const int N = 10000;
            uint seed = 42;
            int doy = 15; // January
            double mu = NoaaMeanWindMs[0];
            double analytic = WeatherEnsemble.RayleighExceedance(mu, LaunchCommit.SurfaceWindLimitMs);
            double band = WeatherEnsemble.BinomialThreeSigmaBand(analytic, N);
            int windViol = 0;
            for (int i = 0; i < N; i++)
            {
                uint ms = Mix(seed, (uint)(i + 1));
                var d = WeatherGenerator.Generate(SiteClimatology.HavenId, doy, ms);
                if (d.windMs > LaunchCommit.SurfaceWindLimitMs) windViol++;
            }
            double sample = windViol / (double)N;
            Assert.AreEqual(analytic, sample, band, "Rayleigh wind exceedance large-N");
            // Small ensemble still runs and reports members.
            var ens = WeatherEnsemble.Run(SiteClimatology.HavenId, doy, seed, 50);
            Assert.AreEqual(50, ens.members);
        }

        static int MonthMidDoy(int month)
        {
            int[] end = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
            return (end[month - 1] + end[month] + 1) / 2;
        }

        [Test]
        public void LaunchAnyway_Penalty_GameAbstractionLiterals()
        {
            var wind = LaunchCommit.Evaluate(LaunchCommit.HandCase(windMs: 13), "STANDARD");
            Assert.AreEqual(LccVerdict.Hold, wind.verdict);
            Assert.IsTrue(LaunchAnyway.CanOverride(wind));
            Assert.AreEqual(PenaltyWindOnly, LaunchAnyway.ReliabilityPenalty(wind), 1e-12);

            var windGust = LaunchCommit.Evaluate(LaunchCommit.HandCase(windMs: 13, gustMs: 17), "STANDARD");
            Assert.AreEqual(PenaltyWindGust, LaunchAnyway.ReliabilityPenalty(windGust), 1e-12);

            // Lightning Hold: 30-min clock, not overridable; no lightning weight in the penalty.
            var lightning = LaunchCommit.Evaluate(LaunchCommit.HandCase(lightningKm: 5), "STANDARD");
            Assert.AreEqual(LccVerdict.Hold, lightning.verdict);
            Assert.AreEqual(30, lightning.holdMinutes);
            Assert.IsFalse(LaunchAnyway.CanOverride(lightning));
            Assert.AreEqual(0, LaunchAnyway.ReliabilityPenalty(lightning), 0);

            var soft = LaunchCommit.Evaluate(
                LaunchCommit.HandCase(windMs: 13, gustMs: 17, precip: true, ceilingM: 1000, tempC: 40, shearPerSec: 0.04),
                "STANDARD");
            Assert.AreEqual(PenaltyAllSoft, LaunchAnyway.ReliabilityPenalty(soft), 1e-12);

            var kid = LaunchCommit.Evaluate(LaunchCommit.HandCase(windMs: 20), "KID");
            Assert.AreEqual(0, LaunchAnyway.ReliabilityPenalty(kid), 0);
            Assert.IsFalse(LaunchAnyway.CanOverride(kid));
        }

        [Test]
        public void WeatherCommit_DrivesMissionEventHold()
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

            // Lightning is a Hold (30 min), not a scrub/Failed.
            poll.status.state = TimelineStatus.Pending;
            var lightning = LaunchCommit.Evaluate(LaunchCommit.HandCase(lightningKm: 5), "STANDARD");
            Assert.AreEqual(LccVerdict.Hold, lightning.verdict);
            Assert.AreEqual(30, lightning.holdMinutes);
            Assert.IsTrue(WeatherCommit.Apply(lightning, poll));
            Assert.AreEqual(TimelineStatus.Held, poll.status.state);

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
