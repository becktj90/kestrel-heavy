using System.Collections.Generic;

namespace Kestrel.Game.Audio
{
    /// <summary>Per-name retrigger lock. Time is supplied by the caller so tests stay deterministic.</summary>
    public sealed class SfxGate
    {
        readonly Dictionary<string, float> last = new Dictionary<string, float>();

        public bool Allow(string name, float lockSec, float now)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (last.TryGetValue(name, out var t) && now - t < lockSec) return false;
            last[name] = now;
            return true;
        }

        public void Reset() => last.Clear();
    }
}
