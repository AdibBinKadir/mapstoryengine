#if UNITY_ANDROID
using System;
using System.IO;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

public sealed class ArdysseyAndroidCredentialsBuild : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var credentials = ArdysseyCredentials.Load();
        if (string.IsNullOrWhiteSpace(credentials.GooglePlacesApiKey)
            || string.IsNullOrWhiteSpace(credentials.MapboxAccessToken))
            throw new BuildFailedException("Configure GOOGLE_PLACES_API_KEY and MAPBOX_ACCESS_TOKEN before building ARdyssey.");
        if (!credentials.MapboxAccessToken.StartsWith("pk.", StringComparison.Ordinal))
            throw new BuildFailedException("Configure a public Mapbox access token (pk.) for the Android APK.");

        // Unity 2019.4 passes the unityLibrary module directory to this callback.
        string assets = Path.Combine(path, "src", "main", "assets");
        Directory.CreateDirectory(assets);
        File.WriteAllText(Path.Combine(assets, ArdysseyCredentials.AndroidAssetName),
            JsonUtility.ToJson(credentials));
    }
}
#endif
