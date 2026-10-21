// F6: "Skip count is disabled during the countdown." On the live web build the button works
// (a tap logs counting->committed skip, then ascending), but it reads as disabled: .webcast-skip
// painted near-white text on a 16% white wash, about 2.5:1 over the daytime pad sky and 1:1 over a white one.
// The other live flight controls (Auto, Pause) are a dark fill with white text.
// This reads Slice.uss and checks the button against the sky it actually sits on.
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class SkipCountLooksLiveTests
    {
        // Pad sky behind the webcast strip, recovered from the live capture (wash 154,167,180 at 16% white).
        static readonly double[] PadSky = { 135, 150, 166 };
        static readonly double[] White = { 255, 255, 255 };
        static readonly double[] Black = { 0, 0, 0 };

        [Test]
        public void SkipCountReadsAsALiveButtonOverAnySky()
        {
            string uss = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Resources", "UI", "Slice.uss"));
            var rule = Rule(uss, ".webcast-skip");
            Assert.That(rule, Is.Not.Null, ".webcast-skip rule");
            var fill = Rgba(Prop(rule, "background-color"));
            var ink = Rgba(Prop(rule, "color"));
            Assert.That(rule, Does.Not.Contain("opacity"), "a faded skip looks disabled");
            foreach (var sky in new[] { PadSky, White, Black })
            {
                var back = Over(fill, sky);
                var text = Over(ink, back);
                double ratio = Contrast(text, back);
                Assert.That(ratio, Is.GreaterThanOrEqualTo(4.5),
                    $"Skip count text {Fmt(text)} on {Fmt(back)} over sky {Fmt(sky)} is {ratio:0.00}:1");
            }
        }

        static string Rule(string uss, string selector)
        {
            var m = Regex.Match(uss, "(?m)^" + Regex.Escape(selector) + @"\s*\{([^}]*)\}");
            return m.Success ? m.Groups[1].Value : null;
        }

        static string Prop(string rule, string name)
        {
            var m = Regex.Match(rule, @"(?:^|;)\s*" + Regex.Escape(name) + @"\s*:\s*([^;]+)");
            Assert.That(m.Success, Is.True, name);
            return m.Groups[1].Value.Trim();
        }

        static double[] Rgba(string v)
        {
            if (v == "white") return new double[] { 255, 255, 255, 1 };
            if (v == "black") return new double[] { 0, 0, 0, 1 };
            var m = Regex.Match(v, @"rgba?\(([^)]*)\)");
            Assert.That(m.Success, Is.True, v);
            var p = m.Groups[1].Value.Split(',');
            double a = p.Length > 3 ? double.Parse(p[3], CultureInfo.InvariantCulture) : 1;
            return new[] { P(p[0]), P(p[1]), P(p[2]), a };
        }

        static double P(string s) => double.Parse(s.Trim(), CultureInfo.InvariantCulture);

        static double[] Over(double[] top, double[] under)
        {
            double a = top[3];
            return new[] { top[0] * a + under[0] * (1 - a), top[1] * a + under[1] * (1 - a), top[2] * a + under[2] * (1 - a) };
        }

        static double Lum(double[] c)
        {
            double L(double v)
            {
                v /= 255.0;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * L(c[0]) + 0.7152 * L(c[1]) + 0.0722 * L(c[2]);
        }

        static double Contrast(double[] a, double[] b)
        {
            double x = Lum(a), y = Lum(b);
            return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
        }

        static string Fmt(double[] c) => $"({c[0]:0},{c[1]:0},{c[2]:0})";
    }
}
