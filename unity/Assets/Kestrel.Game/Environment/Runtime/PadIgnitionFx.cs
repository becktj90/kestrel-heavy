// Deluge spray, the steam that builds when that water meets the jet, a ground-hugging blast,
// and shock diamonds in the deflected plume. Pools are built once per quality tier. No light:
// the plume and the staging glow already own the flashes. Steam takes more than a second to rise.
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Environment
{
    public sealed class PadIgnitionFx : MonoBehaviour
    {
        public const string ReduceMotionPref = "kh.a11y.reduceMotion";
        public const string ReduceFlashingPref = "kh.a11y.reduceFlashing";

        public static PadIgnitionFx Instance { get; private set; }

        const float SprayLife = 0.65f, SteamLife = 2.4f, BlastLife = 0.9f, DiamondLife = 1.2f;

        ParticleSystem spray, steam, blast, diamonds;
        Transform holder, anchor;
        int builtQuality = -1, noise, nozzleCursor;
        float sprayAcc, steamAcc, blastAcc, diamondAcc, steamAge, prevThrottle;

        public int BuiltQuality => builtQuality;
        public int SprayCap => builtQuality < 0 ? 0 : PadVfxBudgets.Spray(builtQuality);
        public int SteamCap => builtQuality < 0 ? 0 : PadVfxBudgets.Steam(builtQuality);
        public int BlastCap => builtQuality < 0 ? 0 : PadVfxBudgets.Blast(builtQuality);
        public int DiamondCap => builtQuality < 0 ? 0 : PadVfxBudgets.Diamonds(builtQuality);
        public int SprayPool => spray != null ? spray.main.maxParticles : 0;
        public int SteamPool => steam != null ? steam.main.maxParticles : 0;
        public int BlastPool => blast != null ? blast.main.maxParticles : 0;
        public int DiamondPool => diamonds != null ? diamonds.main.maxParticles : 0;

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
            var go = new GameObject("KH_PadIgnitionFx") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<PadIgnitionFx>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; }

        void Update() => Tick(Time.deltaTime);

        /// <summary>Find the pad anchor, build the pools, and emit. Public so EditMode can drive a frame.</summary>
        public void Tick(float dt)
        {
            var found = ResolveAnchor();
            if (found == null)
            {
                DropHolder();
                return;
            }
            if (holder == null || holder.parent != found) Attach(found);
            int q = CurrentQuality();
            if (builtQuality != q) Build(q);
            EmitFrame(dt);
        }

        static int CurrentQuality()
        {
            int q = ExplosionFx.QualityOverride >= 0 ? ExplosionFx.QualityOverride : QualitySettings.GetQualityLevel();
            return q < 0 ? 0 : q > 2 ? 2 : q;
        }

        static bool Reduce() =>
            PlayerPrefs.GetInt(ReduceMotionPref, 0) != 0 || PlayerPrefs.GetInt(ReduceFlashingPref, 0) != 0;

        Transform ResolveAnchor()
        {
            if (anchor != null) return anchor;
            var go = GameObject.Find(PadVfxState.AnchorName);
            anchor = go != null ? go.transform : null;
            return anchor;
        }

        void Attach(Transform found)
        {
            DropHolder();
            anchor = found;
            var go = new GameObject("KH_PadIgnition");
            go.transform.SetParent(found, false);
            holder = go.transform;
            builtQuality = -1;
        }

        void DropHolder()
        {
            if (holder != null) Destroy(holder.gameObject);
            holder = null;
            spray = steam = blast = diamonds = null;
            builtQuality = -1;
            anchor = null;
        }

        void Build(int q)
        {
            if (holder == null) return;
            DestroySystem(ref spray);
            DestroySystem(ref steam);
            DestroySystem(ref blast);
            DestroySystem(ref diamonds);
            spray = Make("Spray", PadVfxBudgets.Spray(q), true, 0.4f);
            steam = Make("Steam", PadVfxBudgets.Steam(q), true, -0.12f);
            blast = Make("Blast", PadVfxBudgets.Blast(q), false, 0.05f);
            int gems = PadVfxBudgets.Diamonds(q);
            diamonds = gems > 0 ? Make("Diamonds", gems, false, 0f) : null;
            builtQuality = q;
        }

        void DestroySystem(ref ParticleSystem ps)
        {
            if (ps != null) Destroy(ps.gameObject);
            ps = null;
        }

        ParticleSystem Make(string name, int cap, bool alphaBlend, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(holder, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = Mathf.Max(1, cap);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 0f;
            main.gravityModifier = gravity;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.7f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(1f, 1.4f)));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            var mat = ExplosionFx.MakeMaterial(alphaBlend);
            if (mat != null) r.sharedMaterial = mat;
            ps.Play();
            return ps;
        }

        void EmitFrame(float dt)
        {
            if (dt < 0f) dt = 0f;
            var s = PadVfxState.Current;
            bool reduce = Reduce();
            if (s.Throttle > 0.02f)
            {
                if (prevThrottle <= 0.02f) steamAge = 0f;
                steamAge += dt;
            }
            else steamAge = 0f;
            prevThrottle = s.Throttle;

            float sprayShare = PadVfxPlan.Spray(s.Throttle, s.Deluge, reduce);
            float steamShare = PadVfxPlan.Steam(s.Throttle, steamAge, s.Deluge, reduce);
            float blastShare = PadVfxPlan.Blast(s.Throttle, s.Height, s.TowerHeight, reduce);
            float ratio = PadVfxPlan.PressureRatio(s.Height);
            int gemCount = PadVfxPlan.DiamondCount(builtQuality, ratio);
            float gemShare = gemCount > 0 && s.Throttle > 0.02f ? (reduce ? PadVfxPlan.ReduceShare : 1f) * Mathf.Clamp01(s.Throttle) : 0f;

            EmitSpray(Take(ref sprayAcc, sprayShare, SprayCap, SprayLife, dt), s);
            EmitSteam(Take(ref steamAcc, steamShare, SteamCap, SteamLife, dt), s);
            EmitBlast(Take(ref blastAcc, blastShare, BlastCap, BlastLife, dt), s);
            EmitDiamonds(Take(ref diamondAcc, gemShare, gemCount, DiamondLife, dt), s, ratio);
        }

        static int Take(ref float acc, float share, int cap, float life, float dt)
        {
            if (!(share > 0f) || cap <= 0 || !(life > 0f) || !(dt > 0f)) return 0;
            acc += share * cap * dt / life;
            int n = (int)acc;
            if (n > cap) n = cap;
            acc -= n;
            if (acc < 0f) acc = 0f;
            return n;
        }

        void EmitSpray(int n, PadVfxState s)
        {
            if (spray == null || n <= 0 || s.Nozzles <= 0) return;
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            float alpha = PadVfxPlan.SprayAlpha > 0.5f ? 0.5f : PadVfxPlan.SprayAlpha;
            for (int i = 0; i < n; i++)
            {
                int k = nozzleCursor % s.Nozzles;
                nozzleCursor++;
                float dx = s.DirX[k], dz = s.DirZ[k];
                ep.position = new Vector3(s.X[k], s.Y[k], s.Z[k]);
                ep.velocity = new Vector3(dx * 8f + Jitter(0.6f), 2.4f + Hash() * 1.6f, dz * 8f + Jitter(0.6f));
                ep.startSize = 1.1f + Hash() * 0.8f;
                ep.startLifetime = SprayLife * (0.8f + 0.4f * Hash());
                ep.startColor = new Color(0.82f, 0.88f, 0.92f, alpha);
                spray.Emit(ep, 1);
            }
        }

        void EmitSteam(int n, PadVfxState s)
        {
            if (steam == null || n <= 0) return;
            float rad = s.BearingDeg * Mathf.Deg2Rad;
            float ox = Mathf.Sin(rad), oz = Mathf.Cos(rad);
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < n; i++)
            {
                float along = 6f + Hash() * 18f;
                ep.position = new Vector3(ox * along + Jitter(2f), 1.2f + Hash() * 1.5f, oz * along + Jitter(2f));
                ep.velocity = new Vector3(Jitter(1.2f), 3.2f + Hash() * 2.4f, Jitter(1.2f));
                ep.startSize = 4f + Hash() * 5f;
                ep.startLifetime = SteamLife * (0.85f + 0.3f * Hash());
                ep.startColor = new Color(0.9f, 0.91f, 0.92f, PadVfxPlan.SteamAlpha);
                steam.Emit(ep, 1);
            }
        }

        void EmitBlast(int n, PadVfxState s)
        {
            if (blast == null || n <= 0) return;
            float rad = s.BearingDeg * Mathf.Deg2Rad;
            float ox = Mathf.Sin(rad), oz = Mathf.Cos(rad);
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < n; i++)
            {
                float along = 4f + Hash() * 10f;
                ep.position = new Vector3(ox * along, 0.45f + Hash() * 0.4f, oz * along);
                ep.velocity = new Vector3(ox * (16f + Hash() * 10f) + Jitter(2f), 0.4f + Hash() * 0.6f, oz * (16f + Hash() * 10f) + Jitter(2f));
                ep.startSize = 2.4f + Hash() * 2.2f;
                ep.startLifetime = BlastLife * (0.75f + 0.4f * Hash());
                ep.startColor = new Color(1f, 0.72f, 0.38f, PadVfxPlan.BlastAlpha);
                blast.Emit(ep, 1);
            }
        }

        void EmitDiamonds(int n, PadVfxState s, float ratio)
        {
            if (diamonds == null || n <= 0) return;
            float rad = s.BearingDeg * Mathf.Deg2Rad;
            float ox = Mathf.Sin(rad), oz = Mathf.Cos(rad);
            float spacing = PadVfxPlan.DiamondSpacing(s.ExitDiameter, ratio);
            int slots = PadVfxBudgets.Diamonds(builtQuality);
            if (slots < 1) slots = 1;
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < n; i++)
            {
                int slot = (nozzleCursor + i) % slots;
                float along = 8f + spacing * (slot + 1);
                ep.position = new Vector3(ox * along, 1.1f, oz * along);
                ep.velocity = new Vector3(ox * 6f, 0.15f, oz * 6f);
                ep.startSize = Mathf.Max(0.6f, s.ExitDiameter * 0.28f);
                ep.startLifetime = DiamondLife;
                ep.startColor = new Color(1f, 0.94f, 0.82f, PadVfxPlan.DiamondAlpha);
                diamonds.Emit(ep, 1);
            }
        }

        float Hash()
        {
            noise = noise * 1664525 + 1013904223;
            return ((noise >> 8) & 65535) / 65535f;
        }

        float Jitter(float amp) => (Hash() - 0.5f) * 2f * amp;
    }
}
