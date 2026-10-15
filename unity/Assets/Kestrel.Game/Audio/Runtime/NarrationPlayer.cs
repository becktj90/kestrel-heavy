using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Plays the pre-generated ElevenLabs takes. Keyed by manifest event id.
    /// One voice at a time; while it plays, SFX and the engine loop duck.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NarrationPlayer : MonoBehaviour, IFlightEventSink
    {
        public const string ManifestResource = "Narration/manifest";

        readonly NarrationSchedule schedule = new NarrationSchedule();
        AudioDirector director;
        AudioSource voice;
        NarrationCatalog catalog = NarrationCatalog.Empty;
        float startedAt;
        float playFor;
        bool sawAudio;

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

        public void OnEvent(string id)
        {
            if (id == "flight.begin" || id == "flight.end")
            {
                schedule.Reset();
                StopVoice();
                return;
            }
            var start = schedule.Push(catalog, id, AudioPrefs.NarrationMuted);
            if (start != null) StartVoice(start);
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
            var asset = Resources.Load<TextAsset>(ManifestResource);
            if (asset == null || string.IsNullOrEmpty(asset.text)) return;
            try { catalog = NarrationCatalog.Parse(asset.text); }
            catch (System.FormatException) { catalog = NarrationCatalog.Empty; }
        }

        void Update()
        {
            if (Held || !schedule.Busy || !Application.isPlaying || voice == null) return;
            if (voice.isPlaying) { sawAudio = true; return; }
            if (!sawAudio && Time.unscaledTime < startedAt + 0.1f) return;
            if (Time.unscaledTime < startedAt + playFor && !sawAudio) return;
            var next = schedule.Complete();
            if (next != null) StartVoice(next);
            else director?.SetVoiceDuck(false);
        }

        void StartVoice(string resource)
        {
            Started++;
            LastResource = resource;
            var clip = Resources.Load<AudioClip>(resource);
            if (clip == null)
            {
                var next = schedule.Complete();
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
