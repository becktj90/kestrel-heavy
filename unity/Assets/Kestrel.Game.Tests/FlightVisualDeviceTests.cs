// Device stand-in: an empty KESTREL_REPO_ROOT, fonts and the body material from Resources only.
// The flight body has to stay opaque, and the camera captions have to fit a 335 pt portrait.
using System;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class FlightVisualDeviceTests
    {
        string prev;
        string away;

        [SetUp]
        public void UnavailableCheckout()
        {
            away = Path.Combine(Path.GetTempPath(), "kh-norepo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
        }

        [TearDown]
        public void RestoreCheckout()
        {
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prev);
            try { if (!string.IsNullOrEmpty(away) && Directory.Exists(away)) Directory.Delete(away, true); }
            catch (Exception) { }
        }

        [Test]
        public void ResourcesBodyAndFontsStayAvailableAndOpaque()
        {
            Assert.That(Kestrel.Core.Data.Catalog.RepoRootOrNull(), Is.Null, "a dummy KESTREL_REPO_ROOT must not walk to the checkout");
            var font = Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
            Assert.That(font, Is.Not.Null, "Space Grotesk from Resources");
            var body = Resources.Load<UnityEngine.Material>("Materials/KH_Body");
            Assert.That(body, Is.Not.Null, "KH_Body from Resources");
            var copy = new Material(body);
            try
            {
                FlightSolidBody.Apply(copy, new Color(0.2f, 0.4f, 0.8f, 0.25f));
                Assert.That(copy.color.a, Is.EqualTo(1f).Within(1e-4f));
                if (copy.HasProperty("_BaseColor"))
                    Assert.That(copy.GetColor("_BaseColor").a, Is.EqualTo(1f).Within(1e-4f));
                if (copy.HasProperty("_Surface"))
                    Assert.That(copy.GetFloat("_Surface"), Is.EqualTo(0f).Within(1e-4f));
                if (copy.HasProperty("_ZWrite"))
                    Assert.That(copy.GetFloat("_ZWrite"), Is.EqualTo(1f).Within(1e-4f));
                Assert.That(copy.renderQueue, Is.EqualTo(2000));
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }

        [Test]
        public void CameraCaptionsFit335AndThePlumeScaleDefaultsToKerolox()
        {
            Assert.That(Kestrel.Core.Data.Catalog.RepoRootOrNull(), Is.Null);
            Assert.IsTrue(FlightView.FitsWidth(335f));
            for (int i = 0; i < FlightView.Cycle.Length; i++)
            {
                string name = FlightView.Name(FlightView.Cycle[i]);
                float text = FlightView.LabelWidth(name, FlightView.ButtonFontPt);
                Assert.That(text, Is.LessThanOrEqualTo(FlightView.ButtonWidth(FlightView.Cycle[i]) - 8f), name);
            }

            var plain = PlumeShape.Compute(1f, 1f, 2f);
            var unit = PlumeShape.Compute(1f, 1f, 2f, PlumeShape.DefaultPd, 1f, 1f);
            Assert.AreEqual(plain.Length, unit.Length);
            Assert.AreEqual(plain.Width, unit.Width);
            var laid = PlumeShape.ComputeLayout(1f, 1f, 3f, 2f, 70f, 15f);
            var laidUnit = PlumeShape.ComputeLayout(1f, 1f, 3f, 2f, 70f, 15f, 1f, 1f);
            Assert.AreEqual(laid.Length, laidUnit.Length);
            Assert.AreEqual(laid.Width, laidUnit.Width);
            var hydro = PropellantFlight.Flame("hydrolox");
            var scaled = PlumeShape.Compute(1f, 1f, 2f, PlumeShape.DefaultPd, (float)hydro.LengthScale, (float)hydro.WidthScale);
            Assert.That(scaled.Length, Is.GreaterThan(plain.Length * 1.15f));
            Assert.That(scaled.Width, Is.GreaterThan(plain.Width * 1.15f));
            var solid = PropellantFlight.Flame("solid-apcp");
            var stubby = PlumeShape.Compute(1f, 1f, 2f, PlumeShape.DefaultPd, (float)solid.LengthScale, (float)solid.WidthScale);
            Assert.That(stubby.Length, Is.LessThan(plain.Length));
            Assert.That(stubby.Width, Is.GreaterThan(plain.Width));
        }
    }
}
