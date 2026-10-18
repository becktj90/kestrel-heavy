// Adapts the G7 guide engine (data/guide.json, seen-state, voice clips) to the subtitle box.
// The box types the cards and fills {tokens}. This type only decides which beat is allowed.
using System;
using System.Collections.Generic;
using CoreLine = Kestrel.Core.Guide.GuideLine;
using CoreScript = Kestrel.Core.Guide.GuideScript;
using CoreState = Kestrel.Core.Guide.GuideState;
using CoreTips = Kestrel.Core.Guide.GuideTips;
using CoreSpeed = Kestrel.Core.Guide.GuideSpeed;
using CoreSize = Kestrel.Core.Guide.GuideSize;
using CorePri = Kestrel.Core.Guide.GuidePriority;
using CoreVoice = Kestrel.Core.Guide.GuideVoice;

namespace Kestrel.Game.Guide
{
    /// <summary>The real guide behind <see cref="IGuideEngine"/>. Text is always queued.
    /// A voice path is attached only when a clip file exists under assets/narration/guide.</summary>
    public sealed class LiveGuideEngine : IGuideEngine, IGuideReport
    {
        readonly CoreScript script;
        readonly CoreState state;
        readonly string repoRoot;
        readonly bool clipsKnown;
        readonly List<GuideBeat> queued = new List<GuideBeat>();
        readonly HashSet<string> skippedSteps = new HashSet<string>();
        readonly HashSet<string> thisFlight = new HashSet<string>();
        readonly GuideSettings settings = new GuideSettings();
        string stepSig = "";
        string streakMission = "";
        int streak;
        int reportStamp;

        public LiveGuideEngine(string guideJson, CoreState state, string repoRoot)
            : this(guideJson, state, repoRoot, null)
        {
        }

        /// <param name="clipsOverride">Player layout (StreamingAssets or Resources). Null asks the repo folder.</param>
        public LiveGuideEngine(string guideJson, CoreState state, string repoRoot, bool? clipsOverride)
        {
            if (string.IsNullOrEmpty(guideJson)) throw new ArgumentException("guide json missing", nameof(guideJson));
            script = CoreScript.Parse(guideJson);
            this.state = state ?? CoreState.Fresh(false, false);
            this.repoRoot = repoRoot ?? "";
            bool clips = clipsOverride ?? CoreVoice.ClipsPresent(this.repoRoot);
            clipsKnown = clips;
            this.state.Settings.VoiceAvailable = clips;
            if (!clips) this.state.Settings.VoiceOn = false;
            PullSettings();
        }

        public CoreScript Script => script;
        public CoreState State => state;
        public GuideSettings Settings => settings;
        public bool ClipVoice => state.Settings.VoiceAvailable && state.Settings.VoiceOn;

        public IReadOnlyList<GuideBeat> Pending
        {
            get
            {
                var list = new List<GuideBeat>();
                for (int i = 0; i < queued.Count; i++)
                {
                    var beat = queued[i];
                    if (beat == null) continue;
                    if (beat.Priority == GuidePriority.Chatter && !string.IsNullOrEmpty(beat.StepId) && skippedSteps.Contains(beat.StepId))
                        continue;
                    list.Add(beat);
                }
                return list;
            }
        }

        public void SetEngineer(bool on) => settings.Engineer = on;

        public void PullSettings()
        {
            settings.Speed = MapSpeed(state.Settings.Speed);
            settings.Size = MapSize(state.Settings.Size);
            settings.Tips = MapTips(state.Settings.Tips);
            settings.ReduceMotion = state.Settings.ReduceMotion;
            settings.SubtitlesOn = Kestrel.Core.Flight.MenuPrefs.SubtitlesOn;
            settings.ReadAloud = false;
            bool clips = clipsKnown || CoreVoice.ClipsPresent(repoRoot);
            state.Settings.VoiceAvailable = clips;
            if (!clips) state.Settings.VoiceOn = false;
        }

        public void PushSettings()
        {
            state.Settings.Tips = settings.Tips == TipsMode.Off ? CoreTips.Off
                : settings.Tips == TipsMode.WarningsOnly ? CoreTips.Warnings
                : CoreTips.On;
            state.Settings.Speed = settings.Speed == SubtitleSpeed.Slow ? CoreSpeed.Slow
                : settings.Speed == SubtitleSpeed.Fast ? CoreSpeed.Fast
                : settings.Speed == SubtitleSpeed.Instant ? CoreSpeed.Instant
                : CoreSpeed.Normal;
            state.Settings.Size = settings.Size == SubtitleSize.ExtraLarge ? CoreSize.ExtraLarge
                : settings.Size == SubtitleSize.Large ? CoreSize.Large
                : CoreSize.Normal;
            state.Settings.ReduceMotion = settings.ReduceMotion;
            if (!state.Settings.VoiceAvailable) state.Settings.VoiceOn = false;
        }

        public bool Offer(string trigger, string when, IReadOnlyDictionary<string, string> tokens, string stepId, bool onceThisFlight)
        {
            PushSettings();
            var line = script.Match(trigger, when);
            if (!state.Allows(line)) return false;
            if (IndexOf(line.Id) >= 0) return false;
            if (!TokensCover(line, tokens)) return false;
            if (onceThisFlight && !thisFlight.Add(line.Id)) return false;
            queued.Add(ToBeat(line, tokens, stepId));
            return true;
        }

