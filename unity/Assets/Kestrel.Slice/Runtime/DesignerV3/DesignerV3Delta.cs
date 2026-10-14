// Live delta chips on every control (§5 / §6): "+42 m/s Δv · −0.03 TWR" via Readout.Quick.
// Debounce window is 300 ms (spec); callers hold the timer; this type is the pure compare.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;

namespace Kestrel.Slice.DesignerV3
{
    public static class DesignerV3Delta
    {
        public const int DebounceMs = 300;

        /// <summary>Apply <paramref name="mutate"/> on a clone, then compare Quick readouts.</summary>
        public static string Chip(Catalog c, JsonObject design, Action<JsonObject> mutate)
        {
            if (c == null || design == null || mutate == null) return "";
            var before = Readout.Quick(c, design);
            var copy = JsonMut.CloneObject(design);
            mutate(copy);
            HangarEditor.Normalize(copy);
            var after = Readout.Quick(c, copy);
            return Readout.DeltaChip(after.totalDeltaV - before.totalDeltaV, after.liftoffTWR - before.liftoffTWR);
        }

        /// <summary>Chip for an already-computed before/after pair (tests and cached previews).</summary>
        public static string FromReadouts(QuickReadout before, QuickReadout after)
        {
            if (before == null || after == null) return "";
            return Readout.DeltaChip(after.totalDeltaV - before.totalDeltaV, after.liftoffTWR - before.liftoffTWR);
        }
    }
}
