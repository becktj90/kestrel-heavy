using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Kestrel.Game.Tests
{
    // Cloud builds start from a clean checkout: every Build Settings scene and the generated Resources the
    // player loads must be committed (tools/check-unity-build-scenes.sh enforces git tracking in CI).
    public class BuildScenesTests
    {
        static readonly string[] RequiredResources =
        {
            "Assets/Resources/UI/KH_PanelSettings.asset",
            "Assets/Resources/UI/KH_ShellPanelSettings.asset",
            "Assets/Resources/Materials/KH_Body.mat",
            "Assets/Resources/Materials/KH_LitEmission.mat",
            "Assets/Resources/Materials/KH_Plume.mat",
        };

        static string[] GitIgnored()
        {
            var lines = File.Exists(".gitignore") ? File.ReadAllLines(".gitignore") : new string[0];
            return System.Array.ConvertAll(lines, l => l.Trim().TrimStart('/'));
        }

        [Test]
        public void EveryBuildSceneExistsAndIsNotIgnored()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.That(scenes.Length, Is.GreaterThan(0), "no scenes in Build Settings");
            var ignored = GitIgnored();
            foreach (var s in scenes)
            {
                Assert.That(File.Exists(s.path), Is.True, $"Build Settings scene missing: {s.path}");
                Assert.That(File.Exists(s.path + ".meta"), Is.True, $"scene .meta missing: {s.path}");
                Assert.That(System.Array.IndexOf(ignored, s.path), Is.EqualTo(-1), $"Build Settings scene is gitignored: {s.path}");
            }
        }

        [Test]
        public void SliceSandboxIsNotInBuildSettings()
        {
            foreach (var s in EditorBuildSettings.scenes)
                Assert.That(s.path, Is.Not.EqualTo("Assets/Scenes/Slice.unity"));
        }

        [Test]
        public void GeneratedResourcesAreCommitted()
        {
            foreach (var p in RequiredResources)
            {
                Assert.That(File.Exists(p), Is.True, $"missing {p}");
                Assert.That(File.Exists(p + ".meta"), Is.True, $"missing {p}.meta");
            }
        }

        [Test]
        public void LitKeepersRequestTheInstancedPlayerVariant()
        {
            // RenderMeshInstanced draws nothing on device unless the built URP/Lit material asks for INSTANCING_ON.
            string body = File.ReadAllText("Assets/Resources/Materials/KH_Body.mat");
            string lamps = File.ReadAllText("Assets/Resources/Materials/KH_LitEmission.mat");
            Assert.That(body, Does.Contain("m_EnableInstancingVariants: 1"));
            Assert.That(lamps, Does.Contain("m_EnableInstancingVariants: 1"));
            Assert.That(lamps, Does.Contain("_EMISSION"));
        }
    }
}
