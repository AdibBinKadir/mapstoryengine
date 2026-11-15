#if UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

public static class ArdysseyIosCredentialsBuild
{
    [PostProcessBuild]
    public static void CopyCredentials(BuildTarget target, string outputPath)
    {
        if (target != BuildTarget.iOS)
            return;

        ArdysseyCredentials credentials = ArdysseyCredentials.Load();
        if (string.IsNullOrWhiteSpace(credentials.GooglePlacesApiKey)
            || string.IsNullOrWhiteSpace(credentials.MapboxAccessToken)
            || !credentials.MapboxAccessToken.StartsWith("pk.", StringComparison.Ordinal))
            throw new BuildFailedException("Configure GOOGLE_PLACES_API_KEY and a public MAPBOX_ACCESS_TOKEN before building for iOS.");

        string streamingAssets = Path.Combine(outputPath, "Data", "Raw");
        Directory.CreateDirectory(streamingAssets);
        File.WriteAllText(Path.Combine(streamingAssets, ArdysseyCredentials.ClientAssetName),
            JsonUtility.ToJson(credentials));
    }
}
#endif
