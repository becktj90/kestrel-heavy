// Guided 200 km LEO tutorial (M6 / §6). Pure step list; the panel drives UI and edits.
using System;
using System.Collections.Generic;

namespace Kestrel.Slice.DesignerV3
{
    public sealed class TutorialStep
    {
        public string id, kid, engineer;
        public DesignerV3Tab tab;
        public string field;
        public Func<bool> done;

        public TutorialStep(string id, DesignerV3Tab tab, string field, string kid, string engineer, Func<bool> done = null)
        {
            this.id = id; this.tab = tab; this.field = field; this.kid = kid; this.engineer = engineer; this.done = done;
        }
    }

    public static class DesignerV3Tutorial
    {
        public const string TargetOrbitId = "leo-200";
        public const double TargetAltKm = 200;

        /// <summary>Six beats to a Tiny Orbit-style 200 km LEO build.</summary>
        public static List<TutorialStep> Leo200(Func<string> orbitId, Func<double> liftoffTwr, Func<double> margin, Func<bool> hasFins, Func<bool> fairingOk)
        {
            return new List<TutorialStep>
            {
                new TutorialStep("welcome", DesignerV3Tab.Stages, null,
                    "Welcome to the hangar. We will build a rocket that reaches 200 km.",
                    "Tutorial: guided LEO 200 km build. Follow each tab; Launch stays locked until the checklist is green.",
                    () => true),
                new TutorialStep("engines", DesignerV3Tab.Engines, "engine",
                    "Pick engines with enough push. Liftoff push should stay above 1.2.",
                    "Set stage-1 engine and count so liftoff TWR ≥ 1.2.",
                    () => liftoffTwr != null && liftoffTwr() >= 1.2),
                new TutorialStep("tanks", DesignerV3Tab.Tanks, "propMass",
                    "Add fuel until the speed bar turns green.",
                    "Raise propellant until Δv margin ≥ 5% versus the 200 km LEO requirement.",
                    () => margin != null && margin() >= 0.05),
                new TutorialStep("aero", DesignerV3Tab.Aero, "fins",
                    "Turn on fins so the rocket stays pointed the right way.",
                    "Enable fins (or keep a stable CP behind CG) before launch.",
                    () => hasFins == null || hasFins()),
                new TutorialStep("payload", DesignerV3Tab.Payload, "orbit",
                    "Set the trip to Low Earth Orbit at 200 km.",
                    "Select the LEO 200 km orbit (or equivalent target altitude).",
                    () => orbitId != null && (orbitId() == TargetOrbitId || orbitId() == "leo")),
                new TutorialStep("go", DesignerV3Tab.Stages, "launch",
                    "Nice work. When every light is green, tap Launch.",
                    "Fairing fit and margin green → Launch. Red risks still need Launch anyway.",
                    () => margin != null && margin() >= 0 && (fairingOk == null || fairingOk())),
            };
        }

        public static int Advance(IList<TutorialStep> steps, int index)
        {
            if (steps == null || steps.Count == 0) return 0;
            int i = Math.Max(0, Math.Min(index, steps.Count - 1));
            while (i < steps.Count - 1 && steps[i].done != null && steps[i].done()) i++;
            return i;
        }

        public static bool Complete(IList<TutorialStep> steps)
        {
            if (steps == null || steps.Count == 0) return false;
            var last = steps[steps.Count - 1];
            return last.done == null || last.done();
        }
    }
}
