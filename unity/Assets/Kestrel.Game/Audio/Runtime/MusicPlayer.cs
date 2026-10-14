using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>Original title/designer score. Narration and engine audio take precedence.</summary>
    [DisallowMultipleComponent]
    public sealed class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }
        AudioSource source;
        bool voice;
        bool suspended;
        public AudioClip Clip => source != null ? source.clip : null;
        public float Volume => source != null ? source.volume : 0f;

        void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        public void Select(bool home)
        {
            suspended = false;
            voice = false;
            var clip = Resources.Load<AudioClip>(home ? "Music/kestrel-title" : "Music/kestrel-designer");
            if (source.clip != clip) { source.Stop(); source.clip = clip; }
            Apply();
        }

        public void SetVoiceDuck(bool active) { voice = active; Apply(); }
        public void Suspend(bool active) { suspended = active; Apply(); }
        void Update() => Apply();
        void Apply()
        {
            if (source == null) return;
            bool muted = PlayerPrefs.GetInt(AudioDirector.MutePref, 0) == 1;
            source.volume = MixerLevels.Bus(muted || suspended, MixerLevels.Music, voice ? 0.35f : 1f);
            if (!Application.isPlaying) return;
            if (source.volume > 0f && source.clip != null && !source.isPlaying) source.Play();
            else if (source.volume == 0f && source.isPlaying) source.Pause();
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
