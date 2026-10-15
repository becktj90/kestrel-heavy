// Draws the gull flock in pad space under the floating-origin world root.
// One mesh, the same terrain shader as the coast, so it does not add a shader variant.
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
        Vector3[] verts;
        Vector3[] normals;
        readonly float[] xyz = new float[BirdFlock.VertexCount * 3];
        readonly float[] nrm = new float[BirdFlock.VertexCount * 3];
        readonly BirdPose[] poses = new BirdPose[BirdFlock.Count];
        Light sun;

        /// <summary>1 flaps. 0 holds the wings level for Reduce Motion.</summary>
        public float FlapScale = 1f;

        /// <summary>While set, the flock rides the vehicle through the early climb. Otherwise it circles the pad.</summary>
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
            var shader = Resources.Load<Shader>("Site/KH_Terrain");
            if (shader == null) shader = Shader.Find("Kestrel/Terrain");
            if (shader == null) return;
            mat = new Material(shader) { name = "Birds" };
            mesh = new Mesh { name = "Birds" };
            verts = new Vector3[BirdFlock.VertexCount];
            normals = new Vector3[BirdFlock.VertexCount];
            var colors = new Color[BirdFlock.VertexCount];
            var gull = new Color(Coastline.Gull[0], Coastline.Gull[1], Coastline.Gull[2], 1f);
            var tip = new Color(0.72f, 0.75f, 0.80f, 1f);
            for (int i = 0; i < BirdFlock.Count; i++)
            {
                int v = i * BirdFlock.VertsPerBird;
                for (int k = 0; k < BirdFlock.VertsPerBird; k++)
                    colors[v + k] = (k % 4) >= 2 ? tip : gull;
            }
            var idx = new int[BirdFlock.Count * 24];
            BirdFlock.Indices(idx);
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.triangles = idx;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(4000f, 4000f, 4000f));
            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        void LateUpdate()
        {
            if (mesh == null) return;
            float rx = 0f, ry = 0f, ox = 0f, oy = 0f;
            if (TrackFlight && flight != null)
            {
                rx = (float)flight.Downrange;
                ry = (float)flight.Altitude;
                ox = (float)flight.OriginX;
                oy = (float)flight.OriginY;
            }
            BirdFlock.Evaluate(Time.time, rx, ry, FlapScale, poses);
            for (int i = 0; i < BirdFlock.Count; i++)
                BirdFlock.WriteBird(poses[i], i * BirdFlock.VertsPerBird, xyz, nrm);
            for (int i = 0; i < verts.Length; i++)
            {
                int o = i * 3;
                verts[i] = new Vector3(xyz[o] - ox, xyz[o + 1] - oy, xyz[o + 2]);
                normals[i] = new Vector3(nrm[o], nrm[o + 1], nrm[o + 2]);
            }
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.bounds = new Bounds(new Vector3(rx - ox, ry * 0.5f - oy, 0f), new Vector3(800f, 800f, 800f));
            if (sun == null || mat == null) return;
            mat.SetVector(SunDirId, -sun.transform.forward);
            mat.SetColor(SunColorId, sun.color * sun.intensity);
            mat.SetColor(FogColorId, RenderSettings.fogColor);
            mat.SetVector(FogRangeId, new Vector4(RenderSettings.fogStartDistance, RenderSettings.fogEndDistance, RenderSettings.fog ? 1f : 0f, 0f));
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (mat != null) Destroy(mat);
        }
    }
}
