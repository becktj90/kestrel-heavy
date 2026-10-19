// Device stand-in: KESTREL_REPO_ROOT is an empty directory. Fonts come from Resources.
// Propellant families and the surface grain do not read the checkout.
using System;
using System.IO;
using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Pad.Tests
{
    public class PadArtDeviceTests
    {
        string _away, _prev;

        [SetUp]
        public void UnavailableCheckout()
        {
            _away = Path.Combine(Path.GetTempPath(), "kh-padart-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_away);
            _prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _away);
        }

        [TearDown]
        public void RestoreCheckout()
        {
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _prev);
            try { if (!string.IsNullOrEmpty(_away) && Directory.Exists(_away)) Directory.Delete(_away, true); }
            catch (Exception) { }
        }

        [Test]
        public void EmptyCheckoutStillBuildsFamiliesAndGrain()
        {
            Assert.That(Directory.GetFiles(_away), Is.Empty);
            var plan = FarmPlan.FromIds(new[] { "kerolox", "methalox", "hydrolox", "solid-apcp" });
            Assert.That(plan.Kerolox && plan.Methalox && plan.Hydrolox && plan.Solid, Is.True);
            var px = new byte[SurfaceGrain.Bytes];
            SurfaceGrain.Fill(SurfaceKind.Concrete, px);
            Assert.That(SurfaceGrain.Mean(px), Is.InRange(0.75f, 0.98f));
            SurfaceGrain.Fill(SurfaceKind.Bird, px);
            Assert.That(px.Length, Is.EqualTo(SurfaceGrain.Bytes));
        }

        [Test]
        public void FarmLabelsFitFromResourcesFonts()
        {
            var body = Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
            var black = Resources.Load<Font>("KHFonts/Fraunces-Black");
            Assert.That(body, Is.Not.Null, "SpaceGrotesk-Medium from Resources");
            Assert.That(black, Is.Not.Null, "Fraunces-Black from Resources");

            var layout = PadGenerator.Generate(4, PadArchetype.Tower, new PadFit(70f, 2.5f), null, FarmPlan.FromIds(new[] { "kerolox", "methalox", "hydrolox" }));
            foreach (var e in layout.Graph.All)
            {
                if (e.Id != "pad.farm" && !e.Id.StartsWith("pad.farm.") && e.Id != "pad.deluge" && !e.Id.StartsWith("pad.deluge.")) continue;
                if (string.IsNullOrEmpty(e.DisplayName)) continue;
                float px = Width(body, e.DisplayName, 14);
                Assert.That(px, Is.LessThan(335f), e.DisplayName + " at 14pt");
                Assert.That(Width(black, e.DisplayName, 14), Is.LessThan(335f), e.DisplayName + " in Fraunces");
            }
        }

        static float Width(Font font, string text, int size)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            if (font == null) return text.Length * size * 0.62f;
            font.RequestCharactersInTexture(text, size, FontStyle.Normal);
            float w = 0f;
            bool any = false;
            foreach (var ch in text)
            {
                if (font.GetCharacterInfo(ch, out var info, size, FontStyle.Normal))
                {
                    any = true;
                    w += info.advance;
                }
                else w += size * 0.62f;
            }
            if (!any) w = text.Length * size * 0.62f;
            return w;
        }
    }
}
