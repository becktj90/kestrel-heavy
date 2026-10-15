// What the box is showing. Ascent and the designer stack at most two speakers. Menu screens
// show one box and leave the rest queued for a tap. A decision or a safety line drops idle chatter.
// Designer and menu lines wait for a tap. Ascent lines advance after the reading time, except critical ones.
using System;
using System.Collections.Generic;

namespace Kestrel.Game.Guide
{
    public sealed class GuideBoxModel
    {
        public string Id = "";
        public string Name = "";
        public string Role = "";
        public string Shown = "";
        public string Full = "";
        public string Hint = "";
        public string Spoken = "";
        public string Index = "";
        public bool Portrait;
        public bool Caret;
        public bool Announce;
        public bool Fallback;
        public int Page = 1;
        public int Pages = 1;
        public GuidePriority Priority;
        public GuideSpeaker Speaker;
    }

    public sealed class GuideStage
    {
        sealed class Slot
        {
            public GuideBeat Beat;
            public string[] Pages = Array.Empty<string>();
            public int Page;
            public readonly Typewriter Typer = new Typewriter();
            public float OnScreen;
            public bool Announced;
            public bool AnnounceNow;
            public bool Engineer;
            public string Spoken = "";
        }

        readonly List<Slot> active = new List<Slot>();
        readonly List<GuideBeat> incoming = new List<GuideBeat>();
        readonly List<GuideBeat> parked = new List<GuideBeat>();
        readonly HashSet<string> dropped = new HashSet<string>();

        public IGuideEngine Engine { get; private set; }
        public NarrationSubtitles Clips { get; private set; }
        public GuidePlace Place = GuidePlace.Designer;
        public bool ReduceMotionOverride;
        public GlyphTable Glyphs;
        public int Blips { get; private set; }

        public event Action Blip;

        public GuideStage(IGuideEngine engine, NarrationSubtitles clips = null)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Clips = clips;
        }

        public void Use(IGuideEngine engine, NarrationSubtitles clips)
        {
            if (engine != null) Engine = engine;
            if (clips != null) Clips = clips;
        }

        public IGuideVoice Voice { get; set; }

        GuideSettings Settings => Engine.Settings ?? new GuideSettings();
        bool Reduce => Settings.ReduceMotion || ReduceMotionOverride;

        public int Count => active.Count;

        int BoxCap => Place == GuidePlace.Menu ? 1 : GuidePacing.MaxBoxes;

        public void BeginFlight()
        {
            active.Clear();
            incoming.Clear();
            parked.Clear();
            dropped.Clear();
            Clips?.Reset();
        }

        public void OfferNarration(string eventId)
        {
            if (!Settings.SubtitlesOn || Clips == null) return;
            if (!Clips.TryNext(eventId, out var beat) || beat == null) return;
            incoming.Add(beat);
        }

