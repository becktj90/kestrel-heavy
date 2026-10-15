using System.Runtime.InteropServices;

namespace Kestrel.Game.Guide
{
    /// <summary>iOS VoiceOver and the system voice. Elsewhere these no-op; the box tooltip is the spoken line.
    /// The pref key matches HangarCamera.ReduceMotionPref. No new audio asset: the typewriter tick is Sfx/quindar.</summary>
    public static class GuideNative
    {
        public const string ReduceMotionPref = "kh.a11y.reduceMotion";

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void KHGuideAnnounce(string text);
        [DllImport("__Internal")] static extern void KHGuideSpeak(string text);
        [DllImport("__Internal")] static extern void KHGuideStopSpeech();
        [DllImport("__Internal")] static extern int KHGuideReduceMotion();
#endif

        public static bool ReduceMotion
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return KHGuideReduceMotion() != 0;
#else
                return false;
#endif
            }
        }

        public static void Announce(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
#if UNITY_IOS && !UNITY_EDITOR
            KHGuideAnnounce(text);
#endif
        }

        public static void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
#if UNITY_IOS && !UNITY_EDITOR
            KHGuideSpeak(text);
#endif
        }

        public static void Stop()
        {
#if UNITY_IOS && !UNITY_EDITOR
            KHGuideStopSpeech();
#endif
        }
    }
}
