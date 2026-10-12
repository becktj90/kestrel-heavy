// Coast mode selection and state continuity between ascent RK4, numeric coast, and on-rails.
// On-rails when periapsis altitude ≥ OnRailsPerigeeKm (600 km). Drag/decay stay on the numeric path (later P5).
using System;

namespace Kestrel.Core.Physics
{
    public enum CoastMode
    {
        /// <summary>Ascent / powered flight (RK4 with thrust). Not a coast propagator.</summary>
        Ascent = 0,
        /// <summary>Coasting with point-mass (and later drag) RK4 when perigee is below the on-rails floor.</summary>
        Numeric = 1,
        /// <summary>Analytic Kepler + J2 secular. Used for time warp when perigee ≥ 600 km.</summary>
        OnRails = 2,
    }

    public static class Handoff
    {
        /// <summary>
        /// Periapsis altitude (geometric, m above R_EARTH) at or above which coast uses on-rails.
        /// The gate is periapsis, not the current geometric altitude — a 300×800 km orbit stays Numeric
        /// even while at apoapsis above 600 km, so drag-capable coast remains available.
        /// </summary>
        public const double OnRailsPerigeeKm = 600.0;
        public const double OnRailsPerigeeM = OnRailsPerigeeKm * 1000.0;

        public static double PeriapsisAltitudeM(ClassicalElements el, double radius = Gravity.R_EARTH)
            => el.a * (1.0 - el.e) - radius;

        public static double PeriapsisAltitudeM(double rx, double ry, double rz, double vx, double vy, double vz,
            double mu = Gravity.MU_EARTH, double radius = Gravity.R_EARTH)
        {
            var el = Kepler.FromState(rx, ry, rz, vx, vy, vz, mu);
            return PeriapsisAltitudeM(el, radius);
        }

        /// <summary>Choose Numeric vs OnRails from periapsis altitude. Powered flight stays Ascent.</summary>
        public static CoastMode SelectCoast(ClassicalElements el)
            => PeriapsisAltitudeM(el) >= OnRailsPerigeeM ? CoastMode.OnRails : CoastMode.Numeric;

        public static CoastMode SelectCoast(double rx, double ry, double rz, double vx, double vy, double vz,
            double mu = Gravity.MU_EARTH)
            => SelectCoast(Kepler.FromState(rx, ry, rz, vx, vy, vz, mu));

        /// <summary>
        /// Capture an ECI state into classical elements for on-rails coast.
        /// Release is <see cref="Kepler.ToState"/>; round-trip position must stay within 1 mm.
        /// </summary>
        public static ClassicalElements Capture(double rx, double ry, double rz, double vx, double vy, double vz,
            double mu = Gravity.MU_EARTH)
            => Kepler.FromState(rx, ry, rz, vx, vy, vz, mu);

        public static void Release(ClassicalElements el,
            out double rx, out double ry, out double rz, out double vx, out double vy, out double vz,
            double mu = Gravity.MU_EARTH)
            => Kepler.ToState(el, out rx, out ry, out rz, out vx, out vy, out vz, mu);

        /// <summary>
        /// Propagate coast by Δt using the mode implied by periapsis (or an explicit override).
        /// OnRails → J2 secular; Numeric → two-body analytic Kepler (drag arrives with the decay PR).
        /// </summary>
        public static void PropagateCoast(double rx0, double ry0, double rz0, double vx0, double vy0, double vz0,
            double dt, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz,
            CoastMode? mode = null, double mu = Gravity.MU_EARTH)
        {
            var el = Kepler.FromState(rx0, ry0, rz0, vx0, vy0, vz0, mu);
            CoastMode m = mode ?? SelectCoast(el);
            ClassicalElements next = m == CoastMode.OnRails
                ? OnRails.Propagate(el, dt, mu)
                : Kepler.Propagate(el, dt, mu);
            Kepler.ToState(next, out rx, out ry, out rz, out vx, out vy, out vz, mu);
        }
    }
}
