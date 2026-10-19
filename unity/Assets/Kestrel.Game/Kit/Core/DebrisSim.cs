// Engine-free debris physics: pooled bodies (sparks, glowing fragments, a tumbling spent stage, fairing halves)
// with honest gravity, restitution and Coulomb friction against a deck disc and the ground plane.
// Separation spin comes from the impulse (omega = lever x J / I), not from a scripted rate.
// Bodies are a fixed array: Spawn never grows it and Step never allocates. Fragments and sparks do not collide
// with each other (that is the cost of the budget); rigid bodies use a cylinder, fragments and sparks a sphere.
using System;

namespace Kestrel.Kit
{
    public struct V3
    {
        public float X, Y, Z;
        public V3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static V3 operator *(V3 a, float s) => new V3(a.X * s, a.Y * s, a.Z * s);
        public static float Dot(V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static V3 Cross(V3 a, V3 b) => new V3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public V3 Normalized() { float l = Length; return l > 1e-9f ? new V3(X / l, Y / l, Z / l) : new V3(0f, 1f, 0f); }
    }

    public struct Q4
    {
        public float W, X, Y, Z;
        public static Q4 Identity => new Q4 { W = 1f };
        public V3 Rotate(V3 v)
        {
            // v' = v + 2w(q x v) + 2 q x (q x v)
            var q = new V3(X, Y, Z);
            var t = V3.Cross(q, v) * 2f;
            return v + t * W + V3.Cross(q, t);
        }
        /// <summary>Advance by angular velocity w (world frame, rad/s) over dt.</summary>
        public Q4 Integrate(V3 w, float dt)
        {
            float wl = w.Length;
            if (wl * dt < 1e-9f) return this;
            float half = wl * dt * 0.5f, s = (float)Math.Sin(half) / wl, c = (float)Math.Cos(half);
            var d = new Q4 { W = c, X = w.X * s, Y = w.Y * s, Z = w.Z * s };
            var r = new Q4
            {
                W = d.W * W - d.X * X - d.Y * Y - d.Z * Z,
                X = d.W * X + d.X * W + d.Y * Z - d.Z * Y,
                Y = d.W * Y - d.X * Z + d.Y * W + d.Z * X,
                Z = d.W * Z + d.X * Y - d.Y * X + d.Z * W,
            };
            float n = (float)Math.Sqrt(r.W * r.W + r.X * r.X + r.Y * r.Y + r.Z * r.Z);
            if (n > 1e-9f) { r.W /= n; r.X /= n; r.Y /= n; r.Z /= n; }
            return r;
        }
    }

    public enum DebrisKind { Fragment = 0, Spark = 1, Stage = 2, Fairing = 3 }

    public struct DebrisBody
    {
        public bool Alive, Asleep;
        public DebrisKind Kind;
        public V3 P, V, W;            // position (m), velocity (m/s), angular velocity (rad/s, world)
        public Q4 R;
        public float Mass, Inertia, Radius, Length;
        public float Restitution, Friction;
        public float GravityScale;    // 0 for bodies simulated in a vehicle-relative frame
        public float AccelY;          // constant extra acceleration along +Y (m/s^2), e.g. the frame of a thrusting upper stack
        public float Drag;            // linear drag 1/s
        public float Temp;            // 0 cold .. 1 white hot
        public float CoolTau;         // seconds
        public float Age, Life, RestTimer;
        public int Gen;
        public bool Touched;          // has hit a surface at least once
        public bool Collides;         // tests deck/ground. Off for bodies in a vehicle-relative frame and for in-flight debris
    }

    /// <summary>Deck disc at DeckY (radius 0 disables it) over a ground plane at GroundY.</summary>
    public struct DebrisSurfaces
    {
        public float GroundY, DeckY, DeckX, DeckZ, DeckRadius;
        public float HeightAt(float x, float z)
        {
            if (DeckRadius > 0f)
            {
                float dx = x - DeckX, dz = z - DeckZ;
                if (dx * dx + dz * dz <= DeckRadius * DeckRadius) return DeckY > GroundY ? DeckY : GroundY;
            }
            return GroundY;
        }
    }

    public static class DebrisBudgets
    {
        public const int RigidSlots = 3;   // one spent stage plus two fairing halves, every tier
        public static int Fragments(int q) => q <= 0 ? 12 : q == 1 ? 32 : 64;
        public static int Sparks(int q) => q <= 0 ? 24 : q == 1 ? 80 : 200;
        public static int Capacity(int q) => RigidSlots + Fragments(q) + Sparks(q);
    }

