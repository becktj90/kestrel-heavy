// iOS bridge for the Hangar: Taptic feedback, VoiceOver announcements and the system Reduce Motion setting.
// The native side is Assets/Plugins/iOS/KHHangarNative.mm (UIKit only; no third-party code). Everywhere else
// (Editor, tests, other platforms) these calls do nothing and Reduce Motion reads false.
using System.Runtime.InteropServices;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class HangarNative
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void KHHangarHaptic(int kind);
        [DllImport("__Internal")] static extern void KHHangarAnnounce(string text);
        [DllImport("__Internal")] static extern int KHHangarReduceMotion();
#endif

        /// <summary>Last thing announced, for tests and the Editor (where there is no VoiceOver).</summary>
        public static string LastAnnouncement { get; private set; } = "";
        public static Haptic LastHaptic { get; private set; }

        public static bool HapticsOn => PlayerPrefs.GetInt(HangarFeel.HapticsPref, 1) != 0;

        public static void Play(Haptic h)
        {
            if (h == Haptic.None || !HapticsOn) return;
            LastHaptic = h;
#if UNITY_IOS && !UNITY_EDITOR
            KHHangarHaptic((int)h);
#endif
        }

        public static void Announce(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            LastAnnouncement = text;
#if UNITY_IOS && !UNITY_EDITOR
            KHHangarAnnounce(text);
#endif
        }

        /// <summary>The iOS Settings › Accessibility › Motion › Reduce Motion switch.</summary>
        public static bool SystemReduceMotion
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return KHHangarReduceMotion() != 0;
#else
                return false;
#endif
            }
        }
    }
}
