using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Loops the original menu, hangar, and flight beds and crossfades between them.
    /// Music stays under SFX (mixer bus) and ducks further under narration and while the flight is paused.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MusicDirector : MonoBehaviour
    {
        public static MusicDirector Instance { get; private set; }

        AudioSource[] bus;
        int hot;
        float fade = 1f;
        bool fading;
        bool voice, suspended, performancePaused;

        public MusicBed Bed { get; private set; } = MusicBed.Menu;
        public AudioClip Clip => Hot != null ? Hot.clip : null;
        public float Volume => (Level(0) + Level(1));
        public bool Fading => fading;

        AudioSource Hot => bus != null ? bus[hot] : null;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            EnsureBuses();
        }

        void Reset() => EnsureBuses();

        /// <summary>Build the two buses. EditMode calls this before Awake.</summary>
        public void EnsureBuses()
        {
            if (bus != null) return;
            Instance = this;
            bus = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                bus[i] = gameObject.AddComponent<AudioSource>();
                bus[i].playOnAwake = false;
                bus[i].loop = true;
                bus[i].spatialBlend = 0f;
                bus[i].dopplerLevel = 0f;
                bus[i].ignoreListenerPause = true;
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Play(MusicBed next)
        {
            EnsureBuses();
            if (bus == null) return;
            suspended = false;
            var clip = Resources.Load<AudioClip>(MusicBeds.Resource(next));
            if (clip != null && next == Bed && Hot != null && Hot.clip == clip)
            {
                Apply();
                Drive();
                return;
            }
            Bed = next;
            int incoming = hot ^ 1;
            bus[incoming].clip = clip;
            bus[incoming].loop = true;
            bool cut = Hot == null || Hot.clip == null || Hot.volume <= 0.0001f || clip == null;
            hot = incoming;
            if (cut)
            {
                fading = false;
                fade = 1f;
                bus[hot ^ 1].volume = 0f;
            }
            else
            {
                fading = true;
                fade = 0f;
            }
            Apply();
            Drive();
        }

        public void SetVoiceDuck(bool active) { voice = active; Apply(); }
        public void SetSuspended(bool active) { suspended = active; Apply(); Drive(); }
        public void SetPerformancePaused(bool active) { performancePaused = active; Apply(); }

        public void Apply()
        {
            EnsureBuses();
            if (bus == null) return;
            float gain = MusicBeds.Gain(AudioPrefs.MusicMuted, suspended, voice, performancePaused);
            float inn = fading ? fade : 1f;
            bus[hot].volume = gain * inn;
            bus[hot ^ 1].volume = gain * (fading ? 1f - fade : 0f);
        }

        /// <summary>Advance the crossfade. <see cref="Update"/> calls this with unscaled time so a sim pause does not freeze the mix.</summary>
        public void Tick(float dt)
        {
            if (fading)
            {
                if (dt < 0f || float.IsNaN(dt)) dt = 0f;
                fade += dt / MusicBeds.FadeSeconds;
                if (fade >= 1f)
                {
                    fade = 1f;
                    fading = false;
                    var old = bus[hot ^ 1];
                    old.volume = 0f;
                    if (old.isPlaying) old.Stop();
                }
            }
            Apply();
            Drive();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.1f) dt = 0.1f;
            Tick(dt);
        }

        void Drive()
        {
            if (!Application.isPlaying || bus == null) return;
            for (int i = 0; i < 2; i++)
            {
                var s = bus[i];
                if (s == null) return;
                bool audible = s.volume > 0.0001f && s.clip != null;
                if (audible && !s.isPlaying) s.Play();
                else if (!audible && s.isPlaying) s.Stop();
            }
        }

        float Level(int i) => bus != null && bus[i] != null ? bus[i].volume : 0f;
    }
}
