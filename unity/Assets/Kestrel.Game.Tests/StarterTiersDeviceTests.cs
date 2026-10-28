// Designer tiers, delivery step 2 (docs/DESIGNER_TIERS_SPEC.md): the REAL Starter tree with a Kid Why? card open, and
// the X-ray view, at SE 375x667, Pro Max 430x932 and iPad 820x1180 (plus 335x667). Per size: #300's UiTreeChecks (text
// inside its box, no overlapping leaves, nothing outside the safe area), TextFit, the Core model checks
// (StarterLayout / StarterXrayLayout Problems) and 44 pt targets. PNGs go to CaptureOut/starter-tiers (DeviceCapture),
// which CI uploads. Then the click-through by UiIds: "?" opens and closes its card, X-ray opens and closes, a part tap
// fills the card, and (with the tier flags on, test prefs) the switch persists the tier and Engineer shows numbers.
// Batchmode-safe: no LogError, surfaces disposed per case, globals restored in TearDown.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Features;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class StarterTiersDeviceTests
    {
        const string CaptureSub = "starter-tiers";

        struct Device { public string Name; public float W, H, Top, Bottom; }

        static IEnumerable<Device> Devices()
        {
            yield return new Device { Name = "se-375x667", W = 375, H = 667, Top = 20 };
            yield return new Device { Name = "335x667", W = 335, H = 667, Top = 20 };
            yield return new Device { Name = "promax-430x932", W = 430, H = 932, Top = 59, Bottom = 34 };
            yield return new Device { Name = "ipad-820x1180", W = 820, H = 1180, Top = 24, Bottom = 20 };
        }

        static Catalog catalog;
        static Campaign campaign;
        static DesignerTierText copy;
        IReadOnlyDictionary<string, Kestrel.Core.Json.JsonObject> basesBefore;
        bool loadedBefore;
        readonly List<IDisposable> open = new List<IDisposable>();
        readonly List<StarterPanel> panels = new List<StarterPanel>();

        [OneTimeSetUp]
        public void Load()
        {
            basesBefore = StarterBases.Active;
            loadedBefore = StarterBasesBoot.Loaded;
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            catalog = Catalog.LoadFromRepo(root);
            campaign = Campaign.Load(root);
            copy = DesignerTierText.Load(root);
            StarterBases.Active = StarterBases.Parse(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
            StarterBasesBoot.Loaded = true;
        }

        [TearDown]
        public void CloseEverything()
        {
            foreach (var p in panels) { try { p.Detach(); } catch (Exception e) { Debug.LogWarning("[StarterTiers] detach: " + e.Message); } }
            panels.Clear();
            foreach (var d in open) { try { d.Dispose(); } catch (Exception e) { Debug.LogWarning("[StarterTiers] dispose: " + e.Message); } }
            open.Clear();
        }

        [OneTimeTearDown]
        public void Restore()
        {
            StarterBases.Active = basesBefore;
            StarterBasesBoot.Loaded = loadedBefore;
        }

        sealed class Prefs
        {
            public readonly Dictionary<string, int> Map = new Dictionary<string, int>();
            public bool Has(string k) => Map.ContainsKey(k);
            public int Get(string k) => Map.TryGetValue(k, out var v) ? v : 0;
            public void Set(string k, int v) => Map[k] = v;
        }

        static StarterRocket Rocket(string id) => new StarterRocket(catalog, campaign.ById(id), StarterBases.For(id));

        StarterPanel Panel(StarterRocket r, UiDeviceSurface s, Prefs prefs, FeatureFlags flags)
        {
            var p = new StarterPanel(r, s.Stage, -1, copy) { PrefHas = prefs.Has, PrefGet = prefs.Get, PrefSet = prefs.Set, Flags = () => flags };
            panels.Add(p);
            p.Refresh();
            return p;
        }

        UiDeviceSurface Surface(float w, float h)
        {
            var s = UiDeviceSurface.Open(w, h);
            open.Add(s);
            return s;
        }

        static FeatureFlags Shipped() => FeatureFlags.Load(Catalog.FindRepoRoot(Directory.GetCurrentDirectory()));

        [Test]
        public void EveryDevice_WhyCardOpen_AndXray_RealTree_Fits_Captured()
        {
            var shots = new List<string>();
            foreach (var d in Devices())
                foreach (var (mission, why) in new[] { ("m5", StarterKnob.Stages), ("m10", StarterKnob.Boosters) })
                {
                    CloseEverything();
                    var surface = Surface(d.W, d.H);
                    var panel = Panel(Rocket(mission), surface, new Prefs(), Shipped());
                    panel.Layout(d.W, d.H, d.Top, d.Bottom, 0, 0, 1f, false, false);
                    Click(panel.Root.Q<Button>(UiIds.StarterWhy(why)));
                    Assert.AreEqual(why, panel.WhyOpen, d.Name);
                    surface.Pump(3);
                    Settle(surface, panel.Root);
                    string where = d.Name + " " + mission + " why " + why;
                    Check(panel.Root, surface, d, where);
                    Assert.That(StarterLayout.Problems(panel.Frame, panel.Input), Is.Empty, where + " model");
                    var card = panel.Root.Q(UiIds.StarterWhyCard);
                    Assert.IsTrue(UiTreeChecks.Visible(card), where + " the card is on screen");
                    Assert.AreEqual(copy.WhyText(why, panel.Rocket), panel.Root.Q<Label>(UiIds.StarterWhyText).text, where);
                    Assert.IsFalse(UiTreeChecks.Visible(panel.Root.Q(UiIds.StarterHint)), where + " the card replaces the hint line");
                    Assert.IsFalse(UiTreeChecks.Visible(panel.Root.Q(UiIds.StarterTierSwitch)), where + " only Kid is offered: no switch");
                    Assert.IsTrue(UiTreeChecks.Visible(panel.Root.Q<Button>(UiIds.StarterLaunch)), where + " Launch stays");
                    if (mission == "m5")
                    {
                        string p = DeviceCapture.Save(CaptureSub, "starter-why-" + d.Name, surface, out var kind);
                        TestContext.WriteLine(where + " capture (" + kind + "): " + (p ?? "none"));
                        if (p != null) shots.Add(p);
                    }

                    // Close the card: the speed bar and X-ray button are on screen; then the X-ray view.
                    Click(panel.Root.Q<Button>(UiIds.StarterWhy(why)));
                    Assert.AreEqual(StarterKnob.None, panel.WhyOpen, where);
                    surface.Pump(2);
                    Settle(surface, panel.Root);
                    Assert.IsTrue(UiTreeChecks.Visible(panel.Root.Q(UiIds.StarterSpeedTrack)), where + " speed bar");
                    var xb = panel.Root.Q<Button>(UiIds.StarterXrayButton);
                    Assert.IsTrue(UiTreeChecks.Visible(xb), where + " X-ray button");
                    Assert.That(panel.Root.Q(UiIds.StarterRocket).worldBound.Contains(xb.worldBound.center), where + " X-ray sits in the rocket window");
                    Click(xb);
                    Assert.IsTrue(panel.XrayOpen, where);
                    Click(panel.Root.Q<Button>(UiIds.StarterXrayPart("tank-1")));
                    Assert.AreEqual("tank-1", panel.Xray.Selected, where);
                    surface.Pump(3);
                    Settle(surface, panel.Root);
                    string xw = d.Name + " " + mission + " x-ray";
                    Check(panel.Root, surface, d, xw);
                    Assert.That(StarterXrayLayout.Problems(panel.Xray.Frame, panel.Xray.Input), Is.Empty, xw + " model");
                    Assert.IsFalse(UiTreeChecks.Visible(panel.Root.Q<Button>(UiIds.StarterLaunch)), xw + " the Starter screen is covered");
                    Assert.AreEqual(panel.Xray.Model.Part("tank-1").Line, panel.Root.Q<Label>("starter-xray-card-line").text, xw);
                    Assert.IsFalse(UiTreeChecks.Visible(panel.Root.Q<Label>("starter-xray-card-numbers")), xw + " numbers only in Engineer");
                    foreach (var e in panel.Xray.Model.Events)
                        Assert.IsTrue(UiTreeChecks.Visible(panel.Root.Q(UiIds.StarterXrayEvent(e.Id))), xw + " " + e.Id);
                    bool lands = panel.Xray.Model.LandingPlanned;
                    var band = panel.Root.Q<Button>(UiIds.StarterXrayPart("tank-1")).Q("starter-xray-band-reserve");
                    Assert.AreEqual(lands, UiTreeChecks.Visible(band), xw + " landing band only with a planned landing");
                    if (mission == "m10")
                    {
                        string p = DeviceCapture.Save(CaptureSub, "starter-xray-" + d.Name, surface, out var kind);
                        TestContext.WriteLine(xw + " capture (" + kind + "): " + (p ?? "none"));
                        if (p != null) shots.Add(p);
                    }
                    Click(panel.Root.Q<Button>(UiIds.StarterXrayClose));
                    Assert.IsFalse(panel.XrayOpen, xw);
                    // Visible() reads resolvedStyle, which only catches up with the restored inline visibility on the next panel update.
                    surface.Pump(2);
                    Assert.IsFalse(UiTreeChecks.Visible(panel.Xray.Root), xw + " the X-ray layer is gone");
                    Assert.IsTrue(UiTreeChecks.Visible(panel.Root.Q<Button>(UiIds.StarterLaunch)), xw + " back on the Starter screen");
                }
            Assert.AreEqual(8, shots.Count, "a Why card and an X-ray PNG per size");
            foreach (var p in shots)
            {
                Assert.That(File.Exists(p), p);
                Assert.Greater(new FileInfo(p).Length, 200, p + " is a real image");
            }
        }

        [Test]
        public void TierSwitch_HiddenWhenShipped_PersistsAndShowsEngineerNumbersWhenOffered()
        {
            var surface = Surface(430, 932);
            var prefs = new Prefs();
            prefs.Set(ExperienceSettings.LegacyHangarEngineerPref, 1);   // an old save with the Hangar engineer toggle on
            var shipped = Panel(Rocket("m5"), surface, prefs, Shipped());
            shipped.Layout(430, 932, 59, 34, 0, 0, 1f, false, false);
            surface.Pump(2);
            Assert.AreEqual(ExperienceTier.Starter, shipped.Tier, "Engineer chip not offered yet: Kid");
            Assert.IsFalse(UiTreeChecks.Visible(shipped.Root.Q(UiIds.StarterTierSwitch)));
            Assert.AreEqual(1, prefs.Get(ExperienceSettings.LegacyHangarEngineerPref), "showing Kid never rewrites the old pref");
            CloseEverything();

            surface = Surface(430, 932);
            var flags = Shipped();
            flags.SetOverride(FeatureKeys.DesignerTierBuilder, true);
            flags.SetOverride(FeatureKeys.DesignerTierEngineer, true);
            var panel = Panel(Rocket("m5"), surface, prefs, flags);
            panel.Layout(430, 932, 59, 34, 0, 0, 1f, false, false);
            surface.Pump(3);
            Settle(surface, panel.Root);
            Assert.AreEqual(ExperienceTier.Engineer, panel.Tier, "offered: the old engineer save reads Engineer");
            Assert.IsTrue(UiTreeChecks.Visible(panel.Root.Q(UiIds.StarterTierSwitch)));
            Check(panel.Root, surface, new Device { Name = "promax switch", W = 430, H = 932, Top = 59, Bottom = 34 }, "promax switch");
            Assert.IsTrue(panel.Root.Q<Button>(UiIds.StarterTierChip(ExperienceTier.Engineer)).ClassListContains("starter-on"));

            Click(panel.Root.Q<Button>(UiIds.StarterTierChip(ExperienceTier.Starter)));
            Assert.AreEqual(ExperienceTier.Starter, panel.Tier);
            Assert.AreEqual(0, prefs.Get(ExperienceSettings.TierPref));
            Assert.AreEqual(0, prefs.Get(ExperienceSettings.LegacyHangarEngineerPref), "one tier: the old toggles follow");
            Assert.IsTrue(panel.Root.Q<Button>(UiIds.StarterTierChip(ExperienceTier.Starter)).ClassListContains("starter-on"));

            Click(panel.Root.Q<Button>(UiIds.StarterTierChip(ExperienceTier.Engineer)));
            Assert.AreEqual(ExperienceTier.Engineer, ExperienceSettings.Tier(prefs.Has, prefs.Get));
            Click(panel.Root.Q<Button>(UiIds.StarterXrayButton));
            Click(panel.Root.Q<Button>(UiIds.StarterXrayPart("fairing")));
            surface.Pump(2);
            var numbers = panel.Root.Q<Label>("starter-xray-card-numbers");
            Assert.IsTrue(UiTreeChecks.Visible(numbers), "Engineer shows the numbers");
            Assert.AreEqual(panel.Xray.Model.Part("fairing").Numbers, numbers.text);
        }

        void Check(VisualElement root, UiDeviceSurface surface, Device d, string where)
        {
            var stage = surface.Stage.worldBound;
            var safe = new Rect(stage.xMin, stage.yMin + d.Top, stage.width, stage.height - d.Top - d.Bottom);
            var problems = UiTreeChecks.Problems(root, stage, safe, where);
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems));
            Assert.That(TextFit.Problems(root), Is.Empty, where + " text-fit");
            foreach (var b in root.Query<Button>().ToList())
            {
                if (!UiTreeChecks.Visible(b)) continue;
                Assert.GreaterOrEqual(b.worldBound.width, 44f - 0.5f, where + " " + b.name + " width");
                Assert.GreaterOrEqual(b.worldBound.height, 44f - 0.5f, where + " " + b.name + " height");
            }
        }

        static void Settle(UiDeviceSurface surface, VisualElement root)
        {
            TextFit.Apply(root);
            surface.Pump(3);
            TextFit.Apply(root);
            surface.Pump(2);
        }

        static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null);
            using (var ev = NavigationSubmitEvent.GetPooled())
            {
                ev.target = button;
                button.SendEvent(ev);
            }
        }
    }
}
