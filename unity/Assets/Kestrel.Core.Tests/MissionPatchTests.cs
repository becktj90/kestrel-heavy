// Mission patches: one emblem recipe per KH flight, clean player text, and a layout that
// keeps the rim lettering and the stars inside the disc on phone and tablet sizes.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissionPatchTests
    {
        static readonly int[] Sizes = { 48, 76, 96, 128, 160, 256 };

        [Test]
        public void EveryCampaignFlightHasItsOwnEmblem()
        {
            Assert.AreEqual(MissionPatchArt.CampaignCount, MissionPatchArt.CampaignPatches.Count);
            var seeds = new HashSet<ulong>();
            var recipes = new HashSet<string>();
            for (int i = 1; i <= 20; i++)
            {
                var spec = MissionPatchArt.ForIndex(i);
                Assert.AreEqual(i, spec.Index);
                Assert.AreEqual("KH-" + i, spec.Flight);
                Assert.AreEqual("m" + i, spec.MissionId);
                Assert.IsFalse(seeds.Add(spec.EmblemSeed) == false, spec.Flight);
                string recipe = ((uint)spec.Marks) + "|" + spec.Stars + "|" + (int)spec.Act;
                Assert.IsTrue(recipes.Add(recipe), spec.Flight + " " + recipe);
                Assert.AreNotEqual((ulong)i, spec.EmblemSeed);
                Assert.AreEqual(spec.EmblemSeed, MissionPatchArt.Resolve(spec.MissionId).EmblemSeed);
                Assert.AreEqual(spec.Name, MissionPatchArt.Resolve("KH-" + i).Name);
            }
        }

        [Test]
        public void ActsFollowTheWorldBibleAndTheMoonPath()
        {
            for (int i = 1; i <= 5; i++)
                Assert.AreEqual(PatchAct.FirstLight, MissionPatchArt.ForIndex(i).Act, "KH-" + i);
            foreach (var i in new[] { 6, 7, 8, 10, 11, 12, 13, 14, 15 })
                Assert.AreEqual(PatchAct.Regulars, MissionPatchArt.ForIndex(i).Act, "KH-" + i);
            Assert.AreEqual(PatchAct.Heavy, MissionPatchArt.ForIndex(16).Act);
            Assert.AreEqual(PatchAct.Heavy, MissionPatchArt.ForIndex(19).Act);
            foreach (var i in new[] { 9, 17, 18, 20 })
            {
                var spec = MissionPatchArt.ForIndex(i);
                Assert.AreEqual(PatchAct.FarShore, spec.Act, spec.Flight);
                Assert.IsTrue((spec.Marks & PatchMarks.Moon) != 0, spec.Flight);
            }
            Assert.IsTrue((MissionPatchArt.ForIndex(7).Marks & PatchMarks.Barge) != 0);
            Assert.IsTrue((MissionPatchArt.ForIndex(13).Marks & PatchMarks.Barge) != 0);
            Assert.AreEqual(6, MissionPatchArt.ForIndex(13).Stars);
            Assert.AreEqual(0, MissionPatchArt.ForIndex(1).Stars);
            Assert.AreEqual(0, MissionPatchArt.ForIndex(8).Stars);
            Assert.AreEqual(1, MissionPatchArt.ForIndex(3).Stars);

            int field = MissionPatchArt.Palette(PatchAct.FirstLight).Field;
            Assert.AreNotEqual(field, MissionPatchArt.Palette(PatchAct.Regulars).Field);
            Assert.AreNotEqual(field, MissionPatchArt.Palette(PatchAct.Heavy).Field);
            Assert.AreNotEqual(field, MissionPatchArt.Palette(PatchAct.FarShore).Field);
            Assert.AreEqual("First Light", MissionPatchArt.ForIndex(1).ActName);
            Assert.AreEqual("Far Shore", MissionPatchArt.ForIndex(9).ActName);
        }

        [Test]
        public void PlayerLinesHaveNoRawIds()
        {
            for (int i = 1; i <= 20; i++) AssertClean(MissionPatchArt.ForIndex(i));
            AssertClean(MissionPatchArt.Resolve("kc-pier-7"));
            AssertClean(MissionPatchArt.Resolve("contract-west"));
            AssertClean(MissionPatchArt.Resolve("free"));
            AssertClean(MissionPatchArt.Resolve("sandbox-1"));
            AssertClean(MissionPatchArt.Resolve(null));
            var job = MissionPatchArt.Resolve("kc-pier-7");
            Assert.AreEqual("JOB", job.Flight);
            Assert.That(job.Name, Does.Not.Contain("kc-"));
            Assert.That(job.Motto, Does.Not.Contain("pier-7"));
            Assert.AreEqual(job.EmblemSeed, MissionPatchArt.Resolve("kc-pier-7").EmblemSeed);
            Assert.AreNotEqual(job.EmblemSeed, MissionPatchArt.Resolve("kc-pier-8").EmblemSeed);
            Assert.AreEqual("FREE", MissionPatchArt.Resolve("free").Flight);
            Assert.AreEqual("FREE", MissionPatchArt.Resolve(null).Flight);
            Assert.AreEqual(MissionPatchArt.Resolve("sandbox").EmblemSeed, MissionPatchArt.Resolve("sandbox").EmblemSeed);
        }

        [Test]
        public void CampaignFileNamesMatchThePatch()
        {
            var campaign = Campaign.Load(Golden.Root);
            Assert.GreaterOrEqual(campaign.Missions.Count, 1);
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var mission = campaign.Missions[i];
                if (mission.Index < 1 || mission.Index > 20) continue;
                var spec = MissionPatchArt.Resolve(mission.Id);
                Assert.AreEqual(mission.Name, spec.Name, mission.Id);
                Assert.AreEqual(mission.Index, spec.Index, mission.Id);
            }
        }

        [Test]
        public void LayoutFitsSeProMaxAndIpadSizes()
        {
            foreach (var spec in MissionPatchArt.CampaignPatches)
            foreach (var size in Sizes)
            {
                var open = MissionPatchArt.Layout(spec, size, PatchChrome.Open);
                var medal = MissionPatchArt.Layout(spec, size, PatchChrome.Medal);
                var locked = MissionPatchArt.Layout(spec, size, PatchChrome.Locked);
                Assert.IsTrue(open.ArcFontCovers, spec.Flight + " font");
                Assert.IsTrue(open.FlightFits, spec.Flight + " at " + size);
                Assert.IsTrue(open.EmblemInside, spec.Flight);
                Assert.AreEqual(spec.Stars, open.StarCount);
                Assert.IsTrue(open.StarsInside, spec.Flight + " stars " + size);
                Assert.IsTrue(open.StarsSeparated, spec.Flight + " gap " + size);
                Assert.IsFalse(open.GoldRing);
                Assert.IsFalse(open.GreyOutline);
                Assert.IsTrue(medal.GoldRing);
                Assert.IsTrue(locked.GreyOutline);
                if (size >= 96) Assert.IsTrue(open.NameOnArc, spec.Flight + " " + spec.ArcName + " at " + size);
            }
        }

        [Test]
        public void CollectionGridFitsThePanelWidths()
        {
            Assert.AreEqual(2, PatchGrid.Columns(308f));
            Assert.AreEqual(2, PatchGrid.Columns(375f));
            Assert.AreEqual(3, PatchGrid.Columns(500f));
            Assert.GreaterOrEqual(PatchGrid.PatchSize(308f), 96f);
            Assert.GreaterOrEqual(PatchGrid.PatchSize(375f), 96f);
            Assert.GreaterOrEqual(PatchGrid.PatchSize(500f), 96f);
            Assert.LessOrEqual(PatchGrid.PatchSize(2064f), 148f);
            var narrow = MissionPatchArt.Layout(MissionPatchArt.ForIndex(12), (int)PatchGrid.PatchSize(308f));
            Assert.IsTrue(narrow.NameOnArc, "Glass Meridian on Pro Max");
            Assert.IsTrue(narrow.FlightFits);
        }

        [Test]
        public void PixelsAreRoundDistinctAndStable()
        {
            var hashes = new HashSet<ulong>();
            for (int i = 1; i <= 20; i++)
            {
                var a = MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Open);
                var b = MissionPatchArt.Rgba("KH-" + i, 96, PatchChrome.Open);
                CollectionAssert.AreEqual(a, b);
                Assert.AreEqual(96 * 96 * 4, a.Length);
                Assert.AreEqual(0, a[3], "corner");
                int mid = (48 * 96 + 48) * 4;
                Assert.Greater(a[mid + 3], 200, "center");
                Assert.IsTrue(hashes.Add(MissionPatchArt.PixelHash(a)), "KH-" + i);
            }
            var open = MissionPatchArt.Rgba("m7", 96, PatchChrome.Open);
            var medal = MissionPatchArt.Rgba("m7", 96, PatchChrome.Medal);
            var locked = MissionPatchArt.Rgba("m7", 96, PatchChrome.Locked);
            Assert.Greater(Gold(medal), Gold(open));
            Assert.Less(Chroma(locked), Chroma(open) * 0.45f);
            var png = MissionPatchArt.Png("m1", 64);
            Assert.AreEqual(137, png[0]);
            Assert.AreEqual(80, png[1]);
            Assert.AreEqual(78, png[2]);
            Assert.AreEqual(71, png[3]);
            var same = MissionPatchArt.Resolve("kc-pier-7");
            CollectionAssert.AreEqual(MissionPatchArt.Rgba(same.MissionId, 64), MissionPatchArt.Rgba("kc-pier-7", 64));
        }

        [Test]
        public void SecondPassRgbaReusesTheBitmap()
        {
            for (int i = 1; i <= 20; i++)
            {
                MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Open);
                MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Medal);
                MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Locked);
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 1; i <= 20; i++)
                {
                    var open = MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Open);
                    Assert.AreSame(open, MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Open));
                    var medal = MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Medal);
                    Assert.AreSame(medal, MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Medal));
                    var locked = MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Locked);
                    Assert.AreSame(locked, MissionPatchArt.Rgba("m" + i, 96, PatchChrome.Locked));
                }
            }
            watch.Stop();
            long alloc = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(15), "cached patch pass " + watch.ElapsedMilliseconds + " ms");
            Assert.That(alloc, Is.LessThan(64 * 1024), "cached patch allocations " + alloc);
        }

        static void AssertClean(MissionPatchSpec spec)
        {
            foreach (var line in new[] { spec.Flight, spec.Name, spec.Motto, spec.Anatomy, spec.ActName, spec.ArcName })
            {
                Assert.IsFalse(PlayerText.Leak(line), line);
                Assert.That(line, Does.Not.Contain("kh-m").IgnoreCase);
                Assert.That(line, Does.Not.Contain("payload"));
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(line, @"\bm\d+\b"), line);
            }
            Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Motto));
            Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Anatomy));
            Assert.Less(spec.Anatomy.Length, 110);
        }

        static int Gold(byte[] rgba)
        {
            int n = 0;
            for (int i = 0; i < rgba.Length; i += 4)
                if (rgba[i] > 200 && rgba[i + 1] > 140 && rgba[i + 1] < 220 && rgba[i + 2] < 120 && rgba[i + 3] > 200) n++;
            return n;
        }

        static float Chroma(byte[] rgba)
        {
            double sum = 0;
            int n = 0;
            for (int i = 0; i < rgba.Length; i += 4)
            {
                if (rgba[i + 3] < 200) continue;
                int r = rgba[i], g = rgba[i + 1], b = rgba[i + 2];
                int max = Math.Max(r, Math.Max(g, b));
                int min = Math.Min(r, Math.Min(g, b));
                sum += max - min;
                n++;
            }
            return n == 0 ? 0 : (float)(sum / n);
        }
    }
}
