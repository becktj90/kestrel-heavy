// Campaign map for Mission Select. Four acts, soft-linear locks from Career.Cleared,
// part lines from Career.Unlocks, stars and medals only from the missions record.
// Open Contracts stays off the board until KH-10 is cleared. No timers, no prices, no network.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Save;

namespace Kestrel.Game.MissionSelect
{
    public enum MissionAccess { Locked, Open, Cleared }

    public sealed class PayloadLabel
    {
        public string Name;
        public string Kind;
    }

    public sealed class MissionMapEntry
    {
        public string ElementName;
        public string MissionId;
        public int Index;
        public string ActId;
        public string Title;
        public string SpokenTitle;
        public MissionAccess Access;
        public bool IsNext;
        public string PayloadLine;
        public string OrbitLine;
        public string StarsText;
        public string MedalsText;
        public string PartsLine;
        public string LockLine;
        public string Tooltip;
    }

    public sealed class MissionAct
    {
        public string Id;
        public string Title;
        public readonly List<MissionMapEntry> Entries = new List<MissionMapEntry>();
    }

    public sealed class MissionMap
    {
        public readonly List<MissionAct> Acts = new List<MissionAct>();
        public bool Empty;
        public bool ShowOpenContracts;
        public string Note;
        public MissionMapEntry Next;
    }

    /// <summary>One read of the career save. A missing folder is a fresh career and is not created.</summary>
    public sealed class MissionSnapshot
    {
        public Career Career;
        public readonly Dictionary<string, MissionRecord> Records = new Dictionary<string, MissionRecord>();
    }

    public static class MissionMapCopy
    {
        public const string Title = "Missions";
        public const string Home = "Home";
        public const string HomeTip = "Back to the title screen.";
        public const string Empty = "The campaign file has no missions yet.";
        public const string NextChip = "Next";
        public const string Fly = "Fly this one.";
        public const string Replay = "Replay.";
        public const string BoardClear = "The board is clear.";
        public const string ContractsTitle = "Open Contracts";
        public const string ContractsBody = "KH-11 and up. Seeded on this device. No clock and no expiry. No contract is posted yet.";
        /// <summary>KH-10 is mission index 10. Career.Cleared reaches 10 when that flight passes.</summary>
        public const int ContractsAfterCleared = 10;
        public const long MinorPerKcFallback = 100000;
    }

    public static class MissionActs
    {
        public static readonly string[] Ids = { "act-1", "act-2", "act-3", "act-4" };
        public static readonly string[] Titles =
        {
            "Act I · Up and Down",
            "Act II · Orbit",
            "Act III · Come Back",
            "Act IV · Beyond",
        };

        public static int IndexFor(int missionIndex)
        {
            if (missionIndex <= 2) return 0;
            if (missionIndex <= 5) return 1;
            if (missionIndex <= 7) return 2;
            return 3;
        }
    }

    public static class MissionMapText
    {
        public static long MinorPerKc(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return MissionMapCopy.MinorPerKcFallback;
            try
            {
                double n = MiniJson.ParseObject(json).Num("minorPerKc");
                if (double.IsNaN(n) || double.IsInfinity(n) || n < 1 || n > long.MaxValue) return MissionMapCopy.MinorPerKcFallback;
                return (long)Math.Round(n, MidpointRounding.AwayFromZero);
            }
            catch (Exception) { return MissionMapCopy.MinorPerKcFallback; }
        }

        public static Dictionary<string, PayloadLabel> ReadPayloadLabels(string json)
        {
            var map = new Dictionary<string, PayloadLabel>();
            if (string.IsNullOrWhiteSpace(json)) return map;
            JsonObject root;
            try { root = MiniJson.ParseObject(json); }
            catch (Exception) { return map; }
            var missions = root.Arr("missions");
            if (missions == null) return map;
            for (int i = 0; i < missions.Count; i++)
            {
                if (!(missions[i] is JsonObject row)) continue;
                string id = Clean(row.Str("id"));
                if (id == null) continue;
                var brief = row.Obj("briefing");
                if (brief == null) continue;
                map[id] = new PayloadLabel { Name = Clean(brief.Str("payloadName")), Kind = Clean(brief.Str("payloadKind")) };
            }
            return map;
        }

