// Inputs to the timeline generator. The G2 pad generator is not in the core yet;
// Pad is the hardware the generator is allowed to mention (docs/MISSION_EVENTS.md §2).
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Timeline
{
    public sealed class Pad
    {
        public string id;
        public bool crewArms;
        public bool strongback;
        public bool umbilical;
        public bool deluge;
        public bool internalPower;

        /// <summary>
        /// Deterministic pad for a design until the seeded pad generator lands.
        /// A tower (and its strongback) only exists for a liquid stage at least 3 m across.
        /// A rail pad for a small solid has no umbilical and no deluge.
        /// </summary>
        public static Pad For(Catalog c, Design d)
        {
            bool liquid = false;
            double diam = d.stages != null && d.stages.Count > 0 ? d.stages[0].diameter : 0;
            void Consider(StagePart p)
            {
                if (p == null) return;
                if (c.PropellantById.TryGetValue(p.propellant ?? "", out var prop) && prop.storability != "solid") liquid = true;
            }
            if (d.stages != null) foreach (var s in d.stages) Consider(s);
            if (d.StrapOnCount > 0) Consider(d.strapOns);
            bool tower = diam >= 3;
            bool crewed = d.payloadType != null && c.PayloadById.TryGetValue(d.payloadType, out var pay) && pay.crewed;
            return new Pad
            {
                id = tower ? "pad.tower" : "pad.rail",
                crewArms = crewed || tower,
                strongback = liquid && tower,
                umbilical = liquid,
                deluge = liquid || tower,
                internalPower = true,
            };
        }
    }

    public sealed class Mission
    {
        public string id;
        /// <summary>kid, standard, or engineer. Controls T- compression and variance, not which events exist.</summary>
        public string mode;
        /// <summary>expendable, droneship, rtls, parachute, or null to infer from legs and landing propellant.</summary>
        public string recovery;
        public bool stageParachute;
        public bool fairingParachute;
        /// <summary>Launch latitude in degrees. Null uses Stack.LAUNCH_LATITUDE (the same constant the sim uses today).</summary>
        public double? latitude;
    }

    /// <summary>
    /// Reads the flight section of data/params.json without taking a compile dependency on the U1 registry type.
    /// When that registry is on main, pass its raw object here (key flight.launchLatitude).
    /// </summary>
    public static class TimelineParams
    {
        public const string LatitudeKey = "launchLatitude";

        public static double LaunchLatitude(JsonObject root, double fallback = Stack.LAUNCH_LATITUDE)
        {
            var flight = root?.Obj("flight");
            if (flight != null && flight.TryGetValue(LatitudeKey, out var v) && v is double d && !double.IsNaN(d) && !double.IsInfinity(d)) return d;
            return fallback;
        }
    }
}
