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
        float heightAboveDeck, towerHeight = 80f, vehicleHeight = 70f, vehicleRadius = 4f, deckRadius = 15f;
        int builtLevel = -1; float ignitionTime = -1f; Material mat, coreMat, cloudMat;

        /// <summary>Vehicle dimensions so the plume scales with what the camera frames, not the nozzle alone.</summary>
        public void Configure(float height, float radius) { vehicleHeight = Mathf.Max(5f, height); vehicleRadius = Mathf.Max(0.5f, radius); }

        /// <summary>Collide plume particles with the pad deck plane (top of deck, normal +Y) so the jet deflects
        /// along the deck instead of tunnelling through it. Planes-mode collision: no physics scene, O(planes) per particle.</summary>
        public void SetDeck(Transform deckTop)
        {
            foreach (var ps in new[] { core, diamonds }) if (ps != null) ConfigureDeckCollision(ps, deckTop);
        }

        public static void ConfigureDeckCollision(ParticleSystem ps, Transform deckTop)
        {
            var c = ps.collision;
            c.enabled = deckTop != null;
            if (deckTop == null) return;
            c.type = ParticleSystemCollisionType.Planes;
            c.mode = ParticleSystemCollisionMode.Collision3D;
            c.SetPlane(0, deckTop);
            c.bounce = 0.05f; c.dampen = 0.35f; c.lifetimeLoss = 0f; c.radiusScale = 0.5f;
            c.enableDynamicColliders = false; c.sendCollisionMessages = false;
        }

        public static PlumeController Create(Transform parent, Vector3 localPos, float exitDiameter, Material mat)
        {
            var go = new GameObject("Plume"); go.transform.SetParent(parent, false); go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0); // emit along -Y (down the vehicle axis)
            var pc = go.AddComponent<PlumeController>(); pc.ExitDiameter = exitDiameter;
            pc.mat = SoftParticle.PlumeMaterial(mat);
            pc.coreMat = SoftParticle.CoreMaterial(mat, QualitySettings.GetQualityLevel() >= 2);
            pc.cloudMat = SoftParticle.CloudMaterial(mat);
            pc.Build(QualitySettings.GetQualityLevel());
            return pc;
        }

        void Build(int level)
        {
            builtLevel = level;
            Budget = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            if (core == null)
            {
                core = Make(transform, "Core", coreMat != null ? coreMat : mat, 0.12f);
                body = Make(transform, "Body", mat, 0.45f);
                diamonds = Make(transform, "Diamonds", mat, 0.3f);
                // Core: white -> yellow -> orange, low alpha (additive; must not stack to flat white).
                Grad(core, (Color.white, 0), (PlumeShape.Core, 0.35f), (PlumeShape.Mid, 1), PlumeShape.CoreAlphaMax, 0.3f, 0f);
                // Body: yellow at the nozzle -> orange -> transparent red-orange tail.
                Grad(body, (PlumeShape.BodyHead, 0), (PlumeShape.Mid, 0.3f), (PlumeShape.Tail, 1), 0.8f, 0.55f, 0f);
                Grad(diamonds, (Color.white, 0), (PlumeShape.Core, 0.5f), (PlumeShape.Mid, 1), 1f, 0.8f, 0f);
                var bs = body.sizeOverLifetime; bs.enabled = true; bs.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, PlumeShape.BodySizeStart), new Keyframe(PlumeShape.BodyFullAt, 1f), new Keyframe(1, PlumeShape.ComputeLayout(1f, 1f, 1f, 1f, 10f, 10f).BodyGrow)));
                var cs = core.sizeOverLifetime; cs.enabled = true; cs.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, PlumeShape.CoreSizeStart), new Keyframe(PlumeShape.BodyFullAt, 1f), new Keyframe(1, PlumeShape.CoreSizeOverLife)));
            }
            Cap(core, Budget.Core); Cap(body, Budget.Body); Cap(diamonds, Budget.Diamonds); if (cloud != null) Cap(cloud, Budget.Cloud);
        }

        /// <summary>Ground cloud parented to the pad root (pad origin on the deck, anchored by FlightDriver) so it
        /// stays on the pad while the vehicle climbs.</summary>
        public void AttachGroundCloud(Transform padRoot, float deckR, float towerH)
        {
            towerHeight = towerH; deckRadius = Mathf.Max(5f, deckR);
            var L = PlumeShape.ComputeLayout(1f, 1f, ExitDiameter, vehicleRadius, vehicleHeight, deckRadius);
            if (cloud != null) Destroy(cloud.gameObject);
            var stale = padRoot.Find("GroundCloud"); if (stale != null) Destroy(stale.gameObject); // from a previous design
            var go = new GameObject("GroundCloud"); go.transform.SetParent(padRoot, false);
            go.transform.localPosition = new Vector3(0, L.CloudHeight, 0); go.transform.localRotation = Quaternion.Euler(-90, 0, 0); // circle in XZ
            cloud = go.AddComponent<ParticleSystem>();
            cloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = cloud.main; m.loop = true; m.playOnAwake = false; m.simulationSpace = ParticleSystemSimulationSpace.Local;
            m.startLifetime = new ParticleSystem.MinMaxCurve(PlumeShape.CloudLifeMin, PlumeShape.CloudLifeMax); m.startSpeed = new ParticleSystem.MinMaxCurve(L.CloudRadius * 0.35f, L.CloudRadius * 0.8f);
            m.startSize = new ParticleSystem.MinMaxCurve(L.CloudSizeMin, L.CloudSizeMax);
            m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); m.startColor = new ParticleSystem.MinMaxGradient(PlumeShape.Smoke, PlumeShape.Steam);
            var sh = cloud.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = L.CloudSpawnRadius; sh.radiusThickness = 0f; sh.arc = 360f;   // deck edge, outside the engine clear zone
            // Billow: slow down along the deck, rise, and grow (radial spread to ~deck radius and beyond).
            ConfigureCloudVelocity(cloud, L.CloudRadius);
            var so = cloud.sizeOverLifetime; so.enabled = true; so.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, L.CloudSizeStart, 1, PlumeShape.CloudGrowEnd));
            var rot = cloud.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.6f), 0), new GradientColorKey(Color.white, 0.15f), new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 1) },
                                             System.Array.ConvertAll(PlumeShape.CloudAlphaKeys, k => new GradientAlphaKey(k.y, k.x)));
            var col = cloud.colorOverLifetime; col.enabled = true; col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); var cmat = cloudMat != null ? cloudMat : mat; if (cmat != null) r.sharedMaterial = cmat;
            r.renderMode = ParticleSystemRenderMode.Billboard; r.sortMode = ParticleSystemSortMode.Distance; r.maxParticleSize = 4f; r.minParticleSize = 0f;
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

        /// <summary>Cloud billow motion: slow down along the deck, sink off the edge then rise. Every velocity
        /// curve (linear x/y/z, orbital x/y/z, radial) uses TwoConstants, and the limit is a single non-separate
        /// constant: Unity logs "Particle Velocity curves must all be in the same mode" when they are mixed.</summary>
        public static void ConfigureCloudVelocity(ParticleSystem ps, float cloudRadius)
        {
            var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.separateAxes = false; lv.dampen = 0.12f;
            lv.limit = new ParticleSystem.MinMaxCurve(cloudRadius * 0.08f);
            var vo = ps.velocityOverLifetime; vo.enabled = true; vo.space = ParticleSystemSimulationSpace.World;
            var zero = new ParticleSystem.MinMaxCurve(0f, 0f);
            vo.x = zero; vo.y = new ParticleSystem.MinMaxCurve(-2f, 4f); vo.z = zero;
            vo.orbitalX = zero; vo.orbitalY = zero; vo.orbitalZ = zero; vo.radial = zero;
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
            var L = PlumeShape.ComputeLayout(tf, pa, ExitDiameter, vehicleRadius, vehicleHeight, deckRadius);
            float d = L.VisualD;
            if (tf > 0.01f) { if (ignitionTime < 0f) ignitionTime = Time.time; } else ignitionTime = -1f;

            // Core: small additive zone just below the nozzle. Emitter sits CoreEmitOffset below the exit plane
            // (local +Z points down the axis) so no sprite reaches above it. Alpha comes from the gradient only.
            core.transform.localPosition = new Vector3(0, 0, L.CoreEmitOffset);
            var cm = core.main; cm.startLifetime = 0.15f; cm.startSpeed = L.CoreLength / 0.15f; cm.startSize = L.CoreSize;
            var cs = core.shape; cs.radius = d * 0.05f; cs.angle = 0f;
            cm.startColor = Color.white;
            Rate(core, tf > 0.01f ? 1f : 0f);

            // Body: ~1.2x cluster at sea level, widening with altitude via the cone (VISUAL_SPEC 3.2).
            float life = L.BodyLife;
            body.transform.localPosition = new Vector3(0, 0, L.BodyEmitOffset);
            var bm = body.main; bm.startLifetime = life; bm.startSpeed = L.Length / life;
            bm.startSize = new ParticleSystem.MinMaxCurve(L.BodyMin, L.BodyMax);
            var bs = body.shape; bs.radius = L.BodySpawnRadius; bs.angle = L.BodyConeDeg;
            Rate(body, tf);

            // Shock diamonds: tight bright knots, only while over-expanded / near ideal at low altitude.
            var dm = diamonds.main; dm.startLifetime = 0.3f; dm.startSpeed = L.Length * 0.5f / 0.3f; dm.startSize = d * 0.5f;
            diamonds.transform.localPosition = new Vector3(0, 0, L.CoreEmitOffset);
            var ds = diamonds.shape; ds.radius = d * 0.05f; ds.angle = 0f;
            Rate(diamonds, s.Diamonds > 0 ? tf : 0f);

            float since = ignitionTime < 0f ? 0f : Time.time - ignitionTime;
            if (cloud != null) Rate(cloud, PlumeShape.GroundCloud(tf, heightAboveDeck, towerHeight, since));
        }
    }
}
