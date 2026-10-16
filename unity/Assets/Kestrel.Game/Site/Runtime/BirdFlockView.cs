// Draws the gull skein in pad space under the floating-origin world root.
// One mesh, a sky-tint shader (not the terrain grain), so a bird stays a silhouette.
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Site
{
    [DisallowMultipleComponent]
    public sealed class BirdFlockView : MonoBehaviour
    {
        static readonly int SunDirId = Shader.PropertyToID("_SunDir");
        static readonly int SunColorId = Shader.PropertyToID("_SunColor");
        static readonly int FogColorId = Shader.PropertyToID("_FogColor");
        static readonly int FogRangeId = Shader.PropertyToID("_FogRange");

        FlightDriverSource flight;
        Mesh mesh;
        Material mat;
        MeshRenderer renderer;
        Vector3[] verts;
        Vector3[] normals;
        Color[] colors;
        readonly float[] xyz = new float[BirdFlock.VertexCount * 3];
        readonly float[] nrm = new float[BirdFlock.VertexCount * 3];
        readonly BirdPose[] poses = new BirdPose[BirdFlock.Count];
        Light sun;
        Camera cam;

        /// <summary>1 flaps. 0 holds the glide for Reduce Motion.</summary>
        public float FlapScale = 1f;

        /// <summary>While set, ignition and altitude come from the vehicle. Otherwise the skein cruises the pad.</summary>
        public bool TrackFlight;

        public interface FlightDriverSource
        {
            double Downrange { get; }
            double Altitude { get; }
            double OriginX { get; }
            double OriginY { get; }
        }

        public static BirdFlockView Create(Transform world)
        {
            var go = new GameObject("Birds");
            go.transform.SetParent(world, false);
            var view = go.AddComponent<BirdFlockView>();
            view.Build();
            return view;
        }

        public void Bind(FlightDriverSource source) => flight = source;
        public void BindSun(Light sun) => this.sun = sun;

        void Build()
        {
            var shader = Resources.Load<Shader>("Site/KH_Bird");
            if (shader == null) shader = Shader.Find("Kestrel/Bird");
            if (shader == null) return;
            mat = new Material(shader) { name = "Birds" };
            mesh = new Mesh { name = "Birds" };
            verts = new Vector3[BirdFlock.VertexCount];
            normals = new Vector3[BirdFlock.VertexCount];
            colors = new Color[BirdFlock.VertexCount];
            for (int i = 0; i < BirdFlock.Count; i++)
            {
                int v = i * BirdFlock.VertsPerBird;
                for (int k = 0; k < BirdFlock.VertsPerBird; k++)
                {
                    BirdFlock.VertexColor(k, out float r, out float g, out float b);
                    colors[v + k] = new Color(r, g, b, 1f);
                }
            }
            var idx = new int[BirdFlock.IndexCount];
            BirdFlock.Indices(idx);
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.triangles = idx;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(80f, 80f, 80f));
            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        void LateUpdate()
        {
            if (mesh == null || renderer == null) return;
            if (!BirdFlock.Enabled)
            {
                renderer.enabled = false;
                return;
            }
            float rx = 0f, ry = 0f, ox = 0f, oy = 0f;
            if (TrackFlight && flight != null)
            {
                rx = (float)flight.Downrange;
                ry = (float)flight.Altitude;
                ox = (float)flight.OriginX;
                oy = (float)flight.OriginY;
            }
            BirdFlock.Evaluate(Time.time, rx, ry, FlapScale, poses);
            float shown = 0f;
            for (int i = 0; i < BirdFlock.Count; i++)
                if (poses[i].Presence > shown) shown = poses[i].Presence;
            renderer.enabled = shown > 0.02f;
            if (!renderer.enabled) return;

            for (int i = 0; i < BirdFlock.Count; i++)
                BirdFlock.WriteBird(poses[i], i * BirdFlock.VertsPerBird, xyz, nrm);
            if (cam == null) cam = Camera.main;
            for (int i = 0; i < BirdFlock.Count; i++)
            {
                var center = new Vector3(poses[i].X - ox, poses[i].Y - oy, poses[i].Z);
                float appear = 1f;
                if (poses[i].Presence >= 0.02f && cam != null)
                {
                    float dist = Vector3.Distance(cam.transform.position, transform.TransformPoint(center));
                    appear = BirdFlock.AppearScale(dist);
                }
                int v = i * BirdFlock.VertsPerBird;
                for (int k = 0; k < BirdFlock.VertsPerBird; k++)
                {
                    int o = (v + k) * 3;
                    var p = new Vector3(xyz[o] - ox, xyz[o + 1] - oy, xyz[o + 2]);
                    if (appear != 1f) p = center + (p - center) * appear;
                    verts[v + k] = p;
                    normals[v + k] = new Vector3(nrm[o], nrm[o + 1], nrm[o + 2]);
                    var c = colors[v + k];
                    c.a = poses[i].Presence;
                    colors[v + k] = c;
                }
            }
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            if (sun == null || mat == null) return;
            mat.SetVector(SunDirId, -sun.transform.forward);
            mat.SetColor(SunColorId, sun.color * sun.intensity);
            var sky = RenderSettings.fogColor;
            if (sky.maxColorComponent < 0.2f) sky = new Color(0.55f, 0.68f, 0.80f, 1f);
            mat.SetColor(FogColorId, sky);
            mat.SetVector(FogRangeId, new Vector4(RenderSettings.fogStartDistance, RenderSettings.fogEndDistance, RenderSettings.fog ? 1f : 0f, 0f));
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (mat != null) Destroy(mat);
        }
    }
}
