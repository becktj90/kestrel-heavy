// Draft autosave, settings mirror, and Continue. The career file stays byte-for-byte alone.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class SaveResumeTests
    {
        [Test]
        public void Draft_IsNotASlot_AndProgressKeepsExistingKeys()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-draft-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new NativeStore(dir, () => 50, () => 0.2);
                var progress = new JsonObject();
                progress.Add("orbitsCompleted", 3d);
                store.PutProgress(progress);
                var library = new DesignLibrary(store);
                var design = new JsonObject();
                design.Add("version", 2d);
                design.Add("name", "Hopper");
                library.SaveDraft(design, "Hopper", "m1");

                Assert.IsFalse(NativeStore.DraftKey.StartsWith(NativeStore.DesignPrefix, StringComparison.Ordinal));
                Assert.AreEqual(0, library.Count);
                Assert.AreEqual(0, store.ListDesigns().Count);
                Assert.IsTrue(library.HasDraft());
                var loaded = library.LoadDraft(out string mission, out string name);
                Assert.AreEqual("m1", mission);
                Assert.AreEqual("Hopper", name);
                Assert.AreEqual("Hopper", loaded.Str("name"));

                var again = store.GetProgress();
                Assert.AreEqual(3d, again.Num("orbitsCompleted"));
                Assert.IsTrue(again.Bool("hasDraft"));
                Assert.AreEqual("Hopper", again.Str("draftName"));
                Assert.AreEqual("m1", again.Str("lastMissionId"));
                Assert.IsFalse(again.ContainsKey("bankMinor"));
                Assert.IsFalse(again.ContainsKey("cleared"));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void CareerFile_StaysUntouched_WhenDraftAndSettingsSave()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-career-bytes-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new NativeStore(dir, () => 80, () => 0.3);
                var career = CareerStore.Load(store, Campaign.Load(Golden.Root), 100000);
                career.Save();
                string careerBytes = File.ReadAllText(Path.Combine(dir, NativeStore.CareerKey + ".json"));
                string missionBytes = File.ReadAllText(Path.Combine(dir, NativeStore.MissionsKey + ".json"));
                var design = new JsonObject();
                design.Add("name", "Hopper");
                new DesignLibrary(store).SaveDraft(design, "Hopper", null);
                SettingsMirror.Save(store, key => key == "kh.audio.muted", key => 1);
                Assert.AreEqual(careerBytes, File.ReadAllText(Path.Combine(dir, NativeStore.CareerKey + ".json")));
                Assert.AreEqual(missionBytes, File.ReadAllText(Path.Combine(dir, NativeStore.MissionsKey + ".json")));
                var progress = store.GetProgress();
                Assert.IsTrue(progress.Bool("hasDraft"));
                Assert.IsFalse(progress.ContainsKey("lastMissionId"));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void SettingsRestore_FillsOnlyMissingKeys()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-settings-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new NativeStore(dir, () => 90, () => 0.4);
                var captured = new Dictionary<string, int> { { "kh.audio.muted", 0 }, { "kh.settings.sandbox", 1 } };
                SettingsMirror.Save(store, captured.ContainsKey, key => captured[key]);
                var live = new Dictionary<string, int> { { "kh.audio.muted", 1 } };
                int n = SettingsMirror.Restore(store, live.ContainsKey, (key, value) => live[key] = value);
                Assert.AreEqual(1, n);
                Assert.AreEqual(1, live["kh.audio.muted"], "a key the player already has is left alone");
                Assert.AreEqual(1, live["kh.settings.sandbox"]);
                Assert.IsFalse(live.ContainsKey("kh.quality.chosen"));

                store.PutRecord(FlightView.RecordKey, "settings", FlightView.Data(FlightViewMode.Pad));
                SettingsMirror.Save(store, captured.ContainsKey, key => captured[key]);
                Assert.AreEqual(FlightViewMode.Pad, FlightView.FromData(store.GetRecord(FlightView.RecordKey)));
                var kept = FlightView.Merge(store.GetRecord(FlightView.RecordKey), FlightViewMode.Chase);
                store.PutRecord(FlightView.RecordKey, "settings", kept);
                Assert.AreEqual(FlightViewMode.Chase, FlightView.FromData(store.GetRecord(FlightView.RecordKey)));
                Assert.AreEqual(1d, store.GetRecord(FlightView.RecordKey).Num("kh.settings.sandbox"));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void ResumeOffer_HidesOnAFreshCareer()
        {
            Assert.IsFalse(ResumeOffer.Show(false, 0, 0));
            Assert.IsTrue(ResumeOffer.Show(true, 0, 0));
            Assert.IsTrue(ResumeOffer.Show(false, 1, 0));
            Assert.IsTrue(ResumeOffer.Show(false, 0, 1));
            Assert.AreEqual("CONTINUE", ResumeOffer.ContinueLabel);
        }

        [Test]
        public void UnaffordableEdit_DoesNotMoveHistory()
        {
            var session = new HangarSession(Golden.Catalog, HangarFixtures.Data, HangarFixtures.Template("hopper"));
            string before = HangarSession.Snapshot(session.Design);
            session.Afford = (_, __) => false;
            var edit = session.Apply(d =>
            {
                JsonMut.Set(d.Obj("landing"), "legs", true);
                return new Edit("Legs");
            });
            Assert.IsFalse(edit.changed);
            Assert.AreEqual(0, session.history.UndoCount);
            Assert.AreEqual(before, HangarSession.Snapshot(session.Design));

            session.Afford = (_, __) => true;
            Assert.IsTrue(session.Apply(d =>
            {
                JsonMut.Set(d.Obj("landing"), "legs", true);
                return new Edit("Legs");
            }).changed);
            Assert.AreEqual(1, session.history.UndoCount);
            session.Afford = (_, __) => false;
            Assert.IsNull(session.Undo());
            Assert.AreEqual(1, session.history.UndoCount);
            Assert.IsTrue(session.Design.Obj("landing").Bool("legs"));
        }
    }
}
