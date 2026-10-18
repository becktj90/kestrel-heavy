// Career and per-mission records on the v1 save store.
// Keys: kh.save.v1.career and kh.save.v1.missions. The envelope schema stays 1.
// kh.save.v1.progress is read only when it already holds a career profile, and it is never rewritten.
// A corrupt career or missions file falls back and is kept as .bak. No timers, no ads, no network.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Save;

namespace Kestrel.Core.Economy
{
    /// <summary>Best result and the latest attempt for one mission id (m1, kc3, …).</summary>
    public sealed class MissionRecord
    {
        public int BestStars;
        public double BestScore;
        public readonly List<string> BestMedals = new List<string>();
        public int Attempts;
        public long? LastSeed;
        public string LastDesignId;
    }

    /// <summary>
    /// Loads and saves <see cref="Career"/> plus one <see cref="MissionRecord"/> per mission.
    /// Existing design and progress files are left byte-for-byte alone.
    /// </summary>
    public sealed class CareerStore
    {
        public const string CareerKey = NativeStore.CareerKey;
        public const string MissionsKey = NativeStore.MissionsKey;
        /// <summary>JSON integers stay exact through MiniJson only inside this range.</summary>
        public const long SafeInteger = 9007199254740991L;

        public readonly Career Career;
        public int ContractsCleared;
        public bool LoanUsed;
        /// <summary>True when this load copied a profile out of progress into the new keys.</summary>
        public bool MigratedFromProgress { get; private set; }

        readonly NativeStore store;
        readonly Campaign campaign;
        readonly long minorPerKc;
        readonly Dictionary<string, MissionRecord> missions = new Dictionary<string, MissionRecord>();

        public IReadOnlyDictionary<string, MissionRecord> Missions => missions;

        CareerStore(NativeStore store, Campaign campaign, long minorPerKc)
        {
            this.store = store;
            this.campaign = campaign;
            this.minorPerKc = minorPerKc;
            Career = Career.Start(campaign, minorPerKc);
        }

        public static CareerStore Load(NativeStore store, Campaign campaign, long minorPerKc)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            var career = new CareerStore(store, campaign, minorPerKc);
            career.Read();
            return career;
        }

        public MissionRecord Mission(string missionId)
            => missionId != null && missions.TryGetValue(missionId, out var row) ? row : null;

        /// <summary>
        /// Record one flight. Stars win, then score. A tie keeps the earlier medals.
        /// Attempts, last seed, and last design always follow this flight. Then both keys are saved.
        /// </summary>
        public MissionRecord RecordAttempt(string missionId, int stars, double score, IReadOnlyList<string> medals, long? seed, string designId)
        {
            if (!ValidMissionId(missionId)) throw new ArgumentException("invalid mission id", nameof(missionId));
            if (stars < 0 || stars > 3) throw new ArgumentOutOfRangeException(nameof(stars));
            if (double.IsNaN(score) || double.IsInfinity(score)) throw new ArgumentException("score must be finite", nameof(score));
            if (seed.HasValue && !IsSafe(seed.Value)) throw new ArgumentOutOfRangeException(nameof(seed));
            if (designId != null && designId.Length > NativeStore.MaxKeyLength)
                throw new ArgumentException("design id is too long", nameof(designId));

            if (!missions.TryGetValue(missionId, out var rec))
            {
                rec = new MissionRecord();
                missions[missionId] = rec;
            }
            bool first = rec.Attempts == 0;
            rec.Attempts++;
            rec.LastSeed = seed;
            rec.LastDesignId = string.IsNullOrEmpty(designId) ? null : designId;
            if (first || stars > rec.BestStars || (stars == rec.BestStars && score > rec.BestScore))
            {
                rec.BestStars = stars;
                rec.BestScore = score;
                rec.BestMedals.Clear();
                if (medals != null)
                {
                    for (int i = 0; i < medals.Count; i++)
                    {
                        string medal = medals[i];
                        if (string.IsNullOrEmpty(medal) || rec.BestMedals.Contains(medal)) continue;
                        rec.BestMedals.Add(medal);
                    }
                }
            }
            Save();
            return rec;
        }