    public static class DebrisMath
    {
        public const float G = 9.81f;
        /// <summary>Transverse inertia of a solid cylinder, kg m^2: m (3 r^2 + L^2) / 12.</summary>
        public static float CylinderInertia(float mass, float radius, float length) => mass * (3f * radius * radius + length * length) / 12f;
        public static float SphereInertia(float mass, float radius) => 0.4f * mass * radius * radius;
        /// <summary>Angular speed from an impulse J (N s) applied at a lever arm (m) from the centre of mass: omega = r J / I.</summary>
        public static float SpinFromImpulse(float impulseNs, float leverM, float inertia) => inertia > 1e-6f ? Math.Abs(leverM) * Math.Abs(impulseNs) / inertia : 0f;

        /// <summary>Emissive colour of a cooling body: white-yellow, orange, red, then nearly black. Luminance falls monotonically with temp01.</summary>
        public static Rgb3 Glow(float temp01)
        {
            float t = temp01 < 0f ? 0f : temp01 > 1f ? 1f : temp01;
            if (t > 0.6f) return Rgb3.Lerp(new Rgb3(1f, 0.52f, 0.14f), new Rgb3(1f, 0.95f, 0.7f), (t - 0.6f) / 0.4f);
            if (t > 0.25f) return Rgb3.Lerp(new Rgb3(0.5f, 0.08f, 0.02f), new Rgb3(1f, 0.52f, 0.14f), (t - 0.25f) / 0.35f);
            return Rgb3.Lerp(new Rgb3(0.02f, 0.01f, 0.01f), new Rgb3(0.5f, 0.08f, 0.02f), t / 0.25f);
        }

        internal static float Hash01(int n)
        {
            uint h = (uint)n * 2654435761u ^ 0x9E3779B9u; h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
            return (h & 0xFFFFFFu) / 16777216f;
        }
    }

    public sealed class DebrisWorld
    {
        public const float MaxSubstep = 1f / 120f;
        public readonly DebrisBody[] Bodies;
        public DebrisSurfaces Surfaces;
        public float Gravity = DebrisMath.G;
        public readonly int Quality, FragmentCap, SparkCap;
        int gen, nextRigid, nextFragment, nextSpark, noise;
        int FragStart => DebrisBudgets.RigidSlots;
        int SparkStart => DebrisBudgets.RigidSlots + FragmentCap;

        public DebrisWorld(int quality)
        {
            Quality = quality < 0 ? 0 : quality > 2 ? 2 : quality;
            FragmentCap = DebrisBudgets.Fragments(Quality);
            SparkCap = DebrisBudgets.Sparks(Quality);
            Bodies = new DebrisBody[DebrisBudgets.Capacity(Quality)];
        }

        public int Live { get { int n = 0; for (int i = 0; i < Bodies.Length; i++) if (Bodies[i].Alive) n++; return n; } }
        public int LiveOf(DebrisKind k) { int n = 0; for (int i = 0; i < Bodies.Length; i++) if (Bodies[i].Alive && Bodies[i].Kind == k) n++; return n; }

        public void Clear() { for (int i = 0; i < Bodies.Length; i++) Bodies[i].Alive = false; }

        public static int Handle(int slot, int generation) => (generation << 8) | (slot & 0xFF);
        public bool IsAlive(int handle)
        {
            int s = handle & 0xFF; return handle > 0 && s < Bodies.Length && Bodies[s].Alive && Bodies[s].Gen == (handle >> 8);
        }
        public int SlotOf(int handle) => handle & 0xFF;

        /// <summary>Spawns a cylinder body (stage or fairing half) in the slot ring reserved for rigid bodies. Returns a handle.</summary>
        public int SpawnRigid(DebrisKind kind, V3 p, V3 v, Q4 r, V3 omega, float mass, float radius, float length, float accelY, float gravityScale, float lifeS)
        {
            int slot = nextRigid; nextRigid = (nextRigid + 1) % DebrisBudgets.RigidSlots;
            var b = new DebrisBody
            {
                Alive = true, Kind = kind, P = p, V = v, R = r, W = omega, Mass = Math.Max(1f, mass), Radius = Math.Max(0.05f, radius), Length = Math.Max(0.1f, length),
                Restitution = 0.15f, Friction = 0.6f, AccelY = accelY, GravityScale = gravityScale, Collides = gravityScale > 0f, Drag = 0f, Temp = 0f, CoolTau = 1f, Life = lifeS, Gen = ++gen,
            };
            b.Inertia = DebrisMath.CylinderInertia(b.Mass, b.Radius, b.Length);
            Bodies[slot] = b;
            return Handle(slot, b.Gen);
        }

