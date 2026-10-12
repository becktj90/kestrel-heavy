// Plume per VISUAL_SPEC §3: bright white-yellow core at the nozzle, a body that lengthens with throttle and
// widens as ambient pressure drops (x = pd / pa), shock diamonds while over-expanded at low altitude, and a
// liftoff ground cloud of smoke/steam that spreads across the pad and dies off once the vehicle clears the tower.
//
// Root cause of the "thin string of orange blobs" (main b514d53): one emitter with a 4 deg cone, 0.35-0.5 s life
// and startSize ~1.2 D (old PlumeController.cs:188,198-201,221), tinted orange by BOTH startColor (1,.75,.4) and
// the white->orange gradient (old :188,:203), so there was no white core; width used sqrt(70 kPa / pa) clamped
// >= 0.6 (old :219) so at sea level it was narrower than the nozzle; and there was no ground cloud at all.
// Particle counts are capped by the active quality preset's particleBudget (data/quality.json).
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class PlumeController : MonoBehaviour
    {
        public float ExitDiameter = 1f;      // m, cluster-equivalent exit diameter (RocketMeshBuilder)
        public float DesignPressure = PlumeShape.DefaultPd;
        public float ExitPressure = 70000f;  // kept for callers; regime now uses DesignPressure (VISUAL_SPEC §3.1)
        public PlumeBudget Budget { get; private set; }
        ParticleSystem core, body, diamonds, cloud;
        float heightAboveDeck, towerHeight = 80f;
        int builtLevel = -1; Material mat;

        public static PlumeController Create(Transform parent, Vector3 localPos, float exitDiameter, Material mat)
        {
            var go = new GameObject("Plume"); go.transform.SetParent(parent, false); go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0); // emit along -Y (down the vehicle axis)
            var pc = go.AddComponent<PlumeController>(); pc.ExitDiameter = exitDiameter;
            pc.mat = SoftParticle.PlumeMaterial(mat);
            pc.Build(QualitySettings.GetQualityLevel());
            return pc;
        }

        void Build(int level)
        {
            builtLevel = level;
            Budget = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            if (core == null)
            {
                core = Make(transform, "Core", mat, 0.12f);
                body = Make(transform, "Body", mat, 0.45f);
                diamonds = Make(transform, "Diamonds", mat, 0.3f);
                Grad(core, (Color.white, 0), (PlumeShape.Core, 0.6f), (PlumeShape.Mid, 1), 1f, 0.9f, 0f);
                Grad(body, (PlumeShape.Core, 0), (PlumeShape.Mid, 0.35f), (PlumeShape.Tail, 1), 0.8f, 0.5f, 0f);
                Grad(diamonds, (Color.white, 0), (PlumeShape.Core, 0.5f), (PlumeShape.Mid, 1), 1f, 0.8f, 0f);
                var bs = body.sizeOverLifetime; bs.enabled = true; bs.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 1.6f));
                var cs = core.sizeOverLifetime; cs.enabled = true; cs.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1f, 1, 0.5f));
            }
            Cap(core, Budget.Core); Cap(body, Budget.Body); Cap(diamonds, Budget.Diamonds); if (cloud != null) Cap(cloud, Budget.Cloud);
        }

        /// <summary>Ground cloud parented to the pad root (pad origin on the deck, anchored by FlightDriver) so it
        /// stays on the pad while the vehicle climbs.</summary>
        public void AttachGroundCloud(Transform padRoot, float deckRadius, float towerH)
        {
            towerHeight = towerH;
            if (cloud != null) Destroy(cloud.gameObject);
            var stale = padRoot.Find("GroundCloud"); if (stale != null) Destroy(stale.gameObject); // from a previous design
            var go = new GameObject("GroundCloud"); go.transform.SetParent(padRoot, false);
            go.transform.localPosition = new Vector3(0, 1.5f, 0); go.transform.localRotation = Quaternion.Euler(-90, 0, 0); // circle in XZ
            cloud = go.AddComponent<ParticleSystem>();
            cloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = cloud.main; m.loop = true; m.playOnAwake = false; m.simulationSpace = ParticleSystemSimulationSpace.Local;
            m.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f); m.startSpeed = new ParticleSystem.MinMaxCurve(10f, 26f);
            m.startSize = new ParticleSystem.MinMaxCurve(ExitDiameter * 2.5f, ExitDiameter * 4.5f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); m.startColor = new ParticleSystem.MinMaxGradient(PlumeShape.Smoke, PlumeShape.Steam);
            var sh = cloud.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = Mathf.Max(3f, ExitDiameter); sh.arc = 360f;
            // Billow: slow down along the deck, rise, and grow (radial spread to ~deck radius and beyond).
            var lv = cloud.limitVelocityOverLifetime; lv.enabled = true; lv.dampen = 0.08f; lv.limit = 4f;
            var vo = cloud.velocityOverLifetime; vo.enabled = true; vo.space = ParticleSystemSimulationSpace.World;
            vo.x = new ParticleSystem.MinMaxCurve(0f, 0f); vo.y = new ParticleSystem.MinMaxCurve(1.5f, 5f); vo.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var so = cloud.sizeOverLifetime; so.enabled = true; so.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.6f, 1, Mathf.Max(3f, deckRadius / ExitDiameter * 0.5f)));
            var rot = cloud.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.6f), 0), new GradientColorKey(Color.white, 0.15f), new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 1) },
                                             new[] { new GradientAlphaKey(0f, 0), new GradientAlphaKey(0.75f, 0.08f), new GradientAlphaKey(0.55f, 0.6f), new GradientAlphaKey(0f, 1) });
            var col = cloud.colorOverLifetime; col.enabled = true; col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); if (mat != null) r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard; r.sortMode = ParticleSystemSortMode.Distance; r.maxParticleSize = 3f;
            Cap(cloud, Budget.Cloud);
            var em = cloud.emission; em.enabled = false;
            cloud.Play();
        }

        void OnDestroy() { if (cloud != null) Destroy(cloud.gameObject); }

        /// <summary>Nozzle height above the deck top (m); drives the ground cloud fall-off.</summary>
        public void SetHeightAboveDeck(float h) => heightAboveDeck = h;

        static ParticleSystem Make(Transform parent, string name, Material mat, float sizeJitter)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = 0.3f; main.startSpeed = 60f; main.startSize = 2f;
            main.startColor = Color.white; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.playOnAwake = false;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 4f; sh.radius = 0.5f; sh.randomDirectionAmount = 0.02f;
            var r = go.GetComponent<ParticleSystemRenderer>(); if (mat != null) r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard; r.sortMode = ParticleSystemSortMode.YoungestInFront;
            r.maxParticleSize = 2f;
            ps.Play();
            return ps;
        }

        static void Grad(ParticleSystem ps, (Color c, float t) a, (Color c, float t) b, (Color c, float t) c, float a0, float a1, float a2)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a.c, a.t), new GradientColorKey(b.c, b.t), new GradientColorKey(c.c, c.t) },
                      new[] { new GradientAlphaKey(a0, 0), new GradientAlphaKey(a1, 0.4f), new GradientAlphaKey(a2, 1) });
            var col = ps.colorOverLifetime; col.enabled = true; col.color = g;
        }

        static void Cap(ParticleSystem ps, int max) { var m = ps.main; m.maxParticles = Mathf.Max(0, max); }

        static float Life(ParticleSystem ps) { var l = ps.main.startLifetime; return Mathf.Max(0.05f, l.mode == ParticleSystemCurveMode.TwoConstants ? (l.constantMin + l.constantMax) * 0.5f : l.constant); }

        /// <summary>Emission rate that keeps the live count at the cap: N = rate * lifetime.</summary>
        static void Rate(ParticleSystem ps, float fraction)
        {
            var em = ps.emission; em.enabled = fraction > 0.01f && ps.main.maxParticles > 0;
            em.rateOverTime = ps.main.maxParticles / Life(ps) * Mathf.Clamp01(fraction);
        }

        /// <summary>thrustFraction 0..~1.1, ambient pressure Pa, vehicle speed m/s.</summary>
        public void Apply(double thrustFraction, double ambientP, double speed)
        {
            if (core == null) return;
            int lvl = QualitySettings.GetQualityLevel(); if (lvl != builtLevel) Build(lvl);
            float tf = Mathf.Clamp((float)thrustFraction, 0, 1.2f);
            float pa = Mathf.Max(0f, (float)ambientP) / PlumeShape.P0;
            var s = PlumeShape.Compute(tf, pa, ExitDiameter, DesignPressure);
            float d = ExitDiameter;

            // Core: short, bright white-yellow, nozzle-sized, ~25% of plume length.
            var cm = core.main; cm.startLifetime = 0.12f; cm.startSpeed = s.Length * 0.25f / 0.12f; cm.startSize = d * 1.1f;
            var cs = core.shape; cs.radius = d * 0.3f; cs.angle = 1.5f;
            cm.startColor = new Color(1, 1, 1, s.CoreAlpha);
            Rate(core, tf);

            // Body: full length; cone opens so the far end reaches Width (expansion with altitude).
            float life = 0.4f;
            var bm = body.main; bm.startLifetime = life; bm.startSpeed = s.Length / life;
            bm.startSize = new ParticleSystem.MinMaxCurve(d * 0.9f, Mathf.Max(d * 1.2f, s.Width * 0.9f));
            var bs = body.shape; bs.radius = d * 0.45f;
            bs.angle = Mathf.Clamp(Mathf.Atan2(Mathf.Max(0, s.Width - d) * 0.5f, s.Length) * Mathf.Rad2Deg + 2f, 2f, 70f);
            Rate(body, tf);

            // Shock diamonds: tight bright knots, only while over-expanded / near ideal at low altitude.
            var dm = diamonds.main; dm.startLifetime = 0.3f; dm.startSpeed = s.Length * 0.5f / 0.3f; dm.startSize = d * 0.7f;
            var ds = diamonds.shape; ds.radius = d * 0.15f; ds.angle = 0.5f;
            Rate(diamonds, s.Diamonds > 0 ? tf : 0f);

            if (cloud != null) Rate(cloud, PlumeShape.GroundCloud(tf, heightAboveDeck, towerHeight));
        }
    }
}
