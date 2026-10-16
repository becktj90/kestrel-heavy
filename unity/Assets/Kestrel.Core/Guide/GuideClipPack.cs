// Copies assets/narration/guide into the player layout the device and WebGL builds can read.
// StreamingAssets holds the mp3s (UnityWebRequest). Resources holds the manifest (synchronous).
using System;
using System.Collections.Generic;
using System.IO;

namespace Kestrel.Core.Guide
{
    public static class GuideClipPack
    {
        public const string StreamingFolder = "unity/Assets/StreamingAssets/narration/guide";
        public const string ResourceManifest = "unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/guide/manifest.json";
        public const string PlayerFolder = "narration/guide";

        /// <summary>Copy every manifest clip into <paramref name="destRoot"/>'s player layout.
        /// Returns the number of audio files written. Throws if any card has no file.</summary>
        public static int Stage(string repoRoot, string destRoot = null)
        {
            if (string.IsNullOrEmpty(destRoot)) destRoot = repoRoot;
            var catalog = GuideClipCatalog.Load(repoRoot);
            if (catalog.Count == 0)
                throw new InvalidOperationException("guide manifest has no clips");
            string manifestSrc = Path.Combine(repoRoot ?? "", "assets", "narration", "guide", "manifest.json");
            if (!File.Exists(manifestSrc))
                throw new InvalidOperationException("guide manifest missing");
            string stream = Path.Combine(destRoot, "unity", "Assets", "StreamingAssets", "narration", "guide");
            string resDir = Path.GetDirectoryName(Path.Combine(destRoot, ResourceManifest.Replace('/', Path.DirectorySeparatorChar)));
            Directory.CreateDirectory(stream);
            Directory.CreateDirectory(resDir);
            File.Copy(manifestSrc, Path.Combine(stream, "manifest.json"), true);
            File.Copy(manifestSrc, Path.Combine(destRoot, ResourceManifest.Replace('/', Path.DirectorySeparatorChar)), true);
            var missing = new List<string>();
            int n = 0;
            foreach (var clip in catalog.Clips)
            {
                string src = clip.FullPath;
                if (string.IsNullOrEmpty(src) || !File.Exists(src))
                {
                    missing.Add(clip.Id);
                    continue;
                }
                long bytes = new FileInfo(src).Length;
                if (bytes <= 0)
                {
                    missing.Add(clip.Id);
                    continue;
                }
                string dest = StreamingFile(destRoot, clip);
                File.Copy(src, dest, true);
                if (!File.Exists(dest) || new FileInfo(dest).Length != bytes)
                {
                    missing.Add(clip.Id);
                    continue;
                }
                n++;
            }
            if (missing.Count > 0)
                throw new InvalidOperationException("guide clips missing: " + string.Join(", ", missing));
            return n;
        }

        public static string StreamingFile(string destRoot, GuideClip clip)
        {
            string name = FileName(clip);
            return Path.Combine(destRoot ?? "", "unity", "Assets", "StreamingAssets", "narration", "guide", name);
        }

        public static string ResourceManifestPath(string destRoot)
            => Path.Combine(destRoot ?? "", ResourceManifest.Replace('/', Path.DirectorySeparatorChar));

        public static string FileName(GuideClip clip)
        {
            if (clip == null || string.IsNullOrEmpty(clip.File)) return "";
            string rel = clip.File.Replace('\\', '/');
            int slash = rel.LastIndexOf('/');
            return slash >= 0 ? rel.Substring(slash + 1) : rel;
        }

        /// <summary>Non-empty mp3/ogg/wav files in a directory. A URL (WebGL StreamingAssets) counts as 0.</summary>
        public static int CountAudio(string directory)
        {
            if (string.IsNullOrEmpty(directory) || directory.IndexOf("://", StringComparison.Ordinal) >= 0) return 0;
            if (!Directory.Exists(directory)) return 0;
            int n = 0;
            string[] files;
            try { files = Directory.GetFiles(directory); }
            catch (Exception) { return 0; }
            for (int i = 0; i < files.Length; i++)
            {
                string ext = Path.GetExtension(files[i]);
                if (ext != ".mp3" && ext != ".ogg" && ext != ".wav") continue;
                try { if (new FileInfo(files[i]).Length > 0) n++; }
                catch (Exception) { }
            }
            return n;
        }
    }
}
