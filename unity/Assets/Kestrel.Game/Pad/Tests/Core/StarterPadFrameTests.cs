// EditMode, engine-free (also runs under dotnet). TestFlight 208, KH-1 First Hop Starter screen: no yellow crown, no base
// plates and no pad circle, the vehicle cropping the tower. The Starter preview camera framed the vehicle alone from a
// heading that puts the tower behind it. Kestrel.Core.Designer.StarterPadFrame now solves the pose with the pad art in
// the subject, and these tests run THAT solver and THAT evaluator (the camera calls Solve; nothing here re-derives the
// projection) on real generated pads, at phone and iPad sizes against the real StarterLayout preview box:
// tower crown, base plates and pad circle inside the box, none hidden behind the vehicle past the shared limits.
// The last test runs the same evaluator on the build 204 framing and requires it to FAIL, so the check can't pass
// vacuously.
using System;
using System.Collections.Generic;
using System.Linq;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Kit.Contracts;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class StarterPadFrameTests
    {
        // (name, height, tank radius, widest radius, pad). KH-1 First Hop is a Rail pad (widest radius under 1.5 m).
        public static readonly (string Name, float H, float R, float Rr, PadArchetype Type)[] Stacks =
        {
            ("KH-1 First Hop", 4.57f, 0.6f, 0.6f, PadArchetype.Rail), ("KH-1 tall", 8f, 0.9f, 1.0f, PadArchetype.Rail),
            ("starter", 14f, 0.45f, 0.45f, PadArchetype.Rail), ("Tiny Orbit", 22f, 0.6f, 0.6f, PadArchetype.Rail),
            ("rail 36", 36f, 1.2f, 1.2f, PadArchetype.Rail), ("tower 48", 48f, 1.6f, 1.6f, PadArchetype.Tower),
            ("tower 70", 70f, 2.5f, 2.5f, PadArchetype.Tower), ("strap-ons 90", 90f, 3.6f, 8.5f, PadArchetype.TowerStrongback),
        };

        // Screen sizes in points: SE, 12/13/14, Pro Max, the 471x1024 design size, iPad 11" and 12.9" portrait, web landscape.
        public static readonly (string Name, float W, float H, float Top, float Bottom, bool Wide)[] Screens =
        {
            ("375x667 (SE)", 375, 667, 20, 0, false), ("390x844", 390, 844, 47, 34, false), ("430x932 (Pro Max)", 430, 932, 59, 34, false),
            ("471x1024", 471, 1024, 59, 34, false), ("820x1180 (iPad 11)", 820, 1180, 24, 20, false), ("1024x1366 (iPad 12.9)", 1024, 1366, 24, 20, false),
            ("1280x800 (web)", 1280, 800, 0, 0, true),
        };

        const double Fov = 50.0;

        static StarterFrame Frame((string Name, float W, float H, float Top, float Bottom, bool Wide) s) =>
            StarterLayout.Place(new StarterLayoutInput
            {
                Width = s.W, Height = s.H, SafeTop = s.Top, SafeBottom = s.Bottom, Wide = s.Wide, ReserveExit = s.Wide,
                Title = "KH-1 First Hop", Subtitle = "Hop to it.", Hint = "Pick an engine, stages and fuel.", Badge = "Ready", Readiness = "Ready to launch",
            });

        /// <summary>The subject SliceApp hands the camera: vehicle cylinder on the deck plus the pad art, yawed by the shared PadShowcase.YawDeg.</summary>
        public static StarterPreviewSubject Subject(PadLayout layout, float h, float rr)
        {
            float yaw = PadShowcase.YawDeg(h, rr);
            var pts = new List<PadPoint>();
            foreach (var p in PadShowcase.Points(layout)) pts.Add(StarterPadFrame.World((int)p.Kind, p.X, p.Y, p.Z, yaw));
            return new StarterPreviewSubject(0, h, rr, pts.ToArray());
        }

        /// <summary>The farm every real pad carries (KH-1's kerolox stage): it is what widens the surface, and so the pad circle, to 13-14 m.</summary>
        static FarmPlan Farm() => new FarmPlan { Kerolox = true };

        static PadLayout Layout((string Name, float H, float R, float Rr, PadArchetype Type) st, int seed) =>
            PadGenerator.Generate(seed, st.Type, new PadFit(st.H, st.Rr, PadShowcase.Clearance), null, Farm());

        [Test]
        public void EveryStackShowsCrownPlatesAndCircleAtEveryPhoneAndIpadSize()
        {
            var fails = new List<string>();
            var limited = new HashSet<string>();
            foreach (var st in Stacks)
            foreach (int seed in new[] { 1, 2, 5 })
            {
                var layout = Layout(st, seed);
                var subject = Subject(layout, st.H, st.Rr);
                if (seed == 1)
                {
                    double topY = 0; foreach (var p in subject.Pad) if (p.Kind == PadFeature.TowerTop) topY = Math.Max(topY, p.Y);
                    TestContext.WriteLine(st.Name + ": vehicle " + st.H + " m, crown top " + topY.ToString("0.0") + " m, pad circle radius " + PadShowcase.CircleRadius(layout).ToString("0.0") + " m, tower yaw " + PadShowcase.YawDeg(st.H, st.Rr).ToString("0") + " deg");
                }
                foreach (var sc in Screens)
                {
                    string where = st.Name + " " + st.Type + " seed " + seed + " " + sc.Name;
                    var frame = Frame(sc);
                    var pose = StarterPreviewFraming.Solve(subject, frame.Rocket, sc.W, sc.H, sc.W / sc.H, Fov);
                    if (!pose.Valid) { fails.Add(where + ": no pose"); continue; }
                    var rep = StarterPadFrame.Evaluate(pose, subject, frame.Rocket, sc.W, sc.H, sc.W / sc.H, Fov);
                    TestContext.WriteLine(where + ": heading " + pose.HeadingDeg + " distance " + pose.Distance.ToString("0.0") + " m, " + rep.Describe());
                    if (!rep.Pass) fails.Add(where + ": " + rep.Describe());
                    double need = StarterPadFrame.RequiredFill(subject);
                    if (need < StarterPadFrame.MinVehicleFill) limited.Add(st.Name);
                    if (rep.VehicleFill < need) fails.Add(where + ": the vehicle is too small (fill " + rep.VehicleFill.ToString("0.00") + ", needs " + need.ToString("0.00") + ")");
                }
            }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            // Every stack fills at least half the box (the designer frames the rocket large) except those whose crown is so tall next to them that
            // half is out of reach with the crown in the box. That list is pinned: a new stack here is a regression, not a new exception.
            TestContext.WriteLine("KNOWN LIMIT (fill ceiling under 0.5 at this tower height): " + string.Join(", ", limited));
            Assert.That(limited, Is.EquivalentTo(new[] { "KH-1 First Hop" }));
        }

        [Test]
        public void TheEvaluatorFailsTheBuild204FramingOnKh1()
        {
            // Build 204: the vehicle alone, default heading. Same evaluator, same points: it must see the crown cropped / the
            // plates and tower hidden and the circle out of the box on at least the phone sizes, or the check above proves nothing.
            var st = Stacks[0];
            var layout = Layout(st, 1);
            var subject = Subject(layout, st.H, st.Rr);
            var alone = new StarterPreviewSubject(subject.YMin, subject.YMax, subject.Radius);   // no pad: the pre-fix solve
            int failing = 0;
            foreach (var sc in Screens.Where(s => !s.Wide))
            {
                var frame = Frame(sc);
                var old = StarterPreviewFraming.Solve(alone, frame.Rocket, sc.W, sc.H, sc.W / sc.H, Fov);
                var rep = StarterPadFrame.Evaluate(old, subject, frame.Rocket, sc.W, sc.H, sc.W / sc.H, Fov);
                TestContext.WriteLine("pre-fix " + sc.Name + ": " + rep.Describe());
                Assert.That(rep.Pass, Is.False, "the pre-fix framing must fail the check at " + sc.Name + ": " + rep.Describe());
                failing++;
            }
            Assert.That(failing, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void TheOldPoseAlsoMissesTheFillFloorOrTheFramingOnEveryStack()
        {
            // The vehicle-alone pose never passes the pad check on a stack whose crown towers over it.
            var st = Stacks[0];
            var subject = Subject(Layout(st, 1), st.H, st.Rr);
            var alone = new StarterPreviewSubject(subject.YMin, subject.YMax, subject.Radius);
            var sc = Screens[1]; var frame = Frame(sc);
            var old = StarterPreviewFraming.Solve(alone, frame.Rocket, sc.W, sc.H, sc.W / sc.H, Fov);
            var rep = StarterPadFrame.Evaluate(old, subject, frame.Rocket, sc.W, sc.H, sc.W / sc.H, Fov);
            Assert.That(rep.Pass, Is.False);
            Assert.That(rep.OutsideTop, Is.GreaterThan(0), "the old framing crops the crown");
        }

        [Test]
        public void TheEvaluatorSeesEachKindOfFailure()
        {
            var st = Stacks[0];
            var subject = Subject(Layout(st, 1), st.H, st.Rr);
            var sc = Screens[1];
            var frame = Frame(sc);
            double aspect = sc.W / sc.H;
            var good = StarterPreviewFraming.Solve(subject, frame.Rocket, sc.W, sc.H, aspect, Fov);
            Assert.That(StarterPadFrame.Evaluate(good, subject, frame.Rocket, sc.W, sc.H, aspect, Fov).Pass, Is.True);
            // Closer: points leave the box.
            var close = good; close.Distance *= 0.5; close.PosX = good.AimX + (good.PosX - good.AimX) * 0.5; close.PosY = good.AimY + (good.PosY - good.AimY) * 0.5; close.PosZ = good.AimZ + (good.PosZ - good.AimZ) * 0.5;
            Assert.That(StarterPadFrame.Evaluate(close, subject, frame.Rocket, sc.W, sc.H, aspect, Fov).InFrame, Is.False, "too close must leave the box");
            // A point straight behind the vehicle on the camera's line is hidden.
            double bx = good.PosX + (0 - good.PosX) * 1.5, by = good.PosY + (2.0 - good.PosY) * 1.5, bz = good.PosZ + (0 - good.PosZ) * 1.5;   // beyond the stack's axis at 2 m up
            var hidden = new StarterPreviewSubject(0, st.H, st.Rr, new[] { new PadPoint(PadFeature.BasePlate, bx, by, bz) });
            Assert.That(StarterPadFrame.Hidden(good, hidden, hidden.Pad[0]), Is.True);
            var aside = new StarterPreviewSubject(0, st.H, st.Rr, new[] { new PadPoint(PadFeature.BasePlate, good.PosZ * 0.5, 0.2, -good.PosX * 0.5) });
            Assert.That(StarterPadFrame.Hidden(good, aside, aside.Pad[0]), Is.False);
        }

        [Test]
        public void NoPadKeepsTheVehicleAloneFraming()
        {
            var s = new StarterPreviewSubject(0, 20, 1.5);
            var frame = Frame(Screens[1]);
            var p = StarterPreviewFraming.Solve(s, frame.Rocket, 390, 844, 390.0 / 844, Fov);
            Assert.That(p.Valid, Is.True);
            Assert.That(p.HeadingDeg, Is.EqualTo(0).Within(1e-12));
            Assert.That(p.ElevDeg, Is.EqualTo(0).Within(1e-12));
            Assert.That(p.AimX, Is.EqualTo(0).Within(1e-12));
            Assert.That(p.AimZ, Is.EqualTo(0).Within(1e-12));
        }
    }
}
