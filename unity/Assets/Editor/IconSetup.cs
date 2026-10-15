// Code-first iOS app icon setup for Kestrel Heavy (A2: c1 kestrel-booster).
// Menu: Kestrel > Apply iOS App Icon
// CLI:  Unity -batchmode -quit -projectPath unity -executeMethod KestrelHeavy.EditorTools.IconSetup.Apply
// Excluded when the active target is WebGL: the GameCI WebGL editor image has no UnityEditor.iOS.
#if !UNITY_WEBGL
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.iOS;
using UnityEngine;

namespace KestrelHeavy.EditorTools
{
    public static class IconSetup
    {
        public const string IconPath = "Assets/Art/Icon/AppIcon-1024.png";

        [MenuItem("Kestrel/Apply iOS App Icon")]
        public static void ApplyFromMenu() => Apply();

        public static void Apply()
        {
            try
            {
                ConfigureImporter();
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
                if (tex == null) throw new Exception($"Icon texture not found at {IconPath}");
                if (tex.width != 1024 || tex.height != 1024)
                    throw new Exception($"Icon must be 1024x1024, got {tex.width}x{tex.height}");

                // Default icon (fallback for any slot not set below).
                PlayerSettings.SetIcons(NamedBuildTarget.iOS, new[] { tex }, IconKind.Any);
                Debug.Log($"[IconSetup] Default icon -> {IconPath}");

                var target = NamedBuildTarget.iOS;
                PlatformIconKind[] kinds =
                {
                    iOSPlatformIconKind.Application,
                    iOSPlatformIconKind.Spotlight,
                    iOSPlatformIconKind.Settings,
                    iOSPlatformIconKind.Notification,
                    iOSPlatformIconKind.Marketing,
                };
                int total = 0;
                foreach (var kind in kinds)
                {
                    var icons = PlayerSettings.GetPlatformIcons(target, kind);
                    foreach (var icon in icons)
                    {
                        var layers = new Texture2D[Math.Max(1, icon.maxLayerCount)];
                        for (int i = 0; i < layers.Length; i++) layers[i] = tex;
                        icon.SetTextures(layers);
                        total++;
                        Debug.Log($"[IconSetup] iOS {kind} {icon.width}x{icon.height} -> {IconPath}");
                    }
                    PlayerSettings.SetPlatformIcons(target, kind, icons);
                }
                if (total == 0) throw new Exception("No iOS platform icon slots returned; is the iOS module installed?");

                AssetDatabase.SaveAssets();
                Debug.Log($"[IconSetup] Done: {total} iOS icon slots set.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[IconSetup] FAILED: {e.Message}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (imp == null) throw new Exception($"No TextureImporter for {IconPath}");
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = false;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.alphaIsTransparency = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            var ios = imp.GetPlatformTextureSettings("iPhone");
            ios.overridden = true;
            ios.format = TextureImporterFormat.RGBA32;
            ios.textureCompression = TextureImporterCompression.Uncompressed;
            ios.maxTextureSize = 2048;
            imp.SetPlatformTextureSettings(ios);
            imp.SaveAndReimport();
            Debug.Log($"[IconSetup] Importer: no mipmaps, uncompressed (iPhone RGBA32), no NPOT scale.");
        }
    }
}
#endif