        public bool SameStep(string sig)
        {
            if (sig == stepSig) return true;
            stepSig = sig ?? "";
            return false;
        }

        public void BeginFlight()
        {
            thisFlight.Clear();
            reportStamp = 0;
            queued.Clear();
        }

        public int BumpStreak(string missionId, bool passed)
        {
            if (missionId != streakMission)
            {
                streakMission = missionId ?? "";
                streak = 0;
            }
            if (passed) { streak = 0; return 0; }
            streak++;
            return streak;
        }

        public void Welcome()
        {
            if (state.FirstRunDone) return;
            for (int n = 1; n <= 5; n++)
                Offer("guide.first", n.ToString(), null, "home", false);
        }

        public void Complete(string beatId) => Finish(beatId, true);

        public void Skip(string beatId) => Finish(beatId, true);

        public void SkipTipsForStep(string stepId)
        {
            if (!string.IsNullOrEmpty(stepId)) skippedSteps.Add(stepId);
            for (int i = queued.Count - 1; i >= 0; i--)
            {
                var beat = queued[i];
                if (beat != null && beat.Priority == GuidePriority.Chatter && beat.StepId == stepId)
                    queued.RemoveAt(i);
            }
        }

        public string ClipFor(string beatId)
        {
            if (!ClipVoice || string.IsNullOrEmpty(beatId)) return null;
            return CoreVoice.ClipFor(repoRoot, beatId);
        }

        public void ShowReport(GuideReport facts)
        {
            thisFlight.Clear();
            GuideHooks.ShowReport(this, facts, ref reportStamp);
        }

        void Finish(string beatId, bool seen)
        {
            int at = IndexOf(beatId);
            if (at >= 0) queued.RemoveAt(at);
            if (seen && !string.IsNullOrEmpty(beatId)) state.MarkSeen(beatId);
            if (WelcomeDone()) state.FirstRunDone = true;
        }

        bool WelcomeDone()
        {
            for (int n = 1; n <= 5; n++)
                if (!state.HasSeen("first." + n)) return false;
            return true;
        }

        int IndexOf(string beatId)
        {
            for (int i = 0; i < queued.Count; i++)
                if (queued[i] != null && queued[i].Id == beatId) return i;
            return -1;
        }

        GuideBeat ToBeat(CoreLine line, IReadOnlyDictionary<string, string> tokens, string stepId)
        {
            var pri = MapPri(line.Priority);
            var cards = new GuideCard[line.Cards.Count];
            for (int i = 0; i < line.Cards.Count; i++)
                cards[i] = new GuideCard { Id = line.Cards[i].Id, Kid = line.Cards[i].Kid, Eng = line.Cards[i].Eng };
            var copy = new Dictionary<string, string>();
            if (tokens != null)
                foreach (var pair in tokens)
                    if (!string.IsNullOrEmpty(pair.Key) && pair.Value != null) copy[pair.Key] = pair.Value;
            return new GuideBeat
            {
                Id = line.Id,
                Speaker = GuideSpeaker.Thrum,
                Priority = pri,
                Critical = pri == GuidePriority.Safety,
                Decision = pri == GuidePriority.Decision || (line.Actions != null && line.Actions.Count > 0),
                StepId = stepId ?? "",
                Cards = cards,
                Tokens = copy.Count > 0 ? copy : null,
                FromEngine = true,
                VoiceClip = ClipFor(line.Id),
            };
        }

        static bool TokensCover(CoreLine line, IReadOnlyDictionary<string, string> tokens)
        {
            for (int i = 0; i < line.Cards.Count; i++)
            {
                if (!Cover(line.Cards[i].Kid, tokens)) return false;
                if (!Cover(line.Cards[i].Eng, tokens)) return false;
            }
            return true;
        }

        static bool Cover(string text, IReadOnlyDictionary<string, string> tokens)
        {
            if (string.IsNullOrEmpty(text)) return true;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '{') continue;
                int j = text.IndexOf('}', i + 1);
                if (j <= i + 1) return false;
                string key = text.Substring(i + 1, j - i - 1);
                if (tokens == null || !tokens.TryGetValue(key, out var value) || string.IsNullOrEmpty(value)) return false;
                if (value.IndexOf('{') >= 0) return false;
                i = j;
            }
            return true;
        }

        static GuidePriority MapPri(string priority)
        {
            if (priority == CorePri.Safety) return GuidePriority.Safety;
            if (priority == CorePri.Decision) return GuidePriority.Decision;
            if (priority == CorePri.Failure) return GuidePriority.Failure;
            return GuidePriority.Chatter;
        }

        static SubtitleSpeed MapSpeed(CoreSpeed speed)
        {
            if (speed == CoreSpeed.Slow) return SubtitleSpeed.Slow;
            if (speed == CoreSpeed.Fast) return SubtitleSpeed.Fast;
            if (speed == CoreSpeed.Instant) return SubtitleSpeed.Instant;
            return SubtitleSpeed.Normal;
        }

        static SubtitleSize MapSize(CoreSize size)
        {
            if (size == CoreSize.Large) return SubtitleSize.Large;
            if (size == CoreSize.ExtraLarge) return SubtitleSize.ExtraLarge;
            return SubtitleSize.Normal;
        }

        static TipsMode MapTips(CoreTips tips)
        {
            if (tips == CoreTips.Off) return TipsMode.Off;
            if (tips == CoreTips.Warnings) return TipsMode.WarningsOnly;
            return TipsMode.On;
        }
    }
}
