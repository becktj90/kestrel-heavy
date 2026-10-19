// Sets StarterTable.Active for the Starter screen when nothing else has. A checkout (editor, CLI, tests)
// reads data/campaign.json and data/golden/campaign-winners.json. A player build has neither file in
// Resources yet (PR 4 packages data/starter-rockets.json), so it builds the placeholder from the synced
// campaign and the board starters; a mission with no base design falls back to the design on the pad.
using System;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Game.Shell;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class StarterBoot
    {
        static bool tried;

        public static StarterTable EnsureTable()
        {
            if (StarterTable.Active != null || tried) return StarterTable.Active;
            tried = true;
            try
            {
                string root = ScreenStub.RepoRoot();
                if (root != null && File.Exists(Path.Combine(root, "data", "golden", "campaign-winners.json"))
                    && File.Exists(Path.Combine(root, "data", "campaign.json")))
                {
                    StarterTable.Active = StarterTable.LoadPlaceholder(root);
                    return StarterTable.Active;
                }
                var campaign = ScreenStub.LoadCampaign();
                if (campaign == null) return null;
                var winners = Resources.Load<TextAsset>("KestrelData/campaign-winners");
                var byId = new System.Collections.Generic.Dictionary<string, JsonObject>();
                if (winners != null && !string.IsNullOrWhiteSpace(winners.text))
                {
                    foreach (var o in MiniJson.ParseObject(winners.text).Arr("missions"))
                        if (o is JsonObject row && row.Arr("designs") is System.Collections.Generic.List<object> ds && ds.Count > 0 && ds[0] is JsonObject d)
                            byId[row.Str("id")] = d;
                }
                StarterTable.Active = StarterTable.Placeholder(campaign, id => byId.TryGetValue(id, out var d) ? d : HomeAutoBuild.BoardStarter(id));
            }
            catch (Exception e) { Debug.LogWarning("[Starter] table: " + e.Message); }
            return StarterTable.Active;
        }

        /// <summary>EditMode seam: forget a failed attempt.</summary>
        public static void ResetForTests() => tried = false;
    }
}
