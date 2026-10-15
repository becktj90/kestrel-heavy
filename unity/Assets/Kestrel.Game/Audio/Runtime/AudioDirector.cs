using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Pooled one-shots, a throttle/q engine loop, UI clicks, and mute.
    /// External air drops out by 80 km; structure-borne rumble stays while the engines burn.
    /// Wire with <see cref="Attach"/> once. Original music is managed by MusicPlayer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour, IFlightEventSink
    {
        public const int PoolSize = 12;
        public const string MutePref = "kh.audio.muted";

        AudioSource engine;
        readonly AudioSource[] pool = new AudioSource[PoolSize];
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly VoicePool voices = new VoicePool(PoolSize);
        readonly SfxGate gate = new SfxGate();
        IFlightEventSink forward;
        bool muted;
        bool uiBound;
        float alt, q, throttle, duck = 1f, peakQ;
        bool maxqSent;
        string engineBed = "roar-loop";
        float clock;

        public bool Muted => muted;
        public float EngineVolume { get; private set; }
        public float EnginePitch { get; private set; }
        /// <summary>One-shots that passed mute, vacuum, and the retrigger lock.</summary>
        public int Accepted { get; private set; }
        /// <summary>Flight pause. Sources are paused, not stopped, so Resume continues the same sample.</summary>
        public bool Held { get; private set; }

        /// <summary>The only flight hook. Assigns this director as the bus sink and binds UI clicks.</summary>
        public static AudioDirector Attach(IFlightAudioBus bus)
        {
            if (bus is not Component host)
                throw new ArgumentException("Flight audio bus must be a component.", nameof(bus));
            var director = host.GetComponent<AudioDirector>();
            if (director == null) director = host.gameObject.AddComponent<AudioDirector>();
            var narration = host.GetComponent<NarrationPlayer>() ?? host.gameObject.AddComponent<NarrationPlayer>();
            narration.Bind(director);
            director.ForwardTo(narration);
            bus.Sink = director;
            director.TryBindUi();
            if (MusicPlayer.Instance == null) host.gameObject.AddComponent<MusicPlayer>();
            return director;
        }

        /// <summary>Optional second sink (narration). Receives every event id, including ones with no SFX.</summary>
        public void ForwardTo(IFlightEventSink sink) => forward = sink;

        public void SetVoiceDuck(bool active)
        {
            MusicPlayer.Instance?.SetVoiceDuck(active);
            duck = active ? MixerLevels.VoiceDuck : 1f;
            ApplyEngine();
        }

        public void SetMuted(bool on)
        {
            muted = on;
            PlayerPrefs.SetInt(MutePref, on ? 1 : 0);
            if (on) voices.StopAll();
            ApplyEngine();
        }

        /// <summary>Freeze engine and one-shots with the sim. Music is not paused here.</summary>
        public void Hold(bool on)
        {
            if (Held == on) return;
            Held = on;
            if (on) PauseSources();
            else ReleaseSources();
        }

        public void OnEvent(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (id == "flight.begin") { MusicPlayer.Instance?.Play(MusicBed.Flight); BeginFlight(); }
            else PlaySfx(id);
            if (forward != null) forward.OnEvent(id);
        }

        public void SetFlightState(float altitudeM, float qPa, float throttle01)
        {
            alt = altitudeM;
            q = qPa;
            throttle = throttle01;
            if (qPa > peakQ) peakQ = qPa;
            if (!maxqSent && peakQ >= EngineMix.MaxQArmPa && qPa < peakQ * EngineMix.MaxQDrop)
            {
                maxqSent = true;
                OnEvent("maxq");
            }
            ApplyEngine();
        }

        void Awake()
        {
            muted = PlayerPrefs.GetInt(MutePref, 0) == 1;
            foreach (var file in SfxCatalog.Files)
            {
                var clip = Resources.Load<AudioClip>("Sfx/" + file);
                if (clip != null) clips[file] = clip;
            }
            engine = CreateSource("Engine");
            engine.loop = true;
            for (int i = 0; i < PoolSize; i++) pool[i] = CreateSource("Sfx" + i);
        }

        void LateUpdate() { if (!uiBound) TryBindUi(); }

        void BeginFlight()
        {
            maxqSent = false;
            peakQ = 0f;
            engineBed = "roar-loop";
            gate.Reset();
        }

        void PlaySfx(string id)
        {
            if (id == "burn") engineBed = "burn-loop";
            else if (id == "liftoff" || id == "ignition" || id == "ignite") engineBed = "roar-loop";
            if (!SfxCatalog.TryResolve(id, out var spec)) return;
            if (muted) return;
            if (spec.Air && EngineMix.ExternalGain(alt) <= 0f) return;
            clock = Time.unscaledTime;
            if (!gate.Allow(spec.Name, spec.Lock, clock)) return;
            Accepted++;
            if (!clips.TryGetValue(spec.File, out var clip) || clip == null) return;
            int priority = spec.Critical ? 2 : 1;
            var slot = voices.Acquire(spec.Name, priority);
            if (slot == null) return;
            float gain = spec.Volume * MixerLevels.Bus(false, MixerLevels.Sfx, duck);
            if (spec.Air) gain *= EngineMix.ExternalGain(alt);
            var src = pool[slot.Index];
            slot.Stop = () => { if (src != null) src.Stop(); };
            src.loop = false;
            src.pitch = 1f;
            src.volume = gain;
            src.clip = clip;
            if (Application.isPlaying) src.Play();
        }

        void ApplyEngine()
        {
            EnginePitch = EngineMix.Pitch(alt, q, throttle);
            EngineVolume = EngineMix.Volume(alt, q, throttle) * MixerLevels.Bus(muted, MixerLevels.Engine, duck);
            if (engine == null) return;
            if (clips.TryGetValue(engineBed, out var clip) && engine.clip != clip) engine.clip = clip;
            engine.pitch = EnginePitch;
            engine.volume = EngineVolume;
            bool should = EngineVolume > 0.001f && engine.clip != null && !muted;
            if (!Application.isPlaying || Held) return;
            if (should && !engine.isPlaying) engine.Play();
            else if (!should && engine.isPlaying) engine.Stop();
        }

        void PauseSources()
        {
            PauseOne(engine);
            for (int i = 0; i < pool.Length; i++) PauseOne(pool[i]);
        }

        void ReleaseSources()
        {
            ReleaseOne(engine);
            for (int i = 0; i < pool.Length; i++) ReleaseOne(pool[i]);
        }

        static void PauseOne(AudioSource source) { if (source != null && source.isPlaying) source.Pause(); }
        static void ReleaseOne(AudioSource source) { if (source != null) source.UnPause(); }

        void TryBindUi()
        {
            if (uiBound) return;
            var doc = GetComponent<UIDocument>();
            if (doc == null) { uiBound = true; return; }
            var root = doc.rootVisualElement;
            if (root == null) return;
            root.RegisterCallback<ClickEvent>(OnUiClick);
            uiBound = true;
        }

        void OnUiClick(ClickEvent evt)
        {
            var ve = evt.target as VisualElement;
            while (ve != null && !(ve is Button)) ve = ve.parent;
            if (ve is Button) OnEvent("ui");
        }

        AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f;
            src.loop = false;
            return src;
        }
    }
}
