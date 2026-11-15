namespace Kestrel.Core.Flight.Narration
{
    /// <summary>What a flight event says: the manifest event id, and the core stage it lit (0 for none).</summary>
    public readonly struct NarrationCue
    {
        public readonly string EventId;
        public readonly int LitStage;
        public NarrationCue(string eventId, int litStage) { EventId = eventId; LitStage = litStage; }
        public bool Silent => string.IsNullOrEmpty(EventId);
    }

    /// <summary>
    /// Sim event -> narration cue. The sim numbers ignitions by its stack (with strap-ons, entry 1 is the
    /// strap-on group and entry 2 is the core burning on after they drop), the manifest numbers core stages.
    /// Stage 1 is "ignition". Every later ignition of a core stage is "ses1" with that stage lit; the sustain
    /// burn of the first core after strap-on separation is not an ignition the crowd hears.
    /// </summary>
    public static class NarrationRoute
    {
        /// <param name="simType">AscentSim event type.</param>
        /// <param name="simStage">The event's stage (1-based sim stack entry), or 0 when unknown.</param>
        /// <param name="strapOns">True when the sim stack starts with the strap-on group.</param>
        /// <param name="stages">Core stage count of the design, 0 if unknown.</param>
        public static NarrationCue Route(string simType, int simStage, bool strapOns, int stages)
        {
            if (string.IsNullOrEmpty(simType)) return default;
            switch (simType)
            {
                case "ignition":
                {
                    if (simStage <= 1) return new NarrationCue("ignition", 1);
                    if (strapOns && simStage == 2) return default;
                    int lit = strapOns ? simStage - 1 : simStage;
                    if (stages > 0 && lit > stages) return default;
                    return new NarrationCue("ses1", lit);
                }
                case "meco": return new NarrationCue("meco", 1);
                case "separation":
                {
                    // B01: the sim emits "separation" only after a separation that did not fail (a recontact fails the
                    // flight first), so it is a clean sep. sep-clear is the first core stage dropping (its takes say
                    // booster and upper stage); a strap-on drop or a later core sep says nothing.
                    int core = strapOns ? simStage - 1 : simStage;
                    if (simStage < 1 || core != 1 || stages < 2) return default;
                    return new NarrationCue("sep-clear", 0);
                }
                case "engine-out":
                {
                    // A tolerated stage-1 engine-out. Without strap-ons sim entry 1 is core stage 1, whose engine count
                    // the lit.* facts give. With strap-ons the entry mixes the group and the core: no lit stage, so a
                    // take that claims lit.engines stays silent (fail closed).
                    int lit = !strapOns && simStage == 1 ? 1 : 0;
                    return new NarrationCue("engine-out", lit);
                }
                default: return new NarrationCue(simType, 0);
            }
        }

        /// <summary>
        /// The lit stage a cue gets when its sender gave none. Events that are about "the stage that is burning"
        /// default to the stage they belong to: stage 1 for the first-stage cues, the top stage for seco.
        /// </summary>
        public static int DefaultLit(string eventId, int stages)
        {
            switch (eventId)
            {
                case "ignition":
                case "tankpress":
                case "meco":
                case "fuel-low":
                case "abort-fuel":
                    return 1;
                case "seco": return stages > 0 ? stages : 0;
                case "ses1": return 2;
                default: return 0;
            }
        }
    }
}
