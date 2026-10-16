// One Hangar editing session: the design document, undo/redo, the selected part, and the derived
// report, data sheet and advisories (recomputed lazily, once per change). The UI calls Apply with a
// HangarEditor op; everything else (labels, history, Launch gate) follows from here, so the screen code
// stays a thin view and EditMode tests cover the behaviour.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Save;

namespace Kestrel.Core.Hangar
{
    public sealed class HangarSession
    {
        public readonly Catalog catalog;
        public readonly HangarData data;
        public readonly EditHistory history;
        public HangarProgress progress = HangarProgress.None;

        public JsonObject Design { get; private set; }
        public PartRef Selected { get; private set; } = PartRef.Stage(0);
        /// <summary>Save id this design was loaded from or last saved to. Null for a fresh fork.</summary>
        public string SavedId { get; private set; }
        public string SavedName { get; private set; }
        /// <summary>True while <see cref="Load"/> replaces the document, including during the events it raises.</summary>
        public bool Loading { get; private set; }

        /// <summary>Raised after every document change: edit, undo, redo, load.</summary>
        public event Action Changed;
        /// <summary>Raised when the selected part changes.</summary>
        public event Action<PartRef> SelectionChanged;
        /// <summary>Raised when the save slot, its name or the saved state changes without a document edit (save, rename).</summary>
        public event Action SaveStateChanged;

        string cleanSnapshot;
        EngineeringReport report;
        DataSheet sheet;
        List<Advisory> advisories;

        public HangarSession(Catalog c, HangarData h, JsonObject design, int historyCapacity = EditHistory.DefaultCapacity)
        {
            catalog = c ?? throw new ArgumentNullException(nameof(c));
            data = h;
            history = new EditHistory(historyCapacity);
            Load(design ?? DesignMigration.DefaultDesign(), null, null);
        }

        public static string Snapshot(JsonObject d) => JsonWriter.Stringify(d);

        /// <summary>Replace the document (open a save, fork a template). Clears undo; the loaded state counts as saved.</summary>
        public void Load(JsonObject design, string savedId, string savedName)
        {
            var d = DesignMigration.Migrate(design);
            HangarEditor.Normalize(d);
            Loading = true;
            try
            {
                Design = d;
                SavedId = savedId;
                SavedName = savedName;
                history.Clear();
                cleanSnapshot = savedId != null ? Snapshot(d) : null;
                Invalidate();
                KeepSelection();
                Changed?.Invoke();
            }
            finally { Loading = false; }
        }

        /// <summary>
        /// Run one edit. Records an undo step when the design changed. A coalesce key folds repeated ticks
        /// of one gesture (slider drag, long-press repeat) into a single step until <see cref="EndGesture"/>.
        /// An op that throws leaves the design exactly as it was.
        /// </summary>
        public Edit Apply(Func<JsonObject, Edit> op, string coalesceKey = null)
        {
            string before = Snapshot(Design);
            Edit e;
            try { e = op(Design); }
            catch { Design = MiniJson.ParseObject(before); Invalidate(); throw; }
            if (!e.changed) return e;
            string after;
            try { after = Snapshot(Design); }
            catch { Design = MiniJson.ParseObject(before); Invalidate(); throw; }
            if (after == before) return Edit.None;
            history.Record(before, after, e.what, e.from, e.to, coalesceKey);
            Invalidate();
            KeepSelection();
            Changed?.Invoke();
            return e;
        }

        public void EndGesture() => history.Seal();

        public EditStep Undo()
        {
            string json = history.Undo(out var step);
            if (json == null) return null;
            Restore(json);
            return step;
        }

        public EditStep Redo()
        {
            string json = history.Redo(out var step);
            if (json == null) return null;
            Restore(json);
            return step;
        }

        void Restore(string json)
        {
            Design = MiniJson.ParseObject(json);
            Invalidate();
            KeepSelection();
            Changed?.Invoke();
        }

        public void Select(PartRef p)
        {
            if (!HangarParts.Exists(Design, p) || p == Selected) return;
            Selected = p;
            SelectionChanged?.Invoke(p);
        }

