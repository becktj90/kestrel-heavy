// Fixed particle slots (§3). Acquire never grows the pool; the oldest slot is reused.
namespace Kestrel.Game.Failure
{
    public sealed class FailurePool
    {
        public const int Capacity = 32;
        readonly int[] age;
        int clock, live;

        public FailurePool() { age = new int[Capacity]; }

        public int Live => live;
        public int CapacityCount => Capacity;

        public int Acquire()
        {
            clock++;
            if (live < Capacity)
            {
                age[live] = clock;
                return live++;
            }
            int oldest = 0;
            for (int i = 1; i < Capacity; i++)
                if (age[i] < age[oldest]) oldest = i;
            age[oldest] = clock;
            return oldest;
        }
    }
}
