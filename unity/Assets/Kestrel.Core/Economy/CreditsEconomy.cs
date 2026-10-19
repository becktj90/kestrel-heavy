// Fictional credits. Earn them on a clear, a first clear, and a new medal.
// Spend them on parts and hangar hardware. The Hopper quote is 0, a miss pays nothing,
// and a part you paid for refunds in full. No timers, no energy, no ads.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Json;
using Kestrel.Core.Save;

namespace Kestrel.Core.Economy
{
    public sealed class CreditsTable
    {
        public readonly long Starting, FirstClear, Medal, UnknownEngine, UnknownMaterial;
        public readonly long ExtraStage, StrapOn, Legs, GridFins, Rcs, Fins, Fairing;
        public readonly string FreeStarter;
        readonly long[] missionClear;
        readonly Dictionary<string, long> engines, materials, avionics, finishes, decals;

        CreditsTable(long starting, long firstClear, long medal, long unknownEngine, long unknownMaterial,
            long extraStage, long strapOn, long legs, long gridFins, long rcs, long fins, long fairing,
            string freeStarter, long[] missionClear, Dictionary<string, long> engines, Dictionary<string, long> materials,
            Dictionary<string, long> avionics, Dictionary<string, long> finishes, Dictionary<string, long> decals)
        {
            Starting = starting;
            FirstClear = firstClear;
            Medal = medal;
            UnknownEngine = unknownEngine;
            UnknownMaterial = unknownMaterial;
            ExtraStage = extraStage;
            StrapOn = strapOn;
            Legs = legs;
            GridFins = gridFins;
            Rcs = rcs;
            Fins = fins;
            Fairing = fairing;
            FreeStarter = freeStarter ?? "";
            this.missionClear = missionClear ?? new long[0];
            this.engines = engines;
            this.materials = materials;
            this.avionics = avionics;
            this.finishes = finishes;
            this.decals = decals;
        }

        public static CreditsTable Parse(string json) => Parse(MiniJson.ParseObject(json));

