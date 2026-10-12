// Monthly climatology targets for the fictional Haven launch site (Florida coastal stand-in).
// Targets are NOAA NCEI 1991–2020–class coastal normals (public domain); original site name.
// See LICENSES.md. Used to calibrate WeatherGenerator; goldens in WeatherTests.
namespace Kestrel.Core.Weather
{
    public static class SiteClimatology
    {
        public const string HavenId = "haven";

        /// <summary>Monthly mean surface wind (m/s), Jan…Dec. Haven / Florida coastal stand-in.</summary>
        public static readonly double[] MeanWindMs =
        {
            4.2, 4.3, 4.5, 4.4, 4.0, 3.6, 3.4, 3.3, 3.5, 3.8, 4.0, 4.1,
        };

        /// <summary>Monthly mean surface temperature (°C), Jan…Dec.</summary>
        public static readonly double[] MeanTempC =
        {
            16.0, 17.0, 19.5, 22.0, 25.0, 27.5, 28.5, 28.5, 27.5, 24.5, 20.5, 17.5,
        };

        /// <summary>Monthly probability a day has thunder (Bernoulli parameter).</summary>
        public static readonly double[] ThunderDayProbability =
        {
            0.05, 0.06, 0.10, 0.15, 0.25, 0.40, 0.50, 0.48, 0.35, 0.15, 0.08, 0.05,
        };

        public const double TempSigmaC = 2.5;

        /// <summary>Non-leap cumulative day-of-year at month end (index 0 unused / Jan end = 31).</summary>
        static readonly int[] MonthEndDoy = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };

        public static int MonthFromDayOfYear(int dayOfYear)
        {
            if (dayOfYear < 1) dayOfYear = 1;
            if (dayOfYear > 365) dayOfYear = 365;
            for (int m = 1; m <= 12; m++)
                if (dayOfYear <= MonthEndDoy[m]) return m;
            return 12;
        }
    }
}
