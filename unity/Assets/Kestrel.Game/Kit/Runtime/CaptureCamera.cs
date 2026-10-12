// Validation camera override (Editor + development builds only). Lets CLI shot capture pin the rendered
// frame to an exact pose without touching the design/chase camera code: a hidden driver with a very late
// execution order re-poses Camera.main in LateUpdate AFTER SliceApp's design framing / chase camera ran,
// so the frame that renders really uses the override.
//   CaptureCamera.Override(pos, lookAt) / CaptureCamera.Clear()
//   command line: -khCapCam x,y,z:lx,ly,lz   (world metres; e.g. -khCapCam 40,25,-60:0,30,0)
// Note: while active, the camera's projection is reset to its plain perspective (design-band shift removed).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using UnityEngine;

namespace Kestrel.Kit
{
    public static class CaptureCamera
    {
        public static bool Active { get; private set; }
        public static Vector3 Position { get; private set; }
        public static Vector3 LookAt { get; private set; }
        /// <summary>Camera to drive; null = Camera.main.</summary>
        public static Camera Target;

        public static void Override(Vector3 pos, Vector3 lookAt) { Position = pos; LookAt = lookAt; Active = true; Driver.Ensure(); }
        public static void Clear() { Active = false; }

        /// <summary>Parses "x,y,z:lx,ly,lz" (invariant culture). Returns false on malformed input.</summary>
        public static bool TryParse(string s, out Vector3 pos, out Vector3 look)
        {
            pos = look = default; if (string.IsNullOrEmpty(s)) return false;
            var parts = s.Split(':'); if (parts.Length != 2) return false;
            return TryV(parts[0], out pos) && TryV(parts[1], out look);
        }
        static bool TryV(string s, out Vector3 v)
        {
            v = default; var c = s.Split(','); if (c.Length != 3) return false;
            var f = new float[3];
            for (int i = 0; i < 3; i++) if (!float.TryParse(c[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out f[i])) return false;
            v = new Vector3(f[0], f[1], f[2]); return true;
        }

        /// <summary>Applies the override to a camera. Returns false when inactive or degenerate.</summary>
        public static bool Apply(Camera cam)
        {
            if (!Active || cam == null || (LookAt - Position).sqrMagnitude < 1e-6f) return false;
            cam.ResetProjectionMatrix();
            cam.transform.position = Position; cam.transform.LookAt(LookAt);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FromCommandLine()
        {
            try
            {
                var a = Environment.GetCommandLineArgs();
                for (int i = 0; i < a.Length - 1; i++)
                    if (string.Equals(a[i], "-khCapCam", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryParse(a[i + 1], out var p, out var l)) Override(p, l);
                        else Debug.LogWarning($"[CaptureCamera] bad -khCapCam '{a[i + 1]}', expected x,y,z:lx,ly,lz");
                    }
            }
            catch (Exception) { }
        }

        [DefaultExecutionOrder(32000)]
        sealed class Driver : MonoBehaviour
        {
            static Driver _i;
            public static void Ensure()
            {
                if (_i != null || !Application.isPlaying) return;
                var go = new GameObject("KH_CaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go); _i = go.AddComponent<Driver>();
            }
            void LateUpdate() => Apply(Target != null ? Target : Camera.main);
        }
    }
}
#endif
