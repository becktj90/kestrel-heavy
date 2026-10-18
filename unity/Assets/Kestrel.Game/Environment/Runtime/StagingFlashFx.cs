// Staging flash: a brief capped flash and light at separation, and the soft ignition glow of the next stage.
// One pooled ParticleSystem and one point light, built once per quality tier. Flash amplitude comes from the shared
// FlashGovernor (3 Hz across explosions and staging), colour and ceiling follow TimeOfDayRig.Current.
using Kestrel.Core.Feel;
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Environment
{
    public sealed class StagingFlashFx : MonoBehaviour
    {
        public static StagingFlashFx Instance { get; private set; }

        ParticleSystem glow;
        Material mat;
        Light light;
        FlashEnvelope envelope;
        float lightPeak, lightRise, lightDecay;
        int builtQuality = -1, noise;
        System.Action<StagingFlashEvent> handler;

        public int BuiltQuality => builtQuality;
        public int ParticleCap => glow != null ? glow.main.maxParticles : 0;
        public float LightLevel => envelope.Level * lightPeak;
        public bool LightEnabled => light != null && light.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("KH_StagingFlashFx") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<StagingFlashFx>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        public void Subscribe()
        {
            Instance = this;
            if (handler == null) handler = Handle;
            CinematicEvents.StagingFlashRaised -= handler;
            CinematicEvents.StagingFlashRaised += handler;
        }

        public void Unsubscribe()
        {
            if (handler != null) CinematicEvents.StagingFlashRaised -= handler;
            if (Instance == this) Instance = null;
        }

        void OnDestroy() { if (mat != null) Destroy(mat); }

        static int CurrentQuality()
        {
            int q = ExplosionFx.QualityOverride >= 0 ? ExplosionFx.QualityOverride : QualitySettings.GetQualityLevel();
            return q < 0 ? 0 : q > 2 ? 2 : q;
        }

        void Handle(StagingFlashEvent e)
        {
            int q = CurrentQuality();
            if (builtQuality != q) Build(q);
            var tod = TimeOfDayRig.Current;
            float night = tod != null ? tod.NightFactor : 0f;
            float grant = e.Kind == StagingFlashKind.Separation && !e.Reduce
                ? FlashGovernor.Grant(Time.unscaledTime, 1f, ExplosionBudgets.FlashAmplitudeCap(night)) : 0f;
            var plan = StagingFlashPlan.Build(e, q, tod, e.Reduce, grant);
            if (!plan.Draw) return;
            var at = new Vector3(e.X, e.Y, e.Z);
            Emit(plan, at);
            if (light != null && plan.LightIntensity > 0f)
            {
                light.transform.position = at;
                light.range = plan.LightRange;
                light.color = new Color(plan.Color.R, plan.Color.G, plan.Color.B);
                lightPeak = plan.LightIntensity; lightRise = plan.LightRise; lightDecay = plan.LightDecay;
                envelope.Trigger(1f, plan.LightHold);
                light.enabled = true;
            }
        }

        void Emit(in StagingFlashPlan plan, Vector3 at)
        {
            if (glow == null) return;
            var ep = new ParticleSystem.EmitParams();
            ep.applyShapeToPosition = false;
            bool ign = plan.Kind == StagingFlashKind.Ignition;
            for (int i = 0; i < plan.Particles; i++)
            {
                float a = Hash() * 6.2831853f, r = Hash() * (ign ? 1.2f : 0.8f);
                ep.position = at + new Vector3(Mathf.Cos(a) * r, (Hash() - 0.5f) * 0.6f, Mathf.Sin(a) * r);
                ep.velocity = Vector3.zero;
                ep.startSize = plan.ParticleSize * (0.75f + 0.5f * Hash());
                ep.startLifetime = ign ? 1.6f : 0.22f;
                ep.startColor = new Color(plan.Color.R, plan.Color.G, plan.Color.B, plan.ParticleAlpha);
                ep.rotation = Hash() * 360f;
                glow.Emit(ep, 1);
            }
        }

        float Hash() => (float)DebrisField.Hash01(++noise);

        void Update() => Tick(Time.unscaledDeltaTime);

        public void Tick(float dt)
        {
            if (light == null || !light.enabled) return;
            float level = envelope.Step(dt, lightRise, lightDecay);
            light.intensity = level * lightPeak;
            if (level <= 0f) light.enabled = false;
        }

        void Build(int q)
        {
            if (glow != null) Destroy(glow.gameObject);
            var go = new GameObject("StagingGlow") { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform, false);
            glow = go.AddComponent<ParticleSystem>();
            glow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = glow.main;
            main.loop = false; main.playOnAwake = false;
            main.maxParticles = Mathf.Max(1, StagingFlashBudgets.Particles(q));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f; main.useUnscaledTime = false;
            var emission = glow.emission; emission.enabled = false;
            var shape = glow.shape; shape.enabled = false;
            var col = glow.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            if (mat == null) mat = ExplosionFx.MakeMaterial(false);
            if (mat != null) r.sharedMaterial = mat;
            glow.Play();
            if (light == null)
            {
                var lg = new GameObject("StagingLight") { hideFlags = HideFlags.HideAndDontSave };
                lg.transform.SetParent(transform, false);
                light = lg.AddComponent<Light>();
                light.type = LightType.Point; light.shadows = LightShadows.None; light.intensity = 0f; light.enabled = false;
            }
            if (ExplosionBudgets.Lights(q) == 0) light.enabled = false;
            envelope.Reset();
            builtQuality = q;
        }
    }
}
