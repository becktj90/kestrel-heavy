#if UNITY_EDITOR || DEVELOPMENT_BUILD
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Kit.Tests
{
    public class CaptureCameraTests
    {
        GameObject _go;
        [TearDown] public void TearDown() { CaptureCamera.Clear(); CaptureCamera.Target = null; if (_go != null) Object.DestroyImmediate(_go); }

        [Test]
        public void ParsesCommandLineForm()
        {
            Assert.That(CaptureCamera.TryParse("40,25.5,-60:0,30,0", out var p, out var l), Is.True);
            Assert.That(p, Is.EqualTo(new Vector3(40f, 25.5f, -60f))); Assert.That(l, Is.EqualTo(new Vector3(0f, 30f, 0f)));
            Assert.That(CaptureCamera.TryParse("1,2:3,4,5", out _, out _), Is.False);
            Assert.That(CaptureCamera.TryParse("a,b,c:1,2,3", out _, out _), Is.False);
            Assert.That(CaptureCamera.TryParse(null, out _, out _), Is.False);
        }

        [Test]
        public void ApplyPosesTheCameraOnlyWhileActive()
        {
            _go = new GameObject("cam"); var cam = _go.AddComponent<Camera>();
            Assert.That(CaptureCamera.Apply(cam), Is.False);
            CaptureCamera.Override(new Vector3(10, 5, -20), new Vector3(0, 5, 0));
            Assert.That(CaptureCamera.Apply(cam), Is.True);
            Assert.That(cam.transform.position, Is.EqualTo(new Vector3(10, 5, -20)));
            Assert.That(Vector3.Dot(cam.transform.forward, (new Vector3(0, 5, 0) - new Vector3(10, 5, -20)).normalized), Is.GreaterThan(0.999f));
            CaptureCamera.Clear(); _go.transform.position = Vector3.zero;
            Assert.That(CaptureCamera.Apply(cam), Is.False); Assert.That(cam.transform.position, Is.EqualTo(Vector3.zero));
        }
    }
}
#endif