        public static string PayloadLine(CampaignMission mission, PayloadLabel label)
        {
            if (mission == null) return "";
            string name = label != null ? Clean(label.Name) : null;
            string kind = label != null ? Clean(label.Kind) : null;
            if (name == null) name = Clean(mission.PayloadId);
            if (kind == name) kind = null;
            string mass = Kilograms(mission.MassKg);
            var parts = new List<string>();
            if (name != null) parts.Add(name);
            if (kind != null) parts.Add(kind);
            if (mass.Length > 0) parts.Add(mass);
            return string.Join(" · ", parts);
        }

        /// <summary>The contract target. A hop is an apogee, not an orbit. Negative C3 is not called an escape.</summary>
        public static string OrbitLine(CampaignMission mission)
        {
            if (mission == null || string.IsNullOrEmpty(mission.TargetKind)) return "";
            string kind = mission.TargetKind;
            if (kind.Equals("apogee", StringComparison.OrdinalIgnoreCase))
                return WithInc("Apogee " + Kilometers(mission.ApoKm), mission.IncDeg);
            if (kind.Equals("circular", StringComparison.OrdinalIgnoreCase))
            {
                bool split = mission.PeriKm > 0 && mission.ApoKm > 0 && Math.Abs(mission.ApoKm - mission.PeriKm) > 0.05;
                string body = split
                    ? "Circular orbit, apogee " + Kilometers(mission.ApoKm) + ", perigee " + Kilometers(mission.PeriKm)
                    : "Circular orbit " + Kilometers(mission.ApoKm > 0 ? mission.ApoKm : mission.PeriKm);
                return WithInc(body, mission.IncDeg);
            }
            if (kind.Equals("ellipse", StringComparison.OrdinalIgnoreCase))
                return WithInc("Ellipse " + Kilometers(mission.PeriKm) + " by " + Kilometers(mission.ApoKm), mission.IncDeg);
            if (kind.Equals("c3", StringComparison.OrdinalIgnoreCase))
            {
                string body = "C3 " + Number(mission.C3Km2PerS2) + " km2/s2";
                if (mission.PeriKm > 0) body += ", closest point " + Kilometers(mission.PeriKm);
                return WithInc(body, mission.IncDeg);
            }
            return kind;
        }

        public static string Stars(int stars)
        {
            if (stars == 1) return "1 star";
            if (stars == 2) return "2 stars";
            if (stars == 3) return "3 stars";
            return null;
        }

