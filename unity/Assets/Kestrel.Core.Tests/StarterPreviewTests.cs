// Starter preview (TestFlight build 204: "When I change the design nothing changes with the rocket"). Pure checks
// that run under dotnet and in EditMode: (1) the preview framing puts the vehicle at 55-70% of the sheet-free
// preview box (StarterFrame.Rocket), fully inside with margin, at 335x720, 390x844, 471x1024 and web 1280x800, for
// every KH-3 step and a sweep of rocket shapes; (2) the KH-3 click-through (default, 3 stages, 1 stage, kerosene,
// max fuel, min fuel) changes the built stack in the expected direction; (3) a Starter setting loads even when the
// credits gate would refuse it (the refused load kept the old rocket on the pad); (4) the reframe eases, never snaps.
// The Unity render-level twin is Kestrel.Game.Tests.StarterPreviewDeviceTests (real meshes, ui-captures PNGs).
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterPreviewTests
    {
        public struct Size { public string Name; public float W, H, Top, Bottom; public bool Wide; public override string ToString() => Name; }

        /// <summary>Panel points (web landscape after WebPanelScale) and safe insets for the four preview sizes.</summary>
        public static IEnumerable<Size> Sizes()
        {
            yield return new Size { Name = "335x720", W = 335, H = 720, Top = 20 };
            yield return new Size { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34 };
            yield return new Size { Name = "471x1024", W = 471, H = 1024, Top = 59, Bottom = 34 };
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float s = WebLandscapeType.PanelScale(1280, 800, rw, rh, m);
            yield return new Size { Name = "1280x800", W = (float)Math.Round(1280 / s), H = (float)Math.Round(800 / s), Wide = true };
        }

        /// <summary>KH-3 click-through, cumulative: each step changes one knob from the step before.</summary>
        public static readonly string[] Steps = { "default", "3-stages", "1-stage", "kerosene", "max-fuel", "min-fuel" };

        public static void ApplyStep(StarterRocket r, string step)
        {
            switch (step)
            {
                case "3-stages": r.Stages = 3; break;
                case "1-stage": r.Stages = 1; break;
                case "kerosene": r.Engine = StarterEngine.Kerosene; break;
                case "max-fuel": r.FuelNotch = StarterRocket.FuelNotches - 1; break;
                case "min-fuel": r.FuelNotch = 0; break;
            }
        }

        public static StarterFrame Frame(Size s, StarterRocket r)
        {
            StarterScreen.Readiness(r, out var badge, out var rest);
            var input = new StarterLayoutInput
            {
                Width = s.W, Height = s.H, SafeTop = s.Top, SafeBottom = s.Bottom, Wide = s.Wide, ReserveExit = s.Wide,
                BoostersVisible = r.BoostersVisible, Title = StarterScreen.Title(r.Mission), Subtitle = StarterScreen.Subtitle(r.Mission),
                Hint = StarterScreen.Hint(r, StarterKnob.None), Badge = badge, Readiness = rest,
            };
            return StarterLayout.Place(input);
        }

        /// <summary>Stack height from the same stations RocketMeshBuilder uses for the tanks (bells and thrust sections
        /// left out): the sum of the tank lengths, the interstages and the fairing.</summary>
        static double TankSum(Design d)
        {
            double sum = 0;
            foreach (var s in d.stages) sum += DesignMath.SizeTank(StarterFixture.Catalog, s.propellant, s.propMass, s.diameter).length;
            return sum;
        }

        static double StackApprox(Design d)
        {
            double y = TankSum(d);
            for (int i = 0; i < d.stages.Count; i++)
            {
                double r = d.stages[i].diameter / 2;
                y += 0.6 * r + 3.0; // thrust section + a bell-ish allowance
                if (i < d.stages.Count - 1) y += 1.2 * Math.Max(r, d.stages[i + 1].diameter / 2);
            }
            return y + d.fairingLength;
        }

        const double Fov = 50.0, Margin = 0.04;

        [Test]
        public void Kh3ClickThrough_ChangesTheStack_InTheExpectedDirection()
        {
            var r = StarterFixture.Rocket("m3");
            var tanks = new Dictionary<string, double>();
            var stages = new Dictionary<string, int>();
            var fuel = new Dictionary<string, string>();
            var snaps = new HashSet<string>();
            foreach (var step in Steps)
            {
                ApplyStep(r, step);
                var json = r.BuildDesign();
                var d = Design.FromJson(json);
                tanks[step] = TankSum(d);
                stages[step] = d.stages.Count;
                fuel[step] = d.stages[0].propellant + "/" + d.stages[0].engine;
                Assert.IsTrue(snaps.Add(HangarSession.Snapshot(json)), step + ": every knob change is a different design");
            }
            Assert.AreEqual(2, stages["default"]);
            Assert.AreEqual(3, stages["3-stages"]);
            Assert.AreEqual(1, stages["1-stage"]);
            Assert.Greater(tanks["3-stages"], tanks["default"], "a third stage makes the stack taller");
            Assert.Less(tanks["1-stage"], tanks["default"], "one stage makes it shorter");
            Assert.AreNotEqual(fuel["1-stage"], fuel["kerosene"], "kerosene swaps the first-stage fuel and engine");
            Assert.That(fuel["kerosene"], Does.StartWith("kerolox/"));
            Assert.Greater(Math.Abs(tanks["kerosene"] - tanks["1-stage"]), 1e-3, "kerosene is denser, so the tank length changes");
            Assert.Greater(tanks["max-fuel"], tanks["kerosene"] * 1.1, "the top fuel notch is +20%: longer tanks");
            Assert.Less(tanks["min-fuel"], tanks["max-fuel"] * 0.75, "the bottom notch is -20%: shorter tanks");
        }

        [Test]
        public void Kh3EveryStep_FramedAt55To70PercentOfThePreview_InsideWithMargin()
        {
            var fails = new List<string>();
            foreach (var size in Sizes())
            {
                var r = StarterFixture.Rocket("m3");
                foreach (var step in Steps)
                {
                    ApplyStep(r, step);
                    var d = Design.FromJson(r.BuildDesign());
                    double radius = d.stages[0].diameter / 2;
                    foreach (var s in d.stages) radius = Math.Max(radius, s.diameter / 2);
                    var subject = new StarterPreviewSubject(0, StackApprox(d), radius);
                    var f = Frame(size, r);
                    Check(fails, size + " " + step, subject, f.Rocket, size.W, size.H, size.W / size.H, StarterPreviewFraming.MinFill);
                }
            }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        [Test]
        public void AnyRocketShape_FitsThePreview_OnEverySize()
        {
            var fails = new List<string>();
            var r = StarterFixture.Rocket("m3");
            foreach (var size in Sizes())
            {
                var f = Frame(size, r);
                foreach (double h in new[] { 12.0, 25, 40, 60, 90, 130 })
                    foreach (double rad in new[] { 0.6, 1.5, 3, 5, 8 })
                    {
                        var subject = new StarterPreviewSubject(0, h, rad);
                        // A squat rocket in a narrow box is width-bound: its height fill may drop, but never under 50%
                        // unless it is wider than it is tall on screen.
                        bool squat = rad * 2 * 1.4 > h * (f.Rocket.W / f.Rocket.H);
                        Check(fails, size + " h" + h + " r" + rad, subject, f.Rocket, size.W, size.H, size.W / size.H, squat ? 0.0 : 0.5);
                    }
            }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        static void Check(List<string> fails, string where, StarterPreviewSubject s, HudBox box, double w, double h, double aspect, double minFill)
        {
            var pose = StarterPreviewFraming.Solve(s, box, w, h, aspect, Fov);
            if (!pose.Valid) { fails.Add(where + ": no pose"); return; }
            double fill = StarterPreviewFraming.Fill(pose, s, box, w, h, aspect, Fov, Margin, out bool inside);
            if (fill < minFill - 1e-3 || fill > StarterPreviewFraming.MaxFill + 1e-3) fails.Add(where + ": fill " + fill.ToString("F3"));
            if (!inside) fails.Add(where + ": not inside the preview box with " + Margin + " margin (fill " + fill.ToString("F3") + ")");
            if (pose.PosY <= 0.5) fails.Add(where + ": camera below the deck " + pose.PosY.ToString("F2"));
        }

        [Test]
        public void TallerRocket_BacksTheCameraOff_SoTheFillStaysPut()
        {
            var size = new Size { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34 };
            var f = Frame(size, StarterFixture.Rocket("m3"));
            var a = StarterPreviewFraming.Solve(new StarterPreviewSubject(0, 40, 1.8), f.Rocket, size.W, size.H, size.W / size.H, Fov);
            var b = StarterPreviewFraming.Solve(new StarterPreviewSubject(0, 60, 1.8), f.Rocket, size.W, size.H, size.W / size.H, Fov);
            Assert.Greater(b.Distance, a.Distance * 1.3, "reframed after the change");
            Assert.Greater(b.AimY, a.AimY, "aim follows the new middle");
            double fa = StarterPreviewFraming.Fill(a, new StarterPreviewSubject(0, 40, 1.8), f.Rocket, size.W, size.H, size.W / size.H, Fov, 0, out _);
            double fb = StarterPreviewFraming.Fill(b, new StarterPreviewSubject(0, 60, 1.8), f.Rocket, size.W, size.H, size.W / size.H, Fov, 0, out _);
            Assert.AreEqual(StarterPreviewFraming.TargetFill, fa, 0.01);
            Assert.AreEqual(StarterPreviewFraming.TargetFill, fb, 0.01);
        }

        [Test]
        public void Reframe_EasesWithoutOvershoot_AndReduceMotionSnaps()
        {
            var size = new Size { W = 390, H = 844, Top = 47, Bottom = 34 };
            var f = Frame(size, StarterFixture.Rocket("m3"));
            var from = StarterPreviewFraming.Solve(new StarterPreviewSubject(0, 30, 1.8), f.Rocket, size.W, size.H, size.W / size.H, Fov);
            var to = StarterPreviewFraming.Solve(new StarterPreviewSubject(0, 55, 1.8), f.Rocket, size.W, size.H, size.W / size.H, Fov);
            var now = from;
            double prev = now.Distance;
            now = StarterPreviewFraming.Step(now, to, 1 / 60.0, false);
            Assert.Less(now.Distance - from.Distance, (to.Distance - from.Distance) * 0.25, "no snap on the first frame");
            double t = 1 / 60.0;
            while (!StarterPreviewFraming.Same(now, to) && t < 2)
            {
                now = StarterPreviewFraming.Step(now, to, 1 / 60.0, false);
                Assert.GreaterOrEqual(now.Distance, prev - 1e-9, "monotonic");
                Assert.LessOrEqual(now.Distance, to.Distance + 1e-9, "no overshoot");
                prev = now.Distance; t += 1 / 60.0;
            }
            Assert.Less(t, 1.0, "settles in well under a second");
            Assert.IsTrue(StarterPreviewFraming.Same(StarterPreviewFraming.Step(from, to, 1 / 60.0, true), to), "Reduce Motion snaps");
            Assert.IsTrue(StarterPreviewFraming.Same(StarterPreviewFraming.Step(default, to, 1 / 60.0, false), to), "the first pose snaps");
        }

        [Test]
        public void StarterSetting_LoadsEvenWhenTheCreditsGateWouldRefuseIt()
        {
            var r = StarterFixture.Rocket("m3");
            var session = new HangarSession(StarterFixture.Catalog, null, r.BuildDesign());
            session.Afford = (_, __) => false;
            r.Stages = 3;
            var three = r.BuildDesign();
            // The old path: HangarSession.Load returns silently and the pad keeps the 2-stage rocket.
            session.Load(three, null, "KH-3");
            Assert.AreEqual(2, session.Design.Arr("stages").Count, "a refused Load keeps the previous rocket (the build-204 symptom)");
            JsonObject regifted = null;
            Assert.IsTrue(StarterSettingLoad.Apply(session, three, "KH-3", d => regifted = d));
            Assert.AreEqual(3, session.Design.Arr("stages").Count, "the Starter setting is what the pad shows and Launch flies");
            Assert.AreSame(session.Design, regifted, "the wallet is re-baselined on the loaded rocket");
            Assert.IsFalse(session.Afford(null, null), "the gate is put back for the full designer");
        }

        [Test]
        public void ThreeStages_AreAPaidPart_SoTheCreditsGateCouldRefuseThem()
        {
            string root = Golden.Root;
            var table = CreditsTable.Parse(File.ReadAllText(Path.Combine(root, "data", "credits.json")));
            var r = StarterFixture.Rocket("m3");
            long baseQuote = table.Quote(r.BuildDesign());
            r.Stages = 3;
            long threeQuote = table.Quote(r.BuildDesign());
            // KH-1 and KH-2 cleared once each, first-clear bonus included: what a player meeting KH-3 typically holds.
            long firstPass = 12 + 10 + 14 + 10;
            Assert.Greater(threeQuote - baseQuote, firstPass, "a third stage costs more than two first clears pay");
            TestContext.WriteLine("KH-3 quote " + baseQuote + ", 3 stages " + threeQuote + ", KH-1 + KH-2 first clears pay " + firstPass);
        }
    }
}
