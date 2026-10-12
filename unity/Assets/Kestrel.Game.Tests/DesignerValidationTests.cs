using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    // U1 (DESIGNER_SPEC §6, §11.3): kid wording per code, summary chip, Launch gate.
    public class DesignerValidationTests
    {
        static readonly string[] Codes =
        {
            "zero-engines", "zero-propellant", "schema", "math", "twr-low", "engines-dont-fit", "top-heavy",
            "fairing-narrow", "payload-doesnt-fit", "twr-marginal", "weak-upper", "weak-upper-dv", "dv-margin",
            "landing-hardware", "heavy-fairing",
        };

        [TestCaseSource(nameof(Codes))]
        public void EveryCoreCodeHasKidWording(string code)
        {
            Assert.IsTrue(DesignerValidation.KidWording.ContainsKey(code), code);
            var issue = new Issue { code = code, tab = "stages", msg = "ENGINEER TEXT" };
            string kid = DesignerValidation.Wording(issue, "simple");
            Assert.AreEqual(DesignerValidation.KidWording[code], kid);
            Assert.AreEqual(kid, DesignerValidation.Wording(issue, "standard"));
            Assert.AreEqual("ENGINEER TEXT", DesignerValidation.Wording(issue, "engineer"));
            StringAssert.DoesNotContain("TWR", kid); StringAssert.DoesNotContain("Δv", kid);
        }

        [Test]
        public void DvMarginHasShortAndThinWording()
        {
            var shortIssue = new Issue { code = "dv-margin", msg = "About 300 m/s short of LEO 400 km. ..." };
            var thinIssue = new Issue { code = "dv-margin", msg = "Only 3% \u0394v margin for LEO 400 km. ..." };
            Assert.AreEqual(DesignerValidation.KidWording["dv-margin"], DesignerValidation.Wording(shortIssue, "standard"));
            Assert.AreEqual(DesignerValidation.DvMarginThin, DesignerValidation.Wording(thinIssue, "standard"));
        }

        [Test]
        public void BoosterAwareAndUpperStageWording()
        {
            StringAssert.Contains("booster", DesignerValidation.KidWording["zero-engines"]);
            StringAssert.Contains("booster", DesignerValidation.KidWording["zero-propellant"]);
            StringAssert.Contains("upper stages", DesignerValidation.KidWording["weak-upper"]);
            StringAssert.Contains("the one under it", DesignerValidation.KidWording["top-heavy"]);
            StringAssert.Contains("grid fins", DesignerValidation.KidWording["landing-hardware"]);
        }

        [Test] public void SchemaUsesExperienceKidSchema() => Assert.AreEqual(Experience.KidSchema, DesignerValidation.KidWording["schema"]);

        [Test] public void UnknownCodeFallsBackToFriendlyLine() =>
            Assert.AreEqual(DesignerValidation.KidFallback, DesignerValidation.Wording(new Issue { code = "new-rule", msg = "x" }, "simple"));

        [TestCase(0, 0, "simple", "")]
        [TestCase(1, 0, "simple", "1 thing to fix")]
        [TestCase(3, 0, "simple", "3 things to fix")]
        [TestCase(1, 2, "engineer", "1 block · 2 warnings")]
        [TestCase(2, 1, "engineer", "2 blocks · 1 warning")]
        [TestCase(0, 0, "engineer", "")]
        public void Chip(int b, int w, string layer, string want) => Assert.AreEqual(want, DesignerValidation.Chip(b, w, layer));

        static EngineeringReport Report(bool ok, int blocks, int warnings)
        {
            var r = new EngineeringReport { ok = ok };
            for (int i = 0; i < blocks; i++) r.blocks.Add(new Issue { code = "twr-low", tab = "engines", msg = "Liftoff thrust-to-weight is 0.80; ..." });
            for (int i = 0; i < warnings; i++) r.warnings.Add(new Issue { code = "dv-margin", tab = "propellants", msg = "Only 2% ..." });
            return r;
        }

        [Test]
        public void BlockDisablesLaunchWithReason()
        {
            var s = DesignerValidation.Evaluate(Report(false, 1, 0), "simple");
            Assert.IsFalse(s.launchEnabled);
            Assert.AreEqual("Fix first: " + DesignerValidation.KidWording["twr-low"], s.reason);
            Assert.AreEqual("1 thing to fix", s.chip);
        }

        [Test]
        public void WarningsNeverDisableLaunch()
        {
            var s = DesignerValidation.Evaluate(Report(true, 0, 2), "engineer");
            Assert.IsTrue(s.launchEnabled);
            Assert.AreEqual("", s.reason);
            Assert.AreEqual("0 blocks · 2 warnings", s.chip);
            Assert.AreEqual("", DesignerValidation.Evaluate(Report(true, 0, 2), "simple").chip); // simple hides warnings
        }

        [Test]
        public void StockPresetCanLaunch()
        {
            var d = SliceData.Catalog.Preset("stock").Clone();
            Assert.IsTrue(DesignerValidation.Evaluate(SliceData.Catalog, d, "simple").launchEnabled);
        }

        [Test]
        public void OverloadedStageOneBlocksLaunch()
        {
            var d = SliceData.Catalog.Preset("stock").Clone();
            d.stages[0].engineCount = 1; d.stages[0].propMass = 4_000_000;
            var s = DesignerValidation.Evaluate(SliceData.Catalog, d, "simple");
            Assert.IsFalse(s.launchEnabled);
            StringAssert.StartsWith("Fix first: ", s.reason);
        }

        [Test]
        public void IssueMapsToControl()
        {
            Assert.AreEqual("engines", DesignerValidation.ControlFor(new Issue { tab = "engines" }));
            Assert.AreEqual("prop", DesignerValidation.ControlFor(new Issue { tab = "propellants", index = 0 }));
            Assert.AreEqual("preset", DesignerValidation.ControlFor(new Issue { tab = "engines", index = 1 }));
            Assert.AreEqual("preset", DesignerValidation.ControlFor(new Issue { tab = "payload" }));
        }
    }
}
