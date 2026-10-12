// Inputs to the timeline generator. The G2 pad generator is not in the core yet;
// Pad is the hardware the generator is allowed to mention (docs/MISSION_EVENTS.md §2).
using Kestrel.Core.Data;
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
        /// <summary>Launch latitude in degrees. Null uses Registry.LaunchLatitude when a registry is passed, otherwise Stack.LAUNCH_LATITUDE.</summary>
        public double? latitude;
    }
}
