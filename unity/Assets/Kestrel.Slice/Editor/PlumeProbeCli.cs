// #119 v5: batch-mode plume probe, no play mode and no editor driving. Builds the real PlumeController (same
// Create/Configure/AttachGroundCloud path as SliceApp), steps the particle systems with Simulate, and logs
// PlumeDebugProbe reports at T+0.4 / 1.3 / 3 / 9 s:
//   Unity -batchmode -quit -projectPath unity -executeMethod Kestrel.Slice.Editor.PlumeProbeCli.Run
//         [-khCluster 14] [-khHeight 70] [-khRadius 7] [-khDeck 20] [-khQuality 2]
// For a live capture, add -khPlumeProbe to the play/capture command line instead: every PlumeController then
// carries a PlumeDebugProbe that logs once per second.
using System;
using System.Globalization;
using UnityEngine;

namespace Kestrel.Slice.Editor
{
    public static class PlumeProbeCli
    {
        public static void Run()
        {
            float cluster = F("-khCluster", 14f), height = F("-khHeight", 70f), radius = F("-khRadius", 7f), deck = F("-khDeck", 20f);
            int q = (int)F("-khQuality", QualitySettings.GetQualityLevel()); QualitySettings.SetQualityLevel(q, true);
            var root = new GameObject("PlumeProbeRoot");
            try
            {
                var pad = new GameObject("Pad").transform; pad.SetParent(root.transform, false);
                var deckTop = new GameObject("DeckTop").transform; deckTop.SetParent(pad, false);
                var rocket = new GameObject("Rocket").transform; rocket.SetParent(root.transform, false);
                var mat = Resources.Load<Material>("Materials/KH_Plume");
                var pc = PlumeController.Create(rocket, Vector3.zero, cluster, mat);
                pc.SetDeck(deckTop); pc.Configure(height, radius);
                float clock = 0f; pc.Clock = () => clock;   // Time.time is frozen outside play mode
                pc.AttachGroundCloud(pad, deck, height * 1.1f);
                const float dt = 1f / 30f; float t = 0f;
                foreach (float stop in new[] { 0.4f, 1.3f, 3f, 9f })
                {
                    while (t < stop)
                    {
                        float alt = 0.5f * 1.2f * 9.81f * 0.4f * t * t;      // ~TWR 1.4 climb
                        rocket.localPosition = new Vector3(0, alt, 0);
                        pc.SetHeightAboveDeck(alt);
                        pc.Apply(0.9, Kestrel.Core.Physics.Atmosphere.P0, 0);
                        foreach (var ps in pc.Emitters) if (ps != null) ps.Simulate(dt, false, false, false);
                        t += dt; clock = t;
                    }
                    Debug.Log(PlumeDebugProbe.Report(pc, $"CLI T+{stop:F1}s q={q} alt={rocket.localPosition.y:F1} m"));
                }
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorExit(1); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            if (Application.isBatchMode) EditorExit(0);
        }

        static void EditorExit(int code) => UnityEditor.EditorApplication.Exit(code);

        static float F(string name, float def)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == name && float.TryParse(a[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            return def;
        }
    }
}
