// The pad sim and the campaign batch have to aim at the same altitude.
// Stack.SimInputs aims at the design's orbit. An apogee hop is not that orbit:
// a 10 km hop guided at 200 km burns the short load into a coast past the band.
using Kestrel.Core.Data;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class MissionAim
    {
        /// <summary>An apogee hop coasts after burnout, so prop depletion is judged against this 1 km floor.</summary>
        public const double HopDepletionFloorM = 1000;

        /// <summary>True when the catalog orbit is an apogee target (Stack's negative-periapsis flag): KH-1, KH-2, "suborbital".</summary>
        public static bool IsSuborbital(Catalog catalog, string orbitId)
            => catalog != null && orbitId != null && catalog.Orbits.TryGetValue(orbitId, out var o) && o != null && o.periKm < 0;

        /// <summary>
        /// The designer's reference ascent aims where the pad flight aims (F1). Stack.SimInputs aims an apogee
        /// trip at the apogee itself, so the predict run judged a 100 km hop "out of fuel" at burnout while the
        /// rocket was still climbing. The live and batch flights use the 1 km floor (<see cref="Apply"/>).
        /// </summary>
        public static bool ApplyToDesignOrbit(SimOptions opts, Catalog catalog, string orbitId)
        {
            if (opts == null || !IsSuborbital(catalog, orbitId)) return false;
            opts.targetAlt = HopDepletionFloorM;
            if (opts.guidance != null) opts.guidance.targetAlt = HopDepletionFloorM;
            return true;
        }

        /// <summary>The trip copy is a hop for KH-1, any apogee target, or any target at or under 80 km.</summary>
        public static bool IsHopTrip(string missionId, double targetAltM, bool suborbital)
        {
            if (suborbital || missionId == "m1") return true;
            return targetAltM > 0 && targetAltM <= 80000;
        }

        public static bool IsApogeeHop(CampaignMission mission)
            => mission != null && mission.TargetKind == "apogee" && mission.GuidanceAltM > 0;

        /// <summary>
        /// Point the sim and its pitch profile at the hop's depletion floor.
        /// Orbit contracts are left on the path <see cref="FlightBatch"/> already flies.
        /// </summary>
        public static bool Apply(SimOptions opts, CampaignMission mission)
        {
            if (opts == null || !IsApogeeHop(mission)) return false;
            double target = mission.GuidanceAltM;
            opts.targetAlt = target;
            if (opts.guidance != null) opts.guidance.targetAlt = target;
            return true;
        }
    }
}
