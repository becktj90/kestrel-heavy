// Loads data/features.json into FeatureFlags.Active once, before Home reads its home.* flags (feat(home-simple)).
// The checkout file in the editor, the synced Resources/KestrelData/features TextAsset in a player. No file: the
// FeatureFlags built-ins stay. feat(flags) PR 3 may own this boot later; it only has to set FeatureFlags.Active.
using System;
using System.IO;
using Kestrel.Core.Features;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class HomeFlagsBoot
    {
        /// <summary>Set once loaded. Tests that pick their own flags set it so Home never replaces them.</summary>
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
                    string path = Path.Combine(root, "data", "features.json");
                    if (File.Exists(path)) json = File.ReadAllText(path);
                }
                if (json == null)
                {
                    var asset = Resources.Load<TextAsset>("KestrelData/features");
                    if (asset != null) json = asset.text;
                }
                if (json == null) return;
                var flags = FeatureFlags.Parse(json);
                flags.ShowFullDesigner = PlayerPrefs.GetInt(FeatureKeys.ShowFullDesignerPref, 0) != 0;
                FeatureFlags.Active = flags;
            }
            catch (Exception e) { Debug.LogWarning("[Kestrel] features.json skipped: " + e.Message); }
        }
    }
}
