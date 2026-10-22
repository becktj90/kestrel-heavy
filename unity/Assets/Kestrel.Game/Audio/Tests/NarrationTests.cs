using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Kestrel.Game.Audio.Tests
{
    public class NarrationImportTests
    {
        const string Root = "Assets/Kestrel.Game/Audio/Runtime/Resources/Narration";

        [Test]
        public void NarrationMp3sStayCompressedWithAnIosOverride()
        {
            var files = Directory.GetFiles(Root, "*.mp3", SearchOption.AllDirectories);
            Assert.AreEqual(147, files.Length);
            foreach (var path in files)
            {
                var asset = path.Replace('\\', '/');
                var imp = (AudioImporter)AssetImporter.GetAtPath(asset);
                Assert.IsNotNull(imp, asset);
                Assert.IsTrue(imp.forceToMono, asset);
                bool ios = imp.ContainsSampleSettingsOverride("iOS");
                bool iphone = imp.ContainsSampleSettingsOverride("iPhone");
                Assert.IsTrue(ios || iphone, asset);
                var settings = imp.GetOverrideSampleSettings(ios ? "iOS" : "iPhone");
                Assert.AreEqual(AudioClipLoadType.CompressedInMemory, settings.loadType, asset);
                Assert.IsTrue(
                    settings.compressionFormat == AudioCompressionFormat.MP3
                    || settings.compressionFormat == AudioCompressionFormat.AAC, asset);
            }
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/mission-control/liftoff.2.mp3"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/manifest.json"));
        }
    }
}
