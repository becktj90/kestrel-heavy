// Notices when a welcome line (or any line) has been marked seen, so the host can save.
// The first sample is the baseline loaded from disk and does not count as a change.
namespace Kestrel.Game.Guide
{
    public sealed class GuideSeenWatch
    {
        int seen = -1;
        bool done;

        public bool Consume(int seenCount, bool firstRunDone)
        {
            if (seenCount < 0) seenCount = 0;
            bool changed = seen >= 0 && (seenCount != seen || firstRunDone != done);
            seen = seenCount;
            done = firstRunDone;
            return changed;
        }
    }
}
