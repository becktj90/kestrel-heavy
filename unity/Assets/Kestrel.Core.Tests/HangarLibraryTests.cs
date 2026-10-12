// Named saves (8 slots on NativeStore), load and migrate, rename/duplicate/delete, the session's
// dirty flag, and the starter templates (count, flyable, under budget, earned look, fork tag).
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HangarLibraryTests
    {
        string dir;
        long clock;
        int seq;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "kh-hangar-" + Guid.NewGuid().ToString("N"));
            clock = 1_760_000_000_000;
            seq = 0;
        }

        [TearDown]
        public void TearDown() { try { Directory.Delete(dir, true); } catch (IOException) { } }

        DesignLibrary Lib() => new DesignLibrary(new NativeStore(dir, () => clock += 1000, () => (++seq % 97) / 97.0));

        [Test]
        public void SaveAndLoad_RoundTripsTheDesign()
        {
            var lib = Lib();
            var d = HangarFixtures.Template("lander");
            HangarEditor.AddDecal(HangarFixtures.Data, HangarProgress.None, d, "decal_star_ring", "slot_mission_patch");
            var r = lib.Save(d, "My Lander");
            Assert.IsTrue(r.ok, r.error);
            var back = lib.Load(r.id);
            Assert.AreEqual("My Lander", back.Str("name"));
            var expect = JsonMut.CloneObject(d);
            JsonMut.Set(expect, "name", "My Lander");
            expect.Remove("id"); expect.Order.Remove("id");
            Assert.AreEqual(JsonWriter.Stringify(expect), JsonWriter.Stringify(back), "every key, including look and meta, survives");
            Assert.AreEqual(1, lib.Count);
        }

        [Test]
        public void SaveWithId_Overwrites_WithoutANewSlot()
        {
            var lib = Lib();
            var d = HangarFixtures.Template("hopper");
            var a = lib.Save(d, "Hop");
            HangarEditor.SetFins(d, false);
            var b = lib.Save(d, "Hop v2", a.id);
            Assert.AreEqual(a.id, b.id);
            Assert.AreEqual(1, lib.Count);
            Assert.IsFalse(lib.Load(a.id).Obj("aero").Bool("fins"));
            Assert.AreEqual("Hop v2", lib.NameOf(a.id));
        }

        [Test]
        public void EightSlots_ThenFull()
        {
            var lib = Lib();
            var d = HangarFixtures.Template("hopper");
            var ids = new List<string>();
            for (int i = 0; i < DesignLibrary.MaxSlots; i++) { var r = lib.Save(d, "Slot " + (i + 1)); Assert.IsTrue(r.ok); ids.Add(r.id); }
            Assert.IsTrue(lib.IsFull);
            var full = lib.Save(d, "Ninth");
            Assert.IsFalse(full.ok);
            Assert.AreEqual("full", full.error);
            Assert.AreEqual("full", lib.Duplicate(ids[0]).error);
            Assert.IsTrue(lib.Save(d, "Still fine", ids[3]).ok, "overwriting a slot works when full");
            Assert.IsTrue(lib.Delete(ids[0]));
            Assert.IsTrue(lib.Save(d, "Ninth").ok);
        }

        [Test]
        public void List_IsNewestFirst_AndRenameDuplicateDelete()
        {
            var lib = Lib();
            var a = lib.Save(HangarFixtures.Template("hopper"), "Alpha");
            var b = lib.Save(HangarFixtures.Template("heavy"), "Bravo");
            CollectionAssert.AreEqual(new[] { "Bravo", "Alpha" }, lib.List().ConvertAll(x => x.name));
            Assert.IsTrue(lib.Rename(a.id, "  Alpha   Prime ").ok);
            Assert.AreEqual("Alpha Prime", lib.Load(a.id).Str("name"), "renamed slot loads with its new name");
            var c = lib.Duplicate(b.id);
            Assert.IsTrue(c.ok);
            Assert.AreEqual("Bravo copy", c.name);
            Assert.AreNotEqual(b.id, c.id);
            Assert.AreEqual("Bravo copy 2", lib.UniqueName("Bravo copy"));
            Assert.IsTrue(lib.Delete(b.id));
            Assert.IsFalse(lib.Delete(b.id));
            Assert.IsNull(lib.Load(b.id));
            Assert.AreEqual("missing", lib.Rename("nope", "x").error);
        }

        [Test]
        public void Names_AreCleanedAndCapped()
        {
            Assert.AreEqual(DesignLibrary.DefaultName, DesignLibrary.CleanName("   "));
            Assert.AreEqual(DesignLibrary.DefaultName, DesignLibrary.CleanName(null));
            Assert.AreEqual("A B", DesignLibrary.CleanName(" A\t\nB "));
            Assert.AreEqual(DesignLibrary.NameMax, DesignLibrary.CleanName(new string('x', 80)).Length);
        }

        [Test]
        public void LegacyV1Save_LoadsMigrated()
        {
            var store = new NativeStore(dir, () => clock += 1000, () => 0.5);
            var v1 = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "presets.json"))).Arr("presets")[0] as JsonObject;
            var (id, _) = store.PutDesign(JsonMut.CloneObject(v1), "old-slot", "Old Faithful");
            var lib = new DesignLibrary(store);
            var d = lib.Load(id);
            Assert.AreEqual(DesignMigration.DesignVersion, (int)d.Num("version"));
            var s = new HangarSession(Golden.Catalog, HangarFixtures.Data, d);
            Assert.IsNotNull(s.Sheet);
            Assert.IsTrue(s.LaunchEnabled);
        }

        [Test]
        public void Session_TracksUnsavedChanges()
        {
            var lib = Lib();
            var s = new HangarSession(Golden.Catalog, HangarFixtures.Data, HangarEditor.ForkTemplate(HangarFixtures.Data.Template("hopper")));
            Assert.IsTrue(s.Dirty, "a fresh fork has never been saved");
            var r = s.SaveTo(lib, "Hopper");
            Assert.IsTrue(r.ok);
            Assert.IsFalse(s.Dirty);
            Assert.AreEqual(r.id, s.SavedId);
            s.Apply(d => HangarEditor.SetLegs(d, true));
            Assert.IsTrue(s.Dirty);
            s.Undo();
            Assert.IsFalse(s.Dirty, "undo back to the saved state is clean");
            s.Apply(d => HangarEditor.SetLegs(d, true));
            Assert.AreEqual(r.id, s.SaveTo(lib).id, "save overwrites its own slot");
            Assert.AreEqual(1, lib.Count);
            var copy = s.SaveAsNew(lib, lib.UniqueName("Hopper"));
            Assert.AreEqual("Hopper 2", copy.name);
            Assert.AreEqual(2, lib.Count);

            var reopened = new HangarSession(Golden.Catalog, HangarFixtures.Data, null);
            reopened.Load(lib.Load(r.id), r.id, lib.NameOf(r.id));
            Assert.IsFalse(reopened.Dirty);
            Assert.IsTrue(reopened.Design.Obj("landing").Bool("legs"));
            Assert.AreEqual(0, reopened.history.UndoCount, "a load starts a fresh history");
        }

        // ---------------- starter templates ----------------

        [Test]
        public void Templates_ThreeToFive_WithUniqueIds()
        {
            var t = HangarFixtures.Data.templates;
            Assert.That(t.Count, Is.InRange(3, 5));
            var ids = new HashSet<string>();
            foreach (var x in t)
            {
                Assert.IsTrue(ids.Add(x.id), x.id);
                Assert.IsFalse(string.IsNullOrEmpty(x.name), x.id);
                Assert.IsFalse(string.IsNullOrEmpty(x.kidBlurb), x.id);
                Assert.IsFalse(string.IsNullOrEmpty(x.engineerBlurb), x.id);
                Assert.AreEqual(2, (int)x.design.Num("version"), x.id + " is schema v2");
            }
        }

        [Test]
        public void Templates_FlyTheirMission_UnderBudget()
        {
            var c = Golden.Catalog;
            var lim = DesignerLimits.Active;
            foreach (var t in HangarFixtures.Data.templates)
            {
                var d = HangarFixtures.Template(t.id);
                Assert.IsTrue(Validate.Shape(c, d).ok, t.id + ": " + string.Join("; ", Validate.Shape(c, d).errors));
                var rep = Validate.Engineering(c, d);
                Assert.IsEmpty(rep.blocks, t.id + ": " + string.Join("; ", rep.blocks.ConvertAll(b => b.code + " " + b.msg)));
                Assert.GreaterOrEqual(rep.readout.margin, lim.minMargin, t.id + " Δv margin");
                Assert.GreaterOrEqual(rep.readout.liftoffTWR, lim.goodTwr, t.id + " liftoff TWR");
                var sheet = DataSheet.Build(c, HangarFixtures.Data, d, rep.readout);
                Assert.IsFalse(sheet.overBudget, $"{t.id}: {sheet.cost_Kc:N0} Kc vs {sheet.budget_Kc:N0} Kc");
                Assert.IsNull(rep.warnings.Find(w => w.code == "twr-marginal" || w.code == "dv-margin" || w.code == "landing-hardware" || w.code == "rcs-for-landing"), t.id);
            }
        }

        [Test]
        public void Templates_LookUsesStarterItemsThatExist()
        {
            var h = HangarFixtures.Data;
            foreach (var t in h.templates)
            {
                var look = t.design.Obj("look");
                Assert.IsNotNull(look, t.id);
                var lv = h.Livery(look.Str("livery"));
                Assert.IsNotNull(lv, t.id + " livery");
                Assert.AreEqual("start", lv.earn.type, t.id + " livery is owned from the start");
                var fin = look.Obj("finishes");
                foreach (var slot in fin.Order)
                {
                    var f = h.Finish(fin.Str(slot));
                    Assert.IsNotNull(f, $"{t.id} {slot}");
                    Assert.IsTrue(f.partTypes.Contains(slot), $"{t.id} {f.id} on {slot}");
                    Assert.IsTrue(HangarProgress.None.Earned(f.earn), $"{t.id} {f.id} is a starter finish");
                }
                var decals = look.Arr("decals");
                Assert.LessOrEqual(decals.Count, h.maxDecals);
                foreach (JsonObject x in decals)
                {
                    var def = h.Decal(x.Str("id"));
                    var slot = h.DecalSlot(x.Str("slot"));
                    Assert.IsNotNull(def, t.id);
                    Assert.IsNotNull(slot, t.id);
                    Assert.IsTrue(slot.content.Contains(def.content) && !slot.auto, $"{t.id} {def.id} in {slot.id}");
                    Assert.IsTrue(HangarProgress.None.Earned(def.earn), t.id);
                }
            }
        }

        [Test]
        public void Fork_TagsTheTemplate_AndDropsItsId()
        {
            var t = HangarFixtures.Data.Template("heavy");
            var d = HangarEditor.ForkTemplate(t);
            Assert.IsFalse(d.ContainsKey("id"));
            var f = d.Obj("meta").Obj("forkedFrom");
            Assert.AreEqual("heavy", f.Str("templateId"));
            Assert.AreEqual(t.version, (int)f.Num("templateVersion"));
            HangarEditor.SetLegs(d, true);
            Assert.IsFalse(t.design.Obj("landing").Bool("legs"), "the template itself is never written over");
        }

        [Test]
        public void HangarCatalogs_HaveNoMoneyClockOrRandomFields()
        {
            // Same promise the livery loader makes (earned by play only): no prices, expiry, timers, random drops or streaks.
            string[] banned = { "price", "cost", "currency", "purchase", "iap", "expires", "expiry", "until", "timeLimit", "random", "chance", "streak" };
            foreach (var f in new[] { "designer-look", "designer-templates" })
            {
                var text = File.ReadAllText(Path.Combine(Golden.Root, "data", f + ".json"));
                Walk(MiniJson.Parse(text), f, banned);
            }
            var look = HangarFixtures.Data;
            Assert.AreEqual(16, look.palette.Count, "16-colour palette");
            foreach (var hex in look.palette) StringAssert.IsMatch("^#[0-9A-F]{6}$", hex);
            foreach (var d in look.decals) Assert.Contains(d.earn.type, new[] { "start", "orbits", "landings", "mission" }, d.id);
        }

        static void Walk(object v, string where, string[] banned)
        {
            if (v is JsonObject o)
                foreach (var k in o.Order)
                {
                    foreach (var b in banned) Assert.IsFalse(k.Equals(b, StringComparison.OrdinalIgnoreCase), $"{where}.{k}");
                    Walk(o[k], where + "." + k, banned);
                }
            else if (v is List<object> a) for (int i = 0; i < a.Count; i++) Walk(a[i], $"{where}[{i}]", banned);
        }
    }
}
