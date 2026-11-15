using System;
using Mapbox.Examples;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Random = UnityEngine.Random;

namespace Odyssey.Cyclops
{
    public sealed class CyclopsMapEvent : MonoBehaviour
    {
        private const double MinDistance = 15;
        private const double MaxDistance = 23;
        private const double EntryRadius = 11;
        private const double MaxGpsAccuracy = 20;

        [SerializeField] private Texture medusaImage;

        private readonly BossSite cyclops = new BossSite("Cyclops Cave", "Enter Cyclops Cave");
        private readonly BossSite medusa = new BossSite("Medusa", "Face Medusa");

        private Title title;
        private AbstractMap map;
        private Camera mapCamera;
        private TouchCamera mapTouch;
        private ARSessionOrigin arOrigin;
        private ARSession arSession;
        private ILocationProvider locationProvider;
        private Location location;
        private bool mapInitialized;
        private bool sitesPlaced;
        private bool showingChart;
        private VoyageRoute route;
        private VoyageInterface voyage;
        private Canvas storyCanvas;
        private GraphicRaycaster storyRaycaster;
        private bool practiceFight;
        private int clueCountAtStart;

        private BossSite activeSite;
        private CyclopsEncounter activeCyclops;
        private MedusaEncounter medusaEncounter;
        private IFaceSignals editorFace;
        private bool savedArSession;
        private bool savedArCamera;
        private bool savedMapCamera;
        private bool savedMapTouch;
        private bool savedCanvas;
        private bool savedRaycaster;
        private bool savedStory;
        private StoryManager story;

        private Canvas overlay;
        private RectTransform safePanel;
        private Text mapMessage;
        private Text resultMessage;
        private float resultExpiresAt;

        private static double Now => (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

        private void Start()
        {
            GameObject mapTools = null;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (map == null) map = root.GetComponentInChildren<AbstractMap>(true);
                if (title == null) title = root.GetComponentInChildren<Title>(true);
                if (mapTouch == null) mapTouch = root.GetComponentInChildren<TouchCamera>(true);
                if (arOrigin == null) arOrigin = root.GetComponentInChildren<ARSessionOrigin>(true);
                if (arSession == null) arSession = root.GetComponentInChildren<ARSession>(true);
                if (mapTools != null) continue;
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect.name != "MapToolsPanel_5") continue;
                    mapTools = rect.gameObject;
                    storyCanvas = rect.GetComponentInParent<Canvas>();
                    break;
                }
            }

            mapCamera = mapTouch == null ? null : mapTouch.GetComponent<Camera>();
            story = GetComponent<StoryManager>();
            if (map == null || title == null || mapCamera == null || arOrigin == null
                || arOrigin.camera == null || arSession == null || medusaImage == null
                || storyCanvas == null || mapTools == null || story == null)
            {
                Debug.LogError("The boss map is missing its scene references.", this);
                enabled = false;
                return;
            }

            map.OnInitialized += OnMapInitialized;
            storyRaycaster = storyCanvas.GetComponent<GraphicRaycaster>();

