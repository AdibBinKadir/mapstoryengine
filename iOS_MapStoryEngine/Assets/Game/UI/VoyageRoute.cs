using System.Collections.Generic;
using Mapbox.Examples;
using Mapbox.Unity.MeshGeneration.Factories;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Odyssey
{
    // The existing nearby-place search and StoryManager still choose and run the map chapters.
    internal sealed class VoyageRoute
    {
        private readonly StoryManager story;
        private readonly Camera mapCamera;
        private readonly TouchCamera mapTouch;
        private readonly GameObject mapTools;
        private readonly CanvasGroup mapToolsVisibility;
        private readonly Title legacyTitle;
        private readonly SearchLocationsScript search;
        private readonly GameObject searchProblem;
        private readonly UnityEngine.UI.Button searchButton;
        private bool charting;
        private bool requested;
        private bool pausedStory;
        private bool restoreTouch;

        internal VoyageRoute(Scene scene, StoryManager story, Camera mapCamera, TouchCamera mapTouch,
            GameObject mapTools, Title legacyTitle)
        {
            this.story = story;
            this.mapCamera = mapCamera;
            this.mapTouch = mapTouch;
            this.mapTools = mapTools;
            mapToolsVisibility = mapTools.AddComponent<CanvasGroup>();
            this.legacyTitle = legacyTitle;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (search == null) search = root.GetComponentInChildren<SearchLocationsScript>(true);
                foreach (var walkingRoute in root.GetComponentsInChildren<DirectionsWalking>(true))
                    walkingRoute.SuppressRoute();
                if (searchProblem == null)
                {
                    var problem = root.GetComponentInChildren<Problem>(true);
                    if (problem != null) searchProblem = problem.gameObject;
                }
            }

            if (legacyTitle != null)
            {
                var legacySearch = legacyTitle.transform.Find("SearchLocations_Button_1_2");
                if (legacySearch != null)
                    searchButton = legacySearch.GetComponent<UnityEngine.UI.Button>();
                var hidden = legacyTitle.gameObject.AddComponent<CanvasGroup>();
                hidden.alpha = 0;
                hidden.blocksRaycasts = false;
                hidden.interactable = false;
            }
            mapCamera.enabled = false;
            mapTouch.enabled = false;
            SetChartVisible(false);
        }

        internal bool ShowingChart => charting && mapCamera.enabled;
        internal bool CanToggle => !charting || mapCamera.enabled && (!story.enabled || mapTools.activeInHierarchy);
        private bool Ready => story.enabled && story.currentSiteVar >= 0;

        internal void Toggle()
        {
            if (!CanToggle)
                return;
            if (ShowingChart)
            {
                pausedStory = story.startVar;
                story.startVar = false;
                charting = false;
                mapCamera.enabled = false;
                mapTouch.enabled = false;
                return;
            }

            charting = true;
            mapCamera.enabled = true;
            mapTouch.enabled = true;
            if (!requested && searchButton != null && search != null)
            {
                requested = true;
                searchButton.onClick.Invoke();
            }
            if (pausedStory)
                story.startVar = true;
        }

        internal void SetOverlayOpen(bool open)
        {
            if (open && ShowingChart && mapTouch.enabled)
            {
                mapTouch.enabled = false;
                restoreTouch = true;
            }
            else if (!open && restoreTouch)
            {
                mapTouch.enabled = ShowingChart;
                restoreTouch = false;
            }
        }

        internal void Update()
        {
            SetChartVisible(ShowingChart);
            if (!requested || !legacyTitle.isActiveAndEnabled || !search.SearchComplete)
                return;

            if (searchProblem != null && searchProblem.activeSelf)
            {
                searchProblem.SetActive(false);
                legacyTitle.gameObject.SetActive(false);
                return;
            }
            if (!Ready)
                return;

            search.gameObject.SetActive(false);
            legacyTitle.gameObject.SetActive(false);
            story.startVar = ShowingChart;
            pausedStory = !ShowingChart;
            if (ShowingChart)
                story.ReCentreCam();
        }

        private void SetChartVisible(bool visible)
        {
            mapToolsVisibility.alpha = visible ? 1 : 0;
            mapToolsVisibility.interactable = visible;
            mapToolsVisibility.blocksRaycasts = visible;
        }

        internal string Locations(bool bossesDiscovered, bool caveCleared, bool medusaCleared)
        {
            var locations = new List<string>();
            if (Ready && story.MapSites != null)
                foreach (var site in story.MapSites)
                    if (site != null)
                        locations.Add(site.name);

            if (bossesDiscovered)
            {
                locations.Add(caveCleared ? "Cyclops Cave (cleared)" : "Cyclops Cave");
                locations.Add(medusaCleared ? "Medusa (cleared)" : "Medusa");
            }
            if (locations.Count > 0)
                return string.Join("\n\n", locations);
            if (!requested)
                return "Open the chart to discover nearby story locations.";
            if (!search.SearchComplete || legacyTitle.isActiveAndEnabled)
                return "Charting nearby locations…";
            return "No nearby story locations were available. You can still return to the AR view.";
        }
    }
}
