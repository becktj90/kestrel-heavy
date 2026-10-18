// Player copy for the featured mission card. Numbers come from the campaign row.
// Raw ids (payload, form, band, kh-m) stay in the data.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kestrel.Core;

namespace Kestrel.Core.Flight
{
    public enum MissionBoardAccess { Locked, Open, Cleared }

    public sealed class MissionBoardFacts
    {
        public string Title = "";
        public string Customer = "";
        public string Orbit = "";
        public string Mass = "";
        public string Need = "";
        public string Difficulty = "";
        public string Best = "";
        public string Briefing = "";
        public string Primary = "";
        public string PrimaryTip = "";
        public string Form = "";
        public string Node = "";
        public string Medal = "";
        public string Lock = "";
        public bool CanGo;
        public MissionBoardAccess Access;

        public IEnumerable<string> Visible()
        {
            if (Title.Length > 0) yield return Title;
            if (Customer.Length > 0) yield return Customer;
            if (Orbit.Length > 0) yield return Orbit;
            if (Mass.Length > 0) yield return Mass;
            if (Need.Length > 0) yield return Need;
            if (Difficulty.Length > 0) yield return Difficulty;
            if (Best.Length > 0) yield return Best;
            if (Briefing.Length > 0) yield return Briefing;
            if (Primary.Length > 0) yield return Primary;
            if (PrimaryTip.Length > 0) yield return PrimaryTip;
            if (Node.Length > 0) yield return Node;
            if (Medal.Length > 0) yield return Medal;
            if (Lock.Length > 0) yield return Lock;
        }

        public static MissionBoardFacts Of(CampaignMission mission, MissionBoardAccess access, int stars, IReadOnlyList<string> medals, double score, int attempts, bool canLaunch)
        {
            var card = new MissionBoardFacts { Access = access };
            if (mission == null)
            {
                card.Title = "Missions";
                card.Briefing = "The campaign file has no missions yet.";
                card.Primary = "Locked";
                card.PrimaryTip = card.Briefing;
                return card;
            }

            string code = MissionContext.LabelFor(mission.Index);
            card.Title = JoinTitle(code, mission.Name);
            card.Node = card.Title;
            card.Customer = Label("Customer", Clean(mission.Customer));
            card.Orbit = OrbitLine(mission);
            card.Mass = Label("Mass", Kilograms(mission.MassKg));
            card.Need = Label("Need", Speed(mission.RequiredDeltaV()));
            card.Difficulty = Label("Difficulty", DifficultyWord(mission.Band));
            card.Best = BestLine(stars, medals, score, attempts);
            card.Medal = BestMedal(medals);
            string goal = PlayerGoal(mission.Goal);
            card.Briefing = goal.Length > 0 ? goal : OneSentence(mission.Control);
            card.Form = PayloadForms.KnownForm(mission.Form) ? mission.Form : "box";
            if (access == MissionBoardAccess.Locked)
            {
                card.Lock = mission.Index <= 1
                    ? "Opens at the start of the board."
                    : "Clear " + MissionContext.LabelFor(mission.Index - 1) + " to unlock";
                card.Primary = mission.Index <= 1 ? "Locked" : card.Lock;
                card.PrimaryTip = card.Lock;
                card.CanGo = false;
            }
            else if (canLaunch)
            {
                card.Primary = "Fly " + code;
                card.PrimaryTip = access == MissionBoardAccess.Cleared ? "Replay this flight." : "Fly this one.";
                card.CanGo = true;
            }
            else
            {
                card.Primary = "Design first";
                card.PrimaryTip = "Open the designer with this load.";
                card.CanGo = true;
            }
            return card;
        }

        public static MissionBoardFacts Sandbox()
        {
            return new MissionBoardFacts
            {
                Title = "Sandbox",
                Customer = "Customer · Hangar Desk",
                Orbit = "Any orbit you can reach.",
                Mass = "Mass · yours to set",
                Difficulty = "Difficulty · Free",
                Best = "Best · not scored",
                Briefing = "A free build on this device. No mission and no score.",
                Primary = "Design first",
                PrimaryTip = "Open the designer with an empty rocket.",
                Node = "Sandbox",
                CanGo = true,
                Access = MissionBoardAccess.Open,
            };
        }

