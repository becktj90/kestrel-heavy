// Open contracts after KH-20. A seeded board of three jobs, offline and deterministic.
// Difficulty follows k = contracts cleared. Only (payload, orbit, k) cells that FlightBatch
// marked viable are offered. No timers, no expiry, no purchases. Kc pays for hardware in-game.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public sealed class ContractArchetype
    {
        public string Id, NamePattern, Kind, CatalogId;
        public double BaseMassKg, VolumeM3, DiameterM, MaxG;
    }

    public sealed class ContractOrbit
    {
        public string Id, Band, Label, Kind;
        public int Step;
        public double ApoKm, PeriKm, IncDeg, C3Km2PerS2, CoastHours;
    }

    public sealed class ContractRequirement
    {
        public string Id;
        public double Min = double.NaN, Max = double.NaN;
        public bool FromArchetype;
    }

    /// <summary>One viable (payload, orbit, k) cell. Designs are the three FlightBatch passes.</summary>
    public sealed class ContractCell
    {
        public string Id, ArchetypeId, OrbitId;
        public int K;
        public double MassKg, BudgetKc;
        public readonly List<Design> Designs = new List<Design>();
        public ContractArchetype Archetype;
        public ContractOrbit Orbit;
        public ContractPool Pool;

        public double RewardKc => BudgetKc;
        public double Difficulty => ContractGen.Difficulty(K);
        public double WindScale => ContractGen.WindScale(K);
    }

    /// <summary>One offered contract. Parameters are the viable cell, not an unflown scale step.</summary>
    public sealed class OpenContract
    {
        public string Id, CellId, Label, Name, PayloadKind, CatalogPayloadId;
        public string OrbitId, OrbitLabel, TargetKind;
        public int Number, Slot, RequestedCleared, ProvenK;
        public double MassKg, VolumeM3, DiameterM, MaxG;
        public double ApoKm, PeriKm, IncDeg, C3Km2PerS2, CoastHours;
        public double Difficulty, WindScale, BudgetKc, RewardKc;
        public ulong BoardSeed, CardSeed;
        public LaunchSite Site;
        public string TimeId, TimeLabel, Lighting, Goal, StatusLine, EngineerLine, WindLine;
        public int Hour, DayOfYear;
        public SeededWind Wind;
        public double WindKt;
        public readonly List<string> Requirements = new List<string>();
        public ContractCell Cell;

        public CampaignMission Mission { get; internal set; }
    }

    public sealed class ContractPool
    {
        public int Version = 1;
        public int OfferCount = ContractGen.OfferCount;
        public int FirstNumber = ContractGen.FirstNumber;
        public string LabelPrefix = "KH";
        public bool Timers, Energy, Ads, Expiry, Purchases, Daily;
        public readonly List<ulong> Seeds = new List<ulong>();
        public readonly List<ContractArchetype> Archetypes = new List<ContractArchetype>();
        public readonly List<ContractOrbit> Orbits = new List<ContractOrbit>();
        public readonly List<ContractRequirement> Ladder = new List<ContractRequirement>();
        public readonly List<ContractCell> Cells = new List<ContractCell>();
        public readonly List<string> Rejected = new List<string>();
        public readonly Dictionary<string, ContractArchetype> ArchetypeById = new Dictionary<string, ContractArchetype>();
        public readonly Dictionary<string, ContractOrbit> OrbitById = new Dictionary<string, ContractOrbit>();

        public int MaxOrbitStep
        {
            get
            {
                int m = 0;
                for (int i = 0; i < Orbits.Count; i++) if (Orbits[i].Step > m) m = Orbits[i].Step;
                return m;
            }
        }
    }

    public static class ContractGen
    {
        public const int OfferCount = 3;
        public const int FirstNumber = 21;
        public const double MassCapKg = 12000;
        public const double WindCap = 1.6;
        public const int MaxExtraRequirements = 3;
        public const int RequirementsEvery = 3;
        public const int OrbitFloorEvery = 3;
        public const double DifficultyPerCleared = 0.15;
        public const double MassPerCleared = 0.1;
        public const double WindPerCleared = 0.05;
        public const bool PurchasePath = false;
        public const bool Expires = false;

        public static double Difficulty(int contractsCleared)
            => 1 + DifficultyPerCleared * NonNegative(contractsCleared);

        public static double MassScale(int contractsCleared)
            => 1 + MassPerCleared * NonNegative(contractsCleared);

        /// <summary>Base mass times (1 + 0.1 k), capped at the 12 t class, rounded to the kilogram.</summary>
        public static double MassKg(double baseKg, int contractsCleared)
        {
            double scaled = baseKg * MassScale(contractsCleared);
            if (scaled > MassCapKg) scaled = MassCapKg;
            if (scaled < 0) scaled = 0;
            return Math.Round(scaled, MidpointRounding.AwayFromZero);
        }

        public static double WindScale(int contractsCleared)
        {
            double s = 1 + WindPerCleared * NonNegative(contractsCleared);
            return s > WindCap ? WindCap : s;
        }

        public static int ExtraRequirements(int contractsCleared)
        {
            int n = NonNegative(contractsCleared) / RequirementsEvery;
            return n > MaxExtraRequirements ? MaxExtraRequirements : n;
        }

        public static int OrbitFloor(int contractsCleared, int maxStep)
        {
            int step = NonNegative(contractsCleared) / OrbitFloorEvery;
            if (step > maxStep) step = maxStep;
            if (step < 0) step = 0;
            return step;
        }

        /// <summary>FNV-1a 64 of the profile id and the number of contracts cleared. Offline. No clock.</summary>
        public static ulong Hash(string profileId, int contractsCleared)
        {
            ulong h = 14695981039346656037UL;
            string s = profileId ?? "";
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 1099511628211UL;
            }
            ulong k = (ulong)NonNegative(contractsCleared);
            h ^= k & 0xFF;
            h *= 1099511628211UL;
            h ^= (k >> 8) & 0xFF;
            h *= 1099511628211UL;
            h ^= (k >> 16) & 0xFF;
            h *= 1099511628211UL;
            h ^= (k >> 24) & 0xFF;
            h *= 1099511628211UL;
            return h == 0 ? 1 : h;
        }

        public static ContractPool Load(string repoRoot)
        {
            var pool = FromPool(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "contracts.json"))));
            string viablePath = Path.Combine(repoRoot, "data", "golden", "contracts-viable.json");
            if (File.Exists(viablePath))
                ReadViable(pool, MiniJson.ParseObject(File.ReadAllText(viablePath)));
            return pool;
        }

        public static ContractPool FromPool(JsonObject o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));
            var pool = new ContractPool { Version = (int)o.Num("version", 1) };
            if (o.ContainsKey("offerCount")) pool.OfferCount = (int)o.Num("offerCount");
            if (o.ContainsKey("firstNumber")) pool.FirstNumber = (int)o.Num("firstNumber");
            pool.LabelPrefix = o.Str("labelPrefix", "KH");
            if (string.IsNullOrEmpty(pool.LabelPrefix)) pool.LabelPrefix = "KH";
            var dark = o.Obj("darkPatterns");
            if (dark != null)
            {
                pool.Timers = dark.Bool("timers");
                pool.Energy = dark.Bool("energy");
                pool.Ads = dark.Bool("ads");
                pool.Expiry = dark.Bool("expiry");
                pool.Purchases = dark.Bool("purchases");
                pool.Daily = dark.Bool("daily");
            }
            if (pool.Timers || pool.Energy || pool.Ads || pool.Expiry || pool.Purchases || pool.Daily)
                throw new InvalidOperationException("Open contracts have no timers, expiry, or purchases.");
            var seeds = o.Arr("seeds");
            if (seeds != null) foreach (var x in seeds) if (x is double d) pool.Seeds.Add((ulong)d);
            foreach (JsonObject a in o.Arr("archetypes"))
            {
                var arch = new ContractArchetype
                {
                    Id = a.Str("id"),
                    NamePattern = a.Str("name"),
                    Kind = a.Str("kind"),
                    CatalogId = a.Str("catalogId"),
                    BaseMassKg = a.Num("baseMassKg"),
                    VolumeM3 = a.Num("volumeM3"),
                    DiameterM = a.Num("diameterM"),
                    MaxG = a.Num("maxG"),
                };
                if (string.IsNullOrEmpty(arch.Id)) throw new InvalidOperationException("archetype missing id");
                pool.Archetypes.Add(arch);
                pool.ArchetypeById[arch.Id] = arch;
            }
            foreach (JsonObject b in o.Arr("orbits"))
            {
                var orbit = new ContractOrbit
                {
                    Id = b.Str("id"),
                    Band = b.Str("band"),
                    Label = b.Str("label"),
                    Kind = b.Str("kind"),
                    Step = (int)b.Num("step"),
                    ApoKm = b.Num("apoKm"),
                    PeriKm = b.Num("periKm"),
                    IncDeg = b.Num("incDeg"),
                    C3Km2PerS2 = b.Num("c3Km2PerS2"),
                    CoastHours = b.Num("coastHours"),
                };
                if (string.IsNullOrEmpty(orbit.Id)) throw new InvalidOperationException("orbit missing id");
                pool.Orbits.Add(orbit);
                pool.OrbitById[orbit.Id] = orbit;
            }
            var ladder = o.Arr("requirementLadder");
            if (ladder != null)
            {
                foreach (JsonObject r in ladder)
                {
                    pool.Ladder.Add(new ContractRequirement
                    {
                        Id = r.Str("id"),
                        Min = r.ContainsKey("min") ? r.Num("min") : double.NaN,
                        Max = r.ContainsKey("max") ? r.Num("max") : double.NaN,
                        FromArchetype = r.Bool("fromArchetype"),
                    });
                }
            }
            return pool;
        }

        public static void ReadViable(ContractPool pool, JsonObject o)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (o == null) return;
            var rejected = o.Arr("rejected");
            if (rejected != null)
            {
                foreach (var x in rejected)
                {
                    if (x is string s) pool.Rejected.Add(s);
                    else if (x is JsonObject row && row.Str("id") != null) pool.Rejected.Add(row.Str("id"));
                }
            }
            var cells = o.Arr("cells");
            if (cells == null) return;
            foreach (JsonObject c in cells)
            {
                string archId = c.Str("archetype");
                string orbitId = c.Str("orbit");
                if (!pool.ArchetypeById.TryGetValue(archId ?? "", out var arch))
                    throw new InvalidOperationException("viable cell archetype " + archId);
                if (!pool.OrbitById.TryGetValue(orbitId ?? "", out var orbit))
                    throw new InvalidOperationException("viable cell orbit " + orbitId);
                int k = (int)c.Num("k");
                double mass = c.Num("massKg");
                double expect = MassKg(arch.BaseMassKg, k);
                if (Math.Abs(mass - expect) > 0.5)
                    throw new InvalidOperationException(c.Str("id") + " mass " + mass + " is not the formula mass " + expect);
                var cell = new ContractCell
                {
                    Id = c.Str("id"),
                    ArchetypeId = archId,
                    OrbitId = orbitId,
                    K = k,
                    MassKg = expect,
                    BudgetKc = c.Num("budgetKc"),
                    Archetype = arch,
                    Orbit = orbit,
                };
                if (string.IsNullOrEmpty(cell.Id)) cell.Id = archId + "-" + orbitId + "-k" + k.ToString(CultureInfo.InvariantCulture);
                if (!(cell.BudgetKc > 0)) throw new InvalidOperationException(cell.Id + " needs a budget");
                if (pool.Rejected.Contains(cell.Id)) throw new InvalidOperationException(cell.Id + " is both viable and rejected");
                cell.Pool = pool;
                var designs = c.Arr("designs");
                if (designs == null || designs.Count < OfferCount)
                    throw new InvalidOperationException(cell.Id + " needs 3 designs");
                foreach (JsonObject raw in designs) cell.Designs.Add(Design.FromJson(raw));
                pool.Cells.Add(cell);
            }
        }

        public static CampaignMission ToMission(ContractCell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            var orbit = cell.Orbit;
            var arch = cell.Archetype;
            var mission = new CampaignMission
            {
                Index = FirstNumber + cell.K,
                Id = cell.Id,
                Name = arch.Kind + " " + orbit.Label,
                PayloadId = arch.CatalogId,
                MassKg = cell.MassKg,
                VolumeM3 = arch.VolumeM3,
                DiameterM = arch.DiameterM,
                TargetKind = orbit.Kind,
                ApoKm = orbit.ApoKm,
                PeriKm = orbit.PeriKm,
                IncDeg = orbit.IncDeg,
                C3Km2PerS2 = orbit.C3Km2PerS2,
                CoastHours = orbit.CoastHours,
                BudgetKc = cell.BudgetKc,
                RewardKc = cell.RewardKc,
                GentleDefault = false,
            };
            ApplyLadder(cell.Pool, mission, arch, cell.K);
            return mission;
        }

        public static void ApplyLadder(ContractPool pool, CampaignMission mission, ContractArchetype arch, int k)
        {
            int n = ExtraRequirements(k);
            if (mission == null || arch == null || pool == null) return;
            for (int i = 0; i < n && i < pool.Ladder.Count; i++)
            {
                var req = pool.Ladder[i];
                if (req.Id == "margin") mission.MarginNonNegative = true;
                else if (req.Id == "maxG") mission.MaxG = req.FromArchetype || double.IsNaN(req.Max) ? arch.MaxG : req.Max;
                else if (req.Id == "reliability") mission.ReliabilityMin = double.IsNaN(req.Min) ? 0.98 : req.Min;
                else if (req.Id == "noRedRisks") mission.NoRedRisks = true;
                else if (req.Id == "coastHours") mission.CoastHours = double.IsNaN(req.Min) ? mission.CoastHours : req.Min;
            }
        }

        /// <summary>
        /// Three contracts for this profile and cleared count. The same pair always returns
        /// the same board. Cells above the cleared count, and cells absent from the viable table, are not offered.
        /// </summary>
        public static List<OpenContract> Offer(string repoRoot, string profileId, int contractsCleared)
            => Offer(Load(repoRoot), SiteBook.Load(repoRoot), WeatherBook.Load(repoRoot), profileId, contractsCleared);

        public static List<OpenContract> Offer(ContractPool pool, SiteBook sites, WeatherBook weather, string profileId, int contractsCleared)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (pool.Timers || pool.Expiry || pool.Purchases)
                throw new InvalidOperationException("Open contracts have no timers, expiry, or purchases.");
            int k = NonNegative(contractsCleared);
            ulong board = Hash(profileId, k);
            var chosen = Choose(pool, board, k);
            var list = new List<OpenContract>();
            for (int slot = 0; slot < chosen.Count && slot < pool.OfferCount; slot++)
                list.Add(Compose(pool, sites, weather, chosen[slot], k, board, slot));
            return list;
        }

        public static List<ContractCell> Choose(ContractPool pool, ulong board, int contractsCleared)
        {
            int k = NonNegative(contractsCleared);
            int floor = OrbitFloor(k, pool.MaxOrbitStep);
            var viable = new List<ContractCell>();
            for (int i = 0; i < pool.Cells.Count; i++)
            {
                var cell = pool.Cells[i];
                if (cell.K <= k && !pool.Rejected.Contains(cell.Id)) viable.Add(cell);
            }
            List<ContractCell> band = viable;
            for (int relax = 0; relax <= pool.MaxOrbitStep; relax++)
            {
                int minStep = floor - relax;
                if (minStep < 0) minStep = 0;
                band = new List<ContractCell>();
                for (int i = 0; i < viable.Count; i++)
                    if (viable[i].Orbit != null && viable[i].Orbit.Step >= minStep) band.Add(viable[i]);
                if (band.Count >= pool.OfferCount || minStep == 0) break;
            }
            band.Sort((a, b) =>
            {
                int dk = b.K.CompareTo(a.K);
                if (dk != 0) return dk;
                ulong ha = Mix(board, a.Id);
                ulong hb = Mix(board, b.Id);
                int dh = ha.CompareTo(hb);
                if (dh != 0) return dh;
                return string.CompareOrdinal(a.Id, b.Id);
            });
            var picked = new List<ContractCell>();
            int take = pool.OfferCount < band.Count ? pool.OfferCount : band.Count;
            for (int i = 0; i < take; i++) picked.Add(band[i]);
            return picked;
        }

        static OpenContract Compose(ContractPool pool, SiteBook sites, WeatherBook weather, ContractCell cell, int k, ulong board, int slot)
        {
            ulong card = Mix(board, cell.Id + "#" + slot.ToString(CultureInfo.InvariantCulture));
            int number = pool.FirstNumber + k + slot;
            if (number < pool.FirstNumber) number = pool.FirstNumber;
            var offer = new OpenContract
            {
                Number = number,
                Slot = slot,
                Id = "kc" + number.ToString(CultureInfo.InvariantCulture),
                CellId = cell.Id,
                Label = pool.LabelPrefix + "-" + number.ToString(CultureInfo.InvariantCulture),
                RequestedCleared = k,
                ProvenK = cell.K,
                Cell = cell,
                CatalogPayloadId = cell.Archetype.CatalogId,
                PayloadKind = cell.Archetype.Kind,
                OrbitId = cell.Orbit.Id,
                OrbitLabel = cell.Orbit.Label,
                TargetKind = cell.Orbit.Kind,
                MassKg = cell.MassKg,
                VolumeM3 = cell.Archetype.VolumeM3,
                DiameterM = cell.Archetype.DiameterM,
                MaxG = cell.Archetype.MaxG,
                ApoKm = cell.Orbit.ApoKm,
                PeriKm = cell.Orbit.PeriKm,
                IncDeg = cell.Orbit.IncDeg,
                C3Km2PerS2 = cell.Orbit.C3Km2PerS2,
                CoastHours = cell.Orbit.CoastHours,
                Difficulty = cell.Difficulty,
                WindScale = cell.WindScale,
                BudgetKc = cell.BudgetKc,
                RewardKc = cell.RewardKc,
                BoardSeed = board,
                CardSeed = card,
            };
            int serial = (int)(card % 24UL) + 1;
            offer.Name = (cell.Archetype.NamePattern ?? cell.Archetype.Kind).Replace("{n}", serial.ToString(CultureInfo.InvariantCulture));
            ApplyEnvironment(offer, sites, weather, card, cell.WindScale);
            var mission = ToMission(cell);
            mission.Id = offer.Id;
            mission.Index = number;
            mission.Name = offer.Name;
            offer.Mission = mission;
            if (mission.MarginNonNegative) offer.Requirements.Add("margin");
            if (!double.IsNaN(mission.MaxG)) offer.Requirements.Add("maxG");
            if (!double.IsNaN(mission.ReliabilityMin)) offer.Requirements.Add("reliability");
            if (mission.NoRedRisks) offer.Requirements.Add("noRedRisks");
            offer.Goal = offer.Name + " to " + offer.OrbitLabel + ".";
            string pad = offer.Site != null ? offer.Site.Name : "Pad";
            offer.StatusLine = "Wind " + offer.WindLine + " · " + offer.TimeLabel + " launch · Pad: " + pad + " · Seed " + (card & 0xFFFFUL).ToString("X4");
            double lat = offer.Site != null ? offer.Site.LatitudeDeg : 0;
            double freeInc = offer.Site != null ? offer.Site.FreeInclinationDeg : 0;
            offer.EngineerLine = "d " + offer.Difficulty.ToString("0.00", CultureInfo.InvariantCulture)
                + ", mass " + offer.MassKg.ToString("0", CultureInfo.InvariantCulture) + " kg, wind scale "
                + offer.WindScale.ToString("0.00", CultureInfo.InvariantCulture)
                + ", lat " + lat.ToString("0.0", CultureInfo.InvariantCulture)
                + "°, free inclination " + freeInc.ToString("0.0", CultureInfo.InvariantCulture) + "°.";
            return offer;
        }

        static void ApplyEnvironment(OpenContract offer, SiteBook sites, WeatherBook weather, ulong cardSeed, double windScale)
        {
            if (weather == null) weather = new WeatherBook();
            ulong stream = MissionBriefing.EnvironmentState(cardSeed);
            double uSurface = MissionBriefing.Draw(ref stream);
            double uDir = MissionBriefing.Draw(ref stream);
            double uShearA = MissionBriefing.Draw(ref stream);
            double uShearB = MissionBriefing.Draw(ref stream);
            double uTime = MissionBriefing.Draw(ref stream);
            double uSite = MissionBriefing.Draw(ref stream);
            double uDay = MissionBriefing.Draw(ref stream);
            double surface = MissionBriefing.Rayleigh(uSurface, weather.RayleighScaleMs);
            if (windScale > 0) surface *= windScale;
            var dir = weather.Direction(uDir);
            double shear = weather.Shear(uShearA, uShearB);
            var time = weather.Time(uTime);
            int day = 1 + (int)(uDay * 365.0);
            if (day < 1) day = 1;
            if (day > 365) day = 365;
            offer.Site = PickSite(sites, uSite, offer.IncDeg);
            offer.TimeId = time.Id;
            offer.TimeLabel = time.Label;
            offer.Lighting = time.Lighting;
            offer.Hour = time.Hour;
            offer.DayOfYear = day;
            offer.Wind = weather.Profile(surface, dir, shear);
            offer.WindKt = surface * weather.KtPerMs;
            offer.WindLine = MissionBriefing.FormatWind(surface, dir.Id, weather.KtPerMs);
        }

        public static LaunchSite PickSite(SiteBook sites, double u, double incDeg)
        {
            if (sites == null || sites.Sites.Count == 0) return null;
            var open = new List<LaunchSite>();
            for (int i = 0; i < sites.Sites.Count; i++)
                if (sites.Sites[i].InclinationOpen(incDeg)) open.Add(sites.Sites[i]);
            if (open.Count == 0)
            {
                for (int i = 0; i < sites.Sites.Count; i++) open.Add(sites.Sites[i]);
            }
            int n = (int)(u * open.Count);
            if (n >= open.Count) n = open.Count - 1;
            if (n < 0) n = 0;
            return open[n];
        }

        static ulong Mix(ulong board, string text)
        {
            ulong h = board == 0 ? 1 : board;
            string s = text ?? "";
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 1099511628211UL;
            }
            return h == 0 ? 1 : h;
        }

        static int NonNegative(int k) => k < 0 ? 0 : k;
    }
}
