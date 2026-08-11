using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// One-click build pipeline for TeamTilt.
/// Access via: TeamTilt > Build Windows / Build Android in the Unity menu bar.
/// Builds all scenes enabled in File > Build Settings.
/// </summary>
public class BuildScript
{
    private const string WindowsBuildPath = "Builds/Windows/TeamTilt.exe";
    private const string AndroidBuildPath = "Builds/Android/TeamTilt.apk";

    [MenuItem("TeamTilt/Build Windows")]
    public static void BuildWindows()
    {
        ExecuteBuild("[BuildScript] Starting Windows x64 build...", WindowsBuildPath, BuildTarget.StandaloneWindows64);
    }

    [MenuItem("TeamTilt/Build Windows", validate = true)]
    public static bool ValidateBuildWindows() => !BuildPipeline.isBuildingPlayer;

    // ──────────────────────────────────────────────────────
    // Android Build
    // ──────────────────────────────────────────────────────

    [MenuItem("TeamTilt/Build Android")]
    public static void BuildAndroid()
    {
        // Configure Android-specific settings before building
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false; // APK, not AAB
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        Debug.Log("[BuildScript] Android settings: ARM64, IL2CPP, APK output.");
        ExecuteBuild("[BuildScript] Starting Android ARM64 build...", AndroidBuildPath, BuildTarget.Android);
    }

    [MenuItem("TeamTilt/Build Android", validate = true)]
    public static bool ValidateBuildAndroid() => !BuildPipeline.isBuildingPlayer;

    private static void ExecuteBuild(string logPrefix, string outputPath, BuildTarget target)
    {
        Debug.Log(logPrefix);

        var scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] No scenes in Build Settings! Add scenes via File > Build Settings.");
            return;
        }

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath));

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] ✅ Build succeeded! Size: {summary.totalSize / 1024 / 1024} MB → {outputPath}");
            EditorUtility.RevealInFinder(outputPath);
        }
        else
        {
            Debug.LogError($"[BuildScript] ❌ Build FAILED after {summary.totalErrors} error(s).");
            foreach (var step in report.steps)
                foreach (var msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.LogError($"  [{step.name}] {msg.content}");
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }
}
