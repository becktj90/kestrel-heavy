// Layered explosion and fire/smoke: core flash, fireball, smoke column, sparks, embers.
// Listens to Kit.CinematicEvents (Crash, Failure, EngineFailure). Five pooled ParticleSystems and one point light
// are built once per quality tier; an event only calls Emit, so nothing allocates per frame or per event.
// Colour, exposure and the flash light follow TimeOfDayRig.Current; the flash is rate-limited and amplitude-capped.
using Kestrel.Core.Feel;
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Environment
{
    public sealed class ExplosionFx : MonoBehaviour
    {
        public const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        public static ExplosionFx Instance { get; private set; }
        /// <summary>-1 follows QualitySettings (0..2); tests and capture can pin a tier.</summary>
        public static int QualityOverride = -1;

        readonly ParticleSystem[] systems = new ParticleSystem[ExplosionBudgets.LayerCount];
        readonly Material[] mats = new Material[ExplosionBudgets.LayerCount];
        FlashEnvelope envelope;
        Light flashLight;
        float lightPeak, lightRise, lightDecay;
        int builtQuality = -1, noise;
        System.Action<CinematicEvent> handler;
        static Texture2D softTexture;

        public int BuiltQuality => builtQuality;
        public int LayerCap(ExplosionLayer l) => systems[(int)l] != null ? systems[(int)l].main.maxParticles : 0;
        public float LightLevel => envelope.Level * lightPeak;
        public bool LightEnabled => flashLight != null && flashLight.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("KH_ExplosionFx") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<ExplosionFx>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; QualityOverride = -1; softTexture = null; }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        /// <summary>Called from OnEnable; public so EditMode tests, where lifecycle callbacks do not run, can drive it.</summary>
        public void Subscribe()
        {
            FlashGovernor.Reset();
            Instance = this;
            if (handler == null) handler = Handle;
            CinematicEvents.Raised -= handler;
            CinematicEvents.Raised += handler;
        }

        public void Unsubscribe()
        {
            if (handler != null) CinematicEvents.Raised -= handler;
            if (Instance == this) Instance = null;
        }

        void OnDestroy()
        {
            for (int i = 0; i < mats.Length; i++) if (mats[i] != null) Destroy(mats[i]);
        }

        static int CurrentQuality()
        {
            int q = QualityOverride >= 0 ? QualityOverride : QualitySettings.GetQualityLevel();
            return q < 0 ? 0 : q > 2 ? 2 : q;
        }

        /// <summary>Camera the flash is sized against. Null means Camera.main. Set by capture tests, which render through their own camera.</summary>
        public static Camera ViewCamera;
        /// <summary>The plan the last blast used (after the view limit). Tests assert its sizes and alpha.</summary>
        public ExplosionPlan LastPlan { get; private set; }

        void Handle(CinematicEvent e)
        {
            if (e.Kind > CinematicKind.EngineFailure) return;   // separation / debris events belong to DebrisFx
            int q = CurrentQuality();
            if (builtQuality != q) Build(q);
            var tod = TimeOfDayRig.Current;
            float night = tod != null ? tod.NightFactor : 0f;
            bool wantFlash = !e.Reduce && (e.Kind != CinematicKind.EngineFailure || e.Overpressure);
            float grant = wantFlash ? FlashGovernor.Grant(Time.unscaledTime, 1f, ExplosionBudgets.FlashAmplitudeCap(night)) : 0f;
            var plan = ExplosionPlan.Build(e, q, tod, e.Reduce, grant);
            var at = new Vector3(e.X, e.Y, e.Z);
            // Photosensitivity: the flash card never spans more than ExplosionBudgets.MaxFlashScreenFraction of the view.
            var cam = ViewCamera != null ? ViewCamera : Camera.main;
            float dist = cam != null ? Vector3.Distance(cam.transform.position, at) : 20f;
            float fov = cam != null && !cam.orthographic ? cam.fieldOfView : 60f;
            plan = plan.LimitFlashToView(dist, fov);
            LastPlan = plan;
            // The smoke and fire share the vehicle's velocity, but only as much as a camera framing the blast can follow: a quarter of a
            // 260 m/s airspeed would carry the whole fireball 90 m away in a tenth of a second (a frame with no fire in it). Bounded to a
            // fraction of the blast's own expansion speed, so the cloud drifts with the vehicle's direction without leaving the frame.
            var inherit = LimitInherit(new Vector3(e.VelX, e.VelY, e.VelZ) * 0.25f, plan.Radius * ExplosionBudgets.MaxInheritRadiiPerSecond);

            Emit(ExplosionLayer.Flash, plan.Flash, at, plan.FlashSize, 0.18f, plan.FlashColor, plan.FlashAlpha, 0f, 0f, Vector3.zero, false);
            Emit(ExplosionLayer.Fire, plan.Fire, at, plan.FireSize, plan.FireLife, plan.FireColor, plan.FireAlpha, plan.FireSpeed, plan.Radius * 0.4f, inherit, e.Grounded);
            Emit(ExplosionLayer.Smoke, plan.Smoke, at, plan.SmokeSize, plan.SmokeLife, plan.SmokeColor, plan.SmokeAlpha, plan.SmokeRise, plan.Radius * 0.5f, inherit * 0.6f, e.Grounded);
            Emit(ExplosionLayer.Sparks, plan.Sparks, at, plan.SparkSize, plan.SparkLife, plan.SparkColor, 1f, plan.SparkSpeed, plan.Radius * 0.1f, inherit, e.Grounded);
            Emit(ExplosionLayer.Embers, plan.Embers, at, plan.EmberSize, plan.EmberLife, plan.EmberColor, 0.9f, plan.EmberSpeed, plan.Radius * 0.3f, inherit, e.Grounded);

            if (flashLight != null && plan.LightIntensity > 0f)
            {
                flashLight.transform.position = at;
                flashLight.range = plan.LightRange;
                flashLight.color = new Color(plan.LightColor.R, plan.LightColor.G, plan.LightColor.B);
                lightPeak = plan.LightIntensity;
                lightRise = plan.LightRise;   // envelope runs 0..1, rise is per second
                lightDecay = plan.LightDecay;
                envelope.Trigger(1f, plan.LightHold);
                flashLight.enabled = true;
            }
        }

        /// <summary>The vector, or a NaN-free copy shortened to <paramref name="maxSpeed"/> m/s.</summary>
        static Vector3 LimitInherit(Vector3 v, float maxSpeed)
        {
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)) return Vector3.zero;
            float m = v.magnitude;
            if (float.IsInfinity(m)) return Vector3.zero;
            return m > maxSpeed && m > 0f ? v * (maxSpeed / m) : v;
        }

        void Emit(ExplosionLayer layer, int count, Vector3 at, float size, float life, Rgb3 color, float alpha, float speed, float spread, Vector3 inherit, bool grounded)
        {
            var ps = systems[(int)layer];
            if (ps == null || count <= 0) return;
            if (!ps.isPlaying) ps.Play();   // a pooled system that was paused, or ran out its duration, must still simulate what is emitted
            var ep = new ParticleSystem.EmitParams();
            ep.applyShapeToPosition = false;
            for (int i = 0; i < count; i++)
            {
                float a = Hash() * 6.2831853f;
                float up = layer == ExplosionLayer.Smoke ? 0.9f : layer == ExplosionLayer.Sparks ? 0.2f + Hash() * 0.9f : 0.15f + Hash() * 0.85f;
                if (grounded && up < 0.15f) up = 0.15f;
                float r = layer == ExplosionLayer.Flash ? 0f : Mathf.Sqrt(Hash()) * spread;
                var dir = new Vector3(Mathf.Cos(a), up, Mathf.Sin(a));
                dir.Normalize();
                ep.position = at + new Vector3(Mathf.Cos(a) * r, Hash() * spread * 0.6f, Mathf.Sin(a) * r);
                float v = layer == ExplosionLayer.Smoke ? speed : speed * (0.4f + 0.6f * Hash());
                ep.velocity = layer == ExplosionLayer.Smoke ? new Vector3(dir.x * spread * 0.25f, v, dir.z * spread * 0.25f) + inherit : dir * v + inherit;
                ep.startSize = size * (0.7f + 0.6f * Hash());
                ep.startLifetime = life * (0.75f + 0.5f * Hash());
                ep.startColor = new Color(color.R, color.G, color.B, alpha);
                ep.rotation = Hash() * 360f;
                ps.Emit(ep, 1);
            }
        }

        float Hash() => (float)DebrisField.Hash01(++noise);

        void Update() => Tick(Time.unscaledDeltaTime);

        public void Tick(float dt)
        {
            if (flashLight == null || !flashLight.enabled) return;
            float level = envelope.Step(dt, lightRise, lightDecay);
            flashLight.intensity = level * lightPeak;
            if (level <= 0f) flashLight.enabled = false;
        }

        // ---- pool construction (once per tier) ----

        void Build(int q)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null) Destroy(systems[i].gameObject);
                systems[i] = null;
            }
            for (int i = 0; i < systems.Length; i++) systems[i] = Make((ExplosionLayer)i, ExplosionBudgets.Cap(q, (ExplosionLayer)i));
            if (flashLight == null)
            {
                var go = new GameObject("ExplosionLight") { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(transform, false);
                flashLight = go.AddComponent<Light>();
                flashLight.type = LightType.Point;
                flashLight.shadows = LightShadows.None;
                flashLight.intensity = 0f;
                flashLight.enabled = false;
            }
            if (ExplosionBudgets.Lights(q) == 0) flashLight.enabled = false;
            envelope.Reset();
            builtQuality = q;
        }

        ParticleSystem Make(ExplosionLayer layer, int cap)
        {
            var go = new GameObject("Explosion_" + layer) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = Mathf.Max(1, cap);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.useUnscaledTime = false;
            switch (layer)
            {
                case ExplosionLayer.Smoke: main.gravityModifier = -0.05f; break;
                case ExplosionLayer.Sparks: main.gravityModifier = 0.9f; break;
                case ExplosionLayer.Embers: main.gravityModifier = 0.12f; break;
                default: main.gravityModifier = 0f; break;
            }
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(BuildGradient(layer));

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, SizeCurve(layer));

            if (layer == ExplosionLayer.Smoke || layer == ExplosionLayer.Fire)
            {
                var lim = ps.limitVelocityOverLifetime;
                lim.enabled = true;
                lim.dampen = layer == ExplosionLayer.Smoke ? 0.08f : 0.12f;
                lim.limit = layer == ExplosionLayer.Smoke ? 4f : 12f;
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = layer == ExplosionLayer.Sparks ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (layer == ExplosionLayer.Sparks) { r.velocityScale = 0.06f; r.lengthScale = 2.2f; }
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            int idx = (int)layer;
            if (mats[idx] == null) mats[idx] = MakeMaterial(layer == ExplosionLayer.Smoke || layer == ExplosionLayer.Fire);   // fire is alpha-blended: additive orange on a bright day sky washes out to a pale haze (CI frames)
            FxMaterials.Apply(r, mats[idx]);   // no safe material: the layer is switched off, never drawn with the error (magenta) material
            ps.Play();
            return ps;
        }

        static Gradient BuildGradient(ExplosionLayer layer)
        {
            var g = new Gradient();
            switch (layer)
            {
                case ExplosionLayer.Flash:
                    g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.8f, 0.5f), 1f) },
                              new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                    break;
                case ExplosionLayer.Fire:
                    g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.75f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.35f), new GradientColorKey(new Color(0.35f, 0.07f, 0.02f), 1f) },
                              new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
                    break;
                case ExplosionLayer.Smoke:
                    g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
                    break;
                default:
                    g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.08f), 0.6f), new GradientColorKey(new Color(0.3f, 0.05f, 0.02f), 1f) },
                              new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
                    break;
            }
            return g;
        }

        static AnimationCurve SizeCurve(ExplosionLayer layer)
        {
            switch (layer)
            {
                case ExplosionLayer.Flash: return new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.2f));
                case ExplosionLayer.Fire: return new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.35f, 1f), new Keyframe(1f, 1.5f));
                case ExplosionLayer.Smoke: return new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.5f, 1f), new Keyframe(1f, 1.7f));
                default: return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.3f));
            }
        }

        /// <summary>Additive (fire, flash, sparks, embers) or alpha-blended (smoke) soft particle material. Null only if no usable shader exists.</summary>
        internal static Material MakeMaterial(bool alphaBlend)
            => FxMaterials.Particle(SoftTexture(), alphaBlend ? "ExplosionSmoke" : "ExplosionAdd", !alphaBlend);

        /// <summary>The renderer of one pooled layer (tests read it: the real Build path made it).</summary>
        public ParticleSystemRenderer LayerRenderer(ExplosionLayer layer)
        {
            int i = (int)layer;
            return systems != null && i < systems.Length && systems[i] != null ? systems[i].GetComponent<ParticleSystemRenderer>() : null;
        }

        /// <summary>64x64 gaussian blob, built once. Procedural, no asset.</summary>
        internal static Texture2D SoftTexture()
        {
            if (softTexture != null) return softTexture;
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "ExplosionSoft", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d2 = dx * dx + dy * dy;
                    float a = d2 >= 1f ? 0f : Mathf.Exp(-d2 * 3.2f) * (1f - d2);
                    byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255);
                    px[y * n + x] = new Color32(255, 255, 255, b);
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            softTexture = t;
            return t;
        }
    }
}
