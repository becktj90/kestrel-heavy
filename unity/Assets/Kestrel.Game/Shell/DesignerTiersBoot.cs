// Loads data/designer-tiers.json (the tier switch, Kid Why? cards, speed bar and X-ray copy) into
// DesignerTierText.Active once: the checkout file in the editor, the Resources/KestrelData/designer-tiers TextAsset in a
// player (CI's "cp data/*.json" and SliceBootstrap.SyncData ship it). No file: Active stays null and the Starter screen
// shows no "?", speed bar or X-ray button rather than empty words.
using System;
using System.IO;
using Kestrel.Core.Designer;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class DesignerTiersBoot
    {
        /// <summary>Set once loaded. Tests that pick their own copy set it so the panel never replaces it.</summary>
        public static bool Loaded;

        public static DesignerTierText Load()
        {
            if (Loaded) return DesignerTierText.Active;
            Loaded = true;
            try
            {
                string json = null;
                string root = ScreenStub.RepoRoot();
                if (!string.IsNullOrEmpty(root))
                {
                    string path = Path.Combine(root, "data", DesignerTierText.FileName + ".json");
                    if (File.Exists(path)) json = File.ReadAllText(path);
                }
                if (json == null)
                {
                    var asset = Resources.Load<TextAsset>("KestrelData/" + DesignerTierText.FileName);
                    if (asset != null) json = asset.text;
                }
                if (json != null) DesignerTierText.Active = DesignerTierText.Parse(json);
            }
            catch (Exception e) { Debug.LogWarning("[Kestrel] designer-tiers.json skipped: " + e.Message); }
            return DesignerTierText.Active;
        }
    }
}
