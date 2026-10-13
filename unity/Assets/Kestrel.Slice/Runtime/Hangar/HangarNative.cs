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
        [DllImport("__Internal")] static extern void KHHangarHaptic(int kind, int[] pattern, int count);
        [DllImport("__Internal")] static extern void KHHangarAnnounce(string text);
        [DllImport("__Internal")] static extern int KHHangarReduceMotion();
#endif

        /// <summary>Last thing announced, for tests and the Editor (where there is no VoiceOver).</summary>
        public static string LastAnnouncement { get; private set; } = "";
        public static Haptic LastHaptic { get; private set; }
        public static int[] LastPattern { get; private set; } = System.Array.Empty<int>();

        public static bool HapticsOn => PlayerPrefs.GetInt(HangarFeel.HapticsPref, 1) != 0;

        /// <summary>Play a haptic with its designer-feel.json pattern: pulse, gap, pulse … in milliseconds (the web
        /// vibrate convention). On iOS each pulse is one Taptic tap, stronger for a longer pulse; gaps space them.</summary>
        public static void Play(Haptic h, int[] pattern)
        {
            if (h == Haptic.None || !HapticsOn || pattern == null || pattern.Length == 0) return;
            LastHaptic = h;
            LastPattern = pattern;
#if UNITY_IOS && !UNITY_EDITOR
            KHHangarHaptic((int)h, pattern, pattern.Length);
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
