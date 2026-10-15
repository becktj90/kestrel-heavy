// One Thrum box at a time. Safety outranks a decision, a decision outranks a failure,
// and a failure outranks chatter. A decision drops queued chatter. Critical lines
// wait for a tap. Ascent chatter advances itself after the reading time.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Guide
{
    public sealed class GuideFace
    {
        public string LineId, CardId, Trigger, When, Priority;
        public string Kid, Eng, Full, Visible;
        public int CardIndex, CardCount;
        public bool Complete, Critical, TextShown, VoiceOn;
        public string VoiceClip;
        public IReadOnlyList<string> Actions;
    }

    public sealed class GuideQueue
    {
        public const double AscentMinSeconds = 2.5;
        public const double ReadingCharsPerSecond = 18;
        public const double ReadingPadSeconds = 1.2;

        readonly List<Slot> pending = new List<Slot>();
        Slot current;
        int seq;

        public bool Ascent;
        public bool Engineer;
        public GuideSettings Settings = new GuideSettings();

        public int Pending => pending.Count;
        public bool Busy => current != null;
        public string CurrentId => current == null ? null : current.Line.Id;

        public static double ReadingSeconds(string text)
        {
            int n = string.IsNullOrEmpty(text) ? 0 : text.Length;
            double t = n / ReadingCharsPerSecond + ReadingPadSeconds;
            return t < AscentMinSeconds ? AscentMinSeconds : t;
        }

        public GuideFace Push(GuideLine line, double t, IReadOnlyDictionary<string, string> tokens, string clip)
        {
            if (line == null || line.Cards.Count == 0) return Face(t);
            int rank = GuidePriority.Rank(line.Priority);
            if (current != null && rank == GuidePriority.Rank(GuidePriority.Chatter) && GuidePriority.Rank(current.Line.Priority) >= GuidePriority.Rank(GuidePriority.Decision))
                return Face(t);
            if (rank >= GuidePriority.Rank(GuidePriority.Decision))
            {
                for (int i = pending.Count - 1; i >= 0; i--)
                    if (GuidePriority.Rank(pending[i].Line.Priority) == GuidePriority.Rank(GuidePriority.Chatter))
                        pending.RemoveAt(i);
                if (current != null && GuidePriority.Rank(current.Line.Priority) == GuidePriority.Rank(GuidePriority.Chatter))
                    current = null;
            }
            var slot = new Slot
            {
                Line = line,
                Tokens = Copy(tokens),
                Clip = clip,
                Seq = ++seq,
                StartT = t,
            };
            pending.Add(slot);
            pending.Sort(Order);
            if (current == null)
                StartHead(t);
            else if (rank > GuidePriority.Rank(current.Line.Priority))
            {
                pending.Add(current);
                current = null;
                pending.Sort(Order);
                StartHead(t);
            }
            return Face(t);
        }

        public GuideFace Tick(double t)
        {
            if (current == null) return null;
            if (!Ascent || Critical(current)) return Face(t);
            if (t + 1e-9 < current.StartT + ReadingSeconds(Shown(current))) return Face(t);
            Advance(t);
            return Face(t);
        }

        /// <summary>First tap finishes the typewriter. The next tap moves on. Ascent holds the card for 2.5 s.</summary>
        public GuideFace Tap(double t)
        {
            if (current == null) return null;
            var face = Face(t);
            if (!face.Complete)
            {
                current.Revealed = true;
                return Face(t);
            }
            if (Ascent && t + 1e-9 < current.StartT + AscentMinSeconds) return face;
            Advance(t);
            return Face(t);
        }

        /// <summary>SKIP drops the rest of this beat and keeps anything still queued.</summary>
        public GuideFace Skip(double t)
        {
            if (current == null) return null;
            current = null;
            StartHead(t);
            return Face(t);
        }

        public void DropTrigger(string trigger, double t)
        {
            if (string.IsNullOrEmpty(trigger)) return;
            bool dropped = current != null && current.Line.Trigger == trigger;
            if (dropped) current = null;
            for (int i = pending.Count - 1; i >= 0; i--)
                if (pending[i].Line.Trigger == trigger) pending.RemoveAt(i);
            if (dropped) StartHead(t);
        }

        public bool Contains(string lineId)
        {
            if (string.IsNullOrEmpty(lineId)) return false;
            if (current != null && current.Line.Id == lineId) return true;
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Line.Id == lineId) return true;
            return false;
        }

        public void Clear()
        {
            pending.Clear();
            current = null;
        }

        public GuideFace Face(double t)
        {
            if (current == null) return null;
            var card = current.Line.Cards[current.Card];
            string kid = GuideText.Fill(card.Kid, current.Tokens);
            string eng = GuideText.Fill(card.Eng, current.Tokens);
            string full = Engineer ? kid + "\n" + eng : kid;
            int count = RevealedCount(full, t);
            bool voice = Settings != null && Settings.VoiceOn && !string.IsNullOrEmpty(current.Clip);
            return new GuideFace
            {
                LineId = current.Line.Id,
                CardId = card.Id,
                Trigger = current.Line.Trigger,
                When = current.Line.When,
                Priority = current.Line.Priority,
                Kid = kid,
                Eng = eng,
                Full = full,
                Visible = full.Substring(0, count),
                CardIndex = current.Card,
                CardCount = current.Line.Cards.Count,
                Complete = count >= full.Length,
                Critical = Critical(current),
                TextShown = true,
                VoiceOn = voice,
                VoiceClip = voice ? current.Clip : null,
                Actions = current.Line.Actions,
            };
        }

        int RevealedCount(string full, double t)
        {
            if (current.Revealed) return full.Length;
            double cps = Settings == null ? 40 : Settings.CharsPerSecond;
            if (cps <= 0) return full.Length;
            double elapsed = t - current.StartT;
            if (elapsed <= 0) return 0;
            int n = (int)Math.Floor(elapsed * cps);
            if (n < 0) return 0;
            return n > full.Length ? full.Length : n;
        }

        void Advance(double t)
        {
            if (current == null) return;
            if (current.Card + 1 < current.Line.Cards.Count)
            {
                current.Card++;
                current.StartT = t;
                current.Revealed = false;
                return;
            }
            current = null;
            StartHead(t);
        }

        void StartHead(double t)
        {
            if (pending.Count == 0)
            {
                current = null;
                return;
            }
            current = pending[0];
            pending.RemoveAt(0);
            current.Card = 0;
            current.StartT = t;
            current.Revealed = false;
        }

        string Shown(Slot slot)
        {
            var card = slot.Line.Cards[slot.Card];
            string kid = GuideText.Fill(card.Kid, slot.Tokens);
            if (!Engineer) return kid;
            return kid + "\n" + GuideText.Fill(card.Eng, slot.Tokens);
        }

        static bool Critical(Slot slot)
            => slot.Line.Priority == GuidePriority.Safety || slot.Line.Priority == GuidePriority.Decision;

        static int Order(Slot a, Slot b)
        {
            int rank = GuidePriority.Rank(b.Line.Priority).CompareTo(GuidePriority.Rank(a.Line.Priority));
            if (rank != 0) return rank;
            return a.Seq.CompareTo(b.Seq);
        }

        static Dictionary<string, string> Copy(IReadOnlyDictionary<string, string> tokens)
        {
            var copy = new Dictionary<string, string>();
            if (tokens == null) return copy;
            foreach (var pair in tokens) copy[pair.Key] = pair.Value ?? "";
            return copy;
        }

        sealed class Slot
        {
            public GuideLine Line;
            public Dictionary<string, string> Tokens;
            public string Clip;
            public int Card, Seq;
            public double StartT;
            public bool Revealed;
        }
    }
}
