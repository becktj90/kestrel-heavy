// Step list from the guided-designer spec. Seven steps, same in Simple and Engineer.
// Structure (material, safety factor, avionics) is part of Tanks. Guidance (max-Q throttle, g limit) is part of Aero.
using System.Collections.Generic;

namespace Kestrel.Core.Designer.Stepper
{
    public static class StepperCatalog
    {
        public const int Count = 7;

        static readonly StepDef[] Steps =
        {
            Def(DesignerStep.Mission, "Mission goal", "Mission",
                "Pick the job: payload and orbit", "payload",
                new[] { "mission", "payload", "orbit" },
                new string[0],
                new string[0],
                new[] { "payloadMass", "orbit", "cost" }),
            Def(DesignerStep.Engines, "Propellant & engines", "Engines",
                "Lift off: TWR ≥ 1.3", "engines",
                new[] { "propellant", "engine", "mixtureOffsetPct" },
                new[] { "pc", "expansionRatio", "gimbal" },
                new[] { "engine-overpressure", "bad-mixture", "cryo-material", "engine-out" },
                new[] { "liftoffTWR", "engineCount", "mixtureOffsetPct" }),
            Def(DesignerStep.Tanks, "Tanks & stages", "Tanks",
                "Reach orbit: Δv margin ≥ 0", "tanks",
                new[] { "propMass", "stages", "safetyFactor" },
                new[] { "pressure", "insulation", "material", "avionics" },
                new[] { "prop-depletion", "structural-maxq", "overg", "cryo-material", "separation-collision" },
                new[] { "margin", "totalDeltaV", "maxQFraction", "safetyFactor" }),
            Def(DesignerStep.Aero, "Aero & fairing", "Aero",
                "Stay stable, payload fits", "fairing",
                new[] { "nose", "fins", "fairing" },
                new[] { "maxQThrottle", "gLimit" },
                new[] { "tumble", "thermal", "structural-maxq" },
                new[] { "stability", "car", "fairingFit" }),
            Def(DesignerStep.Recovery, "Recovery", "Recovery",
                "Expend or bring it home", "legs",
                new[] { "recoveryMode", "legs", "landingReserve" },
                new string[0],
                new[] { "leg-collapse", "thermal" },
                new[] { "landingReserve", "landingNeed", "recoveryMode", "payloadCost" }),
            Def(DesignerStep.Look, "Look", "Look",
                "Make it yours (no physics)", "body",
                new[] { "paint", "accent", "name" },
                new string[0],
                new string[0],
                new[] { "name" }),
            Def(DesignerStep.Review, "Review & launch", "Review",
                "Confirm risks and fly", "stack",
                new[] { "risks", "bonuses", "launch" },
                new string[0],
                new[]
                {
                    "structural-maxq", "overg", "tumble", "engine-overpressure", "bad-mixture", "thermal",
                    "cryo-material", "prop-depletion", "separation-collision", "leg-collapse", "engine-out",
                },
                new[] { "cost", "reliability", "maxQ", "peakG" }),
        };

        public static IReadOnlyList<StepDef> All => Steps;

        public static StepDef Get(DesignerStep step) => Steps[(int)step - 1];

        public static string Header(DesignerStep step) => "STEP " + (int)step + " OF " + Count;

        public static bool TryStep(int number, out DesignerStep step)
        {
            if (number >= 1 && number <= Count)
            {
                step = (DesignerStep)number;
                return true;
            }
            step = DesignerStep.Mission;
            return false;
        }

        static StepDef Def(DesignerStep step, string title, string shortName, string goal, string highlight,
            string[] simple, string[] engineer, string[] failures, string[] stats)
            => new StepDef
            {
                Step = step,
                Number = (int)step,
                Title = title,
                ShortName = shortName,
                Goal = goal,
                Highlight = highlight,
                SimpleControls = simple,
                EngineerControls = engineer,
                FailureKinds = failures,
                StatKeys = stats,
            };
    }
}
