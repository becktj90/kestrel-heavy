using System;
using UnityEditor;
using UnityEngine;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Speech takes are a few seconds of mono MP3. Keep them compressed in memory (not PCM)
    /// and override iOS to MP3 so the device does not transcode the whole library to Vorbis.
    /// </summary>
    public sealed class NarrationImportPostprocessor : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            var path = assetPath.Replace('\\', '/');
            if (path.IndexOf("/Resources/Narration/", StringComparison.Ordinal) < 0) return;
            if (!path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return;
            var imp = (AudioImporter)assetImporter;
            imp.forceToMono = true;
            imp.loadInBackground = false;
            imp.ambisonic = false;
            var settings = imp.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
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