        /// <summary>
        /// One hangar loan. When the bank is below the cheapest viable rocket, top it up to the
        /// starting bank and remember loanUsed. A second call does nothing. No timer and no ad.
        /// </summary>
        public bool TryHangarLoan(long cheapestViableMinor)
        {
            if (cheapestViableMinor < 0) throw new ArgumentOutOfRangeException(nameof(cheapestViableMinor));
            if (LoanUsed || Career.BankMinor >= cheapestViableMinor) return false;
            long start = Career.KcToMinor(campaign.StartingBankKc, minorPerKc);
            // The loan only tops the bank up. It never takes Kc away, and it stays available until it pays.
            if (start <= Career.BankMinor) return false;
            Career.BankMinor = start;
            LoanUsed = true;
            Save();
            return true;
        }

        /// <summary>Write career and missions. Schema on both envelopes stays 1. Progress is not written.</summary>
        public void Save()
        {
            store.PutRecord(CareerKey, "career", WriteCareer());
            store.PutRecord(MissionsKey, "missions", WriteMissions());
        }

        enum Slot { Missing, Ok, Corrupt }

        void Read()
        {
            var careerSlot = ReadSlot(CareerKey, "career", out var careerData, out int careerSchema);
            var missionSlot = ReadSlot(MissionsKey, "missions", out var missionData, out int missionSchema);
            JsonObject profile = null;
            if (careerSlot == Slot.Missing || missionSlot == Slot.Missing)
                profile = ProfileOf(store.GetProgress());

            bool writeCareer = false, writeMissions = false;
            if (careerSlot == Slot.Ok)
            {
                // A pre-schema career file is rewritten. When it lists no unlocks, rebuild the
                // parts those cleared missions awarded, the same way a progress profile does.
                bool migrateSchema = careerSchema < NativeStore.Schema;
                ApplyCareer(careerData, migrateSchema);
                if (migrateSchema) writeCareer = true;
            }
            else if (careerSlot == Slot.Missing && profile != null)
            {
                ApplyCareer(profile, true);
                writeCareer = true;
            }

            if (missionSlot == Slot.Ok)
            {
                ReadMissionMap(missionData);
                if (missionSchema < NativeStore.Schema) writeMissions = true;
            }
            else if (missionSlot == Slot.Missing && profile != null && profile.Obj("missions") != null)
            {
                ReadMissionMap(profile.Obj("missions"));
                writeMissions = true;
            }

            // Keep the pre-schema file once, then rewrite the current envelope.
            if (writeCareer && careerSlot == Slot.Ok && careerSchema < NativeStore.Schema) store.KeepBackup(CareerKey);
            if (writeMissions && missionSlot == Slot.Ok && missionSchema < NativeStore.Schema) store.KeepBackup(MissionsKey);
            if (writeCareer) store.PutRecord(CareerKey, "career", WriteCareer());
            if (writeMissions) store.PutRecord(MissionsKey, "missions", WriteMissions());
            MigratedFromProgress = writeCareer || writeMissions;
        }

        Slot ReadSlot(string key, string kind, out JsonObject data, out int schema)
        {
            data = null;
            schema = NativeStore.Schema;
            bool bytes = store.HasBytes(key);
            store.BackupCorruptMain(key);
            var env = store.Read(key);
            if (env == null) return bytes ? Slot.Corrupt : Slot.Missing;
            if (!TryEnvelope(env, kind, out schema, out data))
            {
                store.KeepBackup(key);
                return Slot.Corrupt;
            }
            return Slot.Ok;
        }

