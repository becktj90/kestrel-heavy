using System.Collections.Generic;

namespace Kestrel.Core.Flight.Narration
{
    /// <summary>
    /// Picks the take to speak. Only eligible takes (see <see cref="NarrationRequires"/>) are candidates, ordered
    /// by most claims first and then by manifest order, so the most specific true line is heard first and the
    /// generic ones are the rotation. The caller keeps a cursor per event and lit stage and walks the ordered list.
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
            // Stable insertion sort: claims descending, manifest order kept for ties.
            for (int i = 1; i < list.Count; i++)
            {
                int cur = list[i];
                int j = i - 1;
                while (j >= 0 && takes[list[j]].Claims < takes[cur].Claims) { list[j + 1] = list[j]; j--; }
                list[j + 1] = cur;
            }
            return list;
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
