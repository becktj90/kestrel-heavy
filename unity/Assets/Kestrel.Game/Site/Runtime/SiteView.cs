// Builds the Pier 7 site under the floating-origin world root. Sea level is this transform's origin
// (Slice anchors it at PadGround.GroundY). Land and water are one mesh each.
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Site
{
    [DisallowMultipleComponent]
    public sealed class SiteView : MonoBehaviour
    {
        public const float FarClip = SiteHorizon.FarClip;
        static readonly int SunDirId = Shader.PropertyToID("_SunDir");
        static readonly int SunColorId = Shader.PropertyToID("_SunColor");
        static readonly Color Haze = new Color(0.66f, 0.76f, 0.86f, 1f); // PadGround.HazeColor

        Material _land, _water;
        Light _sun;
        CoastalSite _site;

        public CoastalSite Site => _site;

        public static SiteView Create(Transform world, int seed, int lod)
        {
            var go = new GameObject("Site");
            go.transform.SetParent(world, false);
            var view = go.AddComponent<SiteView>();
            view.Build(seed, lod);
            return view;
        }

        public static int LodForQuality(int qualityLevel)
        {
            if (qualityLevel <= 0) return 2;
            if (qualityLevel == 1) return 1;
            return 0;
        }

        public void BindSun(Light sun) => _sun = sun;

        void Build(int seed, int lod)
        {
            _site = SiteGenerator.Generate(seed);
            _land = Make("Site/KH_Terrain", "Kestrel/Terrain");
            _water = Make("Site/KH_Water", "Kestrel/Water");
            AddMesh("Terrain", _site.Land(lod), _land);
            AddMesh("Water", _site.Water(lod), _water);
            ApplyAerial(8f);
        }

        void AddMesh(string name, SiteMesh src, Material mat)
        {
            if (mat == null || src.VertexCount == 0) return;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var mesh = new Mesh { name = name };
            if (src.VertexCount > 65535) mesh.indexFormat = IndexFormat.UInt32;
            var v = new Vector3[src.VertexCount];
            var n = new Vector3[src.VertexCount];
            var c = new Color[src.VertexCount];
            for (int i = 0; i < src.VertexCount; i++)
            {
                v[i] = new Vector3(src.Positions[i * 3], src.Positions[i * 3 + 1], src.Positions[i * 3 + 2]);
                n[i] = new Vector3(src.Normals[i * 3], src.Normals[i * 3 + 1], src.Normals[i * 3 + 2]);
                int k = i * 4;
                c[i] = new Color(src.Colors[k], src.Colors[k + 1], src.Colors[k + 2], src.Colors[k + 3]);
            }
            mesh.vertices = v; mesh.normals = n; mesh.colors = c; mesh.triangles = src.Indices;
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        static Material Make(string resource, string shaderName)
        {
            var shader = Resources.Load<Shader>(resource);
            if (shader == null) shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[Kestrel] missing shader " + shaderName);
                return null;
            }
            return new Material(shader);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) ApplyAerial(cam.transform.position.y - transform.position.y);
            PushSun();
        }

        public void ApplyAerial(float cameraAltitude)
        {
            float h = cameraAltitude > 0f ? cameraAltitude : 0f;
            float horizon = SiteHorizon.Distance(h);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Haze;
            float start = Mathf.Max(6000f, horizon * 0.42f);
            float end = Mathf.Max(start + 14000f, horizon * 1.02f);
            end = Mathf.Min(end, FarClip * 0.98f);
            if (end < start + 2000f) start = Mathf.Max(50f, end - 2000f);
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = end;
        }

        void PushSun()
        {
            Vector3 to = new Vector3(0.35f, 0.76f, -0.55f).normalized;
            Color col = Color.white * 1.3f;
            if (_sun != null) { to = -_sun.transform.forward; col = _sun.color * _sun.intensity; }
            var sun = new Vector4(to.x, to.y, to.z, 0f);
            if (_land != null) { _land.SetVector(SunDirId, sun); _land.SetColor(SunColorId, col); }
            if (_water != null) { _water.SetVector(SunDirId, sun); _water.SetColor(SunColorId, col); }
        }
    }
}
