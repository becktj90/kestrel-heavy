using System.Collections.Generic;
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
        }

        public void SetFlightClock(double seconds, double altM)
        {
            simTime = seconds;
            altitude = altM;
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
            color?.OnEvent(type);
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
            if (!schedule.Busy) { OfferColor(); return; }
            if (voice.isPlaying) { sawAudio = true; return; }
            if (!sawAudio && Time.unscaledTime < startedAt + 0.1f) return;
            if (Time.unscaledTime < startedAt + playFor && !sawAudio) return;
            var next = schedule.Complete((double)Time.unscaledTime);
            if (next != null) StartVoice(next);
            else director?.SetVoiceDuck(false);
        }

        // Color exchanges are offered only from here: never queued, never on a call's event.
        void OfferColor()
        {
            if (color == null || facts == null || AudioPrefs.CommentaryMode != CommentaryMode.Full) return;
            color.Poll(simTime, altitude, due);
            for (int i = 0; i < due.Count; i++)
            {
                var cue = due[i];
                bool recovery = cue.EventId == ColorWindows.Recovery;
                var offer = schedule.Offer(catalog, cue.EventId, facts, 0, CommentaryMode.Full, (double)Time.unscaledTime,
                    line => !recovery || ColorWindows.RecoveryFits(cue.SecToNextCall, line.Duration));
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
