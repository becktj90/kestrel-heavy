// The Starter screen wiring the preview checks rely on (TestFlight build 204: knob changes never showed on the rocket).
// StarterPreviewTests and Kestrel.Game.Tests.StarterPreviewDeviceTests prove the framing and the rebuilt meshes; these
// keep SliceApp on the same calls: the Starter setting loads past the credits gate, every rebuild hands the vehicle
// bounds to StarterPreviewCamera, Starter framing uses the preview box instead of the whole-pad fit, the preview camera
// owns the view only on the pad, and the bells carry their fuel for the BellFinish cue.
using System.IO;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterPreviewWiringTests
    {
        static string Source() => File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "SliceApp.cs"));

        static string Body(string src, string signature)
        {
            int at = src.IndexOf(signature, System.StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), signature + " is in SliceApp.cs");
            int open = src.IndexOf('{', at);
            int depth = 0;
            for (int i = open; i < src.Length; i++)
            {
                if (src[i] == '{') depth++;
                else if (src[i] == '}' && --depth == 0) return src.Substring(open, i - open + 1);
            }
            Assert.Fail("unbalanced " + signature);
            return "";
        }

        [Test]
        public void StarterSettingLoadsPastTheCreditsGate()
        {
            string load = Body(Source(), "void LoadStarterDesign()");
            StringAssert.Contains("StarterSettingLoad.Apply(session", load, "the Starter setting is what the pad shows and Launch flies");
            StringAssert.DoesNotContain("session.Load(", load, "a gated Load silently keeps the old rocket");
            StringAssert.Contains("ApplyDesignFraming()", Body(Source(), "void OnStarterChanged(StarterRocket _)"), "a knob change reframes");
        }

        [Test]
        public void EveryRebuildFramesTheVehicleInThePreviewBox()
        {
            var src = Source();
            StringAssert.Contains("starterCam.SetVehicle(0f, r.height,", Body(src, "void BuildRocket()"), "the rebuilt vehicle's bounds drive the Starter framing");
            string framing = Body(src, "void ApplyDesignFraming()");
            int starterCam = framing.IndexOf("starterCam.SetPreview(starter.Frame.Rocket", System.StringComparison.Ordinal);
            int hangar = framing.IndexOf("hangarCam.SetBottomVisible", System.StringComparison.Ordinal);
            Assert.That(starterCam, Is.GreaterThan(0), "Starter frames the StarterFrame.Rocket box");
            Assert.That(starterCam, Is.LessThan(hangar), "before the whole-pad fit, which made the rocket a sliver");
        }

        [Test]
        public void ThePreviewCameraOwnsTheViewOnlyOnThePad()
        {
            var src = Source();
            string use = Body(src, "bool UseStarterCamera()");
            StringAssert.Contains("FlightHudVisible) return false", use, "never in flight");
            StringAssert.Contains("hangarCam.enabled = false", use);
            StringAssert.Contains("UseStarterCamera()", Body(src, "void ApplyStarterMode()"));
            string enter = Body(src, "void EnterFlight()");
            int off = enter.IndexOf("starterCam.enabled = false", System.StringComparison.Ordinal);
            Assert.That(off, Is.GreaterThan(0), "liftoff takes the camera back");
            Assert.That(off, Is.LessThan(enter.IndexOf("hangarCam.ApplyPose()", System.StringComparison.Ordinal)), "before the chase offset is sampled");
        }

        [Test]
        public void BellsCarryTheirFuel()
        {
            StringAssert.Contains("Propellant = e.Propellant(design)", Body(Source(), "void AddBells(RocketMeshBuilder.Result r)"));
            string stage = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "FlightStageShow.cs"));
            StringAssert.Contains("Propellant = e.Propellant(design)", stage, "the staging copy keeps the same finish");
        }
    }
}
