using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class ArdysseyCredentials
{
    public const string PlacesEnvironmentVariable = "GOOGLE_PLACES_API_KEY";
    public const string MapboxEnvironmentVariable = "MAPBOX_ACCESS_TOKEN";
    public const string LocalFileName = ".ardyssey.local.json";
    public const string ClientAssetName = "ardyssey-client-config.json";

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
#elif UNITY_IOS
        string path = Path.Combine(Application.streamingAssetsPath, ClientAssetName);
        return File.Exists(path)
            ? JsonUtility.FromJson<ArdysseyCredentials>(File.ReadAllText(path))
            : new ArdysseyCredentials();
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

}
