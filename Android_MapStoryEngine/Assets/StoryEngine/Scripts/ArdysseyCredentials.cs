using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class ArdysseyCredentials
{
    public const string PlacesEnvironmentVariable = "GOOGLE_PLACES_API_KEY";
    public const string MapboxEnvironmentVariable = "MAPBOX_ACCESS_TOKEN";
    public const string LocalFileName = ".ardyssey.local.json";
    public const string AndroidAssetName = "ardyssey-client-config.json";

    public string GooglePlacesApiKey;
    public string MapboxAccessToken;

    public static ArdysseyCredentials Load()
    {
#if UNITY_EDITOR
        string googleKey = ReadEnvironment(PlacesEnvironmentVariable);
        string mapboxToken = ReadEnvironment(MapboxEnvironmentVariable);
        string path = Path.Combine(Application.dataPath, "..", LocalFileName);
        ArdysseyCredentials local = null;
        if ((googleKey == null || mapboxToken == null) && File.Exists(path))
            local = JsonUtility.FromJson<ArdysseyCredentials>(File.ReadAllText(path));

        return new ArdysseyCredentials
        {
            GooglePlacesApiKey = googleKey ?? local?.GooglePlacesApiKey?.Trim(),
            MapboxAccessToken = mapboxToken ?? local?.MapboxAccessToken?.Trim()
        };
#elif UNITY_ANDROID
        return AndroidCredentials.Value;
#else
        return new ArdysseyCredentials
        {
            GooglePlacesApiKey = ReadEnvironment(PlacesEnvironmentVariable),
            MapboxAccessToken = ReadEnvironment(MapboxEnvironmentVariable)
        };
#endif
    }

    private static string ReadEnvironment(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static readonly Lazy<ArdysseyCredentials> AndroidCredentials =
        new Lazy<ArdysseyCredentials>(ReadAndroidAsset);

    private static ArdysseyCredentials ReadAndroidAsset()
    {
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var assets = activity.Call<AndroidJavaObject>("getAssets"))
            using (var stream = assets.Call<AndroidJavaObject>("open", AndroidAssetName))
            using (var scanner = new AndroidJavaObject("java.util.Scanner", stream, "UTF-8"))
            {
                try
                {
                    using (scanner.Call<AndroidJavaObject>("useDelimiter", "\\A"))
                    {
                        string json = scanner.Call<string>("next");
                        return JsonUtility.FromJson<ArdysseyCredentials>(json);
                    }
                }
                finally
                {
                    scanner.Call("close");
                }
            }
        }
        catch (AndroidJavaException)
        {
            Debug.LogWarning("Android client configuration is missing or unreadable. Rebuild ARdyssey with its credentials configured.");
            return new ArdysseyCredentials();
        }
    }
#endif
}
