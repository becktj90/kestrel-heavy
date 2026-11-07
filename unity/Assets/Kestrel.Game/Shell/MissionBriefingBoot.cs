// Player boot for the mission briefing books (campaign, sites, weather). A device build has no
// repo checkout, so MissionBriefing.Compose cannot read data/*.json from disk. CI (and Kestrel >
// Sync Data) copies those files into Assets/Resources/KestrelData; this installs them before the
// first scene loads so the live pad, designer predictor and briefing screen share seed-1 weather
// (DECISIONS #86). In the editor and Core tests nothing is installed and Compose reads the checkout.
using System;
using Kestrel.Core.Flight;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class MissionBriefingBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Boot()
        {
            if (BriefingBooks.HasBooks) return;
            string campaign = Text(BriefingBooks.CampaignResource);
            string sites = Text(BriefingBooks.SitesResource);
            string weather = Text(BriefingBooks.WeatherResource);
            if (campaign == null || sites == null || weather == null)
            {
                if (!Application.isEditor)
                    Debug.LogWarning("[Kestrel] Resources briefing books missing (campaign/sites/weather); device flights stay calm");
                return;
            }
            try { BriefingBooks.UseJson(campaign, sites, weather); }
            catch (Exception e) { Debug.LogWarning("[Kestrel] briefing books: " + e.Message); }
        }

        static string Text(string resource)
        {
            var asset = Resources.Load<TextAsset>(resource);
            return asset != null && !string.IsNullOrEmpty(asset.text) ? asset.text : null;
        }
    }
}
