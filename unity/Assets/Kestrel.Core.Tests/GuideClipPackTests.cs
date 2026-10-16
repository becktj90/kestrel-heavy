// Every Thrum card in assets/narration/guide/manifest.json must land in the player layout
// the iOS and WebGL builds actually copy: StreamingAssets for the mp3, Resources for the manifest.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Guide;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class GuideClipPackTests
    {
        [Test]
        public void EveryManifestCardResolvesInThePlayerLayout()
        {
            var catalog = GuideClipCatalog.Load(Golden.Root);
            Assert.AreEqual(78, catalog.Count, "the shipped guide manifest");
            string dest = Path.Combine(Path.GetTempPath(), "kh-guide-pack-" + Guid.NewGuid().ToString("N"));
            try
            {
                int staged = GuideClipPack.Stage(Golden.Root, dest);
                Assert.AreEqual(catalog.Count, staged);
                var packed = GuideClipCatalog.Parse(File.ReadAllText(GuideClipPack.ResourceManifestPath(dest)));
                Assert.AreEqual(catalog.Count, packed.Count);
                var seen = new HashSet<string>();
                foreach (var clip in catalog.Clips)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(clip.Id));
                    Assert.IsTrue(seen.Add(clip.Id));
                    Assert.IsTrue(packed.TryCard(clip.Id, out var copy), clip.Id);
                    Assert.AreEqual(clip.File, copy.File);
                    string path = GuideClipPack.StreamingFile(dest, clip);
                    Assert.IsTrue(File.Exists(path), clip.Id);
                    Assert.Greater(new FileInfo(path).Length, 0, clip.Id);
                    Assert.AreEqual(new FileInfo(clip.FullPath).Length, new FileInfo(path).Length, clip.Id);
                }
                string streamManifest = Path.Combine(dest, "unity", "Assets", "StreamingAssets", "narration", "guide", "manifest.json");
                Assert.IsTrue(File.Exists(streamManifest));
                Assert.AreEqual(78, GuideClipPack.CountAudio(Path.GetDirectoryName(streamManifest)));
            }
            finally
            {
                if (Directory.Exists(dest)) Directory.Delete(dest, true);
            }

            string build = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Editor", "BuildScript.cs"));
            StringAssert.Contains("PackGuideClips()", build);
            string web = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Editor", "BuildScript.WebGL.cs"));
            StringAssert.Contains("PackGuideClips()", web);
            foreach (var workflow in new[] { "unity.yml", "webgl-build.yml", "xcode-export.yml" })
            {
                string yml = File.ReadAllText(Path.Combine(Golden.Root, ".github", "workflows", workflow));
                StringAssert.Contains("tools/pack-guide-clips.sh", yml, workflow);
            }
            foreach (var workflow in new[] { "unity.yml", "webgl-build.yml" })
            {
                string yml = File.ReadAllText(Path.Combine(Golden.Root, ".github", "workflows", workflow));
                StringAssert.Contains("tools/check-built-guide-clips.sh", yml, workflow);
            }
        }

        [Test]
        public void BuildOutputCheckFailsWhenTheStreamingFolderIsEmpty()
        {
            string script = Path.Combine(Golden.Root, "tools", "check-built-guide-clips.sh");
            Assert.IsTrue(File.Exists(script));
            string root = Path.Combine(Path.GetTempPath(), "kh-built-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Assert.AreNotEqual(0, RunCheck(script, root), "no narration/guide folder");
                string guide = Path.Combine(root, "Data", "Raw", "narration", "guide");
                Directory.CreateDirectory(guide);
                Assert.AreNotEqual(0, RunCheck(script, root), "empty streaming clips folder");
                File.WriteAllText(Path.Combine(guide, "manifest.json"), "{\"clips\":[{\"id\":\"a\"},{\"id\":\"b\"}]}");
                File.WriteAllBytes(Path.Combine(guide, "a.mp3"), new byte[] { 9 });
                Assert.AreNotEqual(0, RunCheck(script, root), "audio short of the manifest");
                File.WriteAllBytes(Path.Combine(guide, "b.mp3"), new byte[] { 8 });
                Assert.AreEqual(0, RunCheck(script, root));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        static int RunCheck(string script, string root)
        {
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "bash",
                Arguments = "\"" + script + "\" \"" + root + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using (var process = System.Diagnostics.Process.Start(start))
            {
                process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}
