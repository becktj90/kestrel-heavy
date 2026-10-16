// Fix it applies the change the warning already tells the player to make.
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class AdvisoryFixTests
    {
        [Test]
        public void PayloadG_StepsTheTopStageToAGentlerEngine()
        {
            var d = HangarFixtures.Template("workhorse");
            int top = d.Arr("stages").Count - 1;
            var stage = (JsonObject)d.Arr("stages")[top];
            string prop = stage.Str("propellant");
            Engine hot = null;
            int choices = 0;
            foreach (var engine in Golden.Catalog.Engines)
            {
                if (engine.propellant != prop) continue;
                choices++;
                if (hot == null || Thrust(engine) > Thrust(hot)) hot = engine;
            }
            Assert.Greater(choices, 1, "the top propellant needs a gentler alternative");
            Assert.IsTrue(HangarEditor.SetEngine(Golden.Catalog, d, PartRef.Stage(top), hot.id).changed || stage.Str("engine") == hot.id);
            double before = Thrust(Golden.Catalog.EngineById[stage.Str("engine")]);
            var advisory = new Advisory { code = "payload-g", part = PartRef.Payload, kid = "Use gentler engines on the top stage." };
            Assert.IsTrue(AdvisoryFix.CanFix(Golden.Catalog, d, advisory));
            var edit = AdvisoryFix.Apply(Golden.Catalog, d, advisory);
            Assert.IsTrue(edit.changed);
            Assert.Less(Thrust(Golden.Catalog.EngineById[stage.Str("engine")]), before);
        }

        [Test]
        public void CryoComposite_BecomesAMetalTank()
        {
            var d = HangarFixtures.Template("workhorse");
            Assert.IsTrue(HangarEditor.SetPropellant(Golden.Catalog, d, PartRef.Stage(0), "hydrolox").changed);
            Assert.IsTrue(HangarEditor.SetMaterial(Golden.Catalog, d, PartRef.Stage(0), "composite").changed);
            var advisory = new Advisory { code = "cryo-composite", part = PartRef.Stage(0) };
            Assert.IsTrue(AdvisoryFix.Apply(Golden.Catalog, d, advisory).changed);
            Assert.AreEqual("al-li", ((JsonObject)d.Arr("stages")[0]).Str("material"));
            Assert.IsFalse(AdvisoryFix.CanFix(Golden.Catalog, d, advisory));
        }

        [Test]
        public void ToCheckRowWiresFixIt()
        {
            string panel = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "Hangar", "HangarPanel.cs"));
            StringAssert.Contains("To check", panel);
            StringAssert.Contains("\"Fix it\"", panel);
            StringAssert.Contains("advisory-fix-", panel);
            StringAssert.Contains("FixAdvisory", panel);
            StringAssert.Contains("\"Show\"", panel);
            StringAssert.Contains("\"More\"", panel);
        }

        [Test]
        public void UnknownCodesStayUntouched()
        {
            var d = HangarFixtures.Template("workhorse");
            string before = HangarSession.Snapshot(d);
            Assert.IsFalse(AdvisoryFix.Apply(Golden.Catalog, d, new Advisory { code = "near-budget", part = PartRef.Stage(0) }).changed);
            Assert.IsFalse(AdvisoryFix.Apply(Golden.Catalog, d, new Advisory { code = "schema", part = PartRef.Stage(0) }).changed);
            Assert.AreEqual(before, HangarSession.Snapshot(d));
        }

        static double Thrust(Engine engine) => engine.thrustVac > 0 ? engine.thrustVac : engine.thrustSL;
    }
}
