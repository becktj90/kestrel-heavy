// U.S. Standard Atmosphere 1976, 0–86 km geometric (layers by geopotential height), isothermal
// exponential tail above 86 km. SI units. Port of web/js/physics/atmosphere.js.
using System;

namespace Kestrel.Core.Physics
{
    public struct AtmState { public double T, p, rho, a; }

    public static class Atmosphere
    {
        public const double R_AIR = 287.053, GAMMA_AIR = 1.4, G0 = 9.80665, R_EARTH_ISA = 6356766;
        public const double P0 = 101325, T0 = 288.15, TAIL_SCALE_HEIGHT = 5800;
        static readonly double[] HB = { 0, 11000, 20000, 32000, 47000, 51000, 71000, 84852 };
        static readonly double[] LB = { -0.0065, 0, 0.001, 0.0028, 0, -0.0028, -0.002, 0 };
        static readonly double[] TB = new double[8], PB = new double[8];
        const double Z86 = 86000;

        static Atmosphere()
        {
            TB[0] = T0; PB[0] = P0;
            for (int i = 1; i < 8; i++)
            {
                double dh = HB[i] - HB[i - 1], L = LB[i - 1];
                TB[i] = TB[i - 1] + L * dh;
                PB[i] = L == 0 ? PB[i - 1] * Math.Exp(-G0 * dh / (R_AIR * TB[i - 1]))
                               : PB[i - 1] * Math.Pow(TB[i] / TB[i - 1], -G0 / (R_AIR * L));
            }
        }

        public static double Geopotential(double z) => R_EARTH_ISA * z / (R_EARTH_ISA + z);

        public static AtmState At(double z)
        {
            double T, p;
            if (z > Z86) { T = TB[7]; p = PB[7] * Math.Exp(-(z - Z86) / TAIL_SCALE_HEIGHT); }
            else
            {
                double h = Geopotential(z);
                int i = 0;
                while (i < 7 && h >= HB[i + 1]) i++;
                double L = LB[i], dh = h - HB[i];
                T = TB[i] + L * dh;
                p = L == 0 ? PB[i] * Math.Exp(-G0 * dh / (R_AIR * TB[i])) : PB[i] * Math.Pow(T / TB[i], -G0 / (R_AIR * L));
            }
            return new AtmState { T = T, p = p, rho = p / (R_AIR * T), a = Math.Sqrt(GAMMA_AIR * R_AIR * T) };
        }

        public static double PressureAt(double z) => At(z).p;
        public static double DensityAt(double z) => At(z).rho;
    }
}
