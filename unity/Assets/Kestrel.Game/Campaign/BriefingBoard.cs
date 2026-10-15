// Mission briefing facts. Every figure comes from the campaign row or the S4 card.
// No UnityEngine. The screen only prints these lines.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;

namespace Kestrel.Game.MissionSelect
{
    public sealed class BriefingBoard
    {
        public string Title = "";
        public string Goal = "";
        public string Payload = "";
        public string Fit = "";
        public string Target = "";
        public readonly List<string> Requirements = new List<string>();
        public string Gentle = "";
        public readonly List<string> Bonuses = new List<string>();
        public string Budget = "";
        public string Reward = "";
        public string Wind = "";
        public string Time = "";
        public string Pad = "";
        public string Seed = "";
        public string Engineer = "";
        public bool CanBuild;

        /// <summary>
        /// Choosing a campaign flight stays on the mission map and opens this overlay.
        /// Build it is the existing route into the designer. A free build is left alone.
        /// </summary>
        public static RouteResult BeforeDesign(RouteResult chosen)
        {
            if (chosen.Screen == SessionScreens.Design && chosen.Session != null && !chosen.Session.IsFree)
                return new RouteResult(SessionScreens.Missions, SessionScreens.Briefing, chosen.Session);
            return chosen;
        }

        public static string SeedLabel(ulong seed) => (seed & 0xFFFFUL).ToString("X4");

        public static BriefingBoard Compose(MissionContext ctx, MissionBriefing card)
        {
            var board = new BriefingBoard();
            if (ctx == null && card == null)
            {
                board.Title = "Briefing";
                return board;
            }

            board.Title = TitleOf(ctx, card);
            board.Goal = First(ctx != null ? ctx.Goal : null, card != null ? card.Goal : null);
            board.Payload = PayloadOf(ctx, card);
            board.Fit = FitOf(ctx);
            board.Target = TargetOf(ctx);
            if (ctx != null)
            {
                for (int i = 0; i < ctx.Requirements.Count; i++)
                {
                    string line = RequirementLine(ctx.Requirements[i]);
                    if (line.Length > 0) board.Requirements.Add(line);
                }
            }
            board.Gentle = GentleOf(ctx);
            BonusesOf(ctx, card, board.Bonuses);
            board.Budget = Money("Budget", ctx != null ? ctx.BudgetKc : 0);
            board.Reward = Money("Reward", ctx != null ? ctx.RewardKc : 0);
            board.Wind = WindOf(ctx, card);
            board.Time = TimeOf(ctx, card);
            board.Pad = PadOf(ctx, card);
            board.Seed = SeedOf(ctx, card);
            board.Engineer = First(ctx != null ? ctx.EngineerLine : null, card != null ? card.EngineerLine : null);
            board.CanBuild = ctx != null && !ctx.IsFree && !string.IsNullOrEmpty(ctx.MissionId);
            return board;
        }

        public static string RequirementLine(MissionRequirement req)
        {
            if (req == null || string.IsNullOrEmpty(req.Id)) return "";
            switch (req.Id)
            {
                case "margin":
                    return "Margin at least " + MissionMapText.Number(req.Min ?? 0);
                case "maxG":
                    return req.Max.HasValue ? "Peak g at most " + MissionMapText.Number(req.Max.Value) : "";
                case "strapOns":
                    return req.Allow ? "Strap-on boosters allowed" : "No strap-on boosters";
                case "landing":
                    return Landing(req);
                case "reliability":
                    return req.Min.HasValue ? "Reliability at least " + MissionMapText.Number(req.Min.Value) : "";
                case "noRedRisks":
                    return "No red risks";
                case "randomFailures":
                    return "Random failures on";
                case "coastHours":
                    return req.Hours.HasValue ? "Coast " + MissionMapText.Number(req.Hours.Value) + " h" : "";
                default:
                    return req.Summary();
            }
        }

        static string TitleOf(MissionContext ctx, MissionBriefing card)
        {
            if (ctx != null && (!string.IsNullOrEmpty(ctx.Label) || !string.IsNullOrEmpty(ctx.Name)))
                return MissionMapText.TitleLine(ctx.Label, ctx.Name);
            if (card != null && !string.IsNullOrEmpty(card.ContractName)) return card.ContractName;
            return "Briefing";
        }

        static string PayloadOf(MissionContext ctx, MissionBriefing card)
        {
            string name = First(ctx != null ? ctx.PayloadName : null, card != null ? card.PayloadName : null);
            string kind = card != null ? card.PayloadKind : null;
            double mass = 0;
            string id = null;
            if (ctx != null)
            {
                mass = ctx.Mass;
                id = ctx.PayloadId;
            }
            else if (card != null) mass = card.MassKg;
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(kind) && !(mass > 0) && string.IsNullOrEmpty(id))
                return "";
            var label = string.IsNullOrEmpty(name) && string.IsNullOrEmpty(kind)
                ? null
                : new PayloadLabel { Name = name, Kind = kind };
            return MissionMapText.PayloadLine(new CampaignMission { MassKg = mass, PayloadId = id }, label);
        }