        /// <summary>
        /// Schema 1 is current. Schema 0, or a career blob with no envelope, is the older file and is rewritten.
        /// A newer schema is left untouched and treated as corrupt so a future format is not guessed.
        /// </summary>
        static bool TryEnvelope(JsonObject env, string kind, out int schema, out JsonObject data)
        {
            schema = 0;
            data = null;
            if (env.ContainsKey("schema") && env["schema"] != null)
            {
                if (!TryInt(env["schema"], out schema)) return false;
            }
            if (schema < 0 || schema > NativeStore.Schema) return false;
            string fileKind = env.Str("kind");
            if (fileKind == kind)
            {
                if (env.ContainsKey("data") && env["data"] != null && !(env["data"] is JsonObject)) return false;
                data = env.Obj("data") ?? new JsonObject();
                return true;
            }
            if (kind == "career" && string.IsNullOrEmpty(fileKind) && schema == 0 && HasCareerKeys(env))
            {
                data = env;
                return true;
            }
            return false;
        }

        void ApplyCareer(JsonObject data, bool fillUnlocksFromCleared)
        {
            if (data == null) return;
            if (TryLong(data.ContainsKey("bankMinor") ? data["bankMinor"] : null, out long bank))
                Career.BankMinor = bank;
            bool clearedSet = TryInt(data.ContainsKey("cleared") ? data["cleared"] : null, out int cleared) && cleared >= 0;
            if (clearedSet) Career.Cleared = cleared;
            if (data.ContainsKey("unlocks") && data["unlocks"] is List<object> list)
            {
                Career.Unlocks.Clear();
                for (int i = 0; i < list.Count; i++)
                    if (list[i] is string id && id.Length > 0) Career.Unlocks.Add(id);
            }
            else if (fillUnlocksFromCleared && clearedSet && cleared > 0 && !data.ContainsKey("unlocks"))
            {
                int n = cleared;
                if (n > campaign.Missions.Count) n = campaign.Missions.Count;
                for (int i = 1; i <= n; i++)
                {
                    var mission = campaign.Mission(i);
                    if (mission == null) continue;
                    for (int u = 0; u < mission.Unlocks.Count; u++) Career.Unlocks.Add(mission.Unlocks[u]);
                }
            }
            var flights = data.Obj("articleFlights");
            if (flights != null)
            {
                Career.ArticleFlights.Clear();
                foreach (var kv in flights)
                {
                    if (kv.Key.Length == 0 || !TryInt(kv.Value, out int count) || count < 0) continue;
                    Career.ArticleFlights[kv.Key] = count;
                }
            }
            if (TryInt(data.ContainsKey("contractsCleared") ? data["contractsCleared"] : null, out int contracts) && contracts >= 0)
                ContractsCleared = contracts;
            if (data.ContainsKey("loanUsed") && data["loanUsed"] is bool loan) LoanUsed = loan;
        }

        void ReadMissionMap(JsonObject map)
        {
            if (map == null) return;
            foreach (var kv in map)
            {
                if (!ValidMissionId(kv.Key) || !(kv.Value is JsonObject row)) continue;
                missions[kv.Key] = ReadMission(row);
            }
        }

        static MissionRecord ReadMission(JsonObject row)
        {
            var rec = new MissionRecord();
            if (TryInt(row.ContainsKey("bestStars") ? row["bestStars"] : null, out int stars) && stars >= 0 && stars <= 3)
                rec.BestStars = stars;
            if (row.ContainsKey("bestScore") && row["bestScore"] is double score && !double.IsNaN(score) && !double.IsInfinity(score))
                rec.BestScore = score;
            var medals = row.Arr("bestMedals");
            if (medals != null)
            {
                for (int i = 0; i < medals.Count; i++)
                {
                    if (medals[i] is string medal && medal.Length > 0 && !rec.BestMedals.Contains(medal))
                        rec.BestMedals.Add(medal);
                }
            }
            if (TryInt(row.ContainsKey("attempts") ? row["attempts"] : null, out int attempts) && attempts >= 0)
                rec.Attempts = attempts;
            if (row.ContainsKey("lastSeed") && row["lastSeed"] != null && TryLong(row["lastSeed"], out long seed) && IsSafe(seed))
                rec.LastSeed = seed;
            string design = row.Str("lastDesignId");
            rec.LastDesignId = string.IsNullOrEmpty(design) ? null : design;
            return rec;
        }

