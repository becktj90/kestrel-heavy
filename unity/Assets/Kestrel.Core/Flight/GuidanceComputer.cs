// #341 (Kestrel Physics and Arcade rulings, DESIGN_341.md): the optional closed-loop guidance computer avionics part
// (player-facing name "Upper-stage guidance computer", data/guidance.json closedLoop.label).
// Basic guidance is a pre-set pitch program with no correction: the stage-1 kick and turn, then PEG-lite on the upper
// stages, cut off at the target energy. With this part fitted the upper stages steer to (aim altitude, vr = 0) at that same
// cutoff (AscentSim.ClosedLoopPitch); stage 1 stays player-flown. The part is opt-in (Design.guidanceComputer, JSON
// avionics.guidanceComputer), costs mass and credits, and is offered only behind the designer.guidanceComputer flag (off in
// the Starter designer) and a campaign unlock. Labels live in data/guidance.json; Codex owns the guide copy.
using System;
using System.IO;
using Kestrel.Core.Economy;
using Kestrel.Core.Features;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public static class GuidanceComputer
    {
        public const string PartId = "guidance-computer";
        /// <summary>Top-stage dry mass the part adds, kg (Mass.GUIDANCE_COMPUTER_MASS).</summary>
        public const double MassKg = Mass.GUIDANCE_COMPUTER_MASS;
        /// <summary>The feature flag that offers the part in the designer.</summary>
        public const string FeatureFlag = FeatureKeys.DesignerGuidanceComputer;
        /// <summary>The part unlocks once this campaign mission is cleared (data/guidance.json closedLoop.unlockAfterMission).</summary>
        public const int UnlockAfterMission = 10;

        /// <summary>True when the design carries the part.</summary>
        public static bool Fitted(Design d) => d != null && d.guidanceComputer;

        /// <summary>The campaign unlock: cleared at least <see cref="UnlockAfterMission"/>.</summary>
        public static bool Unlocked(Career career) => career != null && career.Cleared >= UnlockAfterMission;

        /// <summary>The designer may offer the part: flag on and the campaign unlock earned. Both are required.</summary>
        public static bool Available(FeatureFlags flags, Career career) => flags != null && flags.IsOn(FeatureFlag) && Unlocked(career);

        /// <summary>Fit or remove the part. Removing it leaves a design byte-identical to one that never had it.</summary>
        public static void Fit(Design d, bool on) { if (d != null) d.guidanceComputer = on; }

        /// <summary>The data the report and HUD layers read (data only; Graphics owns the HUD and rail).</summary>
        public sealed class Labels
        {
            /// <summary>Basic guidance, said honestly (data/guidance.json basic.label): it chases the target height but cannot plan the cutoff.</summary>
            public string Basic = "";
            public string Part = "";
            /// <summary>Shown while the part flies an upper stage (AscentSim.GuidanceInControl).</summary>
            public string InControl = "";
            /// <summary>Shown when a live pitch nudge was held on the upper stage (SimResult.nudgeHeldByGuidance).</summary>
            public string NudgeHeld = "";
            /// <summary>Part-card description for the designer (data/guidance.json closedLoop.about; Codex copy).</summary>
            public string About = "";
            /// <summary>Part-card real-world note: real insertion has a few km of dispersion (closedLoop.realWorld; Physics' note, Codex copy).</summary>
            public string RealWorld = "";
            /// <summary>The code the copy describes (closedLoop.sources), for provenance; not player-facing text.</summary>
            public string[] Sources = Array.Empty<string>();
            public double MassKg;
            public long Credits;
            public string FeatureFlag = "", Id = "";
            public int UnlockAfterMission;
        }

        public static Labels Load(string repoRoot) => Parse(File.ReadAllText(Path.Combine(repoRoot, "data", "guidance.json")));

        public static Labels Parse(string json)
        {
            var o = MiniJson.ParseObject(json);
            var basic = o.Obj("basic");
            var cl = o.Obj("closedLoop");
            if (basic == null || cl == null) throw new InvalidOperationException("guidance.json needs basic and closedLoop");
            return new Labels
            {
                Basic = basic.Str("label", ""),
                Part = cl.Str("label", ""),
                InControl = cl.Str("inControlNote", ""),
                NudgeHeld = cl.Str("nudgeHeldNote", ""),
                About = cl.Str("about", ""),
                RealWorld = cl.Str("realWorld", ""),
                Sources = Strings(cl.Arr("sources")),
                MassKg = cl.Num("massKg"),
                Credits = (long)Math.Round(cl.Num("credits")),
                FeatureFlag = cl.Str("featureFlag", ""),
                Id = cl.Str("id", ""),
                UnlockAfterMission = (int)Math.Round(cl.Num("unlockAfterMission")),
            };
        }

        static string[] Strings(System.Collections.Generic.List<object> a)
        {
            if (a == null) return Array.Empty<string>();
            var list = new System.Collections.Generic.List<string>(a.Count);
            foreach (var v in a) if (v is string str) list.Add(str);
            return list.ToArray();
        }

        /// <summary>The guidance line a report shows for a flight: the in-control note when the part flew the upper stage,
        /// the basic label otherwise. Data only.</summary>
        public static string NoteFor(Labels labels, SimResult result)
        {
            if (labels == null) return "";
            if (result != null && result.nudgeHeldByGuidance) return labels.NudgeHeld;
            if (result != null && result.guidanceInControl) return labels.InControl;
            return labels.Basic;
        }
    }
}
