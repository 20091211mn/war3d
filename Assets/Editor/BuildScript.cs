using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

// Called by GitHub Actions (GameCI) via: buildMethod: BuildScript.BuildAndroid
public static class BuildScript
{
    static string GetArg(string name)
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++)
            if (a[i] == name) return a[i + 1];
        return null;
    }

    public static void BuildAndroid()
    {
        string outDir = GetArg("-customBuildPath") ?? "build/Android";
        string name = GetArg("-customBuildName") ?? "war3d";
        Directory.CreateDirectory(outDir);

        // The game builds itself at runtime, so the scene only needs to exist (empty).
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");

        PlayerSettings.companyName = "War3D";
        PlayerSettings.productName = "War 3D";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.war3d.game");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle = false;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = Path.Combine(outDir, name + ".apk"),
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Console.Error.WriteLine("Build failed: " + report.summary.result);
            EditorApplication.Exit(1);
        }
    }
}
