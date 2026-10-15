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
    }

    public static class AscentFlight
    {
        public static AscentHook Prepare(Catalog catalog, Design design, JsonObject json, bool engineer, bool reduceMotion)
        {
            ulong hash = FlightBatch.DesignHash(design);
            ulong seed = ReadSeed(json);
            bool gentle = FailureMonitor.GentleTier(design);
            bool auto = StepperProgress.Read(json).AutoAssistOn(engineer ? DesignerDepth.Engineer : DesignerDepth.Simple);
            double target = 200000;
            double qLim = 0;
            FailureMonitor monitor = null;
            MissionBriefing card = null;
            try
            {
                if (catalog != null && design != null)
                {
                    target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
                    qLim = FailureMonitor.StructuralQLimPa(catalog, design);
                    if (FailureMonitor.Engages(design))
                        monitor = FailureMonitor.Create(catalog, design, target, hash, seed);
                }
            }
            catch (Exception e) { Debug.LogWarning("[Ascent] design check skipped: " + e.Message); }
            try
            {
                string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
                int index = MissionIndex(design, json);
                card = index > 0
                    ? MissionBriefing.Load(root, index, hash, seed)
                    : MissionBriefing.Free(root, hash, seed);
            }
            catch (Exception e) { Debug.LogWarning("[Ascent] briefing skipped: " + e.Message); }
            var vehicle = AscentDecisions.FromDesign(design, catalog, monitor != null ? monitor.Events : null, card != null ? card.Wind : null, qLim, target, gentle, card != null ? card.Bonuses : null, hash, seed);
            if (card != null && !string.IsNullOrEmpty(card.FlightCode)) vehicle.FlightCode = card.FlightCode;
            var director = AscentDirector.Start(vehicle, monitor != null ? monitor.Events : null, card != null ? card.Wind : null, auto, engineer, reduceMotion);
            var hook = new AscentHook
            {
                Director = director,
                Monitor = monitor,
                Wind = card != null ? card.Wind : null,
            };
            if (card != null && card.Site != null)
            {
                hook.HasLatitude = true;
                hook.Latitude = card.Site.LatitudeDeg;
            }
            return hook;
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
