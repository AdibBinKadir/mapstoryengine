using Mapbox.Examples;
using Mapbox.Unity.MeshGeneration.Factories;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Odyssey
{
    internal static class VoyageRoute
    {
        internal static void Prepare(Scene scene, Camera mapCamera, TouchCamera mapTouch,
            GameObject mapTools, Title legacyTitle)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (var walkingRoute in root.GetComponentsInChildren<DirectionsWalking>(true))
                    walkingRoute.SuppressRoute();

            legacyTitle.gameObject.SetActive(false);
            mapTools.SetActive(false);
            mapCamera.enabled = false;
            mapTouch.enabled = false;
        }
    }
}
