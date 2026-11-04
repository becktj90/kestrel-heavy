// Builds the active-ascent director for one launch and hands the sim the same monitor,
// wind, and pad the briefing already disclosed. Missing data falls back to a calm flight.
using System;
using System.Globalization;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Shell;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class AscentHook
    {
        public AscentDirector Director;
        public FailureMonitor Monitor;
        public SeededWind Wind;
        public bool HasLatitude;
        public double Latitude;
        /// <summary>The briefing's launch site id (sites.json), for narration lines that name the pad. Null when no briefing loaded.</summary>
        public string SiteId;
        /// <summary>Campaign hop this launch is flying. Null for a free build.</summary>
        public CampaignMission Mission;
    }

    public static class AscentFlight
    {
        public static AscentHook Prepare(Catalog catalog, Design design, JsonObject json, bool engineer, bool reduceMotion)
        {
            ulong hash = FlightBatch.DesignHash(design);
            ulong seed = ReadSeed(json);
            bool gentle = FailureMonitor.GentleTier(design);
            bool auto = StepperProgress.Read(json).AutoAssistOn(engineer ? DesignerDepth.Engineer : DesignerDepth.Simple);
            var mission = CurrentMission();
            double target = 200000;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = 0;
            FailureMonitor monitor = null;
            MissionBriefing card = null;
            try
            {
                if (catalog != null && design != null)
                {
                    target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
                    // The orbit aim is the goal altitude. An apogee hop depletes at the
                    // campaign floor, or the short load keeps burning and coasts past the band.
                    if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
                    qLim = FailureMonitor.StructuralQLimPa(catalog, design);
                    if (FailureMonitor.Engages(design))
                        monitor = FailureMonitor.Create(catalog, design, target, hash, seed);
                }
            }
            catch (Exception e) { Debug.LogWarning("[Ascent] design check skipped: " + e.Message); }
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            try
            {
                string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
                int index = MissionIndex(design, json);
                // The same call the designer's predictor makes (ReferenceAscent.Briefing, KH-7 E).
                card = MissionBriefing.ForPad(root, index, hash, seed);
            }
            catch (Exception e) { Debug.LogWarning("[Ascent] briefing skipped: " + e.Message); }
            // The one director factory the batch, the designer and the timeline use too (#313).
            var director = MissionFlightSetup.Director(catalog, design, monitor != null ? monitor.Events : null, card != null ? card.Wind : null,
                qLim, gentle, target, hash, seed, auto, engineer, reduceMotion, card != null ? card.Bonuses : null);
            if (card != null && !string.IsNullOrEmpty(card.FlightCode)) director.Vehicle.FlightCode = card.FlightCode;
            var hook = new AscentHook
            {
                Director = director,
                Monitor = monitor,
                Wind = card != null ? card.Wind : null,
                Mission = mission,
            };
            if (card != null && card.Site != null)
            {
                hook.HasLatitude = true;
                hook.Latitude = card.Site.LatitudeDeg;
                hook.SiteId = card.Site.Id;
            }
            return hook;
        }

        /// <summary>
        /// The mission on the pad. Resources campaign when the player build has no checkout.
        /// An apogee session with no row still carries the target so the hop can be graded.
        /// </summary>
        public static CampaignMission CurrentMission()
        {
            var ctx = GameSession.Current;
            if (ctx == null || ctx.IsFree || string.IsNullOrEmpty(ctx.MissionId)) return null;
            Campaign campaign = null;
            try { campaign = ScreenStub.LoadCampaign(); }
            catch (Exception) { campaign = null; }
            var row = campaign != null ? campaign.ById(ctx.MissionId) : null;
            if (row != null) return row;
            if (ctx.Target != null && ctx.Target.Kind == "apogee" && ctx.Target.ApoKm > 0)
                return new CampaignMission { Id = ctx.MissionId, TargetKind = "apogee", ApoKm = ctx.Target.ApoKm };
            return null;
        }

        public static ulong ReadSeed(JsonObject json)
        {
            if (json == null) return 1;
            string s = json.Str("seed");
            if (string.IsNullOrEmpty(s)) s = json.Obj("mission")?.Str("seed");
            if (string.IsNullOrEmpty(s)) s = json.Str("missionSeed");
            if (!string.IsNullOrEmpty(s))
            {
                bool hex = false;
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')) { hex = true; break; }
                }
                if (!hex && ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dec))
                    return dec == 0 ? 1 : dec;
                if (hex && ulong.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var h))
                    return h == 0 ? 1 : h;
            }
            if (json.TryGetValue("missionSeed", out var raw) && raw is double d && d > 0 && d == Math.Truncate(d) && d < ulong.MaxValue)
                return (ulong)d;
            return 1;
        }

        public static int MissionIndex(Design design, JsonObject json)
        {
            string id = design != null ? design.missionId : null;
            if (string.IsNullOrEmpty(id) && json != null) id = json.Str("missionId");
            if (string.IsNullOrEmpty(id) || id.Length < 2 || (id[0] != 'm' && id[0] != 'M')) return 0;
            return int.TryParse(id.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
        }
    }
}