        static string FitOf(MissionContext ctx)
        {
            if (ctx == null) return "";
            var parts = new List<string>();
            if (ctx.Volume > 0) parts.Add(MissionMapText.Number(ctx.Volume) + " m3");
            if (ctx.Diameter > 0) parts.Add("Diameter " + MissionMapText.Number(ctx.Diameter) + " m");
            return string.Join(" · ", parts);
        }

        static string TargetOf(MissionContext ctx)
        {
            if (ctx == null || ctx.Target == null) return "";
            if (string.IsNullOrEmpty(ctx.Target.Kind) || ctx.Target.Kind == "free") return "";
            return MissionMapText.OrbitLine(new CampaignMission
            {
                TargetKind = ctx.Target.Kind,
                ApoKm = ctx.Target.ApoKm,
                PeriKm = ctx.Target.PeriKm,
                IncDeg = ctx.Target.IncDeg,
                C3Km2PerS2 = ctx.Target.C3Km2PerS2,
            });
        }

        static string GentleOf(MissionContext ctx)
        {
            if (ctx == null || ctx.IsFree) return "";
            return ctx.Gentle ? "Gentle on." : "Gentle off.";
        }

        static void BonusesOf(MissionContext ctx, MissionBriefing card, List<string> into)
        {
            if (ctx != null && ctx.BonusGoals.Count > 0)
            {
                for (int i = 0; i < ctx.BonusGoals.Count; i++)
                    if (!string.IsNullOrEmpty(ctx.BonusGoals[i])) into.Add(ctx.BonusGoals[i]);
                return;
            }
            if (card == null) return;
            for (int i = 0; i < card.Bonuses.Count; i++)
            {
                var bonus = card.Bonuses[i];
                if (bonus == null) continue;
                if (!string.IsNullOrEmpty(bonus.Label)) into.Add(bonus.Label);
                else if (!string.IsNullOrEmpty(bonus.Id)) into.Add(bonus.Id);
            }
        }

        static string Money(string label, double kc)
        {
            if (double.IsNaN(kc) || double.IsInfinity(kc) || !(kc > 0)) return "";
            return label + " " + MissionMapText.Number(kc) + " Kc";
        }

        static string WindOf(MissionContext ctx, MissionBriefing card)
        {
            string wind = First(ctx != null ? ctx.Weather : null, card != null ? card.WindLine : null);
            if (wind.Length == 0) return "";
            if (wind.StartsWith("Wind ", StringComparison.Ordinal)) return wind;
            return "Wind " + wind;
        }

        static string TimeOf(MissionContext ctx, MissionBriefing card)
        {
            string label = First(ctx != null ? ctx.TimeOfDay : null, card != null ? card.TimeLabel : null);
            bool sameCard = card != null && (ctx == null || string.IsNullOrEmpty(ctx.TimeOfDay) || ctx.TimeOfDay == card.TimeLabel);
            if (sameCard && card.Hour > 0 && label.Length > 0)
                return label + ", hour " + card.Hour.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (label.Length > 0) return label;
            if (card != null && card.Hour > 0)
                return "Hour " + card.Hour.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "";
        }

        static string PadOf(MissionContext ctx, MissionBriefing card)
        {
            string site = First(ctx != null ? ctx.Site : null, card != null && card.Site != null ? card.Site.Name : null);
            return site.Length == 0 ? "" : "Pad: " + site;
        }

        static string SeedOf(MissionContext ctx, MissionBriefing card)
        {
            if (ctx != null && !ctx.IsFree && !string.IsNullOrEmpty(ctx.MissionId))
                return "Seed " + SeedLabel(ctx.Seed);
            if (card != null && !card.FreeBuild) return "Seed " + card.Replay.Label;
            return "";
        }

        static string Landing(MissionRequirement req)
        {
            string where;
            if (req.Mode == "rtls")
            {
                where = "Land back at the pad";
                if (!string.IsNullOrEmpty(req.Site) && req.Site != "pad") where += ", site " + req.Site;
            }
            else if (req.Mode == "barge")
                where = "Land on " + (string.IsNullOrEmpty(req.Site) ? "the barge" : req.Site);
            else if (!string.IsNullOrEmpty(req.Mode))
                where = "Landing " + req.Mode;
            else
                where = "Landing";
            if (!string.IsNullOrEmpty(req.Grade)) where += ". Grade " + req.Grade;
            if (req.Boosters) where += ". Boosters too";
            return where;
        }

        static string First(string a, string b)
        {
            if (!string.IsNullOrEmpty(a)) return a;
            if (!string.IsNullOrEmpty(b)) return b;
            return "";
        }
    }
}
