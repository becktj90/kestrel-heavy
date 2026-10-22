// Which painted Hangar parts the flight picture hides as the stack comes apart, and when the payload shows.
// Since #288 the visible rocket is HangarRocketView (one GameObject per part); the combined lathe mesh is
// undrawn, so masking it (FlightStageShow.ApplyMask) no longer hides anything on screen. This is the rule the
// flight picture applies to the painted parts instead, engine-free so it is tested without Unity:
//   - fairing separated      -> the painted fairing hides and the payload shows (its halves are FlightStageShow's);
//   - a stage body released  -> that painted stage hides, with the interstage riding on top of it
//                               (Interstage(i + 1) sits on stage i, the same rule FlightStageShow uses);
//   - the boosters released  -> the painted booster set hides.
// The moving pieces show on exactly the same Released()/FairingAt() test, so nothing is drawn twice and
// nothing is missing for a frame. A part never comes back until Reset (a new flight).
using System.Collections.Generic;
using Kestrel.Core.Hangar;

namespace Kestrel.Core.Flight
{
    public sealed class StagingPartVisibility
    {
        struct Body
        {
            public PartKind Kind;
            public int Index, Serial;
        }

        readonly List<Body> bodies = new List<Body>();
        readonly HashSet<PartRef> hidden = new HashSet<PartRef>();
        readonly int stageCount;
        readonly bool hasFairing;

        /// <param name="plan">FlightStagingPlan.Bodies for the design (serials of each stage and booster body).</param>
        /// <param name="stageCount">design.stages.Count.</param>
        /// <param name="hasFairing">The design draws a fairing (length &gt; 0.2 m and diameter &gt; 0.1 m).</param>
        public StagingPartVisibility(IList<StagingBody> plan, int stageCount, bool hasFairing)
        {
            this.stageCount = stageCount < 0 ? 0 : stageCount;
            this.hasFairing = hasFairing;
            if (plan == null) return;
            for (int i = 0; i < plan.Count; i++)
            {
                var b = plan[i];
                if (b.Role == "stage" && b.Index >= 0 && b.Index < this.stageCount)
                    bodies.Add(new Body { Kind = PartKind.Stage, Index = b.Index, Serial = b.Serial });
                else if (b.Role == "booster")
                    bodies.Add(new Body { Kind = PartKind.Boosters, Index = b.Index, Serial = b.Serial });
            }
        }

        /// <summary>The payload is drawn (the fairing has separated). False before, and for a rocket without a fairing
        /// it stays false: then the payload is not enclosed but the Hangar never draws it in flight either.</summary>
        public bool PayloadShown { get; private set; }

        public bool FairingSeparated { get; private set; }

        public IReadOnlyCollection<PartRef> Hidden => hidden;

        public bool IsHidden(PartRef part) => hidden.Contains(part);

        /// <summary>Back to the assembled stack: nothing hidden, payload enclosed.</summary>
        public void Reset()
        {
            hidden.Clear();
            PayloadShown = false;
            FairingSeparated = false;
        }

        /// <summary>Apply the staging state at <paramref name="t"/>. Returns true when the set of hidden parts or the
        /// payload changed this call, i.e. the first frame a separation shows.</summary>
        public bool Update(FlightStaging staging, double t)
        {
            if (staging == null) return false;
            return Update(serial => staging.Released(serial, t), staging.FairingAt(t).Separated);
        }

        /// <summary>Same rule from raw answers (tests, previews). Monotonic: a hidden part stays hidden until Reset.</summary>
        public bool Update(System.Func<int, bool> released, bool fairingSeparated)
        {
            bool changed = false;
            if (fairingSeparated && hasFairing && !FairingSeparated)
            {
                FairingSeparated = true;
                PayloadShown = true;
                changed |= hidden.Add(PartRef.Fairing);
            }
            if (released == null) return changed;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (!released(b.Serial)) continue;
                if (b.Kind == PartKind.Boosters)
                {
                    changed |= hidden.Add(PartRef.Boosters);
                    continue;
                }
                changed |= hidden.Add(PartRef.Stage(b.Index));
                if (b.Index + 1 < stageCount) changed |= hidden.Add(PartRef.Interstage(b.Index + 1));
            }
            return changed;
        }
    }
}
