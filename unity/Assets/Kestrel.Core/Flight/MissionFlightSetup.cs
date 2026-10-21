// One mission flight setup for the live pad flight and the campaign batch (B-01, B-03).
// FlightDriver and FlightBatch both build their SimOptions and flown vehicle here, so the
// run length, cutoff and aim, coast boil-off, strap-on landing fuel, pad/wind and the fairing
// release event cannot drift.
// Only the step size and the failure monitor stay with the caller.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    /// <summary>Launch site and air for a flight. Null on <see cref="MissionFlightSetup.Build"/> is the reference pad (28.5°, calm air).</summary>
    public sealed class MissionFlightSite
    {
        public bool HasLatitude;
        public double LatitudeDeg;
        /// <summary>Null or calm leaves the flight in calm air.</summary>
        public SeededWind Wind;
    }

    public sealed class MissionFlightInputs
    {
        public List<SimStage> Stages;
        public SimOptions Options;
        /// <summary>The design as flown: coast boil-off removed and strap-on landing fuel held out.</summary>
        public Design Flown;
        /// <summary>Guidance aim point in metres.</summary>
        public double TargetAltM;
        public double BoilOffKg;
    }

    public static class MissionFlightSetup
    {
        /// <summary>Shortest run any flight gets. Orbital winners reach orbit at T+525..900 s.</summary>
        public const double MinTMax = 2500;
        /// <summary>The live pad step (FlightDriver.PhysicsDt).</summary>
        public const double LiveDt = 1.0 / 120;
        /// <summary>The campaign batch step.</summary>
        public const double BatchDt = 0.2;

        /// <summary>
        /// Sim inputs for <paramref name="design"/> flying <paramref name="mission"/>.
        /// A null mission is a free build: the design's own orbit, nothing held out. An apogee orbit aims at the
        /// hop floor (<see cref="MissionAim.ApplyToDesignOrbit"/>).
        /// </summary>
        public static MissionFlightInputs Build(Catalog catalog, CampaignMission mission, Design design, double dt, MissionFlightSite site = null, double sampleEvery = 0)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (design == null) throw new ArgumentNullException(nameof(design));
            var flown = design.Clone();
            double boilOff = 0;
            if (mission != null && mission.CoastHours > 0 && flown.stages != null && flown.stages.Count > 0)
            {
                var upper = flown.stages[flown.stages.Count - 1];
                double before = upper.propMass;
                upper.propMass = TankModel.RemainingPropKg(catalog, upper, mission.CoastHours);
                boilOff = before - upper.propMass;
            }
            // Core stages already hold landing prop out of the burn. Strap-ons do not, so keep it here.
            if (mission != null && mission.LandBoosters && flown.strapOns != null && flown.strapOns.landingPropMass > 0)
            {
                double keep = Math.Min(flown.strapOns.landingPropMass, flown.strapOns.propMass);
                flown.strapOns.propMass -= keep;
            }

            var built = Stack.SimInputs(catalog, flown, dt, sampleEvery);
            var opts = built.opts;
            if (!(opts.tMax >= MinTMax)) opts.tMax = MinTMax;
            double target = opts.targetAlt;
            if (mission != null)
            {
                double aim = mission.GuidanceAltM;
                if (aim > 0) target = aim;
                opts.targetAlt = target;
                if (opts.guidance != null) opts.guidance.targetAlt = target;
                // The integrator cuts off when periapsis is merely near the target, which freezes lofted ellipses.
                // Circular, GTO and C3 contracts burn the loaded upper propellant out. The depletion check uses a
                // lower floor so a burnout just under the aim point is not a false "out of fuel". The pass check
                // still requires the flown apoapsis and periapsis. Upper-stage pitch reads the guidance target,
                // which stays on the real aim point when a profile is set.
                if (mission.TargetKind == "circular" || mission.TargetKind == "ellipse" || mission.TargetKind == "c3")
                {
                    // Legacy pitch reads SimOptions.targetAlt, so it keeps burning the loaded
                    // upper propellant (cutoff off) toward the real aim. A custom pitch keeps
                    // the aim on the guidance profile. Headless then cuts off when periapsis is
                    // within 10 km of min(targetAlt, altitude). A huge SimOptions target makes
                    // that "periapsis has caught the vehicle": a near-circular insertion, not a
                    // frozen lofted ellipse. Running out of propellant before then is depletion.
                    bool custom = opts.guidance != null && !Headless.LegacyPitch(opts.guidance);
                    if (custom)
                    {
                        opts.cutoff = true;
                        opts.targetAlt = 1e12;
                        opts.guidance.targetAlt = target;
                    }
                    else
                    {
                        opts.cutoff = false;
                        opts.targetAlt = target;
                    }
                }
            }
            else if (MissionAim.ApplyToDesignOrbit(opts, catalog, design.payloadOrbit))
            {
                // A free build with an apogee trip ("suborbital") is a hop: judge depletion at the hop floor, as a
                // campaign hop does, not at the apogee (F1). The designer's predict run and the live pad agree.
                target = opts.targetAlt;
            }
            opts.fairingJettisonAltM = FairingJettisonAltM(design);
            if (site != null)
            {
                if (site.Wind != null && !site.Wind.Calm) opts.wind = site.Wind;
                if (site.HasLatitude) opts.latitude = site.LatitudeDeg;
            }
            return new MissionFlightInputs { Stages = built.stages, Options = opts, Flown = flown, TargetAltM = target, BoilOffKg = boilOff };
        }

        /// <summary>Default release height when a design keeps none (Design.cs fairing.jettisonAltitudeKm default).</summary>
        public const double DefaultFairingJettisonAltM = 110000;

        /// <summary>
        /// The flown sim's fairing release height: the design's fairing.jettisonAltitudeKm, the same number the
        /// staging show (FlightStageShow), the director (AscentVehicle.JettisonAltM) and the timeline use. 0 for a
        /// design with no fairing, using the staging show's rule, so a bare stack never emits a "fairing" event.
        /// The event is a marker only: the fairing's mass rides with the payload for the whole burn (Stack), as
        /// in every sim test that sets this option.
        /// </summary>
        public static double FairingJettisonAltM(Design design)
        {
            if (design == null || !(design.fairingLength > 0.2 && design.fairingDiameter > 0.1)) return 0;
            return design.fairingJettisonAltitudeKm > 0 ? design.fairingJettisonAltitudeKm * 1000 : DefaultFairingJettisonAltM;
        }
    }
}
