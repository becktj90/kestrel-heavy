using System.Collections.Generic;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Picks the next take for an event id. One line plays at a time; a later line waits.
    /// Abort lines replace whatever is playing. flight.begin clears the cursors.
    /// </summary>
    public sealed class NarrationSchedule
    {
        readonly Dictionary<string, int> cursors = new Dictionary<string, int>();

        public bool Busy { get; private set; }
        public string Current { get; private set; }
        public string Pending { get; private set; }

        /// <summary>Resource to start now, or null when the event was ignored or queued.</summary>
        public string Push(NarrationCatalog catalog, string id, bool muted)
        {
            if (catalog == null || string.IsNullOrEmpty(id) || muted) return null;
            if (!catalog.TryLine(id, Peek(id), out var line)) return null;
            Bump(id);
            if (Busy && !id.StartsWith("abort"))
            {
                Pending = line.Resource;
                return null;
            }
            Pending = null;
            Current = line.Resource;
            Busy = true;
            return Current;
        }

        /// <summary>The line that was waiting, or null. Clears the busy flag when nothing is queued.</summary>
        public string Complete()
        {
            Busy = false;
            Current = null;
            if (string.IsNullOrEmpty(Pending)) return null;
            Current = Pending;
            Pending = null;
            Busy = true;
            return Current;
        }

        public void Release()
        {
            Busy = false;
            Current = null;
            Pending = null;
        }

        public void Reset()
        {
            Release();
            cursors.Clear();
        }

        int Peek(string id) => cursors.TryGetValue(id, out var i) ? i : 0;

        void Bump(string id)
        {
            cursors.TryGetValue(id, out var i);
            cursors[id] = i + 1;
        }
    }
}
