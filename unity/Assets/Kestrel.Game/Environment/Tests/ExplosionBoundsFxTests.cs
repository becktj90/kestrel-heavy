// What the explosion actually puts on screen, measured on the real particle pools, rings and debris (EditMode).
// Written against the API that existed before the fix, with literal limits, so the old code fails it:
//   * a 500 m source radius used to become a multi-km fireball, flash card and ring (Trevor, TestFlight: a flat disc filled the screen);
//   * the particle shader had no pinned Resources material, so a stripped iOS player drew the blast magenta.
// Sizes are read from the live particles (GetCurrentSize), not from the plan, so a plan that is fine but an emitter that is not still fails.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class ExplosionBoundsFxTests
    {
        GameObject go, camGo; ExplosionFx fx; ShockwaveFx sw; DebrisFx debris; Camera cam;

        [SetUp]
        public void SetUp()
        {
            ExplosionFx.QualityOverride = 2;
            CinematicEvents.ClearSubscribers();
            FlashGovernor.Reset();
            go = new GameObject("fx-bounds");
            fx = go.AddComponent<ExplosionFx>(); fx.Subscribe();
            sw = go.AddComponent<ShockwaveFx>(); sw.Subscribe();
            debris = go.AddComponent<DebrisFx>(); debris.Subscribe();
            camGo = new GameObject("cam") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 50f;
            ExplosionFx.ViewCamera = cam;   // the camera the flash card is sized against (production: Camera.main)
            camGo.transform.position = new Vector3(0f, 10f, -8f);   // 8 m from the blast: a chase camera hugging a skinny stack
        }

        [TearDown]
        public void TearDown()
        {
            ExplosionFx.QualityOverride = -1; ExplosionFx.ViewCamera = null;
            fx.Unsubscribe(); sw.Unsubscribe(); debris.Unsubscribe();
            Object.DestroyImmediate(go); Object.DestroyImmediate(camGo);
            CinematicEvents.ClearSubscribers();
            TimeOfDayRig.ClearForce();
        }

        static List<ParticleSystem> Systems(GameObject root) => new List<ParticleSystem>(root.GetComponentsInChildren<ParticleSystem>(true));

        static float LargestParticle(ParticleSystem ps)
        {
            ps.Simulate(0.02f, false, false, true);
            var buf = new ParticleSystem.Particle[Mathf.Max(1, ps.particleCount)];
            int n = ps.GetParticles(buf);
            float big = 0f;
            for (int i = 0; i < n; i++) big = Mathf.Max(big, buf[i].GetCurrentSize(ps));
            return big;
        }

        [Test]
        public void ARunawayBlastRadiusCannotGrowTheParticlesPastTheCap()
        {
            foreach (var kind in new[] { "crash", "failure" })
            {
                if (kind == "crash") CinematicEvents.Crash(0f, 10f, 0f, 1f, 500f, false, false); else CinematicEvents.Failure(0f, 10f, 0f, 1f, 500f, false);
                float biggest = 0f;
                foreach (var ps in Systems(go)) biggest = Mathf.Max(biggest, LargestParticle(ps));
                Assert.That(biggest, Is.GreaterThan(0f), "something was drawn");
                Assert.That(biggest, Is.LessThanOrEqualTo(40f), kind + ": a 500 m radius must not make a 40 m+ particle");
            }
        }

        [Test]
        public void TheFlashCardNeverCoversMoreThanAThirdOfTheView()
        {
            // Camera 8 m from the blast, 50 degrees: the view is 2 * 8 * tan(25) = 7.5 m tall there.
            var at = new Vector3(0f, 10f, 0f);
            foreach (float radius in new[] { 0.4f, 1.5f, 6f, 500f })
            {
                TimeOfDayRig.Force(LaunchTimeOfDay.Night);   // night allows the strongest flash
                FlashGovernor.Reset();                        // the 3 Hz limiter would otherwise swallow the second card
                CinematicEvents.Failure(at.x, at.y, at.z, 1f, radius, false);
                var flash = Systems(fx.LayerRenderer(ExplosionLayer.Flash).gameObject)[0];
                float card = LargestParticle(flash);
                float viewH = 2f * Vector3.Distance(cam.transform.position, at) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                var lp = fx.LastPlan;
                Assert.That(card, Is.GreaterThan(0f), $"radius {radius}: a flash card is still drawn (plan flash {lp.Flash} size {lp.FlashSize:0.00} alpha {lp.FlashAlpha:0.00} radius {lp.Radius:0.0}; system particles {flash.particleCount} playing {flash.isPlaying} max {flash.main.maxParticles})");
                Assert.That(card, Is.LessThanOrEqualTo(viewH * 0.31f), $"radius {radius}: flash card {card:0.0} m in a {viewH:0.0} m view");
                flash.Clear();
            }
        }

        [Test]
        public void TheShockwaveRingAndHazeStayBounded()
        {
            CinematicEvents.Crash(0f, 1f, 0f, 1f, 500f, true, false);
            CinematicEvents.Shockwave(0f, 1f, 0f, 1f, true, false, 500f);
            for (int i = 0; i < 40; i++) sw.Tick(0.05f);
            float widest = 0f;
            foreach (var mr in sw.GetComponentsInChildren<MeshRenderer>(true))
                widest = Mathf.Max(widest, mr.bounds.size.x, mr.bounds.size.z);
            Assert.That(widest, Is.LessThanOrEqualTo(80f), "ring/haze quads are bounded in world size");
        }

        static IEnumerable<Renderer> EveryExplosionRenderer(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r is ParticleSystemRenderer || r is MeshRenderer) yield return r;
        }

        [Test]
        public void EveryExplosionRendererHasARealMaterialAndANonErrorShader()
        {
            CinematicEvents.Crash(0f, 10f, 0f, 1f, 2f, true, false);
            CinematicEvents.Failure(0f, 10f, 0f, 1f, 2f, false);
            CinematicEvents.Shockwave(0f, 1f, 0f, 1f, true, false, 2f);
            int seen = 0;
            foreach (var r in EveryExplosionRenderer(go))
            {
                seen++;
                if (!r.enabled) continue;   // a disabled renderer draws nothing, which is how an unusable material is handled
                Assert.That(r.sharedMaterial, Is.Not.Null, r.name + " has no material (draws magenta)");
                var sh = r.sharedMaterial.shader;
                Assert.That(sh, Is.Not.Null, r.name);
                Assert.That(sh.name, Does.Not.Contain("Error"), r.name + " shader " + sh.name);
                Assert.That(sh.name, Does.Not.Contain("Hidden/InternalErrorShader"), r.name);
                Assert.That(sh.isSupported, Is.True, r.name + " shader " + sh.name + " unsupported");
            }
            Assert.That(seen, Is.GreaterThan(5), "pools, rings and debris were built");
        }

        [Test]
        public void TheParticleMaterialIsPinnedInResourcesSoTheIosPlayerCannotStripIt()
        {
            // Shader.Find works in the editor but a player only keeps variants something references. KH_Plume / KH_Body use the same pin.
            var m = Resources.Load<Material>("Materials/KH_FxParticle");
            Assert.That(m, Is.Not.Null, "Resources/Materials/KH_FxParticle.mat missing: the particle variants are not kept in an iOS build");
            Assert.That(m.shader.name, Is.EqualTo("Universal Render Pipeline/Particles/Unlit"));
            Assert.That(m.shader.isSupported, Is.True);
        }
    }
}
