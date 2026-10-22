// The mission card's payload thumbnail (t461: "Improve rendering of payload"): a small lit 3D render per payload form,
// built from procedural primitives and shaded in pure C# so it is identical in the editor, on device and on WebGL.
using System;
using System.Collections.Generic;
using System.Linq;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PayloadRenderTests
    {
        [Test]
        public void EveryPayloadFormRendersALitNonFlatImageAtTheAskedSize()
        {
            Assert.That(PayloadRender.Types.Count, Is.GreaterThanOrEqualTo(10));
            foreach (var form in PayloadRender.Types)
            {
                var rgba = PayloadRender.Rgba(form, 96);
                Assert.That(rgba.Length, Is.EqualTo(96 * 96 * 4), form);
                double v = PayloadRender.LumaVariance(rgba);
                Assert.That(v, Is.GreaterThan(0.004), form + " luma variance " + v);
                float cover = PayloadRender.Coverage(form, 48);
                Assert.That(cover, Is.InRange(0.12f, 0.92f), form + " fills " + cover);
                Assert.That(PayloadRender.PartCount(form), Is.GreaterThanOrEqualTo(4), form + " is more than a box");
                Assert.That(PayloadRender.Surfaces(form).Count, Is.GreaterThanOrEqualTo(3), form + " surfaces");
            }
        }

        [Test]
        public void TheSetHasFoilSolarCellsDishesAndADetailedBus()
        {
            var all = new HashSet<PayloadSurface>();
            foreach (var form in PayloadRender.Types) all.UnionWith(PayloadRender.Surfaces(form));
            foreach (var s in new[] { PayloadSurface.Foil, PayloadSurface.Cells, PayloadSurface.Dish, PayloadSurface.Bus })
                Assert.That(all, Does.Contain(s), s.ToString());
            Assert.That(PayloadRender.Types.Count(f => PayloadRender.Surfaces(f).Contains(PayloadSurface.Foil)), Is.GreaterThanOrEqualTo(3));
            Assert.That(PayloadRender.Types.Count(f => PayloadRender.Surfaces(f).Contains(PayloadSurface.Cells)), Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void SlicedJobMatchesTheOneShotRenderAndIsDeterministic()
        {
            string form = PayloadRender.Types[0];
            var one = PayloadRender.Rgba(form, 40);
            var job = PayloadRender.Begin(form, 40);
            int steps = 0;
            while (!job.Step(3)) Assert.That(++steps, Is.LessThan(1000));
            Assert.That(job.Done, Is.True);
            Assert.That(job.Px, Is.EqualTo(40));
            Assert.That(job.Pixels, Is.EqualTo(one));
            Assert.That(PayloadRender.Rgba(form, 40), Is.EqualTo(one));
        }

        [Test]
        public void DuskBackdropIsDarkBlueAndClearBackdropIsTransparent()
        {
            string form = PayloadRender.Types[0];
            int px = 64;
            var dusk = PayloadRender.Rgba(form, px, PayloadBackdrop.Dusk);
            var clear = PayloadRender.Rgba(form, px, PayloadBackdrop.Clear);
            foreach (var (x, y) in new[] { (0, 0), (px - 1, 0), (0, px - 1), (px - 1, px - 1) })
            {
                int o = (y * px + x) * 4;
                Assert.That(dusk[o + 3], Is.EqualTo(255), "dusk is opaque");
                Assert.That(dusk[o + 2], Is.GreaterThan(dusk[o]), "dusk is blue");
                Assert.That(dusk[o] + dusk[o + 1] + dusk[o + 2], Is.LessThan(3 * 90), "dusk is dark");
                Assert.That(clear[o + 3], Is.EqualTo(0), "clear corner");
            }
        }

        [Test]
        public void SizeIsClampedAndUnknownFormsStillDraw()
        {
            Assert.That(PayloadRender.Rgba("no-such-form", 4).Length, Is.EqualTo(PayloadRender.MinPx * PayloadRender.MinPx * 4));
            Assert.That(PayloadRender.LumaVariance(PayloadRender.Rgba("no-such-form", 48)), Is.GreaterThan(0.002));
            var png = PayloadRender.Png(PayloadRender.Types[0], 32);
            Assert.That(png.Take(4).ToArray(), Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
        }
    }
}
