using System.Collections.Generic;

namespace Kestrel.Core.Flight.Narration
{
    /// <summary>
    /// Picks the take to speak. Only eligible takes (see <see cref="NarrationRequires"/>) are candidates, ordered
    /// by most claims first and then by clip id (ordinal string compare), so the most specific true line is heard
    /// first, the generic ones are the rotation, and reordering the manifest never changes what plays. The caller
    /// keeps a cursor per event and lit stage and walks the ordered list.
    /// </summary>
    public static class NarrationPicker
    {
        /// <summary>Indexes into <paramref name="takes"/> that are true for the facts, best first. Empty when none.</summary>
        public static List<int> Order(IReadOnlyList<NarrationTake> takes, NarrationFacts facts)
        {
            var list = new List<int>();
            if (takes == null) return list;
            for (int i = 0; i < takes.Count; i++)
                if (NarrationRequires.Eligible(takes[i], facts)) list.Add(i);
            // Insertion sort: claims descending, then clip id ordinal ascending. Equal ids (never in a valid
            // manifest) keep manifest order so the result is still deterministic.
            for (int i = 1; i < list.Count; i++)
            {
                int cur = list[i];
                int j = i - 1;
                while (j >= 0 && Before(takes[cur], takes[list[j]])) { list[j + 1] = list[j]; j--; }
                list[j + 1] = cur;
            }
            return list;
        }

        /// <summary>True when <paramref name="a"/> must be heard before <paramref name="b"/>.</summary>
        static bool Before(NarrationTake a, NarrationTake b)
        {
            if (a.Claims != b.Claims) return a.Claims > b.Claims;
            return string.CompareOrdinal(a.Id ?? "", b.Id ?? "") < 0;
        }

        /// <summary>The take at <paramref name="cursor"/> (wrapping) in the ordered eligible list, or -1.</summary>
        public static int Pick(IReadOnlyList<NarrationTake> takes, NarrationFacts facts, int cursor)
        {
            var order = Order(takes, facts);
            if (order.Count == 0) return -1;
            if (cursor < 0) cursor = 0;
            return order[cursor % order.Count];
        }
    }
}
