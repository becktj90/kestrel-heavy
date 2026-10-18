// Ambient gulls in the mark the web game used: a dark M wing inside a thin amber ring.
// Pad birds flush at ignition. Sky birds stay, then leave before they become specks.
// The 3D skein in Site is a different flock and is left alone.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Environment
{
    public sealed class SkyBirdView : MonoBehaviour
    {
        public static SkyBirdView Instance { get; private set; }

        Transform anchor, holder;
        Mesh mesh;
        MeshRenderer renderer;
        Material mat;
        Texture2D mark;
        readonly SkyBirdPose[] poses = new SkyBirdPose[SkyBirds.Count];
        readonly List<Vector3> verts = new List<Vector3>(SkyBirds.Count * 4);
        readonly List<Vector2> uvs = new List<Vector2>(SkyBirds.Count * 4);
        readonly List<Color> colors = new List<Color>(SkyBirds.Count * 4);
        readonly List<int> tris = new List<int>(SkyBirds.Count * 6);
        bool meshReady;

        public int QuadCount => SkyBirds.Count;
        public int Shown { get; private set; }

        public int LightCount
        {
            get
            {
                if (holder == null) return 0;
                var lights = holder.GetComponentsInChildren<Light>(true);
                return lights == null ? 0 : lights.Length;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("KH_SkyBirds") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<SkyBirdView>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; }
        void OnDestroy() { if (mat != null) Destroy(mat); if (mark != null) Destroy(mark); if (mesh != null) Destroy(mesh); }

        void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            var found = ResolveAnchor();
            if (found == null)
            {
                if (holder != null) Destroy(holder.gameObject);
                holder = null;
                renderer = null;
                anchor = null;
                meshReady = false;
                Shown = 0;
                return;
            }
            if (holder == null || holder.parent != found) Attach(found);
            Pose();
        }

        Transform ResolveAnchor()
        {
            if (anchor != null) return anchor;
            var go = GameObject.Find(PadVfxState.AnchorName);
            anchor = go != null ? go.transform : null;
            return anchor;
        }

        void Attach(Transform found)
        {
            if (holder != null) Destroy(holder.gameObject);
            if (mesh != null) Destroy(mesh);
            anchor = found;
            var go = new GameObject("KH_SkyBirdMesh");
            go.transform.SetParent(found, false);
            holder = go.transform;
            mesh = new Mesh { name = "SkyBirds" };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (mat == null) mat = MakeMaterial();
            renderer.sharedMaterial = mat;
            tris.Clear();
            for (int i = 0; i < SkyBirds.Count; i++)
            {
                int b = i * 4;
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }
            meshReady = false;
        }

        void Pose()
        {
            if (holder == null || mesh == null) return;
            int q = ExplosionFx.QualityOverride >= 0 ? ExplosionFx.QualityOverride : QualitySettings.GetQualityLevel();
            if (q < 0) q = 0; else if (q > 2) q = 2;
            float flap = PlayerPrefs.GetInt(PadIgnitionFx.ReduceMotionPref, 0) != 0 ? 0f : 1f;
            float time = Time.time;
            SkyBirds.Evaluate(time, PadVfxState.Current.Height, flap, q, poses);

            Camera cam = Camera.main;
            Vector3 camLocal = cam != null ? anchor.InverseTransformPoint(cam.transform.position) : new Vector3(0f, 40f, -80f);
            verts.Clear(); uvs.Clear(); colors.Clear();
            int shown = 0;
            for (int i = 0; i < SkyBirds.Count; i++)
            {
                var p = poses[i];
                float presence = p.Presence < 0f ? 0f : p.Presence > 1f ? 1f : p.Presence;
                if (presence > 0.02f) shown++;
                var at = new Vector3(p.X, p.Y, p.Z);
                Vector3 to = camLocal - at;
                if (to.sqrMagnitude < 0.01f) to = Vector3.back;
                to.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, to);
                if (right.sqrMagnitude < 1e-4f) right = Vector3.right;
                right.Normalize();
                Vector3 up = Vector3.Cross(to, right);
                float half = p.Span * 0.5f * presence;
                verts.Add(at - right * half - up * half);
                verts.Add(at + right * half - up * half);
                verts.Add(at + right * half + up * half);
                verts.Add(at - right * half + up * half);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
                var tint = new Color(1f, 1f, 1f, presence);
                colors.Add(tint); colors.Add(tint); colors.Add(tint); colors.Add(tint);
            }
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            if (!meshReady)
            {
                mesh.SetTriangles(tris, 0);
                meshReady = true;
            }
            mesh.RecalculateBounds();
            Shown = shown;
        }

        Material MakeMaterial()
        {
            if (mark == null) mark = BirdMark();
            var shader = Resources.Load<Shader>("Shaders/KH_BirdMark");
            if (shader == null) shader = Shader.Find("Kestrel/BirdMark");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var m = new Material(shader) { name = "SkyBirdMark" };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", mark);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            m.mainTexture = mark;
            return m;
        }

        static Texture2D BirdMark()
        {
            var raw = new byte[SurfaceGrain.Bytes];
            SurfaceGrain.Fill(SurfaceKind.Bird, raw);
            var tex = new Texture2D(SurfaceGrain.Size, SurfaceGrain.Size, TextureFormat.RGBA32, false)
            {
                name = "SkyBirdMark",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var px = new Color32[SurfaceGrain.Size * SurfaceGrain.Size];
            for (int i = 0; i < px.Length; i++)
            {
                int o = i * 4;
                px[i] = new Color32(raw[o], raw[o + 1], raw[o + 2], raw[o + 3]);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
