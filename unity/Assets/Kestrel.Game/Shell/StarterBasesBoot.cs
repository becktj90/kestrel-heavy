// Loads each mission's base rocket for the Starter designer (feat(designer-starter)) into StarterBases.Active
// once, next to HomeFlagsBoot. The checkout's data/golden/campaign-winners.json in the editor; in a player the
// Resources/KestrelData/campaign-winners TextAsset (data/campaign-winners.json, a byte copy of the golden that
// CI's "cp data/*.json" and SliceBootstrap.SyncData both ship). No file: the Starter screen uses the design on
// the pad as its base.
using System;
using System.IO;
using Kestrel.Core.Designer;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class StarterBasesBoot
    {
        /// <summary>Set once loaded. Tests that pick their own bases set it so boot never replaces them.</summary>
        public static bool Loaded;

        public static void Load()
        {
            if (Loaded) return;
            Loaded = true;
            try
            {
                string json = null;
                string root = ScreenStub.RepoRoot();
                if (!string.IsNullOrEmpty(root))
                {
                    string path = Path.Combine(root, "data", "golden", "campaign-winners.json");
                    if (File.Exists(path)) json = File.ReadAllText(path);
                }
                if (json == null)
                {
                    var asset = Resources.Load<TextAsset>("KestrelData/campaign-winners");
                    if (asset != null) json = asset.text;
                }
                if (json == null) return;
                StarterBases.Active = StarterBases.Parse(json);
            }
            catch (Exception e) { Debug.LogWarning("[Kestrel] campaign-winners.json skipped: " + e.Message); }
        }
    }
}
