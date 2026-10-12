// U.S. Standard Atmosphere 1976, 0–86 km geometric (layers by geopotential height), isothermal
// exponential tail above 86 km. SI units. Port of web/js/physics/atmosphere.js.
// Above 86 km, callers use this same Atmosphere.At. UpperAtmosphere does not keep a second table.
using System;

namespace Kestrel.Core.Physics
{
    public struct AtmState { public double T, p, rho, a; }

    public static class Atmosphere
    {
        public const double R_AIR = 287.053, GAMMA_AIR = 1.4, G0 = 9.80665, R_EARTH_ISA = 6356766;
        public const double P0 = 101325, T0 = 288.15, TAIL_SCALE_HEIGHT = 5800;
        const double Z86 = 86000;

        // Layer bases are computed on the stack. Static arrays would keep Atmosphere.At out of Burst.
        public static double Geopotential(double z) => R_EARTH_ISA * z / (R_EARTH_ISA + z);

        public static AtmState At(double z)
        {
            Span<double> hb = stackalloc double[8] { 0, 11000, 20000, 32000, 47000, 51000, 71000, 84852 };
            Span<double> lb = stackalloc double[8] { -0.0065, 0, 0.001, 0.0028, 0, -0.0028, -0.002, 0 };
            Span<double> tb = stackalloc double[8];
            Span<double> pb = stackalloc double[8];
            tb[0] = T0; pb[0] = P0;
            for (int i = 1; i < 8; i++)
            {
                double dh = hb[i] - hb[i - 1], L = lb[i - 1];
                tb[i] = tb[i - 1] + L * dh;
                pb[i] = L == 0 ? pb[i - 1] * Math.Exp(-G0 * dh / (R_AIR * tb[i - 1]))
                               : pb[i - 1] * Math.Pow(tb[i] / tb[i - 1], -G0 / (R_AIR * L));
            }
            double T, p;
            if (z > Z86) { T = tb[7]; p = pb[7] * Math.Exp(-(z - Z86) / TAIL_SCALE_HEIGHT); }
            else
            {
                double h = Geopotential(z);
                int i = 0;
                while (i < 7 && h >= hb[i + 1]) i++;
                double L = lb[i], dh = h - hb[i];
                T = tb[i] + L * dh;
                p = L == 0 ? pb[i] * Math.Exp(-G0 * dh / (R_AIR * tb[i])) : pb[i] * Math.Pow(T / tb[i], -G0 / (R_AIR * L));
            }
            return new AtmState { T = T, p = p, rho = p / (R_AIR * T), a = Math.Sqrt(GAMMA_AIR * R_AIR * T) };
        }

        /// <summary>Density at many altitudes. Batches of 32 or more run in parallel; the same recurrence is what the Burst job calls.</summary>
        public static void SampleDensity(double[] altitudeM, double[] rho)
        {
            if (altitudeM == null) throw new System.ArgumentNullException(nameof(altitudeM));
            if (rho == null || rho.Length != altitudeM.Length) throw new System.ArgumentException("rho length");
            if (altitudeM.Length >= 32)
                System.Threading.Tasks.Parallel.For(0, altitudeM.Length, i => rho[i] = At(altitudeM[i]).rho);
            else
                for (int i = 0; i < altitudeM.Length; i++) rho[i] = At(altitudeM[i]).rho;
        }

        public static double PressureAt(double z) => At(z).p;
        public static double DensityAt(double z) => At(z).rho;
    }
}