        public void Tick(float dt)
        {
            if (dt < 0f) dt = 0f;
            if (Reduce)
            {
                for (int i = 0; i < active.Count; i++)
                    if (!active[i].Typer.Done) active[i].Typer.Finish();
            }
            Pull();
            for (int i = 0; i < active.Count; i++) TickSlot(active[i], dt);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var slot = active[i];
                if (!ReadyToAdvance(slot)) continue;
                if (slot.Page + 1 < slot.Pages.Length) StartPage(slot, slot.Page + 1);
                else Dismiss(slot, false);
            }
        }

        public void Tap(int index)
        {
            SortActive();
            if (index < 0 || index >= active.Count) return;
            var slot = active[index];
            if (!slot.Typer.Done) { slot.Typer.Finish(); return; }
            if (slot.Page + 1 < slot.Pages.Length) StartPage(slot, slot.Page + 1);
            else Dismiss(slot, false);
        }

        public void Skip(int index)
        {
            SortActive();
            if (index < 0 || index >= active.Count) return;
            Dismiss(active[index], true);
        }

        public void SkipStepTips(int index)
        {
            SortActive();
            string step = Settings.StepId ?? "";
            if (index >= 0 && index < active.Count && !string.IsNullOrEmpty(active[index].Beat.StepId))
                step = active[index].Beat.StepId;
            Engine.SkipTipsForStep(step);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var beat = active[i].Beat;
                if (beat.FromEngine && beat.Priority == GuidePriority.Chatter && beat.StepId == step)
                    Dismiss(active[i], true);
            }
        }

        public GuideBoxModel[] Models()
        {
            SortActive();
            var list = new GuideBoxModel[active.Count];
            for (int i = 0; i < active.Count; i++) list[i] = Model(active[i]);
            return list;
        }

        void SortActive() => active.Sort(CompareSlots);

        static int CompareSlots(Slot a, Slot b)
        {
            int pa = GuideSpeakers.HasPortrait(a.Beat.Speaker) ? 1 : 0;
            int pb = GuideSpeakers.HasPortrait(b.Beat.Speaker) ? 1 : 0;
            if (pa != pb) return pa - pb;
            return Rank(b.Beat) - Rank(a.Beat);
        }

        void Pull()
        {
            var pending = Engine.Pending;
            if (pending != null)
            {
                for (int i = 0; i < pending.Count; i++) TryAdmit(pending[i]);
            }
            for (int i = 0; i < incoming.Count; i++) TryAdmit(incoming[i]);
            incoming.Clear();
            FillParked();
        }

        void TryAdmit(GuideBeat beat)
        {
            if (beat == null || string.IsNullOrEmpty(beat.Id)) return;
            if (!ShouldShow(beat) || dropped.Contains(beat.Id) || Showing(beat.Id) || Parked(beat.Id)) return;
            if (ChatterBlocked(beat))
            {
                if (!beat.FromEngine) dropped.Add(beat.Id);
                return;
            }
            if (Rank(beat) >= (int)GuidePriority.Decision) DropChatter();
            if (active.Count < BoxCap) { Start(beat); return; }
            int low = Lowest();
            if (Rank(beat) > Rank(active[low].Beat))
            {
                var displaced = active[low];
                active.RemoveAt(low);
                if (!displaced.Beat.FromEngine) dropped.Add(displaced.Beat.Id);
                Start(beat);
            }
            else if (!beat.FromEngine && beat.Priority > GuidePriority.Chatter) Park(beat);
            else if (!beat.FromEngine) dropped.Add(beat.Id);
        }

        void Park(GuideBeat beat)
        {
            if (!Parked(beat.Id)) parked.Add(beat);
        }

        bool Parked(string id)
        {
            for (int i = 0; i < parked.Count; i++)
                if (parked[i].Id == id) return true;
            return false;
        }

        bool ChatterBlocked(GuideBeat beat)
        {
            if (beat.Priority != GuidePriority.Chatter || beat.Decision) return false;
            for (int i = 0; i < active.Count; i++)
                if (Rank(active[i].Beat) >= (int)GuidePriority.Decision) return true;
            return false;
        }

        void Start(GuideBeat beat)
        {
            var slot = new Slot { Beat = beat, Engineer = Settings.Engineer };
            slot.Pages = GuidePages.Of(beat, Settings.Engineer);
            if (slot.Pages.Length == 0) return;
            active.Add(slot);
            StartPage(slot, 0);
            FillParked();
        }

        void FillParked()
        {
            for (int i = 0; i < parked.Count && active.Count < BoxCap; i++)
            {
                var beat = parked[i];
                if (Showing(beat.Id) || !ShouldShow(beat) || ChatterBlocked(beat))
                {
                    if (!ShouldShow(beat) || ChatterBlocked(beat))
                    {
                        parked.RemoveAt(i);
                        i--;
                    }
                    continue;
                }
                parked.RemoveAt(i);
                i--;
                Start(beat);
            }
        }

        void StartPage(Slot slot, int page)
        {
            slot.Page = page;
            slot.OnScreen = 0f;
            slot.Announced = false;
            slot.AnnounceNow = false;
            slot.Spoken = "";
            string text = page >= 0 && page < slot.Pages.Length ? slot.Pages[page] : "";
            slot.Typer.Start(text, Settings.Speed, Reduce, Settings.Muted);
        }

        void TickSlot(Slot slot, float dt)
        {
            if (slot.Engineer != Settings.Engineer)
            {
                int page = slot.Page;
                slot.Engineer = Settings.Engineer;
                slot.Pages = GuidePages.Of(slot.Beat, Settings.Engineer);
                if (slot.Pages.Length == 0) slot.Pages = new[] { "" };
                if (page >= slot.Pages.Length) page = slot.Pages.Length - 1;
                StartPage(slot, page);
            }
            slot.AnnounceNow = false;
            slot.OnScreen += dt;
            slot.Typer.Tick(dt);
            if (slot.Typer.BlipPending)
            {
                Blips++;
                Blip?.Invoke();
            }
            if (slot.Typer.Done && !slot.Announced) Announce(slot);
        }

        void Announce(Slot slot)
        {
            if (slot.Announced) return;
            slot.Announced = true;
            slot.AnnounceNow = true;
            slot.Spoken = GuideSpeech.Line(GuideSpeakers.Spoken(slot.Beat.Speaker), slot.Typer.Full, slot.Page + 1, slot.Pages.Length);
            Voice?.Announce(slot.Spoken);
            if (Settings.ReadAloud) Voice?.Speak(slot.Spoken);
        }

        bool ReadyToAdvance(Slot slot)
        {
            if (Place != GuidePlace.Ascent || slot.Beat.Critical || !slot.Typer.Done) return false;
            float hold = GuidePacing.HoldSeconds(slot.Typer.Full.Length, slot.Beat.MinHold);
            return slot.OnScreen + 0.0001f >= hold;
        }

        void Dismiss(Slot slot, bool skipped)
        {
            active.Remove(slot);
            if (slot.Beat.FromEngine)
            {
                if (skipped) Engine.Skip(slot.Beat.Id);
                else Engine.Complete(slot.Beat.Id);
            }
            FillParked();
            Pull();
        }

        void DropChatter()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Beat.Priority != GuidePriority.Chatter) continue;
                var beat = active[i].Beat;
                active.RemoveAt(i);
                if (!beat.FromEngine) dropped.Add(beat.Id);
            }
        }

        bool Showing(string id)
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i].Beat.Id == id) return true;
            return false;
        }

        int Lowest()
        {
            int at = 0;
            for (int i = 1; i < active.Count; i++)
                if (Rank(active[i].Beat) < Rank(active[at].Beat)) at = i;
            return at;
        }

        bool ShouldShow(GuideBeat beat)
        {
            if (beat.Speaker != GuideSpeaker.Thrum) return Settings.SubtitlesOn;
            if (beat.Decision || beat.Priority >= GuidePriority.Decision) return true;
            if (Settings.Tips == TipsMode.Off) return false;
            if (Settings.Tips == TipsMode.WarningsOnly) return beat.Priority >= GuidePriority.Failure;
            return true;
        }

        static int Rank(GuideBeat beat)
        {
            int rank = (int)beat.Priority;
            if (beat.Decision && rank < (int)GuidePriority.Decision) rank = (int)GuidePriority.Decision;
            if (beat.Critical && rank < (int)GuidePriority.Safety) rank = (int)GuidePriority.Safety;
            return rank;
        }

        GuideBoxModel Model(Slot slot)
        {
            var beat = slot.Beat;
            bool done = slot.Typer.Done;
            string hint = !done ? "TAP: finish" : (slot.Page + 1 < slot.Pages.Length ? "TAP: next" : "TAP: close");
            return new GuideBoxModel
            {
                Id = beat.Id,
                Name = GuideSpeakers.Tag(beat.Speaker),
                Role = GuideSpeakers.Role(beat.Speaker),
                Shown = slot.Typer.Visible,
                Full = slot.Typer.Full,
                Hint = hint,
                Spoken = slot.Spoken,
                Index = (slot.Page + 1) + "/" + slot.Pages.Length,
                Portrait = GuideSpeakers.HasPortrait(beat.Speaker),
                Caret = !done && (Reduce || ((int)(slot.OnScreen * 2f) % 2) == 0),
                Announce = slot.AnnounceNow,
                Fallback = Glyphs != null && !Glyphs.Covers(slot.Typer.Full),
                Page = slot.Page + 1,
                Pages = slot.Pages.Length,
                Priority = beat.Priority,
                Speaker = beat.Speaker,
            };
        }
    }
}
