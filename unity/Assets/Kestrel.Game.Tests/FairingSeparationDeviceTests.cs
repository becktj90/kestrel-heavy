// Fairing and stage separation as drawn (Trevor: "fairing separation works visually in Unity").
// Since #288 the visible rocket is HangarRocketView (painted parts) and the combined mesh is undrawn, so the
// staging show has to hide the painted part each moving piece replaces. This builds the real flight picture for
// campaign winners (painted parts, fairing seam cluster, FlightStageShow), flies the real mission sim, plays it
// through FlightStageShow.PreviewAt, and calls SetVisible(true) every step as SliceApp.ShowFlightPicture does.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class FairingSeparationDeviceTests
    {
        static Catalog catalog; static Campaign campaign; static string root;
        static string Root => root ??= Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static Catalog C => catalog ??= Catalog.LoadFromRepo(Root);
        static Campaign K => campaign ??= Campaign.Load(Root);

        readonly List<Object> made = new List<Object>();

        [TearDown]
        public void Clean()
        {
            foreach (var o in made) if (o != null) Object.DestroyImmediate(o);
            made.Clear();
        }

        sealed class Rig
        {
            public GameObject Rocket;
            public HangarRocketView View;
            public Kestrel.Vehicle.StrutCluster Seams;
            public FlightStageShow Show;
            public Design Design;
            public SimResult Flight;
        }

        static (CampaignMission mission, Design design) Winner(string id)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root, "data", "golden", "campaign-winners.json")));
            foreach (JsonObject row in file.Arr("missions"))
                if (row.Str("id") == id) return (K.ById(id), Design.FromJson((JsonObject)row.Arr("designs")[0]));
            Assert.Fail("no campaign winner " + id);
            return default;
        }

        Rig Build(string id)
        {
            var (mission, design) = Winner(id);
            var built = RocketMeshBuilder.Build(C, design);
            var parts = RocketMeshBuilder.BuildParts(C, design);
            var go = new GameObject("Rocket");
            made.Add(go);
            go.AddComponent<MeshFilter>().sharedMesh = built.mesh;
            go.AddComponent<MeshRenderer>().forceRenderingOff = true;   // as SliceApp.BuildRocket: the parts are the picture
            var view = new HangarRocketView();
            view.Build(parts, go.transform, null);
            var seams = go.AddComponent<Kestrel.Vehicle.StrutCluster>();
            var show = go.AddComponent<FlightStageShow>();
            show.Prepare(C, design, built, null, view, seams);
            show.Arm(null, false);
            view.SetVisible(true);

            var setup = MissionFlightSetup.Build(C, mission, design, MissionFlightSetup.BatchDt, null, MissionFlightSetup.BatchDt);
            var sim = new AscentSim(setup.Stages, setup.Options);
            while (!sim.Done) sim.Tick();
            return new Rig { Rocket = go, View = view, Seams = seams, Show = show, Design = design, Flight = sim.Finish() };
        }

        static void Step(Rig rig, SimSample s)
        {
            rig.Show.PreviewAt(rig.Flight.events, s.t, s.alt, s.v, s.q, 1);
            rig.View.SetVisible(true);   // SliceApp.ShowFlightPicture() runs after staging.Present() every frame
        }

        static bool Active(HangarRocketView view, PartRef part)
        {
            var t = view.RootOf(part);
            return t != null && t.gameObject.activeSelf;
        }

        [TestCase("m3")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void FairingSeparation_SwapsThePaintedFairingForItsHalves_AndRevealsThePayload(string id)
        {
            var rig = Build(id);
            Assert.That(rig.View.RootOf(PartRef.Fairing), Is.Not.Null, id + ": no painted fairing part");
            Assert.That(rig.View.RootOf(PartRef.Payload), Is.Not.Null, id + ": no payload part");
            Assert.That(rig.Show.FairingHalf(false), Is.Not.Null, id + ": no FairingL");
            Assert.That(rig.Show.FairingHalf(true), Is.Not.Null, id + ": no FairingR");

            bool seen = false;
            foreach (var s in rig.Flight.samples)
            {
                Step(rig, s);
                bool sep = rig.Show.Parts.FairingSeparated;
                if (!sep)
                {
                    Assert.IsTrue(Active(rig.View, PartRef.Fairing), id + " painted fairing missing before separation at T+" + s.t);
                    Assert.IsFalse(Active(rig.View, PartRef.Payload), id + " payload drawn through the fairing at T+" + s.t);
                    Assert.IsFalse(rig.Show.FairingHalf(false).gameObject.activeSelf, id + " FairingL early at T+" + s.t);
                    Assert.IsTrue(rig.Seams.enabled, id + " seams off early at T+" + s.t);
                    continue;
                }
                if (!seen)
                {
                    seen = true;
                    Assert.That(s.alt, Is.GreaterThanOrEqualTo(108000), id + " fairing below the 110 km gate");
                    Assert.IsTrue(rig.Show.FairingHalf(false).gameObject.activeSelf, id + " FairingL not shown on the separation frame");
                    Assert.IsTrue(rig.Show.FairingHalf(true).gameObject.activeSelf, id + " FairingR not shown on the separation frame");
                    TestContext.WriteLine($"{id}: fairing swap T+{s.t:0.0} s at {s.alt / 1000:0.0} km");
                }
                Assert.IsFalse(Active(rig.View, PartRef.Fairing), id + " painted fairing still drawn at T+" + s.t);
                Assert.IsTrue(rig.View.IsPartHidden(PartRef.Fairing));
                Assert.IsTrue(Active(rig.View, PartRef.Payload), id + " payload not revealed at T+" + s.t);
                Assert.IsFalse(rig.Seams.enabled, id + " fairing seams and rings still drawn at T+" + s.t);
            }
            Assert.IsTrue(seen, id + " never separated its fairing");
        }

        [TestCase("m3")]
        [TestCase("m10")]
        public void EachRelease_HidesItsPaintedPart_AndShowsItsPiece_UntilReset(string id)
        {
            var rig = Build(id);
            int stages = rig.Design.stages.Count;
            var releasedStage = new bool[stages];
            bool boostersOut = false;
            foreach (var s in rig.Flight.samples)
            {
                Step(rig, s);
                for (int i = 0; i < stages; i++)
                {
                    bool hidden = rig.Show.Parts.IsHidden(PartRef.Stage(i));
                    if (hidden) releasedStage[i] = true;
                    var piece = rig.Show.PieceOf("stage", i);
                    if (piece == null) continue;
                    Assert.AreEqual(!releasedStage[i], Active(rig.View, PartRef.Stage(i)), $"{id} painted stage {i + 1} at T+{s.t}");
                    if (releasedStage[i] && i + 1 < stages && rig.View.RootOf(PartRef.Interstage(i + 1)) != null)
                        Assert.IsFalse(Active(rig.View, PartRef.Interstage(i + 1)), $"{id} interstage on stage {i + 1} left behind at T+{s.t}");
                    // The piece and the painted part never draw together, and one of them always draws.
                    Assert.AreNotEqual(piece.gameObject.activeSelf, Active(rig.View, PartRef.Stage(i)), $"{id} stage {i + 1} drawn twice or not at all at T+{s.t}");
                }
                if (rig.Design.StrapOnCount > 0)
                {
                    if (rig.Show.Parts.IsHidden(PartRef.Boosters)) boostersOut = true;
                    Assert.AreEqual(!boostersOut, Active(rig.View, PartRef.Boosters), $"{id} painted boosters at T+{s.t}");
                    for (int k = 0; k < rig.Design.StrapOnCount; k++)
                    {
                        var b = rig.Show.PieceOf("booster", k);
                        if (b != null) Assert.AreEqual(boostersOut, b.gameObject.activeSelf, $"{id} booster piece {k} at T+{s.t}");
                    }
                }
            }
            Assert.IsTrue(releasedStage[0], id + ": stage 1 never released");
            if (rig.Design.StrapOnCount > 0) Assert.IsTrue(boostersOut, id + ": boosters never released");

            // A new flight (Arm resets) puts the assembled stack back on the next ShowFlightPicture.
            rig.Show.Arm(null, false);
            rig.View.SetVisible(true);
            foreach (var p in rig.View.Pieces)
            {
                bool want = p.mesh.Part.kind != PartKind.Payload;
                Assert.AreEqual(want, p.root.gameObject.activeSelf, id + " after reset: " + p.mesh.Part);
            }
            Assert.IsTrue(rig.Seams.enabled, id + " seams after reset");
            Assert.IsFalse(rig.Show.FairingHalf(false).gameObject.activeSelf);
            Assert.IsFalse(rig.Show.FairingHalf(true).gameObject.activeSelf);
            for (int i = 0; i < stages; i++)
            {
                var piece = rig.Show.PieceOf("stage", i);
                if (piece != null) Assert.IsFalse(piece.gameObject.activeSelf, id + " stage piece after reset");
            }
            Assert.AreEqual(0, rig.Show.Parts.Hidden.Count);
        }
    }
}
