using Kestrel.Core.Flight;
using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>Persisted mix toggles. Sound effects keep <see cref="AudioDirector.MutePref"/>.
    /// Music and narration are separate so a mute never silences the wrong bus.
    /// Subtitles and the sandbox switch live here too, and are mirrored into <see cref="MenuPrefs"/>
    /// so the guide and the mission board can read them without PlayerPrefs.</summary>
    public static class AudioPrefs
    {
        public const string MusicMutePref = "kh.audio.music.muted";
        public const string NarrationMutePref = "kh.audio.narration.muted";
        public const string SubtitlesPref = "kh.settings.subtitles";
        public const string SandboxPref = "kh.settings.sandbox";

        public static bool MusicMuted => PlayerPrefs.GetInt(MusicMutePref, 0) == 1;
        public static bool SfxMuted => PlayerPrefs.GetInt(AudioDirector.MutePref, 0) == 1;
        public static bool NarrationMuted => PlayerPrefs.GetInt(NarrationMutePref, 0) == 1;
        public static bool SubtitlesOn => PlayerPrefs.GetInt(SubtitlesPref, 1) == 1;
        public static bool SandboxUnlocked => PlayerPrefs.GetInt(SandboxPref, 1) == 1;

        /// <summary>Copy disk into <see cref="MenuPrefs"/>. Call before the guide engine is built.</summary>
        public static void Load()
        {
            MenuPrefs.SubtitlesOn = SubtitlesOn;
            MenuPrefs.SandboxUnlocked = SandboxUnlocked;
        }

        public static void SetMusicMuted(bool on)
        {
            PlayerPrefs.SetInt(MusicMutePref, on ? 1 : 0);
            PlayerPrefs.Save();
            MusicDirector.Instance?.Apply();
        }

        public static void SetSfxMuted(bool on)
        {
            var director = Object.FindAnyObjectByType<AudioDirector>();
            if (director != null) director.SetMuted(on);
            else PlayerPrefs.SetInt(AudioDirector.MutePref, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetNarrationMuted(bool on)
        {
            PlayerPrefs.SetInt(NarrationMutePref, on ? 1 : 0);
            PlayerPrefs.Save();
            if (!on) return;
            var voice = Object.FindAnyObjectByType<NarrationPlayer>();
            voice?.Stop();
        }

        public static void SetSubtitlesOn(bool on)
        {
            PlayerPrefs.SetInt(SubtitlesPref, on ? 1 : 0);
            PlayerPrefs.Save();
            MenuPrefs.SubtitlesOn = on;
        }

        public static void SetSandboxUnlocked(bool on)
        {
            PlayerPrefs.SetInt(SandboxPref, on ? 1 : 0);
            PlayerPrefs.Save();
            MenuPrefs.SandboxUnlocked = on;
        }

        /// <summary>Second argument is the off state, matching the mix buttons (muted, hidden, locked).</summary>
        public static string Caption(string name, bool off) => off ? name + " off" : name + " on";
    }
}
