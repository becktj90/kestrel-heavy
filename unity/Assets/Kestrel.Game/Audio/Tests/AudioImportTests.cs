using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Kestrel.Game.Audio.Tests
{
    public class AudioImportTests
    {
        const string Dir = "Assets/Kestrel.Game/Audio/Runtime/Resources/Sfx";

        [Test]
        public void SfxMp3sAreMonoWithAnIosOverrideAndThemeIsAbsent()
        {
            var files = Directory.GetFiles(Dir, "*.mp3").Select(f => Path.GetFileNameWithoutExtension(f)).OrderBy(s => s).ToArray();
            var expected = SfxCatalog.Files.OrderBy(s => s).ToArray();
            CollectionAssert.AreEqual(expected, files);
            Assert.IsFalse(File.Exists(Dir + "/theme.mp3"));
            Assert.AreEqual(0, Directory.GetFiles(Dir, "*.ogg").Length);
            foreach (var name in SfxCatalog.Files)
            {
                var path = Dir + "/" + name + ".mp3";
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                Assert.IsNotNull(imp, path);
                Assert.IsTrue(imp.forceToMono, name);
                bool ios = imp.ContainsSampleSettingsOverride("iOS");
                bool iphone = imp.ContainsSampleSettingsOverride("iPhone");
                Assert.IsTrue(ios || iphone, name + " missing iOS override");
                var settings = imp.GetOverrideSampleSettings(ios ? "iOS" : "iPhone");
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, settings.loadType, name);
                Assert.IsTrue(
                    settings.compressionFormat == AudioCompressionFormat.MP3
                    || settings.compressionFormat == AudioCompressionFormat.AAC, name);
                Assert.IsTrue(settings.preloadAudioData, name);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(path), name);
            }
        }

        [Test]
        public void OriginalMusicBedsDecompressAndDoNotIncludeTheExcludedTheme()
        {
            const string dir = "Assets/Kestrel.Game/Audio/Runtime/Resources/Music";
            foreach (var name in new[] { "kestrel-title", "kestrel-designer", "kestrel-flight" })
            {
                var path = dir + "/" + name + ".ogg";
                Assert.IsTrue(File.Exists(path), path);
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                Assert.IsNotNull(imp, path);
                Assert.IsFalse(imp.forceToMono, name);
                var settings = imp.defaultSampleSettings;
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, settings.loadType, name);
                Assert.AreEqual(AudioCompressionFormat.Vorbis, settings.compressionFormat, name);
                Assert.IsTrue(settings.preloadAudioData, name);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(path), name);
            }
            Assert.IsFalse(File.Exists(dir + "/theme.ogg"));
            Assert.IsFalse(File.Exists(dir + "/theme.mp3"));
            Assert.AreEqual(3, Directory.GetFiles(dir, "*.ogg").Length);
        }
    }
}
