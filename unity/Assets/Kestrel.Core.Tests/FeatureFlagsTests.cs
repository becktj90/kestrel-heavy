// Phase 1 PR 3 feature flags. The key list comes from the plan; designer.* defaults are on, home.* are off (PR 6).
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Features;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FeatureFlagsTests
    {
        [Test]
        public void DataFile_ListsExactlyThePlanKeys_DesignerOn_HomeStarter_TierStarter()
        {
            var o = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "features.json")));
            CollectionAssert.AreEquivalent(FeatureKeys.All, o.Obj("flags").Order);
            Assert.AreEqual(18, FeatureKeys.All.Count);
            var f = FeatureFlags.Load(Golden.Root);
            Assert.AreEqual(ExperienceTier.Starter, f.Tier);
            var builtIn = new FeatureFlags();
            foreach (var k in FeatureKeys.All)
            {
                if (k.StartsWith("home."))
                {
                    // PR 6 (home-simple): the file ships the Starter Home. The built-in stays on, so a missing
                    // file falls back to the full Home rather than hiding things.
                    Assert.IsFalse(f.IsOn(k), k + " is off in the Starter Home");
                    continue;
                }
                Assert.IsTrue(f.IsOn(k), k + " stays on until PR 5");
                Assert.AreEqual(f.Default(k), builtIn.Default(k), k + " built-in default matches the file");
            }
            Assert.IsFalse(f.IsOn("designer.nope"));
        }

        [Test]
        public void OverridesAndFullDesigner_RaiseChanged()
        {
            var f = FeatureFlags.Parse("{\"tier\":\"builder\",\"flags\":{\"home.thrum\":true,\"designer.stepper\":false,\"home.codexTile\":false}}");
            Assert.AreEqual(ExperienceTier.Builder, f.Tier);
            Assert.IsTrue(f.IsOn(FeatureKeys.HomeThrum));
            int changes = 0;
            f.Changed += _ => changes++;
            f.SetOverride(FeatureKeys.HomeThrum, false);
            Assert.IsFalse(f.IsOn(FeatureKeys.HomeThrum));
            f.ShowFullDesigner = true;
            Assert.IsTrue(f.IsOn(FeatureKeys.DesignerStepper));
            Assert.IsFalse(f.IsOn(FeatureKeys.HomeCodexTile), "full designer leaves Home alone");
            f.ClearOverride(FeatureKeys.HomeThrum);
            Assert.IsTrue(f.IsOn(FeatureKeys.HomeThrum));
            f.Tier = ExperienceTier.Engineer;
            Assert.AreEqual(4, changes);
            Assert.AreEqual("engineer", FeatureFlags.TierId(f.Tier));
        }
    }
}
