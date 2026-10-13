// PlayMode shots of each §3 failure. Writes CaptureOut/failure-<kind>.png for the PR.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Flight;
using Kestrel.Game.Failure;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Game.Failure.Capture
{
    public class FailureCaptureTests
    {
        const int W = 750, H = 1334;

        static readonly string[] Kinds =
        {
            "structural-maxq", "overg", "tumble", "engine-overpressure", "bad-mixture",
            "thermal", "cryo-material", "prop-depletion", "separation-collision", "leg-collapse", "engine-out",
        };

        [UnityTest]
        public IEnumerator EachFailureIsReadable()
        {
            var dir = OutDir();
            foreach (var kind in Kinds)
            {
                var path = Path.Combine(dir, "failure-" + kind + ".png");
                var shot = Shot(kind, path);
                while (shot.MoveNext()) yield return shot.Current;
                Assert.That(File.Exists(path), Is.True, kind);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(1000), kind);
            }
        }

        static IEnumerator Shot(string kind, string path)
        {
            var root = new GameObject("FailureShot");
            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.62f, 0.82f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            cam.transform.position = new Vector3(16f, 14f, -26f);
            cam.transform.LookAt(new Vector3(0f, 10f, 0f));
            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(root.transform, false);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            Paint(ground, new Color(0.35f, 0.32f, 0.26f));
            Prop(root.transform, new Vector3(-6f, 1f, 4f), new Color(0.85f, 0.25f, 0.2f));
            Prop(root.transform, new Vector3(6f, 2f, 3f), new Color(0.95f, 0.75f, 0.15f));
            Prop(root.transform, new Vector3(0f, 1f, 8f), new Color(0.2f, 0.55f, 0.35f));

            var rocket = new GameObject("Rocket");
            rocket.transform.SetParent(root.transform, false);
            var mesh = RocketMesh();
            rocket.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = rocket.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Lit(new Color(0.93f, 0.94f, 0.96f));
            var host = rocket.AddComponent<FailureHost>();
            UIDocument doc = null;
            var panel = Resources.Load<PanelSettings>("UI/KH_PanelSettings");
            if (panel != null)
            {
                var uiGo = new GameObject("Report");
                uiGo.transform.SetParent(root.transform, false);
                doc = uiGo.AddComponent<UIDocument>();
                doc.panelSettings = panel;
            }
            yield return null;
            var exits = new[] { new Vector3(-0.6f, 0.4f, 0f), new Vector3(0.6f, 0.4f, 0f), new Vector3(0f, 0.4f, -0.5f) };
            var cue = new FailureCue
            {
                QPa = (float)FailureModel.TumbleQPa,
                AllowableN = 200000f,
                Joints = new[]
                {
                    new FailureJoint { Y = 8f, Radius = 1.7f, Source = "interstage" },
                    new FailureJoint { Y = 16f, Radius = 1.3f, Source = "fairing" },
                },
                EngineCount = exits.Length,
                EngineIndex = 1,
                Radius = 1.6f,
                AftY = 2f,
                Sink = 14f,
                VLeg = 6f,
            };
            string cause = kind == "thermal" ? "nose" : kind == "structural-maxq" ? "q" : "";
            var ev = new FailureEvent
            {
                t = 62,
                kind = kind,
                cause = cause,
                engineIndex = 1,
                value = kind == "leg-collapse" ? 14 : 41000,
                limit = kind == "leg-collapse" ? 6 : 33000,
                kid = "Your rocket failed at T+62 s.",
                engineer = kind + " at T+62.00 s.",
            };
            if (doc != null && doc.rootVisualElement != null) host.Bind(null, doc.rootVisualElement, null);
            host.Play(ev, cue, exits, false);
            host.Seek(0.7f);
            var ps = rocket.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Simulate(0.35f, true, false);
            for (int i = 0; i < 5; i++) yield return null;

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            if (doc != null && doc.panelSettings != null)
            {
                var old = doc.panelSettings.targetTexture;
                var oldClear = doc.panelSettings.clearColor;
                doc.panelSettings.targetTexture = rt;
                doc.panelSettings.clearColor = false;
                for (int i = 0; i < 3; i++) yield return null;
                doc.panelSettings.targetTexture = old;
                doc.panelSettings.clearColor = oldClear;
            }

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            var px = tex.GetPixels32();
            int distinct = px.Where((_, i) => i % 97 == 0).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count();
            Debug.Log("[Capture] " + kind + ": " + path + " colours=" + distinct);
            Assert.That(distinct, Is.GreaterThan(8), kind + " capture is flat");
            rt.Release();
            UnityEngine.Object.Destroy(tex);
            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        static Mesh RocketMesh()
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Box(verts, tris, new Vector3(0f, 6f, 0f), new Vector3(1.6f, 6f, 1.6f));
            Box(verts, tris, new Vector3(0f, 14f, 0f), new Vector3(1.3f, 3f, 1.3f));
            Box(verts, tris, new Vector3(0f, 18f, 0f), new Vector3(0.7f, 2f, 0.7f));
            var mesh = new Mesh { name = "CaptureRocket" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Box(List<Vector3> verts, List<int> tris, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            int o = verts.Count;
            verts.Add(center + new Vector3(-h.x, -h.y, -h.z));
            verts.Add(center + new Vector3(h.x, -h.y, -h.z));
            verts.Add(center + new Vector3(h.x, h.y, -h.z));
            verts.Add(center + new Vector3(-h.x, h.y, -h.z));
            verts.Add(center + new Vector3(-h.x, -h.y, h.z));
            verts.Add(center + new Vector3(h.x, -h.y, h.z));
            verts.Add(center + new Vector3(h.x, h.y, h.z));
            verts.Add(center + new Vector3(-h.x, h.y, h.z));
            int[] face = { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4, 3, 6, 2, 3, 7, 6, 0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5 };
            for (int i = 0; i < face.Length; i++) tris.Add(o + face[i]);
        }

        static void Prop(Transform parent, Vector3 at, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent, false);
            cube.transform.position = at;
            Paint(cube, color);
        }

        static void Paint(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Lit(color);
        }

        static Material Lit(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            return mat;
        }

        static string OutDir()
        {
            var dir = Environment.GetEnvironmentVariable("KH_CAP_OUT");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
