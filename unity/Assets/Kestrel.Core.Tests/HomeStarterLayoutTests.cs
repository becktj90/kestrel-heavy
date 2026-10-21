// Starter Home (feat(home-simple), PHASED_DESIGNER_PLAN PR 6): ONE primary CTA (CONTINUE with a save, else
// LAUNCH over "KH-n · Name"), MISSIONS and the Settings gear; Design first, Codex, Thrum, credits and stats sit
// behind home.* flags. Every size runs with and without a save, flags off (Starter) and on (full Home):
// rect overlap (no visible block on another, all inside the safe column) and text fit (every word in its box).
using System;
using System.Collections.Generic;
using Kestrel.Core.Features;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HomeStarterLayoutTests
    {
        const string LongRecord = "20 flights cleared · 888.88M Kc · 20 patches";
        const string Mission = "KH-3 · Tiny Orbit";
        const string Thrum = "Oh! A visitor. Mind the oil. I am Thrum. I keep this hangar, and I talk to wrenches.";

        static IEnumerable<(string, HomePhoneScreen)> Phones()
        {
            HomePhoneScreen Points(float w, float h, float top, float bottom) =>
                new HomePhoneScreen { W = w, H = h, Top = top, Bottom = bottom, Left = 8f, Right = 8f, PerPoint = 1f };
            yield return ("335x667 web portrait", Points(335f, 667f, 0f, 0f));
            yield return ("375x667 SE (iOS panel)", HomePhoneLayout.Device(375f, 667f, 20f, 0f));
            yield return ("390x844 (iOS panel)", HomePhoneLayout.Device(390f, 844f, 47f, 34f));
            yield return ("390x844 (points)", Points(390f, 844f, 47f, 34f));
            yield return ("430x932 (iOS panel)", HomePhoneLayout.Device(430f, 932f, 59f, 34f));
            yield return ("430x932 (points)", Points(430f, 932f, 59f, 34f));
            yield return ("820x1180 iPad portrait (iOS panel)", HomePhoneLayout.Device(820f, 1180f, 24f, 20f));
            yield return ("820x1180 iPad portrait (points)", Points(820f, 1180f, 24f, 20f));
        }

        static readonly (string, HomeShow)[] Modes = { ("starter", HomeShow.None), ("full", HomeShow.Full) };
        static readonly (float, float)[] Web = { (1280f, 720f), (1366f, 768f), (1920f, 1080f) };

        [Test]
        public void HomeFlagsAreTheFeatureFlagsKeysAndStarterHidesThemAll()
        {
            Assert.That(HomeFeatures.Names, Is.EquivalentTo(new[] { "home.designFirst", "home.codexTile", "home.thrum", "home.creditsChip", "home.statsCard" }));
            foreach (var k in HomeFeatures.Names) Assert.That(FeatureKeys.All, Does.Contain(k));
            var saved = FeatureFlags.Active;
            try
            {
                FeatureFlags.Active = FeatureFlags.Load(RepoRoot());
                Assert.That(HomeFeatures.Current().ToString(), Is.EqualTo(HomeShow.None.ToString()), "data/features.json ships the Starter Home");
                int changed = 0;
                FeatureFlags.Active.Changed += _ => changed++;
                FeatureFlags.Active.SetOverride(FeatureKeys.HomeThrum, true);
                Assert.That(HomeFeatures.Current().Thrum, Is.True);
                Assert.That(HomeFeatures.Current().Codex, Is.False);
                Assert.That(changed, Is.EqualTo(1), "Changed fires so Home can re-apply");
                FeatureFlags.Active = HomeFeatures.With(HomeShow.Full);
                Assert.That(HomeFeatures.Current().ToString(), Is.EqualTo(HomeShow.Full.ToString()));
            }
            finally { FeatureFlags.Active = saved; }
        }

        [Test]
        public void CtaFollowsTheStarterCopy()
        {
            HomePhoneLayout.Cta(true, Mission, _ => true, out var l, out var c);
            Assert.That((l, c), Is.EqualTo(("CONTINUE", Mission)));
            HomePhoneLayout.Cta(false, Mission, _ => true, out l, out c);
            Assert.That((l, c), Is.EqualTo(("LAUNCH KH-3 · Tiny Orbit", "")));
            HomePhoneLayout.Cta(false, Mission, _ => false, out l, out c);
            Assert.That((l, c), Is.EqualTo(("LAUNCH KH-3", "Tiny Orbit")));
            HomePhoneLayout.Cta(false, null, _ => true, out l, out c);
            Assert.That((l, c), Is.EqualTo(("LAUNCH", HomePhoneLayout.LaunchCaption)));
            HomePhoneLayout.Cta(true, null, _ => true, out l, out c);
            Assert.That((l, c), Is.EqualTo(("CONTINUE", HomePhoneLayout.ContinueCaption)));
            Assert.That(HomePhoneLayout.Flown(3, 20), Is.EqualTo("3 of 20 flown"));
            Assert.That(HomePhoneLayout.Flown(0, 0), Is.Null);
        }

        static string RepoRoot()
        {
            var dir = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "data", "features.json"))) dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "repo root with data/features.json");
            return dir.FullName;
        }

        [Test]
        public void PhoneHomeHasOneCtaAndNothingOverlaps()
        {
            foreach (var (mode, show) in Modes)
                foreach (bool save in new[] { false, true })
                    foreach (var (name, s) in Phones())
                    {
                        string n = name + " " + mode + (save ? " save" : " no save");
                        var f = HomePhoneLayout.Solve(s, show, save, LongRecord, Mission, Thrum);
                        Assert.That(f.Cta, save ? Is.EqualTo("CONTINUE") : Is.EqualTo("LAUNCH " + Mission).Or.EqualTo("LAUNCH KH-3"), n);
                        Assert.That(f.CtaCaption, save ? Is.EqualTo(Mission) : f.Cta == "LAUNCH KH-3" ? Is.EqualTo("Tiny Orbit") : Is.EqualTo(""), n + " sub-line");
                        // Hidden extras are empty and take no column height.
                        Assert.That(f.Design.W > 0f, Is.EqualTo(show.DesignFirst), n + " design");
                        Assert.That(f.Codex.W > 0f, Is.EqualTo(show.Codex), n + " codex");
                        Assert.That(f.Thrum.W > 0f, Is.EqualTo(show.Thrum), n + " thrum");
                        Assert.That(f.Stats.W > 0f, Is.EqualTo(show.Stats), n + " stats");
                        Assert.That(f.Bar.W > 0f, Is.EqualTo(show.Credits), n + " credits chip");
                        Assert.That(f.Missions.W, Is.GreaterThan(0f), n + " MISSIONS");
                        if (!show.Codex) Assert.That(f.Missions.W, Is.EqualTo(f.Launch.W).Within(0.01f), n + " MISSIONS spans the row");

                        var blocks = new List<(string, HomeRect)>
                        {
                            ("credits", f.Bar), ("gear", f.Gear), ("title", f.Title), ("stats", f.Stats), ("CTA", f.Launch),
                            ("design", f.Design), ("missions", f.Missions), ("codex", f.Codex), ("thrum", f.Thrum), ("guide pop", f.Pop),
                        }.FindAll(b => b.Item2.W > 0.5f && b.Item2.H > 0.5f);
                        for (int a = 0; a < blocks.Count; a++)
                            for (int b = a + 1; b < blocks.Count; b++)
                                Assert.That(blocks[a].Item2.Overlaps(blocks[b].Item2), Is.False, $"{n}: {blocks[a].Item1} {R(blocks[a].Item2)} on {blocks[b].Item1} {R(blocks[b].Item2)}");
                        foreach (var (id, r) in blocks)
                        {
                            if (id == "gear") continue;
                            Assert.That(r.X >= f.Safe.X - 0.01f && r.Right <= f.Safe.Right + 0.01f && r.Y >= f.Safe.Y - 0.01f && r.Bottom <= f.Safe.Bottom + 0.01f,
                                n + " " + id + " " + R(r) + " inside " + R(f.Safe));
                        }
                        Assert.That(f.Launch.H, Is.GreaterThanOrEqualTo(HomePhoneLayout.HitMin), n + " CTA target");
                        Assert.That(f.Missions.H, Is.GreaterThanOrEqualTo(HomePhoneLayout.HitMin), n + " MISSIONS target");
                        Assert.That(f.Hero, Is.GreaterThanOrEqualTo(f.Pop.H + 8f), n + " room for the rocket and the guide pop");

                        // Text fit: the CTA word, its caption, MISSIONS.
                        var t = f.Type;
                        float col = f.Launch.W;
                        float ctaInner = col - 2f * HomePhoneLayout.LaunchPadH(t.Scale) - HomePhoneLayout.LaunchChromeX;
                        Assert.That(HomeTypeMetrics.Fits(HomeFace.FrauncesBlack, f.Cta, t.LaunchLabel, ctaInner, 0.10f), n + " " + f.Cta + " @" + t.LaunchLabel + " in " + ctaInner);
                        Assert.That(t.LaunchLabel, Is.GreaterThanOrEqualTo(18f), n + " CTA reads as the primary (STARTER_COPY 18-20 px)");
                        if (f.CtaCaption.Length > 0)
                            Assert.That(HomeTypeMetrics.Lines(HomeFace.GroteskMedium, f.CtaCaption, t.LaunchCaption, ctaInner), Is.LessThanOrEqualTo(f.LaunchCaptionLines), n + " CTA caption lines");
                        float ctaContent = f.Launch.H - HomePhoneLayout.LaunchBorderV - 2f * HomePhoneLayout.LaunchPadV(t.Scale);
                        Assert.That(ctaContent, Is.GreaterThanOrEqualTo(t.LaunchLabel * HomePhoneLayout.LineH + f.LaunchCaptionLines * t.LaunchCaption * HomePhoneLayout.LineH - 0.01f), n + " CTA text inside");
                        float tileInner = f.Missions.W - 2f * HomePhoneLayout.TileBorder - 2f * t.Pad;
                        float nameAvail = tileInner - HomePhoneLayout.TileIcon - HomePhoneLayout.TileLamp;
                        Assert.That(HomeTypeMetrics.Need(HomeFace.FrauncesBold, "Missions", Math.Min(t.TileName, HomeTypeMetrics.FitOneLine(HomeFace.FrauncesBold, "Missions", t.TileName, 12f, nameAvail - HomePhoneLayout.NameSlack, 0.03f)), 0.03f), Is.LessThanOrEqualTo(nameAvail), n + " MISSIONS name");
                        int capLines = HomeTypeMetrics.Lines(HomeFace.GroteskMedium, HomePhoneLayout.MissionsCaption, t.TileCaption, tileInner);
                        Assert.That(capLines, Is.LessThanOrEqualTo(f.TileCaptionLines), n + " MISSIONS caption lines");
                        float tileContent = f.Missions.H - 2f * HomePhoneLayout.TileBorder - 2f * t.Pad - Math.Max(22f, t.TileName * HomePhoneLayout.LineH) - 3f;
                        Assert.That(tileContent - capLines * t.TileCaption * HomePhoneLayout.LineH, Is.GreaterThanOrEqualTo(HomePhoneLayout.TileAir - 0.01f), n + " MISSIONS caption inside");
                        if (show.Stats) Assert.That(f.RecordLines, Is.EqualTo(1), n + " record on one line");
                        if (show.Thrum) Assert.That(f.ThrumLines, Is.LessThanOrEqualTo(2), n + " Thrum two lines at most");
                    }
        }

        [Test]
        public void WideWebHomeHasOneCtaAndNothingOverlaps()
        {
            foreach (var (mode, show) in Modes)
                foreach (bool save in new[] { false, true })
                    foreach (var (w, h) in Web)
                    {
                        string n = w + "x" + h + " " + mode + (save ? " save" : " no save");
                        var f = WebHomeLayout.Place(w, h, 0f, 0f, 0f, 0f, show, save);
                        Assert.That(f.Wide, Is.True, n);
                        var f2 = WebHomeLayout.Place(w, h, 0f, 0f, 0f, 0f, show, save, Mission);
                        Assert.That(f.LaunchAt100, Is.EqualTo(save ? HomeWideText.Continue : HomeWideText.Launch), n);
                        Assert.That(WebHomeLayout.TextFitError(f2), Is.Null, n + " with the mission line: " + f2.CtaLabel);
                        Assert.That(f2.Type.Launch, Is.GreaterThanOrEqualTo(save ? 13f : WebHomeLayout.WideCtaMin - 0.01f), n + " " + f2.CtaLabel + " @" + f2.Type.Launch);
                        Assert.That(f2.CtaLabel, save ? Is.EqualTo("CONTINUE") : Is.EqualTo("LAUNCH " + Mission).Or.EqualTo("LAUNCH KH-3"), n);
                        Assert.That(WebHomeLayout.TextFitError(f), Is.Null, n);
                        Assert.That(f.Design.Live, Is.EqualTo(show.DesignFirst), n + " design");
                        Assert.That(f.Codex.Live, Is.EqualTo(show.Codex), n + " codex");
                        Assert.That(f.Hint.Live, Is.EqualTo(show.Thrum), n + " thrum");
                        Assert.That(f.Stats.Live, Is.EqualTo(show.Stats), n + " stats");
                        Assert.That(f.Missions.Live && f.Launch.Live, n + " CTA and MISSIONS");
                        if (!show.DesignFirst)
                            Assert.That(f.Missions.Y, Is.EqualTo(f.Launch.Bottom + WebHomeLayout.Gap).Within(0.01f), n + " MISSIONS closes up under the CTA");
                        var boxes = new List<(string, HudBox)>
                        {
                            ("rocket", f.Rocket), ("title", f.Title), ("stats", f.Stats), ("CTA", f.Launch), ("design", f.Design),
                            ("missions", f.Missions), ("codex", f.Codex), ("gear", f.Gear), ("exit", f.Exit), ("audio", f.Audio), ("hint", f.Hint),
                        }.FindAll(b => b.Item2.Live);
                        for (int a = 0; a < boxes.Count; a++)
                            for (int b = a + 1; b < boxes.Count; b++)
                                Assert.That(boxes[a].Item2.Overlaps(boxes[b].Item2), Is.False, $"{n}: {boxes[a].Item1} on {boxes[b].Item1}");
                        foreach (var (id, b) in boxes)
                            Assert.That(b.X >= -0.01f && b.Right <= w + 0.01f && b.Y >= -0.01f && b.Bottom <= h + 0.01f, n + " " + id + " on screen");
                        Assert.That(f.Launch.H, Is.GreaterThanOrEqualTo(44f), n + " CTA target");
                        Assert.That(f.Missions.H, Is.GreaterThanOrEqualTo(44f), n + " MISSIONS target");
                    }
        }

        [Test]
        public void PortraitWebFrameDropsTheHiddenExtras()
        {
            foreach (bool save in new[] { false, true })
            {
                var f = WebHomeLayout.Place(390f, 844f, 47f, 34f, 0f, 0f, HomeShow.None, save, Mission);
                Assert.That(f.Wide, Is.False);
                Assert.That(f.Stats.Live || f.Design.Live || f.Codex.Live || f.Hint.Live, Is.False);
                Assert.That(f.Missions.W, Is.EqualTo(f.Menu.W).Within(0.01f), "MISSIONS spans the row");
                Assert.That(f.Launch.Overlaps(f.Missions), Is.False);
            }
        }

        static string R(HomeRect r) => $"({r.X:0.#},{r.Y:0.#} {r.W:0.#}x{r.H:0.#})";
    }
}
