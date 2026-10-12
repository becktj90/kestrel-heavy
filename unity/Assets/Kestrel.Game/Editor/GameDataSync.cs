// Copies the U2 data files (repo-root data/*.json) into Assets/Resources/KestrelData so the player
// can load them. JSON in data/ stays the single source (plan §1); the copies are gitignored.
using System.IO;
using UnityEditor;

namespace Kestrel.Game.Editor
{
    public static class GameDataSync
    {
        public const string Dst = "Assets/Resources/KestrelData";
        public static readonly string[] Files = { "visual-spec", "tod", "quality" };

        [MenuItem("Kestrel/Sync U2 Data (visual-spec, tod, quality)")]
        public static void Sync()
        {
            string repo = RepoRoot();
            Directory.CreateDirectory(Dst);
            foreach (var f in Files)
            {
                string src = Path.Combine(repo, "data", f + ".json"), dst = Path.Combine(Dst, f + ".json");
                var text = File.ReadAllText(src);
                if (!File.Exists(dst) || File.ReadAllText(dst) != text) File.WriteAllText(dst, text);
            }
            AssetDatabase.Refresh();
        }

        /// <summary>The Unity project lives at &lt;repo&gt;/unity; walk up until data/visual-spec.json exists.</summary>
        public static string RepoRoot()
        {
            var env = System.Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            if (!string.IsNullOrEmpty(env)) return env;
            var d = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (d != null && !File.Exists(Path.Combine(d.FullName, "data", "visual-spec.json"))) d = d.Parent;
            if (d == null) throw new FileNotFoundException("repo root with data/visual-spec.json not found (set KESTREL_REPO_ROOT)");
            return d.FullName;
        }
    }
}
