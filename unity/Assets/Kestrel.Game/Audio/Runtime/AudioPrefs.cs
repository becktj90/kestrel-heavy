using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>Persisted mix toggles. Sound effects keep <see cref="AudioDirector.MutePref"/>.
    /// Music and narration are separate so a mute never silences the wrong bus.</summary>
    public static class AudioPrefs
    {
        public const string MusicMutePref = "kh.audio.music.muted";
        public const string NarrationMutePref = "kh.audio.narration.muted";

        public static bool MusicMuted => PlayerPrefs.GetInt(MusicMutePref, 0) == 1;
        public static bool SfxMuted => PlayerPrefs.GetInt(AudioDirector.MutePref, 0) == 1;
        public static bool NarrationMuted => PlayerPrefs.GetInt(NarrationMutePref, 0) == 1;

        public static void SetMusicMuted(bool on)
        {
            PlayerPrefs.SetInt(MusicMutePref, on ? 1 : 0);
            MusicDirector.Instance?.Apply();
        }

        public static void SetSfxMuted(bool on)
        {
            var director = Object.FindAnyObjectByType<AudioDirector>();
            if (director != null) director.SetMuted(on);
            else PlayerPrefs.SetInt(AudioDirector.MutePref, on ? 1 : 0);
        }

        public static void SetNarrationMuted(bool on)
        {
            PlayerPrefs.SetInt(NarrationMutePref, on ? 1 : 0);
            if (!on) return;
            var voice = Object.FindAnyObjectByType<NarrationPlayer>();
            voice?.Stop();
        }

        public static string Caption(string name, bool muted) => muted ? name + " off" : name + " on";
    }
}
