// Fires guide lines from narration bus ids and from MissionEvent.links.narration.
// Designer, briefing, and welcome ids use the same offer path. They are not a new event type.
using System.Collections.Generic;
using Kestrel.Core.Timeline;

namespace Kestrel.Core.Guide
{
    public sealed class GuideBinder
    {
        public readonly GuideScript Script;
        public readonly GuideState State;
        public readonly GuideQueue Queue;
        public readonly string RepoRoot;

        public GuideBinder(GuideScript script, GuideState state, GuideQueue queue, string repoRoot)
        {
            Script = script;
            State = state;
            Queue = queue ?? new GuideQueue();
            RepoRoot = repoRoot;
            if (state != null) Queue.Settings = state.Settings;
        }

        public static GuideBinder Load(string repoRoot, bool hasDesigns)
        {
            bool clips = GuideVoice.ClipsPresent(repoRoot);
            return new GuideBinder(GuideScript.Load(repoRoot), GuideState.Fresh(clips, hasDesigns), new GuideQueue(), repoRoot);
        }

        public GuideFace Offer(string trigger, string when, double t, IReadOnlyDictionary<string, string> tokens)
        {
            var line = Script.Match(trigger, when);
            if (!State.Allows(line)) return Queue.Face(t);
            if (Queue.Contains(line.Id)) return Queue.Face(t);
            return Push(line, t, tokens);
        }

        public GuideFace Tick(double t)
        {
            var face = Queue.Tick(t);
            Note();
            return face;
        }

        public GuideFace Tap(double t)
        {
            var face = Queue.Tap(t);
            Note();
            return face;
        }

        public GuideFace Skip(double t)
        {
            var face = Queue.Skip(t);
            Note();
            return face;
        }

        public GuideFace OnBus(string type, string when, double t, IReadOnlyDictionary<string, string> tokens)
            => Offer(type, when, t, tokens);

        public GuideFace OnFailure(string kind, double t, IReadOnlyDictionary<string, string> tokens)
            => Offer("abort", kind, t, tokens);

        public GuideFace OnMissionEvent(MissionEvent ev, string when, double t, IReadOnlyDictionary<string, string> tokens)
        {
            GuideFace face = Queue.Face(t);
            if (ev == null) return face;
            if (ev.links != null && ev.links.narration != null)
            {
                for (int i = 0; i < ev.links.narration.Count; i++)
                    face = Offer(ev.links.narration[i], when, t, tokens) ?? face;
            }
            if (!string.IsNullOrEmpty(ev.id))
                face = Offer(ev.id, when, t, tokens) ?? face;
            return face;
        }

        public GuideFace Welcome(double t)
        {
            for (int i = 0; i < Script.Lines.Count; i++)
            {
                var line = Script.Lines[i];
                if (line.Trigger != "guide.first" || !State.Allows(line)) continue;
                Push(line, t, null);
            }
            return Queue.Face(t);
        }

        /// <summary>Skip asks nothing. The welcome does not return.</summary>
        public void SkipWelcome()
        {
            for (int i = 0; i < Script.Lines.Count; i++)
            {
                var line = Script.Lines[i];
                if (line.Trigger == "guide.first") State.MarkSeen(line.Id);
            }
            State.FirstRunDone = true;
            Queue.DropTrigger("guide.first", 0);
        }

        GuideFace Push(GuideLine line, double t, IReadOnlyDictionary<string, string> tokens)
        {
            string clip = null;
            if (State.Settings.VoiceOn && State.Settings.VoiceAvailable)
                clip = GuideVoice.ClipFor(RepoRoot, line.Id);
            var face = Queue.Push(line, t, tokens, clip);
            Note();
            return face;
        }

        void Note()
        {
            if (Queue.CurrentId != null) State.MarkSeen(Queue.CurrentId);
        }
    }
}
