// FlightCameraControl: what each camera tap does, on the pad and at altitude (owner, Oct 2026: "The camera
// buttons need to work better"; TestFlight recording at T+116-126, 55-65 km: "Follow rocket" up, "Cam: Auto").
using System;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightCameraControlTests
    {
        static readonly FlightViewMode[] Chasing =
        {
            FlightViewMode.Auto, FlightViewMode.Above, FlightViewMode.Below, FlightViewMode.Chase,
        };

        [Test]
        public void FollowRocketAlwaysBringsTheCameraBack()
        {
            foreach (var mode in Chasing)
            foreach (bool inRange in new[] { true, false })
            {
                var tap = FlightCameraControl.Follow(mode, orbited: true, lastChase: mode, padInRange: inRange);
                Assert.That(tap.Kind, Is.EqualTo(FollowTapKind.Recentre), mode + " orbited: recentre");
                Assert.That(tap.View, Is.EqualTo(mode), mode + " keeps the player's view");
                Assert.That(tap.Recentre, Is.True);
                Assert.That(FlightView.Following(true, tap.View), Is.True, mode + " following after the tap");
            }
            foreach (var last in new[] { FlightViewMode.Auto, FlightViewMode.Chase, FlightViewMode.Pad })
            {
                var tap = FlightCameraControl.Follow(FlightViewMode.Pad, false, last, true);
                Assert.That(tap.Kind, Is.EqualTo(FollowTapKind.SwitchView));
                Assert.That(tap.View, Is.EqualTo(last == FlightViewMode.Pad ? FlightViewMode.Auto : last), "from Pad back to " + last);
                Assert.That(tap.Toast, Is.EqualTo(FlightView.Toast(tap.View)));
            }
        }

        [Test]
        public void FollowingLetsGoToThePadOnlyWhileThePadCanSeeTheRocket()
        {
            foreach (var mode in Chasing)
            {
                var near = FlightCameraControl.Follow(mode, false, mode, padInRange: true);
                Assert.That(near.View, Is.EqualTo(FlightViewMode.Pad), mode + " on the pad: lets go to the ground camera");
                Assert.That(FlightView.FollowCaption(FlightView.Following(true, near.View)), Is.EqualTo(FlightView.FollowLabel), "the label flips with it");

                // 60 km: the old toggle cut to Pad (a dot in the dark). Now nothing changes, and a toast says why.
                var far = FlightCameraControl.Follow(mode, false, mode, padInRange: false);
                Assert.That(far.Kind, Is.EqualTo(FollowTapKind.AlreadyFollowing), mode.ToString());
                Assert.That(far.View, Is.EqualTo(mode), mode + " stays");
                Assert.That(far.Recentre, Is.False);
                Assert.That(far.Toast, Is.EqualTo(FlightCameraControl.AlreadyFollowingToast));
                Assert.That(FlightView.FollowTip(true, false), Does.Not.Contain("pad"), "the tip does not promise the pad");
            }
        }

        [Test]
        public void TheLabelAlwaysMatchesWhatTheNextTapDoes()
        {
            foreach (FlightViewMode mode in Enum.GetValues(typeof(FlightViewMode)))
            foreach (bool orbited in new[] { false, true })
            foreach (bool inRange in new[] { false, true })
            {
                if (!FlightCameraControl.Available(mode, inRange)) continue;
                bool following = FlightView.Following(!orbited, mode);
                var tap = FlightCameraControl.Follow(mode, orbited, FlightViewMode.Chase, inRange);
                string where = mode + (orbited ? " orbited" : "") + (inRange ? " near" : " far");
                if (!following)
                    Assert.That(FlightView.Following(true, tap.View), Is.True, where + ": \"Follow rocket\" always ends following");
                else if (tap.Kind == FollowTapKind.SwitchView)
                    Assert.That(FlightView.Following(true, tap.View), Is.False, where + ": \"Following\" lets go");
                else
                    Assert.That(tap.Toast, Is.Not.Null.And.Not.Empty, where + ": a no-op tap still says something");
                // Whatever the tap, the camera never lands on a view it cannot use.
                Assert.That(FlightCameraControl.Available(tap.View, inRange), Is.True, where + " lands on a usable view");
            }
        }

        [Test]
        public void PadIsDisabledOutOfRangeAndALivePadFallsBackWithoutFighting()
        {
            Assert.That(FlightCameraControl.Available(FlightViewMode.Pad, true), Is.True);
            Assert.That(FlightCameraControl.Available(FlightViewMode.Pad, false), Is.False);
            foreach (var mode in Chasing)
            {
                Assert.That(FlightCameraControl.Available(mode, false), Is.True, mode + " rides with the rocket");
                Assert.That(FlightCameraControl.Fallback(mode, FlightViewMode.Auto, false), Is.Null, mode + ": the player's choice stands");
                Assert.That(FlightCameraControl.Fallback(mode, FlightViewMode.Auto, true), Is.Null);
            }
            Assert.That(FlightCameraControl.Fallback(FlightViewMode.Pad, FlightViewMode.Below, true), Is.Null, "Pad in range stays");
            Assert.That(FlightCameraControl.Fallback(FlightViewMode.Pad, FlightViewMode.Below, false), Is.EqualTo(FlightViewMode.Below), "back to the player's chase");
            Assert.That(FlightCameraControl.Fallback(FlightViewMode.Pad, FlightViewMode.Pad, false), Is.EqualTo(FlightViewMode.Auto));
            Assert.That(FlightCameraControl.FallbackToast(FlightViewMode.Auto), Does.EndWith("Camera: Auto"));
            Assert.That(FlightCameraControl.RememberChase(FlightViewMode.Chase, FlightViewMode.Pad), Is.EqualTo(FlightViewMode.Chase));
            Assert.That(FlightCameraControl.RememberChase(FlightViewMode.Chase, FlightViewMode.Above), Is.EqualTo(FlightViewMode.Above));
        }

        [Test]
        public void CSkipsAViewThatCannotSeeTheRocket()
        {
            Assert.That(FlightCameraControl.Next(FlightViewMode.Below, true), Is.EqualTo(FlightViewMode.Pad));
            Assert.That(FlightCameraControl.Next(FlightViewMode.Below, false), Is.EqualTo(FlightViewMode.Auto));
            var seen = new System.Collections.Generic.HashSet<FlightViewMode>();
            var m = FlightViewMode.Auto;
            for (int i = 0; i < 10; i++) { m = FlightCameraControl.Next(m, false); seen.Add(m); }
            Assert.That(seen, Does.Not.Contain(FlightViewMode.Pad));
            Assert.That(seen.Count, Is.EqualTo(4));
        }

        [Test]
        public void DragsAreMeasuredInPointsPastTheSlop()
        {
            Assert.That(FlightCameraControl.PixelsPerPoint(1290, 430 * 3, 3), Is.EqualTo(3).Within(1e-9), "3x iPhone, panel in px");
            Assert.That(FlightCameraControl.PixelsPerPoint(1290, 430, 1), Is.EqualTo(3).Within(1e-9), "3x iPhone, panel in points");
            Assert.That(FlightCameraControl.PixelsPerPoint(1280, 1280, 1), Is.EqualTo(1).Within(1e-9), "web");
            Assert.That(FlightCameraControl.PixelsPerPoint(0, 430, 1), Is.EqualTo(1), "unknown");
            Assert.That(FlightCameraControl.PixelsPerPoint(1290, double.NaN, 1), Is.EqualTo(1), "no layout yet");

            FlightCameraControl.PastSlop(6, 6, out double x, out double y);
            Assert.That(x == 0 && y == 0, Is.True, "a wobbly tap does not orbit");
            FlightCameraControl.PastSlop(20, 0, out x, out y);
            Assert.That(x, Is.EqualTo(20 - FlightCameraControl.SlopPt).Within(1e-9), "crossing the slop does not jump by the slop");
            FlightCameraControl.PastSlop(-30, 40, out x, out y);
            Assert.That(Math.Sqrt(x * x + y * y), Is.EqualTo(50 - FlightCameraControl.SlopPt).Within(1e-9));
            Assert.That(x / y, Is.EqualTo(-30.0 / 40.0).Within(1e-9), "same direction");
            // A 3x thumb wobble of 15 px (5 pt) is inside the slop; it was past the old 14 px one.
            Assert.That(15 / FlightCameraControl.PixelsPerPoint(1290, 430, 1), Is.LessThan(FlightCameraControl.SlopPt));
        }

        [Test]
        public void ThePadCameraLosesTheRocketHighUpButNotOnThePad()
        {
            FlightPoseInput At(double alt, double downrange) => new FlightPoseInput
            {
                Mode = FlightViewMode.Auto, Vehicle = new PoseVec(downrange, alt, 0), Up = new PoseVec(0.5, 0.866, 0), Height = 40,
                Altitude = alt, GroundY = 0, Pad = new PoseVec(0, 0, 0), TrenchBearingDeg = 90, BaseFovDeg = 50,
            };
            Assert.That(FlightViewPose.PadInRange(At(0, 0)), Is.True, "on the pad");
            Assert.That(FlightViewPose.PadInRange(At(2000, 300)), Is.True, "early ascent");
            Assert.That(FlightViewPose.PadInRange(At(60000, 40000)), Is.False, "60 km: the Pad view is a dot");
            // Whatever the live view, the answer is the Pad pose's own Far flag.
            var p = At(60000, 40000);
            p.Mode = FlightViewMode.Pad;
            Assert.That(FlightViewPose.PadInRange(At(60000, 40000)), Is.EqualTo(!FlightViewPose.Pose(p).Far));
        }
    }
}
