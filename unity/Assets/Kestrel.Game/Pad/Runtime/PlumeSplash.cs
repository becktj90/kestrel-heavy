// Plume light on the pad: an additive emissive splash on the deck (all tiers) plus, on High only, one point light.
// Driven by real throttle and height above the deck; scales down with altitude, vanishes by day only partially
// (sunlight washes it out), no per-frame allocations (matrix + material colour + light fields).
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Pad
{
    public sealed class PlumeSplash
    {
        public static readonly Color Flame = new Color(1f, 0.55f, 0.18f);
        public const float MaxLightIntensity = 9000f;

        public float Throttle { get; private set; }
        public float Height { get; private set; }
        public Light Light { get; private set; }
        public float Level => Strength(Throttle, Height, _deckRadius);

        readonly float _deckRadius; readonly Mesh _quad; readonly Material _mat; readonly Texture2D _tex;

        /// <summary>0..1 splash strength: linear in throttle, inverse-square fade with height above the deck (half at ~1.5 deck radii).</summary>
        public static float Strength(float throttle, float heightM, float deckRadius)
        {
            float t = Mathf.Clamp01(throttle); if (t <= 0f) return 0f;
            float k = Mathf.Max(1f, deckRadius) * 1.5f, u = Mathf.Max(0f, heightM) / k;
            return t / (1f + u * u);
        }

        /// <summary>Daytime keeps 35 % so the splash still reads in sun; night is full.</summary>
        public static float NightScale(float night) => 0.35f + 0.65f * Mathf.Clamp01(night);

        public PlumeSplash(Transform parent, int quality, float deckRadius)
        {
            _deckRadius = Mathf.Max(4f, deckRadius);
            _tex = PadGlow.RadialTexture(32);
            _mat = PadGlow.MakeAdditive("Pad_PlumeSplash", _tex);
            _quad = new Mesh { name = "Pad_PlumeSplashQuad" };
            _quad.vertices = new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1) };
            _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 }; _quad.RecalculateBounds();
            if (KitBudgets_PlumeLights(quality) > 0)
            {
                var go = new GameObject("plume.light"); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0, 4f, 0);
                Light = go.AddComponent<Light>(); Light.type = LightType.Point; Light.color = Flame; Light.range = _deckRadius * 3.5f;
                Light.shadows = LightShadows.None; Light.renderMode = LightRenderMode.ForcePixel; Light.intensity = 0f; Light.enabled = false;
            }
        }

        static int KitBudgets_PlumeLights(int q) => Kestrel.Kit.KitBudgets.PadPlumeLights(q);

        public void SetState(float throttle, float heightAboveDeck) { Throttle = Mathf.Clamp01(throttle); Height = Mathf.Max(0f, heightAboveDeck); }

        /// <summary>Updates the point light and draws the splash (1 draw call when visible).</summary>
        public void Update(Matrix4x4 padToWorld, float night, int layer)
        {
            float s = Level, vis = s * NightScale(night);
            if (Light != null)
            {
                bool on = vis > 0.02f; if (Light.enabled != on) Light.enabled = on;
                if (on) Light.intensity = MaxLightIntensity * vis * (0.55f + 0.45f * Mathf.Clamp01(night));
            }
            if (s <= 0.01f || _mat == null) return;
            float radius = _deckRadius * (0.45f + 0.55f * s);
            _mat.SetColor("_BaseColor", new Color(Flame.r, Flame.g, Flame.b, Mathf.Clamp01(0.85f * vis)));
            Graphics.DrawMesh(_quad, padToWorld * Matrix4x4.TRS(new Vector3(0, 0.12f, 0), Quaternion.identity, new Vector3(radius, 1f, radius)), _mat, layer);
        }

        public void Dispose()
        {
            if (Light != null) Object.DestroyImmediate(Light.gameObject);
            if (_quad != null) Object.DestroyImmediate(_quad);
            if (_mat != null) Object.DestroyImmediate(_mat);
            if (_tex != null) Object.DestroyImmediate(_tex);
        }
    }
}
