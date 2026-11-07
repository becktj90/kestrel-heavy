// #369 gate: parity between StarterVerdict.Judge's landing grade (the return-Δv formula) and the
// flown landing grade (FlightBatch.Fly -> BoosterLanding.FlyPlanned -> RecoverySim.Fly), over every
// mission x every engine-stages-fN-bM Starter key (the 720-combo sweep).
//
// Only conservative disagreements are tolerated (Judge worse than flown), listed explicitly below.
// A Judge that reads perfect/good for a flown fuel exhaustion is a hard failure.
// Knife-edge rows are not pinned to an exact grade.
// Flown guidance failures (miss/fast/tilt with fuel left) are #342's scope, listed explicitly, not failed.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class JudgeLandingParityTests
    {
        static int Rank(string grade)
        {
            switch (grade)
            {
                case "perfect": return 3;
                case "good": return 2;
                case "hard": return 1;
                case "short": return 0;
                case "salvage": return 0;
                default: return -1;
            }
        }

        static IEnumerable<StarterChoice> Settings(StarterRocket r)
        {
            foreach (var e in StarterRocket.EngineOrder)
                foreach (var s in StarterRocket.StageValues)
                    for (int f = 0; f < StarterRocket.FuelNotches; f++)
                        foreach (var b in StarterRocket.BoosterValues)
                            if (r.IsStagesAllowed(s) && r.IsBoostersAllowed(b)) yield return new StarterChoice(e, s, f, b);
        }

        static void Set(StarterRocket r, StarterChoice c) { r.Engine = c.Engine; r.Stages = c.Stages; r.FuelNotch = c.FuelNotch; r.Boosters = c.Boosters; }

        /// <summary>
        /// Conservative disagreements (Judge worse than flown) found on main. Each is "mission key: judge -> flown".
        /// These are tolerated but must stay listed here, not counted silently. All are fuel-margin grades
        /// (Judge) vs touchdown-quality grades (flown): a modest reserve reads "good" while the touchdown is "perfect".
        /// </summary>
        static readonly HashSet<string> Conservative = new HashSet<string>
        {
            // m13 methane-2-f4-b0: the #369 exemplar. Judge names the landing failure; the flown booster
            // lands on fumes (0.1745 pitch rate) or exhausts fuel (0.55). Either way Judge is not optimistic.
            "m13 methane-2-f4-b0",
            "m10 kerosene-3-f3-b2",
            "m10 kerosene-3-f4-b2",
            "m10 methane-3-f1-b2",
            "m10 methane-3-f2-b2",
            "m10 methane-3-f4-b2",
            "m13 kerosene-3-f3-b0",
            "m13 methane-3-f2-b0",
            "m19 methane-3-f0-b2",
            "m19 methane-3-f1-b2",
            "m19 methane-3-f2-b2",
            "m19 methane-3-f3-b2",
            "m19 methane-3-f4-b2",
            // #409 guidance retune: these m13 combos now land (were guidance failures under the 10°/s
            // pitch rate). The Judge grades their tight fuel margins "hard"/"good" vs flown "perfect" —
            // the designed conservative behavior (fuel-margin grade vs touchdown-quality grade).
            "m13 kerosene-2-f1-b0",
            "m13 kerosene-2-f2-b0",
            "m13 kerosene-2-f3-b0",
            "m13 kerosene-3-f4-b0",
            "m13 methane-2-f0-b0",
            "m13 methane-2-f1-b0",
            "m13 methane-2-f2-b0",
            "m13 methane-3-f1-b0",
        };

        /// <summary>
        /// Knife-edge rows: the flown booster lands with &lt;5% of its reserve left (or is in this set).
        /// The exact grade is not pinned.
        /// </summary>
        static readonly HashSet<string> KnifeEdge = new HashSet<string>
        {
            // m13 methane-2-f3-b0: the #369 issue's knife-edge (0.4% of reserve left on the 0.55 rate).
            "m13 methane-2-f3-b0",
        };

        /// <summary>
        /// Flown guidance failures (miss/fast/tilt with fuel left): #342's scope, not #369's. The fuel formula
        /// is not expected to predict them. Listed explicitly; the parity test does not fail on them.
        /// All are m13 2-stage/3-stage designs missing Haven under the 0.1745 rad/s pitch rate (#400).
        /// #409 retuned the guidance: the 2-f1/f2/f3, 3-f4, methane-2-f0/f1/f2 and methane-3-f1 rows now land,
        /// so they moved to <see cref="Conservative"/>. The kerosene-3-f0/f1/f2 rows below still miss.
        /// </summary>
        static readonly HashSet<string> GuidanceFailures = new HashSet<string>
        {
            "m13 kerosene-3-f0-b0",
            "m13 kerosene-3-f1-b0",
            "m13 kerosene-3-f2-b0",
        };

#if KH_DOTNET_TESTS
        [Test]
        public void JudgeLandingGrade_MatchesFlownGrade_AcrossAllSettings()
        {
            var cat = StarterFixture.Catalog;
            StarterFixture.EnsureBases();
            int compared = 0, skipped = 0;
            var hardFailures = new List<string>();
            var conservativeFound = new List<string>();
            var guidanceFound = new List<string>();
            var unexpected = new List<string>();

            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = StarterFixture.Rocket(m.Id);
                foreach (var c in Settings(r))
                {
                    Set(r, c);
                    string label = m.Id + " " + c.Key;
                    if (r.Check().Blocked) { skipped++; continue; }
                    var dj = r.BuildDesign();
                    var d = Design.FromJson(dj);
                    bool fits = Readout.Quick(cat, dj).fairingFits != false;
                    var sim = ReferenceAscent.Flight(cat, d).Run();
                    var v = StarterVerdict.Judge(cat, d, m, sim, fits);

                    // Non-landing missions: neither side grades a landing.
                    if (!m.LandingRequired)
                    {
                        if (v.LandingGrade != null) hardFailures.Add(label + ": Judge grades " + v.LandingGrade + " on a non-landing mission");
                        skipped++;
                        continue;
                    }

                    // Judge only grades the landing for an intact predictor flight.
                    if (v.LandingGrade == null) { skipped++; continue; }

                    var flight = FlightBatch.Fly(cat, m, d, 1);
                    if (flight.failure != null || flight.outcome == "failure" || flight.outcome == "crash") { skipped++; continue; }

                    compared++;
                    string jg = v.LandingGrade, fg = flight.landingGrade;
                    string cause = flight.landingCause ?? "";
                    double reserve = d.stages.Count > 0 ? d.stages[0].landingPropMass : 0;
                    double fracLeft = reserve > 0 && !double.IsNaN(flight.landingPropLeftKg) ? flight.landingPropLeftKg / reserve : double.NaN;

                    // Knife-edge rows are not pinned to an exact grade (#369).
                    if (KnifeEdge.Contains(label)) { skipped++; continue; }

                    bool landed = fg == "perfect" || fg == "good" || fg == "hard";
                    // Fuel exhaustion: the sim ran dry and failed (recovery-fuel), or failed with <5% left.
                    // A booster that LANDED on fumes is not a fuel failure (it is conservative or knife-edge).
                    bool fuelExhausted = !landed && (cause == "recovery-fuel" || (!double.IsNaN(fracLeft) && fracLeft < 0.05));
                    bool guidanceFail = !landed && !fuelExhausted
                        && (cause == "recovery-miss" || cause == "recovery-fast" || cause == "recovery-tilt");

                    if (landed)
                    {
                        bool ke = !double.IsNaN(fracLeft) && fracLeft < 0.05;
                        if (jg == "short" && !ke)
                            hardFailures.Add(label + ": flown " + fg + " (left " + fracLeft.ToString("0%") + ") but Judge=short");
                        else if (Rank(jg) > Rank(fg))
                            hardFailures.Add(label + ": Judge " + jg + " optimistic vs flown " + fg);
                        else if (Rank(jg) < Rank(fg) && !Conservative.Contains(label) && !ke)
                            conservativeFound.Add(label + ": Judge " + jg + " vs flown " + fg);
                    }
                    else if (fuelExhausted)
                    {
                        // #369 core: a flown fuel exhaustion must read as a landing failure.
                        if (jg != "short") hardFailures.Add(label + ": fuel exhaustion (" + fg + "/" + cause + " left=" + fracLeft.ToString("0%") + ") but Judge=" + jg);
                    }
                    else if (guidanceFail)
                    {
                        // #342 scope: the booster had fuel but missed/was fast/tilted.
                        if (!GuidanceFailures.Contains(label)) guidanceFound.Add(label + ": flown " + fg + "/" + cause + " vs Judge " + jg);
                    }
                    else if (fg == "short" && jg != "short")
                    {
                        // Flown short for legs/site/no-separation: Judge must agree (it has the same guards).
                        hardFailures.Add(label + ": flown short (" + cause + ") but Judge=" + jg);
                    }
                    // fg == jg, or both short: agreement.
                }
            }

            TestContext.WriteLine($"compared {compared}, skipped {skipped}");
            foreach (var s in conservativeFound) TestContext.WriteLine("UNLISTED CONSERVATIVE: " + s);
            foreach (var s in guidanceFound) TestContext.WriteLine("GUIDANCE: " + s);

            var problems = new List<string>();
            problems.AddRange(hardFailures);
            // Unlisted conservative disagreements and unlisted guidance failures must be triaged into the explicit lists.
            problems.AddRange(conservativeFound);
            problems.AddRange(guidanceFound);
            if (unexpected.Count > 0) problems.AddRange(unexpected);
            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.Greater(compared, 50, "the sweep compared too few landings");
        }
#endif
    }
}
