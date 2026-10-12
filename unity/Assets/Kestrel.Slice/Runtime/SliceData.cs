// Loads the shared catalogs (data/*.json + economy constants) into the engine-agnostic core.
// The JSON files are copied from ../data into Assets/Resources/KestrelData by the editor
// (Kestrel > Sync Data, and automatically before every build), so the repo-root JSON stays
// the single source of truth.
using Kestrel.Core.Data;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class SliceData
    {
        static Catalog catalog;
        public static Catalog Catalog => catalog ??= Load();

        static Catalog Load()
        {
            string T(string n)
            {
                var a = Resources.Load<TextAsset>("KestrelData/" + n);
                if (a == null) throw new System.IO.FileNotFoundException($"Resources/KestrelData/{n}.json missing: run Kestrel > Sync Data");
                return a.text;
            }
            return Catalog.FromJson(T("propellants"), T("engines"), T("materials"), T("payloads"), T("presets"), T("constants"));
        }
    }
}
