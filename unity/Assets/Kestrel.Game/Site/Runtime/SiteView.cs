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

        Material _land, _water;
        Light _sun;
        CoastalSite _site;
        Mesh _landMesh;
        float[] _landPos;
        int[] _landIdx;

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

        /// <summary>
        /// Drop land triangles that cover a flame-trench opening. Holes are in this transform's local XZ.
        /// Passing none restores the full coast. The trench mesh itself is drawn by the pad.
        /// </summary>
        public void SetGroundOpenings(System.Collections.Generic.IEnumerable<System.Collections.Generic.IList<Vector2>> holes)
        {
            if (_landMesh == null || _landIdx == null) return;
            int[] idx = _landIdx;
            if (holes != null)
            {
                var packed = new System.Collections.Generic.List<float[]>();
                foreach (var ring in holes)
                {
                    if (ring == null || ring.Count < 3) continue;
                    var a = new float[ring.Count * 2];
                    for (int i = 0; i < ring.Count; i++) { a[i * 2] = ring[i].x; a[i * 2 + 1] = ring[i].y; }
                    packed.Add(a);
                }
                if (packed.Count > 0) idx = SiteMesh.WithoutOpenings(_landPos, _landIdx, packed);
            }
            _landMesh.triangles = idx;
            _landMesh.RecalculateBounds();
        }

        void Build(int seed, int lod)
        {
            _site = SiteGenerator.Generate(seed);
            _land = Make("Site/KH_Terrain", "Kestrel/Terrain");
            _water = Make("Site/KH_Water", "Kestrel/Water");
            AddMesh("Terrain", _site.Land(lod), _land);
            AddMesh("Water", _site.Water(lod), _water);
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
            if (name == "Terrain") { _landMesh = mesh; _landPos = src.Positions; _landIdx = src.Indices; }
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

        void LateUpdate() => PushSun();

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
