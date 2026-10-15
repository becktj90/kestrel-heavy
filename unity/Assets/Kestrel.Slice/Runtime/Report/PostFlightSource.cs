// Arms the flying design, then writes the post-flight card into the same career save the mission map reads.
// A captured failure with no finished flight still explains itself and does not touch the bank.
using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Shell;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class PostFlightSource
    {
        public static Action Relaunch { get; private set; }
        public static Action LeaveHome { get; private set; }

        static Catalog catalog;
        static Design design;
        static bool engineer;

        public static void Bind(Action relaunch, Action home)
        {
            Relaunch = relaunch;
            LeaveHome = home;
        }

        public static void Arm(Catalog cat, Design flying, JsonObject doc, bool engineerDepth)
        {
            catalog = cat;
            design = flying;
            engineer = engineerDepth;
            if (design == null && doc != null)
            {
                try { design = Design.FromJson(doc); }
                catch (Exception) { design = null; }
            }
        }

        public static FlightDebrief Publish(FlightDriver flight, FailureEvent failure)
        {
            var facts = Facts(flight, failure);
            var pool = LoadPool();
            long minor = MinorPerKc();
            var look = Look();
            bool live = flight != null && flight.Result != null;
            if (!live)
                return FlightDebrief.Compose(null, GameSession.Current, facts, pool, minor, look);
            var store = TryStore(minor);
            return FlightDebrief.Commit(store, GameSession.Current, facts, pool, minor, look);
        }

        public static ReportActions Actions() => NextMission.Actions(Relaunch, LeaveHome);

        static FlightFacts Facts(FlightDriver flight, FailureEvent failure)
        {
            var result = flight != null ? flight.Result : null;
            var ev = failure ?? (result != null ? result.failure : null);
            var mission = GameSession.Current;
            var facts = new FlightFacts
            {
                Failure = ev,
                Seed = mission != null ? mission.Seed : 0,
                DesignId = design != null ? design.name : null,
                RedundantAvionics = design != null && EventModel.Tier(design.avionicsTier) == "redundant",
                EngineCount = Engines(design),
                BudgetKc = mission != null ? mission.BudgetKc : 0,
                GoalKm = mission != null && mission.Target != null ? mission.Target.ApoKm : 0,
            };
            if (result != null)
            {
                facts.PeakG = result.maxG;
                facts.ApoKm = result.orbit.apoAlt / 1000.0;
                double peri = result.orbit.periAlt;
                CampaignMission row = Row(mission);
                if (row != null)
                {
                    facts.GoalKm = row.ApoKm;
                    facts.LandingRequired = row.LandingRequired;
                    facts.GLimit = double.IsNaN(row.MaxG) ? 0 : row.MaxG;
                    facts.RequiredMps = row.RequiredDeltaV();
                    facts.Margin = facts.RequiredMps > 1 ? (result.idealDeltaV - facts.RequiredMps) / facts.RequiredMps : 0;
                    bool intact = ev == null && result.outcome != "failure" && result.outcome != "crash";
                    facts.Passed = FlightDebrief.GoalMet(row, intact, result.orbit.apoAlt, peri, result.maxG, null);
                }
            }
            if (flight != null && flight.Decisions != null)
            {
                var score = flight.Decisions.Score();
                if (score != null) facts.TimingMedal = score.Medal;
            }
            Quote(facts);
            return facts;
        }

        static void Quote(FlightFacts facts)
        {
            if (catalog == null || design == null) return;
            try
            {
                var price = CostModel.PriceDesign(catalog, design);
                long per = CostModel.MinorPerKc(catalog);
                if (per < 1) per = 100000;
                facts.CostMinor = price.recurring;
                facts.CostKc = (double)price.recurring / per;
                bool home = MissionScore.IsGood(facts.LandingGrade);
                facts.RecoveredHardwareMinor = home ? CostModel.RecoveredHardwareMinor(price, true, false) : 0;
                facts.EconomyKnown = true;
                if (string.IsNullOrEmpty(facts.ArticleKey)) facts.ArticleKey = "core";
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] build cost unavailable: " + e.Message);
            }
        }

        static int Engines(Design d)
        {
            if (d == null || d.stages == null) return 0;
            int n = 0;
            for (int i = 0; i < d.stages.Count; i++)
            {
                var stage = d.stages[i];
                if (stage == null) continue;
                int copies = stage.count > 1 ? stage.count : 1;
                n += (stage.engineCount > 0 ? stage.engineCount : 1) * copies;
            }
            return n;
        }

        static CampaignMission Row(MissionContext mission)
        {
            if (mission == null || mission.IsFree || string.IsNullOrEmpty(mission.MissionId)) return null;
            var campaign = ScreenStub.LoadCampaign();
            return campaign != null ? campaign.ById(mission.MissionId) : null;
        }

        static long MinorPerKc()
        {
            if (catalog != null)
            {
                try
                {
                    long per = CostModel.MinorPerKc(catalog);
                    if (per >= 1) return per;
                }
                catch (Exception) { }
            }
            return 100000;
        }

        static NarrationPool LoadPool()
        {
            try
            {
                string root = ScreenStub.RepoRoot();
                if (!string.IsNullOrEmpty(root) && File.Exists(Path.Combine(root, "data", "narration.json")))
                    return NarrationPool.Load(root);
            }
            catch (Exception) { }
            var asset = Resources.Load<TextAsset>("KestrelData/narration");
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
            {
                try { return NarrationPool.Parse(asset.text); }
                catch (Exception) { }
            }
            return NarrationPool.Empty;
        }

        static HangarData Look()
        {
            try
            {
                string root = ScreenStub.RepoRoot();
                if (!string.IsNullOrEmpty(root)) return HangarData.LoadFromRepo(root);
            }
            catch (Exception) { }
            return null;
        }

        static CareerStore TryStore(long minor)
        {
            try
            {
                var campaign = ScreenStub.LoadCampaign();
                if (campaign == null) return null;
                string dir = Path.Combine(Application.persistentDataPath, "saves");
                Directory.CreateDirectory(dir);
                var native = new NativeStore(dir, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => 0.5);
                return CareerStore.Load(native, campaign, minor);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] career save unavailable: " + e.Message);
                return null;
            }
        }
    }
}
