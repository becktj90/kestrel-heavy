// iOS bridge for the liftoff haptics (DECISIONS #100). Kestrel.Core.Feel.LiftoffHaptics decides each frame's pulses;
// this plays them as Taptic impacts through Assets/Plugins/iOS/KHFlightHaptics.mm (UIKit only). Everywhere else
// (Editor, tests, other platforms) nothing plays and the pulses are only recorded for tests and captures.
// The Haptics setting is the Hangar's existing switch (kh.hangar.haptics), now also in Settings.
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Kestrel.Core.Feel;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class FlightHaptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void KHFlightHapticsPrepare();
        [DllImport("__Internal")] static extern void KHFlightImpact(int style, float intensity);
#endif

        /// <summary>Pulses played since the last <see cref="ResetCount"/> (tests, captures, the Editor log).</summary>
        public static int Played { get; private set; }
        public static HapticPulse Last { get; private set; }

        public static bool Enabled
        {
            get
            {
                try { return LiftoffHaptics.On(PlayerPrefs.HasKey, k => PlayerPrefs.GetInt(k, 1)); }
                catch (System.Exception) { return true; }
            }
        }

        public static void ResetCount() { Played = 0; Last = default; }

        /// <summary>Wake the engine before the rumble (count opens).</summary>
        public static void Prepare()
        {
            if (!Enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            KHFlightHapticsPrepare();
#endif
        }

        public static void Play(List<HapticPulse> pulses)
        {
            if (pulses == null) return;
            for (int i = 0; i < pulses.Count; i++)
            {
                var p = pulses[i];
                Played++;
                Last = p;
#if UNITY_IOS && !UNITY_EDITOR
                KHFlightImpact((int)p.Style, p.Intensity);
#endif
            }
        }
    }
}
