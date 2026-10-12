// One deterministic launch-day weather sample (surface + upper shear). No Unity types.
namespace Kestrel.Core.Weather
{
    public struct WeatherDay
    {
        public string siteId;
        public int dayOfYear;
        public int month;
        public uint seed;
        public double windMs;
        public double gustMs;
        public double directionDeg;
        public double tempC;
        public double cloudFraction;
        public bool precip;
        public double ceilingM;
        public bool thunderDay;
        public double lightningKm;
        /// <summary>Characteristic upper-level speed shear (1/s) across the ascent band.</summary>
        public double shearPerSec;
    }
}
