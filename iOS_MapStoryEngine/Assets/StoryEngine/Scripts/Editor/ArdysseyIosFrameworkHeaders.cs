#if UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

internal static class ArdysseyIosFrameworkHeaders
{
    [PostProcessBuild(1000)]
    public static void FixPublicHeaderIncludes(BuildTarget target, string outputPath)
    {
        if (target != BuildTarget.iOS)
            return;

        // Xcode's module verifier expects framework-qualified includes in public headers.
        RewriteIncludes(outputPath, "UnityFramework/UnityFramework.h",
            "UnityAppController.h", "UndefinePlatforms.h", "RedefinePlatforms.h");
        RewriteIncludes(outputPath, "Classes/UnityAppController.h", "RenderPluginDelegate.h");
        RewriteIncludes(outputPath, "Classes/PluginBase/RenderPluginDelegate.h", "LifeCycleListener.h");
        ConfigureFramework(outputPath);
    }

    private static void ConfigureFramework(string outputPath)
    {
        string path = PBXProject.GetPBXProjectPath(outputPath);
        var project = new PBXProject();
        project.ReadFromFile(path);
        string app = project.GetUnityMainTargetGuid();
        string framework = project.GetUnityFrameworkTargetGuid();

        // The legacy Mapbox postprocessor targets the app. Its native code is built in UnityFramework.
        project.RemoveFrameworkFromProject(app, "ARKit.framework");
        project.AddFrameworkToProject(framework, "ARKit.framework", false);
        project.AddBuildProperty(framework, "HEADER_SEARCH_PATHS",
            "$(SRCROOT)/Libraries/Mapbox/Core/Plugins/iOS/MapboxMobileEvents/include");
        project.UpdateBuildProperty(app, "OTHER_CFLAGS", new string[0], new[] { "-mno-thumb" });
        project.WriteToFile(path);
    }

    private static void RewriteIncludes(string exportPath, string relativePath, params string[] headers)
    {
        string path = Path.Combine(exportPath, relativePath);
        if (!File.Exists(path))
            throw new BuildFailedException("Could not find the exported UnityFramework header: " + relativePath);

        string original = File.ReadAllText(path);
        string updated = original;
        foreach (string header in headers)
        {
            string quoted = "\"" + header + "\"";
            string qualified = "<UnityFramework/" + header + ">";
            if (updated.IndexOf(quoted, StringComparison.Ordinal) >= 0)
                updated = updated.Replace(quoted, qualified);
            else if (updated.IndexOf(qualified, StringComparison.Ordinal) < 0)
                throw new BuildFailedException("Could not find " + header + " in " + relativePath);
        }

        if (updated != original)
            File.WriteAllText(path, updated);
    }
}
#endif
