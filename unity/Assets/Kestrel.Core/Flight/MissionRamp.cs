// Per-mission wind, landing fuel, and swell. Numbers are data/mission-ramp.json.
// A deck finish unlocks the next mission. Stars rate the catch and never gate it.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public sealed class MissionRamp
    {
        public const double DriftWindFloor = 0.28;
        public const double SwellOmega = 0.28;
        public const double GustOmega = 0.62;
        public const double GustForceScale = 0.0034;
        public const double FuelMax = 100;

        public readonly double ThreeMin, TwoMin;
        public readonly double HavenLandingFuel, HavenSwellAmp;
        readonly Dictionary<string, MissionAssist> missions = new Dictionary<string, MissionAssist>();

        public static MissionRamp Load(string repoRoot, double havenLandingFuel, double havenSwellAmp)
            => FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "mission-ramp.json"))), havenLandingFuel, havenSwellAmp);

        public static MissionRamp FromJson(JsonObject o, double havenLandingFuel, double havenSwellAmp)
        {
            var stars = o.Obj("stars");
            var ramp = new MissionRamp(stars.Num("threeMin"), stars.Num("twoMin"), havenLandingFuel, havenSwellAmp);
            foreach (var kv in o.Obj("missions"))
            {
                var m = (JsonObject)kv.Value;
                ramp.missions[kv.Key] = new MissionAssist(m.Num("wind"), m.Num("landingFuel"), m.Num("swellAmp"));
            }
            return ramp;
        }

        MissionRamp(double threeMin, double twoMin, double havenLandingFuel, double havenSwellAmp)
        {
            ThreeMin = threeMin; TwoMin = twoMin;
            HavenLandingFuel = havenLandingFuel; HavenSwellAmp = havenSwellAmp;
        }

        public bool TryGet(string missionId, out MissionAssist assist) => missions.TryGetValue(missionId, out assist);

        /// <summary>difficulty.wind * mission.wind. A missing mission keeps the difficulty wind (scale 1).</summary>
        public double WindForce(double difficultyWind, string missionId)
        {
            double baseWind = double.IsNaN(difficultyWind) || double.IsInfinity(difficultyWind) ? 0 : difficultyWind;
            double scale = 1;
            if (missions.TryGetValue(missionId, out var m) && !double.IsNaN(m.Wind) && !double.IsInfinity(m.Wind)) scale = m.Wind;
            return baseWind * scale;
        }

        public double LandingFuelFloor(string missionId)
            => missions.TryGetValue(missionId, out var m) ? m.LandingFuel : HavenLandingFuel;

        public double SwellAmplitude(string missionId)
            => missions.TryGetValue(missionId, out var m) ? m.SwellAmp : HavenSwellAmp;

        /// <summary>Session fuel is raised to the mission floor and capped at FuelMax. Extra fuel is kept.</summary>
        public double HavenFuel(double sessionFuel, string missionId)
            => Math.Min(FuelMax, Math.Max(sessionFuel, LandingFuelFloor(missionId)));

        public HavenDrift Drift(double swellT, double difficultyWind, string missionId)
        {
            double product = WindForce(difficultyWind, missionId);
            double wind = product == 0 || double.IsNaN(product) ? DriftWindFloor : product;
            double t = double.IsNaN(swellT) || double.IsInfinity(swellT) ? 0 : swellT;
            double swell = Math.Sin(t * SwellOmega) * SwellAmplitude(missionId);
            double gust = Math.Sin(t * GustOmega) * wind;
            return new HavenDrift(product, wind, swell, gust, gust * GustForceScale);
        }

        public bool UnlocksNext(string reason) => reason == "recovered" || reason == "salvage";

        /// <summary>0 for a splash, RUD, or water. 1 for a tip, a miss, or a salvage. 2 or 3 only for a recovered catch.</summary>
        public int Stars(string reason, string kind, double vy, double dx, double landingVy, double landingTol, bool tipped, bool recovered)
        {
            if (reason == "splash" || reason == "rud" || kind == "water") return 0;
            if (kind != "deck") return 0;
            double limVy = Limit(landingVy, 0.1);
            double limDx = Limit(landingTol, 1);
            double vyAbs = double.IsNaN(vy) || double.IsInfinity(vy) ? 0 : Math.Abs(vy);
            double dxAbs = double.IsNaN(dx) || double.IsInfinity(dx) ? 0 : Math.Abs(dx);
            double margin = Math.Min(1 - vyAbs / limVy, 1 - dxAbs / limDx);
            if (tipped || margin < 0 || !recovered) return 1;
            if (margin >= ThreeMin) return 3;
            if (margin >= TwoMin) return 2;
            return 1;
        }

        static double Limit(double value, double floor)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value == 0) value = floor;
            return Math.Max(floor, value);
        }
    }

    public readonly struct MissionAssist
    {
        public readonly double Wind, LandingFuel, SwellAmp;
        public MissionAssist(double wind, double landingFuel, double swellAmp)
        {
            Wind = wind; LandingFuel = landingFuel; SwellAmp = swellAmp;
        }
    }

    public readonly struct HavenDrift
    {
        public readonly double WindForce, DriftWind, Swell, Gust, ForceX;
        public HavenDrift(double windForce, double driftWind, double swell, double gust, double forceX)
        {
            WindForce = windForce; DriftWind = driftWind; Swell = swell; Gust = gust; ForceX = forceX;
        }
    }

    /// <summary>One §4 contract. The payload mass, volume and target drive the goal.</summary>
    public sealed class CampaignMission
    {
        public int Index;
        public string Id, Name, PayloadId;
        /// <summary>Catalog orbit id. Home stamps this on payload.orbit. Legacy builds used kh- plus the mission id.</summary>
        public string OrbitId;
        public double MassKg, VolumeM3, DiameterM;
        public string TargetKind;
        public double ApoKm, PeriKm, IncDeg, C3Km2PerS2;
        public double BudgetKc, RewardKc;
        public bool GentleDefault;
        public double CoastHours, MaxG = double.NaN, ReliabilityMin = double.NaN;
        public bool MarginNonNegative, StrapOnsAllowed, NoRedRisks, RandomFailures, LandBoosters;
        public string LandingMode, LandingGrade, LandingSite;
        public string Customer, Control, Goal, WhatItIs, WhatItDoes, Why, Form, CodexId, Band;
        public readonly List<string> Unlocks = new List<string>();

        public bool HasInclination => TargetKind == "circular" || TargetKind == "ellipse" || TargetKind == "c3";
        public bool LandingRequired => LandingMode == "rtls" || LandingMode == "barge";

        /// <summary>
        /// Altitude the guidance law and the prop-depletion check use.
        /// An apogee hop coasts after burnout, so depletion is judged against a 1 km floor.
        /// The pass check still requires the Kepler apogee within ±5% of ApoKm.
        /// Ellipse and C3 insert at periapsis. Circular aims at the orbit altitude.
        /// </summary>
        public double GuidanceAltM
        {
            get
            {
                if (TargetKind == "apogee") return MissionAim.HopDepletionFloorM;
                if (TargetKind == "circular") return ApoKm * 1000;
                return PeriKm * 1000;
            }
        }

        public double RequiredDeltaV(double latitudeDeg = 28.5)
            => MissionScore.RequiredDeltaV(TargetKind, ApoKm, PeriKm, C3Km2PerS2, latitudeDeg);
    }

    /// <summary>The campaign in data/campaign.json. Haven assists stay on MissionRamp.</summary>
    public sealed class Campaign
    {
        public int Version;
        public double StartingBankKc;
        public bool Timers, Energy, Ads;
        public readonly List<string> StarterUnlocks = new List<string>();
        public readonly List<ulong> Seeds = new List<ulong>();
        public readonly List<CampaignMission> Missions = new List<CampaignMission>();

        public static Campaign Load(string repoRoot)
            => FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "campaign.json"))));

        public static Campaign FromJson(JsonObject o)
        {
            var c = new Campaign { Version = (int)o.Num("version", 1), StartingBankKc = o.Num("startingBankKc") };
            var dark = o.Obj("darkPatterns");
            if (dark != null) { c.Timers = dark.Bool("timers"); c.Energy = dark.Bool("energy"); c.Ads = dark.Bool("ads"); }
            var starter = o.Arr("starterUnlocks");
            if (starter != null) foreach (var x in starter) if (x is string s) c.StarterUnlocks.Add(s);
            var seeds = o.Arr("seeds");
            if (seeds != null) foreach (var x in seeds) if (x is double d) c.Seeds.Add((ulong)d);
            foreach (JsonObject m in o.Arr("missions")) c.Missions.Add(ReadMission(m));
            return c;
        }

        public CampaignMission Mission(int index)
        {
            for (int i = 0; i < Missions.Count; i++) if (Missions[i].Index == index) return Missions[i];
            return null;
        }

        public CampaignMission ById(string id)
        {
            for (int i = 0; i < Missions.Count; i++) if (Missions[i].Id == id) return Missions[i];
            return null;
        }

        static CampaignMission ReadMission(JsonObject o)
        {
            var m = new CampaignMission
            {
                Index = (int)o.Num("index"),
                Id = o.Str("id"),
                Name = o.Str("name"),
                PayloadId = o.Str("payloadId"),
                OrbitId = o.Str("orbit"),
                MassKg = o.Num("massKg"),
                VolumeM3 = o.Num("volumeM3"),
                DiameterM = o.Num("diameterM"),
                BudgetKc = o.Num("budgetKc"),
                RewardKc = o.Num("rewardKc"),
                GentleDefault = o.Bool("gentleDefault"),
            };
            var t = o.Obj("target");
            if (t != null)
            {
                m.TargetKind = t.Str("kind");
                m.ApoKm = t.Num("apoKm");
                m.PeriKm = t.Num("periKm");
                m.IncDeg = t.Num("incDeg");
                m.C3Km2PerS2 = t.Num("c3Km2PerS2");
            }
            var reqs = o.Arr("requirements");
            if (reqs != null)
            {
                foreach (JsonObject r in reqs)
                {
                    string id = r.Str("id");
                    if (id == "margin") m.MarginNonNegative = true;
                    else if (id == "maxG") m.MaxG = r.Num("max");
                    else if (id == "strapOns") m.StrapOnsAllowed = r.Bool("allow");
                    else if (id == "landing")
                    {
                        m.LandingMode = r.Str("mode");
                        m.LandingGrade = r.Str("grade");
                        m.LandingSite = r.Str("site");
                        m.LandBoosters = r.Bool("boosters");
                    }
                    else if (id == "reliability") m.ReliabilityMin = r.Num("min");
                    else if (id == "noRedRisks") m.NoRedRisks = r.Bool("noRedRisks", true);
                    else if (id == "randomFailures") m.RandomFailures = r.Bool("randomFailures", true);
                    else if (id == "coastHours") m.CoastHours = r.Num("hours");
                }
            }
            var unlocks = o.Arr("unlocks");
            if (unlocks != null) foreach (var x in unlocks) if (x is string s) m.Unlocks.Add(s);
            if (string.IsNullOrEmpty(m.OrbitId)) m.OrbitId = "kh-" + (m.Id ?? "");
            m.Band = o.Str("band");
            var brief = o.Obj("briefing");
            if (brief != null)
            {
                m.Customer = brief.Str("customer");
                m.Control = brief.Str("control");
                m.Goal = brief.Str("goal");
                m.WhatItIs = brief.Str("whatItIs");
                m.WhatItDoes = brief.Str("whatItDoes");
                m.Why = brief.Str("why");
                m.Form = brief.Str("form");
                m.CodexId = brief.Str("codex");
            }
            return m;
        }
    }
}
