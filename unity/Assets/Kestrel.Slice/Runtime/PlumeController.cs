// Simple physically-driven plume: one ParticleSystem per vehicle base. Length scales with thrust
// fraction; width / cone angle follow the under-/over-expansion ratio p_exit / p_ambient, so the
// plume is a tight column at sea level and balloons as ambient pressure falls with altitude.
// Shock-diamond banding appears only while the jet is over-expanded (p_ambient > p_exit).
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class PlumeController : MonoBehaviour
    {
        public float ExitDiameter = 1f;      // m, from the engine catalog via RocketMeshBuilder
        public float ExitPressure = 70000f;  // Pa, nominal first-stage nozzle exit pressure (sea-level optimised ~0.7 bar)
        ParticleSystem core, diamonds;

        public static PlumeController Create(Transform parent, Vector3 localPos, float exitDiameter, Material mat)
        {
            var go = new GameObject("Plume"); go.transform.SetParent(parent, false); go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0); // emit along -Y (down the vehicle axis)
            var pc = go.AddComponent<PlumeController>(); pc.ExitDiameter = exitDiameter;
            pc.core = Make(go.transform, "Core", mat, new Color(1f, 0.75f, 0.4f, 0.9f), 400);
            pc.diamonds = Make(go.transform, "Diamonds", mat, new Color(1f, 0.95f, 0.85f, 1f), 60);
            return pc;
        }

        static ParticleSystem Make(Transform parent, string name, Material mat, Color col, int rate)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = 0.35f; main.startSpeed = 60f; main.startSize = 2f;
            main.startColor = col; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.maxParticles = 2000; main.playOnAwake = false;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 4f; sh.radius = 0.5f;
            var col2 = ps.colorOverLifetime; col2.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col2.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            var r = go.GetComponent<ParticleSystemRenderer>(); if (mat != null) r.sharedMaterial = mat;
            ps.Play();
            return ps;
        }

        /// <summary>thrustFraction 0..~1.1, ambient pressure Pa, vehicle speed m/s.</summary>
        public void Apply(double thrustFraction, double ambientP, double speed)
        {
            if (core == null) return;
            float tf = Mathf.Clamp((float)thrustFraction, 0, 1.2f);
            float pa = Mathf.Max(1f, (float)ambientP);
            float expansion = Mathf.Clamp(Mathf.Sqrt(ExitPressure / pa), 0.6f, 12f); // >1 under-expanded (altitude), <1 over-expanded
            var sh = core.shape; sh.radius = ExitDiameter * 0.5f; sh.angle = Mathf.Clamp(3f + 9f * (expansion - 1f), 1f, 70f);
            var m = core.main; m.startSpeed = 40f + 60f * tf; m.startSize = ExitDiameter * (0.8f + 0.5f * expansion);
            m.startLifetime = 0.25f + 0.25f * tf;
            var em = core.emission; em.enabled = tf > 0.01f; em.rateOverTime = 400f * tf;

            bool overExpanded = pa > ExitPressure * 0.9f;
            var dem = diamonds.emission; dem.enabled = tf > 0.01f && overExpanded; dem.rateOverTime = 60f * tf;
            var dsh = diamonds.shape; dsh.radius = ExitDiameter * 0.25f; dsh.angle = 1f;
            var dm = diamonds.main; dm.startSize = ExitDiameter * 0.6f; dm.startSpeed = 30f;
        }
    }
}