            medusaEncounter = gameObject.AddComponent<MedusaEncounter>();
            medusaEncounter.medusaImage = medusaImage;
            gameObject.AddComponent<FaceTrackingServices>();
#if UNITY_EDITOR
            if (Services.Face == null)
                Services.Face = editorFace = gameObject.AddComponent<KeyboardFaceSignals>();
#endif
            GameEvents.EncounterEnded += OnEncounterEnded;
            CreateMapHud();
            OdysseyTheme.StyleLegacyCanvas(storyCanvas.transform);
            route = new VoyageRoute(gameObject.scene, story, mapCamera, mapTouch, mapTools, title);
            voyage = VoyageInterface.Create(arSession, arOrigin.camera, route,
                () => OpenEncounter(cyclops, true), () => OpenEncounter(medusa, true),
                () => sitesPlaced, () => cyclops.Cleared, () => medusa.Cleared);
            var manual = mapTools.transform.Find("ManualTrigger_Button_5_3");
            if (manual != null)
            {
                var hidden = manual.gameObject.AddComponent<CanvasGroup>();
                hidden.alpha = 0;
                hidden.interactable = hidden.blocksRaycasts = false;
            }
        }

        private void OnMapInitialized()
        {
            mapInitialized = true;
        }

        private void Update()
        {
            if (!VoyageState.Started)
                return;

            if (locationProvider == null)
            {
                locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
                if (locationProvider != null)
                {
                    locationProvider.OnLocationUpdated += ReceiveLocation;
                    ReceiveLocation(locationProvider.CurrentLocation);
                }
            }

            if (activeSite != null)
                return;

            double now = Now;
            bool usable = CyclopsLocation.IsUsable(location, now, MaxGpsAccuracy);
            if (!sitesPlaced && mapInitialized && usable)
                PlaceSites(now);

            bool chartOpen = route.ShowingChart;
            if (chartOpen && !showingChart && sitesPlaced)
                CenterMap();
            showingChart = chartOpen;
            overlay.gameObject.SetActive(chartOpen);
            if (!chartOpen)
                return;

            CyclopsView.FitSafeArea(safePanel);
            mapMessage.transform.parent.gameObject.SetActive(!sitesPlaced || (cyclops.Cleared && medusa.Cleared));
            mapMessage.text = sitesPlaced ? "Both bosses defeated" : "Waiting for accurate GPS…";
            resultMessage.transform.parent.gameObject.SetActive(Time.unscaledTime < resultExpiresAt);
            UpdateSite(cyclops, now, usable);
            UpdateSite(medusa, now, usable);
        }

        private void PlaceSites(double now)
        {
            Vector2d origin = location.LatitudeLongitude;
            float bearing = Random.Range(0f, 360f);
            cyclops.Position = CyclopsLocation.Offset(origin, Random.Range((float)MinDistance, (float)MaxDistance), bearing);
            medusa.Position = CyclopsLocation.Offset(origin, Random.Range((float)MinDistance, (float)MaxDistance),
                bearing + Random.Range(120f, 240f));
            sitesPlaced = true;
            cyclops.Gate.Update(location, cyclops.Position, now);
            medusa.Gate.Update(location, medusa.Position, now);
            map.UpdateMap(origin, Mathf.Max(map.Zoom, 19f));
            CenterMap();
        }

        private void ReceiveLocation(Location fix)
        {
            location = fix;
            if (!sitesPlaced)
                return;

            double now = Now;
            cyclops.Gate.Update(fix, cyclops.Position, now);
            medusa.Gate.Update(fix, medusa.Position, now);
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                return;

            cyclops.Gate.Reset();
            medusa.Gate.Reset();
        }

        private void CenterMap()
        {
            if (!mapInitialized || !CyclopsLocation.IsUsable(location, Now, MaxGpsAccuracy))
                return;

            Vector3 target = map.GeoToWorldPosition(location.LatitudeLongitude, false);
            target.y = 125f;
            mapCamera.transform.position = target;
        }

        private void UpdateSite(BossSite site, double now, bool usable)
        {
            bool visible = sitesPlaced && !site.Cleared;
            site.Button.gameObject.SetActive(visible);
            if (!visible)
            {
                site.Marker.gameObject.SetActive(false);
                return;
            }

            bool faceAvailable = site != medusa || Services.Face != null;
            bool ready = faceAvailable && VoyageState.Health > 0 && site.Gate.CanEnter(now);
            site.Button.interactable = ready;
            if (!faceAvailable)
                site.Label.text = "Medusa · Face tracking unavailable";
            else if (ready)
                site.Label.text = site.Action;
            else
                site.Label.text = usable
                    ? site.Name + " · " + Math.Round(CyclopsLocation.Distance(location.LatitudeLongitude, site.Position)) + " m"
                    : site.Name + " · Waiting for GPS";

            Vector3 pixel = mapCamera.WorldToScreenPoint(map.GeoToWorldPosition(site.Position, false) + Vector3.up);
            bool onScreen = pixel.z > 0 && Screen.safeArea.Contains(pixel);
            site.Marker.gameObject.SetActive(onScreen);
            if (onScreen)
                site.Marker.anchorMin = site.Marker.anchorMax = new Vector2(pixel.x / Screen.width, pixel.y / Screen.height);
        }

        private void EnterSite(BossSite site)
        {
            if (!route.ShowingChart || VoyageState.Health == 0 || site.Cleared || !site.Gate.CanEnter(Now))
                return;

            OpenEncounter(site, false);
        }

        private void OpenEncounter(BossSite site, bool practice)
        {
            if (activeSite != null)
                return;

            if (site == medusa)
            {
                if (Services.Face == null)
                    return;

                BeginEncounter(site, practice);
                if (practice)
                    medusaEncounter.BeginPractice();
                else
                    medusaEncounter.Begin();
                return;
            }

            var prefab = Resources.Load<CyclopsEncounter>("Odyssey/CyclopsEncounter");
            if (prefab == null)
            {
                Debug.LogError("Cyclops encounter prefab is missing.", this);
                return;
            }

            BeginEncounter(site, practice);
            activeCyclops = Instantiate(prefab, transform);
            activeCyclops.Begin(arOrigin.camera, arOrigin.transform, Application.isEditor, FinishCyclops,
                practice ? "Return to voyage" : "Back to chart", !practice);
            if (!practice)
                GameEvents.RaiseEncounterStarted(EncounterType.Cyclops);
        }

        private void BeginEncounter(BossSite site, bool practice)
        {
            activeSite = site;
            practiceFight = practice;
            clueCountAtStart = VoyageState.Clues.Count;
            resultExpiresAt = 0f;
            voyage.SetEncounter(true, practice, site == medusa);
            savedArSession = arSession.enabled;
            savedArCamera = arOrigin.camera.enabled;
            savedMapCamera = mapCamera.enabled;
            savedMapTouch = mapTouch.enabled;
            savedCanvas = storyCanvas.enabled;
            savedRaycaster = storyRaycaster != null && storyRaycaster.enabled;
            savedStory = story.enabled;
            story.enabled = false;
            storyCanvas.enabled = false;
            if (storyRaycaster != null) storyRaycaster.enabled = false;
            mapTouch.enabled = false;
            mapCamera.enabled = false;
            overlay.gameObject.SetActive(false);
            arSession.enabled = true;
            arOrigin.camera.enabled = true;
        }

        private void FinishCyclops(bool won)
        {
            activeCyclops.gameObject.SetActive(false);
            Destroy(activeCyclops.gameObject);
            activeCyclops = null;
            if (!practiceFight)
            {
                if (won)
                {
                    GameEvents.RequestClue(ClueTier.Specific);
                    if (VoyageState.Clues.Count == clueCountAtStart)
                        GameEvents.RaiseClueUnlocked("The Cyclops hid his cave away from the marked stops. Look beyond the landmarks as you explore.");
                }
                GameEvents.RaiseEncounterEnded(EncounterType.Cyclops, won);
            }
            ReturnToMap(won);
        }

        private void OnEncounterEnded(EncounterType type, bool won)
        {
            if (type != EncounterType.Medusa || activeSite != medusa)
                return;

            if (won && !practiceFight)
            {
                resultMessage.text = medusaEncounter.LastClue;
                resultExpiresAt = Time.unscaledTime + 10f;
                resultMessage.transform.parent.gameObject.SetActive(true);
            }
            ReturnToMap(won);
        }

        private void ReturnToMap(bool won)
        {
            if (!practiceFight)
            {
                activeSite.Cleared = won;
                activeSite.Gate.Reset();
            }
            activeSite = null;
            arSession.enabled = savedArSession;
            arOrigin.camera.enabled = savedArCamera;
            storyCanvas.enabled = savedCanvas;
            if (storyRaycaster != null) storyRaycaster.enabled = savedRaycaster;
            story.enabled = savedStory;
            mapCamera.enabled = savedMapCamera;
            mapTouch.enabled = savedMapTouch;
            voyage.SetEncounter(false);
            overlay.gameObject.SetActive(route.ShowingChart);
        }

        private void CreateMapHud()
        {
            var hud = new GameObject("Boss map", typeof(RectTransform));
            hud.transform.SetParent(transform, false);
            overlay = CyclopsView.Overlay(hud);
            Vector2 top = new Vector2(0.5f, 1f);
            Vector2 bottom = new Vector2(0.5f, 0f);
            var center = new Vector2(0.5f, 0.5f);
            safePanel = CyclopsView.Place(hud.transform, "Safe area", top, top, Vector2.zero, Vector2.zero);

            CreateSiteHud(cyclops, 194f);
            CreateSiteHud(medusa, 140f);
            var messagePanel = CyclopsView.Place(safePanel, "Map message", bottom, center,
                new Vector2(0f, 140f), new Vector2(320f, 48f));
            OdysseyTheme.Tint(messagePanel, OdysseyTheme.Ink);
            OdysseyTheme.Frame(messagePanel, OdysseyTheme.Bronze, 0);
            mapMessage = CyclopsView.Label(CyclopsView.Place(messagePanel, "Text", center, center,
                Vector2.zero, messagePanel.sizeDelta), "");
            var resultPanel = CyclopsView.Place(safePanel, "Clue", top, top,
                new Vector2(0f, -78f), new Vector2(322f, 84f));
            OdysseyTheme.Tint(resultPanel, OdysseyTheme.Ink);
            OdysseyTheme.Frame(resultPanel, OdysseyTheme.Bronze, 0);
            resultMessage = CyclopsView.Label(CyclopsView.Place(resultPanel, "Clue text", center, center,
                Vector2.zero, new Vector2(296f, 74f)), "", 17);
            resultPanel.gameObject.SetActive(false);
            hud.SetActive(false);
        }

        private void CreateSiteHud(BossSite site, float y)
        {
            var center = new Vector2(0.5f, 0.5f);
            var bottom = new Vector2(0.5f, 0f);
            site.Marker = CyclopsView.Place(overlay.transform, site.Name + " pin", center, center,
                Vector2.zero, new Vector2(96f, 30f));
            OdysseyTheme.Tint(site.Marker, OdysseyTheme.DeepSea);
            OdysseyTheme.Frame(site.Marker, OdysseyTheme.Bronze, 0);
            CyclopsView.Label(CyclopsView.Place(site.Marker, "Name", center, center, Vector2.zero, site.Marker.sizeDelta),
                site.Name, 13);
            var row = CyclopsView.Place(safePanel, site.Name, bottom, center,
                new Vector2(0f, y), new Vector2(286f, 46f));
            site.Button = CyclopsView.Button(row, site.Name, () => EnterSite(site));
            site.Label = site.Button.GetComponentInChildren<Text>();
            site.Marker.gameObject.SetActive(false);
            row.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (map != null) map.OnInitialized -= OnMapInitialized;
            if (locationProvider != null) locationProvider.OnLocationUpdated -= ReceiveLocation;
            GameEvents.EncounterEnded -= OnEncounterEnded;
            if (editorFace != null && ReferenceEquals(Services.Face, editorFace))
                Services.Face = null;
        }

        private sealed class BossSite
        {
            public readonly string Name;
            public readonly string Action;
            public readonly CyclopsLocation Gate = new CyclopsLocation(EntryRadius, MaxGpsAccuracy);
            public Vector2d Position;
            public bool Cleared;
            public RectTransform Marker;
            public Button Button;
            public Text Label;

            public BossSite(string name, string action)
            {
                Name = name;
                Action = action;
            }
        }
    }
}
