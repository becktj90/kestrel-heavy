// Batchmode screenshot entry for CI:
//   Unity -batchmode -projectPath unity -executeMethod Kestrel.EditorTools.CiCapture.Run -khCapOut <dir>
// Opens each build scene, renders Camera.main (or the first camera) at 1179x2556 portrait and writes PNGs.
// Optional: -khCapCam x,y,z:lx,ly,lz positions the camera (same format as runtime CaptureCamera).
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kestrel.EditorTools
{
    public static class CiCapture
    {
        public static void Run()
        {
            string outDir = Arg("-khCapOut") ?? "CaptureOut";
            Directory.CreateDirectory(outDir);
            int failures = 0, written = 0;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.enabled) continue;
                if (!File.Exists(s.path)) { Debug.Log($"[CiCapture] {s.path} not in checkout, skipped"); continue; }
                try
                {
                    EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
                    var cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
                    if (cam == null)
                    {
                        // Shell scenes spawn their camera at runtime; use a temporary one (aim with -khCapCam).
                        cam = new GameObject("CiCaptureCam").AddComponent<Camera>();
                        cam.transform.position = new Vector3(40, 25, -60);
                        cam.transform.LookAt(new Vector3(0, 30, 0));
                        cam.farClipPlane = 100000f;
                    }
                    ApplyCapCam(cam);
                    var rt = new RenderTexture(1179, 2556, 24);
                    cam.targetTexture = rt; cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                    cam.targetTexture = null; RenderTexture.active = null;
                    string file = Path.Combine(outDir, Path.GetFileNameWithoutExtension(s.path) + ".png");
                    File.WriteAllBytes(file, tex.EncodeToPNG());
                    Debug.Log($"[CiCapture] wrote {file}"); written++;
                }
                catch (Exception e) { failures++; Debug.LogError($"[CiCapture] {s.path}: {e}"); }
            }
            EditorApplication.Exit(failures == 0 && written > 0 ? 0 : 1);
        }

        static void ApplyCapCam(Camera cam)
        {
            var v = Arg("-khCapCam"); if (v == null) return;
            var parts = v.Split(':'); if (parts.Length != 2) return;
            cam.transform.position = V(parts[0]);
            cam.transform.LookAt(V(parts[1]));
        }

        static Vector3 V(string s)
        {
            var c = s.Split(',');
            return new Vector3(F(c[0]), F(c[1]), F(c[2]));
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (string.Equals(a[i], name, StringComparison.OrdinalIgnoreCase)) return a[i + 1];
            return null;
        }
    }
}
