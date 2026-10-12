// Weather-hold policy. KID and Relaxed never hold. This is not a weather model.
namespace Kestrel.Core.Flight
{
    public static class Weather
    {
        public const double DefaultWindLimitMs = 12;

        public static (bool hold, bool allowed) Hold(string difficulty, bool relaxed, double windMs, double limitMs = DefaultWindLimitMs)
        {
            if (difficulty == "KID" || relaxed) return (false, false);
            return (windMs > limitMs, true);
        }
    }
}
