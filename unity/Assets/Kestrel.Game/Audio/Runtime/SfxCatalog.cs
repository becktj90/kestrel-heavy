using System.Collections.Generic;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Event id → one-shot. Volumes and locks are the web KEYS table in web/js/audio.js.
    /// Clips are the NASA trims in web/audio (see ATTRIBUTION.md). No music, no theme.
    /// </summary>
    public readonly struct SfxSpec
    {
        public readonly string Name;
        public readonly string File;
        public readonly float Volume;
        public readonly float Lock;
        /// <summary>External air / aero. Silenced at and above <see cref="EngineMix.VacuumAltM"/>.</summary>
        public readonly bool Air;
        public readonly bool Critical;

        public SfxSpec(string name, string file, float volume, float lockSec, bool air, bool critical)
        {
            Name = name; File = file; Volume = volume; Lock = lockSec; Air = air; Critical = critical;
        }
    }

    public static class SfxCatalog
    {
        /// <summary>Resource file names under Resources/Sfx, without extension. Theme is not one of them.</summary>
        public static readonly string[] Files =
        {
            "roar-loop", "burn-loop", "liftoff", "maxq", "meco", "whoosh", "touchdown",
            "recovered", "splash", "quindar", "pickup", "hit", "rud",
        };

        static readonly Dictionary<string, SfxSpec> byEvent = new Dictionary<string, SfxSpec>();

        static SfxCatalog()
        {
            Add(new SfxSpec("liftoff", "liftoff", 0.72f, 3.2f, false, true), "liftoff");
            Add(new SfxSpec("maxq", "maxq", 0.58f, 1.6f, true, true), "maxq");
            Add(new SfxSpec("meco", "meco", 0.52f, 2.1f, false, true), "meco");
            Add(new SfxSpec("whoosh", "whoosh", 0.5f, 1.4f, true, true), "whoosh", "separation", "sep");
            Add(new SfxSpec("touchdown", "touchdown", 0.58f, 1.8f, false, true), "touchdown", "impact");
            Add(new SfxSpec("recovered", "recovered", 0.64f, 2.2f, false, true), "recovered");
            Add(new SfxSpec("splash", "splash", 0.56f, 1.6f, true, true), "splash");
            Add(new SfxSpec("ui", "quindar", 0.34f, 0.28f, false, false), "ui");
            Add(new SfxSpec("countdown", "quindar", 0.28f, 0.45f, false, false), "countdown");
            Add(new SfxSpec("pickup", "pickup", 0.46f, 0.35f, false, false), "pickup");
            Add(new SfxSpec("fuel", "pickup", 0.4f, 0.35f, false, false), "fuel");
            Add(new SfxSpec("shield", "pickup", 0.4f, 0.35f, false, false), "shield");
            Add(new SfxSpec("overdrive", "maxq", 0.34f, 1.2f, true, false), "overdrive");
            Add(new SfxSpec("hit", "hit", 0.5f, 0.4f, false, false), "hit");
            Add(new SfxSpec("rud", "rud", 0.62f, 1.8f, false, true), "rud");
            Add(new SfxSpec("success", "recovered", 0.56f, 2.2f, false, true), "success", "orbit");
            Add(new SfxSpec("ignite", "quindar", 0.42f, 0.55f, false, false), "ignite", "ignition");
        }

        public static bool TryResolve(string eventId, out SfxSpec spec)
        {
            spec = default;
            if (string.IsNullOrEmpty(eventId)) return false;
            return byEvent.TryGetValue(eventId.ToLowerInvariant(), out spec);
        }

        static void Add(SfxSpec spec, params string[] ids)
        {
            for (int i = 0; i < ids.Length; i++) byEvent[ids[i]] = spec;
        }
    }
}