        public static MissionBoardFacts Contracts(bool open, string body, int afterCleared)
        {
            string lockLine = "Opens after " + MissionContext.LabelFor(afterCleared) + ".";
            string copy = open ? OneSentence(body) : lockLine;
            return new MissionBoardFacts
            {
                Title = "Open Contracts",
                Customer = "Customer · the board",
                Orbit = open ? "Seeded on this device." : "Posted with the job.",
                Mass = "Mass · posted with the job",
                Difficulty = "Difficulty · posted with the job",
                Best = "Best · not scored yet",
                Briefing = copy.Length > 0 ? copy : "No contract is posted yet.",
                Primary = "Not posted",
                PrimaryTip = open ? "No contract is posted yet." : lockLine,
                Lock = "",
                Node = "Contracts",
                CanGo = false,
                Access = open ? MissionBoardAccess.Open : MissionBoardAccess.Locked,
            };
        }

        public static string DifficultyWord(string band)
        {
            if (string.Equals(band, "hard", StringComparison.OrdinalIgnoreCase)) return "Hard";
            return "Steady";
        }

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
            return "";
        }

        public static string BestLine(int stars, IReadOnlyList<string> medals, double score, int attempts)
        {
            string medal = Medals(medals);
            bool flown = attempts > 0 || stars > 0 || medal.Length > 0 || (score > 0 && !double.IsNaN(score));
            if (!flown) return "Best · no flight yet";
            var parts = new List<string>();
            if (score > 0 && !double.IsNaN(score) && !double.IsInfinity(score))
                parts.Add(Number(score));
            if (stars == 1) parts.Add("1 star");
            else if (stars > 1) parts.Add(stars.ToString(CultureInfo.InvariantCulture) + " stars");
            else parts.Add("no stars");
            if (medal.Length > 0) parts.Add(medal);
            return "Best · " + string.Join(" · ", parts);
        }

        public static string BestMedal(IReadOnlyList<string> medals)
        {
            if (medals == null || medals.Count == 0) return "";
            string best = "";
            int rank = 0;
            for (int i = 0; i < medals.Count; i++)
            {
                int r = MedalShelf.Rank(medals[i]);
                if (r > rank)
                {
                    rank = r;
                    best = MedalWord(medals[i]);
                }
            }
            if (best.Length > 0) return best;
            return MedalWord(medals[0]);
        }

        public static string Medals(IReadOnlyList<string> medals)
        {
            if (medals == null || medals.Count == 0) return "";
            var seen = new HashSet<string>();
            var parts = new List<string>();
            for (int i = 0; i < medals.Count; i++)
            {
                string word = MedalWord(medals[i]);
                if (word.Length == 0 || !seen.Add(word)) continue;
                parts.Add(word);
            }
            return string.Join(" · ", parts);
        }

        static string MedalWord(string medal)
        {
            if (string.IsNullOrWhiteSpace(medal)) return "";
            if (medal == "gold") return "Gold";
            if (medal == "silver") return "Silver";
            if (medal == "bronze") return "Bronze";
            var sb = new StringBuilder();
            bool cap = true;
            for (int i = 0; i < medal.Length; i++)
            {
                char c = medal[i];
                if (c == '-' || c == '_' || c == ' ')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' ');
                    cap = true;
                    continue;
                }
                if (c < 32 || c > 126) continue;
                sb.Append(cap ? char.ToUpperInvariant(c) : c);
                cap = false;
            }
            return sb.ToString().Trim();
        }

        static string JoinTitle(string label, string name)
        {
            if (string.IsNullOrEmpty(label)) return Clean(name) ?? "";
            if (string.IsNullOrEmpty(name) || name == label) return label;
            return label + "  " + name.Trim();
        }

        static string Label(string caption, string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return caption + " · " + value;
        }

        static string PlayerGoal(string goal)
        {
            string line = OneSentence(goal);
            if (line.Length == 0) return "";
            // The form id is data. The card says what the player is flying.
            return line.Replace("cubesat", "satellite").Replace("Cubesat", "Satellite");
        }

        static string OneSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var sb = new StringBuilder(text.Length);
            bool space = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || c == '\r' || c == '\t' || c == ' ')
                {
                    space = sb.Length > 0;
                    continue;
                }
                if (space) { sb.Append(' '); space = false; }
                sb.Append(c);
            }
            string flat = sb.ToString().Trim();
            if (PlayerText.Leak(flat)) flat = PlayerText.ForPlayer(flat);
            return flat;
        }

        static string WithInc(string body, double inc)
        {
            if (string.IsNullOrEmpty(body) || Math.Abs(inc) < 1e-6) return body;
            return body + ", " + Number(inc) + "°";
        }

        public static string Speed(double mps)
        {
            if (double.IsNaN(mps) || double.IsInfinity(mps) || mps <= 0) return "";
            return Number(mps) + " m/s";
        }

        public static string Kilograms(double kg)
        {
            if (double.IsNaN(kg) || double.IsInfinity(kg) || kg <= 0) return "";
            return Number(kg) + " kg";
        }

        static string Kilometers(double km)
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

        static string Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
