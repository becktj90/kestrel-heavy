using System;
using UnityEditor;
using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// iOS-friendly import for the short NASA trims: mono, decompress on load, MP3 on device.
    /// Clips are about 0.4–3 s at 22.05 kHz, so PCM-at-play is cheaper than decoding each one-shot.
    /// </summary>
    public sealed class SfxImportPostprocessor : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            var path = assetPath.Replace('\\', '/');
            if (path.IndexOf("/Resources/Sfx/", StringComparison.Ordinal) < 0) return;
            if (!path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return;
            var imp = (AudioImporter)assetImporter;
            imp.forceToMono = true;
            imp.loadInBackground = false;
            imp.ambisonic = false;
            var settings = imp.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            imp.defaultSampleSettings = settings;
            var ios = settings;
            ios.compressionFormat = AudioCompressionFormat.MP3;
            ios.quality = 0.7f;
            imp.SetOverrideSampleSettings("iOS", ios);
            imp.SetOverrideSampleSettings("iPhone", ios);
        }
    }
}
