// Command-line client builds for end-to-end tests. The server is built by the Edgegap plugin (EdgegapPluginBuild).
// Usage (project closed in the Editor):
//   <Unity> -batchmode -projectPath <project> -executeMethod EdgegapClientBuild.Build -edgegapClientTarget webgl|win64|mac|linux
// Output: Builds/Client/<target>/. Switches the active build target (slow the first time).
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class EdgegapClientBuild
{
    public static void Build()
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-edgegapClientTarget");
        string name = i >= 0 && i + 1 < args.Length ? args[i + 1].ToLowerInvariant() : "webgl";

        (BuildTarget target, string path) = name switch
        {
            "win64" => (BuildTarget.StandaloneWindows64, "Builds/Client/win64/Game.exe"),
            "mac" => (BuildTarget.StandaloneOSX, "Builds/Client/mac/Game.app"),
            "linux" => (BuildTarget.StandaloneLinux64, "Builds/Client/linux/Game.x86_64"),
            _ => (BuildTarget.WebGL, "Builds/Client/webgl"),
        };

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Fail("No enabled scenes in Build Settings.");
            return;
        }

        if (target != BuildTarget.WebGL)
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            target = target,
            subtarget = target == BuildTarget.WebGL ? 0 : (int)StandaloneBuildSubtarget.Player,
            locationPathName = path,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            Fail($"Client build {report.summary.result} with {report.summary.totalErrors} error(s).");
            return;
        }
        Debug.Log($"EdgegapClientBuild | Succeeded: {path}");
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    private static void Fail(string message)
    {
        Debug.LogError($"EdgegapClientBuild | {message}");
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
