// #336: first-stage sizing audit. Real small-launcher first stages typically burn out ~60-80 km at
// ~2-2.5 km/s, on a 40/60 to 35/65 S1/S2 delta-v split. This test pins the audited ideal delta-v
// split (Headless.StackDeltaV, a pure design property) for every board starter, designer template and
// preset, so the sizing is visible and any retune updates these pins deliberately.
//
// Audit (live seed-1 briefing wind; S1 burnout from the probe, ideal dv from StackDeltaV):
//   2-stage S1 share 42-55% (KH-3 48%, KH-5 55%, KH-8 52%, KH-14 51%, KH-16 53%, scout 43%, kid 42%)
//   burn out at 89-184 km / 4.1-4.9 km/s - oversized S1, and lofted on top of it (KH-4 39% and KH-12
//   40% are in the split band but still burn out at 151 km / 179 km).
//   2-stage S1 share 28-34% (KH-13 28%, KH-11 34%, super-heavy 33%, lander 31%) - S1 a touch small.
//   3/4-stage S1 share 17-23% (KH-6/7/9/10/15/17/18/19/20, heavy, moonshot, stock) - small by design;
//   they burn out low (35-59 km / 1.1-2.0 km/s).
// A prop shift alone does not bring both criteria into range: measured on the scout preset, S1 30000 /
// S2 8000 kg puts the share at 36.8% (in band) but the burnout only drops 150 -> 109 km, still above
// 80 km; reaching 76 km needs S1 25000 / S2 10000 kg, share 31% (below the band). The burnout altitude
// is set by the lofted pitch program as much as by the S1 size, so a proper retune needs joint prop +
// pitch-program tuning per design against its own mission - a campaign-search regeneration (for the
// winners) and a coordinated preset update, not a hand edit here. The proposed retunes and the full
// table are on issue #336.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class S1SizingAuditTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        double S1Share(Design d)
        {
            var s = Stack.SimInputs(catalog, d, 0.2);
            var (per, total) = Headless.StackDeltaV(s.stages, s.opts.payload);
            Assert.Greater(per.Length, 0);
            Assert.Greater(total, 0);
            return per[0] / total;
        }

        // Board starters: the base Choose pick (no fix/strength mode - those change safety factor, not prop).
        // Pinned to the #336 audit (tolerance 0.01 absorbs the probe's whole-m/s rounding).
        [TestCase("m1", 1.0)] [TestCase("m2", 1.0)]
        [TestCase("m3", 0.48)] [TestCase("m4", 0.39)] [TestCase("m5", 0.55)]
        [TestCase("m6", 0.22)] [TestCase("m7", 0.18)] [TestCase("m8", 0.52)]
        [TestCase("m9", 0.20)] [TestCase("m10", 0.19)] [TestCase("m11", 0.34)]
        [TestCase("m12", 0.40)] [TestCase("m13", 0.28)] [TestCase("m14", 0.51)]
        [TestCase("m15", 0.17)] [TestCase("m16", 0.53)] [TestCase("m17", 0.23)]
        [TestCase("m18", 0.23)] [TestCase("m19", 0.18)] [TestCase("m20", 0.23)]
        public void BoardStarter_S1Share_MatchesTheAudit(string missionId, double share)
        {
            var mission = campaign.ById(missionId);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            Assert.AreEqual(share, S1Share(design), 0.01, missionId);
        }

        // Designer templates.
        [TestCase("hopper", 1.0)] [TestCase("workhorse", 0.36)] [TestCase("heavy", 0.43)]
        [TestCase("lander", 0.31)] [TestCase("moonshot", 0.29)]
        public void Template_S1Share_MatchesTheAudit(string templateId, double share)
        {
            var t = hangar.Template(templateId);
            Assert.IsNotNull(t, templateId);
            Assert.AreEqual(share, S1Share(Design.FromJson(t.design)), 0.01, templateId);
        }

        // Presets (data/presets.json).
        [TestCase("stock", 0.46)] [TestCase("scout", 0.43)] [TestCase("kid", 0.42)] [TestCase("super-heavy", 0.33)]
        public void Preset_S1Share_MatchesTheAudit(string presetId, double share)
        {
            var d = Golden.Catalog.Preset(presetId);
            Assert.IsNotNull(d, presetId);
            Assert.AreEqual(share, S1Share(d.Clone()), 0.01, presetId);
        }
    }
}
