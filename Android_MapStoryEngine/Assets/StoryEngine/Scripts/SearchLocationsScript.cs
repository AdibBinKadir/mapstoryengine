using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class SearchLocationsScript : MonoBehaviour
{
    private const string NearbySearchUrl = "https://places.googleapis.com/v1/places:searchNearby";
    private const string PlaceFields = "places.displayName,places.location,places.types";

    [Tooltip("Set the number of local sites that will be visited as part of the story")]
    [Range(2, 20)]
    public int noLocationsToVisit;

    [Tooltip("Set the search radius in metres around the user's position")]
    public string searchRadius = "250";

    public static SearchLocationsScript instance;

    [SerializeField] private AbstractMap _map;
    [SerializeField] private GameObject ARPlayerMapMarker;
    [SerializeField] private GameObject StoryManager_15;
    [SerializeField] private GameObject ProblemPanel_4;

    public JSONNode jsonResult;
    public bool SearchComplete { get; private set; }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Vector2d position = _map.WorldToGeoPosition(ARPlayerMapMarker.transform.position);
        StartCoroutine(SearchNearbyPlaces(position));
    }

    private IEnumerator SearchNearbyPlaces(Vector2d position)
    {
        string apiKey = ArdysseyCredentials.Load().GooglePlacesApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ShowSearchProblem("Configure GOOGLE_PLACES_API_KEY for Places API (New).");
            yield break;
        }

        double radius;
        if (!double.TryParse(searchRadius, NumberStyles.Float, CultureInfo.InvariantCulture, out radius)
            || !(radius > 0 && radius <= 50000))
        {
            ShowSearchProblem("Set a search radius greater than 0 and at most 50000 metres.");
            yield break;
        }

        var body = new JSONObject
        {
            ["maxResultCount"] = 20,
            ["rankPreference"] = "POPULARITY",
            ["locationRestriction"] = new JSONObject
            {
                ["circle"] = new JSONObject
                {
                    ["center"] = new JSONObject
                    {
                        ["latitude"] = position.x,
                        ["longitude"] = position.y
                    },
                    ["radius"] = radius
                }
            }
        };

        using (var request = new UnityWebRequest(NearbySearchUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString()));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 15;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Goog-Api-Key", apiKey);
            request.redirectLimit = 0;
            request.SetRequestHeader("X-Goog-FieldMask", PlaceFields);

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                string failure = request.isNetworkError
                    ? "Could not reach Google Places. Check the connection."
                    : $"Google Places returned HTTP {request.responseCode}. Check API enablement, key restrictions, billing, and quota.";
                ShowSearchProblem(failure);
                yield break;
            }

            JSONArray results;
            try
            {
                results = ConvertPlaces(JSON.Parse(request.downloadHandler.text));
            }
            catch (Exception)
            {
                ShowSearchProblem("Google Places returned an unreadable response.");
                yield break;
            }

            if (results.Count < noLocationsToVisit)
            {
                ShowSearchProblem($"Google Places returned {results.Count} usable places within {radius} m. The story needs {noLocationsToVisit}.");
                yield break;
            }

            jsonResult = new JSONObject { ["results"] = results };
            StoryManager_15.GetComponent<StoryManager>().LocationsToVisit = jsonResult;
            SearchComplete = true;
        }
    }

    // Keep the result shape StoryManager uses for stop selection.
    private static JSONArray ConvertPlaces(JSONNode response)
    {
        var results = new JSONArray();
        var places = response?["places"] as JSONArray;
        if (places == null)
            return results;

        foreach (JSONNode place in places.Children)
        {
            var displayName = place["displayName"] as JSONObject;
            var location = place["location"] as JSONObject;
            var types = place["types"] as JSONArray;
            if (displayName == null || location == null || types == null || types.Count == 0)
                continue;

            string name = displayName["text"].Value;
            if (string.IsNullOrWhiteSpace(name) || !location["latitude"].IsNumber
                || !location["longitude"].IsNumber)
                continue;

            results.Add(new JSONObject
            {
                ["name"] = name,
                ["types"] = types,
                ["geometry"] = new JSONObject
                {
                    ["location"] = new JSONObject
                    {
                        ["lat"] = location["latitude"],
                        ["lng"] = location["longitude"]
                    }
                }
            });
        }

        return results;
    }

    private void ShowSearchProblem(string message)
    {
        Debug.LogWarning(message, this);
        ProblemPanel_4.GetComponent<Problem>().enabled = true;
        ProblemPanel_4.SetActive(true);
        SearchComplete = true;
    }
}
