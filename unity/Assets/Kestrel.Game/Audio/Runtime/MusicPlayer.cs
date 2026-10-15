using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>Shell hook for <see cref="MusicDirector"/>. One player, two buses, original beds only.</summary>
    [DisallowMultipleComponent]
    public sealed class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }
        MusicDirector director;

        public AudioClip Clip => director != null ? director.Clip : null;
        public float Volume => director != null ? director.Volume : 0f;
        public MusicBed Bed => director != null ? director.Bed : MusicBed.Menu;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            director = GetComponent<MusicDirector>() ?? gameObject.AddComponent<MusicDirector>();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Select(bool home) => Play(home ? MusicBed.Menu : MusicBed.Hangar);
        public void Play(MusicBed bed) => director?.Play(bed);
        public void SetVoiceDuck(bool active) => director?.SetVoiceDuck(active);
        public void Suspend(bool active) => director?.SetSuspended(active);
        public void SetPaused(bool active) => director?.SetPerformancePaused(active);
    }
}
