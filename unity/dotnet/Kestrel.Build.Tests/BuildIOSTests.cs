using System;
using System.IO;
using System.Linq;
using Kestrel.Slice.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;

[TestFixture, NonParallelizable]
public class BuildIOSTests
{
    string previousDirectory, directory, previousNumber, previousPath;
    [SetUp] public void SetUp() {
        previousDirectory=Directory.GetCurrentDirectory();
        previousNumber=Environment.GetEnvironmentVariable("KH_BUILD_NUMBER");
        previousPath=Environment.GetEnvironmentVariable("KH_BUILD_PATH");
        directory=Path.Combine(Path.GetTempPath(), "kh-build-test-"+Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(directory,"Assets/Scenes/Shell"));
        Directory.SetCurrentDirectory(directory);
        EditorBuildSettings.scenes=new[] { "Boot", "Home", "Design", "Flight" }.Select(name => {
            var path=$"Assets/Scenes/Shell/{name}.unity";
            File.WriteAllText(path, "test scene");
            return new EditorBuildSettingsScene(path,true);
        }).ToArray();
        Environment.SetEnvironmentVariable("KH_BUILD_PATH", Path.Combine(directory,"export"));
        Environment.SetEnvironmentVariable("KH_BUILD_NUMBER", "101");
        BuildPipeline.Calls=0; BuildPipeline.Result=BuildResult.Succeeded;
        UnityEngine.Application.isBatchMode=true; EditorApplication.ExitCode=-1;
    }
    [TearDown] public void TearDown() {
        Directory.SetCurrentDirectory(previousDirectory);
        Environment.SetEnvironmentVariable("KH_BUILD_NUMBER",previousNumber);
        Environment.SetEnvironmentVariable("KH_BUILD_PATH",previousPath);
        Directory.Delete(directory,true);
    }
    [TestCase("99"), TestCase("abc"), TestCase("-1"), TestCase("2147483648")]
    public void InvalidNumberDoesNotBuild(string number) {
        Environment.SetEnvironmentVariable("KH_BUILD_NUMBER",number);
        Assert.Throws<ArgumentException>(() => BuildScript.BuildIOS());
        Assert.That(BuildPipeline.Calls,Is.Zero);
    }
    [Test] public void MissingSceneDoesNotBuild() {
        File.Delete(EditorBuildSettings.scenes[2].path);
        Assert.Throws<InvalidOperationException>(() => BuildScript.BuildIOS());
        Assert.That(BuildPipeline.Calls,Is.Zero);
    }
    [Test] public void DisabledSceneDoesNotBuild() {
        EditorBuildSettings.scenes[3].enabled=false;
        Assert.Throws<InvalidOperationException>(() => BuildScript.BuildIOS());
        Assert.That(BuildPipeline.Calls,Is.Zero);
    }
    [Test] public void EmptySceneListDoesNotExportSandbox() {
        EditorBuildSettings.scenes=Array.Empty<EditorBuildSettingsScene>();
        Assert.Throws<InvalidOperationException>(() => BuildScript.BuildIOS());
        Assert.That(BuildPipeline.Calls,Is.Zero);
    }
    [Test] public void BootMustBeFirst() {
        Array.Reverse(EditorBuildSettings.scenes);
        Assert.Throws<InvalidOperationException>(() => BuildScript.BuildIOS());
        Assert.That(BuildPipeline.Calls,Is.Zero);
    }
    [TestCase(BuildResult.Succeeded,0), TestCase(BuildResult.Failed,1)]
    public void ValidBuildUsesShippingScenesAndPropagatesResult(BuildResult result,int exitCode) {
        BuildPipeline.Result=result; BuildScript.BuildIOS();
        Assert.That(BuildPipeline.Calls,Is.EqualTo(1));
        Assert.That(BuildPipeline.LastOptions.scenes,Is.EqualTo(EditorBuildSettings.scenes.Select(s=>s.path).ToArray()));
        Assert.That(BuildPipeline.LastOptions.target,Is.EqualTo(BuildTarget.iOS));
        Assert.That(PlayerSettings.iOS.buildNumber,Is.EqualTo("101"));
        Assert.That(EditorApplication.ExitCode,Is.EqualTo(exitCode));
    }
}
