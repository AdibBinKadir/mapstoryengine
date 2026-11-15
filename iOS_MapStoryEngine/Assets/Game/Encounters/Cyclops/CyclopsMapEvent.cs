using System;
using System.Collections.Generic;
using Mapbox.Examples;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Utils;
using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Odyssey.Cyclops
{
    public sealed class CyclopsMapEvent : MonoBehaviour
    {
        private static readonly HashSet<string> PublicTypes = new HashSet<string>
        {
            "park", "plaza", "library", "university", "museum", "tourist_attraction",
            "community_center", "cultural_center", "city_hall", "botanical_garden",
            "playground", "sports_complex", "stadium", "art_gallery", "visitor_center"
        };

        private StoryManager story;
        private AbstractMap map;
        private Camera mapCamera;
        private TouchCamera mapTouch;
        private ARSessionOrigin arOrigin;
        private ARSession arSession;
        private GameObject mapTools;
        private Canvas storyCanvas;
        private GraphicRaycaster storyRaycaster;
        private ILocationProvider locationProvider;
        private Location location;
        private CyclopsLocation gate = new CyclopsLocation();
        private Vector2d? cave;
        private JSONNode consideredResults;
        private bool cleared;
        private CyclopsEncounter activeFight;
        private Canvas overlay;
        private RectTransform safePanel;
        private Text distanceLabel;
        private RectTransform marker;
        private Button enter;
        private Title title;
        private Button continueJourney;
        private Button testButton;
        private bool journeyStarting;
        private bool testFight;
        private bool savedMapCamera;
        private bool savedStory;
        private bool savedMapTouch;
        private bool savedArSession;
        private bool savedArCamera;
        private bool savedCanvas;
        private bool savedRaycaster;

        private static double Now => (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MapStory")
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var manager = root.GetComponentInChildren<StoryManager>(true);
                if (manager != null && manager.GetComponent<CyclopsMapEvent>() == null)
                {
                    manager.gameObject.AddComponent<CyclopsMapEvent>().Bind(scene, manager);
                    return;
                }
            }
        }

        private void Bind(Scene scene, StoryManager manager)
        {
            story = manager;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (map == null) map = root.GetComponentInChildren<AbstractMap>(true);
                if (title == null) title = root.GetComponentInChildren<Title>(true);
                if (mapTouch == null) mapTouch = root.GetComponentInChildren<TouchCamera>(true);
                if (arOrigin == null) arOrigin = root.GetComponentInChildren<ARSessionOrigin>(true);
                if (arSession == null) arSession = root.GetComponentInChildren<ARSession>(true);
                if (mapTools != null)
                    continue;
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                    if (rect.name == "MapToolsPanel_5")
                    {
                        mapTools = rect.gameObject;
                        storyCanvas = rect.GetComponentInParent<Canvas>();
                        break;
                    }
            }

            mapCamera = mapTouch == null ? null : mapTouch.GetComponent<Camera>();
            if (map == null || mapCamera == null || arOrigin == null || arOrigin.camera == null
                || arSession == null || storyCanvas == null)
            {
                Debug.LogWarning("Cyclops could not find the map or rear AR camera.", this);
                enabled = false;
                return;
            }

            storyRaycaster = storyCanvas.GetComponent<GraphicRaycaster>();
            CreateMapHud();
            ConfigureTitleMenu();
            HideLegacyButton(mapTools.transform.Find("ManualTrigger_Button_5_3"));
        }

        private void Update()
        {
            if (overlay == null)
                return;

            if (journeyStarting && continueJourney.isActiveAndEnabled)
            {
                journeyStarting = false;
                continueJourney.onClick.Invoke();
            }

            if (locationProvider == null)
            {
                locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
                if (locationProvider != null)
                {
                    locationProvider.OnLocationUpdated += ReceiveLocation;
                    ReceiveLocation(locationProvider.CurrentLocation);
                }
            }

            bool show = MapIsVisible() && !cleared;
            if (show && !cave.HasValue)
                TryPlaceCave();
            bool visible = show && cave.HasValue;
            overlay.gameObject.SetActive(visible);
            if (!visible)
                return;

            CyclopsView.FitSafeArea(safePanel);
            bool usable = CyclopsLocation.IsUsable(location, Now);
            distanceLabel.text = usable
                ? "Cyclops Cave  ·  " + Math.Round(CyclopsLocation.Distance(location.LatitudeLongitude, cave.Value)) + " m"
                : "Cyclops Cave  ·  Waiting for accurate GPS";
            enter.gameObject.SetActive(gate.CanEnter(Now));

            Vector3 pixel = mapCamera.WorldToScreenPoint(map.GeoToWorldPosition(cave.Value, false) + Vector3.up);
            marker.gameObject.SetActive(pixel.z > 0 && Screen.safeArea.Contains(pixel));
            if (marker.gameObject.activeSelf)
                marker.anchorMin = marker.anchorMax = new Vector2(pixel.x / Screen.width, pixel.y / Screen.height);
        }

        private bool MapIsVisible()
        {
            return activeFight == null && story.enabled && story.startVar
                && mapCamera.enabled && mapTools.activeInHierarchy;
        }

        private void TryPlaceCave()
        {
            JSONNode results = story.LocationsToVisit?["results"];
            if (results == null || ReferenceEquals(results, consideredResults)
                || !CyclopsLocation.IsUsable(location, Now) || story.MapSites == null)
                return;

            consideredResults = results;
            var landmarks = new List<Vector2d>();
            foreach (JSONNode result in results.Children)
            {
                bool isPublic = false;
                foreach (JSONNode type in result["types"].Children)
                    if (PublicTypes.Contains(type.Value))
                    {
                        isPublic = true;
                        break;
                    }
                JSONNode position = result["geometry"]["location"];
                if (isPublic && position["lat"].IsNumber && position["lng"].IsNumber)
                {
                    var point = new Vector2d(position["lat"].AsDouble, position["lng"].AsDouble);
                    if (Math.Abs(point.x) <= 90 && Math.Abs(point.y) <= 180)
                        landmarks.Add(point);
                }
            }

            var storyStops = new List<Vector2d>();
            foreach (GameObject stop in story.MapSites)
                if (stop != null)
                    storyStops.Add(map.WorldToGeoPosition(stop.transform.position));

            var candidates = CyclopsLocation.Candidates(landmarks, storyStops, location.LatitudeLongitude);
            if (candidates.Count > 0)
            {
                cave = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                gate.Update(location, cave.Value, Now);
            }
        }

        private void ReceiveLocation(Location fix)
        {
            location = fix;
            if (cave.HasValue)
                gate.Update(fix, cave.Value, Now);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                gate = new CyclopsLocation();
        }

        private void EnterCave()
        {
            if (MapIsVisible() && gate.CanEnter(Now) && !cleared)
                OpenFight(false);
        }

        private void TestCyclops()
        {
            if (title.isActiveAndEnabled && !journeyStarting && activeFight == null)
                OpenFight(true);
        }

        private void OpenFight(bool testing)
        {
            var prefab = Resources.Load<CyclopsEncounter>("Odyssey/CyclopsEncounter");
            if (prefab == null)
            {
                Debug.LogError("Cyclops encounter prefab is missing.", this);
                return;
            }

            testFight = testing;
            savedMapCamera = mapCamera.enabled;
            savedStory = story.enabled;
            savedMapTouch = mapTouch.enabled;
            savedArSession = arSession.enabled;
            savedArCamera = arOrigin.camera.enabled;
            savedCanvas = storyCanvas.enabled;
            savedRaycaster = storyRaycaster != null && storyRaycaster.enabled;

            story.enabled = false;
            mapTouch.enabled = false;
            mapCamera.enabled = false;
            storyCanvas.enabled = false;
            if (storyRaycaster != null)
                storyRaycaster.enabled = false;
            overlay.gameObject.SetActive(false);
            arSession.enabled = true;
            arOrigin.camera.enabled = true;

            activeFight = Instantiate(prefab, transform);
            activeFight.Begin(arOrigin.camera, arOrigin.transform, Application.isEditor, CloseFight,
                testing ? "Back to Title" : "Back to Map");
        }

        private void CloseFight(bool won)
        {
            if (activeFight == null)
                return;

            activeFight.gameObject.SetActive(false);
            Destroy(activeFight.gameObject);
            activeFight = null;
            if (!testFight)
            {
                cleared = won;
                gate = new CyclopsLocation();
            }
            arSession.enabled = savedArSession;
            arOrigin.camera.enabled = savedArCamera;
            storyCanvas.enabled = savedCanvas;
            if (storyRaycaster != null)
                storyRaycaster.enabled = savedRaycaster;
            mapCamera.enabled = savedMapCamera;
            mapTouch.enabled = savedMapTouch;
            story.enabled = savedStory;
        }

        private void ConfigureTitleMenu()
        {
            if (title == null)
                return;

            Transform panel = title.transform;
            Transform titleObject = panel.Find("Title_Text_1_1");
            Transform search = panel.Find("SearchLocations_Button_1_2");
            Transform next = panel.Find("Continue_Button_1_3");
            if (titleObject == null || search == null || next == null)
            {
                Debug.LogWarning("Could not find the Return to Ithaca title controls.", this);
                return;
            }

            var heading = titleObject.GetComponent<Text>();
            heading.text = "Return to Ithaca";
            heading.fontSize = 112;
            heading.alignment = TextAnchor.MiddleCenter;
            heading.raycastTarget = false;
            heading.rectTransform.anchorMin = new Vector2(0.08f, 0.59f);
            heading.rectTransform.anchorMax = new Vector2(0.92f, 0.85f);
            heading.rectTransform.offsetMin = heading.rectTransform.offsetMax = Vector2.zero;

            var start = search.GetComponent<Button>();
            var startLabel = start.GetComponentInChildren<Text>(true);
            startLabel.text = "Start Journey";
            var startRect = (RectTransform)search;
            startRect.anchorMin = startRect.anchorMax = new Vector2(0.5f, 0.33f);
            startRect.anchoredPosition = Vector2.zero;
            search.gameObject.SetActive(true);

            continueJourney = next.GetComponent<Button>();
            HideLegacyButton(next);
            start.onClick.AddListener(() =>
            {
                journeyStarting = true;
                testButton.gameObject.SetActive(false);
            });

            var testRect = CyclopsView.Place(panel, "Test Cyclops", new Vector2(0.5f, 0.33f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -125), new Vector2(450, 90));
            testButton = CyclopsView.Button(testRect, "Test Cyclops", TestCyclops);
            testButton.GetComponentInChildren<Text>().fontSize = 36;
        }

        private static void HideLegacyButton(Transform button)
        {
            if (button == null)
                return;

            var visibility = button.gameObject.AddComponent<CanvasGroup>();
            visibility.alpha = 0;
            visibility.interactable = false;
            visibility.blocksRaycasts = false;
        }

        private void CreateMapHud()
        {
            var hud = new GameObject("Cyclops map overlay", typeof(RectTransform));
            hud.transform.SetParent(transform, false);
            overlay = CyclopsView.Overlay(hud);
            Vector2 top = new Vector2(0.5f, 1);
            Vector2 bottom = new Vector2(0.5f, 0);
            safePanel = CyclopsView.Place(hud.transform, "Safe area", top, top, Vector2.zero, Vector2.zero);
            marker = CyclopsView.Place(hud.transform, "Cave marker", bottom, bottom,
                Vector2.zero, new Vector2(135, 30));
            CyclopsView.Background(marker, new Color(0.46f, 0.16f, 0.04f, 0.95f));
            CyclopsView.Label(CyclopsView.Place(marker, "Name", top, top,
                Vector2.zero, marker.sizeDelta), "Cyclops Cave", 15);
            var distance = CyclopsView.Place(safePanel, "Cave distance", bottom, bottom,
                new Vector2(0, 100), new Vector2(302, 38));
            CyclopsView.Background(distance, new Color(0.08f, 0.1f, 0.15f, 0.92f));
            distanceLabel = CyclopsView.Label(CyclopsView.Place(distance, "Distance text", top, top,
                Vector2.zero, distance.sizeDelta), "", 15);
            enter = CyclopsView.Button(CyclopsView.Place(safePanel, "Enter Cave", bottom, bottom,
                new Vector2(0, 145), new Vector2(154, 45)), "Enter Cave", EnterCave);
            hud.SetActive(false);
        }

        private void OnDestroy()
        {
            if (locationProvider != null)
                locationProvider.OnLocationUpdated -= ReceiveLocation;
        }
    }
}
