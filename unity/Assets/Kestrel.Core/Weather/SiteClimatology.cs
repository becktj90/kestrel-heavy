// Monthly climatology targets for the fictional Haven launch site (Florida coastal stand-in).
// Calibrated to NOAA NCEI U.S. Climate Normals 1991–2020 at Melbourne Intl AP, FL (USW00012838).
// Temperature: MLY-TAVG-NORMAL (°F→°C). Wind: monthly mean of HLY-WIND-AVGSPD (mph→m/s).
// Thunder-day probabilities are fictional Florida-coastal seasonality (~80 days/yr), not a NOAA product.
// See LICENSES.md. Goldens / NOAA literals in WeatherTests.
namespace Kestrel.Core.Weather
{
    public static class SiteClimatology
    {
        public const string HavenId = "haven";

        /// <summary>NOAA station used to calibrate Haven (Melbourne Intl AP, FL).</summary>
        public const string NoaaStationId = "USW00012838";
        public const string NoaaStationName = "MELBOURNE INTL AP, FL US";
        public const string NoaaProduct = "U.S. Climate Normals 1991–2020";

        /// <summary>
        /// Monthly mean surface wind (m/s), Jan…Dec.
        /// From NOAA HLY-WIND-AVGSPD hourly normals at USW00012838, averaged per month, mph×0.44704.
        /// </summary>
        public static readonly double[] MeanWindMs =
        {
            3.91, 4.05, 4.35, 4.46, 4.31, 3.60, 3.24, 3.30, 3.71, 4.17, 4.13, 3.75,
        };

        /// <summary>
        /// Monthly mean surface temperature (°C), Jan…Dec.
        /// From NOAA MLY-TAVG-NORMAL at USW00012838 (°F→°C).
        /// </summary>
        public static readonly double[] MeanTempC =
        {
            16.50, 17.78, 19.67, 22.11, 24.89, 27.00, 27.61, 27.78, 27.00, 24.61, 20.56, 18.17,
        };

        /// <summary>
        /// Interannual σ of monthly mean temperature (°C) from NOAA MLY-TAVG-STDDEV (°F→°C).
        /// Used in climatology bands: 3σ_draw/√N + this spread.
        /// </summary>
        public static readonly double[] InterannualTempSigmaC =
        {
            1.611, 1.889, 1.722, 1.278, 0.778, 0.611, 0.611, 0.722, 0.556, 1.056, 1.500, 2.167,
        };

        /// <summary>
        /// Estimated interannual σ of monthly mean wind (m/s). Not published in the hourly-normals
        /// product for this station — fictional 10 % of the monthly mean (labelled in PHYSICS.md).
        /// </summary>
        public static readonly double[] InterannualWindSigmaMs =
        {
            0.391, 0.405, 0.435, 0.446, 0.431, 0.360, 0.324, 0.330, 0.371, 0.417, 0.413, 0.375,
        };

        /// <summary>
        /// Monthly probability a day has thunder (Bernoulli). Fictional Florida-coastal seasonality
        /// (~80 thunder-days/year); not a NOAA station product. Interannual spread on p: 0.05 abs.
        /// </summary>
        public static readonly double[] ThunderDayProbability =
        {
            0.05, 0.06, 0.10, 0.15, 0.25, 0.40, 0.50, 0.48, 0.35, 0.15, 0.08, 0.05,
        };

        /// <summary>Absolute interannual spread on thunder-day probability (fictional estimate).</summary>
        public const double InterannualThunderSpread = 0.05;

        /// <summary>Day-to-day temperature draw σ (°C) for Box-Muller (fictional; not NOAA).</summary>
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
