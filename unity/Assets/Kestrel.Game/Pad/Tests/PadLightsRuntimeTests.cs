using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Pad.Tests
{
    public class PadLightsRuntimeTests
    {
        GameObject _root, _sun;
        [TearDown] public void TearDown()
        {
            PadLights.ForceNight = null;
            if (_root != null) Object.DestroyImmediate(_root); if (_sun != null) Object.DestroyImmediate(_sun);
            RenderSettings.sun = null; MaterialLibrary.ClearCache();
        }

        Light MakeSun(float elevationDeg)
        {
            _sun = new GameObject("sun"); var l = _sun.AddComponent<Light>(); l.type = LightType.Directional;
            _sun.transform.rotation = Quaternion.Euler(elevationDeg, 30f, 0f); RenderSettings.sun = l; return l;
        }

        [Test]
        public void BuildAttachesLightsWithinBudget()
        {
            _root = new GameObject("pad");
            var r = PadBuilder.Build(_root.transform, 3, PadArchetype.Tower, new PadFit(70f, 2.5f));
            var l = _root.GetComponent<PadLights>();
            Assert.That(l, Is.Not.Null); Assert.That(l.Plan, Is.Not.Empty);
            Assert.That(l.BuiltQuality, Is.EqualTo(r.Quality));
            Assert.That(l.Realtime.Count, Is.LessThanOrEqualTo(KitBudgets.PadRealtimeLights(r.Quality)));
            Assert.That(_root.GetComponents<PadLights>().Length, Is.EqualTo(1));
            PadBuilder.Build(_root.transform, 3, PadArchetype.Tower, new PadFit(70f, 2.5f));
            Assert.That(_root.GetComponents<PadLights>().Length, Is.EqualTo(1), "rebuild reuses the component");
        }

        [Test]
        public void DaySunMeansNoRealtimeContribution()
        {
            MakeSun(45f);
            _root = new GameObject("pad");
            PadBuilder.Build(_root.transform, 3, PadArchetype.Tower, new PadFit(70f, 2.5f));
            var l = _root.GetComponent<PadLights>();
            Assert.That(l.Night, Is.EqualTo(0f));
            l.ApplyNight(0);
            foreach (var rt in l.Realtime) { Assert.That(rt.enabled, Is.False, rt.name); Assert.That(rt.intensity, Is.EqualTo(0f)); }
        }

        [Test]
        public void NoSunMeansDay()
        {
            Assert.That(PadLights.FindSun() == null || PadLights.ResolveNight() >= 0f, Is.True);
            if (PadLights.FindSun() == null) Assert.That(PadLights.ResolveNight(), Is.EqualTo(0f));
        }

        [Test]
        public void SunBelowHorizonMeansNight()
        {
            var s = MakeSun(-20f);
            Assert.That(PadLights.SunElevation(s), Is.EqualTo(-20f).Within(0.5f));
            Assert.That(PadLights.ResolveNight(), Is.EqualTo(1f));
            s.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            Assert.That(PadLights.ResolveNight(), Is.EqualTo(0.5f).Within(0.02f));
        }

        [Test]
        public void ForceNightOverridesSunAndEnablesRealtime()
        {
            MakeSun(60f);
            PadLights.ForceNight = true;
            _root = new GameObject("pad");
            PadBuilder.Build(_root.transform, 3, PadArchetype.Tower, new PadFit(70f, 2.5f));
            var l = _root.GetComponent<PadLights>();
            Assert.That(l.Night, Is.EqualTo(1f));
            l.ApplyNight(0);
            foreach (var rt in l.Realtime) { Assert.That(rt.enabled, Is.True); Assert.That(rt.intensity, Is.GreaterThan(0f)); }
            PadLights.ForceNight = false; Assert.That(PadLights.ResolveNight(), Is.EqualTo(0f));
        }

        [Test]
        public void GlowBuiltWithFlaresAndTierCones()
        {
            _root = new GameObject("pad");
            var r = PadBuilder.Build(_root.transform, 3, PadArchetype.Tower, new PadFit(70f, 2.5f));
            var g = _root.GetComponent<PadLights>().Glow;
            Assert.That(g, Is.Not.Null);
            Assert.That(g.FlareCount, Is.GreaterThan(10));
            Assert.That(g.ConeCount > 0, Is.EqualTo(r.Quality >= 1));
            Assert.That(g.ConeTriangles, Is.LessThan(5000));
        }

        [Test]
        public void ForceNightSurvivesReload()
        {
            PadLights.ForceNight = true;
            var m = typeof(PadLights).GetMethod("ReloadForceNight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(m, Is.Not.Null);
            m.Invoke(null, null);   // what Play-mode entry runs after the domain reload
            Assert.That(PadLights.ForceNight, Is.EqualTo(true));
            PadLights.ForceNight = null; m.Invoke(null, null);
            Assert.That(PadLights.ForceNight, Is.Null);
        }
    }
}
