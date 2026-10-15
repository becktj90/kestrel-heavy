// The one object carried from briefing to the designer, the ascent and the report.
// Core only: no UnityEngine. Absent session (or an explicit free one) is a free build.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kestrel.Core.Flight
{
    public enum MissionMode { Campaign, Contract, Free, Tutorial }

    /// <summary>Screen and overlay names the shell maps onto scenes. Briefing and Report are overlays.</summary>
    public static class SessionScreens
    {
        public const string Home = "Home";
        public const string Design = "Design";
        public const string Flight = "Flight";
        public const string Missions = "Missions";
        public const string None = "None";
        public const string Briefing = "Briefing";
        public const string Report = "Report";
    }

    public enum RouteId
    {
        HomeLaunch,
        HomeHangar,
        MissionChosen,
        BriefingBuild,
        BriefingBack,
        DesignerBack,
        OpenReport,
        ReportNext,
        ReportRetry,
        ReportHome,
        ReportMissions,
        ExitHome,
        ExitMissions,
    }

    public readonly struct RouteResult
    {
        public readonly string Screen;
        public readonly string Overlay;
        public readonly MissionContext Session;

        public RouteResult(string screen, string overlay, MissionContext session)
        {
            Screen = screen;
            Overlay = overlay;
            Session = session;
        }
    }

    public sealed class MissionTarget
    {
        public string Kind;
        public double ApoKm, PeriKm, IncDeg, C3Km2PerS2;
    }

    /// <summary>One campaign requirement, kept so the briefing can show the real constraint.</summary>
    public sealed class MissionRequirement
    {
        public string Id;
        public double? Min, Max, Hours;
        public bool Allow, Boosters;
        public string Mode, Grade, Site;

        public string Summary()
        {
            switch (Id)
            {
                case "margin": return "margin at least 0";
                case "maxG": return "max g " + Num(Max);
                case "strapOns": return Allow ? "strap-on boosters allowed" : "no strap-on boosters";
                case "landing":
                    var land = "landing " + (Mode ?? "");
                    if (!string.IsNullOrEmpty(Grade)) land += " " + Grade;
                    if (!string.IsNullOrEmpty(Site)) land += " at " + Site;
                    if (Boosters) land += ", boosters too";
                    return land;
                case "reliability": return "reliability " + Num(Min);
                case "noRedRisks": return "no red risks";
                case "randomFailures": return "random failures on";
                case "coastHours": return "coast " + Num(Hours) + " h";
                default: return Id ?? "";
            }
        }

        static string Num(double? value) =>
            value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : "";
    }

    public sealed class MissionContext
    {
        public string MissionId;
        public string Label;
        public string Name;
        public string PayloadId;
        public string PayloadName;
        public string Goal;
        public string EngineerLine;
        public double Mass, Volume, Diameter;
        public MissionTarget Target = new MissionTarget();
        public readonly List<MissionRequirement> Requirements = new List<MissionRequirement>();
        public readonly List<string> BonusGoals = new List<string>();
        public double BudgetKc, RewardKc;
        public ulong Seed;
        public string Weather, Site, TimeOfDay;
        public bool Gentle;
        public MissionMode Mode;

        public bool IsFree => Mode == MissionMode.Free;

        public static ulong DefaultSeed(Campaign campaign)
        {
            if (campaign != null && campaign.Seeds != null && campaign.Seeds.Count > 0) return campaign.Seeds[0];
            return 1;
        }

        public static string LabelFor(int index)
        {
            if (index < 1) return "";
            return "KH-" + index.ToString(CultureInfo.InvariantCulture);
        }

        public static MissionContext Free(ulong seed = 1)
        {
            return new MissionContext
            {
                Label = "Free build",
                Name = "Free build",
                Mode = MissionMode.Free,
                Seed = seed,
                Gentle = false,
                Target = new MissionTarget { Kind = "free" },
            };
        }

        public static List<MissionContext> CampaignBoard(Campaign campaign)
        {
            var list = new List<MissionContext>();
            if (campaign == null || campaign.Missions == null) return list;
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var ctx = FromMission(campaign, campaign.Missions[i]);
                if (ctx != null) list.Add(ctx);
            }
            return list;
        }

        public static MissionContext FromCampaign(Campaign campaign, string missionId, ulong? seed = null)
        {
            if (campaign == null || string.IsNullOrEmpty(missionId)) return null;
            return FromMission(campaign, campaign.ById(missionId), seed);
        }

        public static MissionContext FromMission(Campaign campaign, CampaignMission mission, ulong? seed = null)
        {
            if (mission == null) return null;
            var ctx = new MissionContext
            {
                MissionId = mission.Id,
                Label = LabelFor(mission.Index),
                Name = mission.Name,
                PayloadId = mission.PayloadId,
                Mass = mission.MassKg,
                Volume = mission.VolumeM3,
                Diameter = mission.DiameterM,
                BudgetKc = mission.BudgetKc,
                RewardKc = mission.RewardKc,
                Seed = seed ?? DefaultSeed(campaign),
                Gentle = mission.GentleDefault,
                Mode = MissionMode.Campaign,
                Target = new MissionTarget
                {
                    Kind = mission.TargetKind,
                    ApoKm = mission.ApoKm,
                    PeriKm = mission.PeriKm,
                    IncDeg = mission.IncDeg,
                    C3Km2PerS2 = mission.C3Km2PerS2,
                },
            };
            if (mission.MarginNonNegative)
                ctx.Requirements.Add(new MissionRequirement { Id = "margin", Min = 0 });
            if (!double.IsNaN(mission.MaxG))
                ctx.Requirements.Add(new MissionRequirement { Id = "maxG", Max = mission.MaxG });
            if (mission.StrapOnsAllowed)
                ctx.Requirements.Add(new MissionRequirement { Id = "strapOns", Allow = true });
            if (!string.IsNullOrEmpty(mission.LandingMode))
                ctx.Requirements.Add(new MissionRequirement
                {
                    Id = "landing",
                    Mode = mission.LandingMode,
                    Grade = mission.LandingGrade,
                    Site = mission.LandingSite,
                    Boosters = mission.LandBoosters,
                });
            if (!double.IsNaN(mission.ReliabilityMin))
                ctx.Requirements.Add(new MissionRequirement { Id = "reliability", Min = mission.ReliabilityMin });
            if (mission.NoRedRisks)
                ctx.Requirements.Add(new MissionRequirement { Id = "noRedRisks" });
            if (mission.RandomFailures)
                ctx.Requirements.Add(new MissionRequirement { Id = "randomFailures" });
            if (mission.CoastHours > 0)
                ctx.Requirements.Add(new MissionRequirement { Id = "coastHours", Hours = mission.CoastHours });
            return ctx;
        }

        /// <summary>Copies the seeded pad, clock, wind and bonus lines. Does not change the mission id or mode.</summary>
        public void ApplyBriefing(MissionBriefing card)
        {
            if (card == null) return;
            Seed = card.MissionSeed;
            if (!string.IsNullOrEmpty(card.WindLine)) Weather = card.WindLine;
            if (card.Site != null && !string.IsNullOrEmpty(card.Site.Name)) Site = card.Site.Name;
            if (!string.IsNullOrEmpty(card.TimeLabel)) TimeOfDay = card.TimeLabel;
            if (!string.IsNullOrEmpty(card.Goal)) Goal = card.Goal;
            if (!string.IsNullOrEmpty(card.PayloadName)) PayloadName = card.PayloadName;
            if (!string.IsNullOrEmpty(card.EngineerLine)) EngineerLine = card.EngineerLine;
            BonusGoals.Clear();
            for (int i = 0; i < card.Bonuses.Count; i++)
            {
                var bonus = card.Bonuses[i];
                if (bonus == null) continue;
                if (!string.IsNullOrEmpty(bonus.Label)) BonusGoals.Add(bonus.Label);
                else if (!string.IsNullOrEmpty(bonus.Id)) BonusGoals.Add(bonus.Id);
            }
        }

        public MissionRequirement Find(string id)
        {
            for (int i = 0; i < Requirements.Count; i++)
                if (Requirements[i].Id == id) return Requirements[i];
            return null;
        }

        public string ListText()
        {
            if (string.IsNullOrEmpty(Label)) return Name ?? "";
            if (string.IsNullOrEmpty(Name) || Name == Label) return Label;
            return Label + "  " + Name;
        }

        public string Tooltip()
        {
            var sb = new StringBuilder();
            sb.Append(string.IsNullOrEmpty(Label) ? "Free build" : Label);
            if (!string.IsNullOrEmpty(Name) && Name != Label) sb.Append(' ').Append(Name);
            sb.Append('.');
            if (Mass > 0)
                sb.Append(' ').Append(Mass.ToString("0.##", CultureInfo.InvariantCulture)).Append(" kg.");
            if (Target != null && !string.IsNullOrEmpty(Target.Kind) && Target.Kind != "free")
            {
                sb.Append(' ').Append(Target.Kind);
                if (Target.Kind == "c3")
                    sb.Append(' ').Append(Target.C3Km2PerS2.ToString("0.##", CultureInfo.InvariantCulture)).Append('.');
                else if (Target.ApoKm > 0)
                    sb.Append(' ').Append(Target.ApoKm.ToString("0.##", CultureInfo.InvariantCulture)).Append(" km.");
                else sb.Append('.');
            }
            sb.Append(Gentle ? " Gentle on." : " Gentle off.");
            if (Requirements.Count > 0)
            {
                sb.Append(" Requirements:");
                for (int i = 0; i < Requirements.Count; i++)
                    sb.Append(' ').Append(Requirements[i].Summary()).Append('.');
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Static carry. Briefing sets <see cref="Current"/>; the designer, ascent and report read it.
    /// Null means nobody has started a session, which is a free build.
    /// </summary>
    public static class GameSession
    {
        public static MissionContext Current { get; private set; }

        public static bool IsFreeBuild => Current == null || Current.IsFree;

        public static void Carry(MissionContext context) => Current = context;

        public static void Clear() => Current = null;

        public static RouteResult Apply(RouteId id, Campaign campaign = null, string missionId = null, MissionBriefing briefing = null)
        {
            switch (id)
            {
                case RouteId.HomeLaunch:
                    return Done(SessionScreens.Missions, SessionScreens.None);
                case RouteId.HomeHangar:
                    Current = MissionContext.Free(briefing != null ? briefing.MissionSeed : 1);
                    Current.ApplyBriefing(briefing);
                    return Done(SessionScreens.Design, SessionScreens.None);
                case RouteId.MissionChosen:
                    var chosen = MissionContext.FromCampaign(campaign, missionId);
                    if (chosen == null) return Done(SessionScreens.Missions, SessionScreens.None);
                    chosen.ApplyBriefing(briefing);
                    Current = chosen;
                    return Done(SessionScreens.Design, SessionScreens.None);
                case RouteId.BriefingBuild:
                    return Done(SessionScreens.Design, SessionScreens.None);
                case RouteId.BriefingBack:
                case RouteId.ReportMissions:
                case RouteId.ExitMissions:
                    return Done(SessionScreens.Missions, SessionScreens.None);
                case RouteId.DesignerBack:
                    if (IsFreeBuild)
                    {
                        Current = null;
                        return Done(SessionScreens.Home, SessionScreens.None);
                    }
                    return Done(SessionScreens.Missions, SessionScreens.Briefing);
                case RouteId.OpenReport:
                    return Done(SessionScreens.Missions, SessionScreens.Report);
                case RouteId.ReportRetry:
                    return Done(SessionScreens.Design, SessionScreens.None);
                case RouteId.ReportNext:
                    Advance(campaign);
                    return Done(SessionScreens.Missions, SessionScreens.None);
                case RouteId.ReportHome:
                case RouteId.ExitHome:
                    Current = null;
                    return Done(SessionScreens.Home, SessionScreens.None);
                default:
                    return Done(null, SessionScreens.None);
            }
        }

        static void Advance(Campaign campaign)
        {
            var cur = Current;
            if (campaign == null || cur == null || cur.Mode != MissionMode.Campaign) return;
            var mission = campaign.ById(cur.MissionId);
            if (mission == null) return;
            var next = campaign.Mission(mission.Index + 1);
            if (next == null) return;
            var ctx = MissionContext.FromMission(campaign, next, cur.Seed);
            if (ctx != null) Current = ctx;
        }

        static RouteResult Done(string screen, string overlay) => new RouteResult(screen, overlay, Current);
    }
}
