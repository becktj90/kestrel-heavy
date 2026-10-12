// D5 night look: additive lamp halos (camera-facing flares, one combined mesh rebuilt per frame, 1 draw call)
// and fake volumetric cones on floods/poles (static combined mesh, vertex alpha fades apex→base, 1 draw call).
// Both scale with Night and vanish by day. Unlit additive (URP Particles/Unlit), no extra realtime lights.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Pad
{
    public sealed class PadGlow
    {
        public const string ShaderName = "Universal Render Pipeline/Particles/Unlit";
        public const int ConeSides = 12;
        /// <summary>Peak alpha of a cone at its apex (full night). Kept low so beams read as haze, not solids.</summary>
        public const float ConeAlpha = 0.10f, FlareAlpha = 0.9f;

        readonly List<LightSpec> _halo = new List<LightSpec>();
        readonly Mesh _flares, _cones; readonly Material _flareMat, _coneMat; readonly Texture2D _tex;
        readonly Vector3[] _fv; readonly Color[] _fc; readonly List<int> _blinkIdx = new List<int>();
        public int FlareCount => _halo.Count;
        public int ConeCount { get; private set; }
        public int ConeTriangles { get; private set; }

        public PadGlow(IReadOnlyList<LightSpec> plan, int quality)
        {
            foreach (var s in plan) if (s.Halo > 0) { if (s.BlinkHz > 0) _blinkIdx.Add(_halo.Count); _halo.Add(s); }
            _tex = RadialTexture(64);
            _flareMat = Additive("Pad_Flare", _tex); _coneMat = Additive("Pad_Cone", null);
            _flares = new Mesh { name = "Pad_Flares" }; _flares.MarkDynamic();
            int n = _halo.Count; _fv = new Vector3[n * 4]; _fc = new Color[n * 4];
            var uv = new Vector2[n * 4]; var tri = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(1, 0); uv[i * 4 + 2] = new Vector2(1, 1); uv[i * 4 + 3] = new Vector2(0, 1);
                int v = i * 4, t = i * 6; tri[t] = v; tri[t + 1] = v + 2; tri[t + 2] = v + 1; tri[t + 3] = v; tri[t + 4] = v + 3; tri[t + 5] = v + 2;
            }
            _flares.vertices = _fv; _flares.uv = uv; _flares.colors = _fc; _flares.triangles = tri;
            _cones = BuildCones(plan, quality, out int cc); ConeCount = cc; ConeTriangles = cc * ConeSides * 2;
        }

        static Material Additive(string name, Texture2D tex)
        {
            var sh = Shader.Find(ShaderName); if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return null;
            var m = new Material(sh) { name = name, renderQueue = (int)RenderQueue.Transparent + 10 };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);                 // transparent, additive
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.EnableKeyword("_BLENDMODE_ADD");
            m.SetOverrideTag("RenderType", "Transparent");
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
            m.SetColor("_BaseColor", Color.white);
            return m;
        }

        /// <summary>Soft radial falloff with a hot core: the flare reads as a lamp glowing in damp night air.</summary>
        public static Texture2D RadialTexture(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Pad_FlareTex" };
            var px = new Color[size * size]; float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - r); a = a * a * 0.55f + Mathf.Pow(Mathf.Clamp01(1f - r * 3f), 2f) * 0.45f;
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px); t.Apply(false); return t;
        }

        static Mesh BuildCones(IReadOnlyList<LightSpec> plan, int q, out int count)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var tri = new List<int>(); count = 0;
            foreach (var s in plan)
            {
                if (!PadLighting.HasCone(s.Kind, q) || s.SpotAngle <= 0) continue;
                var apex = new Vector3(s.Position.X, s.Position.Y, s.Position.Z); var dir = new Vector3(s.Aim.X, s.Aim.Y, s.Aim.Z).normalized;
                float len = Mathf.Max(4f, s.Throw * 0.9f), rad = len * Mathf.Tan(s.SpotAngle * 0.5f * Mathf.Deg2Rad) * 0.8f;
                if (apex.y + dir.y * len < 0.2f && dir.y < -1e-3f) len = (apex.y - 0.2f) / -dir.y;   // stop at the ground
                var rot = Quaternion.LookRotation(dir); var col = new Color(s.Color.X, s.Color.Y, s.Color.Z, 1f);
                int baseI = v.Count; v.Add(apex); c.Add(col);
                for (int i = 0; i < ConeSides; i++)
                {
                    float a = i * Mathf.PI * 2f / ConeSides;
                    v.Add(apex + rot * new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, len)); c.Add(new Color(col.r, col.g, col.b, 0f));
                    int i0 = baseI + 1 + i, i1 = baseI + 1 + (i + 1) % ConeSides; tri.Add(baseI); tri.Add(i0); tri.Add(i1);
                }
                count++;
            }
            var m = new Mesh { name = "Pad_Cones" }; m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(tri, 0); m.RecalculateBounds(); return m;
        }

        /// <summary>Rebuild flare quads facing the camera; draw flares + cones scaled by night (nothing by day).</summary>
        public void Draw(Matrix4x4 padToWorld, Camera cam, float night, double t, int layer)
        {
            if (night <= 0.02f || cam == null) return;
            var right = cam.transform.right; var up = cam.transform.up; var camPos = cam.transform.position;
            for (int i = 0; i < _halo.Count; i++)
            {
                var s = _halo[i];
                var p = padToWorld.MultiplyPoint3x4(new Vector3(s.Position.X, s.Position.Y, s.Position.Z));
                // keep a minimum on-screen size far away so distant lamps still read as glows, not single pixels
                float d = Vector3.Distance(p, camPos), size = Mathf.Max(s.Halo, d * 0.012f) * 0.5f;
                float a = FlareAlpha * night * (s.BlinkHz > 0 ? PadLighting.Blink(s.BlinkHz, t) : 1f);
                var col = new Color(s.Color.X, s.Color.Y, s.Color.Z, a);
                Vector3 rr = right * size, uu = up * size; int v = i * 4;
                _fv[v] = p - rr - uu; _fv[v + 1] = p + rr - uu; _fv[v + 2] = p + rr + uu; _fv[v + 3] = p - rr + uu;
                _fc[v] = _fc[v + 1] = _fc[v + 2] = _fc[v + 3] = col;
            }
            if (_halo.Count > 0 && _flareMat != null)
            {
                _flares.vertices = _fv; _flares.colors = _fc; _flares.RecalculateBounds();
                Graphics.DrawMesh(_flares, Matrix4x4.identity, _flareMat, layer);
            }
            if (ConeCount > 0 && _coneMat != null)
            {
                _coneMat.SetColor("_BaseColor", new Color(1, 1, 1, ConeAlpha * night));
                Graphics.DrawMesh(_cones, padToWorld, _coneMat, layer);
            }
        }

        public void Dispose()
        {
            if (_flares != null) Object.DestroyImmediate(_flares); if (_cones != null) Object.DestroyImmediate(_cones);
            if (_flareMat != null) Object.DestroyImmediate(_flareMat); if (_coneMat != null) Object.DestroyImmediate(_coneMat);
            if (_tex != null) Object.DestroyImmediate(_tex);
        }
    }
}
