// The Home tests in this assembly were written for the full Home (Design first, Codex, Thrum, credits, stats). Starter
// Home (feat(home-simple)) hides those while their home.* flags are off, so this runs the namespace with all five on.
// IosPortraitHomeDeviceTests picks its own flags per case (Starter and full) and puts these back after each test.
using Kestrel.Core.Features;
using Kestrel.Game.Shell;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    [SetUpFixture]
    public class FullHomeFlagsFixture
    {
        FeatureFlags saved;
        bool savedLoaded;

        [OneTimeSetUp]
        public void AllHomeFlagsOn()
        {
            saved = FeatureFlags.Active;
            savedLoaded = HomeFlagsBoot.Loaded;
            HomeFlagsBoot.Loaded = true;   // Home must not swap in data/features.json mid-suite
            FeatureFlags.Active = HomeFeatures.With(HomeShow.Full);
        }

        [OneTimeTearDown]
        public void Restore()
        {
            FeatureFlags.Active = saved;
            HomeFlagsBoot.Loaded = savedLoaded;
        }
    }
}
