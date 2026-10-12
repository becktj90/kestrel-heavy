// Hangar UI layer (G3 part 2): exploded view, part picking, camera focus, readout words, panel metrics,
// per-part meshes, and a smoke test that builds the panel for every template, tab and part with no exceptions.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class HangarUiTests
    {
        static Catalog catalog; static HangarData data; static string root;
        static string Root => root ??= Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static Catalog C => catalog ??= Catalog.LoadFromRepo(Root);
        static HangarData H => data ??= HangarData.LoadFromRepo(Root);

        static JsonObject Template(string id) => HangarEditor.ForkTemplate(H.Template(id));

        // ---------------- exploded view ----------------

        [Test]
        public void Exploded_Assembled_IsZeroEverywhere()
        {
            foreach (var p in new[] { PartRef.Stage(0), PartRef.Stage(2), PartRef.Interstage(1), PartRef.Fairing, PartRef.Payload, PartRef.Boosters })
                Assert.AreEqual(Vector3.zero, ExplodedView.Offset(p, 3, 80f, 0f, Vector3.right), p.ToString());
        }

        [Test]
        public void Exploded_PartsRiseInStackOrder_WithoutOverlap()
        {
            int n = 3; float h = 80f, gap = ExplodedView.GapFraction * h;
            float Y(PartRef p) => ExplodedView.Offset(p, n, h, 1f, Vector3.zero).y;
            Assert.AreEqual(0f, Y(PartRef.Stage(0)), 1e-4f, "stage 1 stays on the pad");
            Assert.AreEqual(gap * 0.5f, Y(PartRef.Interstage(1)), 1e-4f);
            Assert.AreEqual(gap, Y(PartRef.Stage(1)), 1e-4f);
            Assert.AreEqual(gap * 2f, Y(PartRef.Stage(2)), 1e-4f);
            Assert.Greater(Y(PartRef.Payload), Y(PartRef.Stage(2)));
            Assert.Greater(Y(PartRef.Fairing), Y(PartRef.Payload), "the fairing lifts off the cargo");
            Assert.AreEqual(gap, ExplodedView.Offset(PartRef.Boosters, n, h, 1f, new Vector3(0, 0, 5)).magnitude, 1e-4f, "boosters slide out one gap");
            Assert.AreEqual(0f, ExplodedView.Offset(PartRef.Boosters, n, h, 1f, new Vector3(0, 0, 5)).y, 1e-6f);
            Assert.AreEqual(h * (1 + ExplodedView.GapFraction * 3f), ExplodedView.ExplodedHeight(h, n), 1e-3f);
        }

        [Test]
        public void Exploded_EasesOver400ms()
        {
            Assert.AreEqual(0.4f, ExplodedView.Seconds);
            Assert.AreEqual(0f, ExplodedView.Ease(0f));
            Assert.AreEqual(0.5f, ExplodedView.Ease(0.2f), 1e-4f);
            Assert.AreEqual(1f, ExplodedView.Ease(0.4f));
            Assert.AreEqual(1f, ExplodedView.Ease(5f));
            Assert.AreEqual(Vector3.zero, ExplodedView.Offset(PartRef.Stage(1), 2, 70f, float.NaN, Vector3.zero));
        }

        // ---------------- picking ----------------

        [Test]
        public void Picker_FindsTheTappedPart_AndIgnoresEmptySky()
        {
            var parts = new List<ScreenPart>
            {
                new ScreenPart { part = PartRef.Stage(0), bottom = new Vector2(500, 100), top = new Vector2(500, 900), radiusPx = 60, depth = 100 },
                new ScreenPart { part = PartRef.Stage(1), bottom = new Vector2(500, 950), top = new Vector2(500, 1400), radiusPx = 60, depth = 100 },
                new ScreenPart { part = PartRef.Fairing, bottom = new Vector2(500, 1400), top = new Vector2(500, 1800), radiusPx = 80, depth = 100 },
            };
            Assert.IsTrue(PartPicker.Pick(parts, new Vector2(520, 400), out var p)); Assert.AreEqual(PartRef.Stage(0), p);
            Assert.IsTrue(PartPicker.Pick(parts, new Vector2(470, 1200), out p)); Assert.AreEqual(PartRef.Stage(1), p);
            Assert.IsTrue(PartPicker.Pick(parts, new Vector2(500, 1700), out p)); Assert.AreEqual(PartRef.Fairing, p);
            Assert.IsFalse(PartPicker.Pick(parts, new Vector2(100, 1200), out _), "far from the rocket");
            Assert.IsFalse(PartPicker.Pick(null, Vector2.zero, out _));
        }

        [Test]
        public void Picker_PrefersTheNearerPartWhenTheyOverlap()
        {
            var parts = new List<ScreenPart>
            {
                new ScreenPart { part = PartRef.Stage(0), bottom = new Vector2(500, 100), top = new Vector2(500, 900), radiusPx = 60, depth = 120 },
                new ScreenPart { part = PartRef.Boosters, bottom = new Vector2(500, 100), top = new Vector2(500, 600), radiusPx = 60, depth = 90 },
            };
            Assert.IsTrue(PartPicker.Pick(parts, new Vector2(500, 300), out var p));
            Assert.AreEqual(PartRef.Boosters, p);
            Assert.AreEqual(5f, PartPicker.DistanceToSegment(new Vector2(5, 3), new Vector2(0, 0), new Vector2(0, 3)), 1e-5f);
        }

        // ---------------- camera focus and taps ----------------

        [Test]
        public void Focus_ZoomFillsHalfTheView_WithinTheZoomLimits()
        {
            Assert.AreEqual(1f, HangarFocus.ZoomFor(70f, 35f), 1e-5f);
            Assert.AreEqual(HangarMotion.ZoomMax, HangarFocus.ZoomFor(70f, 2f), 1e-5f, "small parts stop at the max zoom");
            Assert.AreEqual(HangarMotion.ZoomMin, HangarFocus.ZoomFor(70f, 500f), 1e-5f);
            Assert.AreEqual(1f, HangarFocus.ZoomFor(70f, float.NaN));
            Assert.AreEqual(5f, HangarFocus.Approach(1f, 5f, 0.016f, reduceMotion: true), "Reduce Motion snaps");
            float v = HangarFocus.Approach(1f, 5f, 0.016f, false);
            Assert.That(v, Is.GreaterThan(1f).And.LessThan(5f));
            float t = 1f; for (int i = 0; i < 120; i++) t = HangarFocus.Approach(t, 5f, 1f / 60f, false);
            Assert.AreEqual(5f, t, "settles exactly");
        }

        [Test]
        public void PoseAt_NaN_IsTheRestPose_AndAimFollowsTheFocus()
        {
            HangarOrbit.Pose(70f, 1.2f, 20f, 10f, 1.5f, out var p0, out var a0);
            HangarOrbit.PoseAt(70f, 1.2f, 20f, 10f, 1.5f, float.NaN, out var p1, out var a1);
            Assert.AreEqual(p0, p1); Assert.AreEqual(a0, a1);
            HangarOrbit.PoseAt(70f, 1.2f, 20f, 10f, 1.5f, 60f, out var p2, out var a2);
            Assert.AreEqual(60f, a2.y, 1e-4f);
            Assert.AreEqual((p0 - a0).magnitude, (p2 - a2).magnitude, 1e-3f, "focus slides the view; distance is unchanged");
        }

        [Test]
        public void Tap_IsShortAndStill()
        {
            Assert.IsTrue(HangarCamera.IsTap(new Vector2(10, 10), new Vector2(14, 12), 0.1f, 12f));
            Assert.IsFalse(HangarCamera.IsTap(new Vector2(10, 10), new Vector2(40, 10), 0.1f, 12f), "moved: an orbit drag");
            Assert.IsFalse(HangarCamera.IsTap(new Vector2(10, 10), new Vector2(10, 10), 0.8f, 12f), "held: not a tap");
        }

        // ---------------- words and metrics ----------------

        [Test]
        public void PropSteps_FollowTheSpecAndSnap()
        {
            Assert.AreEqual(1000, HangarFormat.PropStepKg(50000));
            Assert.AreEqual(10000, HangarFormat.PropStepKg(300000));
            Assert.AreEqual(50000, HangarFormat.PropStepKg(2000000));
            Assert.AreEqual(301000 + 9000, HangarFormat.StepProp(300500, +1), 1e-6, "snaps up to the 10 t grid");
            Assert.AreEqual(300000, HangarFormat.StepProp(300500, -1), 1e-6, "snaps down to the grid");
            Assert.AreEqual(99000, HangarFormat.StepProp(100000, -1), 1e-6, "crossing down into the 1 t band");
            Assert.AreEqual(1000, HangarFormat.StepProp(1000, -1), 1e-6, "never below one step");
        }

        [Test]
        public void ReadoutCells_KidHasThree_EngineerHasSix()
        {
            var s = new HangarSession(C, H, Template("workhorse"));
            var kid = HangarFormat.Cells(s.Sheet, false);
            var eng = HangarFormat.Cells(s.Sheet, true);
            CollectionAssert.AreEqual(new[] { "Speed", "Push", "Cost" }, kid.ConvertAll(c => c.label));
            CollectionAssert.AreEqual(new[] { "Δv", "TWR", "Cost", "Mass", "Stability", "Reliability" }, eng.ConvertAll(c => c.label));
            Assert.AreEqual(CellState.Good, kid[0].state, "the template reaches orbit");
            Assert.AreEqual(CellState.Good, kid[1].state);
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 1));
            Assert.AreEqual(CellState.Bad, HangarFormat.Cells(s.Sheet, false)[1].state, "too heavy shows red");
            Assert.AreEqual(3, HangarFormat.Cells(null, false).Count, "an uncomputable design still shows three dashes");
        }

        [Test]
        public void Subtitles_DescribeEachPart()
        {
            var d = Template("heavy");
            StringAssert.Contains("9 ×", HangarFormat.Subtitle(C, d, PartRef.Stage(0)));
            StringAssert.Contains("KH-3", HangarFormat.Subtitle(C, d, PartRef.Payload));
            StringAssert.Contains("GTO", HangarFormat.Subtitle(C, d, PartRef.Payload));
            Assert.AreEqual("Straight", HangarFormat.Subtitle(C, d, PartRef.Interstage(1)));
            StringAssert.StartsWith("Ogive", HangarFormat.Subtitle(C, d, PartRef.Fairing));
            Assert.AreEqual("Gas-generator vac", HangarFormat.EngineChip(C.EngineById["gg-kerolox-vac"]));
        }

        [Test]
        public void PanelLayout_LeavesTheRocketItsBand_OnEveryPhone()
        {
            // SE 667 pt, Pro 852 pt, Pro Max 956 pt; bottom safe area 0 / 34 / 34 pt.
            foreach (var (h, safe) in new[] { (667f, 0f), (852f, 34f), (956f, 34f) })
            {
                float sheet = HangarPanelLayout.FixedPt + HangarPanelLayout.ContentMaxPt(h, safe) + safe;
                Assert.LessOrEqual(sheet / h, 1f - DesignFraming.MinVisibleFraction + 1e-4f, $"{h} pt: sheet {sheet} pt");
                Assert.GreaterOrEqual(HangarPanelLayout.ContentMaxPt(h, safe), HangarPanelLayout.MinContentPt);
            }
            Assert.IsFalse(HangarPanelLayout.SidePanel(1179, 2556), "iPhone: bottom sheet");
            Assert.IsTrue(HangarPanelLayout.SidePanel(2064, 2752), "iPad 13: side panel");
            Assert.AreEqual(DesignFraming.HasSidePanel(1640, 2360), HangarPanelLayout.SidePanel(1640, 2360));
        }

        // ---------------- per-part meshes ----------------

        [Test]
        public void BuildParts_StackMatchesTheFlightMesh()
        {
            foreach (var t in H.templates)
            {
                var d = Design.FromJson(Template(t.id));
                var whole = RocketMeshBuilder.Build(C, d);
                var parts = RocketMeshBuilder.BuildParts(C, d);
                try
                {
                    var fairing = parts.Find(p => p.Part == PartRef.Fairing);
                    Assert.AreEqual(whole.height, fairing.Y1, 1e-3f, t.id + ": the fairing tip is the rocket top");
                    for (int i = 0; i < d.stages.Count; i++)
                    {
                        var s = parts.Find(p => p.Part == PartRef.Stage(i));
                        Assert.IsNotNull(s, $"{t.id} stage {i}");
                        Assert.Greater(s.Mesh.vertexCount, 0);
                        Assert.Greater(s.Engines.vertexCount, 0, $"{t.id} stage {i} engines are drawn");
                        if (i > 0)
                        {
                            var joint = parts.Find(p => p.Part == PartRef.Interstage(i));
                            var below = parts.Find(p => p.Part == PartRef.Stage(i - 1));
                            Assert.AreEqual(below.Y1, joint.Y0, 1e-3f, $"{t.id} joint {i} sits on the stage below");
                        }
                    }
                    Assert.IsNotNull(parts.Find(p => p.Part == PartRef.Payload), t.id);
                    var b = parts.Find(p => p.Part == PartRef.Boosters);
                    Assert.AreEqual(d.StrapOnCount > 0, b != null, t.id);
                    if (b != null) Assert.AreEqual(d.StrapOnCount, b.Instances.Length);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(whole.mesh);
                    foreach (var p in parts) { UnityEngine.Object.DestroyImmediate(p.Mesh); if (p.Engines != null) UnityEngine.Object.DestroyImmediate(p.Engines); }
                }
            }
        }

        // ---------------- panel smoke test ----------------

        [Test]
        public void Panel_BuildsEveryTabForEveryPart_WithoutErrors()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-hangar-ui-" + Guid.NewGuid().ToString("N"));
            int prev = PlayerPrefs.GetInt(HangarPanel.EngineerPref, 0);
            try
            {
                var lib = new DesignLibrary(new NativeStore(dir, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => 0.5));
                foreach (var t in H.templates)
                {
                    var s = new HangarSession(C, H, Template(t.id));
                    var body = new VisualElement();
                    var panel = new HangarPanel(s, lib, body, 3f);
                    foreach (bool eng in new[] { false, true })
                    {
                        panel.SetEngineer(eng);
                        foreach (HangarPanel.Tab tab in Enum.GetValues(typeof(HangarPanel.Tab)))
                            foreach (var info in HangarParts.List(s.Design))
                            {
                                s.Select(info.part);
                                panel.Show(tab);
                                Assert.Greater(body.childCount, 0, $"{t.id} {tab} {info.part}");
                            }
                    }
                    // Edits after the panel is up: refresh and rebuild paths.
                    s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 1));
                    s.Apply(d => HangarEditor.AddStage(C, d, d.Arr("stages").Count));
                    s.Apply(d => HangarEditor.SetBoosterCount(C, d, 2));
                    s.Select(PartRef.Boosters);
                    panel.Show(HangarPanel.Tab.Part);
                    s.Undo(); s.Undo(); s.Redo();
                    s.SaveTo(lib, t.name);
                    panel.Show(HangarPanel.Tab.Designs);
                    panel.SetScale(1.2f);
                    panel.Detach();
                }
            }
            finally
            {
                PlayerPrefs.SetInt(HangarPanel.EngineerPref, prev);
                try { Directory.Delete(dir, true); } catch (IOException) { }
            }
        }

        [Test]
        public void Words_TurnIdsIntoLabels()
        {
            Assert.AreEqual("Star ring", HangarPanel.Words("decal_star_ring"));
            Assert.AreEqual("Wordmark vertical", HangarPanel.Words("slot_wordmark_vertical"));
            Assert.AreEqual("Night shift", HangarPanel.Words("night_shift"));
            Assert.AreEqual("", HangarPanel.Words(null));
        }
    }
}
