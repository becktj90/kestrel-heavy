// The pad sim and the campaign batch have to aim at the same altitude.
// Stack.SimInputs aims at the design's orbit. An apogee hop is not that orbit:
// a 10 km hop guided at 200 km burns the short load into a coast past the band.
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class MissionAim
    {
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