        public static string Medals(IReadOnlyList<string> medals)
        {
            if (medals == null || medals.Count == 0) return null;
            var seen = new HashSet<string>();
            var parts = new List<string>();
            for (int i = 0; i < medals.Count; i++)
            {
                string word = MedalWord(medals[i]);
                if (word.Length == 0 || !seen.Add(word)) continue;
                parts.Add(word);
            }
            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        public static string PartsLine(MissionAccess access, IReadOnlyList<string> unlocks, HashSet<string> owned)
        {
            if (access == MissionAccess.Locked || unlocks == null || unlocks.Count == 0) return null;
            var ids = new List<string>();
            for (int i = 0; i < unlocks.Count; i++)
            {
                string id = unlocks[i];
                if (string.IsNullOrEmpty(id)) continue;
                bool has = owned != null && owned.Contains(id);
                if (access == MissionAccess.Cleared) { if (has) ids.Add(id); }
                else if (!has) ids.Add(id);
            }
            if (ids.Count == 0) return null;
            string list = JoinIds(ids);
            return access == MissionAccess.Cleared ? "Opened " + list + "." : "A pass opens " + list + ".";
        }

        public static string LockLine(int index)
        {
            if (index <= 1) return "Opens at the start of the board.";
            return "Opens after " + MissionContext.LabelFor(index - 1) + ".";
        }

        public static string TitleLine(string label, string name)
        {
            if (string.IsNullOrEmpty(label)) return name ?? "";
            if (string.IsNullOrEmpty(name) || name == label) return label;
            return label + "  " + name;
        }

        public static string SpokenTitle(string label, string name)
        {
            if (string.IsNullOrEmpty(label)) return name ?? "";
            if (string.IsNullOrEmpty(name) || name == label) return label;
            return label + " " + name;
        }

        public static string Kilograms(double kg)
        {
            if (double.IsNaN(kg) || double.IsInfinity(kg)) return "";
            return Number(kg) + " kg";
        }

        public static string Kilometers(double km)
        {
            if (double.IsNaN(km) || double.IsInfinity(km)) return "";
            return Number(km) + " km";
        }

        public static string Number(double n)
        {
            if (double.IsNaN(n) || double.IsInfinity(n)) return "";
            double rounded = Math.Round(n, MidpointRounding.AwayFromZero);
            if (Math.Abs(n - rounded) < 1e-6 && Math.Abs(rounded) < 1e15)
                return ((long)rounded).ToString("N0", CultureInfo.InvariantCulture);
            return n.ToString("0.##", CultureInfo.InvariantCulture);
        }

        static string WithInc(string body, double inc)
        {
            if (string.IsNullOrEmpty(body) || Math.Abs(inc) < 1e-6) return body;
            return body + ", " + Number(inc) + "°";
        }

        static string MedalWord(string medal)
        {
            if (string.IsNullOrWhiteSpace(medal)) return "";
            if (medal == "gold") return "Gold";
            if (medal == "silver") return "Silver";
            if (medal == "bronze") return "Bronze";
            return medal.Trim();
        }

        static string JoinIds(List<string> ids)
        {
            if (ids.Count == 1) return ids[0];
            if (ids.Count == 2) return ids[0] + " and " + ids[1];
            var sb = new StringBuilder();
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) sb.Append(i == ids.Count - 1 ? ", and " : ", ");
                sb.Append(ids[i]);
            }
            return sb.ToString();
        }

        static string Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    public static class MissionProgress
    {
        public static MissionSnapshot Fresh(Campaign campaign, long minorPerKc)
        {
            var snap = new MissionSnapshot();
            if (campaign == null) return snap;
            long minor = minorPerKc < 1 ? MissionMapCopy.MinorPerKcFallback : minorPerKc;
            snap.Career = Career.Start(campaign, minor);
            return snap;
        }

        /// <summary>
        /// Read kh.save.v1.career and kh.save.v1.missions. Does not create the folder and does not write.
        /// A missing folder is a fresh career (KH-1 open, nothing earned).
        /// </summary>
        public static MissionSnapshot ReadSave(Campaign campaign, string saveDirectory, long minorPerKc)
        {
            if (campaign == null || string.IsNullOrEmpty(saveDirectory) || !Directory.Exists(saveDirectory))
                return Fresh(campaign, minorPerKc);
            var store = new NativeStore(saveDirectory, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => 0.5);
            var loaded = CareerStore.Load(store, campaign, minorPerKc < 1 ? MissionMapCopy.MinorPerKcFallback : minorPerKc);
            var snap = new MissionSnapshot { Career = loaded.Career };
            foreach (var kv in loaded.Missions) snap.Records[kv.Key] = kv.Value;
            return snap;
        }
    }

