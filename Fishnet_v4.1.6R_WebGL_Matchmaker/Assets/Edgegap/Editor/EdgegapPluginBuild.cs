// Command-line entry point for the Edgegap plugin's "Build server" button (Tools > Edgegap Server Hosting).
// Same output: a Linux Dedicated Server build in Builds/EdgegapServer/ServerBuild, which the plugin's Dockerfile copies.
// Usage (project closed in the Editor):
//   <Unity> -batchmode -projectPath <project> -executeMethod EdgegapPluginBuild.BuildServer -logFile build-server.log
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class EdgegapPluginBuild
{
    public static void BuildServer()
    {
        // EdgegapBuildUtils is internal to the plugin assembly.
        Type utils = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("Edgegap.EdgegapBuildUtils"))
            .FirstOrDefault(t => t != null);
        if (utils == null)
        {
            Debug.LogError("EdgegapPluginBuild | Edgegap plugin (com.edgegap.unity-servers-plugin) not installed.");
            EditorApplication.Exit(1);
            return;
        }

        var report = (BuildReport)utils.GetMethod("BuildServer").Invoke(null, new object[] { "EdgegapServer" });
        Debug.Log($"EdgegapPluginBuild | {report.summary.result}: {report.summary.outputPath}");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
#endif
