// MissionEvent schema from docs/MISSION_EVENTS.md §1.
// One event type for narration, SFX, music, telemetry, pad, economy and Codex.
// N1 bus types stay on links.narration; they are not a second schema.
using System.Collections.Generic;

namespace Kestrel.Core.Timeline
{
    public static class TimelinePhase
    {
        public const string Pad = "pad", Load = "load", Terminal = "terminal", Ignition = "ignition";
        public const string Ascent = "ascent", Staging = "staging", Orbit = "orbit", Deploy = "deploy";
        public const string Transfer = "transfer", Disposal = "disposal", Recovery = "recovery", Post = "post";
        public static readonly string[] All = { Pad, Load, Terminal, Ignition, Ascent, Staging, Orbit, Deploy, Transfer, Disposal, Recovery, Post };
    }

    public static class TimelineCriticality
    {
        public const string Info = "info", Nominal = "nominal", Critical = "critical", Safety = "safety";
        public static readonly string[] All = { Info, Nominal, Critical, Safety };
    }

    public static class TimelineVisibility
    {
        public const string Kid = "kid", Standard = "standard", Engineer = "engineer";
        public static readonly string[] All = { Kid, Standard, Engineer };
    }

    public static class TimelineStatus
    {
        public const string Pending = "pending", InProgress = "inProgress", Done = "done", Skipped = "skipped";
        public const string Held = "held", Delayed = "delayed", Anomaly = "anomaly", Failed = "failed";
        public static readonly string[] All = { Pending, InProgress, Done, Skipped, Held, Delayed, Anomaly, Failed };
    }

    /// <summary>Trigger union: tRel, simCondition, after, or action. Unused arms stay unset.</summary>
    public sealed class Trigger
    {
        public string kind;
        public double t;
        public string from;
        public string expr;
        public string eventId;
        public double delay;
        public string actor;
        public string actionId;

        public static Trigger TRel(double t, string from) => new Trigger { kind = "tRel", t = t, from = from };
        public static Trigger Sim(string expr) => new Trigger { kind = "simCondition", expr = expr };
        public static Trigger After(string eventId, double delay) => new Trigger { kind = "after", eventId = eventId, delay = delay };
        public static Trigger Action(string actor, string actionId) => new Trigger { kind = "action", actor = actor, actionId = actionId };
    }

    public sealed class AbortRule
    {
        public string condition;
        public string branchId;
    }

    public sealed class EventStatus
    {
        public string state;
        public double progress;
    }

    public sealed class EventLinks
    {
        public List<string> narration = new List<string>();
        public List<string> sfx = new List<string>();
        public List<string> music = new List<string>();
        public List<string> telemetry = new List<string>();
        public List<string> pad = new List<string>();
        public List<string> codex = new List<string>();
    }

    public sealed class EconomyHooks
    {
        public long costOnDone;
        public long refundOnScrub;
        public string currency = "Kc";
    }

    /// <summary>Loc keys plus the source strings the generator actually produced.</summary>
    public sealed class EventStrings
    {
        public string name, shortLabel, explainWhy, anomaly;
    }

    public sealed class MissionEvent
    {
        public string id, name, shortLabel, phase;
        public Trigger trigger;
        public List<string> preconditions = new List<string>();
        public List<string> blocks = new List<string>();
        public bool holdable;
        public string criticality;
        public List<AbortRule> abortRules = new List<AbortRule>();
        public double nominal;
        public double? actual;
        public double tolerance;
        public double variance;
        public EventStatus status;
        public EventLinks links;
        public string visibility;
        public EconomyHooks economy;
        public EventStrings strings;
    }

    /// <summary>Why a spec event was not emitted. Same explain-why duty as a present event.</summary>
    public sealed class Absence
    {
        public string id;
        public string explainWhy;
    }

    public sealed class StageEngineFact
    {
        public string stageKey, engineId;
        public bool restart;
        public int restarts;
    }

    public sealed class CheckReport
    {
        public bool ok;
        public List<string> errors = new List<string>();
    }

    public sealed class Timeline
    {
        public string vehicleId, padId, missionId, mode, recovery;
        public uint seed;
        public double latitude;
        public List<string> hardware = new List<string>();
        public List<StageEngineFact> engines = new List<StageEngineFact>();
        public List<MissionEvent> events = new List<MissionEvent>();
        public List<MissionEvent> branches = new List<MissionEvent>();
        public List<Absence> absent = new List<Absence>();
        public CheckReport checks = new CheckReport();
    }
}
