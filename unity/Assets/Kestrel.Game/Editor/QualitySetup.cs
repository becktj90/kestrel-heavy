// Generates Unity QualitySettings levels + one URP asset per G1 preset from data/quality.json.
// Code-first: no hand edits in Project Settings. Idempotent; re-run after editing quality.json.
using System.IO;
using Kestrel.Game.Quality;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kestrel.Game.Editor
{
    public static class QualitySetup
    {
        const string Dir = "Assets/Settings/Quality";

        [MenuItem("Kestrel/Setup Quality (G1)")]
        public static void Ensure()
        {
            GameDataSync.Sync();
            var table = QualityTable.Parse(File.ReadAllText(Path.Combine(GameDataSync.RepoRoot(), "data", "quality.json")));
            Directory.CreateDirectory(Dir);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Dir + "/KH_Renderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Dir + "/KH_Renderer.asset");
            }

            var assets = new UniversalRenderPipelineAsset[table.presets.Length];
            for (int i = 0; i < table.presets.Length; i++)
            {
                var p = table.presets[i];
                string path = $"{Dir}/KH_URP_{p.label}.asset";
                var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (rp == null) { rp = UniversalRenderPipelineAsset.Create(renderer); AssetDatabase.CreateAsset(rp, path); }
                rp.msaaSampleCount = p.msaa;
                rp.supportsHDR = p.hdr;
                rp.renderScale = 1f;               // runtime sets min(1, dprCap/dpr)
                rp.shadowDistance = p.shadows ? 400f : 0f;
                EditorUtility.SetDirty(rp);
                assets[i] = rp;
            }

            WriteLevels(table, assets);
            GraphicsSettings.defaultRenderPipeline = assets[table.IndexOf("high") >= 0 ? table.IndexOf("high") : 0];
            AssetDatabase.SaveAssets();
            Debug.Log($"[Kestrel] {table.presets.Length} quality levels written (Ultra non-selectable until 1.1).");
        }

        // QualitySettings has no public API to add/rename levels, so edit ProjectSettings/QualitySettings.asset.
        static void WriteLevels(QualityTable table, UniversalRenderPipelineAsset[] assets)
        {
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            var so = new SerializedObject(qs[0]);
            var levels = so.FindProperty("m_QualitySettings");
            levels.arraySize = table.presets.Length;
            for (int i = 0; i < table.presets.Length; i++)
            {
                var p = table.presets[i];
                var l = levels.GetArrayElementAtIndex(i);
                l.FindPropertyRelative("name").stringValue = p.label;
                l.FindPropertyRelative("customRenderPipeline").objectReferenceValue = assets[i];
                l.FindPropertyRelative("vSyncCount").intValue = 0;
                l.FindPropertyRelative("antiAliasing").intValue = p.msaa > 1 ? p.msaa : 0;
                l.FindPropertyRelative("softParticles").boolValue = p.id != "low";
                l.FindPropertyRelative("realtimeReflectionProbes").boolValue = false;
                l.FindPropertyRelative("globalTextureMipmapLimit").intValue = p.id == "low" ? 1 : 0;
                l.FindPropertyRelative("anisotropicTextures").intValue = p.id == "low" ? 0 : 1;
            }
            // iOS default = Medium; QualityService picks per device at runtime anyway.
            var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < perPlatform.arraySize; i++)
            {
                var e = perPlatform.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("second").intValue = Mathf.Max(0, table.IndexOf("medium"));
            }
            so.FindProperty("m_CurrentQuality").intValue = Mathf.Max(0, table.IndexOf("medium"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
