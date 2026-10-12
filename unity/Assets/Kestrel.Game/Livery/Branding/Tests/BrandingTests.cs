// Engine-free: Unity EditMode (Kestrel.Livery.Branding.Tests) and dotnet (Tests/Dotnet~).
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kestrel.Livery.Branding;
using NUnit.Framework;

namespace Kestrel.Livery.Branding.Tests
{
    public class BrandingTests
    {
        static BrandingV1 Ok() => new BrandingV1 { Name = "Tallowmere", Glyph = "glyph_chevron_up", Frame = "frame_roundel", GlyphColour = 3, FrameColour = 0, Primary = 1, Accent = 3, SerialClass = "M" };

        static string ArtDir()
        {
            foreach (var start in new[] { TestContext.CurrentContext.TestDirectory, Directory.GetCurrentDirectory() })
                for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
                {
                    foreach (var rel in new[] { "unity/Assets/Art/Vehicle/Branding", "Assets/Art/Vehicle/Branding" })
                    { var p = Path.Combine(d.FullName, rel); if (Directory.Exists(p)) return p; }
                }
            Assert.Fail("Art/Vehicle/Branding not found"); return null;
        }

        [Test] public void CatalogCounts() { Assert.That(BrandingCatalog.Glyphs.Length, Is.EqualTo(12)); Assert.That(BrandingCatalog.Frames.Length, Is.EqualTo(6)); Assert.That(BrandingCatalog.Palette.Length, Is.EqualTo(16)); Assert.That(BrandingCatalog.Palette.Distinct().Count(), Is.EqualTo(16)); }

        [Test] public void ValidBrandingPasses() => Assert.That(BrandingValidator.Validate(Ok()), Is.Empty);

        [Test]
        public void FieldRules()
        {
            var b = Ok(); b.Name = ""; Assert.That(BrandingValidator.Validate(b), Does.Contain("name_length"));
            b = Ok(); b.Name = new string('a', 21); Assert.That(BrandingValidator.Validate(b), Does.Contain("name_length"));
            b = Ok(); b.Name = "Kes<script>"; Assert.That(BrandingValidator.Validate(b), Does.Contain("name_chars"));
            b = Ok(); b.Glyph = "glyph_logo"; Assert.That(BrandingValidator.Validate(b), Does.Contain("glyph_unknown"));
            b = Ok(); b.Primary = 16; Assert.That(BrandingValidator.Validate(b), Does.Contain("colour_index"));
            b = Ok(); b.Accent = b.Primary; Assert.That(BrandingValidator.Validate(b), Does.Contain("colours_same"));
            b = Ok(); b.GlyphColour = 1; Assert.That(BrandingValidator.Validate(b), Does.Contain("glyph_contrast"));
            b = Ok(); b.SerialClass = "m"; Assert.That(BrandingValidator.Validate(b), Does.Contain("serial_class"));
            Assert.That(BrandingCatalog.Serial("M", 14), Is.EqualTo("KH-M-014"));
        }

        [TestCase("NASA")] [TestCase("nasa labs")] [TestCase("N4SA")] [TestCase("SpaceX")] [TestCase("Space-X")] [TestCase("SpaceY")]
        [TestCase("Blue Origin")] [TestCase("Blu Origin")] [TestCase("BlueOrigin Two")] [TestCase("New Glenn")] [TestCase("Jacklyn")]
        [TestCase("LC-36")] [TestCase("Falcon 9")] [TestCase("Rocket Lab")] [TestCase("Ariane")] [TestCase("Roscosmos")] [TestCase("Starship")]
        public void RealBrandNamesBlocked(string n) { var b = Ok(); b.Name = n; Assert.That(BrandingValidator.Validate(b), Does.Contain("name_brand"), n); }

        [TestCase("Tallowmere")] [TestCase("Kestrel Works")] [TestCase("Pine Ridge Rocketry")] [TestCase("Moth and Lantern")] [TestCase("KH Heavy Lift")] [TestCase("Bramble Orbital")]
        public void OriginalNamesAllowed(string n) { var b = Ok(); b.Name = n; Assert.That(BrandingValidator.Validate(b), Does.Not.Contain("name_brand"), n); }

        [Test]
        public void AgencyStyleLookalikesBlocked()
        {
            var b = Ok(); b.Frame = "frame_roundel"; b.Glyph = "glyph_orbit_dot"; b.Primary = 1; b.Accent = 4; b.GlyphColour = 0;
            Assert.That(BrandingValidator.Validate(b), Does.Contain("mark_lookalike"));
            b = Ok(); b.Frame = "frame_none"; b.Glyph = "glyph_arrow_split"; b.GlyphColour = 4; b.Primary = 0; b.Accent = 1;
            Assert.That(BrandingValidator.Validate(b), Does.Contain("mark_lookalike"));
            b.GlyphColour = 6; Assert.That(BrandingValidator.Validate(b), Does.Not.Contain("mark_lookalike"));
        }

        [Test]
        public void ArtFilesMatchCatalogAndAreTextFree()
        {
            var dir = ArtDir();
            var svgs = BrandingCatalog.Glyphs.Select(g => Path.Combine(dir, "Glyphs", g + ".svg")).Concat(BrandingCatalog.Frames.Select(f => Path.Combine(dir, "Frames", f + ".svg"))).ToList();
            var paths = new System.Collections.Generic.HashSet<string>();
            foreach (var p in svgs)
            {
                Assert.That(File.Exists(p), Is.True, p);
                var s = File.ReadAllText(p);
                Assert.That(s, Does.Contain("viewBox=\"0 0 64 64\""), p);
                foreach (var bad in new[] { "<text", "<image", "<use", "href", "font", "<script", "data:" }) Assert.That(s, Does.Not.Contain(bad), $"{p}: {bad}");
                var d = Regex.Match(s, "d=\"([^\"]*)\"").Groups[1].Value;
                if (!p.EndsWith("frame_none.svg")) { Assert.That(d, Is.Not.Empty, p); Assert.That(paths.Add(d), Is.True, $"{p} duplicates another shape"); }
            }
            Assert.That(Directory.GetFiles(Path.Combine(dir, "Glyphs"), "*.svg").Length, Is.EqualTo(12));
            Assert.That(Directory.GetFiles(Path.Combine(dir, "Frames"), "*.svg").Length, Is.EqualTo(6));
            var pal = File.ReadAllText(Path.Combine(dir, "palette.json"));
            var hex = Regex.Matches(pal, "#[0-9A-Fa-f]{6}").Cast<Match>().Select(m => m.Value).ToArray();
            Assert.That(hex, Is.EqualTo(BrandingCatalog.Palette), "palette.json must match BrandingCatalog.Palette order");
        }
    }
}
