using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>Shell hook for <see cref="MusicDirector"/>. One player, two buses.</summary>
    [DisallowMultipleComponent]
    public sealed class MusicPlayer : MonoBehaviour
    {
        static MusicPlayer instance;

        /// <summary>The live player. A destroyed reference is dropped, so <c>Instance?.Play</c> does not touch it.</summary>
        public static MusicPlayer Instance
        {
            get
            {
                if (instance == null) instance = null;
                return instance;
            }
            private set => instance = value;
        }
        MusicDirector director;

        public AudioClip Clip => director != null ? director.Clip : null;
        public float Volume => director != null ? director.Volume : 0f;
        public MusicBed Bed => director != null ? director.Bed : MusicBed.Menu;

        // EditMode tests add the component and call it immediately. Awake has not run yet there,
        // so playback has to finish setup itself, the same way the previous player did.
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Ensure();
        }

        void Reset() => Ensure();

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Select(bool home) => Play(home ? MusicBed.Menu : MusicBed.Hangar);

        public void Play(MusicBed bed)
        {
            Ensure();
            director?.Play(bed);
        }

        public void SetVoiceDuck(bool active) { Ensure(); director?.SetVoiceDuck(active); }
        public void Suspend(bool active) { Ensure(); director?.SetSuspended(active); }
        public void SetPaused(bool active) { Ensure(); director?.SetPerformancePaused(active); }

        void Ensure()
        {
            if (this == null || director != null) return;
            Instance = this;
            director = GetComponent<MusicDirector>() ?? gameObject.AddComponent<MusicDirector>();
            director.EnsureBuses();
        }
    }
}
