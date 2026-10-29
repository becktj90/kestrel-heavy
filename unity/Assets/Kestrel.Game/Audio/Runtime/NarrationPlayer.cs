using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Flight.Narration;
using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Plays the pre-generated ElevenLabs takes. Keyed by manifest event id.
    /// One voice at a time; while it plays, SFX and the engine loop duck.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NarrationPlayer : MonoBehaviour, IFlightStageSink
    {
        public const string ManifestResource = "Narration/manifest";

        readonly NarrationSchedule schedule = new NarrationSchedule();
        AudioDirector director;
        AudioSource voice;
        NarrationCatalog catalog = NarrationCatalog.Empty;
        AudioSource fadeVoice;
        AudioSource fading;
        float fadeLeft, fadeFrom;
        float startedAt;
        float playFor;
        bool sawAudio;
        NarrationFacts facts;
        ColorDirector color;
        CallTriggers triggers;
        readonly List<string> countBeats = new List<string>();
        bool guideTalking;
        bool strapOns;
        double simTime, altitude;
        readonly List<ColorCue> due = new List<ColorCue>();
        public NarrationSchedule Schedule => schedule;

        public int Started { get; private set; }
        public string LastResource { get; private set; }
        public bool Busy => schedule.Busy;
        public bool Held { get; private set; }

        public void Bind(AudioDirector audio) => director = audio;

        /// <summary>Pause the take with the flight. Unpause continues the same sample.</summary>
        public void Hold(bool on)
        {
            Held = on;
            if (voice == null) return;
            if (on) { if (voice.isPlaying) voice.Pause(); }
            else voice.UnPause();
        }

        public void Use(NarrationCatalog next) => catalog = next ?? NarrationCatalog.Empty;

        /// <summary>A plain id, no stack entry: the old path. Ignition then reads as the pad start.</summary>
        public void OnEvent(string id) => OnStagedEvent(id, 0);

        public void BeginNarration(NarrationFacts launch, CallSchedule calls)
        {
            facts = launch;
            strapOns = launch != null && launch.TryGet("strapons", out var n) && n is int count && count > 0;
            bool recovery = launch != null && launch.Recovery != null && launch.Recovery != "none" && launch.Stages >= 2;
            color = new ColorDirector(recovery);
            color.UseCalls(calls);
            // The flight's own triggers: whatever the count had due is gone at liftoff.
            triggers = new CallTriggers(calls, launch != null ? launch.Stages : 0, strapOns, recovery);
            triggers.Liftoff();
            guideTalking = false;
        }

        /// <summary>
        /// The terminal count opened for this design. Pad and tank-press calls can play during it, into silence and
        /// only with a take that ends before T-0. <paramref name="launch"/> is the same facts the flight will use.
        /// </summary>
        public void BeginCount(NarrationFacts launch, CallSchedule calls)
        {
            facts = launch;
            strapOns = launch != null && launch.TryGet("strapons", out var n) && n is int count && count > 0;
            color = null;
            schedule.Reset();
            StopVoice();
            countBeats.Clear();
            triggers = launch != null ? new CallTriggers(calls, launch.Stages, strapOns) : null;
        }

        /// <summary>
        /// The count's clock (negative seconds) and the ids of the beats it has fired so far.
        /// <paramref name="guideSpeaking"/>: the guide has a line up; a pre-launch call waits for it.
        /// </summary>
        public void CountTick(double countSeconds, IReadOnlyList<Kestrel.Core.Timeline.LaunchBeat> fired, bool guideSpeaking = false)
        {
            guideTalking = guideSpeaking;
            if (triggers == null) return;
            countBeats.Clear();
            if (fired != null) for (int i = 0; i < fired.Count; i++) countBeats.Add(fired[i] != null ? fired[i].Id : null);
            triggers.CountTick(countSeconds, countBeats, (double)Time.unscaledTime);
        }

        /// <summary>The count closed without a flight (back to the hangar). Its calls stop.</summary>
        public void EndCount()
        {
            triggers = null;
            Stop();
        }

        public void SetFlightClock(double seconds, double altM)
        {
            simTime = seconds;
            altitude = altM;
            triggers?.OnAltitude(altM, (double)Time.unscaledTime);
        }

        public void OnStagedEvent(string type, int stage)
        {
            if (type == "flight.begin" || type == "flight.end")
            {
                schedule.Reset();
                StopVoice();
                color?.Reset();
                return;
            }
            color?.OnEvent(type, simTime);
            triggers?.OnSimEvent(type, stage, (double)Time.unscaledTime);
            // Facts unknown (no launch gave them) means only the neutral takes can play.
            var cue = NarrationRoute.Route(type, stage, strapOns, facts != null ? facts.Stages : 0);
            if (cue.Silent) return;
            int lit = cue.LitStage > 0 ? cue.LitStage : NarrationRoute.DefaultLit(cue.EventId, facts != null ? facts.Stages : 0);
            Apply(schedule.Offer(catalog, cue.EventId, facts, lit, AudioPrefs.CommentaryMode, (double)Time.unscaledTime));
        }

        void Apply(NarrationOffer offer)
        {
            if (!offer.Started) return;
            if (offer.FadeOutPrevious) FadeOutVoice();
            StartVoice(offer.Resource);
        }

        /// <summary>The player changed Full / Calls only / Off. Off stops the voice; Calls only drops a color exchange that is playing.</summary>
        public void OnCommentaryChanged(CommentaryMode mode)
        {
            if (mode == CommentaryMode.Off) { Stop(); return; }
            if (mode == CommentaryMode.CallsOnly && schedule.CurrentIsColor)
            {
                FadeOutVoice();
                schedule.Complete((double)Time.unscaledTime);
                director?.SetVoiceDuck(false);
            }
        }

        public void SetFlightState(float alt, float q, float throttle) { }

        public void Stop()
        {
            schedule.Release();
            StopVoice();
        }

        void Awake()
        {
            var child = new GameObject("Voice");
            child.transform.SetParent(transform, false);
            voice = child.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            voice.dopplerLevel = 0f;
            voice.loop = false;
            var spare = new GameObject("VoiceFade");
            spare.transform.SetParent(transform, false);
            fadeVoice = spare.AddComponent<AudioSource>();
            fadeVoice.playOnAwake = false;
            fadeVoice.spatialBlend = 0f;
            fadeVoice.dopplerLevel = 0f;
            fadeVoice.loop = false;
            var asset = Resources.Load<TextAsset>(ManifestResource);
            if (asset == null || string.IsNullOrEmpty(asset.text)) return;
            try { catalog = NarrationCatalog.Parse(asset.text); }
            catch (System.FormatException) { catalog = NarrationCatalog.Empty; }
        }

        void Update()
        {
            TickFade();
            if (Held || !Application.isPlaying || voice == null) return;
            OfferTrigger();
            if (!schedule.Busy) { OfferColor(); return; }
            if (voice.isPlaying) { sawAudio = true; return; }
            if (!sawAudio && Time.unscaledTime < startedAt + 0.1f) return;
            if (Time.unscaledTime < startedAt + playFor && !sawAudio) return;
            var next = schedule.Complete((double)Time.unscaledTime);
            if (next != null) StartVoice(next);
            else director?.SetVoiceDuck(false);
        }

        // Pad, tank press, SECO, Karman and entry: due only when the event really happened (CallTriggers). Never over
        // a waiting call; before liftoff only into silence and with a take that ends before T-0.
        void OfferTrigger()
        {
            if (triggers == null || facts == null) return;
            double now = (double)Time.unscaledTime;
            if (color != null) triggers.OnRecovery(RecoveryLog.FlightMarks, RecoveryLog.RailClock(simTime), now);
            if (!triggers.Next(now, schedule.Busy || guideTalking, !string.IsNullOrEmpty(schedule.Pending), out var cue)) return;
            triggers.Offered(cue);
            int lit = cue.LitStage > 0 ? cue.LitStage : NarrationRoute.DefaultLit(cue.EventId, facts.Stages);
            double max = cue.MaxSec;
            System.Func<NarrationLine, bool> fits = null;
            if (!double.IsInfinity(max)) fits = line => line.Duration <= max;
            Apply(schedule.Offer(catalog, cue.EventId, facts, lit, AudioPrefs.CommentaryMode, now, fits));
        }

        // Color exchanges are offered only from here: never queued, never on a call's event.
        void OfferColor()
        {
            if (color == null || facts == null || AudioPrefs.CommentaryMode != CommentaryMode.Full) return;
            color.Poll(simTime, altitude, due);
            for (int i = 0; i < due.Count; i++)
            {
                var cue = due[i];
                // Recovery, and ascent on a rocket with no MECO, only take a line that ends before the next call.
                bool mustFit = cue.MustFit;
                var offer = schedule.Offer(catalog, cue.EventId, facts, 0, CommentaryMode.Full, (double)Time.unscaledTime,
                    line => !mustFit || ColorWindows.RecoveryFits(cue.SecToNextCall, line.Duration));
                if (!offer.Started) continue;
                color.MarkDone(cue.EventId);
                StartVoice(offer.Resource);
                break;
            }
        }

        void FadeOutVoice()
        {
            if (voice == null) return;
            if (fading != null) fading.Stop();
            if (!voice.isPlaying) return;
            fading = voice;
            fadeLeft = (float)NarrationSchedule.FadeSeconds;
            fadeFrom = fading.volume;
            voice = fadeVoice;
            fadeVoice = fading;
        }

        void TickFade()
        {
            if (fading == null) return;
            fadeLeft -= Time.unscaledDeltaTime;
            if (fadeLeft <= 0f) { fading.Stop(); fading.volume = 0f; fading = null; return; }
            fading.volume = fadeFrom * Mathf.Clamp01(fadeLeft / (float)NarrationSchedule.FadeSeconds);
        }

        void StartVoice(string resource)
        {
            Started++;
            LastResource = resource;
            var clip = Resources.Load<AudioClip>(resource);
            if (clip == null)
            {
                var next = schedule.Complete((double)Time.unscaledTime);
                director?.SetVoiceDuck(false);
                if (next != null && next != resource) StartVoice(next);
                return;
            }
            playFor = Mathf.Max(0.05f, catalog.Duration(resource));
            startedAt = Time.unscaledTime;
            sawAudio = false;
            voice.Stop();
            voice.clip = clip;
            voice.volume = MixerLevels.Bus(false, MixerLevels.Voice, 1f);
            if (Application.isPlaying) voice.Play();
            director?.SetVoiceDuck(true);
        }

        void StopVoice()
        {
            sawAudio = false;
            if (voice != null) voice.Stop();
            director?.SetVoiceDuck(false);
        }
    }
}
