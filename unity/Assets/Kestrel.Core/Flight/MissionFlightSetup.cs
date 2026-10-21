// One mission flight setup for the live pad flight and the campaign batch (B-01, B-03).
// FlightDriver and FlightBatch both build their SimOptions and flown vehicle here, so the
// run length, cutoff and aim, coast boil-off, strap-on landing fuel, pad/wind and the fairing
// release event cannot drift.
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
        /// The specific orbital energy a mission's ascent cuts off at, J/kg (#313): −μ/(2a). A circular target uses
        /// a = R + (h_apo + h_peri)/2. An ellipse or C3 target (Physics A) climbs to the circular parking orbit at its aim
        /// perigee, a = R + aim; the departure burn is judged from there (<see cref="MissionScore.DepartureRequiredMps"/>).
        /// NaN for an apogee hop, which burns out.
        /// </summary>
        public static double TargetEnergy(CampaignMission mission)
        {
            if (mission == null) return double.NaN;
            switch (mission.TargetKind)
            {
                case "circular":
                    return SimOptions.EnergyFor(mission.ApoKm * 1000, mission.PeriKm * 1000);
                case "ellipse":
                case "c3":
                    return SimOptions.EnergyFor(mission.GuidanceAltM, mission.GuidanceAltM);
                default:
                    return double.NaN;
            }
        }

        static OrbitTarget FreeBuildOrbit(Catalog catalog, string orbitId)
        {
            if (catalog.Orbits == null) return null;
            if (orbitId != null && catalog.Orbits.TryGetValue(orbitId, out var o) && o != null) return o;
            return catalog.Orbits.TryGetValue("leo400", out var leo) ? leo : null;
        }

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
            // #313 (Kestrel Physics): every Kestrel flight cuts off at the target's specific orbital energy, located
            // inside the step, so the batch step and the live step cut off at the same state. No perigee test, no offset.
            opts.energyCutoff = true;
            opts.cutoff = true;
            opts.cutoffEnergy = double.NaN;
            opts.continuousTurn = true;
            double target = opts.targetAlt;
            if (mission != null)
            {
                double aim = mission.GuidanceAltM;
                if (aim > 0) target = aim;
                opts.targetAlt = target;
                if (opts.guidance != null) opts.guidance.targetAlt = target;
                opts.cutoffEnergy = TargetEnergy(mission);
                // #341: the guidance computer aims at the circular target or the parking orbit (GuidanceAltM): γ = 0 at that radius.
                if (opts.closedLoopInsertion) opts.closedLoopAimAlt = target;
            }
            else if (MissionAim.ApplyToDesignOrbit(opts, catalog, design.payloadOrbit))
            {
                // A free build with an apogee trip ("suborbital") is a hop: judge depletion at the hop floor, as a
                // campaign hop does, not at the apogee (F1). The designer's predict run and the live pad agree.
                target = opts.targetAlt;
            }
            else
            {
                // A free build aims at its own catalog orbit, circular or elliptical (Stack's leo400 default when unset).
                var orbit = FreeBuildOrbit(catalog, design.payloadOrbit);
                if (orbit != null && orbit.periKm >= 0)
                {
                    opts.cutoffEnergy = SimOptions.EnergyFor(orbit.apoKm * 1000, orbit.periKm * 1000);
                    // #341 (Physics ruling 2): an elliptical target is entered at its perigee radius with γ = 0.
                    if (opts.closedLoopInsertion) opts.closedLoopAimAlt = orbit.periKm * 1000;
                }
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
