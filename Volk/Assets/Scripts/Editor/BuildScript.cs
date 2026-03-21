using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        string outputPath = "/tmp/volk_build/volk.apk";

        // Ensure output directory exists
        System.IO.Directory.CreateDirectory("/tmp/volk_build");

        var buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/CombatTest.unity" },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes at {outputPath}");
        }
        else
        {
            Debug.LogError($"Build failed: {summary.result}");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error)
                        Debug.LogError(msg.content);
                }
            }
        }
    }
}
