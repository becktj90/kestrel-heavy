// Loads the shared catalogs (data/*.json + economy constants) into the engine-agnostic core.
// The JSON files are copied from ../data into Assets/Resources/KestrelData by the editor
// (Kestrel > Sync Data, and automatically before every build), so the repo-root JSON stays
// the single source of truth.
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class SliceData
    {
        static Catalog catalog;
        static HangarData hangar;
        static bool hangarTried;
        static DesignLibrary library;
        static System.Collections.Generic.Dictionary<string, int[]> feel;

        /// <summary>Haptic patterns from data/designer-feel.json ("snap", "payoff"). Empty when the file wasn't synced.</summary>
        public static System.Collections.Generic.Dictionary<string, int[]> Feel
        {
            get
            {
                if (feel != null) return feel;
                try { feel = HangarFeel.ParsePatterns(Optional("designer-feel")); }
                catch (System.FormatException e) { Debug.LogWarning("[Hangar] designer-feel.json unreadable: " + e.Message); feel = new System.Collections.Generic.Dictionary<string, int[]>(); }
                return feel;
            }
        }

        public static Catalog Catalog => catalog ??= Load();

        static string T(string n)
        {
            var a = Resources.Load<TextAsset>("KestrelData/" + n);
            if (a == null) throw new System.IO.FileNotFoundException($"Resources/KestrelData/{n}.json missing: run Kestrel > Sync Data");
            return a.text;
        }

        static string Optional(string n)
        {
            var a = Resources.Load<TextAsset>("KestrelData/" + n);
            return a != null ? a.text : null;
        }

        static Catalog Load()
        {
            // Designer ranges come from data/designer-limits.json when it is synced; otherwise the built-in fallback.
            var limits = Optional("designer-limits");
            if (limits != null)
            {
                try { DesignerLimits.Active = DesignerLimits.Parse(Kestrel.Core.Json.MiniJson.ParseObject(limits)); }
                catch (System.FormatException e) { Debug.LogWarning("[Hangar] designer-limits.json unreadable, using defaults: " + e.Message); }
            }
            var cat = Catalog.FromJson(T("propellants"), T("engines"), T("materials"), T("payloads"), T("presets"), T("constants"));
            var campaign = Optional("campaign");
            if (campaign != null)
            {
                try { HomeAutoBuild.InstallMissionOrbits(cat, Campaign.FromJson(MiniJson.ParseObject(campaign))); }
                catch (System.Exception e) { Debug.LogWarning("[Hangar] campaign orbits: " + e.Message); }
            }
            return cat;
        }

        /// <summary>Templates, Look catalog and budgets. Null (with a warning) when the files were not synced;
        /// the Hangar still edits and flies, it just has no templates, budget or Look choices.</summary>
        public static HangarData Hangar
        {
            get
            {
                if (hangarTried) return hangar;
                hangarTried = true;
                _ = Catalog;
                try
                {
                    hangar = HangarData.FromJson(Optional("designer-templates"), Optional("designer-look"), Optional("designer-budget"), Optional("finishes"), Optional("liveries"));
                }
                catch (System.Exception e) { Debug.LogWarning("[Hangar] catalogs unreadable: " + e.Message); hangar = null; }
                return hangar;
            }
        }

        /// <summary>Named designs on this device (8 slots), in persistentDataPath/saves. Null if the folder can't be made.</summary>
        public static DesignLibrary Library
        {
            get
            {
                if (library != null) return library;
                try
                {
                    var dir = System.IO.Path.Combine(Application.persistentDataPath, "saves");
                    var rng = new System.Random();
                    library = new DesignLibrary(new NativeStore(dir, () => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => rng.NextDouble()));
                }
                catch (System.Exception e) { Debug.LogWarning("[Hangar] save folder unavailable: " + e.Message); library = null; }
                return library;
            }
        }
    }
}
