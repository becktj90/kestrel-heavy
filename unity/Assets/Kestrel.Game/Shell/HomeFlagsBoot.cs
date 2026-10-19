// Loads data/features.json into FeatureFlags.Active once, before Home reads its home.* flags (feat(home-simple)).
// The checkout file in the editor, the Resources/KestrelData/features TextAsset in a player (SliceBootstrap.SyncData
// copies it on every build). No file: the FeatureFlags built-ins stay. #299 (feat(flags)) has no loader; if a later
// PR adds one at boot, this becomes a no-op once that sets HomeFlagsBoot.Loaded or FeatureFlags.Active first.
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
                flags.ApplyPrefs(PlayerPrefs.HasKey, k => PlayerPrefs.GetInt(k, 0));   // tier + Show full designer
                FeatureFlags.Active = flags;
            }
            catch (Exception e) { Debug.LogWarning("[Kestrel] features.json skipped: " + e.Message); }
        }
    }
}
