// Above 86 km, density is Atmosphere.At. There is no second knot table here.
// A coarse log-linear fit of the 1976 table was up to ~25% off in density at 120–150 km.
// The USSA76 5 km tabulation replaces the isothermal tail inside Atmosphere.At
// (fix/atmo-above-86km). Rebase onto that once it lands. NRLMSIS is off limits
// (licence and patent). The later upper atmosphere is Jacchia-71-class from the
// NASA/SAO reports, not a copy of this file.
// Stars, when a caller needs them, are Kestrel.Core.Astro.StarCatalog over
// data/astro/bsc5p-compact.csv. This type does not keep a catalogue.
namespace Kestrel.Core.Physics
{
    public static class UpperAtmosphere
    {
        public const double JoinM = 86000;
        public const double KarmanM = 100000;

        /// <summary>Reference drag case. It uses Atmosphere.At, so it moves when that model does.</summary>
        public const double DragSpeed = 7500, DragCd0 = 0.3, DragArea = 10;

        public static AtmState At(double z) => Atmosphere.At(z);

        public static double Drag(double z, double speed, double cd0, double area)
        {
            var s = Atmosphere.At(z);
            return Aero.DragForce(s.rho, speed, s.a, cd0, area);
        }

        public static double Drag(double z) => Drag(z, DragSpeed, DragCd0, DragArea);
    }
}
