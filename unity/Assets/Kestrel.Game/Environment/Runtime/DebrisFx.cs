// Pooled physics debris: sparks and glowing fragments, a spent stage and fairing halves that tumble from the
// separation impulse. The simulation is Kit.DebrisWorld (engine-free); this file only draws it.
//  - fragments: pooled cubes, URP Lit (so the scene's time-of-day lighting applies) with a cooling emission ramp
//  - sparks: one ParticleSystem refilled each frame from a preallocated buffer, additive, exposure-trimmed at night
//  - stage / fairing: Attach(handle, transform) drives an existing mesh (FlightStageShow's pivots); an unattached
//    body is drawn with a pooled cylinder so a bare Gameplay hook still shows something.
// Nothing allocates per frame or per event; the pools are built once per quality tier.
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Environment
{
    public sealed class DebrisFx : MonoBehaviour
    {
        public static DebrisFx Instance { get; private set; }

        DebrisWorld world;
        int builtQuality = -1;
        Transform[] fragments = System.Array.Empty<Transform>();
        MeshRenderer[] fragmentRenderers = System.Array.Empty<MeshRenderer>();
        readonly Transform[] adopted = new Transform[DebrisBudgets.RigidSlots];
        readonly Transform[] fallback = new Transform[DebrisBudgets.RigidSlots];
        ParticleSystem sparks;
        ParticleSystem.Particle[] sparkBuffer = System.Array.Empty<ParticleSystem.Particle>();
        MaterialPropertyBlock block;
        Material fragmentMat, sparkMat;
        public MeshRenderer FragmentRenderer(int i) => fragmentRenderers != null && i >= 0 && i < fragmentRenderers.Length ? fragmentRenderers[i] : null;
        public ParticleSystemRenderer SparkRenderer => sparks != null ? sparks.GetComponent<ParticleSystemRenderer>() : null;
        Mesh cubeMesh, cylinderMesh;
        System.Action<CinematicEvent> handler;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public DebrisWorld World => world;
        public int BuiltQuality => builtQuality;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("KH_DebrisFx") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<DebrisFx>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        public void Subscribe()
        {
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
            if (fragmentMat != null) Destroy(fragmentMat);
            if (sparkMat != null) Destroy(sparkMat);
            if (cubeMesh != null) Destroy(cubeMesh);
            if (cylinderMesh != null) Destroy(cylinderMesh);
        }

        /// <summary>True while the body behind this handle is simulated; the owner of an attached Transform must not pose it then.</summary>
        public bool IsDriving(int handle) => world != null && world.IsAlive(handle);

        /// <summary>Drive an existing mesh pivot from a rigid body. The pivot is posed in its own parent's space (local position and rotation).</summary>
        public void Attach(int handle, Transform t)
        {
            if (world == null || !world.IsAlive(handle) || t == null) return;
            int slot = world.SlotOf(handle);
            if (slot >= DebrisBudgets.RigidSlots) return;
            adopted[slot] = t;
            if (fallback[slot] != null) fallback[slot].gameObject.SetActive(false);
        }

        public void Detach(Transform t)
        {
            for (int i = 0; i < adopted.Length; i++) if (adopted[i] == t) adopted[i] = null;
        }

        void Handle(CinematicEvent e)
        {
            if (builtQuality < 0 || builtQuality != Quality()) Build(Quality());
            var at = new V3(e.X, e.Y, e.Z);
            var vel = new V3(e.VelX, e.VelY, e.VelZ);
            switch (e.Kind)
            {
                case CinematicKind.DebrisClear: ClearAll(); break;
                case CinematicKind.StageSeparation: Stage(e, at, vel); break;
                case CinematicKind.FairingSeparation: Fairing(e, at, vel); break;
                case CinematicKind.DebrisBurst:
                case CinematicKind.Crash:
                case CinematicKind.Failure:
                    SetSurfaces(e.Grounded);
                    world.Burst(at, LimitBurstVelocity(vel), e.Energy01, Mathf.Max(0.5f, e.Radius), e.Grounded, e.Seed + (int)(e.X * 13f), e.Reduce);
                    break;
                case CinematicKind.EngineFailure:
                    if (!e.Overpressure) break;
                    world.Burst(at, LimitBurstVelocity(vel), e.Energy01 * 0.5f, Mathf.Max(0.5f, e.Radius), false, e.Seed + e.EngineIndex, e.Reduce);
                    break;
            }
        }

        /// <summary>The chunks fly at their own 10..28 m/s and share half the vehicle's velocity. At ascent speed that share carries every
        /// chunk 100+ m away within a tenth of a second, so the blast has no debris in frame: bound the vehicle share to a chunk's own speed.</summary>
        public const float MaxBurstVehicleSpeed = 16f;

        static V3 LimitBurstVelocity(V3 v)
        {
            float m2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z;
            if (float.IsNaN(m2) || float.IsInfinity(m2)) return new V3(0f, 0f, 0f);
            if (m2 <= MaxBurstVehicleSpeed * MaxBurstVehicleSpeed) return v;
            float k = MaxBurstVehicleSpeed / Mathf.Sqrt(m2);
            return new V3(v.X * k, v.Y * k, v.Z * k);
        }

        void SetSurfaces(bool grounded) { if (grounded) world.Surfaces = CinematicEvents.Surfaces; }

        void Stage(CinematicEvent e, V3 at, V3 vel)
        {
            float mass = Mathf.Max(1f, e.MassKg), len = Mathf.Max(0.5f, e.LengthM), rad = Mathf.Max(0.1f, e.Radius);
            float inertia = DebrisMath.CylinderInertia(mass, rad, len);
            float dv = e.ImpulseNs / mass;                                   // the push is along -Y: the body leaves backwards
            float w = DebrisMath.SpinFromImpulse(e.ImpulseNs, e.LeverM, inertia);
            if (e.Reduce) w = Mathf.Min(w, 0.15f);                           // reduce-motion: the same push, a gentler offset
            float sgn = DebrisHash(e.Seed + e.Stage) < 0.5f ? -1f : 1f;
            int h = world.SpawnRigid(DebrisKind.Stage, at, new V3(vel.X, vel.Y - dv, vel.Z), Q4.Identity, new V3(0f, 0f, sgn * w), mass, rad, len, -e.FrameAccel, 0f, 25f);
            CinematicEvents.LastDebrisHandle = h;
            CinematicEvents.LastDebrisHandleB = 0;
            EnsureFallback(world.SlotOf(h), len, rad);
        }

        void Fairing(CinematicEvent e, V3 at, V3 vel)
        {
            float mass = Mathf.Max(1f, e.MassKg), len = Mathf.Max(0.5f, e.LengthM), rad = Mathf.Max(0.1f, e.Radius);
            float inertia = DebrisMath.CylinderInertia(mass, rad, len);
            float dv = e.ImpulseNs / mass;
            float w = DebrisMath.SpinFromImpulse(e.ImpulseNs, e.LeverM, inertia);
            if (e.Reduce) w = Mathf.Min(w, 0.15f);
            int a = world.SpawnRigid(DebrisKind.Fairing, at, new V3(vel.X - dv, vel.Y, vel.Z), Q4.Identity, new V3(0f, -w, 0f), mass, rad, len, -e.FrameAccel, 0f, 25f);
            int b = world.SpawnRigid(DebrisKind.Fairing, at, new V3(vel.X + dv, vel.Y, vel.Z), Q4.Identity, new V3(0f, w, 0f), mass, rad, len, -e.FrameAccel, 0f, 25f);
            CinematicEvents.LastDebrisHandle = a;
            CinematicEvents.LastDebrisHandleB = b;
            EnsureFallback(world.SlotOf(a), len, rad * 0.6f);
            EnsureFallback(world.SlotOf(b), len, rad * 0.6f);
        }

        static float DebrisHash(int n) { uint h = (uint)n * 2654435761u ^ 0x9E3779B9u; h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; return (h & 0xFFFFFF) / 16777216f; }

        static int Quality()
        {
            int q = ExplosionFx.QualityOverride >= 0 ? ExplosionFx.QualityOverride : QualitySettings.GetQualityLevel();
            return q < 0 ? 0 : q > 2 ? 2 : q;
        }

        void ClearAll()
        {
            world.Clear();
            for (int i = 0; i < adopted.Length; i++) adopted[i] = null;
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>Steps the world and syncs transforms. Public so EditMode tests, with no player loop, can drive it.</summary>
        public void Tick(float dt)
        {
            if (world == null) return;
            if (world.Live == 0 && !AnyVisible) return;
            world.Step(dt);
            Sync();
        }

        bool AnyVisible;

        void Sync()
        {
            var bodies = world.Bodies;
            float night = TimeOfDayRig.Current != null ? TimeOfDayRig.Current.NightFactor : 0f;
            float expo = Mathf.Lerp(1f, 0.7f, night);
            bool any = false;
            int sparkCount = 0;
            for (int i = 0; i < bodies.Length; i++)
            {
                var b = bodies[i];
                if (i < DebrisBudgets.RigidSlots)
                {
                    var t = adopted[i] != null ? adopted[i] : fallback[i];
                    if (t == null) continue;
                    if (!b.Alive) { if (adopted[i] != null) adopted[i] = null; if (t.gameObject.activeSelf && t == fallback[i]) t.gameObject.SetActive(false); continue; }
                    any = true;
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    var q = new Quaternion(b.R.X, b.R.Y, b.R.Z, b.R.W);
                    if (adopted[i] != null) t.SetLocalPositionAndRotation(new Vector3(b.P.X, b.P.Y, b.P.Z), q);
                    else t.SetPositionAndRotation(new Vector3(b.P.X, b.P.Y, b.P.Z), q);
                }
                else if (b.Kind == DebrisKind.Fragment)
                {
                    int k = i - DebrisBudgets.RigidSlots;
                    if (k >= fragments.Length) continue;
                    var t = fragments[k];
                    if (!b.Alive) { if (t.gameObject.activeSelf) t.gameObject.SetActive(false); continue; }
                    any = true;
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    t.SetPositionAndRotation(new Vector3(b.P.X, b.P.Y, b.P.Z), new Quaternion(b.R.X, b.R.Y, b.R.Z, b.R.W));
                    float s = b.Radius * 2f * Mathf.Clamp01((b.Life - b.Age) * 2f + 0.001f);
                    t.localScale = new Vector3(s, s, s);
                    var g = DebrisMath.Glow(b.Temp);
                    block.SetColor(EmissionId, new Color(g.R, g.G, g.B) * (b.Temp * 2.5f * expo));
                    fragmentRenderers[k].SetPropertyBlock(block);
                }
                else if (b.Alive && b.Kind == DebrisKind.Spark && sparkCount < sparkBuffer.Length)
                {
                    any = true;
                    var g = DebrisMath.Glow(b.Temp);
                    float fade = Mathf.Clamp01((b.Life - b.Age) * 3f);
                    ref var p = ref sparkBuffer[sparkCount++];
                    p.position = new Vector3(b.P.X, b.P.Y, b.P.Z);
                    p.startSize = b.Radius * 2f;
                    p.startLifetime = 1f; p.remainingLifetime = 1f;
                    p.startColor = new Color(g.R * expo, g.G * expo, g.B * expo, fade * (0.2f + 0.8f * b.Temp));
                }
            }
            if (sparks != null) sparks.SetParticles(sparkBuffer, sparkCount);
            AnyVisible = any;
        }

        // ---- pool construction ----

        void Build(int q)
        {
            for (int i = 0; i < fragments.Length; i++) if (fragments[i] != null) Destroy(fragments[i].gameObject);
            if (sparks != null) Destroy(sparks.gameObject);
            for (int i = 0; i < fallback.Length; i++) { if (fallback[i] != null) Destroy(fallback[i].gameObject); fallback[i] = null; adopted[i] = null; }
            world = new DebrisWorld(q);
            if (block == null) block = new MaterialPropertyBlock();
            if (cubeMesh == null) cubeMesh = BuildCube();
            if (cylinderMesh == null) cylinderMesh = BuildCylinder(16);
            if (fragmentMat == null) fragmentMat = MakeLit();
            fragments = new Transform[world.FragmentCap];
            fragmentRenderers = new MeshRenderer[world.FragmentCap];
            for (int i = 0; i < fragments.Length; i++)
            {
                var go = new GameObject("Fragment") { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = cubeMesh;
                var mr = go.AddComponent<MeshRenderer>();
                FxMaterials.Apply(mr, fragmentMat);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                fragments[i] = go.transform; fragmentRenderers[i] = mr;
                go.SetActive(false);
            }
            sparkBuffer = new ParticleSystem.Particle[world.SparkCap];
            sparks = MakeSparks(world.SparkCap);
            builtQuality = q;
        }

        void EnsureFallback(int slot, float length, float radius)
        {
            if (slot < 0 || slot >= fallback.Length) return;
            if (fallback[slot] == null)
            {
                var go = new GameObject("RigidFallback") { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = cylinderMesh;
                var mr = go.AddComponent<MeshRenderer>();
                FxMaterials.Apply(mr, fragmentMat);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                fallback[slot] = go.transform;
                go.SetActive(false);
            }
            fallback[slot].localScale = new Vector3(radius * 2f, length, radius * 2f);
        }

        ParticleSystem MakeSparks(int cap)
        {
            var go = new GameObject("DebrisSparks") { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false; main.maxParticles = Mathf.Max(1, cap);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (sparkMat == null) sparkMat = MakeSparkMaterial();
            FxMaterials.Apply(r, sparkMat);
            ps.Play();
            return ps;
        }

        static Material MakeLit()
        {
            var shader = MaterialLibrary.Lit();   // Resources-pinned URP/Lit, checked for an error shader
            if (shader == null) return null;
            var m = new Material(shader) { name = "DebrisLit" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.2f, 0.19f, 0.18f));
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.55f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.3f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return m;
        }

        static Material MakeSparkMaterial() => FxMaterials.Particle(null, "DebrisSpark", true);

        static Mesh BuildCube()
        {
            var m = new Mesh { name = "DebrisCube", hideFlags = HideFlags.HideAndDontSave };
            var v = new Vector3[24]; var n = new Vector3[24]; var tri = new int[36];
            var axes = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (int f = 0; f < 6; f++)
            {
                var nn = axes[f];
                var u = Mathf.Abs(nn.y) > 0.5f ? Vector3.right : Vector3.up;
                var w = Vector3.Cross(nn, u);
                for (int k = 0; k < 4; k++)
                {
                    float sx = (k == 0 || k == 3) ? -0.5f : 0.5f, sy = (k < 2) ? -0.5f : 0.5f;
                    v[f * 4 + k] = nn * 0.5f + u * sx + w * sy; n[f * 4 + k] = nn;
                }
                int o = f * 4, t = f * 6;
                tri[t] = o; tri[t + 1] = o + 2; tri[t + 2] = o + 1; tri[t + 3] = o; tri[t + 4] = o + 3; tri[t + 5] = o + 2;
                if (Vector3.Dot(Vector3.Cross(v[o + 2] - v[o], v[o + 1] - v[o]), nn) < 0f) { tri[t + 1] = o + 1; tri[t + 2] = o + 2; tri[t + 4] = o + 2; tri[t + 5] = o + 3; }
            }
            m.vertices = v; m.normals = n; m.triangles = tri;
            m.RecalculateBounds();
            return m;
        }

        static Mesh BuildCylinder(int seg)
        {
            var m = new Mesh { name = "DebrisCylinder", hideFlags = HideFlags.HideAndDontSave };
            var v = new Vector3[seg * 2]; var n = new Vector3[seg * 2]; var tri = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg, c = Mathf.Cos(a) * 0.5f, s = Mathf.Sin(a) * 0.5f;
                v[i] = new Vector3(c, -0.5f, s); v[seg + i] = new Vector3(c, 0.5f, s);
                n[i] = n[seg + i] = new Vector3(c, 0f, s).normalized;
                int j = (i + 1) % seg, t = i * 6;
                tri[t] = i; tri[t + 1] = seg + i; tri[t + 2] = j; tri[t + 3] = j; tri[t + 4] = seg + i; tri[t + 5] = seg + j;
            }
            m.vertices = v; m.normals = n; m.triangles = tri;
            m.RecalculateBounds();
            return m;
        }
    }
}
