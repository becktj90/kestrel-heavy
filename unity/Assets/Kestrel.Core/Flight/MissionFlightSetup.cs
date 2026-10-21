// One mission flight setup for the live pad flight and the campaign batch (B-01, B-03).
// FlightDriver and FlightBatch both build their SimOptions and flown vehicle here, so the
// run length, cutoff and aim, coast boil-off, strap-on landing fuel and pad/wind cannot drift.
// Only the step size and the failure monitor stay with the caller.
// #313: every flight also flies the same AscentDirector (Director + Fly → DirectedFlight), so the
// batch, the designer practice flight, the launch timeline and the live pad share one throttle model.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer.Stepper;
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

        // Batch (BatchDt) vs live pad (LiveDt) agreement under the shared director (#313), set by Physics.
        /// <summary>Apogee hops: relative difference of the drag-aware flown peak.</summary>
        public const double HopPeakTolerance = 0.005;
        /// <summary>Orbits: relative difference of the apoapsis altitude.</summary>
        public const double OrbitApoTolerance = 0.005;
        /// <summary>Orbits: absolute difference of the periapsis altitude, metres.</summary>
        public const double OrbitPeriToleranceM = 1000;
        /// <summary>Orbits: relative difference of the semi-major axis.</summary>
        public const double OrbitSmaTolerance = 0.001;

        /// <summary>
        /// The ascent director a flight of <paramref name="design"/> flies (#313). The live pad builds it with the
        /// player's own settings (AscentFlight.Prepare). Prediction flies the default player: auto-assist on, which
        /// is the Simple-depth default (<see cref="StepperProgress.AutoAssistOn"/>), so every call is taken in its
        /// green window. <paramref name="schedule"/> is the failure monitor's seeded event schedule (null when the
        /// monitor does not engage). <paramref name="wind"/> null or calm is calm air.
        /// </summary>
        public static AscentDirector Director(Catalog catalog, Design design, EventSchedule schedule, SeededWind wind, double targetAltM,
            ulong designHash, ulong missionSeed, bool autoAssist = DefaultAutoAssist, bool engineer = false, bool reduceMotion = false,
            IList<BonusGoal> bonuses = null)
        {
            double qLim = 0;
            bool gentle = false;
            if (catalog != null && design != null)
            {
                qLim = FailureMonitor.StructuralQLimPa(catalog, design);
                gentle = FailureMonitor.GentleTier(design);
            }
            return Director(catalog, design, schedule, wind, qLim, gentle, targetAltM, designHash, missionSeed, autoAssist, engineer, reduceMotion, bonuses);
        }

        /// <summary>As above with the structural q limit and gentle tier already read (the live pad reads them under its own guard).</summary>
        public static AscentDirector Director(Catalog catalog, Design design, EventSchedule schedule, SeededWind wind, double qLimPa, bool gentle,
            double targetAltM, ulong designHash, ulong missionSeed, bool autoAssist, bool engineer, bool reduceMotion, IList<BonusGoal> bonuses)
        {
            var vehicle = AscentDecisions.FromDesign(design, catalog, schedule, wind, qLimPa, targetAltM, gentle, bonuses, designHash, missionSeed);
            return AscentDirector.Start(vehicle, schedule, wind, autoAssist, engineer, reduceMotion);
        }

        /// <summary>The prediction player: auto-assist on, the Simple-depth default.</summary>
        public const bool DefaultAutoAssist = true;

        /// <summary>The director for a built <paramref name="setup"/>: its monitor's schedule, its aim and its wind.</summary>
        public static AscentDirector Director(Catalog catalog, MissionFlightInputs setup, Design design, ulong designHash, ulong missionSeed)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            var opts = setup.Options;
            var schedule = opts != null && opts.monitor != null ? opts.monitor.Events : null;
            var wind = opts != null ? opts.wind : null;
            return Director(catalog, design, schedule, wind, setup.TargetAltM, designHash, missionSeed);
        }

        /// <summary>A directed flight of <paramref name="setup"/>, not yet stepped.</summary>
        public static DirectedFlight Start(MissionFlightInputs setup, AscentDirector director)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            return new DirectedFlight(new AscentSim(setup.Stages, setup.Options), director, setup.Options);
        }

        /// <summary>Fly <paramref name="setup"/> to the end under <paramref name="director"/>.</summary>
        public static SimResult Fly(MissionFlightInputs setup, AscentDirector director) => Start(setup, director).Run();

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
            // #313: cut off inside the step, so the batch step and the live step cut off at the same state.
            opts.locateCutoff = true;
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
            if (site != null)
            {
                if (site.Wind != null && !site.Wind.Calm) opts.wind = site.Wind;
                if (site.HasLatitude) opts.latitude = site.LatitudeDeg;
            }
            return new MissionFlightInputs { Stages = built.stages, Options = opts, Flown = flown, TargetAltM = target, BoilOffKg = boilOff };
        }
    }
}
