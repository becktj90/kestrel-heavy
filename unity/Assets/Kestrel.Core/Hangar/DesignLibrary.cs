// Named save slots for Hangar designs on top of NativeStore (kh.save.v1.design.*, docs/SAVE_BRIDGE.md).
// 8 local slots (DECISIONS #64), names 1–24 characters, no cloud and no account. Loading migrates the
// stored design to the current schema and uses the slot's name, so a rename shows up on the next load.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Save;

namespace Kestrel.Core.Hangar
{
    public sealed class SavedDesignInfo
    {
        public string id, name;
        public double createdAt, updatedAt;
    }

    public struct SaveOutcome
    {
        public bool ok;
        public string id, name;
        /// <summary>"full" (8 slots used), "missing" (no such slot), or a NativeStore code ("quota", "write", "invalid-key").</summary>
        public string error;

        public static SaveOutcome Fail(string error) => new SaveOutcome { error = error };
    }

    public sealed class DesignLibrary
    {
        public const int MaxSlots = 8;
        public const int NameMax = 24;
        public const string DefaultName = "Untitled";

        readonly NativeStore store;

        public DesignLibrary(NativeStore store) { this.store = store ?? throw new ArgumentNullException(nameof(store)); }

        /// <summary>Most recently updated first.</summary>
        public List<SavedDesignInfo> List()
        {
            var o = new List<SavedDesignInfo>();
            foreach (var m in store.ListDesigns())
                o.Add(new SavedDesignInfo { id = m.Str("id"), name = m.Str("name"), createdAt = m.Num("createdAt"), updatedAt = m.Num("updatedAt") });
            return o;
        }

        public int Count => store.ListDesigns().Count;
        public bool IsFull => Count >= MaxSlots;

        public static string CleanName(string name)
        {
            var n = HangarEditor.CleanText(name, NameMax);
            return n.Length == 0 ? DefaultName : n;
        }

        /// <summary>Save. With an id that exists, overwrite it; otherwise take a new slot (refused when all 8 are used).</summary>
        public SaveOutcome Save(JsonObject design, string name, string id = null)
        {
            if (design == null) return SaveOutcome.Fail("missing");
            bool exists = id != null && store.GetMeta(id) != null;
            if (!exists && IsFull) return SaveOutcome.Fail("full");
            string nm = CleanName(name ?? design.Str("name"));
            var copy = JsonMut.CloneObject(design);
            JsonMut.Set(copy, "name", nm);
            if (copy.ContainsKey("id")) { copy.Remove("id"); copy.Order.Remove("id"); }
            try
            {
                var (sid, _) = store.PutDesign(copy, exists ? id : null, nm);
                return new SaveOutcome { ok = true, id = sid, name = nm };
            }
            catch (SaveException e) { return SaveOutcome.Fail(e.Code); }
        }

        /// <summary>The design, migrated, with the slot's name. Null when the slot is missing or unreadable.</summary>
        public JsonObject Load(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var meta = store.GetMeta(id);
            var d = store.GetDesign(id);
            if (meta == null || d == null) return null;
            var copy = JsonMut.CloneObject(d);
            JsonMut.Set(copy, "name", meta.Str("name") ?? copy.Str("name") ?? DefaultName);
            return copy;
        }

        public string NameOf(string id) => store.GetMeta(id)?.Str("name");

        public SaveOutcome Rename(string id, string name)
        {
            if (store.GetMeta(id) == null) return SaveOutcome.Fail("missing");
            string nm = CleanName(name);
            try { store.RenameDesign(id, nm); }
            catch (SaveException e) { return SaveOutcome.Fail(e.Code); }
            return new SaveOutcome { ok = true, id = id, name = nm };
        }

        public SaveOutcome Duplicate(string id, string name = null)
        {
            var d = Load(id);
            if (d == null) return SaveOutcome.Fail("missing");
            if (IsFull) return SaveOutcome.Fail("full");
            return Save(d, name ?? UniqueName(d.Str("name") + " copy"));
        }

        public bool Delete(string id) => !string.IsNullOrEmpty(id) && store.DeleteDesign(id);

        /// <summary>True when a mid-design autosave exists. A draft is not one of the 8 slots.</summary>
        public bool HasDraft()
        {
            var data = DraftData();
            return data != null && data.Obj("design") != null;
        }

        /// <summary>The autosaved rocket, migrated, or null. <paramref name="missionId"/> is the mission that was open, if any.</summary>
        public JsonObject LoadDraft(out string missionId, out string name)
        {
            missionId = null;
            name = null;
            var data = DraftData();
            var design = data != null ? data.Obj("design") : null;
            if (design == null) return null;
            var copy = JsonMut.CloneObject(design);
            name = data.Str("name");
            if (string.IsNullOrEmpty(name)) name = copy.Str("name") ?? DefaultName;
            JsonMut.Set(copy, "name", name);
            missionId = data.Str("missionId");
            if (string.IsNullOrEmpty(missionId)) missionId = null;
            return copy;
        }

        /// <summary>Write the open rocket outside the 8 slots and remember it on the progress record.</summary>
        public void SaveDraft(JsonObject design, string name, string missionId)
        {
            if (design == null) return;
            string nm = CleanName(name ?? design.Str("name"));
            var copy = JsonMut.CloneObject(design);
            JsonMut.Set(copy, "name", nm);
            var data = new JsonObject();
            data.Add("design", copy);
            data.Add("name", nm);
            data.Add("missionId", missionId ?? "");
            store.PutRecord(NativeStore.DraftKey, "draft", data);
            NoteDraft(nm, missionId);
        }

        /// <summary>Autosave whenever the session changes, including the rocket that is already open.</summary>
        public void WatchDraft(HangarSession session, Func<string> missionId)
        {
            if (session == null) return;
            void Keep()
            {
                if (session.Loading || session.Design == null) return;
                string mid = null;
                try { if (missionId != null) mid = missionId(); }
                catch (Exception) { mid = null; }
                try { SaveDraft(session.Design, session.SavedName ?? session.Design.Str("name"), mid); }
                catch (SaveException) { }
            }
            session.Changed += Keep;
            Keep();
        }

        JsonObject DraftData()
        {
            var env = store.Read(NativeStore.DraftKey);
            if (env == null || env.Str("kind") != "draft") return null;
            return env.Obj("data");
        }

        void NoteDraft(string name, string missionId)
        {
            var prev = store.GetProgress();
            var next = prev != null ? JsonMut.CloneObject(prev) : new JsonObject();
            JsonMut.Set(next, "hasDraft", true);
            JsonMut.Set(next, "draftName", name ?? "");
            if (!string.IsNullOrEmpty(missionId)) JsonMut.Set(next, "lastMissionId", missionId);
            store.PutProgress(next);
        }

        /// <summary>"Hopper", then "Hopper 2", "Hopper 3" … so new slots never share a name.</summary>
        public string UniqueName(string baseName)
        {
            string b = CleanName(baseName);
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in List()) if (m.name != null) taken.Add(m.name);
            if (!taken.Contains(b)) return b;
            for (int i = 2; i < 1000; i++)
            {
                string suffix = " " + i;
                string head = b.Length + suffix.Length > NameMax ? b.Substring(0, NameMax - suffix.Length).TrimEnd() : b;
                if (!taken.Contains(head + suffix)) return head + suffix;
            }
            return b;
        }
    }
}
