// Reduced ensemble around a deterministic day draw for "probability of LCC violation".
// Member i uses seed ⊕ (i+1) through a splittable mix (order-independent).
using System;

namespace Kestrel.Core.Weather
{
    public struct EnsembleResult
    {
        public int members;
        public int violateCount;
        public double violateFraction;
        public double meanWindMs;
        public double meanTempC;
    }

    public static class WeatherEnsemble
    {
        /// <summary>
        /// Run <paramref name="members"/> draws around <paramref name="seed"/> for the same site/doy.
        /// A member "violates" when Evaluate reports Hold or Scrub under Standard (commitAllowed).
        /// </summary>
        public static EnsembleResult Run(string siteId, int dayOfYear, uint seed, int members = 50,
            string difficulty = "STANDARD")
        {
            if (members < 1) throw new ArgumentOutOfRangeException(nameof(members));
            int violate = 0;
            double sumWind = 0, sumTemp = 0;
            for (int i = 0; i < members; i++)
            {
                uint memberSeed = Mix(seed, (uint)(i + 1));
                var day = WeatherGenerator.Generate(siteId, dayOfYear, memberSeed, difficulty);
                sumWind += day.windMs;
                sumTemp += day.tempC;
                var lcc = LaunchCommit.Evaluate(day, difficulty, relaxed: false);
                if (lcc.verdict != LccVerdict.Go) violate++;
            }
            return new EnsembleResult
            {
                members = members,
                violateCount = violate,
                violateFraction = violate / (double)members,
                meanWindMs = sumWind / members,
                meanTempC = sumTemp / members,
            };
        }

        /// <summary>
        /// Exact Rayleigh exceedance P(W &gt; L) = exp(−L² / (2 s²)) with scale s = μ / √(π/2).
        /// Surface wind is drawn Rayleigh with monthly mean μ.
        /// </summary>
        public static double RayleighExceedance(double mean, double limit)
        {
            if (mean <= 0) return limit < 0 ? 1.0 : 0.0;
            double s = mean / Math.Sqrt(Math.PI / 2.0);
            double x = limit / s;
            return Math.Exp(-0.5 * x * x);
        }

        /// <summary>
        /// Lognormal exceedance P(X &gt; L) = Φ̄((ln L − μ) / σ) with μ = ln(median).
        /// </summary>
        public static double LognormalExceedance(double median, double sigmaLn, double limit)
        {
            if (median <= 0 || sigmaLn <= 0) return limit < median ? 1.0 : 0.0;
            if (limit <= 0) return 1.0;
            double z = (Math.Log(limit) - Math.Log(median)) / sigmaLn;
            return NormalUpperTail(z);
        }

        /// <summary>Binomial 3σ sampling band: 3 √(p(1−p)/N).</summary>
        public static double BinomialThreeSigmaBand(double p, int n)
        {
            if (n <= 0) return 1.0;
            p = Math.Max(0, Math.Min(1, p));
            return 3.0 * Math.Sqrt(p * (1.0 - p) / n);
        }

        /// <summary>Φ̄(z) = 1 − Φ(z) via Abramowitz &amp; Stegun 7.1.26 erfc.</summary>
        public static double NormalUpperTail(double z)
        {
            return 0.5 * Erfc(z / Math.Sqrt(2.0));
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

        static double Erfc(double x)
        {
            double ax = Math.Abs(x);
            double t = 1.0 / (1.0 + 0.3275911 * ax);
            double poly = t * (0.254829592 + t * (-0.284496736 + t * (1.421413741 + t * (-1.453152027 + t * 1.061405429))));
            double ans = poly * Math.Exp(-ax * ax);
            return x >= 0 ? ans : 2.0 - ans;
        }
    }
}