        public static CreditsTable Parse(JsonObject o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));
            var dark = o.Obj("darkPatterns");
            if (dark != null && (dark.Bool("timers") || dark.Bool("energy") || dark.Bool("ads")))
                throw new InvalidOperationException("Credits dark patterns are off.");
            var parts = o.Obj("parts") ?? new JsonObject();
            var hangar = o.Obj("hangar");
            var clears = new List<long>();
            var arr = o.Arr("missionClear");
            if (arr != null)
                for (int i = 0; i < arr.Count; i++)
                    if (arr[i] is double d) clears.Add((long)Math.Round(d, MidpointRounding.AwayFromZero));
            if (clears.Count == 0) throw new InvalidOperationException("credits missionClear is empty");
            return new CreditsTable(
                Long(o, "starting"), Long(o, "firstClear"), Long(o, "medal"),
                Long(o, "unknownEngine"), Long(o, "unknownMaterial"),
                Long(parts, "extraStage"), Long(parts, "strapOn"), Long(parts, "legs"),
                Long(parts, "gridFins"), Long(parts, "rcs"), Long(parts, "fins"), Long(parts, "fairing"),
                o.Str("freeStarter") ?? "",
                clears.ToArray(),
                Map(o.Obj("engines")), Map(o.Obj("materials")), Map(o.Obj("avionics")),
                Map(hangar?.Obj("finishes")), Map(hangar?.Obj("decals")));
        }

        public long MissionClear(int index)
        {
            if (missionClear.Length == 0 || index < 1) return 0;
            int i = index - 1;
            if (i >= missionClear.Length) return missionClear[missionClear.Length - 1];
            return missionClear[i];
        }

        public int MissionCount => missionClear.Length;

        /// <summary>What this rocket costs in credits right now. The Hopper template quotes 0.</summary>
        public long Quote(JsonObject design)
        {
            if (design == null) return 0;
            long total = 0;
            var stages = design.Arr("stages");
            int n = 0;
            if (stages != null)
            {
                for (int i = 0; i < stages.Count; i++)
                {
                    if (!(stages[i] is JsonObject stage)) continue;
                    n++;
                    total += EngineCost(stage);
                    total += MaterialOf(stage.Str("material"));
                }
            }
            if (n > 1) total += (n - 1) * ExtraStage;
            var strap = design.Obj("strapOns");
            int boosters = Count(strap, "count");
            if (boosters > 0)
            {
                total += boosters * StrapOn;
                total += EngineCost(strap) * boosters;
                total += MaterialOf(strap.Str("material")) * boosters;
            }
            var landing = design.Obj("landing");
            if (landing != null)
            {
                if (landing.Bool("legs")) total += Legs;
                if (landing.Bool("gridFins")) total += GridFins;
                if (landing.Bool("rcs")) total += Rcs;
            }
            var aero = design.Obj("aero");
            if (aero != null && aero.Bool("fins")) total += Fins;
            if (design.Obj("fairing") != null) total += Fairing;
            var av = design.Obj("avionics");
            string tier = av != null ? av.Str("tier") : null;
            total += AvionicsOf(tier);
            total += HangarOf(design.Obj("look"));
            return total < 0 ? 0 : total;
        }

        long EngineCost(JsonObject part)
        {
            if (part == null) return 0;
            string id = part.Str("engine");
            if (string.IsNullOrEmpty(id)) return 0;
            int n = Count(part, "engineCount");
            if (n < 1) n = 1;
            return Engine(id) * n;
        }

        long Engine(string id) => MapOf(engines, id, UnknownEngine);

        long MaterialOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return MapOf(materials, id, UnknownMaterial);
        }

        long AvionicsOf(string tier)
        {
            if (string.IsNullOrEmpty(tier)) tier = "standard";
            return MapOf(avionics, tier, 0);
        }

        long HangarOf(JsonObject look)
        {
            if (look == null) return 0;
            long total = 0;
            var finishesOn = look.Obj("finishes");
            if (finishesOn != null)
            {
                foreach (var kv in finishesOn)
                    if (kv.Value is string id) total += MapOf(finishes, id, 0);
            }
            var decalsOn = look.Arr("decals");
            if (decalsOn != null)
            {
                for (int i = 0; i < decalsOn.Count; i++)
                    if (decalsOn[i] is JsonObject row) total += MapOf(decals, row.Str("id"), 0);
            }
            return total;
        }

        static long MapOf(Dictionary<string, long> map, string id, long fallback)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            if (map != null && map.TryGetValue(id, out var price)) return price;
            return fallback;
        }

        static Dictionary<string, long> Map(JsonObject o)
        {
            var map = new Dictionary<string, long>();
            if (o == null) return map;
            foreach (var kv in o)
            {
                long n = AsLong(kv.Value);
                if (kv.Key.Length > 0) map[kv.Key] = n;
            }
            return map;
        }

        static int Count(JsonObject o, string key)
        {
            if (o == null || !o.ContainsKey(key)) return 0;
            long n = AsLong(o[key]);
            if (n < 0 || n > 10000) return 0;
            return (int)n;
        }

        static long Long(JsonObject o, string key) => o != null && o.ContainsKey(key) ? AsLong(o[key]) : 0;

        static long AsLong(object v)
        {
            if (v is long l) return l;
            if (v is int i) return i;
            if (v is double d && !double.IsNaN(d) && !double.IsInfinity(d))
                return (long)Math.Round(d, MidpointRounding.AwayFromZero);
            return 0;
        }
    }

    /// <summary>One player's credits. Persisted on <see cref="NativeStore.CreditsKey"/> when a store is attached.</summary>
    public sealed class CreditsWallet
    {
        public readonly CreditsTable Table;
        public long Balance;
        public long PaidQuote;
        public long Gifted;
        public string LastMissionId;
        public bool HasRecord { get; private set; }

        readonly NativeStore store;
        readonly HashSet<string> firstClears = new HashSet<string>();
        readonly HashSet<string> medals = new HashSet<string>();

        CreditsWallet(CreditsTable table, NativeStore store)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            this.store = store;
            Balance = table.Starting;
        }

        public static CreditsWallet Load(NativeStore store, CreditsTable table)
        {
            var wallet = new CreditsWallet(table, store);
            if (store == null) return wallet;
            var env = store.Read(NativeStore.CreditsKey);
            var data = env != null && env.Str("kind") == "credits" ? env.Obj("data") : null;
            if (data == null) return wallet;
            wallet.HasRecord = true;
            wallet.Balance = NonNeg(AsLong(data, "balance"));
            wallet.PaidQuote = NonNeg(AsLong(data, "paidQuote"));
            wallet.Gifted = NonNeg(AsLong(data, "gifted"));
            if (wallet.Gifted > wallet.PaidQuote) wallet.Gifted = wallet.PaidQuote;
            wallet.LastMissionId = data.Str("lastMissionId");
            ReadSet(data.Arr("firstClears"), wallet.firstClears);
            ReadSet(data.Arr("medals"), wallet.medals);
            return wallet;
        }

        /// <summary>
        /// The rocket already on the pad was not bought with credits. Later refunds stop at that gift,
        /// so stripping it does not print money. A quote of 0 (the Hopper) gifts nothing.
        /// </summary>
        public void GiftBaseline(long quote)
        {
            if (quote < 0) quote = 0;
            PaidQuote = quote;
            Gifted = quote;
        }

        /// <summary>Move the paid quote onto the rocket that is open. A gap the balance cannot cover is gifted, not deleted.</summary>
        public void CatchUp(long quote)
        {
            if (quote < 0) quote = 0;
            if (TryApply(quote)) return;
            long gap = quote - PaidQuote;
            if (gap <= 0) return;
            Gifted += gap;
            PaidQuote = quote;
            Save();
        }

        /// <summary>Pay the difference, or refund the part of it the player actually bought. A quote of 0 always fits.</summary>
        public bool TryApply(long newQuote)
        {
            if (newQuote < 0) newQuote = 0;
            if (newQuote == PaidQuote) return true;
            long delta = newQuote - PaidQuote;
            if (delta > 0)
            {
                if (Balance < delta) return false;
                Balance -= delta;
                PaidQuote = newQuote;
                Save();
                return true;
            }
            long drop = -delta;
            long realPaid = PaidQuote - Gifted;
            if (realPaid < 0) realPaid = 0;
            long refund = drop < realPaid ? drop : realPaid;
            long giftDrop = drop - refund;
            if (giftDrop > Gifted) giftDrop = Gifted;
            Gifted -= giftDrop;
            Balance += refund;
            PaidQuote = newQuote;
            Save();
            return true;
        }

        /// <summary>A pass pays the mission, once for a first clear, and once per new medal. A miss pays nothing.</summary>
        public long Award(string missionId, int index, bool passed, IReadOnlyList<string> medalIds)
        {
            if (!passed || string.IsNullOrEmpty(missionId) || index < 1 || Table == null) return 0;
            long pay = Table.MissionClear(index);
            if (firstClears.Add(missionId)) pay += Table.FirstClear;
            if (medalIds != null)
            {
                for (int i = 0; i < medalIds.Count; i++)
                {
                    string medal = medalIds[i];
                    if (string.IsNullOrEmpty(medal)) continue;
                    if (medals.Add(missionId + "/" + medal)) pay += Table.Medal;
                }
            }
            if (pay < 0) pay = 0;
            Balance += pay;
            LastMissionId = missionId;
            Save();
            return pay;
        }

        public void Save()
        {
            if (store == null || Table == null) return;
            var data = new JsonObject();
            data.Add("balance", Balance);
            data.Add("paidQuote", PaidQuote);
            data.Add("gifted", Gifted);
            data.Add("lastMissionId", string.IsNullOrEmpty(LastMissionId) ? "" : LastMissionId);
            data.Add("firstClears", Strings(firstClears));
            data.Add("medals", Strings(medals));
            store.PutRecord(NativeStore.CreditsKey, "credits", data);
            HasRecord = true;
        }

        static List<object> Strings(HashSet<string> set)
        {
            var list = new List<string>(set);
            list.Sort(StringComparer.Ordinal);
            var arr = new List<object>(list.Count);
            for (int i = 0; i < list.Count; i++) arr.Add(list[i]);
            return arr;
        }

        static void ReadSet(List<object> arr, HashSet<string> into)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Count; i++)
                if (arr[i] is string s && s.Length > 0) into.Add(s);
        }

        static long NonNeg(long n) => n < 0 ? 0 : n;

        static long AsLong(JsonObject o, string key)
        {
            if (o == null || !o.ContainsKey(key)) return 0;
            var v = o[key];
            if (v is long l) return l;
            if (v is int i) return i;
            if (v is double d && !double.IsNaN(d) && !double.IsInfinity(d))
                return (long)Math.Round(d, MidpointRounding.AwayFromZero);
            return 0;
        }
    }

    public static class CreditsText
    {
        public static string Chip(long credits)
        {
            if (credits < 0) credits = 0;
            if (credits > 9999) return "9999+ cr";
            if (credits == 1) return "1 credit";
            return credits.ToString(CultureInfo.InvariantCulture) + " credits";
        }
    }
}
