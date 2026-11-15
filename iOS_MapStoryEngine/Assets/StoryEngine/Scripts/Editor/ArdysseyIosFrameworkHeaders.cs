#if UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;

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
