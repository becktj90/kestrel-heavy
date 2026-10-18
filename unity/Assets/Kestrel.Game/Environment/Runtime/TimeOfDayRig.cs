// Time of day driver. Picks the R1 sky clock and the sun/ambient/fog look from a TimeOfDayPreset.
//   Gameplay:  TimeOfDayRig.SetMission("KH-7")            // deterministic day/sunset/night + seeded variety
//              TimeOfDayRig.Set(LaunchTimeOfDay.Night, seed)
//   Captures:  -khTimeOfDay=night|sunset|day [-khTodSeed=N]  or  TimeOfDayRig.Force(tod, seed)  (wins over Set)
// Self-attaching: a hidden driver finds the active SkyRig, forces its clock once per rig/selection, then each
// LateUpdate (after SkyRig's own Refresh) pulls sun colour/intensity, ambient and fog toward the preset.
// No per-frame allocations. Pad lights follow via the sun elevation SkyRig publishes (PadLights.ResolveNight).
using System;
using Kestrel.Core.Sky;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Game.Environment
{
    /// <summary>Pure helper: which local hour puts the sun at a preset's elevation. Engine-free maths.</summary>
    public static class TimeOfDayHours
    {
        public static double ElevationDegAt(double hoursLocal, double lat, double lon, int y, int m, int d) =>
            SkyBodies.SunElevationRad(lat, lon, y, m, d, SkyBodies.UtcFromLocalHours(hoursLocal, lon)) * SkyBodies.Rad2Deg;

        /// <summary>Local hour in [0,24) whose sun elevation best matches the preset. Day: nearest to early afternoon;
        /// Sunset: the evening descent; Night: nearest to 02:00. Falls back to the preset's approximate hour.</summary>
        public static double Solve(TimeOfDayPreset p, double lat, double lon, int y, int m, int d)
        {
            double target = p.SunElevationDeg, best = p.ApproxLocalHours, bestErr = double.MaxValue;
            for (double h = 0; h < 24.0; h += 0.05)
            {
                double e = ElevationDegAt(h, lat, lon, y, m, d);
                double err = Math.Abs(e - target);
                bool ok = p.Tod switch
                {
                    LaunchTimeOfDay.Sunset => h >= 12.0 && ElevationDegAt(h + 0.05, lat, lon, y, m, d) < e,   // descending, afternoon side
                    LaunchTimeOfDay.Night => e < Math.Min(-8.0, target + 12.0),
                    _ => h >= 9.0 && h <= 16.0,
                };
                if (!ok) continue;
                // tie-break toward the preset's nominal hour so Day/Night don't wander across the clock
                err += Math.Abs(h - p.ApproxLocalHours) * (p.Tod == LaunchTimeOfDay.Sunset ? 0.0 : 0.02);
                if (err < bestErr) { bestErr = err; best = h; }
            }
            return best;
        }
    }

    public sealed class TimeOfDayRig : MonoBehaviour
    {
        public static LaunchTimeOfDay? Selected { get; private set; }
        public static LaunchTimeOfDay? Forced { get; private set; }
        public static TimeOfDayPreset Current { get; private set; }

        static int _seed = 1, _forcedSeed = 1, _version; static bool _cliChecked;
        const float Mix = 0.85f;   // per-frame pull toward the preset; ~instant, and idempotent when R1 re-applies

        /// <summary>Gameplay selection (e.g. from a mission). Ignored while a capture override is active.</summary>
        public static void Set(LaunchTimeOfDay tod, int seed = 1) { Selected = tod; _seed = seed; Current = TimeOfDayPresets.For(tod, seed); _version++; }
        public static void SetMission(string missionId) { int s = TimeOfDayPresets.SeedFor(missionId); Set(TimeOfDayPresets.Pick(s), s); }
        /// <summary>Capture override. Wins over Set; also what -khTimeOfDay parses to.</summary>
        public static void Force(LaunchTimeOfDay tod, int seed = 1) { Forced = tod; _forcedSeed = seed; Current = TimeOfDayPresets.For(tod, seed); _version++; }
        /// <summary>Removes the capture override. R1's clock stays where it was forced; Set() again to move it.</summary>
        public static void ClearForce() { Forced = null; Current = Selected.HasValue ? TimeOfDayPresets.For(Selected.Value, _seed) : null; _version++; }

        SkyRig _rig; int _appliedVersion = -1; SkyRig _appliedRig; float _nextFind;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!_cliChecked)
            {
                _cliChecked = true;
                try { if (TimeOfDayPresets.TryParseArgs(System.Environment.GetCommandLineArgs(), out var t, out var s)) Force(t, s); } catch (Exception) { }
            }
            var go = new GameObject("KH_TimeOfDay") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go); go.AddComponent<TimeOfDayRig>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Selected = null; Forced = null; Current = null; _seed = _forcedSeed = 1; _version = 0; _cliChecked = false; }

        void LateUpdate()
        {
            var p = Current; if (p == null) return;
            if (_rig == null && Time.unscaledTime >= _nextFind) { _rig = FindFirstObjectByType<SkyRig>(); _nextFind = Time.unscaledTime + 0.5f; }
            if (_rig == null) return;
            if (_appliedRig != _rig || _appliedVersion != _version)
            {
                _appliedRig = _rig; _appliedVersion = _version;
                double h = TimeOfDayHours.Solve(p, _rig.LatitudeDeg, _rig.LongitudeDeg, _rig.Year, _rig.Month, _rig.Day);
                SkyRig.ForceTimeOfDay(h);
            }
            Tint(p, _rig.SunLight);
        }

        static Color C(Rgb3 c) => new Color(c.R, c.G, c.B, 1f);

        /// <summary>Pulls R1's sun, ambient and fog toward the preset. Pure Unity-state writes, no allocation.</summary>
        public static void Tint(TimeOfDayPreset p, Light sun)
        {
            if (sun != null)
            {
                sun.color = Color.Lerp(sun.color, C(p.SunColor), p.SunTintMix * Mix);
                sun.intensity = Mathf.Lerp(sun.intensity, p.SunIntensity, Mix);
                sun.shadowStrength = Mathf.Lerp(sun.shadowStrength, p.ShadowStrength, Mix);
                sun.shadows = p.ShadowStrength > 0.01f ? (sun.shadows == LightShadows.None ? LightShadows.Soft : sun.shadows) : LightShadows.None;
            }
            float nf = p.NightFactor;   // ambient colours move only as far as the preset's own darkness
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, C(p.AmbientSky), Mix * Mathf.Max(0.35f, nf));
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, C(p.AmbientEquator), Mix * Mathf.Max(0.35f, nf));
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, C(p.AmbientGround), Mix * Mathf.Max(0.35f, nf));
            RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, p.AmbientIntensity, Mix);
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, C(p.FogColor), p.FogTintMix * Mix);
                // R1 owns the fog distances; FogDensity stays preset data (scaling them here would compound per frame).
            }
        }
    }
}
