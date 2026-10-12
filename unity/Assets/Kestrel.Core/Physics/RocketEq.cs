// Rocket equation helpers. SI; Isp in seconds. Port of web/js/physics/rocket-eq.js.
using System;

namespace Kestrel.Core.Physics
{
    public static class RocketEq
    {
        public const double G0 = Atmosphere.G0;
        public static double DeltaV(double isp, double m0, double mf) => isp * G0 * Math.Log(m0 / mf);
        public static double MassFlow(double thrust, double isp) => thrust / (isp * G0);
        public static double IspAt(double ispSL, double ispVac, double p) => Math.Max(0, ispVac - (ispVac - ispSL) * p / Atmosphere.P0);
        public static double ThrustAt(double thrustVac, double ispSL, double ispVac, double p) => thrustVac * IspAt(ispSL, ispVac, p) / ispVac;
        public static double BurnTime(double propMass, double thrustVac, double ispVac) => propMass / MassFlow(thrustVac, ispVac);
    }
}
