// X-ray (docs/DESIGNER_TIERS_SPEC.md, "X-ray (all tiers)") on every mission starter: the parts list (nose cone on top,
// per stage tanks / engines / interstage, boosters when fitted), the tank fill shares (oxidizer + fuel = 1, the masses add
// up to the stage propellant), the landing reserve band only when a landing is planned, and the flight plan's planned
// times starting at liftoff and never going backwards. Then the X-ray layout at SE, Pro Max, iPad and web sizes.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterXrayTests
    {
        static DesignerTierText text;
        static DesignerTierText Text => text ??= DesignerTierText.Load(Golden.Root);
        static readonly Dictionary<string, XrayModel> Models = new Dictionary<string, XrayModel>();

        static XrayModel Model(string missionId)
        {
            if (!Models.TryGetValue(missionId, out var x)) Models[missionId] = x = StarterXray.Build(StarterFixture.Rocket(missionId), Text);
            return x;
        }

        [Test]
        public void EveryMissionStarter_PartsList_FillShares_LandingBand_AndThePlan()
        {
            var fails = new List<string>();
            var cat = StarterFixture.Catalog;
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = StarterFixture.Rocket(m.Id);
                var d = Design.FromJson(r.BuildDesign());
                var x = Model(m.Id);
                string w = m.Id + " ";
                int n = d.stages.Count;

                // Parts: nose cone, then from the top stage down tank, engines (and the interstage under each upper stage).
                var want = new List<string> { "fairing" };
                for (int s = n; s >= 1; s--)
                {
                    want.Add("tank-" + s); want.Add("engines-" + s);
                    if (s > 1) want.Add("interstage-" + (s - 1));
                }
                if (d.StrapOnCount > 0) want.Add("boosters");
                var got = x.Parts.ConvertAll(p => p.Id);
                if (string.Join(",", want) != string.Join(",", got)) fails.Add(w + "parts " + string.Join(",", got) + " want " + string.Join(",", want));
                foreach (var p in x.Parts)
                {
                    if (string.IsNullOrEmpty(p.Title) || string.IsNullOrEmpty(p.Line)) fails.Add(w + p.Id + " has no copy");
                    if (p.Title.Contains("{") || p.Line.Contains("{") || p.Numbers.Contains("{")) fails.Add(w + p.Id + " unfilled template");
                    if (string.IsNullOrEmpty(p.Numbers)) fails.Add(w + p.Id + " has no Engineer numbers");
                    if (!(p.LengthM > 0)) fails.Add(w + p.Id + " length " + p.LengthM);
                }

                var masses = Mass.StageMasses(cat, d);
                foreach (var p in x.Parts)
                {
                    if (p.Kind != XrayPartKind.Tank) continue;
                    var sm = masses[p.Stage - 1];
                    if (Math.Abs(p.OxShare + p.FuelShare - 1) > 1e-9) fails.Add(w + p.Id + " shares sum " + (p.OxShare + p.FuelShare));
                    if (p.OxShare < 0 || p.FuelShare <= 0) fails.Add(w + p.Id + " negative share");
                    if (Math.Abs(p.OxKg + p.FuelKg - sm.prop) > 1e-6 * Math.Max(1, sm.prop)) fails.Add(w + p.Id + " ox + fuel != propellant");
                    if (Math.Abs(p.PropKg - sm.prop) > 1e-9) fails.Add(w + p.Id + " propellant");
                    if (p.ReserveShare < 0 || p.ReserveShare >= 1) fails.Add(w + p.Id + " reserve share " + p.ReserveShare);
                    bool band = p.ReserveShare > 0;
                    bool planned = x.LandingPlanned && p.Stage == 1;
                    if (band != planned) fails.Add(w + p.Id + " landing band " + band + " but planned " + planned);
                    if (band && Math.Abs(p.ReserveShare - sm.landingProp / sm.prop) > 1e-9) fails.Add(w + p.Id + " band is not landingProp / prop");
                    if (band != (p.Line == Text.Part("tankLanding").Line)) fails.Add(w + p.Id + " landing line follows the band");
                    if (p.Solid != (p.OxShare == 0)) fails.Add(w + p.Id + " solid has one grain");
                }
                if (x.LandingPlanned != (RecoverySim.Offered(d.recoveryMode) && masses[0].landingProp > 0)) fails.Add(w + "landing planned");
                if (m.LandingRequired && !x.LandingPlanned) fails.Add(w + "the mission lands but the X-ray plans no landing");

                // Flight plan: Liftoff first at T+0, then times never go backwards; Landing only when planned.
                if (x.Events.Count < 2) fails.Add(w + "plan has " + x.Events.Count + " events");
                else if (x.Events[0].Id != "liftoff" || x.Events[0].T != 0) fails.Add(w + "plan starts with " + x.Events[0].Id);
                for (int k = 1; k < x.Events.Count; k++)
                    if (x.Events[k].T < x.Events[k - 1].T) fails.Add(w + "plan goes backwards at " + x.Events[k].Id);
                var ids = x.Events.ConvertAll(e => e.Id);
                if (ids.Contains("landing") && !x.LandingPlanned) fails.Add(w + "landing event without a planned landing");
                if (!ids.Contains("maxq")) fails.Add(w + "no Max-Q");
                if (!ids.Contains("meco")) fails.Add(w + "no MECO");
                if (n > 1 && !ids.Contains("seco")) fails.Add(w + "no SECO on a " + n + "-stage rocket");
                foreach (var e in x.Events)
                {
                    if (e.Clock != StarterXray.Clock(e.T) || !e.Clock.StartsWith("T+", StringComparison.Ordinal)) fails.Add(w + e.Id + " clock " + e.Clock);
                    if (string.IsNullOrEmpty(e.Label)) fails.Add(w + e.Id + " label");
                }
                int maxq = ids.IndexOf("maxq"), meco = ids.IndexOf("meco"), seco = ids.IndexOf("seco");
                if (maxq >= 0 && meco >= 0 && maxq > meco) fails.Add(w + "Max-Q after MECO");
                if (meco >= 0 && seco >= 0 && meco > seco) fails.Add(w + "MECO after SECO");
                TestContext.WriteLine(m.Id + ": " + string.Join(", ", x.Events.ConvertAll(e => e.Label + " " + e.Clock)) + (x.LandingPlanned ? " (lands)" : ""));
            }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        [Test]
        public void ChangingTheControls_ChangesTheXray()
        {
            var r = StarterFixture.Rocket("m5");
            var before = StarterXray.Build(r, Text);
            r.Stages = r.Stages == 3 ? 2 : 3;
            var after = StarterXray.Build(r, Text);
            Assert.AreNotEqual(before.Parts.Count, after.Parts.Count, "a stage more or less is a tank, engines and interstage more or less");
            Assert.IsEmpty(StarterXray.Build(null, Text).Parts);
            Assert.AreEqual("T+0:00", StarterXray.Clock(0));
            Assert.AreEqual("T+2:31", StarterXray.Clock(150.6));
            Assert.AreEqual("T+1:00:05", StarterXray.Clock(3605));
        }

        public static IEnumerable<StarterLayoutTests.Size> Sizes() => DesignerTiersTests.TierSizes();

        public static StarterXrayInput Input(StarterLayoutTests.Size s, XrayModel x, XrayPart selected, bool engineer)
        {
            var t = Text;
            return new StarterXrayInput
            {
                Width = s.W, Height = s.H, SafeTop = s.Top, SafeBottom = s.Bottom, ReserveExit = s.Exit,
                Close = t.XrayClose, Title = t.XrayTitle, Plan = t.XrayPlan, Model = x,
                CardTitle = selected?.Title ?? "", CardLine = selected?.Line ?? x.Pick, CardNumbers = engineer && selected != null ? selected.Numbers : "",
            };
        }

        [Test]
        public void Layout_EverySize_EveryMissionStarter_EveryPart_NoOverlap_TargetsAndTextFit()
        {
            var fails = new List<string>();
            foreach (var s in Sizes())
                foreach (var m in StarterFixture.Campaign.Missions)
                {
                    var x = Model(m.Id);
                    var picks = new List<XrayPart> { null };
                    picks.AddRange(x.Parts);
                    foreach (var sel in picks)
                        foreach (bool eng in new[] { false, true })
                        {
                            var input = Input(s, x, sel, eng);
                            var f = StarterXrayLayout.Place(input);
                            string where = s + " " + m.Id + " " + (sel?.Id ?? "none") + (eng ? " engineer" : "");
                            foreach (var p in StarterXrayLayout.Problems(f, input)) fails.Add(where + ": " + p);
                            if (f.Parts.Count != x.Parts.Count) fails.Add(where + ": " + f.Parts.Count + " targets for " + x.Parts.Count + " parts");
                            if (f.Cells.Count != x.Events.Count) fails.Add(where + ": strip cells");
                        }
                }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        [Test]
        public void Layout_StackOrder_LongerPartsDrawTaller_BoostersBesideStageOne()
        {
            var s = new StarterLayoutTests.Size { Name = "430x932", W = 430, H = 932, Top = 59, Bottom = 34 };
            foreach (var id in new[] { "m5", "m10", "m19" })
            {
                var x = Model(id);
                var f = StarterXrayLayout.Place(Input(s, x, null, false));
                float y = float.MinValue;
                foreach (var kv in f.Parts)
                {
                    if (kv.Key == "boosters") continue;
                    Assert.GreaterOrEqual(kv.Value.Y, y - 0.01f, id + " " + kv.Key + " below the part above it");
                    y = kv.Value.Y;
                }
                var tank1 = f.PartBox("tank-1");
                var fair = f.PartBox("fairing");
                if (x.Part("tank-1").LengthM > x.Part("fairing").LengthM) Assert.Greater(tank1.H, fair.H, id + " the long tank draws taller");
                if (x.Part("boosters") != null)
                {
                    var b = f.PartBox("boosters");
                    Assert.AreEqual(tank1.Y, b.Y, 0.01f, id);
                    Assert.Less(b.Right, tank1.X, id + " boosters beside the core");
                    Assert.IsTrue(f.BoosterMirror.Live, id);
                }
            }
        }
    }
}