    public static class MissionMapBuilder
    {
        public static MissionMap From(Campaign campaign, Career career, IReadOnlyDictionary<string, MissionRecord> records, IReadOnlyDictionary<string, PayloadLabel> labels)
        {
            var map = new MissionMap();
            if (campaign == null || campaign.Missions == null || campaign.Missions.Count == 0)
            {
                map.Empty = true;
                map.Note = MissionMapCopy.Empty;
                return map;
            }

            int cleared = career == null || career.Cleared < 0 ? 0 : career.Cleared;
            var owned = career != null ? career.Unlocks : null;
            var ordered = new List<CampaignMission>(campaign.Missions);
            ordered.Sort((a, b) => a.Index.CompareTo(b.Index));
            var acts = new MissionAct[MissionActs.Ids.Length];
            for (int i = 0; i < ordered.Count; i++)
            {
                var mission = ordered[i];
                if (mission == null) continue;
                int act = MissionActs.IndexFor(mission.Index);
                if (acts[act] == null) acts[act] = new MissionAct { Id = MissionActs.Ids[act], Title = MissionActs.Titles[act] };
                var entry = Entry(mission, cleared, owned, records, labels, acts[act].Id);
                acts[act].Entries.Add(entry);
                if (entry.IsNext) map.Next = entry;
            }
            for (int i = 0; i < acts.Length; i++) if (acts[i] != null) map.Acts.Add(acts[i]);
            map.ShowOpenContracts = cleared >= MissionMapCopy.ContractsAfterCleared;
            if (map.Next != null) map.Note = "Next is " + map.Next.SpokenTitle + ".";
            else if (map.ShowOpenContracts) map.Note = MissionMapCopy.BoardClear;
            else map.Note = "Pick the next open flight, or replay one.";
            return map;
        }

        static MissionMapEntry Entry(CampaignMission mission, int cleared, HashSet<string> owned, IReadOnlyDictionary<string, MissionRecord> records, IReadOnlyDictionary<string, PayloadLabel> labels, string actId)
        {
            var access = AccessFor(mission.Index, cleared);
            PayloadLabel label = null;
            if (labels != null && mission.Id != null) labels.TryGetValue(mission.Id, out label);
            MissionRecord record = null;
            if (records != null && mission.Id != null) records.TryGetValue(mission.Id, out record);
            string stars = record == null ? null : MissionMapText.Stars(record.BestStars);
            string medals = record == null ? null : MissionMapText.Medals(record.BestMedals);
            string labelText = MissionContext.LabelFor(mission.Index);
            var entry = new MissionMapEntry
            {
                ElementName = "mission-" + (string.IsNullOrEmpty(mission.Id) ? "i" + mission.Index.ToString(CultureInfo.InvariantCulture) : mission.Id),
                MissionId = mission.Id,
                Index = mission.Index,
                ActId = actId,
                Title = MissionMapText.TitleLine(labelText, mission.Name),
                SpokenTitle = MissionMapText.SpokenTitle(labelText, mission.Name),
                Access = access,
                IsNext = access == MissionAccess.Open,
                PayloadLine = MissionMapText.PayloadLine(mission, label),
                OrbitLine = MissionMapText.OrbitLine(mission),
                StarsText = stars,
                MedalsText = medals,
                PartsLine = MissionMapText.PartsLine(access, mission.Unlocks, owned),
                LockLine = access == MissionAccess.Locked ? MissionMapText.LockLine(mission.Index) : null,
            };
            string gentle = mission.GentleDefault ? "Gentle on." : "Gentle off.";
            string action = access == MissionAccess.Locked ? entry.LockLine : access == MissionAccess.Cleared ? MissionMapCopy.Replay : MissionMapCopy.Fly;
            entry.Tooltip = Sentences(entry.SpokenTitle, entry.PayloadLine, entry.OrbitLine, gentle, stars, medals, action);
            return entry;
        }

        public static MissionAccess AccessFor(int index, int cleared)
        {
            if (index < 1) return MissionAccess.Locked;
            if (cleared < 0) cleared = 0;
            if (index <= cleared) return MissionAccess.Cleared;
            if (index == cleared + 1) return MissionAccess.Open;
            return MissionAccess.Locked;
        }

        static string Sentences(params string[] parts)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(parts[i]);
                if (!parts[i].EndsWith(".", StringComparison.Ordinal)) sb.Append('.');
            }
            return sb.ToString();
        }
    }
}