        void KeepSelection()
        {
            if (HangarParts.Exists(Design, Selected)) return;
            int n = Design.Arr("stages")?.Count ?? 1;
            var next = Selected.kind == PartKind.Stage || Selected.kind == PartKind.Interstage
                ? PartRef.Stage(Math.Max(0, Math.Min(n - 1, Selected.index)))
                : PartRef.Stage(0);
            Selected = next;
            SelectionChanged?.Invoke(next);
        }

        void Invalidate() { report = null; sheet = null; advisories = null; }

        public EngineeringReport Report => report ??= Validate.Engineering(catalog, Design);

        /// <summary>Null when the design cannot be computed (a schema block); the readout strip then shows dashes.</summary>
        public DataSheet Sheet
        {
            get
            {
                if (sheet != null) return sheet;
                var r = Report.readout;
                if (r == null)
                {
                    try { r = Readout.Quick(catalog, Design); }
                    catch (Exception) { return null; }
                }
                try { sheet = DataSheet.Build(catalog, data, Design, r); }
                catch (Exception) { sheet = null; }
                return sheet;
            }
        }

        public List<Advisory> Advisories => advisories ??= Hangar.Advisories.From(Report, Sheet, Design);

        /// <summary>Launch needs no blocks. Warnings (unstable, over budget, ...) never stop a launch.</summary>
        public bool LaunchEnabled => Report.ok && Report.blocks.Count == 0;

        /// <summary>Validate.MakeItFly as one undoable step. Returns what it changed (empty when nothing).</summary>
        public List<string> MakeItFly()
        {
            List<string> changes = null;
            Apply(d =>
            {
                var (fixedDesign, list) = Validate.MakeItFly(catalog, d);
                changes = list;
                if (list.Count == 0) return Edit.None;
                d.Clear(); d.Order.Clear();
                foreach (var k in fixedDesign.Order) d.Add(k, fixedDesign[k]);
                HangarEditor.Normalize(d);
                return new Edit("Make it fly");
            });
            return changes ?? new List<string>();
        }

        /// <summary>
        /// Close a negative Δv margin in one undo step: propellant, tanks, a stage, or a higher-Isp engine.
        /// A design that still cannot reach is left unchanged; <see cref="DvFixResult.Reason"/> says why.
        /// </summary>
        public DvFixResult FixDeltaV()
        {
            DvFixResult result = null;
            Apply(d =>
            {
                result = DvMarginFix.Solve(catalog, d);
                if (result == null || !result.Solved || !result.Changed || result.Design == null) return Edit.None;
                d.Clear();
                d.Order.Clear();
                foreach (var k in result.Design.Order) d.Add(k, JsonMut.Clone(result.Design[k]));
                HangarEditor.Normalize(d);
                return new Edit("Fixed the speed shortfall");
            });
            return result ?? new DvFixResult { Solved = false, Reason = "The speed for this trip could not be worked out. Lighten the cargo, raise the budget, or pick a different mission." };
        }

        public bool Dirty => cleanSnapshot == null || Snapshot(Design) != cleanSnapshot;

        /// <summary>Save into the library: a new slot when this design has none yet, else overwrite its slot.</summary>
        public SaveOutcome SaveTo(DesignLibrary library, string name = null)
        {
            var r = library.Save(Design, name ?? SavedName, SavedId);
            if (r.ok) MarkSaved(r.id, r.name);
            return r;
        }

        /// <summary>Save as a new slot, keeping the current one untouched.</summary>
        public SaveOutcome SaveAsNew(DesignLibrary library, string name)
        {
            var r = library.Save(Design, name, null);
            if (r.ok) MarkSaved(r.id, r.name);
            return r;
        }

        void MarkSaved(string id, string name)
        {
            // The slot name lives outside the versioned document: writing it into the design here would leave every
            // undo snapshot with the old name, so undo then redo would come back "renamed" and never clean.
            SavedId = id;
            SavedName = name;
            cleanSnapshot = Snapshot(Design);
            SaveStateChanged?.Invoke();
        }

        /// <summary>The library renamed a slot. When it is the open one, the session follows, so the next Save keeps the new name.</summary>
        public void SlotRenamed(string id, string name)
        {
            if (id == null || id != SavedId || name == SavedName) return;
            SavedName = name;
            SaveStateChanged?.Invoke();
        }
    }
}
