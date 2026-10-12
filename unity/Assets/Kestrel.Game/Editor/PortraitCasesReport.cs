// Headless check of the VISUAL_SPEC §5 scaling cases (iPhone, Pro Max, iPad 11", iPad 13") from
// data/visual-spec.json. For the Unity CLI: -executeMethod Kestrel.Game.Editor.PortraitCasesReport.Run
// Writes Logs/portrait-cases.txt. Screenshots on SE / Pro Max / iPad 13" (plan U2 exit) still need a
// device or simulator run.
using System.IO;
using System.Text;
using Kestrel.Game.Layout;
using Kestrel.Game.Shell;
using UnityEditor;
using UnityEngine;

namespace Kestrel.Game.Editor
{
    public static class PortraitCasesReport
    {
        [System.Serializable] sealed class Dev { public string @class; public float[] pt; public float dpr; public float[] safe; }
        [System.Serializable] sealed class P { public Dev[] devices; }
        [System.Serializable] sealed class Root { public P portrait; }

        [MenuItem("Kestrel/Report Portrait Cases")]
        public static void Run()
        {
            string dir = Path.Combine(GameDataSync.RepoRoot(), "data");
            string specJson = File.ReadAllText(Path.Combine(dir, "visual-spec.json"));
            var spec = VisualSpecData.Parse(specJson, File.ReadAllText(Path.Combine(dir, "tod.json")));
            var root = JsonUtility.FromJson<Root>(specJson);
            var sb = new StringBuilder("class\tpx\tpx/du\tvisibleDu\tk\tcontent(px)\tanchorY\tplumeMaxDu\n");
            foreach (var d in root.portrait.devices)
            {
                double w = d.pt[0] * d.dpr, h = d.pt[1] * d.dpr;
                var f = PortraitLayout.Compute(w, h, PortraitLayout.SafeFromInsets(d.pt[0], d.pt[1], d.safe[0], d.safe[1], d.dpr), d.dpr, spec.Portrait);
                sb.Append($"{d.@class}\t{w}x{h}\t{f.PxPerDu:0.###}\t{f.VisibleWDu:0.#}x{f.VisibleHDu:0}\t{f.StageK}\t{f.Content}\t{f.VehicleAnchorPxY:0}\t{f.PlumeMaxWidthDu:0.#}\n");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/portrait-cases.txt", sb.ToString());
            Debug.Log("[Kestrel] portrait cases\n" + sb);
        }
    }
}