        /// <summary>One ballistic fragment or spark. Recycles the oldest of its own kind when the tier budget is full.</summary>
        public int SpawnSmall(DebrisKind kind, V3 p, V3 v, V3 omega, float radius, float mass, float temp, float coolTau, float lifeS, float restitution, float friction, bool collides)
        {
            int slot;
            if (kind == DebrisKind.Spark) { slot = SparkStart + nextSpark; nextSpark = (nextSpark + 1) % Math.Max(1, SparkCap); if (SparkCap == 0) return 0; }
            else { slot = FragStart + nextFragment; nextFragment = (nextFragment + 1) % Math.Max(1, FragmentCap); if (FragmentCap == 0) return 0; }
            var b = new DebrisBody
            {
                Alive = true, Kind = kind, P = p, V = v, W = omega, R = Q4.Identity, Mass = Math.Max(0.01f, mass), Radius = Math.Max(0.01f, radius), Length = 0f,
                Restitution = restitution, Friction = friction, GravityScale = 1f, Collides = collides, Drag = kind == DebrisKind.Spark ? 0.35f : 0.05f, Temp = temp, CoolTau = Math.Max(0.05f, coolTau),
                Life = lifeS, Gen = ++gen,
            };
            b.Inertia = DebrisMath.SphereInertia(b.Mass, b.Radius);
            Bodies[slot] = b;
            return Handle(slot, b.Gen);
        }

        /// <summary>Explosion debris: hot chunks plus sparks flung out of a ball, inheriting a share of the vehicle velocity.</summary>
        public void Burst(V3 at, V3 vehicleV, float energy01, float radius, bool grounded, int seed, bool reduce)
        {
            float e = energy01 < 0f ? 0f : energy01 > 1f ? 1f : energy01;
            float s = 0.35f + 0.65f * e;
            int frags = (int)Math.Round(FragmentCap * s * (reduce ? 0.33f : 1f));
            int sparks = reduce ? 0 : (int)Math.Round(SparkCap * s);
            noise = seed * 7919;
            float speed = 10f + 18f * e;
            for (int i = 0; i < frags; i++)
            {
                var d = Dir(grounded);
                float v = speed * (0.35f + 0.65f * Rnd());
                float size = 0.12f + 0.35f * Rnd() * (0.5f + 0.5f * e);
                SpawnSmall(DebrisKind.Fragment, at + d * (radius * 0.3f), d * v + vehicleV * 0.5f, new V3(Rnd() * 8f - 4f, Rnd() * 8f - 4f, Rnd() * 8f - 4f), size, 800f * size * size * size, 0.9f + 0.1f * Rnd(), 2.5f + 2f * Rnd(), 9f + 3f * Rnd(), 0.3f, 0.55f, grounded);
            }
            for (int i = 0; i < sparks; i++)
            {
                var d = Dir(grounded);
                float v = speed * (0.8f + 1.2f * Rnd());
                SpawnSmall(DebrisKind.Spark, at + d * (radius * 0.2f), d * v + vehicleV * 0.5f, new V3(0f, 0f, 0f), 0.04f, 0.02f, 1f, 0.35f + 0.4f * Rnd(), 1.4f + 1.2f * Rnd(), 0.45f, 0.35f, grounded);
            }
        }

        float Rnd() => DebrisMath.Hash01(++noise);

        V3 Dir(bool grounded)
        {
            float a = Rnd() * 6.2831853f, up = grounded ? 0.15f + 0.85f * Rnd() : Rnd() * 2f - 1f;
            float r = (float)Math.Sqrt(Math.Max(0f, 1f - up * up));
            return new V3((float)Math.Cos(a) * r, up, (float)Math.Sin(a) * r);
        }

