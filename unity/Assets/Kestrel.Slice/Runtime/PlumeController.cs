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
        /// <summary>Time source for the ignition clock (the batch-mode probe steps it manually).</summary>
        public System.Func<float> Clock = () => Time.time;
        ParticleSystem core, body, diamonds, cloud;
        float heightAboveDeck, towerHeight = 80f, vehicleHeight = 70f, vehicleRadius = 4f, deckRadius = 15f;
        float bodyFadeIn = -1f, coreFadeIn = -1f; int builtLevel = -1; float ignitionTime = -1f; Material mat, coreMat, cloudMat;

        /// <summary>Vehicle dimensions so the plume scales with what the camera frames, not the nozzle alone.</summary>
        public void Configure(float height, float radius) { vehicleHeight = Mathf.Max(5f, height); vehicleRadius = Mathf.Max(0.5f, radius); }

        /// <summary>Collide plume particles with the pad deck plane (top of deck, normal +Y) so the jet deflects
        /// along the deck instead of tunnelling through it. Planes-mode collision: no physics scene, O(planes) per particle.</summary>
        public void SetDeck(Transform deckTop)
        {
            foreach (var ps in new[] { core, diamonds })
            {
                if (ps == null) continue;
                ConfigureDeckCollision(ps, deckTop); var nc = ps.collision;
                if (nc.enabled) { nc.bounce = PlumeShape.NearDeckBounce; nc.lifetimeLoss = PlumeShape.NearDeckLifetimeLoss; nc.dampen = PlumeShape.BodyDeckDampen; nc.radiusScale = PlumeShape.BodyDeckRadiusScale; }
            }
            // v5 root cause D: body particles tunnelled through the deck and kept travelling ~Length (200 m) under
            // it. Billboards are camera-facing, so the upper half of every sprite just under the deck rim projected
            // ABOVE the deck/horizon, where only sky/far ground is behind it, and passed the depth test: the
            // "white glow" over the deck. Deflecting them instead (tried in the v5 reference render) makes an
            // orange wall across the frame. #129 probe: killing them (lifetimeLoss 1) left 0/208 live on the pad.
            // Now they deflect sideways with low bounce and lose half their life on contact, so the spray is short
            // and already in the faded tail of the colour gradient.
            if (body != null)
            {
                ConfigureDeckCollision(body, deckTop); var bc = body.collision;
                if (bc.enabled) { bc.bounce = PlumeShape.BodyDeckBounce; bc.dampen = PlumeShape.BodyDeckDampen; bc.lifetimeLoss = PlumeShape.BodyDeckLifetimeLoss; bc.radiusScale = PlumeShape.BodyDeckRadiusScale; }
            }
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
            pc.mat = SoftParticle.TightPlumeMaterial(mat);   // body + diamonds (#129-v7 tight sprite)
            pc.coreMat = SoftParticle.CoreMaterial(mat, QualitySettings.GetQualityLevel() >= 2);
            pc.cloudMat = SoftParticle.CloudMaterial(mat);
            pc.Build(QualitySettings.GetQualityLevel());
            if (PlumeDebugProbe.RequestedOnCommandLine()) go.AddComponent<PlumeDebugProbe>();
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
                Grad(body, (PlumeShape.BodyHead, 0), (PlumeShape.Mid, 0.3f), (PlumeShape.Tail, 1), 0.9f, 0.6f, 0f);
                // #129: 7 m white alpha-1 knots running ~100 m down the axis read as detached white blobs under the centre engines.
                { var gd = new Gradient(); gd.SetKeys(new[] { new GradientColorKey(PlumeShape.Core, 0), new GradientColorKey(PlumeShape.BodyHead, 0.5f), new GradientColorKey(PlumeShape.Mid, 1) }, FadeKeys(PlumeShape.DiamondFadeIn, 0.35f, (0.25f, 0.4f)));
                  var dc = diamonds.colorOverLifetime; dc.enabled = true; dc.color = gd; }
                // #130-v9: sprites born flat (full width, YStart height) so the visible flame starts at the exit.
                bodyYFull = coreYFull = -1f;   // Y (height) curves are per layout: built in Apply
                FlatBorn(diamonds, u => 1f, PlumeShape.DiamondYSize);
                // #130: deck-deflected sprites shrink (and grey/fade, #130-v9) with their speed instead of dying.
                foreach (var ps in new[] { body, core, diamonds }) DeflectBySpeed(ps);
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
            m.startLifetime = new ParticleSystem.MinMaxCurve(PlumeShape.CloudLifeMin, PlumeShape.CloudLifeMax); m.startSpeed = new ParticleSystem.MinMaxCurve(L.CloudRadius * PlumeShape.CloudSpeedMin, L.CloudRadius * PlumeShape.CloudSpeedMax);
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

        /// <summary>Every emitter (core, body, diamonds, ground cloud) for PlumeDebugProbe.</summary>
        public ParticleSystem[] Emitters => new[] { core, body, diamonds, cloud };
        public ParticleSystem Body => body;
        /// <summary>Cluster footprint (m) the layout was built from.</summary>
        public float ClusterDiameter => ExitDiameter;

        float bodyYFull = -1f, coreYFull = -1f;   // #130-v9: last flat-born Y curves built

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

        /// <summary>Linear piecewise curve sampled from f over [0, 1].</summary>
        static AnimationCurve Sampled(System.Func<float, float> f, int n = 12)
        {
            var keys = new Keyframe[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n, h = 1f / n;
                float slope = (f(Mathf.Min(1f, u + h * 0.5f)) - f(Mathf.Max(0f, u - h * 0.5f))) / (Mathf.Min(1f, u + h * 0.5f) - Mathf.Max(0f, u - h * 0.5f));
                keys[i] = new Keyframe(u, f(u), slope, slope);
            }
            return new AnimationCurve(keys);
        }

        /// <summary>Separate-axes size over life: X (and Z) = width curve, Y = height curve.</summary>
        static void FlatBorn(ParticleSystem ps, System.Func<float, float> x, System.Func<float, float> y)
        {
            var so = ps.sizeOverLifetime; so.enabled = true; so.separateAxes = true;
            so.x = new ParticleSystem.MinMaxCurve(1f, Sampled(x)); so.y = new ParticleSystem.MinMaxCurve(1f, Sampled(y)); so.z = new ParticleSystem.MinMaxCurve(1f, Sampled(x));
        }

        /// <summary>Deck-deflected (slow) sprites shrink to BodyDeflectSize and turn grey at DeflectAlpha. Speed
        /// range is set per frame (SetSpeedRange) to the emitter's start speed.</summary>
        static void DeflectBySpeed(ParticleSystem ps)
        {
            var sb = ps.sizeBySpeed; sb.enabled = true; sb.separateAxes = false;
            sb.size = new ParticleSystem.MinMaxCurve(1f, Sampled(PlumeShape.BodySizeBySpeed, 20));
            var cb = ps.colorBySpeed; cb.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(0.85f, 0.83f, 0.8f), PlumeShape.DeflectSpeedFrac), new GradientColorKey(Color.white, PlumeShape.FullSpeedFrac) },
                      new[] { new GradientAlphaKey(PlumeShape.DeflectAlpha, PlumeShape.DeflectSpeedFrac), new GradientAlphaKey(1f, PlumeShape.FullSpeedFrac) });
            cb.color = g;
        }

        static void SetSpeedRange(ParticleSystem ps, float startSpeed)
        {
            var r = new Vector2(0f, Mathf.Max(0.5f, startSpeed));
            var sb = ps.sizeBySpeed; sb.range = r; var cb = ps.colorBySpeed; cb.range = r;
        }

        /// <summary>Body colour/alpha over life with an alpha fade-in to u = fadeIn (see Apply).</summary>
        public static void BodyGradient(ParticleSystem ps, float fadeIn, float rampU = PlumeShape.FadeRamp)
        {
            fadeIn = Mathf.Clamp(fadeIn, 0f, 0.35f);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(PlumeShape.BodyHead, 0), new GradientColorKey(PlumeShape.Mid, 0.3f), new GradientColorKey(PlumeShape.Tail, 1) },
                      FadeKeys(fadeIn, rampU, 0.9f, (PlumeShape.BodyMidAlpha, PlumeShape.BodyMidAt), (PlumeShape.BodyTailAlpha, PlumeShape.BodyTailAt)));
            var col = ps.colorOverLifetime; col.enabled = true; col.color = g;
        }

        /// <summary>#129-v6: alpha exactly 0 until u = fadeIn (sprite top still above the nozzle plane), then a
        /// short ramp to peak. The old linear 0 -> peak ramp over [0, fadeIn] showed a fringe above the nozzles.</summary>
        public static GradientAlphaKey[] FadeKeys(float fadeIn, float peak, params (float a, float t)[] tail) => FadeKeys(fadeIn, PlumeShape.FadeRamp, peak, tail);

        /// <summary>FadeKeys with an explicit ramp (normalised life).</summary>
        public static GradientAlphaKey[] FadeKeys(float fadeIn, float ramp, float peak, params (float a, float t)[] tail)
        {
            fadeIn = Mathf.Clamp(fadeIn, 0f, 0.9f);
            float up = Mathf.Min(0.95f, fadeIn + Mathf.Max(0.002f, ramp));
            var k = new System.Collections.Generic.List<GradientAlphaKey> { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0f, Mathf.Max(0.001f, fadeIn)), new GradientAlphaKey(peak, up) };
            foreach (var kt in tail) if (kt.t > up + 0.001f && kt.t < 0.999f) k.Add(new GradientAlphaKey(kt.a, kt.t));
            k.Add(new GradientAlphaKey(0f, 1f));
            return k.ToArray();
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
            vo.x = zero; vo.y = new ParticleSystem.MinMaxCurve(-0.3f, 0.6f); vo.z = zero;   // #129: slow rise so billows stay below the nozzles early
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
            int lvl = QualitySettings.GetQualityLevel();
            // #129: QualityService's governor (QualityGovernor.Tick) steps the preset down after 3 s of p95 > 20 ms;
            // the rebuild here then cut every cap mid-liftoff. Keep the liftoff budget for the first seconds.
            if (lvl != builtLevel) Build(lvl);   // follows the governor (which holds the tier for 20 s after ignition)
            float tf = Mathf.Clamp((float)thrustFraction, 0, 1.2f);
            float pa = Mathf.Max(0f, (float)ambientP) / PlumeShape.P0;
            var s = PlumeShape.Compute(tf, pa, ExitDiameter, DesignPressure);
            var L = PlumeShape.ComputeLayout(tf, pa, ExitDiameter, vehicleRadius, vehicleHeight, deckRadius);
            float d = L.VisualD;
            if (tf > 0.01f)
            {
                if (ignitionTime < 0f)
                {
                    ignitionTime = Clock();
                    Kestrel.Game.Quality.QualityHold.AtIgnition();   // hold the quality tier through liftoff
                    // Ignition burst so the ground cloud is already dense at T+0.4 (it only ramped in before).
                    if (cloud != null && heightAboveDeck < towerHeight) cloud.Emit(Mathf.RoundToInt(cloud.main.maxParticles * PlumeShape.CloudIgnitionBurst));
                }
            }
            else ignitionTime = -1f;

            // Core: small additive zone just below the nozzle. Emitter sits CoreEmitOffset below the exit plane
            // (local +Z points down the axis) so no sprite reaches above it. Alpha comes from the gradient only.
            // #129: pinned to the nozzle plane (emitter at the exit, fade-in over CoreEmitOffset, see body).
            core.transform.localPosition = Vector3.zero;
            float cfi = PlumeShape.CoreFadeIn(L);   // #130-v9: flat-born, exact clear point
            if (Mathf.Abs(cfi - coreFadeIn) > 0.005f)
            {
                coreFadeIn = cfi; var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(PlumeShape.Core, 0.35f), new GradientColorKey(PlumeShape.Mid, 1) },
                          FadeKeys(cfi, PlumeShape.CoreAlphaMax, (0.3f, 0.75f)));
                var col = core.colorOverLifetime; col.enabled = true; col.color = g;
            }
            var cm = core.main; cm.startLifetime = PlumeShape.CoreLife; cm.startSpeed = L.CoreLength / PlumeShape.CoreLife; cm.startSize = L.CoreSize;
            cm.gravityModifier = 0f; cm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var cs = core.shape; cs.radius = L.CoreSpawnRadius; cs.angle = 0f; cs.radiusThickness = 1f;   // spread over the cluster disc
            cm.startColor = Color.white;
            Rate(core, tf > 0.01f ? 1f : 0f);

            // Body: ~1.2x cluster at sea level, widening with altitude via the cone (VISUAL_SPEC 3.2).
            float life = L.BodyLife;
            // #129: the emitter sat BodyEmitOffset (~1 D, 12-14 m) below the nozzles, under the deck at liftoff, so
            // the streak "started well below the nozzles" and every particle was born inside the collision plane.
            // Emit AT the exit plane and fade in over the same distance instead (alpha 0 until a sprite's top is
            // below the plane): identical silhouette, but particles exist above the deck and can deflect off it.
            body.transform.localPosition = Vector3.zero;
            float fadeIn = PlumeShape.BodyFadeIn(L);
            if (Mathf.Abs(fadeIn - bodyFadeIn) > 0.005f) { bodyFadeIn = fadeIn; BodyGradient(body, fadeIn, PlumeShape.BodyFadeRamp(L)); }
            if (Mathf.Abs(L.BodyYFullAt - bodyYFull) > 0.005f) { bodyYFull = L.BodyYFullAt; FlatBorn(body, u => PlumeShape.BodySize(L, u), u => PlumeShape.BodySize(L, u) * PlumeShape.BodyYSize(L, u)); }
            if (Mathf.Abs(L.CoreYFullAt - coreYFull) > 0.005f) { coreYFull = L.CoreYFullAt; FlatBorn(core, PlumeShape.CoreSize, u => PlumeShape.CoreSize(u) * PlumeShape.CoreYSize(L, u)); }
            var bm = body.main; bm.startLifetime = life; bm.startSpeed = L.Length / life;
            bm.startSize = new ParticleSystem.MinMaxCurve(L.BodyMin, L.BodyMax);
            var bs = body.shape; bs.radius = L.BodySpawnRadius; bs.angle = L.BodyConeDeg;
            SetSpeedRange(body, L.Length / life);   // speed fraction 0..1 of startSpeed
            Rate(body, tf);

            // Shock diamonds: tight bright knots, only while over-expanded / near ideal at low altitude.
            // #130: drift DiamondTravelD x D (was 0.5 Length, ~310 m/s): the pattern hangs off the exit.
            var dm = diamonds.main; dm.startLifetime = PlumeShape.DiamondLife; dm.startSpeed = PlumeShape.DiamondTravelD * d / PlumeShape.DiamondLife; dm.startSize = d * PlumeShape.DiamondSizeD;
            dm.simulationSpace = ParticleSystemSimulationSpace.Local; bm.simulationSpace = ParticleSystemSimulationSpace.Local;   // locked to the nozzles (inherit-velocity is a World-space-only module)
            diamonds.transform.localPosition = Vector3.zero;   // #129-v6: was CoreEmitOffset (-2.25 m); fades in instead
            var ds = diamonds.shape; ds.radius = d * 0.05f; ds.angle = 0f;
            SetSpeedRange(diamonds, PlumeShape.DiamondTravelD * d / PlumeShape.DiamondLife); SetSpeedRange(core, L.CoreLength / PlumeShape.CoreLife);
            Rate(diamonds, s.Diamonds > 0 ? tf : 0f);

            float since = ignitionTime < 0f ? 0f : Clock() - ignitionTime;
            if (cloud != null) Rate(cloud, PlumeShape.GroundCloud(tf, heightAboveDeck, towerHeight, since));
        }
    }
}
