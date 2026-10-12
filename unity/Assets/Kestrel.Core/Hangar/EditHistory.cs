// Undo/redo for the Hangar. Each step stores the whole design as canonical JSON text before and after,
// so undo restores the exact document (key order included), not a reconstruction.
// 50 steps per session (DECISIONS #63). One step per committed edit: a slider drag or a long-press
// repeat records with a coalesce key, and every tick with the same key folds into one step until
// Seal() (finger up). The label keeps the value from before the first tick and the latest value.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Hangar
{
    public sealed class EditStep
    {
        public string before, after;
        /// <summary>What changed ("Stage 1 engines"). The UI shows <see cref="Label"/>.</summary>
        public string what;
        /// <summary>Old and new value as shown, or null for structural edits (add, remove, move).</summary>
        public string from, to;
        public string coalesceKey;

        public string Label => from == null && to == null ? what : $"{what} {from} → {to}";
        /// <summary>VoiceOver text: "stage 1 engines 9 to 8".</summary>
        public string Spoken => from == null && to == null ? what : $"{what} {from} to {to}";
    }

    public sealed class EditHistory
    {
        public const int DefaultCapacity = 50;

        readonly List<EditStep> undo = new List<EditStep>();
        readonly List<EditStep> redo = new List<EditStep>();
        readonly int capacity;
        bool sealedTop = true;

        public EditHistory(int capacity = DefaultCapacity) { this.capacity = Math.Max(1, capacity); }

        public int Capacity => capacity;
        public int UndoCount => undo.Count;
        public int RedoCount => redo.Count;
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;
        public EditStep NextUndo => undo.Count > 0 ? undo[undo.Count - 1] : null;
        public EditStep NextRedo => redo.Count > 0 ? redo[redo.Count - 1] : null;

        /// <summary>
        /// Record one committed edit. No-op when nothing changed. With a coalesce key equal to the open
        /// step's key, the edit folds into that step (same "before" and "from", new "after" and "to").
        /// Any new edit clears redo.
        /// </summary>
        public void Record(string before, string after, string what, string from = null, string to = null, string coalesceKey = null)
        {
            if (before == null || after == null || before == after)
            {
                // A drag that returns to its start leaves nothing to undo.
                if (coalesceKey != null && !sealedTop && undo.Count > 0 && undo[undo.Count - 1].coalesceKey == coalesceKey && undo[undo.Count - 1].before == after)
                {
                    undo.RemoveAt(undo.Count - 1);
                    sealedTop = true;
                }
                return;
            }
            var top = undo.Count > 0 ? undo[undo.Count - 1] : null;
            if (coalesceKey != null && !sealedTop && top != null && top.coalesceKey == coalesceKey && top.after == before)
            {
                top.after = after;
                top.to = to;
                if (top.before == top.after) { undo.RemoveAt(undo.Count - 1); sealedTop = true; }
                redo.Clear();
                return;
            }
            undo.Add(new EditStep { before = before, after = after, what = what, from = from, to = to, coalesceKey = coalesceKey });
            if (undo.Count > capacity) undo.RemoveAt(0);
            sealedTop = coalesceKey == null;
            redo.Clear();
        }

        /// <summary>Close the open coalescing step (finger up, stepper released).</summary>
        public void Seal() => sealedTop = true;

        /// <summary>Step back. Returns the JSON to restore, or null when there is nothing to undo.</summary>
        public string Undo(out EditStep step)
        {
            step = null;
            if (undo.Count == 0) return null;
            step = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            redo.Add(step);
            sealedTop = true;
            return step.before;
        }

        /// <summary>Step forward. Returns the JSON to restore, or null when there is nothing to redo.</summary>
        public string Redo(out EditStep step)
        {
            step = null;
            if (redo.Count == 0) return null;
            step = redo[redo.Count - 1];
            redo.RemoveAt(redo.Count - 1);
            undo.Add(step);
            if (undo.Count > capacity) undo.RemoveAt(0);
            sealedTop = true;
            return step.after;
        }

        public void Clear() { undo.Clear(); redo.Clear(); sealedTop = true; }
    }
}