        JsonObject WriteCareer()
        {
            var data = new JsonObject();
            data.Add("bankMinor", Career.BankMinor);
            data.Add("cleared", Career.Cleared);
            var unlocks = new List<string>(Career.Unlocks);
            unlocks.Sort(StringComparer.Ordinal);
            var arr = new List<object>(unlocks.Count);
            for (int i = 0; i < unlocks.Count; i++) arr.Add(unlocks[i]);
            data.Add("unlocks", arr);
            var flights = new JsonObject();
            var keys = new List<string>(Career.ArticleFlights.Keys);
            keys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < keys.Count; i++) flights.Add(keys[i], Career.ArticleFlights[keys[i]]);
            data.Add("articleFlights", flights);
            data.Add("contractsCleared", ContractsCleared < 0 ? 0 : ContractsCleared);
            data.Add("loanUsed", LoanUsed);
            return data;
        }

        JsonObject WriteMissions()
        {
            var data = new JsonObject();
            var ids = new List<string>(missions.Keys);
            ids.Sort(StringComparer.Ordinal);
            for (int i = 0; i < ids.Count; i++)
            {
                var rec = missions[ids[i]];
                var row = new JsonObject();
                row.Add("bestStars", rec.BestStars);
                row.Add("bestScore", rec.BestScore);
                var medals = new List<object>(rec.BestMedals.Count);
                for (int m = 0; m < rec.BestMedals.Count; m++) medals.Add(rec.BestMedals[m]);
                row.Add("bestMedals", medals);
                row.Add("attempts", rec.Attempts);
                row.Add("lastSeed", rec.LastSeed.HasValue ? (object)rec.LastSeed.Value : null);
                row.Add("lastDesignId", rec.LastDesignId);
                data.Add(ids[i], row);
            }
            return data;
        }

        /// <summary>
        /// Progress holds a profile when the career fields sit on it, or under "profile" / "career".
        /// Designer fields such as orbitsCompleted and engineerEnabled are not a profile.
        /// Top-level career fields win over the nested object.
        /// </summary>
        static JsonObject ProfileOf(JsonObject progress)
        {
            if (progress == null) return null;
            JsonObject nested = progress.Obj("profile") ?? progress.Obj("career");
            bool top = HasCareerKeys(progress);
            bool nest = HasCareerKeys(nested);
            if (!top && !nest) return null;
            if (!nest) return progress;
            if (!top) return nested;
            var merged = JsonMut.CloneObject(nested);
            CopyCareerKey(progress, merged, "bankMinor");
            CopyCareerKey(progress, merged, "cleared");
            CopyCareerKey(progress, merged, "unlocks");
            CopyCareerKey(progress, merged, "articleFlights");
            CopyCareerKey(progress, merged, "contractsCleared");
            CopyCareerKey(progress, merged, "loanUsed");
            CopyCareerKey(progress, merged, "missions");
            return merged;
        }

        static void CopyCareerKey(JsonObject from, JsonObject to, string key)
        {
            if (from.ContainsKey(key)) JsonMut.Set(to, key, JsonMut.Clone(from[key]));
        }

        static bool HasCareerKeys(JsonObject o)
        {
            if (o == null) return false;
            if (o.ContainsKey("bankMinor") || o.ContainsKey("cleared") || o.ContainsKey("unlocks")
                || o.ContainsKey("articleFlights") || o.ContainsKey("contractsCleared") || o.ContainsKey("loanUsed"))
                return true;
            return o.Obj("missions") != null;
        }

        public static bool ValidMissionId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            foreach (char c in id)
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-'))
                    return false;
            return true;
        }

        static bool IsSafe(long n) => n >= -SafeInteger && n <= SafeInteger;

        static bool TryInt(object v, out int n)
        {
            n = 0;
            if (!TryLong(v, out long wide) || wide < int.MinValue || wide > int.MaxValue) return false;
            n = (int)wide;
            return true;
        }

        static bool TryLong(object v, out long n)
        {
            n = 0;
            if (v is long l) { n = l; return true; }
            if (v is int i) { n = i; return true; }
            if (!(v is double d) || !JsonMut.IsInteger(d) || d < long.MinValue || d > long.MaxValue) return false;
            n = (long)d;
            return true;
        }
    }
}