        public void Step(float dt)
        {
            if (float.IsNaN(dt) || dt <= 0f) return;
            if (dt > 0.05f) dt = 0.05f;
            int n = (int)Math.Ceiling(dt / MaxSubstep); if (n < 1) n = 1;
            float h = dt / n;
            for (int s = 0; s < n; s++)
                for (int i = 0; i < Bodies.Length; i++)
                    if (Bodies[i].Alive) StepBody(ref Bodies[i], h);
        }

        void StepBody(ref DebrisBody b, float h)
        {
            b.Age += h;
            if (b.Life > 0f && b.Age >= b.Life) { b.Alive = false; return; }
            if (b.CoolTau > 0f && b.Temp > 0f) { b.Temp *= (float)Math.Exp(-h / b.CoolTau); if (b.Temp < 0.002f) b.Temp = 0f; }
            if (b.Asleep) return;
            float ay = -Gravity * b.GravityScale + b.AccelY;
            b.V.Y += ay * h;
            if (b.Drag > 0f) { float k = (float)Math.Exp(-b.Drag * h); b.V = b.V * k; }
            b.P = b.P + b.V * h;
            b.R = b.R.Integrate(b.W, h);
            if (b.Collides) Collide(ref b, h);
        }

        void Collide(ref DebrisBody b, float h)
        {
            // Lowest point of the shape and its offset from the centre of mass.
            V3 r;
            if (b.Kind == DebrisKind.Stage || b.Kind == DebrisKind.Fairing)
            {
                var a = b.R.Rotate(new V3(0f, 1f, 0f));
                float sgn = a.Y >= 0f ? 1f : -1f;
                var cap = a * (-sgn * b.Length * 0.5f);
                var down = new V3(0f, -1f, 0f);
                var perp = down - a * V3.Dot(down, a);
                float pl = perp.Length;
                var rim = pl > 1e-5f ? perp * (b.Radius / pl) : new V3(b.Radius, 0f, 0f);
                r = cap + rim;
            }
            else r = new V3(0f, -b.Radius, 0f);

            var low = b.P + r;
            float surface = Surfaces.HeightAt(low.X, low.Z);
            float depth = surface - low.Y;
            if (depth <= 0f) return;
            b.P.Y += depth;
            var n = new V3(0f, 1f, 0f);
            var vc = b.V + V3.Cross(b.W, r);
            float vn = vc.Y;
            var rxn = V3.Cross(r, n);
            float invM = 1f / b.Mass, invI = 1f / b.Inertia;
            float j;
            if (vn < 0f)
            {
                float e = vn > -0.6f ? 0f : b.Restitution;
                float denom = invM + V3.Dot(rxn, rxn) * invI;
                j = -(1f + e) * vn / denom;
                b.V = b.V + n * (j * invM);
                b.W = b.W + rxn * (j * invI);
            }
            else j = b.Mass * Gravity * b.GravityScale * h;   // resting contact: the support impulse that cancels this step's gravity
            b.Touched = true;
            // Rolling and spinning resistance while in contact (soft ground, deformed shell), so nothing rolls forever.
            b.W = b.W * (float)Math.Exp(-3f * h);

            // Coulomb friction on the tangential contact velocity.
            vc = b.V + V3.Cross(b.W, r);
            var vt = new V3(vc.X, 0f, vc.Z);
            float vtl = vt.Length;
            if (vtl > 1e-5f)
            {
                var t = vt * (1f / vtl);
                var rxt = V3.Cross(r, t);
                float dt2 = invM + V3.Dot(rxt, rxt) * invI;
                float jt = vtl / dt2;
                float maxJt = b.Friction * j;
                if (jt > maxJt) jt = maxJt;
                b.V = b.V - t * (jt * invM);
                b.W = b.W - rxt * (jt * invI);
            }

            // Come to rest instead of jittering.
            float speed = b.V.Length, spin = b.W.Length;
            if (speed < 0.15f && spin * b.Radius < 0.15f) { b.RestTimer += h; if (b.RestTimer > 0.4f) { b.Asleep = true; b.V = new V3(); b.W = new V3(); } }
            else b.RestTimer = 0f;
        }

        public float KineticEnergy()
        {
            float k = 0f;
            for (int i = 0; i < Bodies.Length; i++)
            {
                if (!Bodies[i].Alive) continue;
                var b = Bodies[i];
                var v = b.V; var w = b.W;
                k += 0.5f * b.Mass * V3.Dot(v, v) + 0.5f * b.Inertia * V3.Dot(w, w);
            }
            return k;
        }
    }
}
