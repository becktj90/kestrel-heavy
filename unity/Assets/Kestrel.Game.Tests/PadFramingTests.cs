using System.Collections.Generic;
using Kestrel.Game.Layout;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // P0 fix 4: pre-launch framing cut off the tower base and pad (hidden behind the design panel).
    public class PadFramingTests
    {
        // (name, w px, h px, w pt, h pt, top pt, bottom pt, dpr)
        static readonly object[] Devices =
        {
            new object[] { "iPhone",  1290, 2796, 430.0, 932.0, 59.0, 34.0, 3.0 },
            new object[] { "ProMax",  1320, 2868, 440.0, 956.0, 62.0, 34.0, 3.0 },
            new object[] { "iPad13",  2064, 2752, 1032.0, 1376.0, 24.0, 20.0, 2.0 },
        };

        static List<Vector3> SliceScene(float h, float r)
        {
            // Tower box as the D1 generator places it for the default seed: beside/behind the vehicle, 1.08 h tall.
            var tower = new List<Vector3>();
            foreach (var x in new[] { -(r + 3f), -(r + 15f) }) foreach (var y in new[] { 0f, h * 1.08f + 5f }) foreach (var z in new[] { -2f, 20f }) tower.Add(new Vector3(x, y, z));
            return PadFraming.Points(h, r, 15f, tower);
        }

        [TestCaseSource(nameof(Devices))]
        public void VehicleTowerAndDeckFitAboveDesignPanel(string name, int w, int hpx, double wpt, double hpt, double top, double bottom, double dpr)
        {
            const float fov = 50f, panel = 0.27f;
            float aspect = (float)w / hpx;
            var vp = PadFraming.Viewport(w, hpx, PortraitLayout.SafeFromInsets(wpt, hpt, top, bottom, dpr), dpr, panel);
            Assert.That(vp.yMin, Is.GreaterThanOrEqualTo(panel - 1e-4f), name);
            foreach (var (h, r) in new[] { (70f, 6f), (40f, 1.5f), (110f, 9f) })
            {
                var pts = SliceScene(h, r);
                var pos = PadFraming.Fit(pts, PadFraming.View, fov, aspect, vp);
                var inv = Quaternion.Inverse(PadFraming.View);
                float ty = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), tx = ty * aspect;
                float minV = 1, maxV = 0;
                foreach (var p in pts)
                {
                    var q = inv * (p - pos);
                    Assert.That(q.z, Is.GreaterThan(0f), $"{name} h={h}: point behind camera");
                    float u = 0.5f + 0.5f * q.x / (q.z * tx), v = 0.5f + 0.5f * q.y / (q.z * ty);
                    Assert.That(u, Is.InRange(vp.xMin - 1e-3f, vp.xMax + 1e-3f), $"{name} h={h} u");
                    Assert.That(v, Is.InRange(vp.yMin - 1e-3f, vp.yMax + 1e-3f), $"{name} h={h} v");
                    minV = Mathf.Min(minV, v); maxV = Mathf.Max(maxV, v);
                }
                Assert.That(maxV - minV, Is.GreaterThan(0.6f * vp.height), $"{name} h={h}: scene should fill the window, not be a speck");
            }
        }
    }
}
