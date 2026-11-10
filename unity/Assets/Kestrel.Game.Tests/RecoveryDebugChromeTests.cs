// Trevor 2026-11-09 (KH-3 on TestFlight 218/219): Pitch −/+ and Thr −/+ and a "Coast  sink 0.0 m/s  tilt 0.0°" line
// were on screen under a booster the flight had just called BOOSTER LOST, with the Haven corner inset over the sky.
// They were RecoveryShow's Engineer-tier stick and numbers line, and Trevor ruled on 2026-10-21 that the debug line and
// Pitch/Thr are hidden in normal play. They are developer tools now (dev builds with kh.dev.recoveryStick set), and
// every piece of landing chrome follows the one booster outcome (BoosterLanding.ShowsLanding).
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    public class RecoveryDebugChromeTests
    {
        [Test]
        public void ReleaseBuilds_NeverShowTheRecoveryStickOrNumbersLine_WhateverThePref()
        {
            var on = new Dictionary<string, int> { [RecoveryShow.DevControlsPref] = 1 };
            Func<string, int> prefOn = k => on.TryGetValue(k, out var v) ? v : 0;
            Func<string, int> prefOff = k => 0;
            Assert.IsFalse(RecoveryShow.DevControls(false, prefOn), "a release build (TestFlight, App Store, WebGL) never shows them");
            Assert.IsFalse(RecoveryShow.DevControls(false, prefOff));
            Assert.IsFalse(RecoveryShow.DevControls(true, prefOff), "a dev build shows them only when a developer asks");
            Assert.IsFalse(RecoveryShow.DevControls(true, null));
            Assert.IsTrue(RecoveryShow.DevControls(true, prefOn), "dev build + kh.dev.recoveryStick");
        }

        static string Source()
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            return File.ReadAllText(Path.Combine(root, "unity", "Assets", "Kestrel.Slice", "Runtime", "RecoveryShow.cs"));
        }

        [Test]
        public void EngineerTier_NoLongerOpensTheDebugStickOrLine()
        {
            string src = Source();
            // Before: `engineer && !BurnActive(...)` showed the stick and `burn || engineer` the line for every Engineer player.
            StringAssert.DoesNotContain("stick.style.display = engineer", src, "the Engineer tier does not show Pitch/Thr");
            StringAssert.DoesNotContain("burn || engineer", src, "the Engineer tier does not show the numbers line");
            StringAssert.DoesNotContain("if (engineer && (stick == null", src, "the stick is not even built for the Engineer tier");
            StringAssert.Contains("stick.style.display = DevReadout()", src);
            StringAssert.Contains("burn || dev", src);
            StringAssert.Contains("if (devControls && (stick == null", src);
            StringAssert.Contains("Debug.isDebugBuild", src, "dev controls are read only in dev builds");
        }

        [Test]
        public void LandingChrome_FollowsTheOneBoosterOutcome()
        {
            string src = Source();
            StringAssert.Contains("pic = BoosterLanding.ForOutcome(landing.Fate, pic);", src, "follow/inset/dust/boom follow the outcome");
            StringAssert.Contains("if (landing != null && landing.ShowsLanding) BuildDeck();", src, "no Haven deck under a lost stage");
            StringAssert.Contains("if (!landing.ShowsLanding) return StageSkipTarget.None;", src, "a lost stage's burns do not stop the skip");
            // The burn panel stays the planned landing's only (BurnActive checks BoosterFate.Planned).
            StringAssert.Contains("landing.Fate == BoosterFate.Planned && sample.Phase == RecoveryPhase.Landing", src);
        }
    }
}
