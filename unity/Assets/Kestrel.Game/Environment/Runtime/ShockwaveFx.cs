// Shockwave ring and heat shimmer. Pooled quads on the same additive particle shader the explosions use, so no new
// shader or keyword. Rings follow ShockwavePlan (r ~ t^0.4, soft monotone alpha, capped); Low draws nothing, reduce-motion draws nothing.
// The "haze" is a shimmer veil (rising, faint, warm), NOT screen-space refraction: refraction needs a distortion shader
// plus URP's opaque texture, which cannot be verified without Unity. See the PR notes.
// Binds to the existing explosion cues too: Crash, Failure and overpressure engine failures raise a ring and haze.
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Environment
{
    public sealed class ShockwaveFx : MonoBehaviour
    {
        struct Ring { public bool Live; public float Age; public ShockwavePlan Plan; public Vector3 At; public int Birth; }
        struct Haze { public bool Live; public float Age, Life, Alpha; public Vector3 At; public int Birth; }

        public static ShockwaveFx Instance { get; private set; }

        Ring[] rings = System.Array.Empty<Ring>();
        Haze[] hazes = System.Array.Empty<Haze>();
        Transform[] ringT = System.Array.Empty<Transform>(), hazeT = System.Array.Empty<Transform>();
        MeshRenderer[] ringR = System.Array.Empty<MeshRenderer>(), hazeR = System.Array.Empty<MeshRenderer>();
        MaterialPropertyBlock block;
        Material ringMat, hazeMat;
        Mesh quad;
        Camera cam;
        int builtQuality = -1, birth;
        System.Action<ShockwaveEvent> onShock; System.Action<HazeEvent> onHaze; System.Action<CinematicEvent> onBlast;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        public int BuiltQuality => builtQuality;
        public int LiveRings { get { int n = 0; for (int i = 0; i < rings.Length; i++) if (rings[i].Live) n++; return n; } }
        public int LiveHaze { get { int n = 0; for (int i = 0; i < hazes.Length; i++) if (hazes[i].Live) n++; return n; } }
        public int RingCap => rings.Length;
        public MeshRenderer RingRenderer(int i) => i >= 0 && i < ringR.Length ? ringR[i] : null;
        public MeshRenderer HazeRenderer(int i) => i >= 0 && i < hazeR.Length ? hazeR[i] : null;
        /// <summary>Largest ring quad edge (m) currently drawn, for the size-bound tests.</summary>
        public float LargestRingEdge { get { float m = 0f; for (int i = 0; i < rings.Length; i++) if (rings[i].Live && ringT[i] != null) m = Mathf.Max(m, ringT[i].localScale.x); return m; } }
        public int HazeCap => hazes.Length;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("KH_ShockwaveFx") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<ShockwaveFx>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        public void Subscribe()
        {
            Instance = this;
            if (onShock == null) { onShock = OnShock; onHaze = OnHaze; onBlast = OnBlast; }
            Unsubscribe(); Instance = this;
            CinematicEvents.ShockwaveRaised += onShock; CinematicEvents.HazeRaised += onHaze; CinematicEvents.Raised += onBlast;
        }

        public void Unsubscribe()
        {
            if (onShock != null) { CinematicEvents.ShockwaveRaised -= onShock; CinematicEvents.HazeRaised -= onHaze; CinematicEvents.Raised -= onBlast; }
            if (Instance == this) Instance = null;
        }

        void OnDestroy()
        {
            if (ringMat != null) Destroy(ringMat);
            if (hazeMat != null) Destroy(hazeMat);
            if (quad != null) Destroy(quad);
        }

        static int Quality()
        {
            int q = ExplosionFx.QualityOverride >= 0 ? ExplosionFx.QualityOverride : QualitySettings.GetQualityLevel();
            return q < 0 ? 0 : q > 2 ? 2 : q;
        }

        void OnBlast(CinematicEvent e)
        {
            if (e.Kind > CinematicKind.EngineFailure) return;
            if (e.Kind == CinematicKind.EngineFailure && !e.Overpressure) return;
            float energy = e.Kind == CinematicKind.Failure ? 1f : e.Energy01;
            OnShock(new ShockwaveEvent { X = e.X, Y = e.Y, Z = e.Z, Energy01 = energy, Radius = e.Radius, Grounded = e.Grounded, Reduce = e.Reduce });
            OnHaze(new HazeEvent { X = e.X, Y = e.Y + e.Radius, Z = e.Z, Intensity01 = energy, Seconds = 4f, Reduce = e.Reduce });
        }

        void EnsureBuilt()
        {
            int q = Quality();
            if (builtQuality == q) return;
            for (int i = 0; i < ringT.Length; i++) if (ringT[i] != null) Destroy(ringT[i].gameObject);
            for (int i = 0; i < hazeT.Length; i++) if (hazeT[i] != null) Destroy(hazeT[i].gameObject);
            if (quad == null) quad = BuildQuad();
            if (ringMat == null) ringMat = MakeMaterial(RingTexture(), "ShockwaveRing");
            if (hazeMat == null) hazeMat = MakeMaterial(BlobTexture(), "HeatShimmer");
            if (block == null) block = new MaterialPropertyBlock();
            rings = new Ring[ShockwaveBudgets.Rings(q)]; ringT = new Transform[rings.Length]; ringR = new MeshRenderer[rings.Length];
            hazes = new Haze[ShockwaveBudgets.HazeCards(q)]; hazeT = new Transform[hazes.Length]; hazeR = new MeshRenderer[hazes.Length];
            for (int i = 0; i < rings.Length; i++) Make(out ringT[i], out ringR[i], "Ring", ringMat);
            for (int i = 0; i < hazes.Length; i++) Make(out hazeT[i], out hazeR[i], "Shimmer", hazeMat);
            builtQuality = q;
        }

        void Make(out Transform t, out MeshRenderer r, string name, Material mat)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            r = go.AddComponent<MeshRenderer>();
            FxMaterials.Apply(r, mat);   // no safe material: the quad is switched off, never drawn with the error (magenta) material
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            t = go.transform; go.SetActive(false);
        }

        void OnShock(ShockwaveEvent e)
        {
            EnsureBuilt();
            if (rings.Length == 0) return;
            var tod = TimeOfDayRig.Current;
            var plan = ShockwavePlan.Build(e, builtQuality, tod);
            if (!plan.Draw) return;
            int slot = 0; int oldest = int.MaxValue;
            for (int i = 0; i < rings.Length; i++)
            {
                if (!rings[i].Live) { slot = i; oldest = -1; break; }
                if (rings[i].Birth < oldest) { oldest = rings[i].Birth; slot = i; }
            }
            rings[slot] = new Ring { Live = true, Age = 0f, Plan = plan, At = new Vector3(e.X, e.Y, e.Z), Birth = ++birth };
            ringT[slot].gameObject.SetActive(true);
            enabled = true;
        }

        void OnHaze(HazeEvent e)
        {
            EnsureBuilt();
            if (hazes.Length == 0) return;
            float a = ShockwavePlan.HazeAlpha(e, builtQuality, TimeOfDayRig.Current);
            if (a <= 0f) return;
            int slot = 0; int oldest = int.MaxValue;
            for (int i = 0; i < hazes.Length; i++)
            {
                if (!hazes[i].Live) { slot = i; oldest = -1; break; }
                if (hazes[i].Birth < oldest) { oldest = hazes[i].Birth; slot = i; }
            }
            hazes[slot] = new Haze { Live = true, Age = 0f, Life = Mathf.Max(0.5f, e.Seconds), Alpha = a, At = new Vector3(e.X, e.Y, e.Z), Birth = ++birth };
            hazeT[slot].gameObject.SetActive(true);
            enabled = true;
        }

        void LateUpdate() => Tick(Time.deltaTime);

        /// <summary>Advance and pose. Public so EditMode tests can drive it.</summary>
        public void Tick(float dt)
        {
            if (cam == null) cam = Camera.main;
            bool any = false;
            for (int i = 0; i < rings.Length; i++)
            {
                if (!rings[i].Live) continue;
                rings[i].Age += dt;
                var p = rings[i].Plan; float t = rings[i].Age;
                if (t >= p.Duration) { rings[i].Live = false; ringT[i].gameObject.SetActive(false); continue; }
                any = true;
                float r = p.RadiusAt(t);
                var tr = ringT[i];
                tr.position = rings[i].At;
                tr.localScale = new Vector3(r * 2f, r * 2f, 1f);
                if (p.Grounded) tr.rotation = Quaternion.Euler(90f, 0f, 0f);
                else if (cam != null) tr.rotation = cam.transform.rotation;
                float a = p.AlphaAt(t);
                block.SetColor(ColorId, new Color(p.Color.R, p.Color.G, p.Color.B, a));
                ringR[i].SetPropertyBlock(block);
            }
            for (int i = 0; i < hazes.Length; i++)
            {
                if (!hazes[i].Live) continue;
                hazes[i].Age += dt;
                float t = hazes[i].Age, life = hazes[i].Life;
                if (t >= life) { hazes[i].Live = false; hazeT[i].gameObject.SetActive(false); continue; }
                any = true;
                float fadeIn = Mathf.Min(1f, t / 0.4f), fadeOut = Mathf.Clamp01((life - t) / 0.8f);
                var tr = hazeT[i];
                tr.position = hazes[i].At + new Vector3(0f, 1.2f * t, 0f);
                float size = 6f + 2.5f * t;
                tr.localScale = new Vector3(size, size * 1.4f, 1f);
                if (cam != null) tr.rotation = cam.transform.rotation;
                block.SetColor(ColorId, new Color(1f, 0.82f, 0.62f, hazes[i].Alpha * fadeIn * fadeOut));
                hazeR[i].SetPropertyBlock(block);
            }
            if (!any) enabled = false;
        }

        static Mesh BuildQuad()
        {
            var m = new Mesh { name = "ShockQuad", hideFlags = HideFlags.HideAndDontSave };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };   // double-sided
            m.RecalculateBounds();
            return m;
        }

        internal static Material MakeMaterial(Texture2D tex, string name) => FxMaterials.Particle(tex, name, true, true);

        static Texture2D ringTex, blobTex;

        static Texture2D RingTexture()
        {
            if (ringTex != null) return ringTex;
            const int n = 128;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "ShockRing", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);                 // 0 centre .. 1 edge of the quad
                    float edge = Mathf.Exp(-Mathf.Pow((d - 0.88f) / 0.07f, 2f));   // thin soft band near the rim, brighter at the leading edge
                    float tail = d < 0.88f ? Mathf.Exp(-(0.88f - d) * 9f) * 0.25f : 0f;
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt((edge + tail) * 255f), 0, 255);
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            t.SetPixels32(px); t.Apply(false, true);
            ringTex = t; return t;
        }

        static Texture2D BlobTexture()
        {
            if (blobTex != null) return blobTex;
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "ShimmerBlob", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d2 = dx * dx + dy * dy;
                    float a = d2 >= 1f ? 0f : Mathf.Exp(-d2 * 2.4f) * (1f - d2);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            blobTex = t; return t;
        }
    }
}
