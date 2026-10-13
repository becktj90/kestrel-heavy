using System;
using System.Collections.Generic;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// One-shot voice cap. When full, the lowest-priority (then oldest) voice is stolen.
    /// Same rules as web/js/audio/voices.js. Each slot keeps a stable index into the AudioSource pool.
    /// </summary>
    public sealed class VoicePool
    {
        readonly List<VoiceSlot> active = new List<VoiceSlot>();
        readonly Stack<VoiceSlot> free = new Stack<VoiceSlot>();
        int seq;

        public int Cap { get; }
        public int Size => active.Count;
        public int Started { get; private set; }
        public int Stolen { get; private set; }
        public int Rejected { get; private set; }

        public VoicePool(int cap)
        {
            if (cap < 1) throw new ArgumentOutOfRangeException(nameof(cap));
            Cap = cap;
            for (int i = cap - 1; i >= 0; i--) free.Push(new VoiceSlot(i));
        }

        public VoiceSlot Acquire(string name, int priority = 0)
        {
            if (active.Count >= Cap)
            {
                VoiceSlot victim = null;
                for (int i = 0; i < active.Count; i++)
                {
                    var v = active[i];
                    if (v.Priority > priority) continue;
                    if (victim == null || v.Priority < victim.Priority || (v.Priority == victim.Priority && v.Seq < victim.Seq))
                        victim = v;
                }
                if (victim == null) { Rejected++; return null; }
                var stop = victim.Stop;
                Release(victim);
                Stolen++;
                if (stop != null) stop();
            }
            var slot = free.Pop();
            slot.Name = name;
            slot.Priority = priority;
            slot.Seq = ++seq;
            slot.Stop = null;
            slot.Active = true;
            active.Add(slot);
            Started++;
            return slot;
        }

        public void Release(VoiceSlot slot)
        {
            if (slot == null || !slot.Active) return;
            active.Remove(slot);
            slot.Stop = null;
            slot.Name = "";
            slot.Active = false;
            free.Push(slot);
        }

        public void StopAll()
        {
            var copy = new List<VoiceSlot>(active);
            for (int i = 0; i < copy.Count; i++)
            {
                var stop = copy[i].Stop;
                Release(copy[i]);
                if (stop != null) stop();
            }
        }
    }

    public sealed class VoiceSlot
    {
        public int Index { get; }
        public string Name;
        public int Priority;
        public int Seq;
        public Action Stop;
        public bool Active;

        internal VoiceSlot(int index) { Index = index; }
    }
}
